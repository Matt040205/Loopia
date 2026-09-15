using UnityEngine;

public class PlataformaLar : MonoBehaviour
{
    [Header("Configuração da Recompensa")]
    [Tooltip("Quantidade de flechas a serem restauradas a cada volta.")]
    public int flechasRestauradas = 10;

    private bool playerEstaNaPlataforma = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !playerEstaNaPlataforma)
        {
            playerEstaNaPlataforma = true;
            GameManager.Instance.AvancarLoop();
            other.GetComponent<PlayerAttack>()?.AdicionarFlechas(flechasRestauradas);
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