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

        [Header("Modelo no centro")]
        public GameObject modelo;
        [Min(0.1f)] public float tamanhoDoModelo = 0.8f;
        [Min(0f)] public float alturaDoModelo = 0.25f;
        public bool girarModelo;

        [Header("Marcador da carta (Pass C)")]
        [Tooltip("Prefab em escala de mundo (1 u = altura do Lucca), pivo no centro e no topo da ilha, eixos do mundo. " +
                 "Vira filho da ilha (acompanha a altura dela), sem normalizacao de tamanho. Cartas do caminho: so baixo " +
                 "no centro e alto no lado longe da camera.")]
        public GameObject marcador;
        [Tooltip("Esconde a decoracao da ilha enquanto a carta estiver nela (o marcador a substitui).")]
        public bool ocultarDecoracao;

        [Header("Design")]
        [Tooltip("Fora do jogo por decisao de design: nao entra na mao inicial, no sorteio nem nas recompensas e nao pode " +
                 "ser colocada. Cartas que dependem dela tambem deixam de aparecer (sem Mola nao ha Ilha Alta).")]
        public bool desativada;

        public virtual bool DisponivelParaCompra(MapaDePlataformas mapa) => true;

        /// <summary>Pode aparecer na mao/sorteio e ser colocada agora: nao esta desativada e o mapa cumpre o pre-requisito.</summary>
        public bool PodeAparecer(MapaDePlataformas mapa) => !desativada && DisponivelParaCompra(mapa);

        // --- Ganchos ---

        /// <summary>Bonus de PV maximo que esta plataforma soma ao Lucca.</summary>
        public virtual int BonusDePv(Plataforma plataforma) => 0;

        /// <summary>Bonus de dano das flechas, em fracao (0.08 = +8%).</summary>
        public virtual float BonusDeDano(Plataforma plataforma) => 0f;

        /// <summary>Flechas maximas extras (G2: Oficina de Arqueiro).</summary>
        public virtual int BonusDeFlechas(Plataforma plataforma) => 0;

        /// <summary>PV extra a cada coracao coletado (G2: Fonte Restauradora).</summary>
        public virtual int BonusDeCuraPorCoracao(Plataforma plataforma) => 0;

        /// <summary>
        /// Tudo o que esta plataforma soma ao Lucca, montado a partir dos ganchos acima. O mapa soma isto;
        /// subclasses sobrescrevem os ganchos, nao este metodo.
        /// </summary>
        public ModificadoresDoLucca Modificadores(Plataforma plataforma)
        {
            return new ModificadoresDoLucca(BonusDePv(plataforma), BonusDeDano(plataforma),
                BonusDeFlechas(plataforma), BonusDeCuraPorCoracao(plataforma));
        }

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
