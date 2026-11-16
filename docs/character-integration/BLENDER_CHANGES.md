# Blender e exportação

A personagem e o rig da Sprint 2C passaram pela auditoria via Blender MCP. A fonte aprovada mantém seu hash, suas ações e seu skinning. A integração usa o FBX principal byte a byte e os clipes Humanoid da Sprint 2C com as curvas auxiliares preservadas.

A captura inicial da cena mostrou a flecha como um bloco amarelo. A flecha de produção foi exportada pelo Blender MCP a partir de uma cena temporária, com 72 triângulos, haste, ponta e três aletas. Seu FBX está em `Assets/Modelos/Lucca/Flecha.fbx`. O prefab `VisualFlecha` orienta a ponta em +Z, tem cerca de 0,34 unidade de comprimento, três materiais compartilhados e zero colliders. A orientação inicial segue a direção ao alvo e a trajetória teleguiada continua no script existente.

A redução dos clipes foi feita no Editor. A comparação e os tamanhos ficam em [TEST_RESULTS.md](TEST_RESULTS.md).
