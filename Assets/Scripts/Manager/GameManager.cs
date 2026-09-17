using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Importante para gerenciar cenas

public class GameManager : MonoBehaviour
{
    // Singleton pattern para acesso f�cil de outros scripts
    public static GameManager Instance;

    [Header("Estado do Jogo")]
    public int numeroDoLoop = 1;
    public float multiplicadorDeStatus = 1.0f;
    [Tooltip("O jogador vence ao completar este n�mero de loops.")]
    public int loopsParaVencer = 3;

    // Vari�vel para controlar o estado de pausa do jogo
    private bool isPaused = false;

    [Header("Refer�ncias UI")]
    [Tooltip("Arraste aqui o texto do Canvas que mostrar� o loop atual. Este campo exibe 'Loop: X'.")]
    public TextMeshProUGUI textoLoop; // Refer�ncia ao TextMeshProUGUI para o loop

    [Tooltip("Arraste aqui o painel (GameObject) do seu Menu de Pausa.")]
    public GameObject pauseMenuUI; // Refer�ncia ao painel do Menu de Pausa

    [Header("Configura��es de Cena")]
    [Tooltip("O nome exato do arquivo da sua cena de vit�ria.")]
    public string nomeCenaVitoria = "WinScene"; // Nome da cena de vit�ria

    [Tooltip("O nome exato do arquivo da sua cena do Menu Principal.")]
    public string nomeCenaMenuPrincipal = "MainMenuScene"; // Nome da cena do menu principal

