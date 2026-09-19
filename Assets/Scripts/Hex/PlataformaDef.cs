using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Onde a carta pode ser solta, seguindo a marcacao (caminho) / (fora) do GDD.</summary>
    public enum ColocacaoPermitida
    {
        /// <summary>So em cima de uma casa do anel.</summary>
        NoCaminho,

        /// <summary>So fora do anel, em espaco vazio.</summary>
        ForaDoCaminho,

        /// <summary>Tanto faz.</summary>
        Qualquer,
    }

    /// <summary>
    /// Uma carta de plataforma do GDD. Cada tipo de efeito e uma subclasse, entao acrescentar
    /// plataforma nova e criar uma subclasse pequena em vez de engordar um switch gigante.
    ///
    /// Os metodos virtuais sao os ganchos em que as plataformas se penduram: o que fazem ao
    /// serem colocadas, quando o Lucca passa por cima, que bonus somam, e o que acontece a
    /// cada volta fechada.
    /// </summary>
    public abstract class PlataformaDef : ScriptableObject
    {
        [Header("Carta")]
        public string nomeExibido = "Plataforma";

        [TextArea(2, 4)]
        public string descricao = "";

        public Color cor = Color.white;

        [Header("Regras de colocacao")]
        public ColocacaoPermitida onde = ColocacaoPermitida.NoCaminho;

        [Tooltip("Uma carta nova pode ser solta por cima desta? A basica do GDD existe " +
                 "justamente para o jogador substituir depois.")]
        public bool podeSerSubstituida = true;

        // --- Ganchos ---

        /// <summary>Bonus de PV maximo que esta plataforma soma ao Lucca.</summary>
        public virtual int BonusDePv(Plataforma plataforma) => 0;

        /// <summary>Bonus de dano das flechas, em fracao (0.08 = +8%).</summary>
        public virtual float BonusDeDano(Plataforma plataforma) => 0f;

        /// <summary>Chamado quando o Lucca termina de entrar nesta casa.</summary>
        public virtual void AoPlayerPassar(Plataforma plataforma, LuccaStatus status) { }

        /// <summary>Chamado em toda plataforma do mapa quando uma volta fecha.</summary>
        public virtual void AoFecharVolta(Plataforma plataforma) { }

        /// <summary>Chamado logo depois de a plataforma ser colocada no mapa.</summary>
        public virtual void AoColocar(Plataforma plataforma) { }

        /// <summary>Chamado antes de a plataforma sair do mapa.</summary>
        public virtual void AoRemover(Plataforma plataforma) { }
    }
}
