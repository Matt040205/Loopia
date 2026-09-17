using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILevelUp : MonoBehaviour
{
    public TextMeshProUGUI textoNivel;

    private PlayerStatus playerStatus;

    void Start()
    {
        // Conecta automaticamente o evento de level-up do player
        playerStatus = FindFirstObjectByType<PlayerStatus>();
        if (playerStatus != null)
        {
            playerStatus.onLevelUp.AddListener(AtualizarNivel);
            AtualizarNivel(playerStatus.nivel);
        }
    }

    void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.onLevelUp.RemoveListener(AtualizarNivel);
        }
    }

    public void AtualizarNivel(int novoNivel)
    {
        if (textoNivel != null)
            textoNivel.text = "Lvl: " + novoNivel;
    }
}