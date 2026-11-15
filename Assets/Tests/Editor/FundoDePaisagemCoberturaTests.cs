using NUnit.Framework;
using UnityEngine;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Estabilizacao do MVP (P1-1, faixa rosa): a vista do anel inteiro nunca pode passar da borda do chao do
    /// FundoDePaisagem, senao aparece a cor de fundo da camera (rosa no fim da tarde). Le os numeros da HexPrototipo
    /// (vale, borda, camera, raio do anel) e enquadra aneis gerados em 16:9, 16:10, 4:3 e 21:9 numa cena vazia.
    /// </summary>
    [Category("SceneStatic")]
    public class FundoDePaisagemCoberturaTests
    {
        /// <summary>Relevo da borda (morros de ~3 u) sobe a silhueta na tela: equivale a ~5 u de folga.</summary>
        const float FolgaDoRelevo = 6f;
        const int Sementes = 24;
        static readonly float[] Aspectos = { 16f / 9f, 16f / 10f, 4f / 3f, 21f / 9f };

        float _alturaDoChao, _meiaLargura, _meiaProfundidade, _borda;
        int _raioMinimo, _raioMaximo, _deformacoes;
        float _margem, _inclinacao, _giro, _distancia;

        [OneTimeSetUp]
        public void LerCena()
        {
            CenaDoTeste.AbrirCenaPrincipal();
            var fundo = Object.FindFirstObjectByType<FundoDePaisagem>();
            var gerador = Object.FindFirstObjectByType<HexLoopGenerator>();
            var iso = Object.FindFirstObjectByType<HexIsoCamera>();
            Assert.IsNotNull(fundo); Assert.IsNotNull(gerador); Assert.IsNotNull(iso);
            _alturaDoChao = fundo.alturaDoChao; _meiaLargura = fundo.meiaLargura; _meiaProfundidade = fundo.meiaProfundidade;
            _borda = fundo.bordaDeChao;
            _raioMinimo = gerador.raioMinimo; _raioMaximo = gerador.raioMaximo; _deformacoes = gerador.deformacoes;
            _margem = iso.margem; _inclinacao = iso.inclinacao; _giro = iso.giro; _distancia = iso.distancia;
            CenaDoTeste.NovaCenaVazia();
        }

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        float PiorFolga(float meiaLargura, float meiaProfundidade, out string onde)
        {
            float pior = float.MaxValue;
            onde = null;
            for (int s = 1; s <= Sementes; s++)
            {
                var m = MundoDeTeste.Criar(s);
                m.gerador.raioMinimo = _raioMinimo;
                m.gerador.raioMaximo = _raioMaximo;
                m.gerador.deformacoes = _deformacoes;
                Assert.IsTrue(m.Gerar(), "semente " + s);

                var objeto = new GameObject("CameraDeTeste");
                var cam = objeto.AddComponent<Camera>();
                var iso = objeto.AddComponent<HexIsoCamera>();
                iso.mundo = m.mundo; iso.margem = _margem; iso.inclinacao = _inclinacao; iso.giro = _giro; iso.distancia = _distancia;
                foreach (float aspecto in Aspectos)
                {
                    cam.aspect = aspecto;
                    iso.Enquadrar();
                    Assert.IsTrue(iso.TryVistaBase(out Vector3 pos, out Quaternion rot, out float meia));
                    float folga = FundoDePaisagem.FolgaDaVista(pos, rot, meia, aspecto, _alturaDoChao, meiaLargura, meiaProfundidade);
                    if (folga < pior)
                    {
                        pior = folga;
                        onde = "semente " + s + ", volta " + m.gerador.Loop.Count + ", aspecto " + aspecto.ToString("0.00") +
                               ", orthoSize " + meia.ToString("0.0");
                    }
                }
                Object.DestroyImmediate(objeto);
                m.Destruir();
            }
            return pior;
        }

        [Test]
        public void VistaDoAnelInteiro_NuncaPassaDaBordaDoChao_EmNenhumaTela()
        {
            Assert.Greater(_borda, 0f, "sem bordaDeChao o 4:3 volta a mostrar a faixa");
            float pior = PiorFolga(_meiaLargura + _borda, _meiaProfundidade + _borda, out string onde);
            Assert.GreaterOrEqual(pior, FolgaDoRelevo, "a vista chega perto demais da borda do chao: " + onde);
        }

        [Test]
        public void SemABorda_O4Por3PassavaDoVale_ReproduzOProblema()
        {
            // Caracteriza o defeito corrigido: so o vale (+-64 x +-52) nao cobre a vista do anel de 30 em 4:3.
            float pior = PiorFolga(_meiaLargura, _meiaProfundidade, out string onde);
            Assert.Less(pior, FolgaDoRelevo, "o vale sozinho passou a cobrir todas as telas; a borda pode ser revista (" + onde + ")");
        }
    }
}
