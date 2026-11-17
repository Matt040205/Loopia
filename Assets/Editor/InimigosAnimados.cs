// Sprint 5B: configura (de forma reproduzivel) os inimigos animados a partir dos FBX v1.1 e dos FBX de animacao
// exportados de ArtSource/Inimigos/Sprint-5B (scripts/exportar_animacoes.py).
// Menu: Loopia/Inimigos/Configurar modelos animados  (importadores, Animator Controllers, prefabs, bounds, InimigoDef)
//       Loopia/Inimigos/Ligar visual do chefe na cena (EncontroDoChefe.variante.prefab na HexPrototipo)
//
// - Modelo (FBX recebido, GUID preservado): Generic, avatar do proprio modelo, sem clipes, escala 1, Bake Axis Conversion.
// - Animacoes (<Modelo>_Animacoes.fbx): Generic, Copy From Other Avatar (o do modelo), Preserve Hierarchy (mesmos caminhos
//   do modelo), sem compressao (as poses das asas foram validadas no Blender quadro a quadro), nomes limpos e Loop Time
//   so nos lacos.
// - Controller por familia: camada Base (estados de gameplay) + camada Reacao aditiva (Hit). Attack e Revoada por
//   motion time (parametros TempoDoAtaque/TempoDaRevoada), dirigidos por AnimacaoDoInimigo.
// - Prefab <Modelo>_Visual: raiz com AnimacaoDoInimigo + NavMeshModifier(ignoreFromBuild); filho "Modelo" com o Animator
//   (sem root motion) e o SkinnedMeshRenderer com localBounds medidos nos clipes (BakeMesh a cada 1/48 s) + margem.
using System.Collections.Generic;
using System.Linq;
using Loopia.Hex;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class InimigosAnimados
{
    public const string Pasta = "Assets/Modelos/Inimigos";
    public const float MargemDosLimites = 0.08f;

    public sealed class Familia
    {
        public string modelo;
        public string[] lacos;
        public string idle, fly, ataque, hit, morte, revoada, walk, run;
        public bool patas;
        public float duracaoDoAtaque, mordida, duracaoDaMorte;
        public float duracaoDaRevoada, raiseDaRevoada;
        public float velocidadeDaCaminhada, velocidadeDaCorrida;
        public float quedaMaxima = 99f;
        public string Fbx => Pasta + "/" + modelo + "/" + modelo + ".fbx";
        public string FbxAnimacoes => Pasta + "/" + modelo + "/" + modelo + "_Animacoes.fbx";
        public string Controller => Pasta + "/" + modelo + "/" + modelo + ".controller";
        public string Prefab => Pasta + "/" + modelo + "/" + modelo + "_Visual.prefab";
    }

    // Tempos e velocidades das acoes de producao (ArtSource/Inimigos/Sprint-5B/Blender/acoes_de_producao.json).
    public static readonly Familia[] Familias =
    {
        new Familia { modelo = "Bat_Common", lacos = new[] { "Idle", "Fly" }, idle = "Idle", fly = "Fly", ataque = "Attack",
            hit = "Hit", morte = "Death", duracaoDoAtaque = 0.75f, mordida = 10f / 24f, duracaoDaMorte = 1f },
        new Familia { modelo = "Bat_AncientBoss", lacos = new[] { "Boss_Idle", "Boss_Fly" }, idle = "Boss_Idle",
            fly = "Boss_Fly", ataque = "Boss_Attack", hit = "Boss_Hit", morte = "Boss_Death", revoada = "Boss_Revoada",
            duracaoDoAtaque = 1f, mordida = 13f / 24f, duracaoDaMorte = 1.75f, duracaoDaRevoada = 3.5f,
            raiseDaRevoada = 2f, quedaMaxima = 1.5f },
        new Familia { modelo = "Wolf_Common", lacos = new[] { "Wolf_Idle", "Wolf_Walk", "Wolf_Run" }, idle = "Wolf_Idle",
            walk = "Wolf_Walk", run = "Wolf_Run", ataque = "Wolf_Attack", hit = "Wolf_Hit", morte = "Wolf_Death",
            patas = true, duracaoDoAtaque = 0.625f, mordida = 4f / 24f, duracaoDaMorte = 1.25f,
            velocidadeDaCaminhada = 0.98909f, velocidadeDaCorrida = 4.571429f },
    };

    [MenuItem("Loopia/Inimigos/Configurar modelos animados")]
    public static void ConfigurarTudo()
    {
        foreach (var f in Familias)
        {
            ConfigurarModelo(f);
            ConfigurarAnimacoes(f);
            var ctrl = CriarController(f);
            CriarPrefab(f, ctrl);
        }
        LigarInimigoDefs();
        AssetDatabase.SaveAssets();
        Debug.Log("[InimigosAnimados] modelos, controllers e prefabs configurados.");
    }

    // ------------------------------------------------------------------ importacao

    static void ConfigurarModelo(Familia f)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(f.Fbx);
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = false;
        mi.globalScale = 1f;
        mi.useFileScale = true;
        mi.bakeAxisConversion = true;
        mi.isReadable = false; // nenhuma ferramenta de runtime le a malha (handoff 5A.1 §4)
        mi.SaveAndReimport();
    }

    public static Avatar AvatarDoModelo(Familia f) =>
        AssetDatabase.LoadAllAssetsAtPath(f.Fbx).OfType<Avatar>().FirstOrDefault();

    static void ConfigurarAnimacoes(Familia f)
    {
        var ai = (ModelImporter)AssetImporter.GetAtPath(f.FbxAnimacoes);
        ai.animationType = ModelImporterAnimationType.Generic;
        ai.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        ai.sourceAvatar = AvatarDoModelo(f);
        ai.importAnimation = true;
        ai.resampleCurves = true;
        ai.animationCompression = ModelImporterAnimationCompression.Off;
        ai.importConstraints = false;
        ai.materialImportMode = ModelImporterMaterialImportMode.None;
        ai.bakeAxisConversion = true;
        ai.globalScale = 1f;
        ai.useFileScale = true;
        ai.isReadable = false;
        // O FBX de animacao tem so a armature: sem isto a Unity colapsa o no raiz e as curvas viram "Root/..." em vez
        // de "<Modelo>/Root/...", o caminho do modelo (que tem armature + malha na raiz).
        ai.preserveHierarchy = true;
        ai.SaveAndReimport();
        var clipes = new List<ModelImporterClipAnimation>();
        foreach (var take in ai.importedTakeInfos)
        {
            string nome = take.name.Contains("|") ? take.name.Substring(take.name.LastIndexOf('|') + 1) : take.name;
            float fps = take.sampleRate > 0 ? take.sampleRate : 24f;
            var c = new ModelImporterClipAnimation
            {
                name = nome,
                takeName = take.name,
                firstFrame = take.startTime * fps,
                lastFrame = take.stopTime * fps,
                loopTime = f.lacos.Contains(nome),
                loopPose = false, // a ultima chave ja repete a primeira (laco exato vindo do Blender)
                keepOriginalOrientation = true,
                keepOriginalPositionY = true,
                keepOriginalPositionXZ = true,
                lockRootRotation = false,
                lockRootHeightY = false,
                lockRootPositionXZ = false,
                hasAdditiveReferencePose = nome == f.hit,
                additiveReferencePoseFrame = take.startTime * fps,
            };
            clipes.Add(c);
        }
        ai.clipAnimations = clipes.ToArray();
        ai.SaveAndReimport();
    }

    public static AnimationClip Clipe(Familia f, string nome) =>
        nome == null ? null : AssetDatabase.LoadAllAssetsAtPath(f.FbxAnimacoes).OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == nome);

    // ------------------------------------------------------------------ controllers

    static AnimatorStateTransition Transicao(AnimatorState de, AnimatorState para, float duracao)
    {
        var t = de.AddTransition(para);
        t.hasExitTime = false;
        t.duration = duracao;
        t.interruptionSource = TransitionInterruptionSource.None;
        return t;
    }

    static AnimatorController CriarController(Familia f)
    {
        AssetDatabase.DeleteAsset(f.Controller);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(f.Controller);
        ctrl.AddParameter("Atacando", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("TempoDoAtaque", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Dano", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Morto", AnimatorControllerParameterType.Bool);
        if (f.patas)
        {
            ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            ctrl.AddParameter(new AnimatorControllerParameter { name = "Ritmo", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        }
        else ctrl.AddParameter("Voando", AnimatorControllerParameterType.Bool);
        if (f.revoada != null)
        {
            ctrl.AddParameter("Revoada", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("TempoDaRevoada", AnimatorControllerParameterType.Float);
        }

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle", new Vector3(300, 0));
        idle.motion = Clipe(f, f.idle);
        sm.defaultState = idle;
        var ataque = sm.AddState("Attack", new Vector3(300, 220));
        ataque.motion = Clipe(f, f.ataque);
        ataque.timeParameterActive = true;
        ataque.timeParameter = "TempoDoAtaque";
        var morte = sm.AddState("Death", new Vector3(600, 220));
        morte.motion = Clipe(f, f.morte);

        var paraMorte = sm.AddAnyStateTransition(morte);
        paraMorte.AddCondition(AnimatorConditionMode.If, 0, "Morto");
        paraMorte.hasExitTime = false;
        paraMorte.duration = 0.12f;
        paraMorte.canTransitionToSelf = false;
        var paraAtaque = sm.AddAnyStateTransition(ataque);
        paraAtaque.AddCondition(AnimatorConditionMode.If, 0, "Atacando");
        paraAtaque.AddCondition(AnimatorConditionMode.IfNot, 0, "Morto");
        paraAtaque.hasExitTime = false;
        paraAtaque.duration = 0.08f;
        paraAtaque.canTransitionToSelf = false;

        if (f.patas)
        {
            var loco = ctrl.CreateBlendTreeInController("Locomocao", out BlendTree arvore, 0);
            loco.speedParameterActive = true;
            loco.speedParameter = "Ritmo";
            arvore.blendType = BlendTreeType.Simple1D;
            arvore.blendParameter = "Velocidade";
            arvore.useAutomaticThresholds = false;
            arvore.AddChild(Clipe(f, f.walk), f.velocidadeDaCaminhada);
            arvore.AddChild(Clipe(f, f.run), f.velocidadeDaCorrida);
            var anda = Transicao(idle, loco, 0.2f);
            anda.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Velocidade");
            var para = Transicao(loco, idle, 0.25f);
            para.AddCondition(AnimatorConditionMode.Less, 0.12f, "Velocidade");
            var fim = Transicao(ataque, idle, 0.15f);
            fim.AddCondition(AnimatorConditionMode.IfNot, 0, "Atacando");
        }
        else
        {
            var fly = sm.AddState("Fly", new Vector3(600, 0));
            fly.motion = Clipe(f, f.fly);
            Transicao(idle, fly, 0.2f).AddCondition(AnimatorConditionMode.If, 0, "Voando");
            var pousa = Transicao(fly, idle, 0.35f);
            pousa.AddCondition(AnimatorConditionMode.IfNot, 0, "Voando");
            if (f.revoada != null) pousa.AddCondition(AnimatorConditionMode.IfNot, 0, "Revoada");
            if (f.revoada != null)
            {
                var rev = sm.AddState("Revoada", new Vector3(450, -160));
                rev.motion = Clipe(f, f.revoada);
                rev.timeParameterActive = true;
                rev.timeParameter = "TempoDaRevoada";
                var r1 = Transicao(ataque, rev, 0.2f);
                r1.AddCondition(AnimatorConditionMode.IfNot, 0, "Atacando");
                r1.AddCondition(AnimatorConditionMode.If, 0, "Revoada");
                Transicao(idle, rev, 0.2f).AddCondition(AnimatorConditionMode.If, 0, "Revoada");
                Transicao(fly, rev, 0.2f).AddCondition(AnimatorConditionMode.If, 0, "Revoada");
                var sai = Transicao(rev, fly, 0.3f);
                sai.AddCondition(AnimatorConditionMode.IfNot, 0, "Revoada");
            }
            var fim = Transicao(ataque, fly, 0.15f);
            fim.AddCondition(AnimatorConditionMode.IfNot, 0, "Atacando");
        }

        // Reacao (aditiva): o Hit soma por cima de qualquer estado, sem interromper ataque nem voo.
        ctrl.AddLayer("Reacao");
        var camadas = ctrl.layers;
        camadas[1].blendingMode = AnimatorLayerBlendingMode.Additive;
        camadas[1].defaultWeight = 1f;
        ctrl.layers = camadas;
        var smr = ctrl.layers[1].stateMachine;
        var vazio = smr.AddState("Vazio", new Vector3(300, 0));
        vazio.writeDefaultValues = false;
        smr.defaultState = vazio;
        var hit = smr.AddState("Hit", new Vector3(300, 120));
        hit.motion = Clipe(f, f.hit);
        hit.writeDefaultValues = false;
        var paraHit = smr.AddAnyStateTransition(hit);
        paraHit.AddCondition(AnimatorConditionMode.If, 0, "Dano");
        paraHit.hasExitTime = false;
        paraHit.duration = 0.03f;
        paraHit.canTransitionToSelf = true;
        var volta = hit.AddTransition(vazio);
        volta.hasExitTime = true;
        volta.exitTime = 0.95f;
        volta.duration = 0.05f;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // ------------------------------------------------------------------ prefab e limites

    /// <summary>
    /// Mede a uniao das poses de todos os clipes no espaco do rootBone (o espaco de SkinnedMeshRenderer.localBounds) e a
    /// altura do pivo acima do ponto mais baixo no fim do Death.
    /// </summary>
    public static (Bounds limites, float alturaNoChao) MedirClipes(Familia f)
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(f.Fbx);
        var inst = Object.Instantiate(modelo);
        inst.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>();
            var malha = new Mesh();
            bool primeiro = true;
            var limites = new Bounds();
            float alturaNoChao = 0f;
            var clipes = AssetDatabase.LoadAllAssetsAtPath(f.FbxAnimacoes).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__")).ToList();
            var vs = new List<Vector3>();
            foreach (var clipe in clipes)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt(clipe.length * 48f));
                for (int i = 0; i <= n; i++)
                {
                    float t = clipe.length * i / n;
                    clipe.SampleAnimation(inst, t);
                    smr.BakeMesh(malha, true);
                    malha.GetVertices(vs);
                    float menorY = float.MaxValue;
                    foreach (var v in vs)
                    {
                        Vector3 mundo = smr.transform.TransformPoint(v);
                        Vector3 p = smr.rootBone.InverseTransformPoint(mundo);
                        if (primeiro) { limites = new Bounds(p, Vector3.zero); primeiro = false; }
                        else limites.Encapsulate(p);
                        menorY = Mathf.Min(menorY, inst.transform.InverseTransformPoint(mundo).y);
                    }
                    if (clipe.name == f.morte && i == n) alturaNoChao = -menorY;
                }
            }
            Object.DestroyImmediate(malha);
            limites.Expand(limites.size * MargemDosLimites);
            return (limites, alturaNoChao);
        }
        finally
        {
            Object.DestroyImmediate(inst);
        }
    }

    static void CriarPrefab(Familia f, AnimatorController ctrl)
    {
        var (limites, alturaNoChao) = MedirClipes(f);
        var raiz = new GameObject(f.modelo + "_Visual");
        try
        {
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(f.Fbx), raiz.transform);
            modelo.name = "Modelo";
            var animator = modelo.GetComponent<Animator>();
            if (animator == null) animator = modelo.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.avatar = AvatarDoModelo(f);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.updateMode = AnimatorUpdateMode.Normal;
            var smr = modelo.GetComponentInChildren<SkinnedMeshRenderer>();
            smr.updateWhenOffscreen = false;
            smr.localBounds = limites;
            smr.shadowCastingMode = ShadowCastingMode.Off;
            smr.skinnedMotionVectors = false;
            smr.lightProbeUsage = LightProbeUsage.Off;
            smr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var anim = raiz.AddComponent<AnimacaoDoInimigo>();
            anim.animator = animator;
            anim.locomocao = f.patas ? AnimacaoDoInimigo.TipoDeLocomocao.Patas : AnimacaoDoInimigo.TipoDeLocomocao.Voo;
            anim.duracaoDoAtaque = f.duracaoDoAtaque;
            anim.mordida = f.mordida;
            anim.duracaoDaMorte = f.duracaoDaMorte;
            anim.alturaDoPivoNoChao = Mathf.Max(0f, alturaNoChao);
            anim.quedaMaxima = f.quedaMaxima;
            anim.temRevoada = f.revoada != null;
            if (f.revoada != null)
            {
                anim.duracaoDaRevoada = f.duracaoDaRevoada;
                anim.raiseDaRevoada = f.raiseDaRevoada;
            }
            if (f.patas)
            {
                anim.velocidadeDaCaminhada = f.velocidadeDaCaminhada;
                anim.velocidadeDaCorrida = f.velocidadeDaCorrida;
            }
            raiz.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var c in raiz.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(raiz, f.Prefab);
            Debug.Log("[InimigosAnimados] " + f.Prefab + " limites " + limites + " altura no chao " + alturaNoChao.ToString("0.000"));
        }
        finally
        {
            Object.DestroyImmediate(raiz);
        }
    }

    static void LigarInimigoDefs()
    {
        Ligar("Assets/Dados/Morcego.asset", "Bat_Common");
        Ligar("Assets/Dados/Lobo.asset", "Wolf_Common");
    }

    static void Ligar(string caminhoDoDef, string modelo)
    {
        var def = AssetDatabase.LoadAssetAtPath<InimigoDef>(caminhoDoDef);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pasta + "/" + modelo + "/" + modelo + "_Visual.prefab");
        if (def == null || prefab == null) return;
        def.prefab = prefab;
        EditorUtility.SetDirty(def);
    }

    // ------------------------------------------------------------------ cena

    public const string CenaDoJogo = "Assets/Scenes/HexPrototipo.unity";

    [MenuItem("Loopia/Inimigos/Ligar visual do chefe na cena")]
    public static void LigarVisualDoChefe()
    {
        var cena = EditorSceneManager.GetActiveScene();
        if (cena.path != CenaDoJogo)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            cena = EditorSceneManager.OpenScene(CenaDoJogo);
        }
        var encontro = Object.FindFirstObjectByType<EncontroDoChefe>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pasta + "/Bat_AncientBoss/Bat_AncientBoss_Visual.prefab");
        if (encontro == null || prefab == null)
        {
            Debug.LogError("[InimigosAnimados] faltou EncontroDoChefe na cena ou o prefab do Anciao.");
            return;
        }
        var so = new SerializedObject(encontro);
        so.FindProperty("variante.prefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Debug.Log("[InimigosAnimados] EncontroDoChefe.variante.prefab = " + prefab.name);
    }
}
