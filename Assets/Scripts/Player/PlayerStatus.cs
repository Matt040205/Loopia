using UnityEngine;
using UnityEngine.Events;

public class PlayerStatus : MonoBehaviour
{
    [Header("Nível e XP")]
    public int nivel = 1;
    public float xpAtual = 0f;
    public float xpParaProximoNivelBase = 100f;
    [Tooltip("Fator de crescimento do XP por nível: 1.75 = +75% a cada nível (conforme PDF). ")]
    public float fatorCrescimentoXP = 1.75f;

    [Header("Atributos Base")]
    public float vidaMaxBase = 100f;
    public float danoBase = 20f;

    [Header("Recursos")]
    [Tooltip("Moedas coletadas durante o jogo.")]
    public int moedas = 0;
    [Tooltip("Bônus permanente de vida máxima (campo pacífico, cabana...).")]
    public float vidaBonusPermanente = 0f;

    private float vidaAtual;

    [Header("Eventos")]
    public UnityEvent<float> onTakeDamage;
    public UnityEvent onDeath;
    public UnityEvent<int> onLevelUp;
    public UnityEvent<int> onMoedasAlteradas;

    void Awake()
    {
        vidaAtual = GetVidaMaxima();
    }

    void Start()
    {
        vidaAtual = GetVidaMaxima();
        gameObject.layer = LayerMask.NameToLayer("Gameplay");
    }

    public float GetVidaMaxima() => vidaMaxBase * nivel + vidaBonusPermanente;
    public float GetDano() => danoBase * nivel;
    public float GetXPParaProximoNivel() => xpParaProximoNivelBase * Mathf.Pow(fatorCrescimentoXP, nivel - 1);

    public void AdicionarMoedas(int quantidade)
    {
        moedas += quantidade;
        onMoedasAlteradas?.Invoke(moedas);
        Debug.Log($"<color=yellow>[PLAYERSTATUS] Moedas: {moedas}.</color>");
    }

    // Aumenta a vida máxima permanentemente e cura o mesmo valor
    public void AdicionarVidaPermanente(float valor)
    {
        vidaBonusPermanente += valor;
        vidaAtual = Mathf.Clamp(vidaAtual + valor, 0f, GetVidaMaxima());
        Debug.Log($"<color=green>[PLAYERSTATUS] Vida máxima permanente agora: {GetVidaMaxima():F0}.</color>");
    }

    public void ReceberDano(float valor)
    {
        // DEBUG: Confirma que este método foi chamado.
        Debug.Log($"<color=orange>[PLAYERSTATUS - DEBUG] Método ReceberDano foi chamado! Dano bruto: {valor}.</color>");

        vidaAtual -= valor;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, GetVidaMaxima());

        // DEBUG: Mostra a vida após o cálculo.
        Debug.Log($"<color=orange>[PLAYERSTATUS - DEBUG] Vida calculada: {vidaAtual}/{GetVidaMaxima()}.</color>");

        // DEBUG: Confirma que o evento está prestes a ser disparado.
        Debug.Log("<color=orange>[PLAYERSTATUS - DEBUG] Disparando evento onTakeDamage...</color>");
        onTakeDamage?.Invoke(valor);

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
    }

    public void ReceberCura(float valor)
    {
        vidaAtual += valor;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, GetVidaMaxima());
    }

    public void GanharXP(float valor)
    {
        xpAtual += valor;
        while (xpAtual >= GetXPParaProximoNivel())
        {
            xpAtual -= GetXPParaProximoNivel();
            NivelarUp();
        }
    }

    private void NivelarUp()
    {
        nivel++;
        vidaAtual = GetVidaMaxima();
        onLevelUp?.Invoke(nivel);
    }

    [Header("Gerenciamento de Cena")]
    public GameOverManager gameOverManager;

    private void Morrer()
    {
        Debug.Log("<color=red>[PLAYERSTATUS] Jogador morreu! Indo para a tela de Game Over...</color>");
        onDeath?.Invoke();
        gameObject.SetActive(false);

        // Vai para a tela GameOver (que possui botões de Menu/Reiniciar)
        if (gameOverManager == null)
        {
            gameOverManager = FindFirstObjectByType<GameOverManager>();
        }

        if (gameOverManager != null)
        {
            gameOverManager.IrParaGameOver();
        }
        else if (GameManager.Instance != null)
        {
            Debug.LogWarning("<color=orange>[PLAYERSTATUS] GameOverManager não encontrado, usando GameManager como fallback.</color>");
            GameManager.Instance.LoadMainMenu();
        }
    }

    /// <summary>
    /// Reseta o XP para um novo loop (conforme PDF: 'apenas no loop, não globalmente').
    /// O nível e seus buffs permanecem permanentes entre loops.
    /// </summary>
    public void ResetarParaNovoLoop()
    {
        xpAtual = 0f;
        Debug.Log("<color=cyan>[PLAYERSTATUS] XP resetado para novo loop. Nível " + nivel + " mantido como buff permanente.</color>");
    }

    public float GetPorcentagemVida()
    {
        return vidaAtual / GetVidaMaxima();
    }
}