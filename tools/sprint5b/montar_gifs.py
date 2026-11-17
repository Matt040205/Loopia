"""Sprint 5B: GIFs e pranchas dos quadros renderizados na Unity (capturas.py clipes).

Uso: python -I tools/sprint5b/montar_gifs.py <pasta_clipes> <saida> [fps]
Entrada: <pasta_clipes>/<Familia>/<Clipe>/qNNN.png. Laco (Idle, Fly, Locomocao): o ultimo quadro repete o primeiro
e sai do GIF. Saida: <Familia>_<Clipe>.gif e <Familia>_<Clipe>_prancha.png (ate 12 quadros).
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

LACOS = ("Idle", "Fly", "Locomocao_walk", "Locomocao_run")
DURACAO = {  # segundos a 1x, para o GIF tocar em tempo real
    ("Bat_Common", "Idle"): 32 / 24, ("Bat_Common", "Fly"): 1.0, ("Bat_Common", "Attack"): 0.75,
    ("Bat_Common", "Hit"): 8 / 24, ("Bat_Common", "Death"): 1.0,
    ("Bat_AncientBoss", "Idle"): 2.0, ("Bat_AncientBoss", "Fly"): 1.5, ("Bat_AncientBoss", "Attack"): 1.0,
    ("Bat_AncientBoss", "Hit"): 10 / 24, ("Bat_AncientBoss", "Death"): 1.75, ("Bat_AncientBoss", "Revoada"): 3.5,
    ("Wolf_Common", "Idle"): 3.0, ("Wolf_Common", "Locomocao_walk"): 15 / 24, ("Wolf_Common", "Locomocao_run"): 9 / 24,
    ("Wolf_Common", "Attack"): 15 / 24, ("Wolf_Common", "Hit"): 8 / 24, ("Wolf_Common", "Death"): 1.25,
}


def main():
    origem, saida = Path(sys.argv[1]), Path(sys.argv[2])
    saida.mkdir(parents=True, exist_ok=True)
    for pasta in sorted(p for p in origem.glob("*/*") if p.is_dir()):
        familia, clipe = pasta.parent.name, pasta.name
        quadros = sorted(pasta.glob("q*.png"))
        if not quadros:
            continue
        if clipe in LACOS:
            quadros = quadros[:-1]
        imgs = [Image.open(q).convert("RGB") for q in quadros]
        voltas = 3 if clipe in ("Locomocao_walk", "Locomocao_run", "Hit") else 1
        dur = DURACAO.get((familia, clipe), 1.0)
        ms = max(20, int(round(dur * 1000 / len(imgs))))
        seq = imgs * voltas
        pal = [i.resize((480, 360)).convert("P", palette=Image.ADAPTIVE, colors=160) for i in seq]
        pal[0].save(saida / f"{familia}_{clipe}.gif", save_all=True, append_images=pal[1:], duration=ms, loop=0)
        n = min(12, len(imgs))
        idx = [round(i * (len(imgs) - 1) / max(1, n - 1)) for i in range(n)]
        w, h, cols = 240, 180, 6
        prancha = Image.new("RGB", (cols * w, ((n + cols - 1) // cols) * h + 22), (30, 30, 34))
        d = ImageDraw.Draw(prancha)
        d.text((6, 4), f"Unity / {familia} / {clipe} ({len(imgs)} amostras, {dur:.3f} s)", fill=(235, 235, 235))
        for j, i in enumerate(idx):
            x, y = (j % cols) * w, 22 + (j // cols) * h
            prancha.paste(imgs[i].resize((w, h)), (x, y))
            d.text((x + 4, y + 2), f"t={i / max(1, len(quadros)):.2f}", fill=(15, 15, 15))
        prancha.save(saida / f"{familia}_{clipe}_prancha.png")
        print("OK", familia, clipe, len(imgs), ms)


if __name__ == "__main__":
    main()
