"""Sprint 5B - acoes de producao do Wolf_Common.

Locomocao com IK auxiliar analitico (plano sagital) assado em FK nos ossos exportados:
- cada pata tem um alvo (cabeca do osso Paw) no espaco da armature; no apoio o alvo fica no chao e anda para tras
  exatamente a velocidade do clipe (sem deslizar); no balanco faz um arco para frente;
- Base (escapula/quadril) gira em FK para ganhar alcance; Upper/Lower resolvem 2 ossos ate o "joelho" e Ankle
  fecha a cadeia; Paw mantem a orientacao de repouso (plana) no apoio e dobra no balanco;
- nenhum osso novo, nenhuma restricao fica no arquivo: so chaves de rotacao (e local do Pelvis).

Velocidades (unidades do modelo/s) calculadas por v = S / (duty * T); a Unity recebe esses valores como limiares do
blend tree (ver exportar/relatorio).
"""
import math

import bpy
from mathutils import Matrix, Vector

from animacao_base import FPS, X, Y, Z, assar, catmull, cosseno, suave

PERNAS = {
    # nome: (prefixo, lado, fase da caminhada, fase da corrida)
    "LH": ("Hind_", ".L"),
    "RH": ("Hind_", ".R"),
    "LF": ("Fore_", ".L"),
    "RF": ("Fore_", ".R"),
}

# Caminhada em sequencia lateral; corrida em galope transverso.
WALK = dict(T=15 / FPS, duty=0.55, S=0.34, altura={"Fore_": 0.05, "Hind_": 0.06}, fases={"LH": 0.0, "LF": 0.25, "RH": 0.5, "RF": 0.75},
            agacha=0.025, base=(9.0, 6.0))
RUN = dict(T=9 / FPS, duty=0.21, S=0.36, altura={"Fore_": 0.085, "Hind_": 0.11}, fases={"LH": 0.0, "RH": 0.08, "LF": 0.42, "RF": 0.50},
           agacha=0.04, base=(9.0, 6.0), dobra=18.0)


def velocidade(g):
    return g["S"] / (g["duty"] * g["T"])


