"""Minimal client for the Unity MCP TCP bridge (127.0.0.1:6400).
Usage: python umcp.py <tool> '<json params>'   or   python umcp.py <tool> @file.json
"""
import json, socket, struct, sys

def call(tool, params, port=6400, timeout=300):
    s = socket.create_connection(("127.0.0.1", port), timeout=timeout)
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
