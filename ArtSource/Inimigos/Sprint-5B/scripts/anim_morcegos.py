"""Sprint 5B - acoes de producao do Bat_Common e do Bat_AncientBoss.

Cada clipe e uma funcao do tempo que devolve comandos no formato do QA 5A.1:
  asa = (ombro, varredura do cotovelo, seguimento do cotovelo, punho, digitos)
  corpo = pitch/yaw/roll do Body, bob (sobe/desce do Body), cabeca, giro da cabeca, mandibula, orelhas,
          pernas, cauda, crista e avanco (Body para frente).
Sinais: ombro negativo ergue a asa; pitch positivo inclina o corpo para frente (nariz para baixo); cabeca positiva
abaixa o focinho; mandibula positiva abre; varredura positiva leva a asa para tras.
"""
import math

from animacao_base import FPS, X, Y, Z, assar, catmull, cosseno

CANAIS = ("pitch", "yaw", "roll", "bob", "head", "hyaw", "jaw", "ear", "leg", "tail", "crest", "avanco")
ZERO_ASA = (0.0, 0.0, 0.0, 0.0, 0.0)


def P(L=ZERO_ASA, R=None, **corpo):
    for k in corpo:
        if k not in CANAIS:
            raise KeyError(k)
    return {"L": tuple(map(float, L)), "R": tuple(map(float, R if R is not None else L)),
            "c": {k: float(corpo.get(k, 0.0)) for k in CANAIS}}


def aplicar(pose, p, escala_bob=1.0):
    """Mesma ordem e conversao do wing_motion.apply do pacote, com bob/avanco/giro de cabeca acrescentados."""
    pose.zerar()
    c = p["c"]
    for eixo, ang in ((Z, c["yaw"]), (Y, c["roll"]), (X, c["pitch"])):
        pose.girar("Body", eixo, ang)
    pose.mover("Body", (0.0, -c["avanco"] * escala_bob, c["bob"] * escala_bob))
    pose.girar("Head", Z, c["hyaw"])
    pose.girar("Head", X, c["head"])
    pose.girar("Jaw", X, c["jaw"])
    pose.girar("Tail", X, c["tail"])
    pose.girar("Crest", X, c["crest"])
    for sinal, lado, asa in ((1, ".L", p["L"]), (-1, ".R", p["R"])):
        sh, sw, el, wr, dg = asa
        pose.girar("Wing_Shoulder" + lado, Y, sinal * sh)
        pose.girar("Wing_Elbow" + lado, Z, sinal * sw)
        pose.girar("Wing_Elbow" + lado, Y, sinal * el)
        pose.girar("Wing_Wrist" + lado, Y, sinal * wr)
        for i in range(1, 5):
            pose.girar(f"Wing_Digit_{i:02d}" + lado, Y, sinal * dg * (1 + .12 * i))
        pose.girar("Ear" + lado, Y, sinal * c["ear"])
        pose.girar("Leg" + lado, X, c["leg"])
        pose.girar("Foot" + lado, X, -c["leg"] * .48)


# ---------------------------------------------------------------- batimento ciclico


def s_norm(fase, desce):
    """-1 com a asa no alto, +1 embaixo. Descida mais rapida que a subida, C1 nas pontas."""
    fase %= 1.0
    if fase < desce:
        return -math.cos(math.pi * fase / desce)
    return math.cos(math.pi * (fase - desce) / (1 - desce))


def bossa_subida(fase, desce):
    """0 na descida, sobe e volta a 0 durante a subida (dobra da asa)."""
    fase %= 1.0
    if fase < desce:
        return 0.0
    return math.sin(math.pi * (fase - desce) / (1 - desce)) ** 2


def bossa_descida(fase, desce):
    fase %= 1.0
    if fase >= desce:
        return 0.0
    return math.sin(math.pi * fase / desce) ** 2


def asa_no_ciclo(fase, k, ganho=1.0, dedos_extra=0.0):
    d = k["desce"]
    a_ombro = (k["baixo"] - k["cima"]) / 2 * ganho
    m_ombro = (k["baixo"] + k["cima"]) / 2
    sh = m_ombro + a_ombro * s_norm(fase, d)
    fe = fase - k["atraso"][0]
    sw = k["varre"][0] + (k["varre"][1] - k["varre"][0]) * bossa_subida(fe, d) * ganho
    el = k["cotovelo"] * s_norm(fe, d) * ganho
    fw = fase - k["atraso"][1]
    wr = k["punho"][0] + k["punho"][1] * s_norm(fw, d) * ganho
    fd = fase - k["atraso"][2]
    dg = k["dedos"][0] + k["dedos"][1] * bossa_subida(fd, d) * ganho - k["dedos"][2] * bossa_descida(fd, d) + dedos_extra
    return (sh, sw, el, wr, dg)