class IKdoLobo:
    def __init__(self, rig, pose):
        self.rig = rig
        self.pose = pose
        self.ossos = {pb.name: pb for pb in rig.pose.bones}
        self.repouso = {b.name: b.matrix_local.copy() for b in rig.data.bones}
        self.cauda = {b.name: b.tail_local.copy() for b in rig.data.bones}
        self.cabeca = {b.name: b.head_local.copy() for b in rig.data.bones}
        self.comp = {b.name: b.length for b in rig.data.bones}
        self.recortes = []
        self.alvo_repouso = {}
        self.pivo_repouso = {}
        self.pata = {}
        for k, (pre, lado) in PERNAS.items():
            self.alvo_repouso[k] = self.cauda[pre + "Ankle" + lado].copy()
            # pivo do "toe-off": articulacao Paw/Toes (os dedos ficam chapados no chao enquanto o calcanhar sobe)
            self.pivo_repouso[k] = self.cauda[pre + "Paw" + lado].copy()
            self.pata[k] = self.pivo_repouso[k] - self.alvo_repouso[k]
        self.quadro = None

    def resolver_pe(self, k, pivo, theta, delta_tornozelo=0.0, dedos=0.0):
        """Pata pelo pivo dos dedos: o alvo do IK (cabeca do Paw) sai do pivo menos o vetor da pata girado por theta."""
        alvo = pivo - Matrix.Rotation(math.radians(theta), 3, "X") @ self.pata[k]
        self.resolver(k, alvo, delta_tornozelo, theta, dedos)

    @staticmethod
    def ang(v):  # angulo no plano (y, z)
        return math.atan2(v.z, v.y)

    def resolver(self, k, alvo, delta_tornozelo=0.0, dobra_pata=0.0, dobra_dedos=0.0):
        """Cadeia Upper-Lower-Ankle ate o alvo; Paw com a orientacao de repouso girada por dobra_pata (graus)."""
        pre, lado = PERNAS[k]
        nb, nu, nl, na, np_, nt = (pre + s + lado for s in ("Base", "Upper", "Lower", "Ankle", "Paw", "Toes"))
        base = self.ossos[nb]
        ombro = base.tail.copy()  # pose avaliada (Base ja girado em FK)
        Ru, Rl, Ra, Rp = (self.repouso[n] for n in (nu, nl, na, np_))
        Lu, Ll, La = self.comp[nu], self.comp[nl], self.comp[na]
        # tornozelo: angulo de repouso no plano + delta pedido
        dir_a_rep = Ra.col[1].xyz
        th_a = self.ang(dir_a_rep) + math.radians(delta_tornozelo)
        joelho = alvo - La * Vector((0.0, math.cos(th_a), math.sin(th_a)))
        joelho.x = alvo.x
        d_vec = Vector((0.0, joelho.y - ombro.y, joelho.z - ombro.z))
        d = d_vec.length
        lim_max, lim_min = Lu + Ll - 1e-4, abs(Lu - Ll) + 1e-4
        if d > lim_max or d < lim_min:
            self.recortes.append((k, self.quadro, round(d - (lim_max if d > lim_max else lim_min), 4)))
            d = min(lim_max, max(lim_min, d))
        base_ang = self.ang(d_vec)
        alfa = math.acos(max(-1.0, min(1.0, (Lu * Lu + d * d - Ll * Ll) / (2 * Lu * d))))
        # lado da dobra igual ao do repouso
        # cruz(u, d) < 0 no repouso: o Upper fica no sentido anti-horario da reta ombro->joelho
        u_rep = Ru.col[1].xyz
        alvo_rep = self.cauda[nl]
        cruz = u_rep.y * (alvo_rep.z - self.cabeca[nu].z) - u_rep.z * (alvo_rep.y - self.cabeca[nu].y)
        th_u = base_ang + alfa if cruz < 0 else base_ang - alfa
        cotovelo = Vector((ombro.x, ombro.y + Lu * math.cos(th_u), ombro.z + Lu * math.sin(th_u)))
        th_l = self.ang(Vector((0.0, joelho.y - cotovelo.y, joelho.z - cotovelo.z)))

        def girada(R, theta):
            delta = theta - self.ang(R.col[1].xyz)
            return Matrix.Rotation(delta, 3, "X") @ R.to_3x3()

        Mu = Matrix.Translation(ombro) @ girada(Ru, th_u).to_4x4()
        Ml = Matrix.Translation(cotovelo) @ girada(Rl, th_l).to_4x4()
        Ma = Matrix.Translation(joelho) @ girada(Ra, th_a).to_4x4()
        Mp = Matrix.Translation(alvo) @ (Matrix.Rotation(math.radians(dobra_pata), 3, "X") @ Rp.to_3x3()).to_4x4()
        pai = base.matrix.copy()
        pai_rep = self.repouso[nb]
        for nome, M in ((nu, Mu), (nl, Ml), (na, Ma), (np_, Mp)):
            R = self.repouso[nome]
            local = (pai @ pai_rep.inverted() @ R).inverted() @ M
            pb = self.ossos[nome]
            pb.rotation_quaternion = local.to_quaternion()
            pai, pai_rep = M, R
        self.ossos[nt].rotation_quaternion = (1, 0, 0, 0)
        self.pose.girar(nt, X, dobra_dedos)


TOE_OFF = 30.0   # graus que o metacarpo/metatarso levanta no fim do apoio, girando sobre os dedos
DOBRA = 32.0     # dobra extra da pata no meio do balanco
CARPO = 22.0     # flexao do carpo (dianteira) no meio do balanco
JARRETE = 12.0   # flexao do jarrete (traseira) no meio do balanco
CURVA = 14.0     # dedos curvados no balanco


def hermite(p0, p1, m0, m1, s):
    h00 = 2 * s ** 3 - 3 * s ** 2 + 1
    h10 = s ** 3 - 2 * s ** 2 + s
    h01 = -2 * s ** 3 + 3 * s ** 2
    h11 = s ** 3 - s ** 2
    return h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1


