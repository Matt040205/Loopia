"""Sprint 5B - exporta um FBX so de animacao por modelo (armature + clipes de producao, sem malha).

Uso:
  blender -b --factory-startup --python-exit-code 7 --python exportar_animacoes.py -- <Modelo.blend> <saida.fbx>

Mesmos parametros de eixo/escala do export_fbx.py do pacote 5A.1 (rotacao temporaria de 180 graus em Z,
FBX_SCALE_UNITS, -Z frente / Y cima, sem bake_space_transform, ossos primario Y / secundario X, armature como NULL),
para os caminhos e a base de repouso baterem com o FBX neutro recebido (Avatar copiado dele na Unity).
O .blend nao e salvo: as acoes de QA so saem desta sessao em memoria para o FBX conter apenas producao.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    fonte, alvo = Path(args[0]), Path(args[1])
    bpy.ops.wm.open_mainfile(filepath=str(fonte))
    cena = bpy.context.scene
    rig = next(o for o in cena.objects if o.type == "ARMATURE")
    producao = [t.strips[0].action for t in rig.animation_data.nla_tracks if t.name.startswith("PROD ")]
    nomes = {a.name for a in producao}
    for t in list(rig.animation_data.nla_tracks):
        rig.animation_data.nla_tracks.remove(t)
    rig.animation_data.action = None
    for a in list(bpy.data.actions):
        if a.name not in nomes:
            bpy.data.actions.remove(a)
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    rig.animation_data.action = producao[0]
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    rig.matrix_world = Matrix.Rotation(math.pi, 4, "Z") @ rig.matrix_world
    bpy.context.view_layer.update()
    alvo.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(alvo), use_selection=True, global_scale=1, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        object_types={"ARMATURE"}, use_custom_props=False, add_leaf_bones=False, primary_bone_axis="Y",
        secondary_bone_axis="X", armature_nodetype="NULL", use_armature_deform_only=False,
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_step=0.5,
        bake_anim_simplify_factor=0.0, path_mode="AUTO", embed_textures=False)
    print("EXPORTADO", alvo.name, sorted(nomes), flush=True)


if __name__ == "__main__":
    main()
