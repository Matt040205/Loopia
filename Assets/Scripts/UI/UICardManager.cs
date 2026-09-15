using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

[System.Serializable]
public class CartaDisponivel
{
    public string nomeDaCarta;
    public GameObject plataformaPrefab;
    public GameObject cartaUIPrefab;
    [TextArea(3, 5)]
    public string descricao;
}

public class UICardManager : MonoBehaviour
{
    [Header("Baralho de Cartas")]
    public List<CartaDisponivel> baralhoDeCartas = new List<CartaDisponivel>();
    [Header("Slots de UI")]
    public List<Transform> cardSlots = new List<Transform>();

    // A referência agora é privada, o script vai encontrá-la sozinho.
    private TextMeshProUGUI campoDescricao;

    private GameObject[] cartasNosSlots;

    void Start()
    {
        cartasNosSlots = new GameObject[cardSlots.Count];

        // Procura pelo objeto de texto da descrição via Tag
        GameObject descObj = GameObject.FindGameObjectWithTag("UI_CampoDescricao");
        if (descObj != null)
        {
            campoDescricao = descObj.GetComponent<TextMeshProUGUI>();
            if (campoDescricao != null)
            {
                campoDescricao.text = "Select a card to see its description.";
                Debug.Log("<color=green>SUCESSO:</color> UI de Descrição encontrada e conectada via Tag!");
            }
        }
        else
        {
            Debug.LogWarning("AVISO: Não foi encontrado um objeto de texto com a tag 'UI_CampoDescricao'. As descrições não funcionarão.");
        }
    }

    public void MostrarDescricao(string texto)
    {
        if (campoDescricao != null)
        {
            campoDescricao.text = texto;
        }
    }

    // O resto dos seus métodos (AdicionarCartaAleatoria, etc.) permanece o mesmo.
    public void AdicionarCartaAleatoria() { bool maoCheia = cartasNosSlots.All(slot => slot != null); if (maoCheia) { Debug.Log("Mão de cartas está cheia. Acionando lógica de substituição."); } List<CartaDisponivel> cartasDisponiveis = baralhoDeCartas.Where(cartaDoBaralho => !cartasNosSlots.Any(cartaNoSlot => cartaNoSlot != null && cartaNoSlot.name == "UI_" + cartaDoBaralho.nomeDaCarta)).ToList(); if (cartasDisponiveis.Count == 0) { if (!maoCheia) { Debug.Log("Não há cartas novas disponíveis para adicionar."); } cartasDisponiveis = new List<CartaDisponivel>(baralhoDeCartas); } if (cartasDisponiveis.Count > 0) { int idx = Random.Range(0, cartasDisponiveis.Count); CartaDisponivel cartaSorteada = cartasDisponiveis[idx]; AdicionarCartaNaUI(cartaSorteada); } }
    private void AdicionarCartaNaUI(CartaDisponivel cartaParaAdicionar) { int slotIndex = -1; for (int i = 0; i < cartasNosSlots.Length; i++) { if (cartasNosSlots[i] == null) { slotIndex = i; break; } } if (slotIndex != -1) { InstanciarNovaCarta(cartaParaAdicionar, slotIndex); } else { Destroy(cartasNosSlots[cardSlots.Count - 1]); for (int i = cardSlots.Count - 1; i > 0; i--) { cartasNosSlots[i] = cartasNosSlots[i - 1]; if (cartasNosSlots[i] != null) { cartasNosSlots[i].transform.SetParent(cardSlots[i], false); cartasNosSlots[i].transform.localPosition = Vector3.zero; } } InstanciarNovaCarta(cartaParaAdicionar, 0); } }
    private void InstanciarNovaCarta(CartaDisponivel cartaData, int slotIndex) { Transform slotTransform = cardSlots[slotIndex]; GameObject novaCartaUI = Instantiate(cartaData.cartaUIPrefab, slotTransform, false); novaCartaUI.name = "UI_" + cartaData.nomeDaCarta; var cardScript = novaCartaUI.GetComponent<UIPlatformCard>(); if (cardScript != null) { cardScript.plataformaPrefab = cartaData.plataformaPrefab; cardScript.cardManager = this; cardScript.minhaDescricao = cartaData.descricao; } cartasNosSlots[slotIndex] = novaCartaUI; }
    public void RemoverCartaDaMao(GameObject cartaGameObject) { for (int i = 0; i < cartasNosSlots.Length; i++) { if (cartasNosSlots[i] == cartaGameObject) { cartasNosSlots[i] = null; if (campoDescricao != null) campoDescricao.text = ""; return; } } }
}