def pe_no_ciclo(g, k, fase):
    """Pivo dos dedos relativo ao repouso (dy, dz), giro da pata, giro dos dedos e posicao relativa no passo.

    Apoio: o pivo fica no chao e anda para tras exatamente a velocidade do clipe; no fim o calcanhar sobe girando
    sobre os dedos (dedos chapados: giro dos dedos = -giro da pata). Balanco: arco para frente com a pata dobrada.
    """
    pre = PERNAS[k][0]
    d, S, h = g["duty"], g["S"], g["altura"][pre]
    p = (fase - g["fases"][k]) % 1.0
    if p < d:
        s = p / d
        dy = -S / 2 + S * s
        th = TOE_OFF * suave((s - 0.65) / 0.35)
        return dy, 0.0, th, -th, dy / (S / 2), 0.0
    s = (p - d) / (1 - d)
    # velocidade do pe casada com o chao na saida e na chegada (recolhe antes do contato): sem quina no apoio
    m = min(S / (d * g["T"]) * (1 - d) * g["T"], 0.5 * S)
    dy = hermite(S / 2, -S / 2, m, m, s)
    dz = h * math.sin(math.pi * s) ** 1.2
    sai = 1 - suave(s / 0.3)
    th = TOE_OFF * sai + g.get("dobra", DOBRA) * math.sin(math.pi * min(1.0, s / 0.85)) ** 2
    dedos = -TOE_OFF * sai + CURVA * math.sin(math.pi * s) ** 2
    return dy, dz, th, dedos, dy / (S / 2), math.sin(math.pi * s) ** 2


def posar_corpo(pose, c):
    """c: dict com pelvis (dz, dy, pitch, roll, yaw), spine, chest, neck, head, hyaw, jaw, ear, tail (3), base."""
    pose.zerar()
    pose.mover("Pelvis", (0.0, c.get("dy", 0.0), c.get("dz", 0.0)))
    for eixo, ang in ((Z, c.get("yaw", 0.0)), (Y, c.get("roll", 0.0)), (X, c.get("pitch", 0.0))):
        pose.girar("Pelvis", eixo, ang)
    pose.girar("Spine", X, c.get("spine", 0.0))
    pose.girar("Spine", Z, c.get("spine_yaw", 0.0))
    pose.girar("Chest", X, c.get("chest", 0.0))
    pose.girar("Chest", Y, c.get("chest_roll", 0.0))
    pose.girar("Neck", X, c.get("neck", 0.0))
    pose.girar("Neck", Z, c.get("neck_yaw", 0.0))
    pose.girar("Head", Z, c.get("hyaw", 0.0))
    pose.girar("Head", X, c.get("head", 0.0))
    pose.girar("Jaw", X, c.get("jaw", 0.0))
    for sinal, lado in ((1, ".L"), (-1, ".R")):
        pose.girar("Ear" + lado, X, c.get("ear", 0.0) + sinal * c.get("ear_lr", 0.0))
        pose.girar("Ear" + lado, Y, sinal * c.get("ear_abre", 0.0))
    t1, t2, t3 = c.get("tail", (0.0, 0.0, 0.0))
    ty1, ty2, ty3 = c.get("tail_yaw", (0.0, 0.0, 0.0))
    for nome, a, b in (("Tail_01", t1, ty1), ("Tail_02", t2, ty2), ("Tail_03", t3, ty3)):
        pose.girar(nome, X, a)
        pose.girar(nome, Z, b)


def girar_bases(pose, angulos):
    for k, (pre, lado) in PERNAS.items():
        pose.girar(pre + "Base" + lado, X, angulos.get(k, 0.0))


