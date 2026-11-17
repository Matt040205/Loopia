"""Sprint 5B - junta os PNGs de renderizar.py em GIFs e pranchas (Python comum com Pillow).

Uso: python -I montar_evidencias.py <pasta_render> <pasta_saida>
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

LACOS = {"Idle", "Fly", "Boss_Idle", "Boss_Fly", "Wolf_Idle", "Wolf_Walk", "Wolf_Run"}


def main():
    origem, saida = Path(sys.argv[1]), Path(sys.argv[2])
    saida.mkdir(parents=True, exist_ok=True)
    for pasta in sorted(p for p in origem.glob("*/*") if p.is_dir()):
        modelo, acao = pasta.parent.name, pasta.name
        vistas = sorted({f.name.split("_")[0] for f in pasta.glob("*.png")})
        for vista in vistas:
            quadros = sorted(pasta.glob(vista + "_*.png"))
            if acao in LACOS:
                quadros = quadros[:-1]  # o ultimo repete o primeiro
            imgs = [Image.open(q).convert("RGB") for q in quadros]
            nome = f"{modelo}_{acao}_{vista}"
            paleta = [i.convert("P", palette=Image.ADAPTIVE, colors=128) for i in imgs]
            paleta[0].save(saida / (nome + ".gif"), save_all=True, append_images=paleta[1:], duration=42, loop=0,
                           disposal=2)
            # prancha: ate 12 quadros igualmente espacados
            n = min(12, len(imgs))
            idx = [round(i * (len(imgs) - 1) / max(1, n - 1)) for i in range(n)]
            w, h = 240, 180
            cols = 6
            linhas = (n + cols - 1) // cols
            prancha = Image.new("RGB", (cols * w, linhas * h + 22), (30, 30, 34))
            d = ImageDraw.Draw(prancha)
            d.text((6, 4), f"{modelo} / {acao} / {vista}  ({len(imgs)} quadros a 24 fps)", fill=(235, 235, 235))
            for j, i in enumerate(idx):
                im = imgs[i].resize((w, h))
                x, y = (j % cols) * w, 22 + (j // cols) * h
                prancha.paste(im, (x, y))
                d.text((x + 4, y + 2), f"f{int(quadros[i].stem.split('_')[-1])}", fill=(20, 20, 20))
            prancha.save(saida / (nome + "_prancha.png"))
            print("OK", nome, len(imgs))


if __name__ == "__main__":
    main()
