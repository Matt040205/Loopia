"""Sprint 5B - auditoria dos .blend recebidos (v1.1).

Uso:
  blender -b --factory-startup --python-exit-code 7 --python auditar_fontes.py -- <pasta_dos_blend> <saida.json>

So le. Nao salva nada nos .blend.
"""
import json
import sys
from pathlib import Path

import bpy


def identidade(m, tol=1e-6):
    return all(abs(m[i][j] - (1.0 if i == j else 0.0)) < tol for i in range(4) for j in range(4))


def auditar(caminho):
    bpy.ops.wm.open_mainfile(filepath=str(caminho))
    cena = bpy.context.scene
    rig = next(o for o in cena.objects if o.type == "ARMATURE")
    malhas = [o for o in cena.objects if o.type == "MESH"]
    ossos = [b.name for b in rig.data.bones]
    hierarquia = {b.name: (b.parent.name if b.parent else None) for b in rig.data.bones}
    pose_fora_do_repouso = []
    cena.frame_set(1)
    for pb in rig.pose.bones:
        q = pb.rotation_quaternion if pb.rotation_mode == "QUATERNION" else pb.rotation_euler.to_quaternion()
        if abs(q.w) < 0.99999 or pb.location.length > 1e-6 or (pb.scale - pb.scale.__class__((1, 1, 1))).length > 1e-6:
            pose_fora_do_repouso.append(pb.name)
    grupos_sem_osso = set()
    pesos = {"vertices": 0, "sem_peso": 0, "max_influencias": 0, "soma_fora_de_1": 0}
    malhas_info = []
    for o in malhas:
        nomes = {g.index: g.name for g in o.vertex_groups}
        for g in o.vertex_groups:
            if g.name not in ossos:
                grupos_sem_osso.add(g.name)
        for v in o.data.vertices:
            ws = [g.weight for g in v.groups if g.weight > 0 and nomes[g.group] in ossos]
            pesos["vertices"] += 1
            if not ws:
                pesos["sem_peso"] += 1
            pesos["max_influencias"] = max(pesos["max_influencias"], len(ws))
            if ws and abs(sum(ws) - 1) > 1e-3:
                pesos["soma_fora_de_1"] += 1
        malhas_info.append({
            "nome": o.name,
            "pai": o.parent.name if o.parent else None,
            "parent_type": o.parent_type,
            "matriz_identidade": identidade(o.matrix_world),
            "modificadores": [(m.type, getattr(m, "object", None) and m.object.name) for m in o.modifiers],
            "uv": [u.name for u in o.data.uv_layers],
            "materiais": [m.name if m else None for m in o.data.materials],
            "vertices": len(o.data.vertices),
            "faces": len(o.data.polygons),
        })
    acoes = []
    for a in bpy.data.actions:
        acoes.append({
            "nome": a.name,
            "fake_user": a.use_fake_user,
            "usuarios": a.users,
            "intervalo": list(a.frame_range),
            "fcurves": sum(len(cb.fcurves) for layer in a.layers for strip in layer.strips
                           for cb in strip.channelbags) if hasattr(a, "layers") else len(a.fcurves),
        })
    imagens = [{"nome": i.name, "empacotada": i.packed_file is not None, "tamanho": list(i.size)}
               for i in bpy.data.images if i.type == "IMAGE"]
    return {
        "arquivo": caminho.name,
        "blender": bpy.app.version_string,
        "fps": cena.render.fps / cena.render.fps_base,
        "frame_atual": cena.frame_current,
        "intervalo_da_cena": [cena.frame_start, cena.frame_end],
        "armature": rig.name,
        "armature_matriz_identidade": identidade(rig.matrix_world),
        "pose_position": rig.data.pose_position,
        "acao_ativa": rig.animation_data.action.name if rig.animation_data and rig.animation_data.action else None,
        "ossos": len(ossos),
        "hierarquia": hierarquia,
        "ossos_com_pose_no_frame_1": pose_fora_do_repouso,
        "grupos_sem_osso": sorted(grupos_sem_osso),
        "pesos": pesos,
        "malhas": malhas_info,
        "acoes": acoes,
        "marcadores": sorted([(m.frame, m.name) for m in cena.timeline_markers]),
        "imagens": imagens,
        "materiais": [m.name for m in bpy.data.materials],
    }


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    pasta, saida = Path(args[0]), Path(args[1])
    resultado = [auditar(pasta / (n + ".blend")) for n in ("Bat_Common", "Bat_AncientBoss", "Wolf_Common")]
    saida.parent.mkdir(parents=True, exist_ok=True)
    saida.write_text(json.dumps(resultado, ensure_ascii=False, indent=1), encoding="utf-8")
    for r in resultado:
        print("AUDIT", r["arquivo"], "ossos", r["ossos"], "malhas", len(r["malhas"]), "acoes",
              [a["nome"] for a in r["acoes"]], "pesos", r["pesos"], "grupos_sem_osso", r["grupos_sem_osso"][:5])


if __name__ == "__main__":
    main()
