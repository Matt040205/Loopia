"""Minimal client for the Unity MCP TCP bridge (127.0.0.1:6400).
Usage: python umcp.py <tool> '<json params>'   or   python umcp.py <tool> @file.json
"""
import glob, json, os, socket, struct, sys

PROJETO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _porta():
    """Porta do bridge: UNITY_MCP_PORT > status do projeto em ~/.unity-mcp > arquivo geral > 6400.
    O Unity escolhe a porta ao abrir (6400, 6401, ...) e a registra nesses arquivos."""
    if os.environ.get("UNITY_MCP_PORT"):
        return int(os.environ["UNITY_MCP_PORT"])
    pasta = os.path.expanduser("~/.unity-mcp")
    alvo = os.path.normcase(os.path.join(PROJETO, "Assets")).replace("\\", "/")
    candidatos = []
    for caminho in glob.glob(os.path.join(pasta, "unity-mcp-status-*.json")) + \
            glob.glob(os.path.join(pasta, "unity-mcp-port*.json")):
        try:
            dados = json.load(open(caminho, encoding="utf-8-sig"))
        except Exception:
            continue
        projeto = os.path.normcase(str(dados.get("project_path", ""))).replace("\\", "/")
        if dados.get("unity_port") and projeto == alvo:
            candidatos.append((os.path.getmtime(caminho), int(dados["unity_port"])))
    if candidatos:
        return max(candidatos)[1]
    return 6400


def call(tool, params, port=None, timeout=300):
    s = socket.create_connection(("127.0.0.1", port or _porta()), timeout=timeout)
    buf = b""
    while b"\n" not in buf:
        chunk = s.recv(256)
        if not chunk:
            raise RuntimeError("closed before handshake")
        buf += chunk
    payload = json.dumps({"type": tool, "params": params}).encode("utf-8")
    s.sendall(struct.pack(">Q", len(payload)) + payload)
    def recvn(n):
        d = b""
        while len(d) < n:
            c = s.recv(n - len(d))
            if not c:
                raise RuntimeError("closed mid-frame")
            d += c
        return d
    n = struct.unpack(">Q", recvn(8))[0]
    data = recvn(n)
    s.close()
    return data.decode("utf-8", "replace")

if __name__ == "__main__":
    tool = sys.argv[1]
    arg = sys.argv[2] if len(sys.argv) > 2 else "{}"
    if arg.startswith("@"):
        arg = open(arg[1:], encoding="utf-8").read()
    print(call(tool, json.loads(arg)))
