# Fontes de produção da Lucca (resgate de 2026-11-15)

Fora de `Assets/`: o Unity **não** importa esta pasta. É a fonte editável usada pelo Codex para gerar os assets em
`Assets/Modelos/Lucca`. Origem: `C:\Users\Sitr3n\Documents\Codex\2026-11-14\ess\outputs` (máquina do responsável).
Inventário completo, hashes e o que ficou de fora: `docs/recovery/CODEX_RECOVERY_REPORT.md`.

| Pasta | Conteúdo | Gera |
|---|---|---|
| `Sprint-2C/` | `Loopia-Animacao-2C.blend` (rig 57 ossos, ações 2C), FBX de ação 2C (Idle, Walk, Jump, SpringJump, BowReady, BowShot), relatório e evidências | `Idle`, `Bow Ready`, `Bow Shot`, `Loopia_BowUpper.mask` |
| `Sprint-2C.2/` | `Loopia-Locomocao-2C.2.blend` (Walk/Run/Jump/SpringJump revisados), FBX de ação 2C.2, relatório de locomoção e telemetria | `Walk`, `Run`, `Jump`, `Spring Jump` (commit `da0ad72`) |
| `Integracao/` | evidências do Test Runner e das partidas da integração na cena (commit `9eff525`) e o rascunho de PR do Codex | — |

Dependências: Blender 5.2.0 LTS (versão usada pelo Codex). O FBX do modelo (`Loopia-Game-Ready-2B.2.fbx`) e as texturas
já estão em `Assets/Modelos/Lucca` e não foram duplicados aqui. Vídeos, capturas PNG e os bundles git do Codex ficaram
fora do repositório (cópia de segurança local, hashes no relatório).
