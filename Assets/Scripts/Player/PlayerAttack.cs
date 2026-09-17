using UnityEngine;
using TMPro;

public class PlayerAttack : MonoBehaviour
{
    [Header("Configuração de Ataque")]
    public float tempoEntreAtaques = 1f;

    [Header("Projétil")]
    [Tooltip("Prefab da flecha. Se vazio, o ataque é instantâneo (hitscan).")]
    public GameObject flechaPrefab;
    [Tooltip("Velocidade da flecha (requisito do PDF: valor facilmente alterável).")]
    public float velocidadeDaFlecha = 25f;

    [Header("Recursos")]
    public int flechas = 10;

    // A referência ao texto da UI
    private TextMeshProUGUI textoFlechas;

    // Outras referências e variáveis de estado
    private LoopMover loopMover;
    private float tempoProximoAtaque = 0f;
    private bool emCombate = false;
    private InimigoBase inimigoDetectado;
    private PlayerStatus playerStatus;

    void Awake()
    {
        playerStatus = GetComponent<PlayerStatus>();
        loopMover = GetComponent<LoopMover>();
    }

    void Start()
    {
        // Encontra o objeto de texto da UI pela tag.
        GameObject textoUIObj = GameObject.FindGameObjectWithTag("UI_ContadorFlechas");
        if (textoUIObj != null)
        {
            textoFlechas = textoUIObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning("AVISO: Não foi encontrado um objeto de texto com a tag 'UI_ContadorFlechas' na cena. A UI de flechas não funcionará.");
        }

        // Atualiza a UI com o valor inicial
        AtualizarUIFlechas();
    }

    void Update()
    {
        if (emCombate && inimigoDetectado != null && Time.time >= tempoProximoAtaque)
        {
            TentarAtacar();
        }
    }

    private void TentarAtacar()
    {
        if (inimigoDetectado == null) { EncerrarCombate(); return; }
        if (flechas <= 0) { if (loopMover != null) loopMover.ResumirMovimento(); return; }

        flechas--;
        AtualizarUIFlechas();

        float danoPlayer = playerStatus.GetDano();
        Vector3 direcao = (inimigoDetectado.transform.position - transform.position);
        direcao.y = 0;

        if (flechaPrefab != null)
        {
            // Dispara um projétil físico com velocidade configurável
            GameObject flecha = Instantiate(flechaPrefab, transform.position + Vector3.up * 1f + direcao.normalized * 0.5f, Quaternion.identity);
            Flecha componenteFlecha = flecha.GetComponent<Flecha>();
            if (componenteFlecha != null)
            {
                componenteFlecha.Disparar(direcao, danoPlayer, velocidadeDaFlecha);
            }
        }
        else
        {
            // Fallback: dano instantâneo
            inimigoDetectado.ReceberDano(danoPlayer);
            Debug.DrawLine(transform.position, inimigoDetectado.transform.position, Color.red, 0.5f);
        }

        tempoProximoAtaque = Time.time + tempoEntreAtaques;
    }

    /// <summary>
    /// Atualiza o texto de flechas no Canvas.
    /// </summary>
    private void AtualizarUIFlechas()
    {
        if (textoFlechas != null)
        {
            textoFlechas.text = "Arrows: " + flechas;
        }
    }

    public void AdicionarFlechas(int quantidade)
    {
        flechas += quantidade;
        AtualizarUIFlechas();
    }

    public bool PodeAtacar() { return flechas > 0; }
    public void IniciarCombate(InimigoBase inimigoAlvo) { if (!emCombate) { emCombate = true; inimigoDetectado = inimigoAlvo; tempoProximoAtaque = Time.time; } }
    public void EncerrarCombate() { emCombate = false; inimigoDetectado = null; }
}
