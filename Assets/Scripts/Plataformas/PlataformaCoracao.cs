using System.Collections;
using UnityEngine;

public class PlataformaCoracao : MonoBehaviour
{
    [Header("Configuração da Recompensa")]
    [Tooltip("Quantidade de vida curada ao passar por ela.")]
    public float quantidadeDeVida = 25f;
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

        float cura = quantidadeDeVida * (recompensaDobrada ? 2 : 1);
        playerStatus.ReceberCura(cura);
        Debug.Log($"<color=green>[PlataformaCoracao] Player curou {cura:F0} de vida.</color>");

        Destroy(gameObject);
    }
}
