using UnityEngine;
using System.Collections;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance;
    private GameObject plataformaSelecionada;
    private UIPlatformCard cartaSelecionada;

    void Awake() { Instance = this; }

    public void SelecionarPlataforma(GameObject prefab, UIPlatformCard cartaVisual)
    {
        plataformaSelecionada = prefab;
        cartaSelecionada = cartaVisual;
    }

    public void TentarConstruirNaPlataforma(PlataformaAlvo alvo)
    {
        // --- NOVA VERIFICA��O DE SEGURAN�A ---
        // Se o alvo tem o componente PlataformaLar, exibe uma mensagem e n�o faz nada.
        if (alvo.GetComponent<PlataformaLar>() != null)
        {
            Debug.Log("A Plataforma Lar � insubstitu�vel!");
            return;
        }

        if (plataformaSelecionada != null && alvo != null)
        {
            bool ehCartaVazio = cartaSelecionada != null && cartaSelecionada.name == "UI_Vazio";
            Vector3 posAlvo = alvo.transform.position;

            StartCoroutine(SubstituirPlataforma(alvo, plataformaSelecionada));
            if (cartaSelecionada != null)
            {
                cartaSelecionada.UsarCarta();
                cartaSelecionada = null;
            }
            plataformaSelecionada = null;

            if (ehCartaVazio)
            {
                AplicarEfeitoVazio(posAlvo);
            }
        }
    }

    // Carta Vazio: remove a plataforma complexa (vira base) e dobra a recompensa da proxima plataforma do caminho
    private void AplicarEfeitoVazio(Vector3 posAlvo)
    {
        LoopGenerator gerador = FindFirstObjectByType<LoopGenerator>();
        if (gerador == null) return;

        Vector2Int hex = gerador.WorldToHex(posAlvo);
        if (!gerador.EhHexDoCaminho(hex.x, hex.y)) return;

        int idx = gerador.GetIndexPorPosicao(posAlvo);
        GameObject seguinte = gerador.GetPlataformaEm(gerador.GetWorldPositionByIndex(idx + 1));
        if (seguinte == null) return;

        PlataformaFlecha flecha = seguinte.GetComponent<PlataformaFlecha>();
        if (flecha != null) { flecha.recompensaDobrada = true; Debug.Log("[PlacementManager] Efeito Vazio: proxima recompensa de flechas DOBRADA."); return; }

        PlataformaCoracao coracao = seguinte.GetComponent<PlataformaCoracao>();
        if (coracao != null) { coracao.recompensaDobrada = true; Debug.Log("[PlacementManager] Efeito Vazio: proxima cura DOBRADA."); return; }

        PlataformaMoeda moeda = seguinte.GetComponent<PlataformaMoeda>();
        if (moeda != null) { moeda.recompensaDobrada = true; Debug.Log("[PlacementManager] Efeito Vazio: proximas moedas DOBRADAS."); }
    }

    private IEnumerator SubstituirPlataforma(PlataformaAlvo plataformaAntiga, GameObject prefabNovaPlataforma)
    {
        Vector3 pos = plataformaAntiga.transform.position;
        Quaternion rot = plataformaAntiga.transform.rotation;
        Transform parent = plataformaAntiga.transform.parent;
        Destroy(plataformaAntiga.gameObject);
        yield return new WaitForEndOfFrame();
        GameObject novaPlataforma = Instantiate(prefabNovaPlataforma, pos, rot, parent);

        LoopGenerator gerador = FindFirstObjectByType<LoopGenerator>();
        if (gerador != null)
        {
            gerador.UpdatePlatformReference(pos, novaPlataforma);
        }
    }
}