using UnityEngine;
using System.Collections;

public class PlataformaFlecha : MonoBehaviour
{
    [Header("Configuração da Recompensa")]
    [Tooltip("A quantidade de flechas que esta plataforma dará ao player.")]
    public int quantidadeDeFlechas = 5;

    private bool podeAtivar = false;

    void Start()
    {
        // Inicia uma rotina para "ativar" a plataforma após um pequeno delay,
        // garantindo que ela não seja acionada no mesmo instante em que é criada.
        StartCoroutine(AtivarPlataformaAposDelay(0.2f));
    }

    private IEnumerator AtivarPlataformaAposDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        podeAtivar = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Só funciona se estiver ativa E se for o player
        if (podeAtivar && other.CompareTag("Player"))
        {
            PlayerAttack playerAttack = other.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                // --- CORREÇÃO AQUI ---
                // Chamamos o método direto para adicionar flechas, que já existe no PlayerAttack.
                playerAttack.AdicionarFlechas(quantidadeDeFlechas);

                Debug.Log($"<color=green>[PlataformaFlecha - SUCESSO]</color> Sinal enviado ao Player para coletar {quantidadeDeFlechas} flechas!");

                // A plataforma se destrói após dar a recompensa.
                Destroy(gameObject);
            }
        }
    }
}