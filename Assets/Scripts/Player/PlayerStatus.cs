using UnityEngine;
using UnityEngine.Events;

public class PlayerStatus : MonoBehaviour
{
    [Header("Nível e XP")]
    public int nivel = 1;
    public float xpAtual = 0f;
    public float xpParaProximoNivelBase = 100f;
    public float fatorCrescimentoXP = 1.5f;

    [Header("Atributos Base")]
    public float vidaMaxBase = 100f;
    public float danoBase = 20f;

    private float vidaAtual;

    [Header("Eventos")]
    public UnityEvent<float> onTakeDamage;
    public UnityEvent onDeath;
    public UnityEvent<int> onLevelUp;

    void Awake()
    {
        vidaAtual = GetVidaMaxima();
    }

    void Start()
    {
        vidaAtual = GetVidaMaxima();
        gameObject.layer = LayerMask.NameToLayer("Gameplay");
    }

    public float GetVidaMaxima() => vidaMaxBase * nivel;
    public float GetDano() => danoBase * nivel;
    public float GetXPParaProximoNivel() => xpParaProximoNivelBase * Mathf.Pow(fatorCrescimentoXP, nivel - 1);

    public void ReceberDano(float valor)
    {
        // DEBUG: Confirma que este método foi chamado.
        Debug.Log($"<color=orange>[PLAYERSTATUS - DEBUG] Método ReceberDano foi chamado! Dano bruto: {valor}.</color>");

        vidaAtual -= valor;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, GetVidaMaxima());

        // DEBUG: Mostra a vida após o cálculo.
        Debug.Log($"<color=orange>[PLAYERSTATUS - DEBUG] Vida calculada: {vidaAtual}/{GetVidaMaxima()}.</color>");

        // DEBUG: Confirma que o evento está prestes a ser disparado.
        Debug.Log("<color=orange>[PLAYERSTATUS - DEBUG] Disparando evento onTakeDamage...</color>");
        onTakeDamage?.Invoke(valor);

        if (vidaAtual <= 0f)
        {
            Morrer();
        }
    }

    public void ReceberCura(float valor)
    {
        vidaAtual += valor;
        vidaAtual = Mathf.Clamp(vidaAtual, 0f, GetVidaMaxima());
    }

    public void GanharXP(float valor)
    {
        xpAtual += valor;
        while (xpAtual >= GetXPParaProximoNivel())
        {
            xpAtual -= GetXPParaProximoNivel();
            NivelarUp();
        }
    }

    private void NivelarUp()
    {
        nivel++;
        vidaAtual = GetVidaMaxima();
        onLevelUp?.Invoke(nivel);
    }

    private void Morrer()
    {
        onDeath?.Invoke();
        gameObject.SetActive(false);
    }

    public float GetPorcentagemVida()
    {
        return vidaAtual / GetVidaMaxima();
    }
}