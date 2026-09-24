using UnityEngine;

/// <summary>
/// Controla uma nuvem individual: deriva lentamente (efeito de vento),
/// balança suavemente pra cima/baixo (efeito de "boiar"), e sincroniza
/// cor/opacidade com o DayNightController — mais quente no entardecer,
/// mais discreta de noite. Auto-contido: só precisa desse componente
/// no objeto da nuvem, ele encontra o DayNightController sozinho.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CloudController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Direção da deriva (efeito de vento). Não precisa ser normalizado.")]
    public Vector3 driftDirection = Vector3.right;

    [Tooltip("Velocidade da deriva, em unidades por segundo.")]
    public float driftSpeed = 0.3f;

    [Tooltip("Amplitude do balanço vertical (boiar suavemente).")]
    public float bobAmplitude = 0.15f;

    [Tooltip("Velocidade do balanço vertical.")]
    public float bobSpeed = 0.5f;

    [Tooltip("Se marcado, a nuvem 'teleporta' de volta pro início depois de percorrer Wrap Distance, criando um loop infinito e discreto.")]
    public bool wrapAround = true;

    [Tooltip("Distância (na direção da deriva) até a nuvem reiniciar o ciclo.")]
    public float wrapDistance = 40f;

    [Header("Sincronia dia/noite")]
    [Tooltip("O DayNightController da cena. Se deixar vazio, o script procura um sozinho.")]
    public DayNightController dayNightController;

    [Range(0f, 1f)]
    [Tooltip("Opacidade mínima da nuvem, atingida de noite.")]
    public float minOpacityAtNight = 0.15f;

    [Range(0f, 1f)]
    [Tooltip("Opacidade máxima da nuvem, atingida no pico do dia.")]
    public float maxOpacityAtDay = 0.85f;

    [Tooltip("Valor de intensidade do sol considerado 'pico' (deve bater com o Sun Intensity Curve do DayNightController, ex: 1.4).")]
    public float peakSunIntensity = 1.4f;

    [Tooltip("Se marcado, a nuvem pega um pouco da cor atual do sol (fica dourada/rosada no fim do dia).")]
    public bool tintWithSunColor = true;

    [Range(0f, 1f)]
    [Tooltip("O quanto da cor do sol se mistura na nuvem (0 = sempre branca, 1 = cor do sol pura).")]
    public float tintStrength = 0.25f;

    Vector3 startPos;
    Vector3 basePosition;
    float bobPhase;
    Renderer rend;
    MaterialPropertyBlock mpb;
    static readonly int ColorID = Shader.PropertyToID("_Color");

    void OnEnable()
    {
        startPos = transform.position;
        basePosition = transform.position;
        bobPhase = Random.Range(0f, Mathf.PI * 2f); // fase aleatória, pra nuvens não boiarem todas sincronizadas
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        UpdateMovement();
        UpdateDayNightSync();
    }

    void UpdateMovement()
    {
        Vector3 dir = driftDirection.sqrMagnitude > 0.0001f ? driftDirection.normalized : Vector3.right;

        if (!Application.isPlaying)
        {
            // Fora do Play mode, não mexe na posição — só mantém a referência
            // interna atualizada, pra você conseguir arrastar a nuvem livremente
            // no Editor sem ela "voltar" sozinha no frame seguinte.
            basePosition = transform.position;
            return;
        }

        basePosition += dir * driftSpeed * Time.deltaTime;

        if (wrapAround)
        {
            float traveled = Vector3.Dot(basePosition - startPos, dir);
            if (traveled > wrapDistance)
            {
                basePosition -= dir * (wrapDistance * 2f);
            }
        }

        float bobOffset = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bobOffset;
    }

    void UpdateDayNightSync()
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

        float t = Mathf.Clamp01(dayNightController.sunLight.intensity / Mathf.Max(0.01f, peakSunIntensity));
        float opacity = Mathf.Lerp(minOpacityAtNight, maxOpacityAtDay, t);

        Color baseColor = Color.white;
        if (tintWithSunColor)
            baseColor = Color.Lerp(Color.white, dayNightController.sunLight.color, tintStrength);

        baseColor.a = opacity;

        rend.GetPropertyBlock(mpb);
        mpb.SetColor(ColorID, baseColor);
        rend.SetPropertyBlock(mpb);
    }
}