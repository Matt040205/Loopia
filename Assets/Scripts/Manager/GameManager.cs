using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Importante para gerenciar cenas

public class GameManager : MonoBehaviour
{
    // Singleton pattern para acesso fácil de outros scripts
    public static GameManager Instance;

    [Header("Estado do Jogo")]
    public int numeroDoLoop = 1;
    public float multiplicadorDeStatus = 1.0f;
    [Tooltip("O jogador vence ao completar este número de loops.")]
    public int loopsParaVencer = 3;

    // Variável para controlar o estado de pausa do jogo
    private bool isPaused = false;

    [Header("Referências UI")]
    [Tooltip("Arraste aqui o texto do Canvas que mostrará o loop atual. Este campo exibe 'Loop: X'.")]
    public TextMeshProUGUI textoLoop; // Referência ao TextMeshProUGUI para o loop

    [Tooltip("Arraste aqui o painel (GameObject) do seu Menu de Pausa.")]
    public GameObject pauseMenuUI; // Referência ao painel do Menu de Pausa

    [Header("Configurações de Cena")]
    [Tooltip("O nome exato do arquivo da sua cena de vitória.")]
    public string nomeCenaVitoria = "WinScene"; // Nome da cena de vitória

    [Tooltip("O nome exato do arquivo da sua cena do Menu Principal.")]
    public string nomeCenaMenuPrincipal = "MainMenuScene"; // Nome da cena do menu principal

