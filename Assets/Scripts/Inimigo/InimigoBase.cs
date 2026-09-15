using UnityEngine;
using Random = UnityEngine.Random;

public class InimigoBase : MonoBehaviour
{
    [Header("Atributos Base (Nível 1, Loop 1)")]
    public float vidaBase = 50f;
    public float danoBase = 10f;
    public float xpBase = 20f;
    [Header("Configurações Gerais")]
    public int nivel = 1;
    public float tempoEntreAtaques = 2f;
    public float raioDeDeteccao = 5f;
    [Header("Cartas Recompensa")]
    public int numCartasMin = 1;
    public int numCartasMax = 2;

    private float vidaAtual, danoAtual, xpAtual;
    private UICardManager uiCardManager;
    private bool emCombate = false;
    private float proximoAtaque = 0f;
    private PlayerStatus alvo;

    void Awake()
    {
        float multiplicador = 1.0f;
        if (GameManager.Instance != null)
        {
            multiplicador = GameManager.Instance.multiplicadorDeStatus;
        }
        vidaAtual = vidaBase * nivel * multiplicador;
        danoAtual = danoBase * nivel * multiplicador;
        xpAtual = xpBase * nivel * multiplicador;
    }

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Gameplay");
        // MUDANÇA AQUI: Usando o método moderno FindFirstObjectByType
        uiCardManager = FindFirstObjectByType<UICardManager>();
    }

    void Update()
    {
        if (!emCombate) { ProcurarPlayer(); }
        else if (emCombate && Time.time >= proximoAtaque) { Atacar(); }
    }

    private void Atacar()
    {
        if (alvo == null) { emCombate = false; return; }
        alvo.ReceberDano(danoAtual);
        proximoAtaque = Time.time + tempoEntreAtaques;
    }

    private void ProcurarPlayer() { Collider[] collidersNaArea = Physics.OverlapSphere(transform.position, raioDeDeteccao); foreach (Collider col in collidersNaArea) { if (col.CompareTag("Player")) { IniciarCombate(col.GetComponent<PlayerStatus>()); return; } } }
    public void IniciarCombate(PlayerStatus playerAlvo) { if (emCombate || playerAlvo == null) return; alvo = playerAlvo; emCombate = true; proximoAtaque = Time.time + 1f; }
    public void ReceberDano(float valor) { vidaAtual -= valor; if (vidaAtual <= 0f) Morrer(); }

    void Morrer()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.GetComponent<PlayerStatus>()?.GanharXP(xpAtual);
            playerObj.GetComponent<LoopMover>()?.ResumirMovimento();
        }
        if (uiCardManager != null)
        {
            int qtd = Random.Range(numCartasMin, numCartasMax + 1);
            for (int i = 0; i < qtd; i++) uiCardManager.AdicionarCartaAleatoria();
        }
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, raioDeDeteccao); }
}