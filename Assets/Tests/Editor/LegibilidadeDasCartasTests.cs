using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Legibilidade das 14 cartas em 1080p: titulo da carta compacta, tooltip ampliado inteiro (nome, resumo/valor,
    /// efeito, regra e condicao) e cobertura de caracteres das fontes para todo texto mostrado ao jogador.
    /// </summary>
    [Category("SceneStatic")]
    public class LegibilidadeDasCartasTests
    {
        const string Arte = "Assets/Arte/Cartas/";
        CatalogoVisualDeCartas _catalogo;
        CartaVisual _prefab;
        GameObject _canvas;

        [SetUp]
        public void Preparar()
        {
            CenaDoTeste.NovaCenaVazia();
            _catalogo = AssetDatabase.LoadAssetAtPath<CatalogoVisualDeCartas>(Arte + "Catalogo.asset");
            _prefab = AssetDatabase.LoadAssetAtPath<CartaVisual>(Arte + "Carta.prefab");
            Assert.IsNotNull(_catalogo);
            Assert.IsNotNull(_prefab);
            _canvas = new GameObject("Canvas de teste", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(1920, 1080);
        }

        [TearDown]
        public void Limpar()
        {
            if (_canvas != null) UnityEngine.Object.DestroyImmediate(_canvas);
            CenaDoTeste.Restaurar();
        }

        static float EscalaCompacta =>
            (float)typeof(HudDeCartas).GetField("EscalaCompacta", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        static void Conferir(TMP_Text texto, string onde)
        {
            texto.ForceMeshUpdate(true, true);
            Assert.IsFalse(texto.isTextOverflowing, onde + ": texto transborda: " + texto.text);
            Assert.IsFalse(texto.isTextTruncated, onde + ": texto truncado: " + texto.text);
            Assert.AreEqual(texto.text.Length, texto.textInfo.characterCount, onde + ": caracteres perdidos");
        }

        [Test]
        public void CartaCompacta_TitulosCabemNaPlaca_ELegiveisEm1080p()
        {
            float escala = EscalaCompacta;
            Assert.GreaterOrEqual(escala, .58f, "a mao ficou pequena demais de novo");
            var carta = UnityEngine.Object.Instantiate(_prefab, _canvas.transform, false);
            carta.Caixa.localScale = Vector3.one * escala;
            carta.Compactar(true);
            var titulo = carta.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Titulo");
            var valor = carta.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Valor");
            var resumo = carta.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Efeito compacto");

            var tamanhos = new List<string>();
            foreach (ArteDeCarta arte in _catalogo.cartas)
            {
                carta.Exibir(arte.carta, arte);
                Conferir(titulo, arte.titulo);
                float px = titulo.fontSize * escala;
                tamanhos.Add(arte.titulo + " " + px.ToString("F1") + "px");
                Assert.GreaterOrEqual(px, 11f, arte.titulo + ": titulo abaixo de 11 px em 1080p");
                if (arte.titulo.Length <= 12)
                    Assert.GreaterOrEqual(px, 14f, arte.titulo + ": nome curto deveria ter 14 px ou mais");
                Assert.IsFalse(resumo.gameObject.activeSelf, "o resumo minusculo sai da mao (vai para o tooltip)");
                if (!string.IsNullOrEmpty(arte.valor))
                {
                    Conferir(valor, arte.titulo + " valor");
                    Assert.GreaterOrEqual(valor.fontSize * escala, 10f, arte.titulo + ": valor ilegivel");
                }
            }
            Debug.Log("[Legibilidade] titulos compactos: " + string.Join(", ", tamanhos));

            carta.Exibir(null, null);
            var livre = carta.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Espaco livre");
            Assert.IsNotNull(livre, "espaco vazio com indicacao propria");
            Assert.IsTrue(livre.gameObject.activeSelf);
            Assert.IsTrue(carta.Vazio);

            carta.Compactar(false);
            Assert.IsTrue(resumo.gameObject.activeSelf, "a carta cheia (previa do tooltip) mantem o resumo");
        }

        [Test]
        public void TooltipAmpliado_MostraTudoSemCorteNas14Cartas()
        {
            Type tipo = typeof(HudDeCartas).Assembly.GetType("Loopia.Hex.TooltipDeCarta");
            var raiz = new GameObject("Mao", typeof(RectTransform));
            raiz.transform.SetParent(_canvas.transform, false);
            object tooltip = Activator.CreateInstance(tipo, (RectTransform)raiz.transform, _prefab, _catalogo);
            MethodInfo mostrar = tipo.GetMethod("Mostrar");
            var textos = (TextMeshProUGUI[])tipo.GetProperty("Textos", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(tooltip);
            RectTransform painel = (RectTransform)textos[0].transform.parent;

            foreach (ArteDeCarta arte in _catalogo.cartas)
            {
                mostrar.Invoke(tooltip, new object[] { arte.carta, arte, true });
                Assert.AreEqual(arte.titulo, textos[0].text);
                StringAssert.Contains(arte.carta.descricao, textos[2].text);
                if (!string.IsNullOrEmpty(arte.valor)) StringAssert.Contains(arte.valor, textos[1].text, arte.titulo + ": valor");
                if (!string.IsNullOrEmpty(arte.condicao)) StringAssert.Contains(arte.condicao, textos[3].text, arte.titulo + ": condicao");
                StringAssert.StartsWith("Coloque", textos[3].text, arte.titulo + ": regra de posicionamento");
                Canvas.ForceUpdateCanvases();
                float baseDoPainel = -painel.rect.height;
                foreach (var t in textos.Where(t => t.gameObject.activeSelf))
                {
                    Conferir(t, arte.titulo + "/" + t.name);
                    Assert.GreaterOrEqual(t.fontSize, t.name == "Nome" ? 22f : 15f, arte.titulo + "/" + t.name + ": letra pequena");
                    float fundo = t.rectTransform.anchoredPosition.y - t.preferredHeight;
                    Assert.GreaterOrEqual(fundo, baseDoPainel + 2f, arte.titulo + "/" + t.name + ": sai do painel");
                }
            }
        }

        [Test]
        public void Fontes_CobremTodoTextoMostradoAoJogador()
        {
            var textos = new List<(string texto, TMP_FontAsset fonte, string onde)>();
            foreach (ArteDeCarta a in _catalogo.cartas)
            {
                textos.Add((a.titulo, _catalogo.fonteTitulo, "titulo " + a.titulo));
                foreach (string t in new[] { a.resumo, a.valor, a.condicao, a.carta.descricao, a.carta.nomeExibido })
                    if (!string.IsNullOrEmpty(t)) textos.Add((t, _catalogo.fonteCorpo, a.titulo));
            }
            foreach (string motivo in new[]
            {
                "esta carta so vai no caminho", "esta carta so vai fora do caminho", "o miolo do anel e reservado ao Boss",
                "o acampamento nao pode ser substituido", "Básica nao pode ser substituida", "a jornada terminou",
                "coloque uma ilha de mola primeiro", "espere o Lucca sair desta ilha", "espere o Lucca terminar o salto",
                "o Lucca precisa passar por uma mola antes de cada subida alta", "Solte para construir", "Aponte para fora da mão",
            })
                textos.Add((TextoDoJogador.Motivo(motivo), _catalogo.fonteCorpo, "motivo"));
            textos.Add(("Morcego Ancião", _catalogo.fonteTitulo, "chefe"));
            textos.Add(("Espaço\nlivre", _catalogo.fonteTitulo, "espaco livre"));
            textos.Add(("+1 pendente · Moedas nesta jornada · 12 / 4 …", _catalogo.fonteCorpo, "interface"));
            textos.Add(("Chefe derrotado · Lucca venceu depois de 4 voltas …", _catalogo.fonteTitulo, "titulos"));
            textos.Add(("ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç+%,.;:!?()/", _catalogo.fonteTitulo, "acentos titulo"));
            textos.Add(("ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç+%,.;:!?()/…·×", _catalogo.fonteCorpo, "acentos corpo"));

            var faltas = new List<string>();
            foreach (var (texto, fonte, onde) in textos)
                foreach (char c in texto.Where(c => !char.IsWhiteSpace(c)).Distinct())
                    if (!fonte.HasCharacter(c, true, false))
                        faltas.Add($"{onde}: '{c}' (U+{(int)c:X4}) em {fonte.name}");
            CollectionAssert.IsEmpty(faltas.Distinct().ToList(), "caracteres sem glifo (nem nos fallbacks)");

            foreach (var (texto, _, onde) in textos)
            {
                StringAssert.DoesNotContain("Anciao", texto, onde);
                StringAssert.DoesNotContain("coracao", texto, onde);
                StringAssert.DoesNotContain("maximo", texto, onde);
                StringAssert.DoesNotContain(" so vai", texto, onde);
            }
        }
    }
}
