"""Sprint 5B: capturas e medidas dos inimigos animados na HexPrototipo (Editor em Play Mode, via unity-mcp).

Uso (da raiz do repo, Editor aberto na HexPrototipo, save com backup ou Editor com -loopia-perfil):
  python -I tools/sprint5b/capturas.py play
  python -I tools/sprint5b/capturas.py escala   <pasta>   # 0,60 / 0,85 / 1,034 x Lucca, dia e noite, cheio e close
  python -I tools/sprint5b/capturas.py familias <pasta>   # morcego, Anciao e lobo novos x antigos, dia e noite
  python -I tools/sprint5b/capturas.py clipes   <pasta>   # quadros de cada clipe na Unity (para GIF/pranchas)
  python -I tools/sprint5b/capturas.py cs <arquivo.cs> CHAVE=valor ...   # roda um snippet de tools/sprint5b/cs
  python -I tools/sprint5b/capturas.py stop

Um render por chamada (foto.cs) com pausa entre chamadas: varios Camera.Render() num execute_code derrubam o D3D12.
"""
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "visual-baseline"))
sys.dont_write_bytecode = True
import ub  # noqa: E402

SEMENTE = 12345


def cs(nome, **kw):
    with open(os.path.join(HERE, "cs", nome), encoding="utf-8") as f:
        return ub.run_code(f.read(), **kw)


def base(nome, **kw):
    return ub.run_file(nome, **kw)


def jl(s):
    return json.loads(s) if isinstance(s, str) else s


def fwd(p):
    return os.path.abspath(p).replace("\\", "/")


def foto(saida, modo="cheio", centro=(0, 0, 0), size=4.0, w=1920, h=1080):
    r = cs("foto.cs", MODO=modo, X=centro[0], Y=centro[1], Z=centro[2], SIZE=size, W=w, H=h, OUT=fwd(saida))
    time.sleep(0.25)
    return r


def hora(h):
    cs("hora.cs", HOUR=h)
    time.sleep(0.6)


def preparar():
    st = jl(base("setup_seed.cs", SEED=SEMENTE))
    time.sleep(2.0)
    return st


def montar(itens, passo=1.6, deslocamento=0.0):
    r = jl(cs("montar.cs", ITENS=";".join(itens), PASSO=passo, DESLOCAMENTO=deslocamento))
    time.sleep(0.4)
    return r


def pose(estado, t, vel=0.0, familia=""):
    return cs("pose.cs", ESTADO=estado, T=t, VEL=vel, FAMILIA=familia)


def escala(pasta):
    os.makedirs(pasta, exist_ok=True)
    preparar()
    medidas = {}
    grupos = {
        "escalas": ["esfera:0.6", "morcego:0.6", "morcego:0.85", "morcego:1.034"],
    }
    for nome, itens in grupos.items():
        m = montar(itens, passo=1.7)
        pose("Idle", 0.12)
        c = m["centro"]
        for h, rot in (("12", "dia"), ("22", "noite")):
            hora(h)
            foto(os.path.join(pasta, f"{nome}_{rot}_cheio_1920x1080.png"), "cheio")
            foto(os.path.join(pasta, f"{nome}_{rot}_close_1920x1080.png"), "close", c, 3.2)
        medidas[nome] = {"montagem": m, "projecao": jl(cs("projecao.cs", W=1920, H=1080))}
    with open(os.path.join(pasta, "escala.json"), "w", encoding="utf-8") as f:
        json.dump(medidas, f, indent=1, ensure_ascii=False)
    print(json.dumps(medidas["escalas"]["projecao"], ensure_ascii=False))


def familias(pasta, bat="0.85", chefe="3.0"):
    """Novo x antigo ao lado da Lucca: morcego, Anciao e lobo, dia e noite, close e quadro oficial."""
    os.makedirs(pasta, exist_ok=True)
    preparar()
    grupos = {
        "morcego_novo_x_esfera": (["esfera:0.6", f"morcego:{bat}"], 1.6, 0.0, 2.6),
        "lobo_novo_x_antigo": (["loboantigo:0", "lobo:0"], 1.9, 0.0, 2.8),
        "chefe_novo_x_esfera": ([f"esferachefe:{chefe}", f"chefe:{chefe}"], 3.6, 0.0, 4.8),
        "lucca_e_os_tres": ([f"morcego:{bat}", "lobo:0", f"chefe:{chefe}"], 2.2, 0.0, 4.6),
    }
    medidas = {}
    for nome, (itens, passo, desl, size) in grupos.items():
        m = montar(itens, passo, desl)
        pose("Idle", 0.12)
        for h, rot in (("12", "dia"), ("22", "noite")):
            hora(h)
            foto(os.path.join(pasta, f"{nome}_{rot}_close_1920x1080.png"), "close", m["centro"], size)
        medidas[nome] = {"montagem": m, "projecao": jl(cs("projecao.cs", W=1920, H=1080))}
    with open(os.path.join(pasta, "familias.json"), "w", encoding="utf-8") as f:
        json.dump(medidas, f, indent=1, ensure_ascii=False)


