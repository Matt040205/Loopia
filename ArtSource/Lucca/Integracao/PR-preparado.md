# Lucca na cena de gameplay

**Branch:** `feature/lucca-character-integration`  
**Base:** `sprint-3/gameplay-cena` (`ee46799`, PR #8)

A cena `HexPrototipo` ganha o prefab visual de Lucca com o rig Humanoid, locomoção in-place, camada de arco, mira e saltos. O combate mantém a autoridade sobre munição, alvo e projétil. A soltura usa o evento do clipe e o socket do arco. A pasta `Assets/Modelos/Lucca` tem os assets e importadores necessários.

## Revisão

- O diff de cena foi salvo pelo Unity Editor. A raiz de gameplay e as referências existentes foram mantidas.
- O conteúdo novo está agrupado em commits de assets, gameplay e testes/documentação. Os assets novos incluem .meta.
- Consulte `docs/character-integration/TEST_RESULTS.md` para as execuções do Test Runner e a validação na cena real.
- A passada ainda desliza a 3,5 unidades/s. O clipe aprovado tem amplitude curta. Veja `docs/character-integration/KNOWN_ISSUES.md`.
- A auditoria de origem, o comparativo visual, o vídeo e os relatórios do Test Runner estão no pacote de evidências.
- Rollback da conexão com a cena e do gameplay voltando o commit `9eff525`, após reverter as alterações de teste dependentes se elas estiverem presentes.

O PR de integração fica preparado localmente enquanto o PR #8 estiver aberto. Os dois alteram `HexPrototipo`.