def ciclo(k, t, duracao, batidas):
    """Pose de um ciclo de voo com variacao lenta entre batidas (periodica no clipe inteiro: laco exato)."""
    ft = (t / duracao) % 1.0
    onda = math.cos(2 * math.pi * ft)
    onda2 = math.sin(2 * math.pi * ft)
    defas = k["defasagem"] * onda2
    ganho = 1.0 + k["variacao"] * onda
    fase = (ft * batidas) % 1.0
    L = asa_no_ciclo(fase + defas / 2, k, ganho, dedos_extra=k["dedos_alterna"] * onda)
    R = asa_no_ciclo(fase - defas / 2, k, ganho, dedos_extra=-k["dedos_alterna"] * onda)
    d = k["desce"]
    sobe = math.cos(2 * math.pi * (fase - d - 0.04))
    pitch = k["pitch"][0] + k["pitch"][1] * math.sin(2 * math.pi * (fase - d))
    c = dict(
        pitch=pitch,
        yaw=k["guinada"] * onda2,
        roll=k["rolagem"] * math.sin(2 * math.pi * ft - 0.6) + 0.35 * (L[0] - R[0]) * k["rolagem_asas"],
        bob=k["bob"] * sobe * ganho,
        # cabeca estabilizada: devolve a maior parte do pitch do corpo
        head=k["cabeca"] - 0.75 * (pitch - k["pitch"][0]) + 1.5 * onda2,
        hyaw=-0.6 * k["guinada"] * onda2,
        jaw=k["mandibula"] * (0.5 + 0.5 * math.sin(2 * math.pi * ft)),
        ear=k["orelha"][0] + k["orelha"][1] * s_norm(fase - 0.22, d),
        leg=k["perna"][0] + k["perna"][1] * s_norm(fase - 0.3, d),
        tail=k["cauda"][0] + k["cauda"][1] * s_norm(fase - 0.27, d),
        crest=k["crista"][0] + k["crista"][1] * s_norm(fase - 0.15, d),
        avanco=0.0,
    )
    return {"L": L, "R": R, "c": c}


# Parametros de voo. Faixas validadas (handoff 5A.1): comum ombro -50..+38, varredura -12..+22,
# seguimento -6,51..+8, punho -11..+9, digitos -7..+9; Anciao ombro -58..+41, varredura -13..+22.
COMUM_IDLE = dict(cima=-32, baixo=24, desce=0.46, varre=(-3, 13), cotovelo=4.0, punho=(-1.5, 5.0),
                  dedos=(1.0, 5.0, 2.0), atraso=(0.06, 0.11, 0.17), defasagem=0.035, variacao=0.08,
                  dedos_alterna=1.2, pitch=(-3.0, 1.5), guinada=3.0, rolagem=2.5, rolagem_asas=0.10,
                  bob=0.012, cabeca=2.0, mandibula=2.0, orelha=(-2.0, 4.0), perna=(4.0, 3.0),
                  cauda=(2.0, 4.0), crista=(0.0, 0.0))
COMUM_FLY = dict(cima=-45, baixo=33, desce=0.44, varre=(-6, 19), cotovelo=5.5, punho=(-2.0, 6.5),
                 dedos=(1.0, 6.0, 3.0), atraso=(0.07, 0.12, 0.19), defasagem=0.03, variacao=0.06,
                 dedos_alterna=0.8, pitch=(6.0, 2.5), guinada=2.0, rolagem=2.0, rolagem_asas=0.12,
                 bob=0.022, cabeca=-4.0, mandibula=3.0, orelha=(-8.0, 5.0), perna=(14.0, 4.0),
                 cauda=(6.0, 5.0), crista=(0.0, 0.0))
