"""Sprint 5B - QA das acoes de producao (nao das acoes de teste da 5A.1).

Uso:
  blender -b --factory-startup --python-exit-code 7 --python qa_clipes.py -- <pasta_blend> <saida.json>

Morcegos: o metodo da 5A.1 (wing_qa.Membrane: BVH das duas membranas fechadas sem pares que compartilham vertice,
normal transportada pela media das rotacoes de skin, area degenerada) aplicado a cada meio frame de cada clipe de
producao, mais:
  - Hit aditivo por cima de amostras de Idle/Fly/Attack (composicao local base @ hit, como o aditivo da Unity);
  - cross-fades das transicoes do Animator (nlerp a 50% entre as duas poses).
Lobo: chao (menor z de todas as malhas), deslizamento das patas no apoio, auto-intersecao do corpo (comparada ao
repouso), lacos e bounds.
Todos: ossos (angulo local maximo, continuidade de hemisferio), laco (primeira = ultima chave e derivada continua),
bounds por clipe e uniao.
"""
import json
import math
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Quaternion
from mathutils.bvhtree import BVHTree

PASSO = 0.5
CONTATO = 0.004  # dedo a menos de 4 mm do chao = apoio


def armature():
    return next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")


def vertices(obj):
    ob = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
    vs = ob.data.vertices
    xyz = np.empty(len(vs) * 3, dtype=np.float32)
    vs.foreach_get("co", xyz)
    return xyz.reshape((-1, 3)).astype(np.float64)


class Membrana:
    """Copia do metodo de wing_qa.Membrane (pacote 5A.1), para medir as acoes de producao."""

    def __init__(self, obj, rig):
        obj.data.calc_loop_triangles()
        self.obj, self.rig, self.nome = obj, rig, obj.name
        nomes = [g.name for g in obj.vertex_groups]
        self.tri = np.array([list(t.vertices) for t in obj.data.loop_triangles], dtype=int)
        self.faces = list(map(tuple, self.tri))
        self.adj = list(map(set, self.faces))
        rest = np.array([v.co[:] for v in obj.data.vertices])
        tv = rest[self.tri]
        n = np.cross(tv[:, 1] - tv[:, 0], tv[:, 2] - tv[:, 0])
        self.area = np.linalg.norm(n, axis=1)
        self.n_rep = n / self.area[:, None]
        vw = np.zeros((len(rest), len(nomes)))
        for v in obj.data.vertices:
            for e in v.groups:
                vw[v.index, e.group] = e.weight
        tw = vw[self.tri].mean(axis=1)
        idx = np.flatnonzero(vw.sum(axis=0) > 0)
        self.ossos = [nomes[i] for i in idx]
        self.tw = tw[:, idx]
        self.inv = {n: np.asarray(rig.data.bones[n].matrix_local.inverted().to_3x3()) for n in self.ossos}

    def medir(self, xyz):
        mats = np.array([np.asarray(self.rig.pose.bones[n].matrix.to_3x3()) @ self.inv[n] for n in self.ossos])
        esperado = np.einsum("fg,gij,fj->fi", self.tw, mats, self.n_rep, optimize=True)
        pos = xyz[self.tri]
        cruz = np.cross(pos[:, 1] - pos[:, 0], pos[:, 2] - pos[:, 0])
        areas = np.linalg.norm(cruz, axis=1)
        dot = (cruz * esperado).sum(axis=1) / np.maximum(areas * np.linalg.norm(esperado, axis=1), 1e-20)
        razao = areas / self.area
        sopa = BVHTree.FromPolygons(xyz.tolist(), self.faces, all_triangles=True, epsilon=0)
        cruzados = [(i, j) for i, j in sopa.overlap(sopa) if i < j and not (self.adj[i] & self.adj[j])]
        return {
            "intersecoes": len(cruzados),
            "normais_invertidas": int(np.count_nonzero(dot < 0)),
            "menor_cosseno": float(np.min(dot)),
            "degenerados": int(np.count_nonzero((areas < 2e-10) | (razao < .02))),
            "menor_razao_de_area": float(np.min(razao)),
        }, sopa


def caixa(pts):
    lo, hi = pts.min(axis=0), pts.max(axis=0)
    return {"min": lo.tolist(), "max": hi.tolist(), "dim": (hi - lo).tolist()}


def uniao(caixas):
    lo = np.min([c["min"] for c in caixas], axis=0)
    hi = np.max([c["max"] for c in caixas], axis=0)
    return {"min": lo.tolist(), "max": hi.tolist(), "dim": (hi - lo).tolist()}


