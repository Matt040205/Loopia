# Integração na Unity

A cena `Assets/Scenes/HexPrototipo.unity` reutiliza o GameObject Lucca e suas referências. A alteração de cena foi serializada pelo Unity Editor via MCP. Os objetos visuais de protótipo `Corpo` e `Frente` ficaram desativados. O prefab `LuccaVisual` é a origem de `Lucca/VisualRoot` na cena. O corolário da escala visual de 0,66 é uma altura próxima ao indicador de 1 unidade da direção de arte, sem mudar os parâmetros do NavMeshAgent.

O Animator e `LuccaAnimationBridge` ficam no objeto visual. O bridge recebe o início da travessia de `HexPlayer`, lê a velocidade real do agente e aplica a mira no osso `Chest` depois da avaliação da animação. A mira leva braços, arco e capa pela mesma hierarquia. O quadril, o agente e a raiz de navegação permanecem sob o gameplay. Não há pacote adicional.

`LuccaCombate` fixa um alvo por preparação e revalida vida, alcance, ângulo e munição na soltura. Mudar a preferência de `AlvoAtual` durante uma preparação mantém o alvo reservado enquanto ele seguir elegível. A preparação é descartada quando o alvo deixa de ser elegível, o Animator abandona o estado antes da soltura ou a partida termina. Sem Animator funcional, vale o disparo imediato na altura anterior. Quando a animação está ativa, o lançamento usa `Arrow_Spawn`, com a mesma origem antiga como fallback se o socket estiver ausente.

## Revisar a cena

1. Abrir a worktree em Unity 6000.3.10f1 e carregar `Assets/Scenes/HexPrototipo.unity`.
2. Conferir `Lucca/VisualRoot` e os componentes do GameObject Lucca. O prefab visual está em `Assets/Modelos/Lucca`.
3. Entrar em Play Mode pela cena ou iniciar a partida pelo Acampamento. Para reproduzir a subida, colocar Mola antes de Ilha Alta no sentido do percurso.
4. No Test Runner, executar as categorias EditMode `Logic`, `SceneStatic`, `PlayModeSmoke` e `PlayModeMedicao`. As duas últimas entram em Play Mode.

A reversão da integração de cena e gameplay é um `git revert` do commit correspondente. O patch permanece empilhado sobre o PR #8, que já altera a mesma cena. A revisão pode ser preparada localmente enquanto o PR #8 estiver aberto.