BOSS_IDLE = dict(cima=-38, baixo=28, desce=0.47, varre=(-4, 14), cotovelo=4.5, punho=(-1.5, 5.0),
                 dedos=(1.0, 5.0, 2.0), atraso=(0.08, 0.14, 0.21), defasagem=0.03, variacao=0.07,
                 dedos_alterna=1.0, pitch=(-4.0, 2.5), guinada=4.0, rolagem=3.0, rolagem_asas=0.10,
                 bob=0.05, cabeca=3.0, mandibula=4.0, orelha=(-2.0, 4.0), perna=(4.0, 4.0),
                 cauda=(2.0, 5.0), crista=(2.0, 3.0))
BOSS_FLY = dict(cima=-50, baixo=37, desce=0.45, varre=(-7, 20), cotovelo=6.0, punho=(-2.0, 6.5),
                dedos=(1.0, 6.0, 3.0), atraso=(0.09, 0.15, 0.23), defasagem=0.025, variacao=0.06,
                dedos_alterna=0.8, pitch=(7.0, 3.5), guinada=3.0, rolagem=3.0, rolagem_asas=0.12,
                bob=0.07, cabeca=-5.0, mandibula=4.0, orelha=(-8.0, 5.0), perna=(15.0, 4.0),
                cauda=(6.0, 6.0), crista=(4.0, 3.0))


# ---------------------------------------------------------------- acoes por chaves


def com_atraso(chaves, t, atraso_punho=0.03, atraso_dedos=0.06):
    """Chaves com seguimento: punho e dedos avaliados um pouco atras do resto (overlap)."""
    p = catmull(chaves, t)
    pw = catmull(chaves, max(0.0, t - atraso_punho))
    pd = catmull(chaves, max(0.0, t - atraso_dedos))
    for lado in ("L", "R"):
        sh, sw, el, _, _ = p[lado]
        p[lado] = (sh, sw, el, pw[lado][3], pd[lado][4])
    return p


def tremulo(p, t, inicio, fim, amp, freq):
    """Batimento curto por cima das chaves numa janela (sem segurar pose morta), com entrada e saida suaves."""
    if t <= inicio or t >= fim:
        return p
    peso = math.sin(math.pi * (t - inicio) / (fim - inicio))
    s = math.sin(2 * math.pi * freq * (t - inicio))
    for i, lado in enumerate(("L", "R")):
        a = list(p[lado])
        a[0] += amp * peso * math.sin(2 * math.pi * freq * (t - inicio) - 0.25 * i)
        a[3] += -0.4 * amp * peso * s
        p[lado] = tuple(a)
    p["c"]["bob"] += 0.002 * amp * peso * s
    return p


def chaves_ataque(boss):
    if not boss:
        meio = P((-10, 6, -2, 1, 2), pitch=4, head=-2, ear=-6, leg=12, tail=4)
        return 0.75, 10 / FPS, [
            (0.000, meio),
            (0.125, P((-42, 16, -6, 6, 5), (-38, 15, -5, 5, 5), pitch=-14, roll=-3, bob=0.03, head=8, jaw=6,
                      ear=-10, leg=-12, tail=-6)),
            (0.290, P((-26, 21, -5, 4, 7), (-24, 20, -5, 4, 7), pitch=20, bob=-0.01, head=-12, jaw=20,
                      ear=-14, leg=6, tail=10, avanco=0.04)),
            (0.417, P((-18, 12, -4, 2, 4), (-16, 11, -4, 2, 4), pitch=12, head=8, jaw=0, ear=-10, leg=4,
                      tail=6, avanco=0.05)),
            (0.540, P((30, 6, -3, -7, 3), (28, 6, -3, -6, 3), pitch=2, bob=0.025, head=0, jaw=2, ear=-6,
                      leg=10, tail=2)),
            (0.750, meio),
        ]
    meio = P((-12, 6, -2, 1, 2), pitch=5, head=-2, ear=-6, leg=12, tail=4, crest=3)
    return 1.0, 13 / FPS, [
        (0.000, meio),
        (0.250, P((-46, -6, 6, 8, -4), (-50, -9, 7, 6, -5), pitch=-17, yaw=4, roll=-5, bob=0.06, head=10,
                  jaw=18, ear=6, leg=16, tail=-12, crest=10)),
        (0.400, P((-30, 20, -5, 4, 7), (-28, 19, -5, 4, 7), pitch=18, bob=-0.02, head=-12, jaw=22,
                  ear=-12, leg=6, tail=10, crest=6, avanco=0.10)),
        (0.540, P((-20, 12, -4, 2, 4), (-18, 11, -4, 2, 4), pitch=12, head=9, jaw=0, ear=-10, leg=4,
                  tail=6, crest=4, avanco=0.12)),
        (0.700, P((34, 6, -3, -8, 3), (32, 6, -3, -7, 3), pitch=3, bob=0.07, head=0, jaw=3, ear=-6,
                  leg=10, tail=2, crest=2)),
        (1.000, meio),
    ]


