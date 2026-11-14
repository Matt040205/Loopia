# Baseline visual (capturas + medidas)

Captura reproduzível da `HexPrototipo` pelo Unity MCP (ver `tools/unity-mcp/README.md`). Nunca salva cena ou asset.

## Antes de rodar
1. Unity aberto na `HexPrototipo`, cena sem alterações pendentes, sem testes rodando.
2. **Faça backup do save**: o Play Mode regrava
   `%USERPROFILE%/AppData/LocalLow/DefaultCompany/GameJam13-06/loopia_save.json`. Copie o arquivo antes e restaure
   depois (compare SHA-256). O runner não faz isso sozinho.

## Uso (da raiz do repo)
```bash
python -I tools/visual-baseline/run_baseline.py play
python -I tools/visual-baseline/run_baseline.py capture
python -I tools/visual-baseline/run_baseline.py measure
python -I tools/visual-baseline/run_baseline.py stats
python -I tools/visual-baseline/run_baseline.py stop
```
- Matriz fixa: seeds 12345/7/2024 × horas 12/18/22 × 1920×1080 e 2520×1080; `timeScale = 0` durante as capturas.
- Saída padrão: `docs/art/baseline/` (+ `manifest.json`). Para um "depois" sem sobrescrever o baseline:
  `LOOPIA_CAPTURE_DIR=docs/art/pass-a/depois python -I tools/visual-baseline/run_baseline.py capture`.
- Medidas brutas vão para `docs/art/audit/_raw_*.json`; `build_report.py` gera `MEDIDAS.md`.
- `gameview_shot.py`: print da Game view (inclui HUD/IMGUI, na resolução do painel do Editor).

## Limites
- Render da câmera não inclui Canvas overlay nem IMGUI.
- `UnityStats` são números do Editor (indicativos), não de build.
