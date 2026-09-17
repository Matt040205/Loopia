using TMPro;
using UnityEngine;

public class UIMoedas : MonoBehaviour
{
    private TextMeshProUGUI texto;
    private PlayerStatus playerStatus;

    void Start()
    {
        texto = GetComponent<TextMeshProUGUI>();

        playerStatus = FindFirstObjectByType<PlayerStatus>();
        if (playerStatus != null)
        {
            playerStatus.onMoedasAlteradas.AddListener(AtualizarMoedas);
            AtualizarMoedas(playerStatus.moedas);
        }
    }

    void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.onMoedasAlteradas.RemoveListener(AtualizarMoedas);
        }
    }

    public void AtualizarMoedas(int valor)
    {
        if (texto != null)
        {
            texto.text = "Moedas: " + valor;
        }
    }
}
