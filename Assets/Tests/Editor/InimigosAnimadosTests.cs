using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Sprint 5B: modelos, clipes, controllers e prefabs dos inimigos animados (sem Play Mode).
    /// </summary>
    [Category("Logic")]
    public class InimigosAnimadosTests
    {
        static readonly Dictionary<string, int> Ossos = new Dictionary<string, int>
        {
            { "Bat_Common", 25 }, { "Bat_AncientBoss", 26 }, { "Wolf_Common", 36 },
        };

        static IEnumerable<InimigosAnimados.Familia> Familias => InimigosAnimados.Familias;

        static GameObject Prefab(InimigosAnimados.Familia f)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(f.Prefab);
            Assert.IsNotNull(p, f.Prefab + " nao existe");
            return p;
        }

        [Test]
        public void TresFbx_ImportamComAvatarGenericUmaMalhaEORigCompleto()
        {
            foreach (var f in Familias)
            {
                var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(f.Fbx);
                Assert.IsNotNull(modelo, f.Fbx);
                var avatar = InimigosAnimados.AvatarDoModelo(f);
                Assert.IsNotNull(avatar, f.modelo + " sem Avatar");
                Assert.IsTrue(avatar.isValid, f.modelo + " Avatar invalido");
                Assert.IsFalse(avatar.isHuman, f.modelo + " deveria ser Generic");
                var smrs = modelo.GetComponentsInChildren<SkinnedMeshRenderer>();
                Assert.AreEqual(1, smrs.Length, f.modelo + ": um SkinnedMeshRenderer");
                Assert.AreEqual(Ossos[f.modelo], smrs[0].bones.Length, f.modelo + ": ossos");
                Assert.AreEqual("Root", smrs[0].rootBone.name);
                Assert.AreEqual("Universal Render Pipeline/Lit", smrs[0].sharedMaterial.shader.name, f.modelo + ": material URP");
                Assert.AreEqual(Vector3.one, modelo.transform.localScale, f.modelo + ": escala 1");
                Assert.AreEqual(0, AssetDatabase.LoadAllAssetsAtPath(f.Fbx).OfType<AnimationClip>()
                    .Count(c => !c.name.StartsWith("__")), f.modelo + ": o FBX do modelo nao traz clipes");
            }
        }

        [Test]
        public void Rigs_SaoGeneric_EAnimacoesCopiamOAvatarDoModelo()
        {
            foreach (var f in Familias)
            {
                var mi = (ModelImporter)AssetImporter.GetAtPath(f.Fbx);
                var ai = (ModelImporter)AssetImporter.GetAtPath(f.FbxAnimacoes);
                Assert.AreEqual(ModelImporterAnimationType.Generic, mi.animationType, f.Fbx);
                Assert.AreEqual(ModelImporterAnimationType.Generic, ai.animationType, f.FbxAnimacoes);
                Assert.IsTrue(mi.bakeAxisConversion && ai.bakeAxisConversion, f.modelo + ": Bake Axis Conversion");
                Assert.AreEqual(1f, mi.globalScale, 1e-6f);
                Assert.AreEqual(ModelImporterAvatarSetup.CopyFromOther, ai.avatarSetup, f.FbxAnimacoes);
                Assert.AreSame(InimigosAnimados.AvatarDoModelo(f), ai.sourceAvatar, f.FbxAnimacoes + ": avatar de origem");
                Assert.AreEqual(ModelImporterAnimationCompression.Off, ai.animationCompression, "poses validadas sem compressao");
            }
        }

        [Test]
        public void Clipes_EstaoPresentes_ComDuracaoEDefinicaoDeLaco()
        {
            var esperado = new Dictionary<string, (float dur, bool laco)>
            {
                { "Idle", (32 / 24f, true) }, { "Fly", (1f, true) }, { "Attack", (0.75f, false) }, { "Hit", (8 / 24f, false) },
                { "Death", (1f, false) }, { "Boss_Idle", (2f, true) }, { "Boss_Fly", (1.5f, true) },
                { "Boss_Attack", (1f, false) }, { "Boss_Hit", (10 / 24f, false) }, { "Boss_Death", (1.75f, false) },
                { "Boss_Revoada", (3.5f, false) }, { "Wolf_Idle", (3f, true) }, { "Wolf_Walk", (15 / 24f, true) },
                { "Wolf_Run", (9 / 24f, true) }, { "Wolf_Attack", (15 / 24f, false) }, { "Wolf_Hit", (8 / 24f, false) },
                { "Wolf_Death", (1.25f, false) },
            };
            int achados = 0;
            foreach (var f in Familias)
            {
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(f.FbxAnimacoes).OfType<AnimationClip>()
                             .Where(c => !c.name.StartsWith("__")))
                {
                    Assert.IsTrue(esperado.ContainsKey(c.name), "clipe inesperado " + c.name);
                    Assert.AreEqual(esperado[c.name].dur, c.length, 0.002f, c.name + ": duracao");
                    Assert.AreEqual(esperado[c.name].laco, c.isLooping, c.name + ": laco");
                    Assert.AreEqual(24f, c.frameRate, 0.01f, c.name + ": fps");
                    Assert.IsFalse(c.hasGenericRootTransform, c.name + ": anima a raiz (root motion)");
                    Assert.IsFalse(AnimationUtility.GetCurveBindings(c).Any(b => b.path == ""), c.name + ": curva na raiz do modelo");
                    Assert.IsFalse(AnimationUtility.GetCurveBindings(c).Any(b => b.path.EndsWith("/Root") && b.propertyName.StartsWith("m_LocalPosition")
                        && AnimationUtility.GetEditorCurve(c, b).keys.Select(k => k.value).Distinct().Count() > 1),
                        c.name + ": o osso Root se desloca (o clipe deve ser in-place)");
                    Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(c).Length,
                        c.name + ": nenhum Animation Event (dano continua so no gameplay)");
                    achados++;
                }
            }
            Assert.AreEqual(esperado.Count, achados, "clipes faltando");
        }

        [Test]
        public void Lacos_PrimeiraEUltimaPoseIguais()
        {
            foreach (var f in Familias)
            {
                var inst = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(f.Fbx));
                try
                {
                    var ossos = inst.GetComponentInChildren<SkinnedMeshRenderer>().bones;
                    foreach (var nome in f.lacos)
                    {
                        var clipe = InimigosAnimados.Clipe(f, nome);
                        clipe.SampleAnimation(inst, 0f);
                        var a = ossos.Select(o => (o.localRotation, o.localPosition)).ToArray();
                        clipe.SampleAnimation(inst, clipe.length);
                        for (int i = 0; i < ossos.Length; i++)
                        {
                            Assert.Less(Quaternion.Angle(a[i].localRotation, ossos[i].localRotation), 0.05f,
                                nome + " " + ossos[i].name + ": laco com salto de rotacao");
                            Assert.Less((a[i].localPosition - ossos[i].localPosition).magnitude, 1e-4f,
                                nome + " " + ossos[i].name + ": laco com salto de posicao");
                        }
                        // e o laco realmente anda (nao e uma pose parada)
                        clipe.SampleAnimation(inst, clipe.length * 0.37f);
                        Assert.Greater(ossos.Max(o => Quaternion.Angle(o.localRotation,
                            a[System.Array.IndexOf(ossos, o)].localRotation)), 2f, nome + " parado");
                    }
                }
                finally { Object.DestroyImmediate(inst); }
            }
        }

        [Test]
        public void Prefabs_TemAnimatorSemRootMotion_ControllerEAvatar()
        {
            foreach (var f in Familias)
            {
                var p = Prefab(f);
                var animators = p.GetComponentsInChildren<Animator>(true);
                Assert.AreEqual(1, animators.Length, f.Prefab + ": um Animator");
                var an = animators[0];
                Assert.IsFalse(an.applyRootMotion, f.Prefab + ": root motion ligado");
                Assert.IsNotNull(an.runtimeAnimatorController, f.Prefab + ": sem controller");
                Assert.AreSame(InimigosAnimados.AvatarDoModelo(f), an.avatar, f.Prefab + ": avatar");
                Assert.AreEqual(AnimatorCullingMode.CullUpdateTransforms, an.cullingMode);
                var c = p.GetComponent<AnimacaoDoInimigo>();
                Assert.IsNotNull(c, f.Prefab + ": sem AnimacaoDoInimigo na raiz");
                Assert.AreSame(an, c.animator);
                Assert.AreEqual(f.duracaoDoAtaque, InimigosAnimados.Clipe(f, f.ataque).length, 0.002f,
                    "duracao do ataque no componente = clipe");
                Assert.AreEqual(f.duracaoDoAtaque, c.duracaoDoAtaque, 1e-4f);
                Assert.Less(c.mordida, c.duracaoDoAtaque);
                Assert.AreEqual(InimigosAnimados.Clipe(f, f.morte).length, c.duracaoDaMorte, 0.002f);
                Assert.AreEqual(f.revoada != null, c.temRevoada);
                if (c.temRevoada)
                    Assert.AreEqual(InimigosAnimados.Clipe(f, f.revoada).length, c.duracaoDaRevoada, 0.002f);
            }
        }

        [Test]
        public void Controllers_TemOsEstadosDoGameplay_ECamadaDeReacaoAditiva()
        {
            foreach (var f in Familias)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(f.Controller);
                Assert.IsNotNull(ctrl, f.Controller);
                var estados = ctrl.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);
                foreach (var n in new[] { "Idle", "Attack", "Death" }) Assert.IsTrue(estados.ContainsKey(n), f.modelo + " sem " + n);
                Assert.IsTrue(estados.ContainsKey(f.patas ? "Locomocao" : "Fly"));
                Assert.AreEqual(f.revoada != null, estados.ContainsKey("Revoada"));
                Assert.IsTrue(estados["Attack"].timeParameterActive && estados["Attack"].timeParameter == "TempoDoAtaque",
                    "Attack por motion time");
                Assert.AreEqual(2, ctrl.layers.Length);
                Assert.AreEqual(UnityEditor.Animations.AnimatorLayerBlendingMode.Additive, ctrl.layers[1].blendingMode);
                Assert.AreEqual(InimigosAnimados.Clipe(f, f.hit),
                    ctrl.layers[1].stateMachine.states.First(s => s.state.name == "Hit").state.motion);
                var nomes = ctrl.parameters.Select(p => p.name).ToList();
                foreach (var n in new[] { "Atacando", "TempoDoAtaque", "Dano", "Morto" }) CollectionAssert.Contains(nomes, n);
            }
        }

        [Test]
        public void Limites_DoRendererCobremTodasAsPosesDosClipes()
        {
            foreach (var f in Familias)
            {
                var smr = Prefab(f).GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsFalse(smr.updateWhenOffscreen, "sem updateWhenOffscreen: os limites e que valem");
                var (medido, _) = InimigosAnimados.MedirClipes(f);
                // MedirClipes devolve a uniao + margem; a uniao crua precisa caber nos limites do prefab
                var crua = new Bounds(medido.center, medido.size / (1f + InimigosAnimados.MargemDosLimites));
                Assert.IsTrue(smr.localBounds.Contains(crua.min) && smr.localBounds.Contains(crua.max),
                    f.modelo + ": localBounds " + smr.localBounds + " nao cobre " + crua);
            }
        }

        [Test]
        public void Visuais_NaoInterferemNoNavMesh()
        {
            foreach (var f in Familias)
            {
                var p = Prefab(f);
                var mod = p.GetComponent<NavMeshModifier>();
                Assert.IsNotNull(mod, f.Prefab + ": sem NavMeshModifier");
                Assert.IsTrue(mod.ignoreFromBuild, f.Prefab + ": entra no bake");
                Assert.IsFalse(p.GetComponentsInChildren<Collider>(true).Any(c => c.enabled), f.Prefab + ": collider ligado");
                Assert.IsNull(p.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true), "agente so no GameObject de gameplay");
            }
        }

        [Test]
        public void InimigoDefs_UsamOsNovosModelos_SemMudarNumeros()
        {
            var morcego = AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/Morcego.asset");
            var lobo = AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/Lobo.asset");
            Assert.AreEqual("Bat_Common_Visual", morcego.prefab.name, "morcego ainda sem modelo (esfera)");
            Assert.AreEqual("Wolf_Common_Visual", lobo.prefab.name);
            // GDD: morcego 50/5/50, lobo 100/15/100; comportamento intacto
            Assert.AreEqual((50, 5, 50), (morcego.pv, morcego.ataque, morcego.xp));
            Assert.AreEqual((100, 15, 100), (lobo.pv, lobo.ataque, lobo.xp));
            Assert.AreEqual((1.6f, 2, 9f, 1.2f, 0.45f, 1.1f), (morcego.intervaloDeAtaque, morcego.alcance,
                morcego.velocidadeDoRasante, morcego.duracaoMaximaDoRasante, morcego.distanciaDoAcerto, morcego.alturaDeVoo));
            Assert.AreEqual((2.2f, 0, 4.5f, 4f, 0.65f), (lobo.intervaloDeAtaque, lobo.alcance,
                lobo.velocidadeDePerseguicao, lobo.raioDeDeteccao, lobo.distanciaCorpoACorpo));
        }

        [Test]
        public void Variante_ComPrefabProprio_UsaOTamanhoDoVisualSemMultiplicarDeNovo()
        {
            var morcego = AssetDatabase.LoadAssetAtPath<InimigoDef>("Assets/Dados/Morcego.asset");
            var anciao = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Inimigos/Bat_AncientBoss/Bat_AncientBoss_Visual.prefab");
            var v = new VarianteDoChefe { prefab = anciao };
            InimigoDef chefe = v.Montar(morcego);
            try
            {
                Assert.AreSame(anciao, chefe.prefab, "o chefe usa o proprio modelo");
                Assert.AreEqual(v.tamanhoDoVisual, chefe.escala, 1e-5f, "envergadura do chefe = tamanhoDoVisual");
                Assert.AreEqual(3f, v.tamanhoDoVisual, 1e-5f);
                Assert.AreEqual(500, chefe.pv, "numeros continuam derivados do morcego");
            }
            finally { Object.DestroyImmediate(chefe); }
        }
    }
}
