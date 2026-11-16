"""Compoe as camadas de arte da HUD a partir das ortografias do Loopia. Requer Pillow."""
from argparse import ArgumentParser
from math import exp, sin
from pathlib import Path
from random import Random

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter

ROOT = Path(__file__).resolve().parents[2] / "Assets/Arte/Cartas/Arte"
W, H = 640, 700
PALETAS = {
    "Bosque": ((40, 64, 65), (179, 197, 143), (44, 91, 89)),
    "Altura": ((44, 54, 92), (201, 187, 160), (89, 97, 143)),
    "Brasa": ((75, 51, 70), (219, 181, 135), (139, 86, 99)),
    "Ouro": ((57, 61, 81), (221, 192, 141), (123, 108, 128)),
    "Agua": ((32, 60, 75), (165, 208, 192), (60, 116, 127)),
}
CARTAS = {
    "Torre": ("Altura", 475, 502),
    "Basica": ("Bosque", 475, 425),
    "Floresta": ("Bosque", 484, 508),
    "IlhaAlta": ("Altura", 483, 520),
    "Mola": ("Brasa", 407, 469),
    "Flechas": ("Ouro", 439, 485),
    "Coracao": ("Brasa", 433, 427),
    "Moedas": ("Ouro", 433, 430),
    "AreaDeTreino": ("Ouro", 481, 474),
    "Cabana": ("Brasa", 484, 483),
    "CampoPacifico": ("Bosque", 480, 468),
    "Santuario": ("Altura", 454, 481),
    "FonteRestauradora": ("Agua", 464, 472),
    "OficinaDeArqueiro": ("Ouro", 482, 480),
}


def recorte():
    """Fecha o miolo pela silhueta externa da moldura, sem pintar seus cantos vazios."""
    border = Image.open(ROOT / "T_Card_Border_Blank.png").convert("RGBA").getchannel("A")
    exterior = border.point(lambda a: 0 if a > 20 else 255)
    ImageDraw.floodfill(exterior, (0, 0), 128)
    for x, y in ((border.width - 1, 0), (0, border.height - 1),
                 (border.width - 1, border.height - 1)):
        ImageDraw.floodfill(exterior, (x, y), 128)
    silhouette = exterior.point(lambda a: 0 if a == 128 else 255)
    # A máscara termina sob o dourado, deixando o antialias original desenhar a borda externa.
    silhouette = silhouette.resize((256, 356), Image.Resampling.LANCZOS).filter(ImageFilter.MinFilter(5))
    mask = Image.new("RGBA", silhouette.size, "white")
    mask.putalpha(silhouette)
    mask.save(ROOT / "Silhueta.png", optimize=True)


def paisagem(nome, seed):
    escuro, luz, pedra = PALETAS[nome]
    rnd = Random(seed)
    base = Image.new("RGB", (W, H))
    def pigmento(x, y):
        halo = exp(-((x-W*.46)**2/(W*.33)**2 + (y-H*.43)**2/(H*.46)**2))
        fita = .72 + sin(y*.012 + x*.006)*.11
        mancha = max(0, min(1, .14 + halo * fita + (1-y/H)*.07))
        grao = rnd.gauss(0, 1.08)
        transicao = max(0, min(1, (y-23)/98, (H-22-y)/92))
        transicao = transicao*transicao*(3-2*transicao)
        return tuple(round(fundo+(a+(b-a)*mancha+grao-fundo)*transicao)
                     for a,b,fundo in zip(escuro, luz, (33,28,49)))
    base.putdata([pigmento(x, y) for y in range(H) for x in range(W)])
    base.save(ROOT / f"Ceu_{nome}.png", optimize=True)
    plano = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(plano)
    for level, margin in enumerate((340, 433, 513)):
        pontos = [(-20, margin+78)]
        for x in range(0, W+42, 26):
            y = margin + sin((x+level*83)*.013)*47 + sin(x*.044)*12 + rnd.randrange(-16,17)
            y += 94 * exp(-((x-W*.52)/134)**2)
            pontos.append((x,y))
        d.polygon(pontos+[(W+30,H),(-25,H)], fill=(*pedra, 42+level*16))
        d.line(pontos, fill=(*luz, 34+level*4), width=2)
    fitas = Image.new("RGBA", (W, H))
    d = ImageDraw.Draw(fitas)
    for x,y,s in [(48,164,70),(530,231,71),(86,433,101),(541,491,115),(45,525,71),(586,597,86),
                  (268,568,114),(340,649,125),(593,370,47),(43,320,41)]:
        for j in range(5):
            cx, cy = x+(j-2)*s*.44, y+sin((j+1)*.6)*s*.17
            d.ellipse((cx-s*.66,cy-s*.115,cx+s*.64,cy+s*.10),fill=(*luz,36+j*4))
    plano.alpha_composite(fitas.filter(ImageFilter.GaussianBlur(5)))
    for k in range(24):
        x,y = rnd.randrange(25,W-25), rnd.randrange(79,607)
        if 171 < x < 468 and 200 < y < 490:
            continue
        d = ImageDraw.Draw(plano)
        t = rnd.choice((1,1,1,2,3))
        d.ellipse((x-t,y-t,x+t,y+t), fill=(*luz,rnd.randrange(60,160)))
    bordas = Image.new("L", (1,H))
    bordas.putdata([int(255*max(0,min(1,(y-25)/42,(H-26-y)/78))) for y in range(H)])
    plano.putalpha(ImageChops.multiply(plano.getchannel("A"), bordas.resize((W,H))))
    plano.save(ROOT / f"Atmosfera_{nome}.png", optimize=True)


