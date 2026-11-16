using System.Collections.Generic;

namespace Loopia.Hex
{
    /// <summary>
    /// Texto mostrado ao jogador a partir dos motivos internos de MapaDePlataformas.PodeColocar. Os motivos
    /// continuam sendo dados de regra (testes e logs dependem deles); a HUD so troca a forma de apresentar:
    /// frase com inicial maiuscula, acentuacao e ponto final.
    /// </summary>
    public static class TextoDoJogador
    {
        static readonly Dictionary<string, string> Motivos = new Dictionary<string, string>
        {
            { "sem carta", "Escolha uma carta." },
            { "a jornada terminou", "A jornada terminou." },
            { "coloque uma ilha de mola primeiro", "Coloque uma Mola no mapa primeiro." },
            { "espere o Lucca sair desta ilha", "Espere o Lucca sair desta ilha." },
            { "espere o Lucca terminar o salto", "Espere o Lucca terminar o salto." },
            { "o acampamento nao pode ser substituido", "O acampamento não pode ser substituído." },
            { "esta carta so vai no caminho", "Esta carta só pode ser colocada no caminho." },
            { "esta carta so vai fora do caminho", "Esta carta só pode ser colocada fora do caminho." },
            { "o miolo do anel e reservado ao Boss", "O centro do anel é reservado ao chefe." },
            { "o Lucca precisa passar por uma mola antes de cada subida alta",
                "O Lucca precisa passar por uma Mola antes de cada subida alta." },
            { "fora do mapa", "Aponte para uma casa do tabuleiro" },
            // Instrucoes curtas da HUD ficam sem ponto (o teste das cartas do Codex fixa este texto).
            { "Solte para construir", "Solte para construir" },
            { "Aponte para fora da mão", "Aponte para fora da mão" },
            { "Aponte para uma casa do tabuleiro", "Aponte para uma casa do tabuleiro" },
        };

        const string NaoSubstitui = " nao pode ser substituida";

        public static string Motivo(string motivo)
        {
            if (string.IsNullOrEmpty(motivo)) return motivo;
            if (Motivos.TryGetValue(motivo, out string texto)) return texto;
            if (motivo.EndsWith(NaoSubstitui))
                return motivo.Substring(0, motivo.Length - NaoSubstitui.Length) + " não pode ser substituída.";
            return Frase(motivo);
        }

        /// <summary>Inicial maiuscula e ponto final, sem mexer no resto.</summary>
        public static string Frase(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            texto = char.ToUpperInvariant(texto[0]) + texto.Substring(1);
            char fim = texto[texto.Length - 1];
            return fim == '.' || fim == '!' || fim == '?' ? texto : texto + ".";
        }
    }
}
