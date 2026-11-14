"""Helper para driblar o Unity via tools/unity-mcp/umcp.py (importado, nao shell)."""
import json, os, sys, time
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tools", "unity-mcp"))
import umcp  # noqa: E402

def tool(name, params, retries=20):
    for i in range(retries):
        try:
            return json.loads(umcp.call(name, params))
        except (ConnectionRefusedError, ConnectionResetError, OSError, RuntimeError):
            time.sleep(2)
    raise RuntimeError("bridge indisponivel")

def run_code(code, **repl):
    for k, v in repl.items():
        code = code.replace("%%" + k + "%%", str(v))
    r = tool("execute_code", {"action": "execute", "code": code})
    if r.get("status") != "success":
        raise RuntimeError(json.dumps(r)[:2000])
    d = r.get("result", {})
    data = d.get("data", d) if isinstance(d, dict) else d
    res = data.get("result") if isinstance(data, dict) else data
    return res

def run_file(name, **repl):
    with open(os.path.join(HERE, "cs", name), encoding="utf-8") as f:
        return run_code(f.read(), **repl)

def play():
    tool("manage_editor", {"action": "play"})
def stop():
    tool("manage_editor", {"action": "stop"})
def is_playing():
    return run_code("return UnityEngine.Application.isPlaying;")