    void Awake()
    {
        // Garante que haja apenas uma instância do GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém o GameManager ativo entre cenas
        }
        else
        {
            Destroy(gameObject); // Destrói instâncias duplicadas
        }
    }

    void OnEnable()
    {
        // Assina o evento de carregamento de cena para redefinir o estado do jogo e a UI
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Desassina o evento para evitar vazamentos de memória
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Tenta encontrar o TextMeshProUGUI se não estiver atribuído no Inspector (útil para a primeira cena)
        if (textoLoop == null)
        {
            textoLoop = FindObjectOfType<TextMeshProUGUI>();
            if (textoLoop != null)
            {
                Debug.Log("<color=green>TextMeshProUGUI encontrado no Start.</color>");
            }
        }
        // Garante que o menu de pausa esteja desativado e o jogo despausado ao iniciar
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f; // Normaliza a velocidade do tempo
        isPaused = false; // Define o estado inicial como não pausado
        ResetGameVariables(); // Reinicia as variáveis do jogo
        AtualizarUILoop(); // Atualiza a UI do loop
    }

    // Chamado sempre que uma nova cena é carregada
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se a cena carregada não é a de vitória, reinicia o jogo e reconecta a UI
        if (scene.name != nomeCenaVitoria)
        {
            Debug.Log($"<color=orange>Cena '{scene.name}' carregada. Reiniciando variáveis de jogo e buscando TextMeshProUGUI.</color>");

            // Reencontra o componente TextMeshProUGUI na nova cena, pois o anterior foi destruído
            textoLoop = FindObjectOfType<TextMeshProUGUI>();
            if (textoLoop == null)
            {
                Debug.LogError("<color=red>ERRO: TextMeshProUGUI para 'textoLoop' não encontrado na cena carregada! Certifique-se de que há um TextMeshProUGUI ativo e único na cena do jogo para o loop.</color>");
            }
            else
            {
                Debug.Log("<color=green>TextMeshProUGUI encontrado e reatribuído após carregamento da cena.</color>");
            }

            // Garante que os menus estejam desativados e o jogo despausado ao carregar uma nova cena
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            // Garante que o painel de opções também seja fechado se o OptionsManager existir
            if (OptionsManager.Instance != null) OptionsManager.Instance.CloseOptionsPanel();
            Time.timeScale = 1f; // Normaliza a velocidade do tempo
            isPaused = false; // Garante que o jogo não esteja pausado

            // Só reseta as variáveis do jogo se não estiver voltando para o menu principal
            if (scene.name != nomeCenaMenuPrincipal)
            {
                ResetGameVariables();
            }
            AtualizarUILoop(); // Atualiza a UI
        }
        else
        {
            Debug.Log($"<color=blue>Cena de vitória '{scene.name}' carregada. Não reiniciando variáveis de jogo.</color>");
        }
    }

    void Update()
    {
        // Detecta o pressionar da tecla ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Se o painel de configurações estiver aberto, o ESC o fecha primeiro
            if (OptionsManager.Instance != null && OptionsManager.Instance.IsOptionsPanelActive())
            {
                // Chama o método no OptionsManager para lidar com o "Voltar" (seja do crédito para opções, ou opções para pausa)
                OptionsManager.Instance.HandleBackButtonPress();
            }
            else // Se o menu de configurações não está aberto, alterna o estado de pausa normal
            {
                if (isPaused)
                {
                    ResumeGame(); // Se já estiver pausado, despausa
                }
                else
                {
                    PauseGame(); // Se não estiver pausado, pausa
                }
            }
        }
    }

    // Método público para avançar o número do loop
    public void AvancarLoop()
    {
        numeroDoLoop++;
        multiplicadorDeStatus *= 1.10f; // Aumenta o multiplicador de status

        Debug.Log($"<color=magenta>NOVO LOOP INICIADO! Loop: {numeroDoLoop}. Multiplicador: {multiplicadorDeStatus:F2}</color>");

        AtualizarUILoop(); // Atualiza a UI do loop

        // Lógica de vitória: verifica se o jogador atingiu ou excedeu o número de loops necessários
        if (numeroDoLoop > loopsParaVencer)
        {
            Debug.Log($"<color=green><b>VITÓRIA!</b></color> O jogador completou {loopsParaVencer} loops. Carregando cena de vitória...");
            SceneManager.LoadScene(nomeCenaVitoria); // Carrega a cena de vitória
        }
    }

    // Método para pausar o jogo
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

    // Método para resumir o jogo
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

    // Método para carregar a cena do menu principal
    public void LoadMainMenu()
    {
        // Garante que o jogo esteja despausado e os menus fechados antes de carregar uma nova cena
        Time.timeScale = 1f;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (OptionsManager.Instance != null) OptionsManager.Instance.CloseOptionsPanel();
        SceneManager.LoadScene(nomeCenaMenuPrincipal);
        Debug.Log("<color=cyan>Carregando Menu Principal...</color>");
    }

    // Método para abrir o menu de configurações (chamado pelo botão "Configs" no pauseMenuUI)
    public void OpenConfigs()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Esconde o menu de pausa
        }

        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.OpenOptionsPanel(); // Chama o método para abrir o painel de configurações no OptionsManager
            Debug.Log("<color=white>Abrindo Configurações...</color>");
        }
        else
        {
            Debug.LogError("<color=red>ERRO: OptionsManager.Instance não encontrado! Certifique-se de que o OptionsManager está na cena e é um Singleton válido.</color>");
        }
    }

    // Método chamado pelo OptionsManager para retornar ao menu de pausa
    public void ReturnToPauseMenuFromConfigs()
    {
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.CloseOptionsPanel(); // Fecha o painel de configurações
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Mostra o menu de pausa novamente
            Debug.Log("<color=white>Retornando ao Menu de Pausa.</color>");
        }
    }

    // Método auxiliar para reiniciar as variáveis de estado do jogo (loops, multiplicadores)
    private void ResetGameVariables()
    {
        numeroDoLoop = 0; // Reinicia o contador de loops
        multiplicadorDeStatus = 1.0f; // Reinicia o multiplicador de status
        Debug.Log("<color=green>Variáveis de jogo resetadas.</color>");
    }

    // Atualiza o texto na interface do usuário para mostrar o loop atual
    private void AtualizarUILoop()
    {
        if (textoLoop != null)
        {
            textoLoop.text = "Loop: " + numeroDoLoop;
            Debug.Log($"<color=gray>UI do Loop Atualizada: {numeroDoLoop}</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange>Aviso: A referência ao textoLoop é nula. Não foi possível atualizar a UI.</color>");
        }
    }
}
