using UnityEngine;
using TMPro;

public class PlayerAttack : MonoBehaviour
{
    [Header("Configuração de Ataque")]
    public float tempoEntreAtaques = 1f;

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
        AtualizarUIFlechas(); // Chama a atualização da UI

        float danoPlayer = playerStatus.GetDano();
        inimigoDetectado.ReceberDano(danoPlayer);

        if (inimigoDetectado != null)
        {
            Debug.DrawLine(transform.position, inimigoDetectado.transform.position, Color.red, 0.5f);
        }

        tempoProximoAtaque = Time.time + tempoEntreAtaques;
    }

    /// <summary>
    /// Atualiza o texto de flechas no Canvas. Agora sem efeitos visuais.
    /// </summary>
    private void AtualizarUIFlechas()
    {
        if (textoFlechas != null)
        {
            // Apenas atualiza o texto diretamente.
            textoFlechas.text = "Arrows: " + flechas;
        }
    }

    public void AdicionarFlechas(int quantidade)
    {
        flechas += quantidade;
        AtualizarUIFlechas();
    }

    // O resto dos métodos de controle de combate permanecem os mesmos
    public bool PodeAtacar() { return flechas > 0; }
    public void IniciarCombate(InimigoBase inimigoAlvo) { if (!emCombate) { emCombate = true; inimigoDetectado = inimigoAlvo; tempoProximoAtaque = Time.time; } }
    public void EncerrarCombate() { emCombate = false; inimigoDetectado = null; }
}