using UnityEngine;
using UnityEngine.UI; // Necessário para controlar o componente Button

// Garante que o objeto tenha um botão para funcionar
[RequireComponent(typeof(Button))]
public class UIPlatformCard : MonoBehaviour
{
    // Variáveis que serão configuradas pelo UICardManager quando a carta for criada
    [HideInInspector] public GameObject plataformaPrefab;
    [HideInInspector] public UICardManager cardManager;
    [HideInInspector] public string minhaDescricao; // <-- A VARIÁVEL QUE FALTAVA!

    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
        // Adiciona um "listener" para que o método AoSelecionarCarta seja chamado quando o botão for clicado
        btn.onClick.AddListener(AoSelecionarCarta);
    }

    /// <summary>
    /// Chamado quando o botão desta carta é clicado.
    /// </summary>
    public void AoSelecionarCarta()
    {
        // 1. Avisa o PlacementManager qual plataforma deve ser construída
        if (PlacementManager.Instance != null && plataformaPrefab != null)
        {
            PlacementManager.Instance.SelecionarPlataforma(plataformaPrefab, this);
        }

        // 2. Avisa o UICardManager para mostrar a descrição desta carta
        if (cardManager != null)
        {
            cardManager.MostrarDescricao(minhaDescricao);
        }
    }

    /// <summary>
    /// Este método é chamado pelo PlacementManager após a construção ser bem-sucedida.
    /// </summary>
    public void UsarCarta()
    {
        if (cardManager != null)
        {
            // Limpa a descrição quando a carta é usada
            cardManager.MostrarDescricao("");
            cardManager.RemoverCartaDaMao(this.gameObject);
        }

        // Destrói o próprio objeto da carta na UI
        Destroy(gameObject);
    }
}