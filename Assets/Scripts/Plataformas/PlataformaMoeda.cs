using System.Collections;
using UnityEngine;

public class PlataformaMoeda : MonoBehaviour
{
    [Header("Configuração da Recompensa")]
    [Tooltip("Quantidade de moedas dadas ao passar por ela.")]
    public int quantidadeDeMoedas = 5;
    [Tooltip("Se true, a recompensa vem em dobro (efeito da carta Vazio).")]
    public bool recompensaDobrada = false;

    private bool podeAtivar = false;

    void Start()
    {
        StartCoroutine(AtivarPlataformaAposDelay(0.2f));
    }

    private IEnumerator AtivarPlataformaAposDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        podeAtivar = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!podeAtivar || !other.CompareTag("Player")) return;

        PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
        if (playerStatus == null) return;

        int total = quantidadeDeMoedas * (recompensaDobrada ? 2 : 1);
        playerStatus.AdicionarMoedas(total);
        Debug.Log($"<color=yellow>[PlataformaMoeda] Player coletou {total} moedas.</color>");

        Destroy(gameObject);
    }
}