CLIPES = {
    "Bat_Common": [("Idle", 32), ("Fly", 24), ("Attack", 18), ("Hit", 8), ("Death", 24)],
    "Bat_AncientBoss": [("Idle", 24), ("Fly", 18), ("Attack", 24), ("Hit", 10), ("Death", 21), ("Revoada", 42)],
    "Wolf_Common": [("Idle", 24), ("Locomocao:walk", 15), ("Locomocao:run", 9), ("Attack", 15), ("Hit", 8), ("Death", 30)],
}


def clipes(pasta, bat="0.85", chefe="3.0"):
    """Quadros de cada clipe dentro da Unity (geometria e material reais), close em 720x540."""
    os.makedirs(pasta, exist_ok=True)
    preparar()
    hora("12")
    for familia, itens, size in (("Bat_Common", [f"morcego:{bat}"], 1.0), ("Wolf_Common", ["lobo:0"], 1.2),
                                 ("Bat_AncientBoss", [f"chefe:{chefe}"], 2.4)):
        m = montar(itens, 0.01, 0.0)
        c = m["itens"][0]
        centro = (c["x"], c["y"] + (0.45 if familia == "Wolf_Common" else 0.0), c["z"])
        for clipe, quadros in CLIPES[familia]:
            estado, _, var = clipe.partition(":")
            vel = {"walk": 0.989, "run": 4.571}.get(var, 0.0)
            sub = os.path.join(pasta, familia, clipe.replace(":", "_"))
            os.makedirs(sub, exist_ok=True)
            for i in range(quadros + 1):
                t = i / quadros
                pose(estado, min(t, 0.999) if estado not in ("Attack", "Revoada") else t, vel)
                foto(os.path.join(sub, f"q{i:03d}.png"), "close", centro, size, 720, 540)
            print("clipe", familia, clipe, quadros, flush=True)


def desempenho(pasta, morcegos=6, lobos=2, repeticoes=3):
    """Antes (esfera/Lobo.prefab antigo) x depois (modelos animados) com a carga maxima, no mesmo Editor e mapa.
    Cada medida: monta, deixa assentar 3 s, grava ~240 quadros de ProfilerRecorder e le UnityStats/FrameTiming."""
    os.makedirs(pasta, exist_ok=True)
    preparar()
    hora("12")
    ub.run_code("UnityEngine.Time.timeScale = 1f; return 1;")
    res = []
    for rep in range(repeticoes):
        for visual in ("antigo", "novo"):
            m = jl(cs("desempenho_montar.cs", VISUAL=visual, MORCEGOS=morcegos, LOBOS=lobos))
            time.sleep(3)
            cs("desempenho_medir.cs", ACAO="iniciar")
            time.sleep(5)
            r = jl(cs("desempenho_medir.cs", ACAO="ler"))
            r["montagem"] = m
            r["repeticao"] = rep
            res.append(r)
            print(visual, rep, json.dumps({k: r.get(k) for k in ("Main Thread", "Animators.Update", "MeshSkinning.Update",
                                                                   "GC Allocated In Frame", "UnityStats")}, ensure_ascii=False)[:900], flush=True)
    with open(os.path.join(pasta, "desempenho_editor.json"), "w", encoding="utf-8") as f:
        json.dump(res, f, indent=1, ensure_ascii=False)


def main():
    cmd = sys.argv[1]
    if cmd == "play":
        ub.play()
        for _ in range(40):
            time.sleep(1)
            try:
                if ub.is_playing():
                    break
            except Exception:
                pass
        time.sleep(3)
        print("playing", ub.is_playing())
    elif cmd == "stop":
        ub.run_code("UnityEngine.Time.timeScale = 1f; return 1;")
        ub.stop()
    elif cmd == "escala":
        escala(sys.argv[2])
    elif cmd == "familias":
        familias(sys.argv[2])
    elif cmd == "clipes":
        clipes(sys.argv[2])
    elif cmd == "desempenho":
        desempenho(sys.argv[2])
    elif cmd == "cs":
        kw = dict(a.split("=", 1) for a in sys.argv[3:])
        print(cs(sys.argv[2], **kw))
    else:
        raise SystemExit("comando desconhecido: " + cmd)


if __name__ == "__main__":
    main()
