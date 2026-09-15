using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILevelUp : MonoBehaviour
{
    public TextMeshProUGUI textoNivel;

    public void AtualizarNivel(int novoNivel)
    {
        if (textoNivel != null)
            textoNivel.text = "Lvl: " + novoNivel;
    }
}