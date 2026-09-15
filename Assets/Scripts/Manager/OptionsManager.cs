using UnityEngine;
using UnityEngine.UI; // Necessário para o tipo Button

public class OptionsManager : MonoBehaviour
{
    // Singleton pattern para acesso fácil de outros scripts
    public static OptionsManager Instance;

    [Header("Referências UI")]
    [Tooltip("Arraste aqui o painel (GameObject) principal do seu Menu de Configurações.")]
    public GameObject configsPanelUI; // Contêiner principal para toda a UI das opções

    [Tooltip("Arraste aqui o GameObject que contém os botões principais de Opções (ex: Áudio, Vídeo, Controles).")]
    public GameObject mainOptionsPanel; // Contêiner para os botões de opções gerais

    [Tooltip("Arraste aqui o componente Button que ativa o painel de Créditos.")]
    public Button creditsButton; // O componente de botão para "Créditos"

    [Tooltip("Arraste aqui o GameObject que contém o conteúdo dos Créditos (o objeto 'Pessoas').")]
    public GameObject creditsContentObject; // O GameObject 'Pessoas' com o conteúdo dos créditos

    [Tooltip("Arraste aqui o componente Button de Voltar (Back) dentro do menu de opções.")]
    public Button backButton; // O componente de botão "Voltar" (para atribuição no Inspector)

    // Variável interna para rastrear se estamos no painel de créditos
    private bool isInCreditsPanel = false;

    void Awake()
    {
        // Garante que haja apenas uma instância do OptionsManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantém o OptionsManager ativo entre cenas
        }
        else
        {
            Destroy(gameObject); // Destrói instâncias duplicadas
        }
    }

    void Start()
    {
        // Garante que todos os painéis e conteúdos comecem desativados
        if (configsPanelUI != null) configsPanelUI.SetActive(false);
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
        if (creditsButton != null) creditsButton.gameObject.SetActive(false); // Esconde o botão de créditos inicialmente
        if (creditsContentObject != null) creditsContentObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false); // Esconde o botão de voltar inicialmente

        // Verificação de erros para atribuições no Inspector
        if (configsPanelUI == null) Debug.LogError("<color=red>ERRO: 'Configs Panel UI' não atribuído no OptionsManager!</color>");
        if (mainOptionsPanel == null) Debug.LogError("<color=red>ERRO: 'Main Options Panel' não atribuído no OptionsManager!</color>");
        if (creditsButton == null) Debug.LogError("<color=red>ERRO: 'Credits Button' não atribuído no OptionsManager!</color>");
        if (creditsContentObject == null) Debug.LogError("<color=red>ERRO: 'Credits Content Object' não atribuído no OptionsManager!</color>");
        if (backButton == null) Debug.LogError("<color=red>ERRO: 'Back Button' não atribuído no OptionsManager!</color>");

        // Adiciona listeners para os eventos de clique dos botões via código (melhor prática para singletons)
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(ShowCredits);
        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    // Chamado pelo GameManager para abrir o painel de configurações
    public void OpenOptionsPanel()
    {
        if (configsPanelUI != null)
        {
            configsPanelUI.SetActive(true); // Ativa o painel principal de configurações

            // Esconde outros botões de opções (se houver) e mostra o de créditos
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (creditsButton != null) creditsButton.gameObject.SetActive(true);
            if (creditsContentObject != null) creditsContentObject.SetActive(false); // Garante que o conteúdo dos créditos esteja oculto
            if (backButton != null) backButton.gameObject.SetActive(true); // O botão de voltar deve estar visível

            isInCreditsPanel = false; // Não estamos no painel de créditos ao abrir as opções
            Debug.Log("<color=green>Painel de Configurações Aberto. Mostrando apenas o botão de Créditos.</color>");
        }
    }

    // Chamado pelo GameManager para fechar o painel de opções
    public void CloseOptionsPanel()
    {
        if (configsPanelUI != null)
        {
            configsPanelUI.SetActive(false);
            // Garante que todos os sub-elementos sejam desativados ao fechar o painel
            if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false);
            if (creditsButton != null) creditsButton.gameObject.SetActive(false);
            if (creditsContentObject != null) creditsContentObject.SetActive(false);
            if (backButton != null) backButton.gameObject.SetActive(false);
            isInCreditsPanel = false; // Reseta o estado
            Debug.Log("<color=green>Painel de Configurações Fechado.</color>");
        }
    }

    // Método para exibir o painel de Créditos (chamado pelo clique no 'creditsButton')
    private void ShowCredits()
    {
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(false); // Esconde os botões de opções principais
        if (creditsButton != null) creditsButton.gameObject.SetActive(false); // Esconde o próprio botão de créditos
        if (creditsContentObject != null) creditsContentObject.SetActive(true); // Mostra o GameObject 'Pessoas'
        if (backButton != null) backButton.gameObject.SetActive(true); // Garante que o botão de voltar esteja ativo
        isInCreditsPanel = true; // Define que estamos no painel de créditos
        Debug.Log("<color=green>Mostrando Painel de Créditos (Pessoas).</color>");
    }

    // Método para retornar do painel de Créditos para a visualização principal de Opções
    private void ShowMainOptions()
    {
        if (mainOptionsPanel != null) mainOptionsPanel.SetActive(true); // Mostra os botões de opções principais
        if (creditsButton != null) creditsButton.gameObject.SetActive(true); // Mostra o botão de créditos novamente
        if (creditsContentObject != null) creditsContentObject.SetActive(false); // Esconde o GameObject 'Pessoas'
        isInCreditsPanel = false; // Define que não estamos mais no painel de créditos
        Debug.Log("<color=green>Retornando ao Painel Principal de Opções.</color>");
    }

    // Lógica para o clique no botão "Voltar"
    private void OnBackButtonClicked()
    {
        // Se estiver no painel de créditos, volta para a visualização principal de opções
        if (isInCreditsPanel)
        {
            ShowMainOptions();
        }
        else // Caso contrário, estamos na visualização principal de opções, então voltamos para o Menu de Pausa
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToPauseMenuFromConfigs(); // Chama o GameManager para retornar ao menu de pausa
            }
            else
            {
                Debug.LogError("<color=red>ERRO: GameManager.Instance não encontrado ao tentar voltar do menu de opções!</color>");
            }
        }
    }

    // NOVO MÉTODO: Chamado pelo GameManager para simular o clique do botão Voltar (útil para o ESC)
    public void HandleBackButtonPress()
    {
        // Verifica se o botão "Voltar" existe e está ativo antes de invocar o clique.
        if (backButton != null && backButton.gameObject.activeSelf)
        {
            OnBackButtonClicked(); // Chama a mesma lógica do clique do botão
        }
        else
        {
            Debug.LogWarning("<color=orange>Aviso: Botão 'Voltar' no OptionsManager não está ativo ou não existe, não foi possível simular o clique.</color>");
        }
    }

    // Método para verificar se o painel principal de opções está ativo (útil para o GameManager)
    public bool IsOptionsPanelActive()
    {
        return configsPanelUI != null && configsPanelUI.activeSelf;
    }
}
