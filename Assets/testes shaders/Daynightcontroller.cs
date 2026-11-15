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

    [Range(0f, 30f)]
    [Tooltip("Elevação mínima (graus acima do horizonte) da luz direcional (26° = o topo das ilhas ainda recebe sol ao nascer e ao pôr; legibilidade medida no Pass D1). O sol nasce e se põe nesta elevação em vez de exatamente 0°, e a luz nunca vem de baixo do horizonte.")]
    public float elevacaoMinimaDoSol = 26f;

    [Range(30f, 90f)]
    [Tooltip("Elevação máxima do sol (graus). 90 = passa pelo zênite ao meio-dia (comportamento até o Pass D1: luz de cima, relevo e volume somem). Abaixo de 90 o sol sobe só até aqui e o azimute varre os mesmos 180° entre o nascer e o pôr (nascer e pôr idênticos), então ao meio-dia a luz vem de lado e o relevo lê.")]
    public float elevacaoMaximaDoSol = 90f;

    [Min(0.25f)]
    [Tooltip("Duração (horas de jogo) do crepúsculo: o sol sobe de 0 à intensidade da curva antes do nascer e desce até 0 depois do pôr. Evita o salto de intensidade de antes (0,2 -> 1,0 em 43 min de jogo).")]
    public float duracaoDoCrepusculo = 2f;

    [Header("Noite — lua e preenchimento")]
    [Tooltip("Cor da luz direcional à noite (luar frio).")]
    public Color luaCor = new Color(0.56f, 0.66f, 0.88f, 1f);

    [Range(0f, 2f)]
    [Tooltip("Intensidade da luz direcional à noite (a mesma Light do sol; um só dono de luz).")]
    public float luaIntensidade = 0.35f;

    [Range(5f, 60f)]
    [Tooltip("Elevação mínima da lua (graus) no começo e no fim da noite; no meio da noite ela passa a 90°.")]
    public float luaElevacaoMinima = 25f;

    [Tooltip("Cor somada à luz ambiente à noite, para o topo das ilhas continuar legível (Bible: topo >= 2x o fundo). Multiplicada pelo fator de noite (0 de dia, 1 de noite).")]
    public Color preenchimentoNoturno = new Color(0.20f, 0.25f, 0.42f, 1f);

    [Header("Cores — Sol")]
    public Gradient sunColorGradient;
    public AnimationCurve sunIntensityCurve;

    [Header("Cores — Céu / Fundo")]
    public Gradient skyColorGradient;

    [Tooltip("Também aplicar essas cores na Ambient Light (Lighting > Environment, modo Gradient)? Mantém a luz ambiente sincronizada com o ciclo do dia.")]
    public bool syncAmbientLight = true;

    [Range(0f, 1f)]
    [Tooltip("Quanto a luz ambiente se afasta da cor do céu em direção ao cinza de mesma luminância. 0 = ambiente igual ao céu (comportamento antes do Pass D1). Evita que o topo das ilhas fique 'laranja chapado' no entardecer.")]
    public float ambienteDessaturacao = 0.85f;

    [Header("Fog (névoa atmosférica)")]
    [Tooltip("Liga o Fog e sincroniza a cor dele automaticamente com a cor do céu — assim ele nunca destoa em nenhum horário do dia.")]
    public bool syncFog = true;

    [Tooltip("Modo do Fog. Exponential/ExponentialSquared usam 'Fog Density'; Linear usa 'Fog Inicio' e 'Fog Fim' (distância de câmera em unidades). Com a câmera ortográfica todas as ilhas ficam a profundidades parecidas, então Linear com início além da profundidade do anel deixa as ilhas sem névoa e só o fundo distante com perspectiva aérea.")]
    public FogMode modoDaNevoa = FogMode.Linear;

    [Range(0f, 0.1f)]
    [Tooltip("Densidade do Fog (Exponential). Comece baixo, tipo 0.01-0.02, e suba aos poucos.")]
    public float fogDensity = 0.015f;

    [Min(0f)]
    [Tooltip("Fog Linear: distância de câmera (profundidade) onde a névoa começa. Deve ser maior que a profundidade máxima de qualquer ilha (medido: até 100 u nas 12 seeds de teste).")]
    public float fogInicio = 105f;

    [Min(0f)]
    [Tooltip("Fog Linear: distância de câmera onde a névoa chega a 100%.")]
    public float fogFim = 230f;

    [Range(0f, 1f)]
    [Tooltip("Mistura entre a cor do céu (0) e a cor do horizonte (1) na névoa. O horizonte é o céu mais claro e menos saturado (de dia).")]
    public float misturaDaNevoaComHorizonte = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Quanto a cor do horizonte é dessaturada em relação ao céu.")]
    public float nevoaDessaturacao = 0.25f;

    [Range(0f, 1f)]
    [Tooltip("Quanto a cor do horizonte é clareada (em direção ao branco) de dia. À noite o clareamento some, para o fundo não competir com as ilhas.")]
    public float nevoaClareamento = 0.15f;

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

    /// <summary>
    /// Rampa suave (smoothstep) de 0 a 1 entre a e b. Pura e determinística (testada).
    /// </summary>
    static float Rampa(float x, float a, float b)
    {
        if (b <= a) return x >= b ? 1f : 0f;
        float u = Mathf.Clamp01((x - a) / (b - a));
        return u * u * (3f - 2f * u);
    }

    /// <summary>
    /// Fator de dia 0..1: sobe durante o crepúsculo da manhã (antes do nascer) e desce no do fim da tarde
    /// (depois do pôr). É 1 do nascer ao pôr do sol. "1 - fator" é o fator de noite.
    /// </summary>
    public float FatorDeDia(float hora)
    {
        float crep = Mathf.Max(0.25f, duracaoDoCrepusculo);
        return Rampa(hora, sunriseHour - crep, sunriseHour) * (1f - Rampa(hora, sunsetHour, sunsetHour + crep));
    }

    /// <summary>
    /// Direção, intensidade e cor da luz direcional para uma hora (0-24). Função pura do estado do componente.
    /// Sol: da elevação mínima à elevação mínima do outro lado (X de e a 180-e), nunca abaixo do horizonte; a
    /// intensidade sobe de 0 à da curva no crepúsculo da manhã e desce a 0 no da tarde.
    /// Lua: do fim do crepúsculo da tarde ao começo do crepúsculo da manhã, em arco (X de luaElevacaoMinima a
    /// 180 - luaElevacaoMinima), com intensidade própria e rampas nas pontas. Na troca sol-lua as duas
    /// intensidades são 0, então a mudança de direção não aparece.
    /// </summary>
    public void AvaliarLuz(float hora, out float anguloX, out float intensidade, out Color cor)
        => AvaliarLuz(hora, out anguloX, out _, out intensidade, out cor);

    /// <summary>
    /// Igual a <see cref="AvaliarLuz(float, out float, out float, out Color)"/>, devolvendo também o azimute (rotação Y)
    /// da luz. Com elevacaoMaximaDoSol &lt; 90 o sol vai da elevação mínima à máxima e volta (seno), enquanto o
    /// azimute varre sunAzimuth -> sunAzimuth + 180: no nascer e no pôr a direção é a mesma do arco antigo.
    /// </summary>
    public void AvaliarLuz(float hora, out float anguloX, out float azimute, out float intensidade, out Color cor)
    {
        azimute = sunAzimuth;
        float crep = Mathf.Max(0.25f, duracaoDoCrepusculo);
        float inicioSol = sunriseHour - crep;
        float fimSol = sunsetHour + crep;
        hora = Mathf.Repeat(hora, 24f);
        float t = hora / 24f;

        if (hora >= inicioSol && hora <= fimSol)
        {
            float e = Mathf.Clamp(elevacaoMinimaDoSol, 0f, 45f);
            float f = Mathf.Clamp01(Mathf.InverseLerp(sunriseHour, sunsetHour, hora));
            float eMax = Mathf.Clamp(elevacaoMaximaDoSol, e, 90f);
            if (eMax >= 89.99f)
            {
                anguloX = Mathf.Lerp(e, 180f - e, f);
            }
            else
            {
                anguloX = Mathf.Lerp(e, eMax, Mathf.Sin(Mathf.PI * f));
                azimute = sunAzimuth + 180f * f;
            }
            intensidade = sunIntensityCurve.Evaluate(t) * FatorDeDia(hora);
            cor = sunColorGradient.Evaluate(t);
        }
        else
        {
            float inicioNoite = fimSol;
            float fimNoite = inicioSol + 24f;
            float h = hora < inicioSol ? hora + 24f : hora;
            float duracao = Mathf.Max(0.01f, fimNoite - inicioNoite);
            float n = Mathf.Clamp01((h - inicioNoite) / duracao);
            float r = Mathf.Clamp(crep / duracao, 0.01f, 0.5f);
            float m = Mathf.Clamp(luaElevacaoMinima, 5f, 60f);
            anguloX = Mathf.Lerp(m, 180f - m, n);
            intensidade = luaIntensidade * Rampa(n, 0f, r) * (1f - Rampa(n, 1f - r, 1f));
            cor = luaCor;
        }
    }

    /// <summary>
    /// Cor da névoa: entre a cor do céu e uma cor de horizonte (céu dessaturado e, de dia, mais claro).
    /// Pura. Com mistura = 0 devolve exatamente a cor do céu (comportamento antigo).
    /// </summary>
    public static Color CorDaNevoa(Color ceu, float mistura, float dessaturacao, float clareamento, float fatorDeDia)
    {
        float lum = ceu.r * 0.2126f + ceu.g * 0.7152f + ceu.b * 0.0722f;
        Color cinza = new Color(lum, lum, lum, 1f);
        Color horizonte = Color.Lerp(ceu, cinza, dessaturacao);
        horizonte = Color.Lerp(horizonte, Color.white, clareamento * fatorDeDia);
        Color c = Color.Lerp(ceu, horizonte, mistura);
        c.a = 1f;
        return c;
    }

    public void Apply()
    {
        if (sunLight == null) return;
        if (sunColorGradient == null || skyColorGradient == null || sunIntensityCurve == null) SetDefaultPalette();

        float t = timeOfDay / 24f; // normalizado 0-1
        float dia = FatorDeDia(Mathf.Repeat(timeOfDay, 24f));

        // --- Cor, intensidade e direção da luz (sol de dia, lua à noite; nunca vem de baixo do horizonte) ---
        AvaliarLuz(timeOfDay, out float anguloX, out float azimute, out float intensidade, out Color corDaLuz);
        sunLight.color = corDaLuz;
        sunLight.intensity = intensidade;
        sunLight.transform.rotation = Quaternion.Euler(anguloX, azimute, 0f);

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
            Color ceuAmb = DessaturarAmbiente(skyColor);
            Color equadorAmb = DessaturarAmbiente(Color.Lerp(skyColor, sunLight.color, 0.3f));
            Color soloAmb = DessaturarAmbiente(skyColor) * 0.5f;

            // preenchimento de lua: soma luz fria à noite para o topo das ilhas continuar legível
            float noite = 1f - dia;
            ceuAmb += preenchimentoNoturno * noite;
            equadorAmb += preenchimentoNoturno * (0.8f * noite);
            soloAmb += preenchimentoNoturno * (0.35f * noite);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ceuAmb;
            RenderSettings.ambientEquatorColor = equadorAmb;
            RenderSettings.ambientGroundColor = soloAmb;
        }

        // --- Fog (único dono: este componente). Linear: ilhas sem névoa, fundo distante com perspectiva aérea ---
        if (syncFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = modoDaNevoa;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogInicio;
            RenderSettings.fogEndDistance = Mathf.Max(fogFim, fogInicio + 1f);
            RenderSettings.fogColor = CorDaNevoa(skyColor, misturaDaNevoaComHorizonte, nevoaDessaturacao, nevoaClareamento, dia);
        }
    }

    Color DessaturarAmbiente(Color c)
    {
        if (ambienteDessaturacao <= 0f) return c;
        float lum = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
        Color r = Color.Lerp(c, new Color(lum, lum, lum, c.a), ambienteDessaturacao);
        r.a = c.a;
        return r;
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
            // Só vale durante o dia (nascer-crepúsculo até pôr-crepúsculo); a noite usa luaIntensidade.
            // Sem a chave 0.22 -> 0.25 (0,2 -> 1,0 em 43 min de jogo): a subida é suave e o crepúsculo faz a rampa.
            new Keyframe(0.00f, 0.20f),
            new Keyframe(0.25f, 1.00f),
            new Keyframe(0.50f, 1.40f),
            new Keyframe(0.75f, 1.50f),
            new Keyframe(1.00f, 0.20f)
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