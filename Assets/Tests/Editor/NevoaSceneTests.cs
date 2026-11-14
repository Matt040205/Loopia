using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Pass D1 na cena HexPrototipo (sem Play, sem salvar): o DayNightController e o unico dono da nevoa, em modo Linear com
    /// inicio alem da profundidade maxima medida das ilhas, e a cena guarda esses valores nas RenderSettings.
    /// </summary>
    [Category("SceneStatic")]
    public class NevoaSceneTests
    {
        /// <summary>Profundidade de camera maxima (cantos do bounds de todo renderer das ilhas, incl. decoracoes e o modelo flutuante),
        /// medida em Play Mode nas 12 seeds 1..10, 12345 e 2024, em 1920x1080 e 2520x1080 (docs/art/pass-d1/profundidades.json):
        /// maximo 99,976 u (seed 4). Se a cena, a camera ou a decoracao mudarem a ponto de empurrar isso, remedir.</summary>
        const float ProfundidadeMaximaMedidaDasIlhas = 100f;

        /// <summary>Folga minima entre a ilha mais profunda e o inicio da nevoa.</summary>
        const float MargemMinima = 4f;

        [OneTimeSetUp]
        public void AbrirCena() => CenaDoTeste.AbrirCenaPrincipal();

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static DayNightController Controlador()
        {
            var todos = Object.FindObjectsByType<DayNightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, todos.Length, "esperava exatamente um DayNightController na cena (dono unico da nevoa)");
            return todos[0];
        }

        [Test]
        public void Controlador_UsaNevoaLinear_ComInicioAlemDaProfundidadeDasIlhas()
        {
            var dn = Controlador();
            Assert.IsTrue(dn.syncFog, "o controlador tem que ser o dono da nevoa");
            Assert.AreEqual(FogMode.Linear, dn.modoDaNevoa);
            Assert.GreaterOrEqual(dn.fogInicio, ProfundidadeMaximaMedidaDasIlhas + MargemMinima,
                "fogInicio abaixo da profundidade maxima medida das ilhas + margem: as ilhas entrariam na nevoa");
            Assert.Greater(dn.fogFim, dn.fogInicio + 20f, "faixa de nevoa curta demais: o fundo viraria uma parede");
            Assert.LessOrEqual(dn.fogFim, 400f, "fogFim alem do far plane util: sem perspectiva aerea no fundo");
        }

        [Test]
        public void RenderSettingsDaCena_GuardamNevoaLinearComOsValoresDoControlador()
        {
            var dn = Controlador();
            Assert.IsTrue(RenderSettings.fog, "a cena nao liga a nevoa");
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
            Assert.AreEqual(dn.fogInicio, RenderSettings.fogStartDistance, 1e-3f);
            Assert.AreEqual(dn.fogFim, RenderSettings.fogEndDistance, 1e-3f);
            Assert.GreaterOrEqual(RenderSettings.fogStartDistance, ProfundidadeMaximaMedidaDasIlhas + MargemMinima);
        }

        [Test]
        public void Controlador_ParametrosDeNoiteESolDoPassD1()
        {
            var dn = Controlador();
            Assert.Greater(dn.luaIntensidade, 0.2f);
            Assert.Greater(dn.preenchimentoNoturno.b, 0.2f);
            Assert.Greater(dn.elevacaoMinimaDoSol, 5f, "sol com elevacao minima > 0: nao fica rasante nem abaixo do horizonte");
            Assert.GreaterOrEqual(dn.duracaoDoCrepusculo, 1.75f, "crepusculo curto demais para o limite de 0,02 de intensidade por minuto");
            Assert.AreEqual(2f, dn.dayLengthInMinutes, 1e-4f, "o ciclo de 2 minutos nao muda no Pass D1");
        }

        [Test]
        public void Curva_DeIntensidadeNaoTemOSaltoDe0517As0600()
        {
            var dn = Controlador();
            // a chave antiga 0.22 -> 0.25 (0,2 -> 1,0 em 43 min de jogo) nao existe mais
            foreach (Keyframe k in dn.sunIntensityCurve.keys)
                Assert.IsFalse(Mathf.Approximately(k.time, 0.22f), "chave 0.22 da curva de intensidade ainda existe");
        }
    }
}