def chaves_hit(boss):
    """Clipe aditivo: o primeiro e o ultimo quadro sao o repouso (referencia do aditivo na Unity)."""
    k = 0.7 if boss else 1.0
    zero = P()
    dur = 10 / FPS if boss else 8 / FPS
    return dur, [
        (0.0, zero),
        (dur * 0.22, P((-7 * k, 4 * k, -2 * k, 2 * k, 3 * k), (-3 * k, 2 * k, -1 * k, 1 * k, 2 * k),
                       pitch=-14 * k, roll=9 * k, yaw=-4 * k, bob=0.01 if not boss else 0.03,
                       head=-10 * k, jaw=10 * k, ear=-8 * k, leg=-6 * k, tail=-6 * k, crest=6 * k)),
        (dur * 0.55, P((2 * k, -1, 0, -1, 0), (1 * k, 0, 0, 0, 0), pitch=4 * k, roll=-3 * k, head=3 * k,
                       jaw=3 * k)),
        (dur, zero),
    ]


def chaves_morte(boss):
    if not boss:
        return 1.0, [
            (0.000, P((-10, 6, -2, 1, 2), pitch=4, head=-2, ear=-6, leg=12, tail=4)),
            (0.083, P((-30, 10, -4, 3, 4), (-22, 8, -3, 2, 3), pitch=-20, roll=10, head=-14, jaw=18, ear=-12,
                      leg=-8, tail=-8)),
            (0.290, P((24, 14, -3, -6, 5), (-36, 20, -6, 4, 6), pitch=14, roll=35, yaw=10, bob=-0.04,
                      head=10, jaw=14, ear=-4, leg=-14, tail=6)),
            (0.500, P((-46, 22, -6, 5, 6), (-48, 22, -6, 5, 6), pitch=48, roll=68, yaw=20, bob=-0.10,
                      head=24, jaw=12, ear=4, leg=-20, tail=10)),
            (0.750, P((-50, 22, -6, 5, 7), pitch=68, roll=92, yaw=24, bob=-0.15, head=28, jaw=8, ear=6,
                      leg=-22, tail=12)),
            (1.000, P((-50, 22, -6, 5, 7), pitch=74, roll=100, yaw=26, bob=-0.17, head=30, jaw=6, ear=6,
                      leg=-24, tail=12)),
        ]
    return 1.75, [
        (0.000, P((-12, 6, -2, 1, 2), pitch=5, head=-2, ear=-6, leg=12, tail=4, crest=3)),
        (0.200, P((-30, -8, 5, 2, 3), (-34, -10, 6, 1, 3), pitch=-16, head=-20, jaw=24, ear=8, leg=10,
                  tail=-12, crest=12, bob=0.05)),
        (0.450, P((-4, -11, 5, 1, 3), (-6, -12, 5, 1, 3), pitch=-12, roll=-4, head=-16, jaw=24, ear=9, leg=16,
                  tail=-14, crest=12, bob=0.08)),
        (0.750, P((30, 14, -3, -7, 5), (-30, 20, -6, 4, 6), pitch=16, roll=30, yaw=12, bob=-0.10,
                  head=10, jaw=16, ear=-4, leg=-10, tail=6, crest=4)),
        (1.100, P((-48, 22, -6, 5, 6), (-52, 22, -6, 5, 6), pitch=46, roll=62, yaw=20, bob=-0.28,
                  head=22, jaw=12, ear=4, leg=-18, tail=10, crest=0)),
        (1.450, P((-54, 22, -6, 5, 7), pitch=66, roll=86, yaw=24, bob=-0.40, head=28, jaw=8, ear=6,
                  leg=-22, tail=12)),
        (1.750, P((-56, 22, -6, 5, 7), pitch=72, roll=94, yaw=26, bob=-0.44, head=30, jaw=6, ear=6,
                  leg=-24, tail=12)),
    ]


# Revoada: Raise e Open copiados das poses validadas no QA 5A.1 (frames 351 e 367 da acao de QA).
RAISE = P((-48, -8, 5, -6, 3), (-54, -10, 8, -8, 4), pitch=-18, yaw=-5, roll=9, head=11, jaw=24, ear=9,
          leg=18, tail=-14, crest=12)