def poses_da_acao(rig, acao):
    """Lista de (frame, {osso: (quat, loc)}) a cada meio frame da acao."""
    rig.animation_data.action = acao
    cena = bpy.context.scene
    ini, fim = acao.frame_range
    out = []
    f = ini
    while f <= fim + 1e-6:
        cena.frame_set(int(f), subframe=f % 1)
        out.append((f, {pb.name: (pb.rotation_quaternion.copy(), pb.location.copy()) for pb in rig.pose.bones}))
        f += PASSO
    return out


def aplicar_basis(rig, basis):
    for nome, (q, loc) in basis.items():
        pb = rig.pose.bones[nome]
        pb.rotation_quaternion = q
        pb.location = loc
    bpy.context.view_layer.update()


def angulos_e_lacos(amostras, laco):
    maximo, saltos = {}, {}
    for _, b in amostras:
        for n, (q, _) in b.items():
            maximo[n] = max(maximo.get(n, 0.0), math.degrees(2 * math.acos(min(1.0, abs(q.w)))))
    for (_, a), (_, b) in zip(amostras, amostras[1:]):
        for n in a:
            if a[n][0].dot(b[n][0]) < 0:
                saltos[n] = saltos.get(n, 0) + 1
    res = {"angulo_local_maximo": {k: round(v, 3) for k, v in maximo.items()}, "trocas_de_hemisferio": saltos}
    if laco:
        a, b = amostras[0][1], amostras[-1][1]
        res["laco_diferenca_graus"] = round(max(math.degrees(a[n][0].rotation_difference(b[n][0]).angle) for n in a), 5)
        res["laco_diferenca_local"] = round(max((a[n][1] - b[n][1]).length for n in a), 6)
        # quina na emenda: 2a diferenca angular na emenda comparada a maior 2a diferenca do interior do clipe
        def passo(x, y, n):
            return math.degrees(x[n][0].rotation_difference(y[n][0]).angle)
        seq = [s for _, s in amostras]
        emenda, interior = 0.0, 0.0
        for n in a:
            e = abs(passo(seq[0], seq[1], n) - passo(seq[-2], seq[-1], n))
            emenda = max(emenda, e)
            for i in range(1, len(seq) - 1):
                interior = max(interior, abs(passo(seq[i - 1], seq[i], n) - passo(seq[i], seq[i + 1], n)))
        res["laco_quina_na_emenda_graus"] = round(emenda, 4)
        res["laco_maior_quina_interna_graus"] = round(interior, 4)
        res["laco_emenda_sem_quina_extra"] = emenda <= interior + 1e-6
    return res


def misturar(a, b, w):
    out = {}
    for n in a:
        qa, la = a[n]
        qb, lb = b[n]
        if qa.dot(qb) < 0:
            qb = -qb
        q = Quaternion([x * (1 - w) + y * w for x, y in zip(qa, qb)]).normalized()
        out[n] = (q, la.lerp(lb, w))
    return out


def compor(base, aditivo, ref):
    out = {}
    for n in base:
        qd = ref[n][0].inverted() @ aditivo[n][0]
        out[n] = (base[n][0] @ qd, base[n][1] + (aditivo[n][1] - ref[n][1]))
    return out


