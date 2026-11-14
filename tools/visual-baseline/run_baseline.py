"""Baseline visual do HexPrototipo: capturas + medidas, dirigindo o Editor via tools/unity-mcp/umcp.py.

Pre-requisito: Unity Editor aberto em HexPrototipo (sem alteracoes pendentes) e em Play Mode
(use `python -I tools/visual-baseline/run_baseline.py play`). O runner nunca salva cena/asset.

Uso (da raiz do repo):
  python -I tools/visual-baseline/run_baseline.py play
  python -I tools/visual-baseline/run_baseline.py capture   # 18 JPGs + docs/art/baseline/manifest.json
  python -I tools/visual-baseline/run_baseline.py measure   # docs/art/audit/_raw_*.json
  python -I tools/visual-baseline/run_baseline.py enemies   # lobo+morcego, normal e close-up (padrao docs/art/pass-a/antes)
  python -I tools/visual-baseline/run_baseline.py stats     # estatisticas de render do Editor
  python -I tools/visual-baseline/run_baseline.py stop      # restaura timeScale=1 e sai do Play

Limite conhecido: Camera.Render() so inclui o que a Main Camera desenha. Canvas Screen Space - Overlay
(HUD) e IMGUI nao aparecem; para isso use screenshot da Game view (ver MEDIDAS.md).
"""
import json, os, sys, time, datetime

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import ub  # noqa: E402

ROOT = ub.ROOT
# Pasta de saida das capturas: padrao docs/art/baseline; para comparacoes antes/depois use
# LOOPIA_CAPTURE_DIR=docs/art/pass-a/depois (relativo a raiz do repo) para nao sobrescrever o baseline.
BASE_DIR = os.path.join(ROOT, os.environ.get("LOOPIA_CAPTURE_DIR", os.path.join("docs", "art", "baseline")))
# Saida das medidas brutas: padrao docs/art/audit; LOOPIA_AUDIT_DIR=docs/art/pass-a (relativo a raiz) para nao
# sobrescrever a auditoria original.
AUDIT_DIR = os.path.join(ROOT, os.environ.get("LOOPIA_AUDIT_DIR", os.path.join("docs", "art", "audit")))
SEEDS = [12345, 7, 2024]
HOURS = [("12", "noon"), ("18", "dusk"), ("22", "night")]
RESOLUTIONS = [(1920, 1080), (2520, 1080)]
SETTLE_SEED = 2.5
SETTLE_FRAMES = 1.2


def jl(s):
    return json.loads(s) if isinstance(s, str) else s


def fwd(p):
    return p.replace("\\", "/")


def setup(seed):
    r = jl(ub.run_file("setup_seed.cs", SEED=seed))
    time.sleep(SETTLE_SEED)
    return r


def prepare(hour, w, h):
    ub.run_file("prepare.cs", HOUR=hour, W=w, H=h)
    time.sleep(SETTLE_FRAMES)


