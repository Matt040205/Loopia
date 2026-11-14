"""Metrica de legibilidade (Pass D1): luminancia relativa do topo das ilhas vs fundo imediato.

Entrada: imagens JPG da captura + arquivos de pontos gerados por `run_baseline.py pontos` (pontos_s<seed>_<w>x<h>.json).
Para cada imagem:
  L_topo  = media da luminancia relativa (sRGB -> linear, Rec.709) em janelas 5x5 nos pontos de topo das ilhas
  L_fundo = idem em celulas "fundo imediato": nao cobertas por renderers de ilha/heroi/inimigo e a ate RAIO_PX de uma coberta
  razao   = L_topo / L_fundo      michelson = (Lt - Lf) / (Lt + Lf)
  matiz_topo / matiz_fundo = matiz medio (graus, media circular ponderada por saturacao*valor)

Uso (da raiz do repo):
  python -I tools/visual-baseline/legibilidade.py <pasta_de_imagens> <pasta_de_pontos> <saida.json> [--cinza <pasta_saida_cinza>]
Nome de arquivo esperado: s<seed>_h<hora>_<w>x<h>.jpg (as extras de transicao tambem; horas fracionadas nao).
"""
import colorsys, glob, json, math, os, re, sys
import numpy as np
from PIL import Image

RAIO_PX = 60
JANELA = 2  # janela (2*J+1)^2


def srgb_para_linear(a):
    a = a / 255.0
    return np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)


def luminancia(img):
    lin = srgb_para_linear(img.astype(np.float64))
    return 0.2126 * lin[..., 0] + 0.7152 * lin[..., 1] + 0.0722 * lin[..., 2]


def janela(mapa, x, y):
    h, w = mapa.shape[:2]
    x, y = int(round(x)), int(round(y))
    return mapa[max(0, y - JANELA):min(h, y + JANELA + 1), max(0, x - JANELA):min(w, x + JANELA + 1)]


def matiz_medio(rgbs):
    """Media circular do matiz (graus), ponderada por S*V; retorna (matiz, saturacao media)."""
    sx = sy = peso = 0.0
    sats = []
    for r, g, b in rgbs:
        h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        w = s * v
        sx += math.cos(h * 2 * math.pi) * w
        sy += math.sin(h * 2 * math.pi) * w
        peso += w
        sats.append(s)
    if peso <= 1e-9:
        return None, float(np.mean(sats)) if sats else 0.0
    return (math.degrees(math.atan2(sy, sx)) % 360.0), float(np.mean(sats))


def faixa_de_fundo(pts):
    gw, gh, passo = pts["gw"], pts["gh"], pts["passo"]
    cob = np.array([[c == "1" for c in linha] for linha in pts["cobertura"].strip().split("\n")], dtype=bool)
    r = int(math.ceil(RAIO_PX / passo))
    yy, xx = np.ogrid[-r:r + 1, -r:r + 1]
    disco = (xx * xx + yy * yy) <= r * r
    dil = np.zeros_like(cob)
    ys, xs = np.nonzero(cob)
    for y, x in zip(ys, xs):
        y0, y1 = max(0, y - r), min(gh, y + r + 1)
        x0, x1 = max(0, x - r), min(gw, x + r + 1)
        dil[y0:y1, x0:x1] |= disco[(y0 - y + r):(y1 - y + r), (x0 - x + r):(x1 - x + r)]
    banda = dil & ~cob
    # margem de 1 celula da borda da imagem fora
    cel = [((x + 0.5) * passo, (y + 0.5) * passo) for y, x in zip(*np.nonzero(banda))]
    return cel, int(cob.sum())


def medir(caminho_img, pts):
    img = np.asarray(Image.open(caminho_img).convert("RGB"))
    lum = luminancia(img)
    topo_l, topo_rgb = [], []
    for x, y in pts["topos"]:
        topo_l.append(float(janela(lum, x, y).mean()))
        topo_rgb.append(tuple(janela(img, x, y).reshape(-1, 3).mean(axis=0)))
    cel, ncob = faixa_de_fundo(pts)
    fundo_l, fundo_rgb = [], []
    for x, y in cel:
        fundo_l.append(float(janela(lum, x, y).mean()))
        fundo_rgb.append(tuple(janela(img, x, y).reshape(-1, 3).mean(axis=0)))
    lt, lf = float(np.mean(topo_l)), float(np.mean(fundo_l))
    mt, st = matiz_medio(topo_rgb)
    mf, sf = matiz_medio(fundo_rgb)
    return {
        "L_topo": lt, "L_fundo": lf, "razao": lt / lf if lf > 0 else None,
        "michelson": (lt - lf) / (lt + lf) if lt + lf > 0 else None,
        "pontos_topo": len(topo_l), "celulas_fundo": len(fundo_l),
        "matiz_topo_graus": mt, "saturacao_topo": st, "matiz_fundo_graus": mf, "saturacao_fundo": sf,
    }


def para_cinza(caminho_img, saida):
    img = Image.open(caminho_img).convert("L")
    img.save(saida, quality=90)


def main():
    pasta_img, pasta_pts, saida = sys.argv[1], sys.argv[2], sys.argv[3]
    cinza = sys.argv[sys.argv.index("--cinza") + 1] if "--cinza" in sys.argv else None
    resultados = {}
    for caminho in sorted(glob.glob(os.path.join(pasta_img, "s*_h*_*x*.jpg"))):
        nome = os.path.basename(caminho)
        m = re.match(r"s(\d+)_h(\d+)_(\d+)x(\d+)\.jpg$", nome)
        if not m:
            continue
        seed, hora, w, h = m.groups()
        pf = os.path.join(pasta_pts, "pontos_s%s_%sx%s.json" % (seed, w, h))
        if not os.path.exists(pf):
            print("sem pontos para", nome)
            continue
        with open(pf, encoding="utf-8") as f:
            pts = json.load(f)
        r = medir(caminho, pts)
        r.update({"seed": int(seed), "hora": int(hora), "resolucao": "%sx%s" % (w, h)})
        resultados[nome] = r
        print("%-28s Lt=%.4f Lf=%.4f razao=%.2f michelson=%.3f" % (nome, r["L_topo"], r["L_fundo"], r["razao"], r["michelson"]))
        if cinza and int(w) == 1920 and seed == "12345":
            os.makedirs(cinza, exist_ok=True)
            para_cinza(caminho, os.path.join(cinza, nome.replace(".jpg", "_cinza.jpg")))
    with open(saida, "w", encoding="utf-8") as f:
        json.dump({"metodo": __doc__.strip().split("\n")[0], "raio_fundo_px": RAIO_PX, "janela_px": 2 * JANELA + 1, "capturas": resultados}, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