def qa_morcego(cena, rig, acoes):
    malhas = [o for o in cena.objects if o.type == "MESH"]
    membranas = [Membrana(o, rig) for o in sorted((o for o in malhas if "membrana" in o.name), key=lambda o: o.name)]

    def medir_pose():
        verts = {o.name: vertices(o) for o in malhas}
        pts = np.concatenate(list(verts.values()))
        if not np.isfinite(pts).all():
            raise ValueError("vertice nao finito")
        lados, conchas = {}, []
        for m in membranas:
            r, s = m.medir(verts[m.nome])
            lados[m.nome[-1]] = r
            conchas.append(s)
        entre = len(conchas[0].overlap(conchas[1]))
        falha = entre or any(r["intersecoes"] or r["normais_invertidas"] or r["degenerados"] for r in lados.values())
        asa = np.concatenate([verts[m.nome] for m in membranas])
        return {"lados": lados, "entre_asas": entre, "falha": bool(falha), "caixa": caixa(pts),
                "envergadura_x": float(asa[:, 0].max() - asa[:, 0].min())}

    rel = {"clipes": {}, "hit_aditivo": {}, "transicoes": {}}
    poses = {}
    todas = []
    for acao in acoes:
        am = poses_da_acao(rig, acao)
        poses[acao.name] = am
        linhas = []
        for f, b in am:
            aplicar_basis(rig, b)
            m = medir_pose()
            linhas.append({"frame": f, **m})
            todas.append(m["caixa"])
        falhas = [l["frame"] for l in linhas if l["falha"]]
        rel["clipes"][acao.name] = {
            "amostras": len(linhas), "frames": [acao.frame_range[0], acao.frame_range[1]],
            "duracao_s": (acao.frame_range[1] - acao.frame_range[0]) / 24.0, "fps": 24, "laco": bool(acao.use_cyclic),
            "falhas_nos_frames": falhas,
            "menor_cosseno": min(min(l["lados"][s]["menor_cosseno"] for s in l["lados"]) for l in linhas),
            "menor_razao_de_area": min(min(l["lados"][s]["menor_razao_de_area"] for s in l["lados"]) for l in linhas),
            "envergadura_max": max(l["envergadura_x"] for l in linhas),
            "envergadura_min": min(l["envergadura_x"] for l in linhas),
            "caixa": uniao([l["caixa"] for l in linhas]),
            **angulos_e_lacos(am, acao.use_cyclic),
        }
        print("QA", acao.name, len(linhas), "falhas", len(falhas), flush=True)
    nomes = {a.name: a for a in acoes}
    pre = "Boss_" if "Boss_Idle" in nomes else ""
    hit = poses[pre + "Hit"]
    ref = hit[0][1]
    for base_nome in (pre + "Idle", pre + "Fly", pre + "Attack") + ((pre + "Revoada",) if pre else ()):
        base = poses[base_nome]
        falhas, n = [], 0
        for i in range(0, len(base), 2):
            for fh, bh in hit[1:-1:2]:
                aplicar_basis(rig, compor(base[i][1], bh, ref))
                m = medir_pose()
                todas.append(m["caixa"])
                n += 1
                if m["falha"]:
                    falhas.append((base[i][0], fh))
        rel["hit_aditivo"][base_nome] = {"combinacoes": n, "falhas": falhas}
        print("QA hit sobre", base_nome, n, "falhas", len(falhas), flush=True)
    pares = [(pre + "Idle", pre + "Fly"), (pre + "Fly", pre + "Attack"), (pre + "Idle", pre + "Attack"),
             (pre + "Attack", pre + "Fly"), (pre + "Fly", pre + "Death"), (pre + "Attack", pre + "Death"),
             (pre + "Idle", pre + "Death")]
    if pre:
        pares += [("Boss_Idle", "Boss_Revoada"), ("Boss_Fly", "Boss_Revoada"), ("Boss_Revoada", "Boss_Fly"),
                  ("Boss_Revoada", "Boss_Attack"), ("Boss_Attack", "Boss_Revoada"), ("Boss_Revoada", "Boss_Death")]
    for a, b in pares:
        falhas, n = [], 0
        A, B = poses[a], poses[b]
        for i in range(0, len(A), 3):
            for j in range(0, min(len(B), 12), 3):
                for w in (0.33, 0.66):
                    aplicar_basis(rig, misturar(A[i][1], B[j][1], w))
                    m = medir_pose()
                    n += 1
                    if m["falha"]:
                        falhas.append((A[i][0], B[j][0], w))
        rel["transicoes"][a + "->" + b] = {"combinacoes": n, "falhas": falhas}
        print("QA transicao", a, b, n, "falhas", len(falhas), flush=True)
    rel["caixa_uniao_blender"] = uniao(todas)
    return rel


