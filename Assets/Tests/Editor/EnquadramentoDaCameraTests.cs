using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// HexIsoCamera.MedirIlhas enquadra so pela malha do hexagono de cada ilha: o orthoSize nao pode depender
    /// de decoracoes, do modelo da ilha nem do perfil de escala. Os valores fixados abaixo sao medidos no Editor
    /// (Pass A, margem 1.14 da cena, raio 4..6 / tamanho 3 / folga 0.3) e servem de regressao: se mudarem, o
    /// zoom da tela mudou (Lucca/ilha em pixels) e isso precisa ser uma decisao.
    /// </summary>
    [Category("Logic")]
    public class EnquadramentoDaCameraTests
    {
        // Margem escolhida no Pass A (HexPrototipo > Main Camera > HexIsoCamera.margem). Ver docs/art/pass-a/RELATORIO_PASS_A.md.
        public const float MargemDaCena = 1.14f;
        const float Tolerancia = 0.02f;
        const float Largo = 21f / 9f;
        const float Padrao = 16f / 9f;

        // seed, ortho em 16:9, ortho em 21:9
        static readonly object[] Pinados =
        {
            new object[] { 12345, 16.829f, 16.829f },
            new object[] { 7, 18.559f, 18.452f },
            new object[] { 2024, 15.967f, 15.967f },
        };

        const string CaminhoDoPerfil = "Assets/Dados/Arte/EscalaDeArte.asset";
        readonly List<Object> _extras = new List<Object>();
        MundoDeTeste _m;

        [OneTimeSetUp]
        public void CenaVazia() => CenaDoTeste.NovaCenaVazia();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        [TearDown]
        public void Limpar()
        {
            foreach (var o in _extras) if (o != null) Object.DestroyImmediate(o);
            _extras.Clear();
            if (_m != null) _m.Destruir();
            _m = null;
        }

        enum Arte { Nada, DecoracaoComPerfil, DecoracaoComOutroPerfil, ModeloDaIlha, TudoJunto }

        float Ortho(int semente, float aspecto, Arte arte)
        {
            _m = MundoDeTeste.Criar(semente);
            bool deco = arte == Arte.DecoracaoComPerfil || arte == Arte.DecoracaoComOutroPerfil || arte == Arte.TudoJunto;
            bool modelo = arte == Arte.ModeloDaIlha || arte == Arte.TudoJunto;
            if (deco)
            {
                var d = _m.raiz.AddComponent<DecoracaoDeIlha>();
                var perfil = AssetDatabase.LoadAssetAtPath<ArtScaleProfile>(CaminhoDoPerfil);
                Assert.IsNotNull(perfil);
                if (arte == Arte.DecoracaoComOutroPerfil)
                {
                    // perfil diferente: tudo 2x maior e sem regra de lado.
                    var copia = Object.Instantiate(perfil);
                    _extras.Add(copia);
                    copia.alturaDeReferencia = 2f;
                    foreach (var r in copia.regras) r.soNoLadoLongeDaCamera = false;
                    perfil = copia;
                }
                d.perfil = perfil;
                _m.mundo.decoracao = d;
            }
            if (modelo)
                _m.mundo.modeloDaIlha = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/HexPrototype/IlhaFlutuante.prefab");
            Assert.IsTrue(_m.Gerar());

            if (deco)
            {
                int decoracoes = 0;
                foreach (HexTile ilha in _m.mundo.Ilhas)
                    if (ilha.transform.Find("Decoracoes") != null) decoracoes += ilha.transform.Find("Decoracoes").childCount;
                Assert.Greater(decoracoes, 0, "o teste precisa ter decoracoes para provar a independencia");
            }

            var cam = new GameObject("CameraDeTeste");
            _extras.Add(cam);
            cam.AddComponent<Camera>();
            var hic = cam.AddComponent<HexIsoCamera>();
            hic.mundo = _m.mundo;
            hic.margem = MargemDaCena;
            hic.enquadrarAutomaticamente = true;
            cam.GetComponent<Camera>().aspect = aspecto;
            hic.Enquadrar();
            float ortho = cam.GetComponent<Camera>().orthographicSize;
            Object.DestroyImmediate(cam);
            _m.Destruir(); _m = null;
            return ortho;
        }

        [TestCaseSource(nameof(Pinados))]
        public void Ortho_EIgualComESemDecoracoes_ComESemModeloDaIlha(int semente, float _169, float _219)
        {
            float base169 = Ortho(semente, Padrao, Arte.Nada);
            foreach (Arte arte in new[] { Arte.DecoracaoComPerfil, Arte.DecoracaoComOutroPerfil, Arte.ModeloDaIlha, Arte.TudoJunto })
                Assert.AreEqual(base169, Ortho(semente, Padrao, arte), 1e-4f, "ortho 16:9 mudou com " + arte);

            float base219 = Ortho(semente, Largo, Arte.Nada);
            foreach (Arte arte in new[] { Arte.DecoracaoComPerfil, Arte.ModeloDaIlha, Arte.TudoJunto })
                Assert.AreEqual(base219, Ortho(semente, Largo, arte), 1e-4f, "ortho 21:9 mudou com " + arte);
        }

        [TestCaseSource(nameof(Pinados))]
        public void Ortho_BateComOsValoresMedidosNoPassA(int semente, float esperado169, float esperado219)
        {
            // Valores medidos no Editor com a margem 1.14. Mais que isso = mais zoom out (Lucca e ilha menores).
            Assert.AreEqual(esperado169, Ortho(semente, Padrao, Arte.Nada), Tolerancia, "ortho 16:9 da semente " + semente);
            Assert.AreEqual(esperado219, Ortho(semente, Largo, Arte.Nada), Tolerancia, "ortho 21:9 da semente " + semente);
        }

        [TestCaseSource(nameof(Pinados))]
        public void Ortho_EMenorQueOEnquadramentoAntigoDoModelo(int semente, float _169, float _219)
        {
            // Antes do Pass A (todos os renderers + margem 1.3): 22.484 / 24.335 / 21.501 em 16:9 nestas 3 sementes.
            float antigo = semente == 12345 ? 22.484f : semente == 7 ? 24.335f : 21.501f;
            Assert.Less(Ortho(semente, Padrao, Arte.Nada), antigo - 1f);
        }
    }
}
