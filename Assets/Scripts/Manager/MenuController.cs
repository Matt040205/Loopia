using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para gerenciar cenas

/// <summary>
/// Este é um controlador aprimorado para gerenciar tanto cenas quanto a visibilidade de painéis de menu.
/// Adicione este componente a um GameObject persistente na sua cena (como um "GameManager").
/// </summary>
public class SceneController : MonoBehaviour
{
    // Variáveis públicas para referenciar os painéis de UI no Editor da Unity.
    // Arraste os GameObjects dos seus painéis de menu para esses campos no Inspector.
    public GameObject mainMenuPanel;    // Painel que contém os botões Iniciar, Sair, Créditos, Opções
    public GameObject optionsPanel;     // Painel que contém as opções do jogo
    public GameObject creditsPanel;     // Painel que exibe os créditos

    /// <summary>
    /// Chamado quando o script é carregado. Usado para inicializar o estado dos painéis.
    /// Garante que apenas o painel do menu principal esteja ativo no início.
    /// </summary>
    void Awake()
    {
        // Certifica-se de que os painéis foram atribuídos no Inspector para evitar erros.
        if (mainMenuPanel == null)
            Debug.LogError("mainMenuPanel não foi atribuído no Inspector!", this);
        if (optionsPanel == null)
            Debug.LogError("optionsPanel não foi atribuído no Inspector!", this);
        if (creditsPanel == null)
            Debug.LogError("creditsPanel não foi atribuído no Inspector!", this);

        // Ao iniciar, mostra apenas o menu principal.
        ShowMainMenu();
    }

    /// <summary>
    /// Ativa o painel do menu principal e desativa todos os outros painéis.
    /// Conecte este método a botões como "Voltar" nos painéis de opções/créditos.
    /// </summary>
    public void ShowMainMenu()
    {
        Debug.Log("Exibindo Menu Principal.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>
    /// Ativa o painel de opções e desativa todos os outros painéis.
    /// Conecte este método a um botão "Opções" no menu principal.
    /// </summary>
    public void ShowOptionsMenu()
    {
        Debug.Log("Exibindo Opções.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>
    /// Ativa o painel de créditos e desativa todos os outros painéis.
    /// Conecte este método a um botão "Créditos" no menu principal.
    /// </summary>
    public void ShowCreditsMenu()
    {
        Debug.Log("Exibindo Créditos.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    /// <summary>
    /// Carrega uma cena com base no nome fornecido.
    /// Este método é público para que possa ser facilmente vinculado a eventos de botões no Inspector da Unity.
    /// </summary>
    /// <param name="nomeDaCena">O nome exato da cena a ser carregada (deve estar nos Build Settings).</param>
    public void CarregarCenaPorNome(string nomeDaCena)
    {
        // Verifica se o nome da cena não é nulo ou vazio para evitar erros.
        if (string.IsNullOrEmpty(nomeDaCena))
        {
            Debug.LogError("O nome da cena para carregar não foi fornecido!");
            return;
        }

        Debug.Log($"Carregando cena: {nomeDaCena}...");
        SceneManager.LoadScene(nomeDaCena);
    }

    /// <summary>
    /// Recarrega a cena que está atualmente ativa.
    /// Útil para botões de "Reiniciar" ou "Tentar Novamente".
    /// </summary>
    public void ReiniciarCena()
    {
        // Obtém o nome da cena ativa no momento.
        string nomeCenaAtual = SceneManager.GetActiveScene().name;

        Debug.Log($"Reiniciando cena: {nomeCenaAtual}...");
        SceneManager.LoadScene(nomeCenaAtual);
    }

    /// <summary>
    /// Fecha a aplicação.
    /// Esta função só funciona em builds finais (PC, Mac, Linux, Mobile).
    /// No Editor da Unity, ele para o modo de reprodução.
    /// </summary>
    public void SairDoJogo()
    {
        Debug.Log("Fechando a aplicação...");
        Application.Quit();

        // O código abaixo é uma diretiva de pré-processamento.
        // Ele só será compilado e executado se o projeto estiver rodando no Editor da Unity.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}