def qa_lobo(cena, rig, acoes):
    malhas = [o for o in cena.objects if o.type == "MESH"]
    corpo = next(o for o in malhas if o.name.startswith("Corpo"))
    corpo.data.calc_loop_triangles()
    faces = [tuple(t.vertices) for t in corpo.data.loop_triangles]
    adj = list(map(set, faces))
    dedos = {k: [o for o in malhas if o.name.startswith(pre + " · dedo") and o.name.endswith(lado)]
             for k, (pre, lado) in {"LF": ("Fore", ".L"), "RF": ("Fore", ".R"), "LH": ("Hind", ".L"),
                                    "RH": ("Hind", ".R")}.items()}

    centros = np.array([[0.0, 0.0, 0.0]])

    def auto(xyz, onde=None):
        s = BVHTree.FromPolygons(xyz.tolist(), faces, all_triangles=True, epsilon=0)
        pares = [(i, j) for i, j in s.overlap(s) if i < j and not (adj[i] & adj[j])]
        if onde is not None:
            for i, j in pares[:3]:
                onde.append([round(float(c), 3) for c in xyz[list(faces[i])].mean(axis=0)])
        return len(pares)

    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    bpy.context.view_layer.update()
    base_auto = auto(vertices(corpo))
    rel = {"auto_intersecao_do_corpo_em_repouso": base_auto, "clipes": {}}
    todas = []
    for acao in acoes:
        am = poses_da_acao(rig, acao)
        v_esperada = float(acao.get("loopia_velocidade_modelo", 0.0))
        linhas, rastro = [], {k: [] for k in dedos}
        for f, b in am:
            aplicar_basis(rig, b)
            verts = {o.name: vertices(o) for o in malhas}
            pts = np.concatenate(list(verts.values()))
            todas.append(caixa(pts))
            onde = []
            linha = {"frame": f, "menor_z": float(pts[:, 2].min()), "auto": auto(verts[corpo.name], onde),
                     "onde": onde, "caixa": caixa(pts)}
            for k, objs in dedos.items():
                p = np.concatenate([verts[o.name] for o in objs])
                i = int(np.argmin(p[:, 2]))
                rastro[k].append((f, float(p[i, 1]), float(p[i, 2]), float(p[:, 1].mean())))
            linhas.append(linha)
        desl = {}
        if acao.name in ("Wolf_Walk", "Wolf_Run", "Wolf_Idle", "Wolf_Attack"):
            for k, r in rastro.items():
                erros = []
                for (f0, y0, z0, m0), (f1, y1, z1, m1) in zip(r, r[1:]):
                    if max(z0, z1) < CONTATO:  # pata no chao nos dois instantes
                        v = (m1 - m0) / ((f1 - f0) / 24.0)
                        erros.append(abs(v - v_esperada))
                desl[k] = {"amostras_no_apoio": len(erros), "erro_medio": float(np.mean(erros)) if erros else None,
                           "erro_max": float(np.max(erros)) if erros else None}
        rel["clipes"][acao.name] = {
            "amostras": len(linhas), "frames": [acao.frame_range[0], acao.frame_range[1]],
            "duracao_s": (acao.frame_range[1] - acao.frame_range[0]) / 24.0, "fps": 24,
            "laco": bool(acao.use_cyclic), "velocidade_modelo": v_esperada,
            "menor_z": min(l["menor_z"] for l in linhas),
            "frames_abaixo_do_chao": [l["frame"] for l in linhas if l["menor_z"] < -0.005],
            "auto_intersecao_max": max(l["auto"] for l in linhas),
            "auto_intersecao_frames": [[l["frame"], l["auto"], l["onde"]] for l in linhas if l["auto"]][:12],
            "rastro_dos_dedos": {k: [[round(a, 2), round(b, 4), round(c, 4)] for a, b, c, _ in r] for k, r in rastro.items()}
            if acao.name in ("Wolf_Walk", "Wolf_Run") else {},
            "deslizamento_no_apoio": desl,
            "caixa": uniao([l["caixa"] for l in linhas]),
            **angulos_e_lacos(am, acao.use_cyclic),
        }
        print("QA", acao.name, "menor_z %.4f" % rel["clipes"][acao.name]["menor_z"], "auto",
              rel["clipes"][acao.name]["auto_intersecao_max"], "desl",
              {k: (v["erro_medio"] and round(v["erro_medio"], 3), v["amostras_no_apoio"]) for k, v in desl.items()},
              flush=True)
    rel["caixa_uniao_blender"] = uniao(todas)
    return rel


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    pasta, saida = Path(args[0]), Path(args[1])
    modelos = args[2:] or ["Bat_Common", "Bat_AncientBoss", "Wolf_Common"]
    resultado = {"blender": bpy.app.version_string, "passo_frames": PASSO, "modelos": {}}
    for modelo in modelos:
        bpy.ops.wm.open_mainfile(filepath=str(pasta / (modelo + ".blend")))
        cena = bpy.context.scene
        rig = armature()
        acoes = [t.strips[0].action for t in rig.animation_data.nla_tracks if t.name.startswith("PROD ")]
        for t in rig.animation_data.nla_tracks:
            t.mute = True
        if modelo == "Wolf_Common":
            resultado["modelos"][modelo] = qa_lobo(cena, rig, acoes)
        else:
            resultado["modelos"][modelo] = qa_morcego(cena, rig, acoes)
    saida.parent.mkdir(parents=True, exist_ok=True)
    saida.write_text(json.dumps(resultado, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main()
