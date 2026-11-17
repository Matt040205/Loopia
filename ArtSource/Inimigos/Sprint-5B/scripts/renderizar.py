"""Sprint 5B - renderiza os quadros de cada acao de producao (geometria real, Workbench com o atlas).

Uso:
  blender -b --factory-startup --python-exit-code 7 --python renderizar.py -- <pasta_blend> <pasta_saida> [Modelo ...]

Saida: <pasta_saida>/<Modelo>/<Acao>/<vista>_<quadro>.png (um PNG por frame inteiro). O script montar_evidencias.py
(Python comum + Pillow) junta em GIF e pranchas.
"""
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

VISTAS = {
    "Bat_Common": {"jogo": (35, 28, 1.25, (0, 0, 0.02)), "frente": (0, 6, 1.25, (0, 0, 0.02))},
    "Bat_AncientBoss": {"jogo": (35, 28, 3.6, (0, 0, 0.2)), "frente": (0, 6, 3.6, (0, 0, 0.2))},
    "Wolf_Common": {"lado": (90, 4, 1.9, (0, 0.05, 0.42)), "jogo": (35, 28, 2.0, (0, 0.05, 0.42))},
}


def preparar_cena(modelo):
    cena = bpy.context.scene
    cena.render.engine = "BLENDER_WORKBENCH"
    sh = cena.display.shading
    sh.light = "STUDIO"
    sh.color_type = "TEXTURE"
    sh.show_shadows = True
    sh.shadow_intensity = 0.35
    sh.show_cavity = True
    sh.cavity_type = "WORLD"
    sh.background_type = "VIEWPORT"
    sh.background_color = (0.80, 0.84, 0.88)
    cena.display.light_direction = (0.4, -0.5, 0.75)
    cena.render.resolution_x = 480
    cena.render.resolution_y = 360
    cena.render.film_transparent = False
    cena.render.image_settings.file_format = "PNG"
    if modelo == "Wolf_Common":
        bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, 0))
        chao = bpy.context.active_object
        chao.name = "Chao_QA"
        mat = bpy.data.materials.new("Chao_QA")
        mat.diffuse_color = (0.55, 0.6, 0.5, 1)
        chao.data.materials.append(mat)
        # marcas no chao a cada 0,1 u para enxergar deslizamento
        for i in range(-30, 31):
            bpy.ops.mesh.primitive_cube_add(size=1, location=(0.35, i * 0.1, 0.0005))
            m = bpy.context.active_object
            m.scale = (0.25, 0.004, 0.001)
            m.data.materials.append(mat if i % 5 else bpy.data.materials.new("Marca"))
    cam_data = bpy.data.cameras.new("Cam_QA")
    cam_data.type = "ORTHO"
    cam = bpy.data.objects.new("Cam_QA", cam_data)
    cena.collection.objects.link(cam)
    cena.camera = cam
    return cam


def posicionar(cam, az, el, escala, alvo):
    alvo = Vector(alvo)
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = alvo + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = escala
    cam.data.clip_end = 50


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    origem, saida = Path(args[0]), Path(args[1])
    modelos = args[2:] or list(VISTAS)
    for modelo in modelos:
        bpy.ops.wm.open_mainfile(filepath=str(origem / (modelo + ".blend")))
        cena = bpy.context.scene
        rig = next(o for o in cena.objects if o.type == "ARMATURE")
        cam = preparar_cena(modelo)
        acoes = [t.strips[0].action for t in rig.animation_data.nla_tracks if t.name.startswith("PROD ")]
        for acao in acoes:
            rig.animation_data.action = acao
            ini, fim = int(acao.frame_range[0]), int(acao.frame_range[1])
            for vista, (az, el, esc, alvo) in VISTAS[modelo].items():
                posicionar(cam, az, el, esc, alvo)
                pasta = saida / modelo / acao.name
                pasta.mkdir(parents=True, exist_ok=True)
                for f in range(ini, fim + 1):
                    cena.frame_set(f)
                    cena.render.filepath = str(pasta / f"{vista}_{f:03d}.png")
                    bpy.ops.render.render(write_still=True)
            print("RENDER", modelo, acao.name, ini, fim, flush=True)


if __name__ == "__main__":
    main()
