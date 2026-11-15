using System;

namespace Loopia.Hex
{
    /// <summary>
    /// Maquina de estados da jornada, sem nada de Unity: so as transicoes permitidas. O LoopDirector
    /// e o dono dela e decide quando pedir cada transicao; aqui so se garante que:
    ///
    /// - a ordem e Preparando -> Correndo -> Chefe -> Vitoria;
    /// - a Derrota pode vir de qualquer estado que ainda nao terminou;
    /// - Vitoria e Derrota sao terminais e mutuamente exclusivas (vale a primeira);
    /// - cada transicao acontece no maximo uma vez (pedir de novo devolve false e nao dispara evento).
    /// </summary>
    public sealed class CicloDaPartida
    {
        public EstadoDaPartida Estado { get; private set; } = EstadoDaPartida.Preparando;

        /// <summary>De onde a partida terminou (Correndo ou Chefe, por exemplo). Igual ao Estado enquanto nao terminou.</summary>
        public EstadoDaPartida EstadoAntesDoFim { get; private set; } = EstadoDaPartida.Preparando;

        public bool Terminou => EhTerminal(Estado);

        /// <summary>So em Correndo uma passagem pelo acampamento vale como volta.</summary>
        public bool ContaVoltas => Estado == EstadoDaPartida.Correndo;

        /// <summary>Disparado a cada transicao aceita: (anterior, novo).</summary>
        public event Action<EstadoDaPartida, EstadoDaPartida> AoMudar;

        public static bool EhTerminal(EstadoDaPartida estado) =>
            estado == EstadoDaPartida.Vitoria || estado == EstadoDaPartida.Derrota;

        public static bool TransicaoValida(EstadoDaPartida de, EstadoDaPartida para)
        {
            if (EhTerminal(de)) return false;

            switch (para)
            {
                case EstadoDaPartida.Correndo: return de == EstadoDaPartida.Preparando;
                case EstadoDaPartida.Chefe: return de == EstadoDaPartida.Correndo;
                case EstadoDaPartida.Vitoria: return de == EstadoDaPartida.Chefe;
                case EstadoDaPartida.Derrota: return true;
                default: return false;
            }
        }

        public bool Comecar() => Mudar(EstadoDaPartida.Correndo);

        public bool ComecarChefe() => Mudar(EstadoDaPartida.Chefe);

        public bool RegistrarVitoria() => Mudar(EstadoDaPartida.Vitoria);

        public bool RegistrarDerrota() => Mudar(EstadoDaPartida.Derrota);

        bool Mudar(EstadoDaPartida novo)
        {
            if (!TransicaoValida(Estado, novo)) return false;

            EstadoDaPartida anterior = Estado;
            Estado = novo;
            EstadoAntesDoFim = EhTerminal(novo) ? anterior : novo;
            AoMudar?.Invoke(anterior, novo);
            return true;
        }
    }
}
