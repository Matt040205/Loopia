using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Escala da HUD IMGUI (painel do LoopDirector, barra de cartas, barra do chefe, dica do zoom) em telas maiores
    /// que 1080p (Sprint 4). O layout foi desenhado em pixels fixos (fonte 10, painel 172x168, cartas ate 96 px) e
    /// ficava minusculo em 1440p/4K. Ate 1080p nada muda: o fator nunca e menor que 1.
    /// </summary>
    public static class EscalaDaInterface
    {
        public const float AlturaDeReferencia = 1080f;

        /// <summary>Fator de escala para uma tela desta altura em pixels: altura/1080, nunca menor que 1.</summary>
        public static float Fator(float alturaDaTela) => Mathf.Max(1f, alturaDaTela / AlturaDeReferencia);

        /// <summary>
        /// Aplica o fator ao <see cref="GUI.matrix"/> (o IMGUI converte o mouse sozinho) e devolve a matriz anterior,
        /// que o OnGUI restaura no fim. <paramref name="larguraLogica"/>/<paramref name="alturaLogica"/> substituem
        /// Screen.width/Screen.height no layout.
        /// </summary>
        public static Matrix4x4 Aplicar(out float larguraLogica, out float alturaLogica)
        {
            float fator = Fator(Screen.height);
            Matrix4x4 anterior = GUI.matrix;
            GUI.matrix = anterior * Matrix4x4.Scale(new Vector3(fator, fator, 1f));
            larguraLogica = Screen.width / fator;
            alturaLogica = Screen.height / fator;
            return anterior;
        }
    }
}
