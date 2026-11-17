"""Sprint 5B - gera as acoes de producao a partir dos .blend v1.1 recebidos.

Uso:
  blender -b --factory-startup --python-exit-code 7 --python produzir.py -- <pasta_v1.1/01_Blender> <pasta_saida>

Para cada modelo: abre o fonte recebido, mantem as acoes de QA (fake user, acao ativa preservada), cria as acoes de
producao (fake user + faixas NLA silenciadas "PROD ...") e salva uma copia em <pasta_saida>/<Modelo>.blend.
Geometria, pesos, UVs, materiais e hierarquia nao sao tocados (conferido por hash de dados ao final).
"""
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import anim_lobo  # noqa: E402
import anim_morcegos  # noqa: E402
from animacao_base import Pose, armature, registrar_nla  # noqa: E402

NOTA = (
    "Loopia - Sprint 5B (acoes de producao)\n\n"
    "As acoes de QA da 5A.1 continuam intactas (fake user) e a acao ativa ao abrir e a mesma do pacote.\n"
    "As acoes de producao estao como faixas NLA silenciadas 'PROD <nome>' e com fake user.\n"
    "Para editar: Action Editor > escolha a acao (ex.: Idle, Fly, Boss_Revoada, Wolf_Run).\n"
    "Geradas por ArtSource/Inimigos/Sprint-5B/scripts/produzir.py (curvas por chave a cada meio frame, 24 fps).\n"
    "Root nunca e animado. Oscilacao visual em Body (morcegos) ou Pelvis (lobo).\n"
    "Lobo: IK analitico no plano sagital assado em FK; nenhum osso ou restricao extra no arquivo.\n"
)


def assinatura_dos_dados():
    """Hash de geometria, pesos, UVs, hierarquia de ossos e materiais: prova de que nada disso mudou."""
    h = hashlib.sha256()
    cena = bpy.context.scene
    rig = armature(cena)
    for b in rig.data.bones:
        h.update(repr((b.name, b.parent.name if b.parent else None, tuple(round(x, 6) for x in b.head_local),
                       tuple(round(x, 6) for x in b.tail_local), round(b.roll if hasattr(b, "roll") else 0, 6))).encode())
    for o in sorted((o for o in cena.objects if o.type == "MESH"), key=lambda o: o.name):
        h.update(o.name.encode())
        for v in o.data.vertices:
            h.update(repr((tuple(round(c, 6) for c in v.co),
                           tuple(sorted((o.vertex_groups[g.group].name, round(g.weight, 6)) for g in v.groups)))).encode())
        for uv in o.data.uv_layers:
            h.update(repr([tuple(round(c, 6) for c in d.uv) for d in uv.data]).encode())
        h.update(repr([m.name if m else None for m in o.data.materials]).encode())
    return h.hexdigest()


def autoteste_ik(rig, pose, ik):
    """Com os alvos no repouso o IK precisa reproduzir o repouso das pernas (< 0,5 grau)."""
    pose.zerar()
    bpy.context.view_layer.update()
    pior = 0.0
    for k in anim_lobo.PERNAS:
        ik.resolver(k, ik.alvo_repouso[k].copy())
        pre, lado = anim_lobo.PERNAS[k]
        for s in ("Upper", "Lower", "Ankle", "Paw"):
            q = rig.pose.bones[pre + s + lado].rotation_quaternion
            pior = max(pior, math.degrees(2 * math.acos(min(1.0, abs(q.w)))))
    pose.zerar()
    return pior


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    origem, destino = Path(args[0]), Path(args[1])
    destino.mkdir(parents=True, exist_ok=True)
    resumo = {}
    for modelo in ("Bat_Common", "Bat_AncientBoss", "Wolf_Common"):
        bpy.ops.wm.open_mainfile(filepath=str(origem / (modelo + ".blend")))
        cena = bpy.context.scene
        rig = armature(cena)
        antes = assinatura_dos_dados()
        ativa = rig.animation_data.action
        qa = sorted(a.name for a in bpy.data.actions)
        pose = Pose(rig)
        info = {"acoes_de_qa": qa, "acao_ativa_preservada": ativa.name if ativa else None}
        if modelo == "Wolf_Common":
            ik = anim_lobo.IKdoLobo(rig, pose)
            info["autoteste_ik_graus"] = autoteste_ik(rig, pose, ik)
            if info["autoteste_ik_graus"] > 0.5:
                raise RuntimeError("IK nao reproduz o repouso: %.3f graus" % info["autoteste_ik_graus"])
            acoes, ik = anim_lobo.produzir(rig, pose)
            info["ik_recortes"] = ik.recortes
            info["ik_recortes_total"] = len(ik.recortes)
            info["velocidade_walk"] = anim_lobo.velocidade(anim_lobo.WALK)
            info["velocidade_run"] = anim_lobo.velocidade(anim_lobo.RUN)
        else:
            acoes = anim_morcegos.produzir(rig, pose, modelo.endswith("Boss"))
        registrar_nla(rig, acoes)
        rig.animation_data.action = ativa
        pose.zerar()
        cena.frame_set(1)
        texto = bpy.data.texts.get("LEIA · Sprint 5B") or bpy.data.texts.new("LEIA · Sprint 5B")
        texto.clear()
        texto.write(NOTA)
        depois = assinatura_dos_dados()
        if antes != depois:
            raise RuntimeError(modelo + ": geometria/pesos/UV/ossos mudaram")
        info["assinatura_dos_dados"] = depois
        info["acoes_de_producao"] = [
            {"nome": a.name, "quadros": [a.frame_range[0], a.frame_range[1]],
             "duracao_s": a["loopia_duracao_s"], "laco": bool(a["loopia_laco"]),
             "marcadores": {m.name: m.frame for m in a.pose_markers},
             **({"velocidade_modelo": a["loopia_velocidade_modelo"]} if "loopia_velocidade_modelo" in a else {})}
            for a in acoes]
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(destino / (modelo + ".blend")), compress=True, relative_remap=False)
        resumo[modelo] = info
        print("PRODUZIDO", modelo, [a.name for a in acoes], {k: v for k, v in info.items() if "ik" in k or "vel" in k})
    (destino / "acoes_de_producao.json").write_text(json.dumps(resumo, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main()