    void Awake()
    {
        // Garante que haja apenas uma inst�ncia do GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mant�m o GameManager ativo entre cenas
        }
        else
        {
            Destroy(gameObject); // Destr�i inst�ncias duplicadas
        }
    }

    void OnEnable()
    {
        // Assina o evento de carregamento de cena para redefinir o estado do jogo e a UI
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Desassina o evento para evitar vazamentos de mem�ria
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Tenta encontrar o TextMeshProUGUI se n�o estiver atribu�do no Inspector (�til para a primeira cena)
        if (textoLoop == null)
        {
            textoLoop = BuscarTextoLoop();
            if (textoLoop != null)
            {
                Debug.Log("<color=green>TextMeshProUGUI encontrado no Start.</color>");
            }
        }
        // Garante que o menu de pausa esteja desativado e o jogo despausado ao iniciar
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Normaliza a velocidade do tempo
        isPaused = false; // Define o estado inicial como n�o pausado
        ResetGameVariables(); // Reinicia as vari�veis do jogo
        AtualizarUILoop(); // Atualiza a UI do loop
    }

    // Chamado sempre que uma nova cena � carregada
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se a cena carregada n�o � a de vit�ria, reinicia o jogo e reconecta a UI
        if (scene.name != nomeCenaVitoria)
        {
            Debug.Log($"<color=orange>Cena '{scene.name}' carregada. Reiniciando vari�veis de jogo e buscando TextMeshProUGUI.</color>");

            // Reencontra o componente TextMeshProUGUI na nova cena, pois o anterior foi destru�do
            textoLoop = BuscarTextoLoop();
            if (textoLoop == null)
            {
                Debug.LogError("<color=red>ERRO: TextMeshProUGUI para 'textoLoop' n�o encontrado na cena carregada! Certifique-se de que h� um TextMeshProUGUI ativo e �nico na cena do jogo para o loop.</color>");
            }
            else
            {
                Debug.Log("<color=green>TextMeshProUGUI encontrado e reatribu�do ap�s carregamento da cena.</color>");
            }

            // Garante que os menus estejam desativados e o jogo despausado ao carregar uma nova cena
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            // Garante que o painel de op��es tamb�m seja fechado se o OptionsManager existir
            if (OptionsManager.Instance != null) OptionsManager.Instance.CloseOptionsPanel();
            Time.timeScale = 1f; // Normaliza a velocidade do tempo
            isPaused = false; // Garante que o jogo n�o esteja pausado

            // S� reseta as vari�veis do jogo se n�o estiver voltando para o menu principal
            if (scene.name != nomeCenaMenuPrincipal)
            {
                ResetGameVariables();
            }
            AtualizarUILoop(); // Atualiza a UI
        }
        else
        {
            Debug.Log($"<color=blue>Cena de vit�ria '{scene.name}' carregada. N�o reiniciando vari�veis de jogo.</color>");
        }
    }

    void Update()
    {
        // Detecta o pressionar da tecla ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Se o painel de configura��es estiver aberto, o ESC o fecha primeiro
            if (OptionsManager.Instance != null && OptionsManager.Instance.IsOptionsPanelActive())
            {
                // Chama o m�todo no OptionsManager para lidar com o "Voltar" (seja do cr�dito para op��es, ou op��es para pausa)
                OptionsManager.Instance.HandleBackButtonPress();
            }
            else // Se o menu de configura��es n�o est� aberto, alterna o estado de pausa normal
            {
                if (isPaused)
                {
                    ResumeGame(); // Se j� estiver pausado, despausa
                }
                else
                {
                    PauseGame(); // Se n�o estiver pausado, pausa
                }
            }
        }
    }

    // M�todo p�blico para avan�ar o n�mero do loop
    // Evento disparado sempre que um loop é completado (blocos regeneram, etc.)
    public static event System.Action OnLoopAvancado;

    public void AvancarLoop()
    {
        numeroDoLoop++;
        OnLoopAvancado?.Invoke();
        multiplicadorDeStatus *= 1.10f; // Aumenta o multiplicador de status em 10% por loop (conforme PDF)

        Debug.Log($"<color=magenta>NOVO LOOP INICIADO! Loop: {numeroDoLoop}. Multiplicador: {multiplicadorDeStatus:F2}</color>");

        // Resetar XP para novo loop (PDF: 'apenas no loop, n�o globalmente')
        var player = FindFirstObjectByType<PlayerStatus>();
        if (player != null)
        {
            player.ResetarParaNovoLoop();
        }

        AtualizarUILoop(); // Atualiza a UI do loop

        // L�gica de vit�ria: verifica se o jogador atingiu ou excedeu o n�mero de loops necess�rios
        if (numeroDoLoop > loopsParaVencer)
        {
            Debug.Log($"<color=green><b>VIT�RIA!</b></color> O jogador completou {loopsParaVencer} loops. Carregando cena de vit�ria...");
            SceneManager.LoadScene(nomeCenaVitoria); // Carrega a cena de vit�ria
        }
    }

    // M�todo para pausar o jogo
    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Ativa o menu de pausa
        }
        Time.timeScale = 0f; // Congela o tempo do jogo
        Debug.Log("<color=yellow>Jogo Pausado.</color>");
    }

    // M�todo para resumir o jogo
    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Desativa o menu de pausa
        }
        Time.timeScale = 1f; // Retoma o tempo normal do jogo
        Debug.Log("<color=yellow>Jogo Resumido.</color>");
    }

    // M�todo para carregar a cena do menu principal
    public void LoadMainMenu()
    {
        // Garante que o jogo esteja despausado e os menus fechados antes de carregar uma nova cena
        Time.timeScale = 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (OptionsManager.Instance != null) OptionsManager.Instance.CloseOptionsPanel();
        SceneManager.LoadScene(nomeCenaMenuPrincipal);
        Debug.Log("<color=cyan>Carregando Menu Principal...</color>");
    }

    // M�todo para abrir o menu de configura��es (chamado pelo bot�o "Configs" no pauseMenuUI)
    public void OpenConfigs()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Esconde o menu de pausa
        }

        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.OpenOptionsPanel(); // Chama o m�todo para abrir o painel de configura��es no OptionsManager
            Debug.Log("<color=white>Abrindo Configura��es...</color>");
        }
        else
        {
            Debug.LogError("<color=red>ERRO: OptionsManager.Instance n�o encontrado! Certifique-se de que o OptionsManager est� na cena e � um Singleton v�lido.</color>");
        }
    }

    // M�todo chamado pelo OptionsManager para retornar ao menu de pausa
    public void ReturnToPauseMenuFromConfigs()
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.CloseOptionsPanel(); // Fecha o painel de configura��es
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Mostra o menu de pausa novamente
            Debug.Log("<color=white>Retornando ao Menu de Pausa.</color>");
        }
    }

    // M�todo auxiliar para reiniciar as vari�veis de estado do jogo (loops, multiplicadores)
    private void ResetGameVariables()
    {
        numeroDoLoop = 0; // Reinicia o contador de loops
        multiplicadorDeStatus = 1.0f; // Reinicia o multiplicador de status
        Debug.Log("<color=green>Vari�veis de jogo resetadas.</color>");
    }

    // Atualiza o texto na interface do usu�rio para mostrar o loop atual
    // Procura o texto do loop pela tag (evita pegar qualquer TextMeshProUGUI da cena)
    private TextMeshProUGUI BuscarTextoLoop()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("UI_ContadorLoop");
        if (obj != null)
        {
            return obj.GetComponent<TextMeshProUGUI>();
        }
        return textoLoop;
    }

    private void AtualizarUILoop()
    {
        if (textoLoop != null)
        {
            textoLoop.text = "Loop: " + numeroDoLoop;
            Debug.Log($"<color=gray>UI do Loop Atualizada: {numeroDoLoop}</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>Aviso: A refer�ncia ao textoLoop � nula. N�o foi poss�vel atualizar a UI.</color>");
        }
    }
}
