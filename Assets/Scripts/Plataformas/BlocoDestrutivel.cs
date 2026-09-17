using UnityEngine;

public class BlocoDestrutivel : MonoBehaviour
{
    [Tooltip("Vida do bloco (dano por cabeçada = dano do player).")]
    public float vida = 30f;
    [Tooltip("Índice da posição deste bloco dentro da PlataformaBlocos dona.")]
    public int indice = 0;
    [HideInInspector] public PlataformaBlocos plataformaDona;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        // Cabeçada: o player encosta no bloco enquanto corre
        PlayerStatus playerStatus = collision.collider.GetComponent<PlayerStatus>();
        float dano = playerStatus != null ? playerStatus.GetDano() : 10f;
        ReceberDano(dano);
    }

    public void ReceberDano(float valor)
    {
        vida -= valor;
        if (vida <= 0f)
        {
            Destruir();
        }
    }

    private void Destruir()
    {
        if (plataformaDona != null)
        {
            plataformaDona.RegistrarBlocoDestruido(this);
        }
        DroparRecompensa();
        Destroy(gameObject);
    }

    // Blocos destruídos dropam corações, flechas ou moedas (sorteio)
    private void DroparRecompensa()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        int tipo = Random.Range(0, 3);
        if (tipo == 0)
        {
            player.GetComponent<PlayerStatus>()?.ReceberCura(10f);
            Debug.Log("<color=green>[BlocoDestrutivel] Drop: +10 de vida.</color>");
        }
        else if (tipo == 1)
        {
            player.GetComponent<PlayerAttack>()?.AdicionarFlechas(2);
            Debug.Log("<color=green>[BlocoDestrutivel] Drop: +2 flechas.</color>");
        }
        else
        {
            player.GetComponent<PlayerStatus>()?.AdicionarMoedas(3);
            Debug.Log("<color=yellow>[BlocoDestrutivel] Drop: +3 moedas.</color>");
        }
    }
}
