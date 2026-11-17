"""Sprint 5B - utilitarios comuns das acoes de producao dos inimigos.

Convencoes (as mesmas do pacote 5A.1, ver 05_ANIMATION_HANDOFF.md):
- fonte com -Y para frente, +Z para cima, 1 u = 1 m;
- um comando de rotacao e dado num eixo do espaco da armature e convertido para o espaco local do osso pela
  inversa da rotacao de repouso (wing_motion.apply). Assim os angulos daqui tem o mesmo significado dos JSONs de QA;
- lado .R espelha o sinal dos comandos de asa/orelha;
- Root nunca e animado (clipes in-place). Oscilacao visual vai em Body (morcegos) ou Pelvis (lobo).
"""
import math

import bpy
from mathutils import Quaternion, Vector

FPS = 24
PASSO = 0.5  # chaves a cada meio frame (48 amostras/s), o mesmo passo da exportacao FBX

X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)


def armature(cena=None):
    cena = cena or bpy.context.scene
    return next(o for o in cena.objects if o.type == "ARMATURE")


class Pose:
    """Aplica comandos em eixos da armature convertidos para o local de cada osso."""

    def __init__(self, rig):
        self.rig = rig
        self._eixos = {}
        self._repouso_inv = {pb.name: pb.bone.matrix_local.to_quaternion().inverted() for pb in rig.pose.bones}

    def zerar(self):
        for pb in self.rig.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)
            pb.scale = (1, 1, 1)

    def girar(self, nome, eixo, graus):
        if not graus or nome not in self.rig.pose.bones:
            return
        pb = self.rig.pose.bones[nome]
        chave = (nome, eixo)
        if chave not in self._eixos:
            self._eixos[chave] = self._repouso_inv[nome] @ Vector(eixo)
        pb.rotation_quaternion = pb.rotation_quaternion @ Quaternion(self._eixos[chave], math.radians(graus))

    def mover(self, nome, deslocamento_armature):
        """Translacao no espaco da armature (para ossos cujo pai nao gira no repouso, como Body/Pelvis)."""
        if nome not in self.rig.pose.bones:
            return
        pb = self.rig.pose.bones[nome]
        pb.location = self._repouso_inv[nome] @ Vector(deslocamento_armature)


# ---------------------------------------------------------------- curvas


