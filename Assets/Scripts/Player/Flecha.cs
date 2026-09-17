using UnityEngine;

public class Flecha : MonoBehaviour
{
    [HideInInspector] public float dano = 20f;
    [HideInInspector] public float velocidade = 20f;
    [Tooltip("Tempo máximo de vida da flecha em voo.")]
    public float tempoDeVida = 3f;

    private Vector3 direcao = Vector3.forward;

    public void Disparar(Vector3 direcaoDoTiro, float danoDaFlecha, float velocidadeDaFlecha)
    {
        direcao = direcaoDoTiro.normalized;
        if (direcao == Vector3.zero) direcao = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(direcao);
        dano = danoDaFlecha;
        velocidade = velocidadeDaFlecha;
    }

    void Update()
    {
        float passo = velocidade * Time.deltaTime;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direcao, out hit, passo + 0.1f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                InimigoBase inimigo = hit.collider.GetComponentInParent<InimigoBase>();
                if (inimigo != null)
                {
                    inimigo.ReceberDano(dano);
                }
            }
            Destroy(gameObject);
            return;
        }

        transform.position += direcao * passo;

        tempoDeVida -= Time.deltaTime;
        if (tempoDeVida <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
