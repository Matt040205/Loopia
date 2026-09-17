using UnityEngine;
using System.Collections;

public class PlataformaTrampolim : MonoBehaviour
{
    [Header("Configuração")]
    public LoopGenerator loopGenerator;

    [Header("Atributos do Pulo")]
    [Tooltip("Altura do arco do pulo (fácil de ajustar no Inspector).")]
    public float jumpArcHeight = 5f;
    [Tooltip("Velocidade do pulo (1 = ~1 segundo para completar o arco).")]
    public float jumpSpeed = 1.5f;

    private bool isUsed = false;

    void Start()
    {
        if (loopGenerator == null)
        {
            loopGenerator = FindFirstObjectByType<LoopGenerator>();
            if (loopGenerator == null)
            {
                Debug.LogError("ERRO no Trampolim: Não encontrou o 'LoopGenerator' na cena!", this.gameObject);
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isUsed)
        {
            isUsed = true;
            LoopMover playerMover = other.GetComponent<LoopMover>();
            if (playerMover != null && loopGenerator != null)
            {
                AcionarTrampolim(other.transform, playerMover);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { isUsed = false; }
    }

    private void AcionarTrampolim(Transform playerTransform, LoopMover playerMover)
    {
        int indiceAtual = loopGenerator.GetIndexPorPosicao(transform.position);
        if (indiceAtual < 0) { isUsed = false; return; }

        // Gap configurável no LoopGenerator (quantos passos do caminho pular)
        int passos = Mathf.Max(1, loopGenerator.gapSize);
        int indiceAlvo = (indiceAtual + passos) % loopGenerator.PathCount;
        Vector3 destino = loopGenerator.GetWorldPositionByIndex(indiceAlvo);
        playerMover.SetCurrentIndex(indiceAlvo);
        StartCoroutine(MoverPlayerEmArco(playerTransform, destino, playerMover));
    }

    private IEnumerator MoverPlayerEmArco(Transform playerTransform, Vector3 destino, LoopMover playerMover)
    {
        playerMover.PausarMovimento();
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null) { playerRb.isKinematic = true; }

        Vector3 inicio = playerTransform.position;
        Vector3 pontoDeControle = inicio + (destino - inicio) / 2 + Vector3.up * jumpArcHeight;
        float percentualCompleto = 0;
        while (percentualCompleto < 1.0f)
        {
            percentualCompleto += Time.deltaTime * jumpSpeed;
            Vector3 m1 = Vector3.Lerp(inicio, pontoDeControle, percentualCompleto);
            Vector3 m2 = Vector3.Lerp(pontoDeControle, destino, percentualCompleto);
            playerTransform.position = Vector3.Lerp(m1, m2, percentualCompleto);
            yield return null;
        }

        playerTransform.position = destino + new Vector3(0, playerMover.alturaDoSpawn, 0);
        if (playerRb != null) { playerRb.isKinematic = false; }
        playerMover.ResumirMovimento();
    }
}