def produzir(rig, pose):
    ik = IKdoLobo(rig, pose)
    ossos = [pb.name for pb in rig.pose.bones if pb.name != "Root"]
    local = ("Pelvis",)
    acoes = []
    vl = bpy.context.view_layer

    def com_pes(corpo, pes, bases):
        """pes: {k: (dy, dz, giro_da_pata, giro_dos_dedos, delta_tornozelo)}; dy/dz movem o pivo dos dedos."""
        posar_corpo(pose, corpo)
        girar_bases(pose, bases)
        vl.update()
        for k, (dy, dz, th, dedos, dtorn) in pes.items():
            ik.resolver_pe(k, ik.pivo_repouso[k] + Vector((0.0, dy, dz)), th, dtorn, dedos)

    # ------------------------------------------------ Idle (3 s): respiracao, olhar, orelhas e cauda
    def idle(t):
        u = t / 3.0
        resp = math.sin(2 * math.pi * 2 * u)
        olhar = math.sin(2 * math.pi * u)
        corpo = dict(dz=-0.006 + 0.004 * resp, pitch=0.4 * resp, spine=-0.6 * resp, chest=0.9 * resp,
                     neck=-1.0 * resp + 2.0, head=1.5 * math.sin(2 * math.pi * u + 1.2) + 1.0,
                     hyaw=9.0 * olhar, neck_yaw=4.0 * olhar, jaw=1.0 + 1.0 * (0.5 + 0.5 * resp),
                     ear=-2.0 + 3.0 * max(0.0, math.sin(2 * math.pi * 3 * u)) ** 6,
                     ear_lr=4.0 * math.sin(2 * math.pi * u + 0.5),
                     tail=(-2.0 + 1.0 * resp, 2.0, 3.0),
                     tail_yaw=tuple(7.0 * math.sin(2 * math.pi * 2 * u - 0.5 * i) for i in range(3)))
        com_pes(corpo, {k: (0.0, 0.0, 0.0, 0.0, 0.0) for k in PERNAS}, {})
    acoes.append(assar(rig, "Wolf_Idle", 72, idle, ossos, local, laco=True))

    # ------------------------------------------------ Walk / Run
    def locomocao(g, nome, corrida):
        quadros = round(g["T"] * FPS)

        def amostra(t):
            fase = (t / g["T"]) % 1.0
            pes, bases = {}, {}
            ik.quadro = round(1 + t * FPS, 2)
            for k in PERNAS:
                dy, dz, th, dedos, rel, balanco = pe_no_ciclo(g, k, fase)
                pre = PERNAS[k][0]
                kb = g["base"][0] if pre == "Fore_" else g["base"][1]
                bases[k] = kb * rel
                # carpo/jarrete dobram no balanco (a pata vai para tras e para cima): o cotovelo nao leva tudo
                torn = (-6.0 * rel - CARPO * balanco if pre == "Fore_" else 4.0 * rel + JARRETE * balanco)
                pes[k] = (dy, dz, th, dedos, torn)
            w = 2 * math.pi * fase
            if corrida:
                flex = math.cos(w - 2 * math.pi * 0.8)
                corpo = dict(dz=-g["agacha"] + 0.03 * math.cos(w - 2 * math.pi * 0.8),
                             pitch=5.0 * math.cos(w - 2 * math.pi * 0.55),
                             spine=5.0 * flex, chest=3.0 * flex,
                             neck=-3.0 - 0.5 * 5.0 * math.cos(w - 2 * math.pi * 0.55) + 6.0,
                             head=-2.0 - 0.4 * 5.0 * math.cos(w - 2 * math.pi * 0.55),
                             jaw=6.0 + 2.0 * math.sin(w), ear=-24.0, ear_abre=-6.0,
                             tail=(12.0 + 4.0 * math.sin(w - 1.0), 6.0 + 5.0 * math.sin(w - 1.6),
                                   4.0 + 6.0 * math.sin(w - 2.2)),
                             tail_yaw=(0.0, 3.0 * math.sin(w - 1.3), 5.0 * math.sin(w - 2.0)))
            else:
                w2 = 2 * w
                corpo = dict(dz=-g["agacha"] + 0.008 * math.cos(w2), roll=2.0 * math.sin(w),
                             chest_roll=-2.5 * math.sin(w - 0.4), spine_yaw=2.0 * math.sin(w),
                             pitch=0.8 * math.cos(w2), neck=2.0 - 1.6 * math.cos(w2 - 0.6),
                             head=1.0 - 1.2 * math.cos(w2 - 0.9), hyaw=-2.0 * math.sin(w - 0.3),
                             jaw=2.0, ear=-6.0, ear_lr=2.0 * math.sin(w),
                             tail=(2.0, 3.0, 4.0),
                             tail_yaw=tuple(9.0 * math.sin(w - 0.7 - 0.6 * i) for i in range(3)))
            com_pes(corpo, pes, bases)
        marc = {}
        for k in PERNAS:
            marc["Apoio_" + k] = 1 + g["fases"][k] * quadros
        acao = assar(rig, nome, quadros, amostra, ossos, local, laco=True, marcadores=marc)
        acao["loopia_velocidade_modelo"] = velocidade(g)
        return acao

    acoes.append(locomocao(WALK, "Wolf_Walk", False))
    acoes.append(locomocao(RUN, "Wolf_Run", True))

    # ------------------------------------------------ Attack: antecipacao curta, bote e mordida em 4/24 s
    zero = dict(dz=0.0, dy=0.0, pitch=0.0, spine=0.0, chest=0.0, neck=0.0, head=0.0, hyaw=0.0, jaw=0.0,
                ear=0.0, t1=0.0, t2=0.0)
    ch_ataque = [
        (0.000, dict(zero)),
        (0.090, dict(zero, dz=-0.035, dy=0.045, pitch=-3.0, spine=3.0, chest=-2.0, neck=-6.0, head=-6.0,
                     jaw=10.0, ear=-22.0, t1=-6.0, t2=-4.0)),
        (0.140, dict(zero, dz=-0.010, dy=-0.090, pitch=6.0, spine=-3.0, chest=4.0, neck=14.0, head=6.0,
                     jaw=28.0, ear=-26.0, t1=4.0, t2=6.0)),
        (0.167, dict(zero, dz=-0.012, dy=-0.100, pitch=6.5, spine=-3.0, chest=4.0, neck=15.0, head=9.0,
                     jaw=0.0, ear=-26.0, t1=5.0, t2=7.0)),
        (0.330, dict(zero, dz=-0.020, dy=-0.040, pitch=2.0, neck=6.0, head=3.0, jaw=6.0, ear=-14.0, t1=3.0,
                     t2=3.0)),
        (0.625, dict(zero)),
    ]

    def ataque(t):
        c = catmull(ch_ataque, t)
        corpo = dict(dz=c["dz"], dy=c["dy"], pitch=c["pitch"], spine=c["spine"], chest=c["chest"],
                     neck=c["neck"], head=c["head"], jaw=c["jaw"], ear=c["ear"],
                     tail=(c["t1"], c["t2"], c["t2"] * 0.8))
        com_pes(corpo, {k: (0.0, 0.0, 0.0, 0.0, 0.0) for k in PERNAS}, {})
    acoes.append(assar(rig, "Wolf_Attack", 15, ataque, ossos, local,
                       marcadores={"Antecipacao": 1 + 0.09 * FPS, "Mordida": 1 + 4, "Recuperacao": 1 + 0.33 * FPS}))

    # ------------------------------------------------ Hit aditivo: so pescoco, cabeca, orelhas e cauda
    ch_hit = [
        (0.000, dict(neck=0.0, head=0.0, hyaw=0.0, jaw=0.0, ear=0.0, t1=0.0)),
        (0.075, dict(neck=-7.0, head=-12.0, hyaw=14.0, jaw=10.0, ear=-26.0, t1=12.0)),
        (0.190, dict(neck=2.0, head=3.0, hyaw=-4.0, jaw=4.0, ear=-12.0, t1=4.0)),
        (8 / FPS, dict(neck=0.0, head=0.0, hyaw=0.0, jaw=0.0, ear=0.0, t1=0.0)),
    ]

    def hit(t):
        c = catmull(ch_hit, t)
        # so pescoco/cabeca/orelhas/cauda: como e aditivo na Unity, nada que mova as patas
        posar_corpo(pose, dict(neck=c["neck"], head=c["head"], hyaw=c["hyaw"], jaw=c["jaw"], ear=c["ear"],
                               tail=(c["t1"], c["t1"] * 0.5, 0.0)))
    acoes.append(assar(rig, "Wolf_Hit", 8, hit, ossos, local, marcadores={"Impacto": 1 + 0.075 * FPS}))

    # ------------------------------------------------ Death: tranco, pernas cedem, cai de lado
    ch_morte = [
        (0.000, dict(dz=0.0, roll=0.0, pitch=0.0, neck=0.0, head=0.0, jaw=0.0, ear=0.0, fl=0.0, hl=0.0, t1=0.0)),
        (0.170, dict(dz=0.02, roll=-4.0, pitch=-4.0, neck=-10.0, head=-14.0, jaw=22.0, ear=-26.0, fl=0.0, hl=0.0,
                     t1=10.0)),
        (0.420, dict(dz=-0.12, roll=10.0, pitch=4.0, neck=8.0, head=10.0, jaw=14.0, ear=-20.0, fl=30.0, hl=20.0,
                     t1=-6.0)),
        (0.750, dict(dz=-0.24, roll=52.0, pitch=4.0, neck=14.0, head=16.0, jaw=10.0, ear=-14.0, fl=24.0, hl=18.0,
                     t1=-12.0)),
        (1.250, dict(dz=-0.30, roll=84.0, pitch=2.0, neck=18.0, head=20.0, jaw=6.0, ear=-10.0, fl=10.0, hl=8.0,
                     t1=-16.0)),
    ]
    malhas = [o for o in bpy.context.scene.objects if o.type == "MESH"]

    def menor_z():
        dg = bpy.context.evaluated_depsgraph_get()
        m = 1e9
        for o in malhas:
            ev = o.evaluated_get(dg)
            for v in ev.data.vertices:
                if v.co.z < m:
                    m = v.co.z
        return m

    def morte(t):
        c = catmull(ch_morte, t)
        corpo = dict(dz=c["dz"], roll=c["roll"], pitch=c["pitch"], neck=c["neck"], head=c["head"], jaw=c["jaw"],
                     ear=c["ear"], tail=(c["t1"], c["t1"] * 0.7, c["t1"] * 0.4))
        posar_corpo(pose, corpo)
        for k, (pre, lado) in PERNAS.items():
            dobra = c["fl"] if pre == "Fore_" else c["hl"]
            pose.girar(pre + "Upper" + lado, X, (-1 if pre == "Fore_" else 1) * dobra * 0.6)
            pose.girar(pre + "Lower" + lado, X, (1 if pre == "Fore_" else -1) * dobra)
            pose.girar(pre + "Paw" + lado, X, dobra * 0.5)
        vl.update()
        z = menor_z()
        # nunca atravessa o chao; da metade da queda em diante encosta no chao (sem flutuar)
        if z < 0.002 or (t >= 0.75 and z > 0.002):
            corpo["dz"] += (0.002 - z) * (1.0 if z < 0.002 else suave((t - 0.75) / 0.25))
            posar_corpo(pose, corpo)
            for k, (pre, lado) in PERNAS.items():
                dobra = c["fl"] if pre == "Fore_" else c["hl"]
                pose.girar(pre + "Upper" + lado, X, (-1 if pre == "Fore_" else 1) * dobra * 0.6)
                pose.girar(pre + "Lower" + lado, X, (1 if pre == "Fore_" else -1) * dobra)
                pose.girar(pre + "Paw" + lado, X, dobra * 0.5)
    acoes.append(assar(rig, "Wolf_Death", 30, morte, ossos, local,
                       marcadores={"Impacto": 1 + 4, "Pernas_cedem": 1 + 10, "No_chao": 30}))
    return acoes, ik
