using UnityEngine;

/// <summary>
/// Sincroniza a opacidade do material DappledLight com a intensidade atual do sol
/// (via DayNightController), sem precisar de nenhuma referência cruzada manual
/// além desse componente. Fica 100% auto-contido: coloque esse componente no
/// mesmo objeto (o "plano de sombra") em qualquer prefab de ilha, e ele encontra
/// o DayNightController sozinho e se ajusta sozinho, esteja a ilha onde estiver.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class DappledLightSync : MonoBehaviour
{
    [Tooltip("O DayNightController da cena. Se deixar vazio, o script procura um sozinho automaticamente.")]
    public DayNightController dayNightController;

    [Range(0f, 1f)]
    [Tooltip("Opacidade máxima do efeito, atingida quando o sol está no pico (meio-dia).")]
    public float maxOpacity = 0.6f;

    [Tooltip("Valor de intensidade do sol considerado 'pico' (deve bater com o valor máximo configurado no Sun Intensity Curve do DayNightController, ex: 1.4).")]
    public float peakSunIntensity = 1.4f;

    Renderer rend;
    MaterialPropertyBlock mpb;
    static readonly int OpacityID = Shader.PropertyToID("_Opacity");

    void OnEnable()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (dayNightController == null)
        {
#if UNITY_2023_1_OR_NEWER
            dayNightController = FindFirstObjectByType<DayNightController>();
#else
            dayNightController = FindObjectOfType<DayNightController>();
#endif
        }

        if (dayNightController == null || dayNightController.sunLight == null || rend == null)
            return;

        // Normaliza a intensidade atual do sol em relação ao pico (0 = noite, 1 = meio-dia)
        float t = Mathf.Clamp01(dayNightController.sunLight.intensity / Mathf.Max(0.01f, peakSunIntensity));
        float opacity = t * maxOpacity;

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(OpacityID, opacity);
        rend.SetPropertyBlock(mpb);
    }
}