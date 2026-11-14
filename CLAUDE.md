@AGENTS.md

## Notas específicas para Claude Code
- O contrato acima vale integralmente. Comece por `docs/handoff/ESTADO_ATUAL.md`.
- Unity MCP: se não houver connector registrado, use `tools/unity-mcp/umcp.py` via Bash.
- Ao delegar a subagentes: um único agente dirige o Editor por vez; subagentes de leitura/inventário
  não tocam o Editor nem arquivos fora do que foi pedido.