def capture():
    os.makedirs(BASE_DIR, exist_ok=True)
    shots, light = [], []
    for seed in SEEDS:
        st = setup(seed)
        print("seed", seed, st)
        for hour, label in HOURS:
            for w, h in RESOLUTIONS:
                prepare(hour, w, h)
                if (w, h) == RESOLUTIONS[0]:
                    lz = jl(ub.run_file("measure_light.cs"))
                    lz["seed"] = seed; lz["hora_pedida"] = float(hour); lz["rotulo"] = label
                    light.append(lz)
                name = "s%d_h%s_%dx%d.jpg" % (seed, hour, w, h)
                out = fwd(os.path.join(BASE_DIR, name))
                meta = jl(ub.run_file("shoot.cs", W=w, H=h, SEED=seed, HOUR=hour, OUT=out))
                meta["arquivo"] = name; meta["rotulo_hora"] = label
                meta["setup"] = st
                shots.append(meta)
                print(name, "ortho=%.3f lum=%.1f inim=%d" % (meta["orthographicSize"], meta["luminanciaMedia0a255"], meta["inimigosVivos"]))
    manifest = {
        "gerado_em": datetime.datetime.now().isoformat(timespec="seconds"),
        "cena": "Assets/Scenes/HexPrototipo.unity", "unity": "6000.3.10f1",
        "ambiente": "Editor Play Mode (nao e build)", "metodo": "Camera.Render() da Main Camera em RenderTexture ARGB32 sRGB + ReadPixels + EncodeToJPG(85)",
        "limitacao": "UI Screen Space Overlay e IMGUI nao entram (nao fazem parte do render de camera).",
        "timeScale": 0.0, "capturas": shots,
    }
    with open(os.path.join(BASE_DIR, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1, ensure_ascii=False)
    with open(os.path.join(AUDIT_DIR, "_raw_lighting.json"), "w", encoding="utf-8") as f:
        json.dump(light, f, indent=1, ensure_ascii=False)


def measure():
    os.makedirs(AUDIT_DIR, exist_ok=True)
    for seed in SEEDS:
        setup(seed)
        prepare("12", 1920, 1080)
        raw = {"seed": seed}
        raw["cena"] = jl(ub.run_file("measure_seed.cs", SEED=seed))
        raw["camera"] = jl(ub.run_file("measure_camera.cs", SEED=seed))
        raw["fundo_1920x1080"] = jl(ub.run_file("measure_bg.cs", SEED=seed, W=1920, H=1080))
        raw["fundo_2520x1080"] = jl(ub.run_file("measure_bg.cs", SEED=seed, W=2520, H=1080))
        with open(os.path.join(AUDIT_DIR, "_raw_seed%d.json" % seed), "w", encoding="utf-8") as f:
            json.dump(raw, f, indent=1, ensure_ascii=False)
        print("medido", seed)


def enemies():
    """Lobo + Morcego nascidos via InimigoSpawner.Nascer: enquadramento normal e close-up (ortho 6).
    Saida: LOOPIA_CAPTURE_DIR (padrao deste comando: docs/art/pass-a/antes) + enemies.json."""
    out_dir = os.path.join(ROOT, os.environ.get("LOOPIA_CAPTURE_DIR", os.path.join("docs", "art", "pass-a", "antes")))
    os.makedirs(out_dir, exist_ok=True)
    seed, hour, w, h = 12345, "12", 1920, 1080
    setup(seed)
    prepare(hour, w, h)
    n = "enemies_s%d_h%s_%dx%d.jpg" % (seed, hour, w, h)
    c = "enemies_closeup_s%d_h%s_%dx%d.jpg" % (seed, hour, w, h)
    meta = jl(ub.run_file("enemies.cs", W=w, H=h, CLOSE_ORTHO=6,
                          OUT_NORMAL=fwd(os.path.join(out_dir, n)), OUT_CLOSE=fwd(os.path.join(out_dir, c))))
    meta["seed"] = seed; meta["hora"] = float(hour); meta["arquivos"] = [n, c]
    meta["gerado_em"] = datetime.datetime.now().isoformat(timespec="seconds")
    with open(os.path.join(out_dir, "enemies.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=1, ensure_ascii=False)
    print(json.dumps(meta["enquadramentoNormal"]))
    print(json.dumps(meta["closeUp"]))


def stats():
    """Estatisticas do Editor com a camera desenhando em 1920x1080 (RenderTexture persistente)."""
    setup(12345)
    prepare("12", 1920, 1080)
    ub.run_code("""
var cam = UnityEngine.Camera.main;
var rt = new UnityEngine.RenderTexture(1920, 1080, 24); rt.name = "__baseline_rt"; rt.Create();
cam.aspect = 1920f/1080f; cam.targetTexture = rt;
UnityEngine.Application.targetFrameRate = -1;
return "ok";
""")
    time.sleep(3)
    first = jl(ub.run_file("stats_sample.cs"))
    samples = []
    t0 = time.time()
    for _ in range(12):
        time.sleep(0.5)
        samples.append(jl(ub.run_file("stats_sample.cs")))
    last = samples[-1]
    df = last["frameCount"] - first["frameCount"]
    dt = last["realtime"] - first["realtime"]
    out = {
        "AVISO": "VALORES DO EDITOR (Play Mode no Editor, GPU/CPU do PC do usuario), NAO de build.",
        "resolucao": "1920x1080 (RenderTexture; Main Camera com targetTexture, Game view nao desenha a camera)",
        "seed": 12345, "hora": 12, "timeScale": 0,
        "frames_no_intervalo": df, "segundos": dt, "frametime_medio_ms_do_editor": 1000.0 * dt / max(1, df),
        "fps_medio_do_editor": df / dt if dt > 0 else None,
        "amostras_UnityStats": samples,
    }
    def med(k):
        v = sorted(s[k] for s in samples)
        return v[len(v) // 2]
    out["mediana"] = {k: med(k) for k in ["batches", "setPassCalls", "drawCalls", "triangles", "vertices", "shadowCasters"]}
    ub.run_code("""
var cam = UnityEngine.Camera.main; var rt = cam.targetTexture; cam.targetTexture = null; cam.ResetAspect();
if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
return "ok";
""")
    with open(os.path.join(AUDIT_DIR, "_raw_stats.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, indent=1, ensure_ascii=False)
    print(json.dumps(out["mediana"]), out["frametime_medio_ms_do_editor"])


def play():
    ub.play()
    for _ in range(30):
        time.sleep(1)
        try:
            if ub.is_playing():
                break
        except Exception:
            pass
    time.sleep(3)
    ub.run_code("UnityEngine.Application.runInBackground = true; return true;")
    print("playing, runInBackground=true")


def stop():
    try:
        ub.run_code("UnityEngine.Time.timeScale = 1f; return true;")
    except Exception as e:
        print("aviso:", e)
    ub.stop()
    print("stop enviado")


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    {"play": play, "capture": capture, "measure": measure, "enemies": enemies, "stats": stats, "stop": stop}[cmd]()
