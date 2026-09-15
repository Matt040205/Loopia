using UnityEngine;
using UnityEngine.UI;

public class UIPlayerHealth : MonoBehaviour
{
    // A referência agora é privada, o script vai encontrá-la sozinho via Tag.
    private Image barraVidaImagem;

    [Header("Sprites de Vida")]
    [Tooltip("Arraste aqui as 10 sprites da barra de vida, na ordem de vida CHEIA para VAZIA.")]
    public Sprite[] vidaSprites = new Sprite[10];

    void Start()
    {
        // Encontra o objeto da barra de vida na cena que tem a nossa tag customizada.
        GameObject barraVidaObj = GameObject.FindGameObjectWithTag("UI_BarraVida");
        if (barraVidaObj != null)
        {
            barraVidaImagem = barraVidaObj.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("AVISO: Não foi encontrado um objeto com a tag 'UI_BarraVida' na cena. A UI de vida não funcionará.");
        }
    }

    public void AtualizarVida(float danoRecebido)
    {
        if (barraVidaImagem == null) return; // Se a referência não foi encontrada, não faz nada.

        // MUDANÇA: Usando o método moderno FindFirstObjectByType
        PlayerStatus player = FindFirstObjectByType<PlayerStatus>();

        if (player != null && vidaSprites.Length == 10)
        {
            float porcentagemVida = player.GetPorcentagemVida();
            int indiceSprite;
            if (porcentagemVida >= 1.0f)
            {
                indiceSprite = 0;
            }
            else
            {
                indiceSprite = 9 - Mathf.FloorToInt(porcentagemVida * 9);
            }
            indiceSprite = Mathf.Clamp(indiceSprite, 0, vidaSprites.Length - 1);
            barraVidaImagem.sprite = vidaSprites[indiceSprite];
        }
    }
}