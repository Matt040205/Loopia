// Depois da Sprint 5B (pedido do responsavel): fora do tiro, o braco do arco (esquerdo) acompanha a marcha em vez de
// correr com o arco erguido na frente do peito. Menu: Loopia/Lucca/Braco do arco acompanha a marcha
//
// Nos clipes Walk e Run, o balanco do braco esquerdo (ombro, braco e cotovelo) vira o do braco direito com meio ciclo de
// defasagem: a passada e simetrica, entao o braco esquerdo vai a frente com a perna direita, no estilo que o artista
// animou o direito. A pegada (torcao do braco e do antebraco, mao) fica constante, a do Idle: o arco segue o braco de pe,
// apontado para baixo, sem deitar para tras no recuo. Dedos continuam os do clipe (fechados no arco). O tiro (camada
// Upper Body, Bow Shot) nao muda. Idempotente: sempre deriva do braco direito e do Idle.
using UnityEditor;
using UnityEngine;

public static class BracosDaLucca
{
    public const string Pasta = "Assets/Modelos/Lucca/Animacoes/";
    public static readonly string[] Clipes = { "Walk", "Run" };
    /// <summary>Balanco: copiado do braco direito com meio ciclo de defasagem.</summary>
    public static readonly string[] Balanco =
    {
        "Shoulder Down-Up", "Shoulder Front-Back", "Arm Down-Up", "Arm Front-Back", "Forearm Stretch",
    };

    /// <summary>Pegada do arco: constante, a media do Idle (arco de pe).</summary>
    public static readonly string[] Pegada = { "Arm Twist In-Out", "Forearm Twist In-Out", "Hand Down-Up", "Hand In-Out" };

    [MenuItem("Loopia/Lucca/Braco do arco acompanha a marcha")]
    public static void Aplicar()
    {
        var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Pasta + "Idle.anim");
        foreach (string nome in Clipes)
        {
            var clipe = AssetDatabase.LoadAssetAtPath<AnimationClip>(Pasta + nome + ".anim");
            if (clipe == null || idle == null) { Debug.LogError("[BracosDaLucca] faltou " + nome + " ou Idle"); continue; }
            Espelhar(clipe, idle);
            EditorUtility.SetDirty(clipe);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[BracosDaLucca] braco esquerdo de Walk e Run = braco direito com meio ciclo de defasagem.");
    }

    static EditorCurveBinding Musculo(string nome) =>
        EditorCurveBinding.FloatCurve("", typeof(Animator), nome);

    public static float MediaNoIdle(AnimationClip idle, string musculo)
    {
        var c = AnimationUtility.GetEditorCurve(idle, Musculo(musculo));
        if (c == null) return 0f;
        float soma = 0f;
        for (int i = 0; i < 32; i++) soma += c.Evaluate(idle.length * i / 32f);
        return soma / 32f;
    }

    public static void Espelhar(AnimationClip clipe, AnimationClip idle)
    {
        float duracao = clipe.length;
        float passo = 1f / Mathf.Max(1f, clipe.frameRate);
        int n = Mathf.RoundToInt(duracao / passo);
        foreach (string m in Balanco) Gravar(clipe, m, n, passo, duracao, AnimationUtility.GetEditorCurve(clipe, Musculo("Right " + m)), 0f);
        foreach (string m in Pegada) Gravar(clipe, m, n, passo, duracao, null, MediaNoIdle(idle, "Left " + m));
    }

    static void Gravar(AnimationClip clipe, string m, int n, float passo, float duracao, AnimationCurve direita, float constante)
    {
        {
            var chaves = new Keyframe[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = Mathf.Min(duracao, i * passo);
                float origem = Mathf.Repeat(t + duracao * 0.5f, duracao);
                chaves[i] = new Keyframe(t, direita != null ? direita.Evaluate(origem) : constante);
            }
            chaves[n].value = chaves[0].value; // laco exato
            var esquerda = new AnimationCurve(chaves);
            for (int i = 0; i <= n; i++) AnimationUtility.SetKeyLeftTangentMode(esquerda, i, AnimationUtility.TangentMode.ClampedAuto);
            for (int i = 0; i <= n; i++) AnimationUtility.SetKeyRightTangentMode(esquerda, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetEditorCurve(clipe, Musculo("Left " + m), esquerda);
        }
    }
}
