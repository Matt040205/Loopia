# Inimigos — fontes editáveis (Sprint 5B)

Modelos recebidos: pacote `Loopia_Sprint5_3D_Models_Review_v1.1.zip` (Sprint 5A.1, GPT). Geometria, pesos, UVs,
materiais e rigs **não foram alterados** nesta sprint; os `.blend` daqui são cópias dos recebidos com as **ações de
produção** acrescentadas (as ações de QA da 5A.1 continuam no arquivo, com fake user, e a ação ativa ao abrir é a do
pacote). Registro completo: [SPRINT-5B-ANIMATION-INTEGRATION.md](../../docs/sprints/SPRINT-5B-ANIMATION-INTEGRATION.md).

| Arquivo | Conteúdo |
|---|---|
| `Sprint-5B/Blender/Bat_Common.blend` | Idle, Fly, Attack, Hit, Death (faixas NLA silenciadas `PROD …`) |
| `Sprint-5B/Blender/Bat_AncientBoss.blend` | Boss_Idle, Boss_Fly, Boss_Attack, Boss_Hit, Boss_Death, Boss_Revoada |
| `Sprint-5B/Blender/Wolf_Common.blend` | Wolf_Idle, Wolf_Walk, Wolf_Run, Wolf_Attack, Wolf_Hit, Wolf_Death |
| `Sprint-5B/Blender/acoes_de_producao.json` | duração, quadros, laço, marcadores, velocidade da locomoção, hash dos dados do modelo |
| `Sprint-5B/Blender/qa_clipes_producao.json` | QA das ações de produção (membranas, Hit aditivo, cross-fades, chão, deslizamento, laços, bounds) |
| `Sprint-5B/Blender/auditoria_v1.1.json` | auditoria dos `.blend` recebidos |
| `Sprint-5B/scripts/` | geração, QA, exportação e renders (Blender 5.2, Python embutido) |

## Regenerar (da raiz do repo; `blender` = Blender 5.2)

```powershell
# 1. ações de produção a partir dos .blend recebidos (pasta 01_Blender do pacote extraído)
blender -b --factory-startup --python-exit-code 7 --python ArtSource/Inimigos/Sprint-5B/scripts/produzir.py -- <pacote>/01_Blender ArtSource/Inimigos/Sprint-5B/Blender
# 2. QA dos clipes de produção
blender -b --factory-startup --python-exit-code 7 --python ArtSource/Inimigos/Sprint-5B/scripts/qa_clipes.py -- ArtSource/Inimigos/Sprint-5B/Blender qa.json
# 3. FBX só de animação (mesmos eixos/escala do export_fbx.py do pacote), um por modelo
blender -b --factory-startup --python-exit-code 7 --python ArtSource/Inimigos/Sprint-5B/scripts/exportar_animacoes.py -- ArtSource/Inimigos/Sprint-5B/Blender/Bat_Common.blend Assets/Modelos/Inimigos/Bat_Common/Bat_Common_Animacoes.fbx
# 4. na Unity: Loopia > Inimigos > Configurar modelos animados
```

Para editar à mão: abra o `.blend`, Action Editor → escolha a ação de produção. O lobo foi assado de um IK analítico
(nenhuma restrição fica no arquivo): para mudar a passada, edite `anim_lobo.py` e regenere, ou edite as chaves em FK.
Renders de conferência: `renderizar.py` + `montar_evidencias.py` (Pillow).
