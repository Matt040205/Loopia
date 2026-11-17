"""Sprint 5B - pose neutra (bind pose, sem acao) dos tres modelos, nas mesmas vistas de renderizar.py.

Uso: blender -b --factory-startup --python-exit-code 7 --python renderizar_neutra.py -- <pasta_blend> <pasta_saida>
"""
import sys
from pathlib import Path

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from renderizar import VISTAS, posicionar, preparar_cena  # noqa: E402


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    origem, saida = Path(args[0]), Path(args[1])
    saida.mkdir(parents=True, exist_ok=True)
    for modelo in VISTAS:
        bpy.ops.wm.open_mainfile(filepath=str(origem / (modelo + ".blend")))
        cena = bpy.context.scene
        rig = next(o for o in cena.objects if o.type == "ARMATURE")
        rig.animation_data.action = None
        for pb in rig.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = (1, 0, 0, 0)
            pb.location = (0, 0, 0)
        cam = preparar_cena(modelo)
        cena.render.resolution_x, cena.render.resolution_y = 960, 720
        for vista, (az, el, esc, alvo) in VISTAS[modelo].items():
            posicionar(cam, az, el, esc, alvo)
            cena.render.filepath = str(saida / f"{modelo}_Neutra_{vista}.png")
            bpy.ops.render.render(write_still=True)
        print("NEUTRA", modelo, flush=True)


if __name__ == "__main__":
    main()
