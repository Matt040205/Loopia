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

## Legibilidade (Pass D1)
Metrica de figura/fundo por captura: luminancia relativa (sRGB -> linear, Rec.709) do topo das ilhas vs. fundo imediato.
```bash
python -I tools/visual-baseline/run_baseline.py pontos     # pontos por seed x resolucao -> docs/art/pass-d1/pontos/ (LOOPIA_PONTOS_DIR)
python -I tools/visual-baseline/legibilidade.py <pasta_de_jpgs> docs/art/pass-d1/pontos <saida.json> [--cinza <pasta>]
```
- `pontos.cs`: varios pontos dentro do topo de cada ilha (so os nao ocluidos) e mascara de cobertura (renderers de ilha/heroi/inimigo).
  Fundo imediato = celulas nao cobertas a ate 60 px de uma coberta (fora do anel, no miolo e entre ilhas).
- Saida: `L_topo`, `L_fundo`, `razao`, `michelson`, matiz medio de topo e fundo. `--cinza` salva a versao em escala de cinza (seed 12345, 1920x1080).
- `LOOPIA_HORAS_EXTRAS=1` no `capture` inclui horas 5, 6, 7, 17 e 19 (seed 12345, 1920x1080); o manifest ganha `luz` (fog, ambient, sol) por captura.
- `LOOPIA_MODO_ANTIGO=1` emula o `DayNightController` anterior ao Pass D1 (`cs/antigo.cs`: fog Exponential 0,01, sol girando abaixo do horizonte)
  para um "antes" na mesma sessao e geometria. **Cuidado:** depois de usar, o componente fica desligado (`enabled=false`) ate `dn.enabled = true`.
- `cs/measure_profundidade.cs`: profundidade de camera (eye depth) de ilhas e fundo, usada para escolher `fogInicio`/`fogFim`.
- As capturas antigas de `docs/art/pass-a/depois/` mostram ilhas residuais do mapa anterior (a sessao nao limpou tudo): servem so de referencia, nao de geometria.

## Limites
- Render da câmera não inclui Canvas overlay nem IMGUI.
- `UnityStats` são números do Editor (indicativos), não de build.
