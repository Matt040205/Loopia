using UnityEngine;

/// <summary>
/// Controla o ciclo de dia/noite: rotação e cor do sol, intensidade,
/// cor do céu/fundo, e (opcionalmente) a luz ambiente (Environment > Gradient).
/// Paleta padrão pensada pro visual "pintado" estilo Tunic.
/// </summary>
[ExecuteAlways]
public class DayNightController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("A Directional Light que representa o sol.")]
    public Light sunLight;

    [Tooltip("Câmera cujo Background Color (quando estiver em Solid Color) vai acompanhar o ciclo. Deixe vazio para usar Camera.main.")]
    public Camera targetCamera;

    [Header("Ciclo de tempo")]
    [Range(0f, 24f)]
    [Tooltip("Hora atual do dia, em formato 24h (0 = meia-noite, 12 = meio-dia). Arraste este slider no Editor (fora do Play) para pré-visualizar qualquer hora instantaneamente.")]
    public float timeOfDay = 12f;

    [Tooltip("Quantos MINUTOS reais um dia completo (24h no jogo) deve durar. Só é usado se Auto Advance Time estiver ligado.")]
    public float dayLengthInMinutes = 5f;

    [Tooltip("Se ligado, o tempo avança sozinho durante o Play. Se desligado, você controla 'Time Of Day' manualmente (ótimo pra testar uma hora específica).")]
    public bool autoAdvanceTime = true;

    [Header("Trajetória do sol")]
    [Tooltip("Ângulo Y (direção leste-oeste) do sol. Combine com a rotação da sua câmera isométrica (ex: mesma Y da câmera ou um valor próximo).")]
    public float sunAzimuth = -30f;

    [Tooltip("Em qual hora o sol nasce (ângulo X = 0°, luz rasante).")]
    public float sunriseHour = 6f;

    [Tooltip("Em qual hora o sol se põe (ângulo X = 180°, luz rasante do outro lado).")]
    public float sunsetHour = 18f;

    [Header("Cores — Sol")]
    public Gradient sunColorGradient;
    public AnimationCurve sunIntensityCurve;

    [Header("Cores — Céu / Fundo")]
    public Gradient skyColorGradient;

    [Tooltip("Também aplicar essas cores na Ambient Light (Lighting > Environment, modo Gradient)? Mantém a luz ambiente sincronizada com o ciclo do dia.")]
    public bool syncAmbientLight = true;

    [Header("Fog (névoa atmosférica)")]
    [Tooltip("Liga o Fog e sincroniza a cor dele automaticamente com a cor do céu — assim ele nunca destoa em nenhum horário do dia.")]
    public bool syncFog = true;

    [Range(0f, 0.1f)]
    [Tooltip("Densidade do Fog (Exponential). Comece baixo, tipo 0.01-0.02, e suba aos poucos.")]
    public float fogDensity = 0.015f;

    void Reset()
    {
        SetDefaultPalette();
    }

    void OnValidate()
    {
        if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            SetDefaultPalette();

        Apply();
    }

    void Update()
    {
        if (autoAdvanceTime && Application.isPlaying)
        {
            float hoursPerSecond = 24f / Mathf.Max(1f, dayLengthInMinutes * 60f);
            timeOfDay += Time.deltaTime * hoursPerSecond;
            if (timeOfDay >= 24f) timeOfDay -= 24f;
        }

        Apply();
    }

    void Apply()
    {
        if (sunLight == null) return;

        float t = timeOfDay / 24f; // normalizado 0-1

        // --- Cor e intensidade do sol ---
        sunLight.color = sunColorGradient.Evaluate(t);
        sunLight.intensity = sunIntensityCurve.Evaluate(t);

        // --- Rotação do sol (nasce a leste ~0°, meio-dia ~90°, se põe a oeste ~180°) ---
        float sunAngleX;

        if (timeOfDay >= sunriseHour && timeOfDay <= sunsetHour)
        {
            float dayFraction = Mathf.InverseLerp(sunriseHour, sunsetHour, timeOfDay);
            sunAngleX = Mathf.Lerp(0f, 180f, dayFraction);
        }
        else
        {
            float nightLength = 24f - (sunsetHour - sunriseHour);
            float hoursSinceSunset = timeOfDay < sunriseHour
                ? (timeOfDay + 24f - sunsetHour)
                : (timeOfDay - sunsetHour);
            float nightFraction = nightLength > 0f ? hoursSinceSunset / nightLength : 0f;
            sunAngleX = Mathf.Lerp(180f, 360f, nightFraction);
        }

        sunLight.transform.rotation = Quaternion.Euler(sunAngleX, sunAzimuth, 0f);

        // --- Cor do céu / fundo (Camera Background em modo Solid Color) ---
        Color skyColor = skyColorGradient.Evaluate(t);

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam != null && cam.clearFlags == CameraClearFlags.SolidColor)
        {
            cam.backgroundColor = skyColor;
        }

        // --- Luz ambiente sincronizada (Lighting > Environment > Gradient) ---
        if (syncAmbientLight)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyColor;
            RenderSettings.ambientEquatorColor = Color.Lerp(skyColor, sunLight.color, 0.3f);
            RenderSettings.ambientGroundColor = skyColor * 0.5f;
        }

        // --- Fog sincronizado com a cor do céu (nunca destoa em nenhum horário) ---
        if (syncFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = skyColor;
        }
    }

    /// <summary>
    /// Aplica de uma vez a paleta V2 (Tunic + Sky: Children of Light) com os valores
    /// já calibrados na conversa: meio-dia azul-pastel neutro (não amarelo),
    /// alvorada/entardecer quentes, noite azul-violeta suave.
    /// Clique com o botão direito no componente (ou no ícone de engrenagem) e escolha
    /// esta opção no menu pra aplicar tudo de uma vez, sem editar gradiente na mão.
    /// </summary>
    [ContextMenu("Reset Default Palette (V2 - Tunic + Sky)")]
    void ResetPaletteV2()
    {
        SetDefaultPalette();
        Apply();
    }

    /// <summary>
    /// Paleta padrão estilo Tunic: sombras/noite frias e azuladas,
    /// meio-dia quente sem ser branco puro, alvorada/entardecer saturados.
    /// Ajuste livremente no Inspector depois — isso é só o ponto de partida.
    /// </summary>
    void SetDefaultPalette()
    {
        sunColorGradient = new Gradient();
        sunColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(HexColor("4A5A8C"), 0.00f), // 00h - luar (mais claro/visível que antes)
                new GradientColorKey(HexColor("FFC896"), 0.25f), // 06h - alvorada pêssego-dourado
                new GradientColorKey(HexColor("F2F5FF"), 0.50f), // 12h - meio-dia branco levemente FRIO (não amarelo — combina com céu azul)
                new GradientColorKey(HexColor("FFA98C"), 0.75f), // 18h - entardecer coral suave
                new GradientColorKey(HexColor("4A5A8C"), 1.00f)  // 24h - volta pro luar
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        sunIntensityCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.10f),
            new Keyframe(0.22f, 0.20f),
            new Keyframe(0.25f, 1.00f),
            new Keyframe(0.50f, 1.40f),
            new Keyframe(0.75f, 0.70f),
            new Keyframe(0.85f, 0.20f),
            new Keyframe(1.00f, 0.10f)
        );

        skyColorGradient = new Gradient();
        skyColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(HexColor("2B3A67"), 0.00f), // 00h - céu noturno azul-violeta
                new GradientColorKey(HexColor("E8A9C3"), 0.25f), // 06h - céu de alvorada lavanda-rosa
                new GradientColorKey(HexColor("9FD8E8"), 0.50f), // 12h - céu de dia azul-pastel claro e vivo
                new GradientColorKey(HexColor("F2937A"), 0.75f), // 18h - céu de entardecer pêssego-coral
                new GradientColorKey(HexColor("2B3A67"), 1.00f)  // 24h - volta pro noturno
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}