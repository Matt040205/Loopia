using UnityEngine;

public class PlataformaLar : MonoBehaviour
{
    [Header("Configuração da Recompensa")]
    [Tooltip("Quantidade de flechas a serem restauradas a cada volta.")]
    public int flechasRestauradas = 10;
    [Tooltip("Fração da vida máxima a restaurar (1.0 = 100%).")]
    public float vidaRestaurada = 1.0f;

    private bool playerEstaNaPlataforma = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerEstaNaPlataforma)
        {
            playerEstaNaPlataforma = true;
            GameManager.Instance.AvancarLoop();

            // Restaurar flechas
            var playerAttack = other.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.AdicionarFlechas(flechasRestauradas);
            }

            // Restaurar vida (conforme PDF: barraca restaura vida e munição ao voltar)
            var playerStatus = other.GetComponent<PlayerStatus>();
            if (playerStatus != null)
            {
                float vidaMax = playerStatus.GetVidaMaxima();
                playerStatus.ReceberCura(vidaMax * vidaRestaurada);
                Debug.Log($"<color=green>[PlataformaLar] Vida em {playerStatus.GetPorcentagemVida() * 100f:F0}% de {vidaMax:F0}. Flechas +{flechasRestauradas}.</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerEstaNaPlataforma = false;
        }
    }
}
