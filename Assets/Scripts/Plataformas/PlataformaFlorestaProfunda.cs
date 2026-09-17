using UnityEngine;

public class PlataformaFlorestaProfunda : MonoBehaviour
{
    [Header("Configuração do Efeito")]
    [Tooltip("Multiplicador de vida dos lobos enquanto o efeito estiver ativo (0.85 = -15%).")]
    public float multiplicadorDeVidaDosLobos = 0.85f;

    // Debuff global aplicado aos lobos spawnados enquanto houver floresta profunda ativa
    public static float MultiplicadorGlobalDeVidaDosLobos = 1f;

    void Start()
    {
        RecalcularGlobal();
    }

    void OnDestroy()
    {
        RecalcularGlobal();
    }

    // O efeito fica ativo se QUALQUER floresta profunda tiver uma floresta (acampamento) na área 3x3
    public static void RecalcularGlobal()
    {
        float novo = 1f;

        LoopGenerator gerador = Object.FindFirstObjectByType<LoopGenerator>();
        if (gerador != null)
        {
            foreach (var floresta in Object.FindObjectsByType<PlataformaFlorestaProfunda>(FindObjectsSortMode.None))
            {
                Vector2Int hex = gerador.WorldToHex(floresta.transform.position);
                if (GridAdjacencySystem.ContarTipoNaArea<PlataformaCampoBarbaro>(gerador, hex) > 0)
                {
                    novo = floresta.multiplicadorDeVidaDosLobos;
                    break;
                }
            }
        }

        MultiplicadorGlobalDeVidaDosLobos = novo;
    }
}
