using UnityEngine;
using System.Collections;

public class PlataformaFlecha : MonoBehaviour
{
    [Header("Configura��o da Recompensa")]
    [Tooltip("A quantidade de flechas que esta plataforma dar� ao player.")]
    public int quantidadeDeFlechas = 5;
    [Tooltip("Se true, a recompensa vem em dobro (efeito da carta Vazio).")]
    public bool recompensaDobrada = false;

    private bool podeAtivar = false;

    void Start()
    {
        // Inicia uma rotina para "ativar" a plataforma ap�s um pequeno delay,
        // garantindo que ela n�o seja acionada no mesmo instante em que � criada.
        StartCoroutine(AtivarPlataformaAposDelay(0.2f));
    }

    private IEnumerator AtivarPlataformaAposDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        podeAtivar = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // S� funciona se estiver ativa E se for o player
        if (podeAtivar && other.CompareTag("Player"))
        {
            PlayerAttack playerAttack = other.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                // --- CORRE��O AQUI ---
                // Chamamos o m�todo direto para adicionar flechas, que j� existe no PlayerAttack.
                playerAttack.AdicionarFlechas(quantidadeDeFlechas * (recompensaDobrada ? 2 : 1));

                Debug.Log($"<color=green>[PlataformaFlecha - SUCESSO]</color> Sinal enviado ao Player para coletar {quantidadeDeFlechas} flechas!");

                // A plataforma se destr�i ap�s dar a recompensa.
                Destroy(gameObject);
            }
        }
    }
}