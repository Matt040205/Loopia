// Depois da Sprint 5B (pedido do responsavel): fora do tiro, o braco do arco (esquerdo) acompanha a marcha em vez de
// correr com o arco erguido na frente do peito. Menu: Loopia/Lucca/Braco do arco acompanha a marcha
//
// Nos clipes Walk e Run, o balanco do braco esquerdo (ombro, braco e cotovelo) vira o do braco direito com meio ciclo de
// defasagem: a passada e simetrica, entao o braco esquerdo vai a frente com a perna direita, no estilo que o artista
// animou o direito. A pegada (torcao do braco e do antebraco, mao) e ajustada quadro a quadro para o arco (Bow_Grip)
// manter a orientacao que tem no Idle: o punho compensa o vaivem e o arco nao gira em torno da mao. Dedos continuam os
// do clipe (fechados no arco). O tiro (camada Upper Body, Bow Shot) nao muda. Idempotente: deriva do braco direito e
// do Idle.
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

    /// <summary>Pegada do arco: ajustada por quadro para o Bow_Grip ficar como no Idle.</summary>
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
            float residuo = AjustarPegada(clipe, idle);
            EditorUtility.SetDirty(clipe);
            Debug.Log("[BracosDaLucca] " + nome + ": desvio maximo do arco em relacao ao Idle " + residuo.ToString("0.0") + " graus");
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

    /// <summary>
    /// Para cada quadro: avalia o clipe no LuccaVisual, e busca (coordenada a coordenada) os musculos da Pegada que deixam
    /// o Bow_Grip, no espaco do personagem, o mais perto possivel da orientacao do Idle. Devolve o maior desvio (graus).
    /// </summary>
    public static float AjustarPegada(AnimationClip clipe, AnimationClip idle)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Lucca/LuccaVisual.prefab");
        var inst = Object.Instantiate(prefab);
        inst.hideFlags = HideFlags.HideAndDontSave;
        float pior = 0f;
        try
        {
            var an = inst.GetComponentInChildren<Animator>();
            Transform grip = null;
            foreach (var t in inst.GetComponentsInChildren<Transform>()) if (t.name == "Bow_Grip") grip = t;
            var handler = new HumanPoseHandler(an.avatar, an.transform);
            var indices = new int[Pegada.Length];
            for (int k = 0; k < Pegada.Length; k++) indices[k] = System.Array.IndexOf(HumanTrait.MuscleName, "Left " + Pegada[k]);
            Quaternion Orientacao() => Quaternion.Inverse(an.transform.rotation) * grip.rotation;

            Avaliar(an, idle, 0f);
            Quaternion alvo = Orientacao();

            float duracao = clipe.length, passo = 1f / Mathf.Max(1f, clipe.frameRate);
            int n = Mathf.RoundToInt(duracao / passo);
            var valores = new float[Pegada.Length, n + 1];
            var pose = new HumanPose();
            for (int i = 0; i <= n; i++)
            {
                Avaliar(an, clipe, Mathf.Min(duracao, i * passo));
                handler.GetHumanPose(ref pose);
                float Erro() { handler.SetHumanPose(ref pose); return Quaternion.Angle(alvo, Orientacao()); }
                float melhor = Erro();
                foreach (float salto in new[] { .4f, .2f, .1f, .05f, .025f, .0125f })
                {
                    bool melhorou = true;
                    while (melhorou)
                    {
                        melhorou = false;
                        foreach (int m in indices)
                        {
                            float antes = pose.muscles[m];
                            foreach (float d in new[] { salto, -salto })
                            {
                                pose.muscles[m] = Mathf.Clamp(antes + d, -1f, 1f);
                                float e = Erro();
                                if (e < melhor - 1e-3f) { melhor = e; antes = pose.muscles[m]; melhorou = true; }
                            }
                            pose.muscles[m] = antes;
                        }
                    }
                }
                pior = Mathf.Max(pior, melhor);
                for (int k = 0; k < indices.Length; k++) valores[k, i] = pose.muscles[indices[k]];
            }
            for (int k = 0; k < Pegada.Length; k++)
            {
                var chaves = new Keyframe[n + 1];
                for (int i = 0; i <= n; i++) chaves[i] = new Keyframe(Mathf.Min(duracao, i * passo), valores[k, i]);
                chaves[n].value = chaves[0].value; // laco exato
                var curva = new AnimationCurve(chaves);
                for (int i = 0; i <= n; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curva, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curva, i, AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(clipe, Musculo("Left " + Pegada[k]), curva);
            }
        }
        finally
        {
            Object.DestroyImmediate(inst);
        }
        return pior;
    }

    static void Avaliar(Animator an, AnimationClip clipe, float tempo)
    {
        var grafo = UnityEngine.Playables.PlayableGraph.Create("BracosDaLucca");
        try
        {
            var saida = UnityEngine.Animations.AnimationPlayableOutput.Create(grafo, "s", an);
            var tocador = UnityEngine.Animations.AnimationClipPlayable.Create(grafo, clipe);
            tocador.SetApplyFootIK(false);
            UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(saida, tocador);
            grafo.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
            UnityEngine.Playables.PlayableExtensions.SetTime(tocador, (double)tempo);
            grafo.Evaluate(0f);
        }
        finally
        {
            grafo.Destroy();
        }
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
