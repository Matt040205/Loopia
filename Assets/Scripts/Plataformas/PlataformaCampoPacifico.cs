using UnityEngine;

public class PlataformaCampoPacifico : MonoBehaviour
{
    [Header("Configuração do Efeito")]
    [Tooltip("Vida máxima permanente concedida ao personagem quando construída.")]
    public float vidaPermanente = 2f;

    private bool aplicado = false;

    void Start()
    {
        AplicarEfeito();
    }

    private void AplicarEfeito()
    {
        if (aplicado) return;
        aplicado = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        PlayerStatus playerStatus = playerObj.GetComponent<PlayerStatus>();
        if (playerStatus != null)
        {
            playerStatus.AdicionarVidaPermanente(vidaPermanente);
            Debug.Log($"<color=green>[PlataformaCampoPacifico] +{vidaPermanente:F0} PV permanente.</color>");
        }
    }
}
