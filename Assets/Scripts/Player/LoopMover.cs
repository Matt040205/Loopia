using UnityEngine;
using System.Collections.Generic;

public class LoopMover : MonoBehaviour
{
    [Header("Configuração do Caminho")]
    public LoopGenerator loopGenerator;

    [Header("Atributos de Movimento")]
    public float moveSpeed = 3f;
    public float alturaDoSpawn = 1f;
    [Tooltip("A velocidade com que o personagem gira para encarar a nova direção.")]
    public float velocidadeDeRotacao = 10f; // <-- NOVA VARIÁVEL!

    [Header("Detecção de Inimigo")]
    [Tooltip("A que distância à frente o player detectará um inimigo.")]
    public float distanciaDeDeteccao = 3f;
    [Tooltip("A 'grossura' do raio de detecção. Use 0.5 para começar.")]
    public float raioDeDeteccaoPlayer = 0.5f;

    // Referências e estado interno
    private PlayerAttack playerAttack;
    private Rigidbody rb;
    private List<Vector3> path;
    private int currentIndex = 0;
    private bool podeMover = true;
    private Animator anim;

    void Awake()
    {
        playerAttack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (loopGenerator == null) { loopGenerator = FindFirstObjectByType<LoopGenerator>(); }
        if (loopGenerator == null) { Debug.LogError("ERRO CRÍTICO: LoopMover não encontrou um 'LoopGenerator'!"); this.enabled = false; return; }

        path = loopGenerator.GetPath();
        if (path != null && path.Count > 0)
        {
            transform.position = path[0] + new Vector3(0, alturaDoSpawn, 0);
            // Faz o player já começar olhando para o segundo ponto do caminho
            Vector3 direcaoInicial = (path[1] - path[0]).normalized;
            direcaoInicial.y = 0;
            if (direcaoInicial != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direcaoInicial);
            }
        }
    }

    void Update()
    {
        if (podeMover)
        {
            VerificarSeHaInimigoAFrente();
            Movimentar();
        }
    }

    private void Movimentar()
    {
        if (path == null || path.Count == 0) return;

        Vector3 targetNoCaminho = path[currentIndex];
        Vector3 targetParaMovimento = new Vector3(targetNoCaminho.x, transform.position.y, targetNoCaminho.z);

        // --- LÓGICA DE ROTAÇÃO ADICIONADA ---
        // Calcula a direção para o alvo, ignorando a altura
        Vector3 direcao = targetParaMovimento - transform.position;
        direcao.y = 0;

        // Se houver uma direção para olhar (evita erros quando já chegou ao destino)
        if (direcao != Vector3.zero)
        {
            // Cria a rotação que "olha" para a direção do movimento
            Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
            // Interpola suavemente da rotação atual para a rotação alvo
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeDeRotacao * Time.deltaTime);
        }

        // Move o player para frente
        transform.position = Vector3.MoveTowards(transform.position, targetParaMovimento, moveSpeed * Time.deltaTime);

        if (anim != null) anim.SetBool("correr", true);

        Vector3 playerPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPosXZ = new Vector3(targetNoCaminho.x, 0, targetNoCaminho.z);
        if (Vector3.Distance(playerPosXZ, targetPosXZ) < 0.01f)
        {
            currentIndex = (currentIndex + 1) % path.Count;
        }
    }

    private void VerificarSeHaInimigoAFrente()
    {
        if (path == null || path.Count == 0) return;
        Vector3 direcao = (path[currentIndex] - transform.position).normalized;
        direcao.y = 0;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, raioDeDeteccaoPlayer, direcao, out hit, distanciaDeDeteccao))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                if (playerAttack != null && playerAttack.PodeAtacar())
                {
                    PausarMovimento();
                    playerAttack.IniciarCombate(hit.collider.GetComponent<InimigoBase>());
                }
            }
        }
    }

    // O resto dos seus métodos permanece igual
    public void PausarMovimento() { podeMover = false; if (rb != null) { rb.constraints = RigidbodyConstraints.FreezeAll; } if (anim != null) { anim.SetBool("correr", false); } }
    public void ResumirMovimento() { podeMover = true; if (rb != null) { rb.constraints = RigidbodyConstraints.None; } if (anim != null) { anim.SetBool("correr", true); } if (playerAttack != null) { playerAttack.EncerrarCombate(); } }
    public void SetCurrentIndex(int newIndex) { if (path != null && newIndex >= 0 && newIndex < path.Count) { this.currentIndex = newIndex; } }
    void OnDrawGizmosSelected() { if (!Application.isPlaying || path == null || path.Count == 0) return; Vector3 direcao = (path[currentIndex] - transform.position).normalized; direcao.y = 0; Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position + direcao * distanciaDeDeteccao, raioDeDeteccaoPlayer); }
}