OPEN = P((-2, -11, 5, 1, 3), (-3, -13, 5, 1, 3), pitch=-15, yaw=-4, roll=5, head=11, jaw=24, ear=9, leg=18,
         tail=-14, crest=12)
REVOADA_DUR = 84 / FPS
REVOADA_RAISE_T = 48 / FPS   # instante do AoUsarEspecial quando o aviso dura 2 s
REVOADA_OPEN_T = 56 / FPS


def chaves_revoada():
    meio = P((-12, 6, -2, 1, 2), pitch=5, head=-2, ear=-6, leg=12, tail=4, crest=3)
    return [
        (0.000, meio),
        (0.500, P((28, -9, 2, -6, 4), (26, -10, 2, -6, 4), pitch=11, bob=-0.06, head=12, jaw=2, ear=-8,
                  leg=6, tail=6, crest=0)),
        (1.250, P((-24, -6, 4, -3, 3), (-27, -7, 5, -4, 3), pitch=-6, bob=0.02, head=2, jaw=10, ear=4,
                  leg=12, tail=-6, crest=7)),
        (REVOADA_RAISE_T, RAISE),
        (REVOADA_OPEN_T, OPEN),
        (64 / FPS, P((2, -11, 5, 0, 3), (1, -12, 5, 0, 3), pitch=-12, yaw=-3, roll=3, head=8, jaw=20, ear=8,
                     leg=16, tail=-12, crest=11)),
        (REVOADA_DUR, meio),
    ]


# ---------------------------------------------------------------- montagem das acoes


def ossos_animados(rig):
    return [pb.name for pb in rig.pose.bones if pb.name != "Root"]


def produzir(rig, pose, boss):
    prefixo = "Boss_" if boss else ""
    ossos = ossos_animados(rig)
    local = ("Body",)
    acoes = []

    def por_ciclo(nome, k, quadros, batidas):
        dur = quadros / FPS

        def amostra(t):
            aplicar(pose, ciclo(k, t, dur, batidas))
        return assar(rig, nome, quadros, amostra, ossos, local, laco=True,
                     marcadores={f"Batida_{i + 1}": 1 + i * quadros / batidas for i in range(batidas)})

    acoes.append(por_ciclo(prefixo + "Idle", BOSS_IDLE if boss else COMUM_IDLE, 48 if boss else 32, 2))
    acoes.append(por_ciclo(prefixo + "Fly", BOSS_FLY if boss else COMUM_FLY, 36 if boss else 24, 2))

    dur, mordida, chaves = chaves_ataque(boss)

    def ataque(t):
        aplicar(pose, com_atraso(chaves, t))
    acoes.append(assar(rig, prefixo + "Attack", round(dur * FPS), ataque, ossos, local,
                       marcadores={"Antecipacao": 1 + chaves[1][0] * FPS, "Investida": 1 + chaves[2][0] * FPS,
                                   "Mordida": 1 + mordida * FPS, "Recuperacao": 1 + chaves[4][0] * FPS}))

    dur_h, ch = chaves_hit(boss)

    def hit(t):
        aplicar(pose, com_atraso(ch, t, 0.02, 0.04))
    acoes.append(assar(rig, prefixo + "Hit", round(dur_h * FPS), hit, ossos, local,
                       marcadores={"Impacto": 1 + ch[1][0] * FPS}))

    dur_m, cm = chaves_morte(boss)

    def morte(t):
        aplicar(pose, com_atraso(cm, t, 0.04, 0.08))
    acoes.append(assar(rig, prefixo + "Death", round(dur_m * FPS), morte, ossos, local,
                       marcadores={"Impacto": 1 + cm[1][0] * FPS, "Queda": 1 + cm[3][0] * FPS}))

    if boss:
        cr = chaves_revoada()

        def revoada(t):
            p = com_atraso(cr, t, 0.04, 0.08)
            p = tremulo(p, t, 0.05, 1.7, 7.0, 1.6)
            aplicar(pose, p)
        acoes.append(assar(rig, "Boss_Revoada", 84, revoada, ossos, local,
                           marcadores={"Preparacao": 1 + 0.5 * FPS, "Revoada_Raise": 1 + REVOADA_RAISE_T * FPS,
                                       "Revoada_Open": 1 + REVOADA_OPEN_T * FPS, "Retorno": 1 + 64}))
    return acoes
