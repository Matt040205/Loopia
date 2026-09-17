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

    [Header("Voo")]
    [Tooltip("Se true, o inimigo voa pairando acima do chão e oscilando.")]
    public bool voa = false;
    [Tooltip("Altura do voo em relação ao ponto de spawn.")]
    public float alturaDeVoo = 2.5f;
    [Tooltip("Amplitude da oscilação vertical do voo.")]
    public float amplitudeDeOscilacao = 0.4f;
    [Tooltip("Velocidade da oscilação vertical do voo.")]
    public float velocidadeDeOscilacao = 2f;

    [Header("Investida")]
    [Tooltip("Se true, o inimigo dá um bote rápido até o player ao atacar.")]
    public bool fazInvestida = true;
    [Tooltip("Duração (em segundos) de cada trecho da investida (ida e volta).")]
    public float duracaoInvestida = 0.3f;

    [Header("Boss")]
    [Tooltip("Se true, exibe a barra de vida de boss acima do inimigo.")]
    public bool ehBoss = false;
    [Tooltip("Fill (quad) da barra de vida do boss, filho deste objeto.")]
    public Transform barraVidaFill;

    [Header("Cartas Recompensa")]
    public int numCartasMin = 1;
    public int numCartasMax = 2;

    private float vidaAtual, danoAtual, xpAtual, vidaMaxInicial;
    private float baseY;
    private UICardManager uiCardManager;
    private bool emCombate = false;
    private bool emInvestida = false;
    private float proximoAtaque = 0f;
    private PlayerStatus alvo;

    void Awake()
    {
        float multiplicador = 1.0f;
        if (GameManager.Instance != null)
        {
            multiplicador = GameManager.Instance.multiplicadorDeStatus;
        }
        vidaMaxInicial = vidaBase * nivel * multiplicador;
        vidaAtual = vidaMaxInicial;
        danoAtual = danoBase * nivel * multiplicador;
        xpAtual = xpBase * nivel * multiplicador;
    }

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Gameplay");
        uiCardManager = FindFirstObjectByType<UICardManager>();

        if (voa)
        {
            baseY = transform.position.y + alturaDeVoo;
            transform.position = new Vector3(transform.position.x, baseY, transform.position.z);
        }
        else
        {
            baseY = transform.position.y;
        }

        if (ehBoss && barraVidaFill == null)
        {
            barraVidaFill = transform.Find("BarraVidaFill");
        }
    }

    void Update()
    {
        if (voa && !emInvestida)
        {
            float y = baseY + Mathf.Sin(Time.time * velocidadeDeOscilacao) * amplitudeDeOscilacao;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        if (!emCombate)
        {
            ProcurarPlayer();
        }
        else if (Time.time >= proximoAtaque)
        {
            Atacar();
        }

        if (ehBoss && barraVidaFill != null && vidaMaxInicial > 0f)
        {
            float porcentagem = Mathf.Clamp01(vidaAtual / vidaMaxInicial);
            barraVidaFill.localScale = new Vector3(Mathf.Max(0.01f, porcentagem), barraVidaFill.localScale.y, barraVidaFill.localScale.z);
        }
    }

    private void Atacar()
    {
        if (alvo == null) { emCombate = false; return; }

        alvo.ReceberDano(danoAtual);
        proximoAtaque = Time.time + tempoEntreAtaques;

        if (fazInvestida && !emInvestida)
        {
            StartCoroutine(RotinaDeInvestida());
        }
    }

    // Bote rápido até o player e retorno à posição original
    private System.Collections.IEnumerator RotinaDeInvestida()
    {
        emInvestida = true;
        Vector3 inicio = transform.position;
        Vector3 posAlvo = alvo != null ? alvo.transform.position + Vector3.up * 1f : inicio;

        float t = 0f;
        while (t < duracaoInvestida)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(inicio, posAlvo, Mathf.Clamp01(t / duracaoInvestida));
            yield return null;
        }

        Vector3 voltaInicio = transform.position;
        t = 0f;
        while (t < duracaoInvestida)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(voltaInicio, inicio, Mathf.Clamp01(t / duracaoInvestida));
            yield return null;
        }

        if (voa)
        {
            transform.position = new Vector3(transform.position.x, baseY, transform.position.z);
        }
        emInvestida = false;
    }

    private void ProcurarPlayer()
    {
        Collider[] collidersNaArea = Physics.OverlapSphere(transform.position, raioDeDeteccao);
        foreach (Collider col in collidersNaArea)
        {
            if (col.CompareTag("Player"))
            {
                IniciarCombate(col.GetComponent<PlayerStatus>());
                return;
            }
        }
    }

    public void IniciarCombate(PlayerStatus playerAlvo)
    {
        if (emCombate || playerAlvo == null) return;
        alvo = playerAlvo;
        emCombate = true;
        proximoAtaque = Time.time + 1f;
    }

    public void ReceberDano(float valor)
    {
        vidaAtual -= valor;
        if (vidaAtual <= 0f) Morrer();
    }

    // Multiplica os stats atuais (usado pelo BossManager)
    public void AplicarMultiplicadorExtra(float multiplicador)
    {
        vidaMaxInicial *= multiplicador;
        vidaAtual = vidaMaxInicial;
        danoAtual *= multiplicador;
        xpAtual *= multiplicador;
    }

    // Modifica apenas a vida (usado pelo debuff da floresta profunda nos lobos)
    public void AplicarModificadorDeVida(float multiplicador)
    {
        vidaMaxInicial *= multiplicador;
        vidaAtual = Mathf.Min(vidaAtual, vidaMaxInicial);
    }

    public float GetPorcentagemVida()
    {
        return vidaMaxInicial > 0f ? Mathf.Clamp01(vidaAtual / vidaMaxInicial) : 0f;
    }

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioDeDeteccao);
    }
}
