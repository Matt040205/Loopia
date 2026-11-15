# Unity MCP via TCP (`umcp.py`)

Cliente mínimo para o bridge do pacote `com.coplaydev.unity-mcp`, para agentes que não têm o
servidor MCP da Unity registrado como connector. Requer o Unity Editor aberto com o projeto.

```bash
python -I tools/unity-mcp/umcp.py ping '{}'
python -I tools/unity-mcp/umcp.py manage_scene '{"action":"get_active"}'
python -I tools/unity-mcp/umcp.py read_console '{"action":"get","types":["error"],"count":30}'
python -I tools/unity-mcp/umcp.py refresh_unity '{"mode":"force","compile":"request","wait_for_ready":true}'
python -I tools/unity-mcp/umcp.py run_tests '{"mode":"EditMode"}'            # devolve job_id
python -I tools/unity-mcp/umcp.py get_test_job '{"job_id":"<id>","include_details":true}'
```

Parâmetros grandes: grave num arquivo JSON e passe `@arquivo.json`.

## Protocolo
1. Conecta em `127.0.0.1:<porta>`. A porta muda quando o Unity reabre (6400, 6401, ...): o cliente lê
   `~/.unity-mcp/unity-mcp-status-*.json` do projeto Loopia (ou use `UNITY_MCP_PORT`).
2. Lê a linha `WELCOME UNITY-MCP 1 FRAMING=1\n`. **Não responder nada a ela.**
3. Cada mensagem: prefixo de 8 bytes big-endian com o tamanho + JSON `{"type": "<tool>", "params": {...}}`.

## Pegadinhas
- `execute_code`: o código entra num método — sem `using`; qualifique tipos
  (`UnityEditor.SceneManagement.EditorSceneManager`, `UnityEngine.Object`). `\n` dentro de string
  literal quebra a compilação. Lambdas não podem repetir nomes de variáveis do escopo externo.
- Durante domain reload o socket recusa conexão: esperar e tentar de novo.
- Editor fora de foco + `Application.runInBackground = false` congela o Play Mode no frame 2.
  Em testes, setar `Application.runInBackground = true` logo após entrar em Play Mode.
- Se o git reescrever a cena aberta, o Unity abre um diálogo modal que trava o bridge. Feche a
  cena (ou o Editor) antes de checkout/rebase que mexa em `.unity`.
- Só um agente por vez dirige o Editor.

## Testes de Play Mode e o rastreador de jobs
Os testes `PlayModeSmoke` entram em Play Mode e recarregam o domínio; o rastreador do MCP perde o
fim da execução: `get_test_job` fica em `running` / `completed: 0` e novos `run_tests` /
`refresh_unity` são recusados com `tests_running`. Procedimento:
1. Esperar ~60 s após disparar `PlayModeSmoke` (cada teste leva 7–20 s).
2. Ler o resultado no XML do Test Framework:
   `%USERPROFILE%/AppData/LocalLow/DefaultCompany/Loopia/TestResults.xml` (confirmar que o
   horário do arquivo é posterior ao disparo; olhar `<test-run ... result= total= passed= failed=>`).
3. Destravar o rastreador:
   `python -I tools/unity-mcp/umcp.py execute_code @tools/unity-mcp/clear_stuck_test_job.json`
   (deve responder `"result":true`).
Rode `Logic`/`SceneStatic` separados de `PlayModeSmoke`: esses têm resultado direto no `get_test_job`.
