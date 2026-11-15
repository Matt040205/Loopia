"""Pass C: coloca as cartas com marcador e captura escala de jogo + close de cada uma (Play Mode ja ativo).
Uso: python -I tools/visual-baseline/cartas.py <pasta_saida> [seed=12345] [horas=12,18,22]
Fazer backup do save antes (ver README)."""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "unity-mcp"))
import run_baseline as rb
import ub

out = os.path.abspath(sys.argv[1]); os.makedirs(out, exist_ok=True)
seed = int(sys.argv[2]) if len(sys.argv) > 2 else 12345
horas = (sys.argv[3] if len(sys.argv) > 3 else "12,18,22").split(",")
print("seed", seed, rb.setup(seed))
rb.prepare(horas[0], 1920, 1080)
cartas = rb.jl(ub.run_file("cartas.cs"))
json.dump(cartas, open(os.path.join(out, "cartas_s%d.json" % seed), "w", encoding="utf-8"), indent=1)
for c in cartas:
    print(c)
for h in horas:
    rb.prepare(h, 1920, 1080)
    meta = rb.jl(ub.run_file("shoot.cs", W=1920, H=1080, SEED=seed, HOUR=h, OUT=rb.fwd(os.path.join(out, "jogo_s%d_h%s.jpg" % (seed, h)))))
    for c in cartas:
        if not c["ok"]:
            continue
        nome = "close_%s_s%d_h%s.jpg" % (c["carta"], seed, h)
        ub.run_file("close.cs", X=c["x"], Y=c["y"] + 0.8, Z=c["z"], SIZE=3.2, W=960, H=720, OUT=rb.fwd(os.path.join(out, nome)))
    print("hora", h, "ok")