def polir(im):
    alpha = im.getchannel("A")
    rgb = ImageEnhance.Color(im.convert("RGB")).enhance(.86)
    # Levanta os meios-tons do render ortográfico e dá calor às faces iluminadas.
    canais = []
    for canal, ganho in zip(rgb.split(), (1.05,1.02,.99)):
        canais.append(canal.point(lambda x: min(255, round((x/255)**.77 * 252 * ganho))))
    rgb = Image.merge("RGB", canais)
    rgb.putalpha(alpha)
    return rgb


def ilustracao(nome, origem):
    _, w, h = CARTAS[nome]
    im = Image.open(origem / f"{nome}.png").convert("RGBA")
    im = polir(im.crop(im.getchannel("A").getbbox()))
    im.thumbnail((w,h), Image.Resampling.LANCZOS)
    x, y = (W-im.width)//2, 313-im.height//2
    arte = Image.new("RGBA", (W,H))
    mascara = Image.new("L", (W,H))
    mascara.paste(im.getchannel("A"), (x+2,y+8))
    sombra = Image.new("RGBA", (W,H), (27,28,43,0))
    sombra.putalpha(mascara.filter(ImageFilter.GaussianBlur(13)).point(lambda p:p*48//100))
    arte.alpha_composite(sombra)
    arte.alpha_composite(im, (x,y))
    arte.save(ROOT / f"{nome}_Arte.png", optimize=True)


def detalhes(nome):
    poses = {
        "Moedas": ([(259,222,10),(417,335,9)], (255,220,142)),
        "Santuario": ([(320,178,11),(252,230,8),(386,232,6)], (177,255,223)),
        "FonteRestauradora": ([(252,290,8),(406,284,6),(318,183,9),(343,353,7)], (188,250,246)),
    }
    if nome not in poses:
        return
    pontos, cor = poses[nome]
    for fase, letra in enumerate(("A","B")):
        teto = Image.new("RGBA", (W,H))
        d = ImageDraw.Draw(teto)
        for i,(x,y,r) in enumerate(pontos):
            x += fase*(7-i*3)
            y -= fase*(4+i*5)
            r *= (1.16 if (i+fase)%2 else .66)
            d.polygon(((x-r,y),(x-r*.14,y-r*.12),(x,y-r),(x+r*.16,y-r*.12),
                       (x+r,y),(x+r*.14,y+r*.14),(x,y+r),(x-r*.14,y+r*.14)),
                      fill=(*cor,174))
        brilho = teto.filter(ImageFilter.GaussianBlur(8))
        brilho.alpha_composite(teto)
        brilho.save(ROOT / f"{nome}_Pose{letra}.png", optimize=True)


def icones():
    for nome in ("Caminho", "Construcao", "Risco", "Recurso", "Vida", "Dano"):
        im = Image.new("RGBA", (96,96))
        d = ImageDraw.Draw(im)
        if nome == "Caminho":
            d.line(((17,51),(48,28),(78,51),(48,73),(17,51)), fill="white",width=7,joint="curve")
            d.line(((34,47),(48,55),(64,44)), fill="white",width=5)
        elif nome == "Construcao":
            d.polygon([(23,22),(34,22),(34,34),(42,34),(42,22),(54,22),(54,34),(62,34),
                       (62,22),(73,22),(73,42),(67,49),(67,74),(29,74),(29,49),(23,42)],fill="white")
            d.rounded_rectangle((43,53,54,76),5,fill=(0,0,0,0))
        elif nome == "Risco":
            for x,y in ((30,40),(42,27),(55,27),(67,40)):
                d.ellipse((x-6,y-8,x+6,y+8),fill="white")
            d.polygon(((31,63),(38,50),(48,44),(58,48),(65,61),(61,70),(36,71)),fill="white")
        elif nome == "Recurso":
            d.ellipse((22,17,74,79), outline="white",width=7)
            d.ellipse((34,30,63,66), outline="white",width=4)
            d.polygon(((48,35),(54,48),(48,60),(42,48)),fill="white")
        elif nome == "Vida":
            d.ellipse((20,26,52,58),fill="white")
            d.ellipse((44,26,76,58),fill="white")
            d.polygon(((22,49),(74,49),(69,61),(48,78),(27,61)),fill="white")
        else:
            d.line(((22,74),(72,24)), fill="white",width=8)
            d.polygon(((57,23),(76,20),(75,39)),fill="white")
            d.polygon(((17,65),(35,51),(34,64),(48,65),(30,79),(17,81)),fill="white")
        im.resize((48,48),Image.Resampling.LANCZOS).save(ROOT / f"Icone_{nome}.png",optimize=True)


def main():
    cli = ArgumentParser(description=__doc__)
    cli.add_argument("ortografias", type=Path)
    cli.add_argument("cartas", choices=tuple(CARTAS)+("Tudo",), nargs="+")
    args = cli.parse_args()
    recorte()
    icones()
    cartas = tuple(CARTAS) if args.cartas == ["Tudo"] else args.cartas
    for n, nome in enumerate(PALETAS):
        if any(CARTAS[carta][0] == nome for carta in cartas):
            paisagem(nome, 2026+n*119)
    for nome in cartas:
        ilustracao(nome, args.ortografias)
        detalhes(nome)


if __name__ == "__main__":
    main()
