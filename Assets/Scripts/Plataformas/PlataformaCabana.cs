using UnityEngine;

public class PlataformaCabana : MonoBehaviour
{
    [Header("Configuração do Efeito")]
    [Tooltip("Vida máxima permanente concedida ao construir a cabana.")]
    public float vidaPermanente = 2f;
    [Tooltip("Vida permanente EXTRA por campo pacífico na área 3x3 da cabana.")]
    public float vidaExtraPorCampoPacifico = 2f;

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
        if (playerStatus == null) return;

        float total = vidaPermanente;

        LoopGenerator gerador = FindFirstObjectByType<LoopGenerator>();
        if (gerador != null)
        {
            Vector2Int hex = gerador.WorldToHex(transform.position);
            int camposPacificos = GridAdjacencySystem.ContarTipoNaArea<PlataformaCampoPacifico>(gerador, hex);
            if (camposPacificos > 0)
            {
                total += camposPacificos * vidaExtraPorCampoPacifico;
                Debug.Log($"<color=green>[PlataformaCabana] {camposPacificos} campo(s) pacífico(s) na área: +{camposPacificos * vidaExtraPorCampoPacifico:F0} PV extra.</color>");
            }
        }

        playerStatus.AdicionarVidaPermanente(total);
        Debug.Log($"<color=green>[PlataformaCabana] +{total:F0} PV permanente no total.</color>");
    }
}