def suave(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def cosseno(x):
    x = min(1.0, max(0.0, x))
    return 0.5 - 0.5 * math.cos(math.pi * x)


def catmull(chaves, t):
    """Spline Catmull-Rom (tangentes de diferenca finita) por canal. chaves = [(t, valores...)].

    As pontas tem velocidade zero (entrada e saida suaves); as chaves internas nao param o movimento.
    Devolve o mesmo formato dos valores (dicts/tuplas aninhados sao achatados e reconstruidos).
    Lacos usam formas de onda periodicas, nao esta funcao.
    """
    ts = [k[0] for k in chaves]
    vs = [achatar(k[1]) for k in chaves]
    forma = k_forma(chaves[0][1])
    if t <= ts[0]:
        return reconstruir(vs[0], forma)
    if t >= ts[-1]:
        return reconstruir(vs[-1], forma)
    i = max(j for j in range(len(ts) - 1) if ts[j] <= t)
    t0, t1 = ts[i], ts[i + 1]
    u = (t - t0) / (t1 - t0)
    h = t1 - t0

    def tangente(j):
        if j == 0 or j == len(ts) - 1:
            return [0.0] * len(vs[j])
        # Chave que e extremo local fica com tangente zero: sem passar do valor pedido entre chaves
        # (os limites das asas sao validados por pose; a curva nao pode inventar amplitude).
        return [0.0 if (vc - va) * (vb - vc) <= 0 else (vb - va) / (ts[j + 1] - ts[j - 1])
                for va, vc, vb in zip(vs[j - 1], vs[j], vs[j + 1])]

    m0, m1 = tangente(i), tangente(i + 1)
    h00 = 2 * u ** 3 - 3 * u ** 2 + 1
    h10 = u ** 3 - 2 * u ** 2 + u
    h01 = -2 * u ** 3 + 3 * u ** 2
    h11 = u ** 3 - u ** 2
    out = [h00 * a + h10 * h * ma + h01 * b + h11 * h * mb for a, b, ma, mb in zip(vs[i], vs[i + 1], m0, m1)]
    return reconstruir(out, forma)


def k_forma(v):
    if isinstance(v, dict):
        return ("d", [(k, k_forma(v[k])) for k in sorted(v)])
    if isinstance(v, (tuple, list)):
        return ("t", [k_forma(x) for x in v])
    return ("f", None)


def achatar(v):
    if isinstance(v, dict):
        return [x for k in sorted(v) for x in achatar(v[k])]
    if isinstance(v, (tuple, list)):
        return [x for e in v for x in achatar(e)]
    return [float(v)]


def reconstruir(lista, forma):
    it = iter(lista)

    def montar(f):
        tipo, filhos = f
        if tipo == "f":
            return next(it)
        if tipo == "t":
            return tuple(montar(c) for c in filhos)
        return {k: montar(c) for k, c in filhos}

    return montar(forma)


# ---------------------------------------------------------------- acoes


def nova_acao(rig, nome):
    if nome in bpy.data.actions:
        bpy.data.actions.remove(bpy.data.actions[nome])
    acao = bpy.data.actions.new(nome)
    acao.use_fake_user = True
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = acao
    return acao


def gravar(rig, ossos, frame, com_local=()):
    for nome in ossos:
        pb = rig.pose.bones[nome]
        pb.keyframe_insert("rotation_quaternion", frame=frame, group=nome)
    for nome in com_local:
        rig.pose.bones[nome].keyframe_insert("location", frame=frame, group=nome)


def continuidade_de_quaternions(acao):
    """Garante hemisferio continuo nas chaves (sem giro de 360 graus ao interpolar)."""
    for fcs in grupos_de_quaternion(acao).values():
        if len(fcs) != 4:
            continue
        pontos = [fc.keyframe_points for fc in fcs]
        n = len(pontos[0])
        anterior = None
        for i in range(n):
            q = [p[i].co[1] for p in pontos]
            if anterior is not None and sum(a * b for a, b in zip(q, anterior)) < 0:
                for p in pontos:
                    p[i].co[1] = -p[i].co[1]
                q = [-x for x in q]
            anterior = q
        for fc in fcs:
            fc.update()


def fcurves(acao):
    if hasattr(acao, "layers") and len(acao.layers):
        return [fc for layer in acao.layers for strip in layer.strips for cb in strip.channelbags for fc in cb.fcurves]
    return list(acao.fcurves)


def grupos_de_quaternion(acao):
    grupos = {}
    for fc in fcurves(acao):
        if fc.data_path.endswith("rotation_quaternion"):
            grupos.setdefault(fc.data_path, [None] * 4)[fc.array_index] = fc
    return {k: v for k, v in grupos.items() if None not in v}


def assar(rig, nome, duracao_frames, amostra, ossos, com_local=(), marcadores=None, laco=False):
    """Cria a acao `nome` chamando amostra(t_segundos) a cada PASSO frame, de 1 ate 1+duracao_frames."""
    acao = nova_acao(rig, nome)
    n = int(round(duracao_frames / PASSO))
    for i in range(n + 1):
        f = 1 + i * PASSO
        t = (f - 1) / FPS
        if laco and i == n:
            t = 0.0  # ultima chave = primeira: laco sem salto
        amostra(t)
        gravar(rig, ossos, f, com_local)
    continuidade_de_quaternions(acao)
    acao.frame_range = (1, 1 + duracao_frames)
    acao.use_frame_range = True
    acao.use_cyclic = laco
    for nome_m, quadro in (marcadores or {}).items():
        m = acao.pose_markers.new(nome_m)
        m.frame = int(round(quadro))
    acao["loopia_laco"] = bool(laco)
    acao["loopia_duracao_s"] = duracao_frames / FPS
    return acao


def registrar_nla(rig, acoes):
    """Cada acao de producao vira uma faixa NLA silenciada, para navegar no Blender sem afetar a avaliacao."""
    ad = rig.animation_data
    for trilha in list(ad.nla_tracks):
        if trilha.name.startswith("PROD "):
            ad.nla_tracks.remove(trilha)
    for acao in acoes:
        trilha = ad.nla_tracks.new()
        trilha.name = "PROD " + acao.name
        trilha.strips.new(acao.name, int(acao.frame_range[0]), acao)
        trilha.mute = True
