using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Plataforma de bonus fixo. Cobre a basica (0 de tudo), o campo pacifico (+2 PV)
    /// e a torre (+8% de dano) do GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Bonus simples")]
    public class PlataformaSimplesDef : PlataformaDef
    {
        [Header("Bonus")]
        [Tooltip("Quanto de PV maximo esta plataforma soma ao Lucca.")]
        public int bonusDePv = 0;

        [Tooltip("Quanto de dano soma as flechas, em fracao. 0.08 = +8%.")]
        public float bonusDeDano = 0f;

        public override int BonusDePv(Plataforma plataforma) => bonusDePv;

        public override float BonusDeDano(Plataforma plataforma) => bonusDeDano;
    }

    /// <summary>
    /// Plataforma cujo bonus cresce conforme as vizinhas. Cobre a cabana ("+2 de PV; se tiver
    /// campo pacifico em area adjacente 3x3, +2 por campo pacifico") e a area de treino
    /// ("+8% de dano; se tiver torre em area adjacente 3x3, +15% por torre").
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Bonus por vizinhanca")]
    public class PlataformaVizinhancaDef : PlataformaSimplesDef
    {
        [Header("Vizinhanca")]
        [Tooltip("Que tipo de plataforma conta como vizinha boa.")]
        public PlataformaDef tipoQueConta;

        [Tooltip("Alcance em hexagonos. O GDD fala em 3x3 e 5x5, pensando em grade quadrada; " +
                 "no hexagono isso vira 1 (as 6 vizinhas) e 2 (as 18 em volta).")]
        [Min(1)] public int raio = 1;

        public int pvPorVizinha = 0;
        public float danoPorVizinha = 0f;

        public override int BonusDePv(Plataforma plataforma)
        {
            return bonusDePv + pvPorVizinha * ContarVizinhas(plataforma);
        }

        public override float BonusDeDano(Plataforma plataforma)
        {
            return bonusDeDano + danoPorVizinha * ContarVizinhas(plataforma);
        }

        int ContarVizinhas(Plataforma plataforma)
        {
            if (tipoQueConta == null || plataforma == null || plataforma.Mapa == null) return 0;
            return plataforma.Mapa.ContarPorTipo(tipoQueConta, plataforma.Casa, raio);
        }
    }

    /// <summary>
    /// Plataforma de recompensa: o Lucca pega ao passar por cima. Cobre coracao (+5 PV),
    /// flechas (+10) e moedas (+3) do GDD.
    /// </summary>
    [CreateAssetMenu(fileName = "Plataforma", menuName = "Loopia/Plataforma/Recompensa")]
    public class PlataformaRecompensaDef : PlataformaDef
    {
        [Header("O que o Lucca pega ao passar")]
        [Tooltip("GDD: o coracao restaura +5 de PV.")]
        public int cura = 0;

        [Tooltip("GDD: o item flecha adiciona 10 flechas.")]
        public int flechas = 0;

        [Tooltip("GDD: a plataforma com moedas adiciona 3 moedas.")]
        public int moedas = 0;

        [Tooltip("Ligado: volta a valer a cada volta. Desligado: so da a recompensa uma vez.")]
        public bool recarregaACadaVolta = true;

        public override void AoColocar(Plataforma plataforma) => plataforma.Carregada = true;

        public override void AoFecharVolta(Plataforma plataforma)
        {
            if (recarregaACadaVolta) plataforma.Carregada = true;
        }

        public override void AoPlayerPassar(Plataforma plataforma, LuccaStatus status)
        {
            if (!plataforma.Carregada || status == null) return;

            plataforma.Carregada = false;

            status.Curar(cura);
            status.GanharFlechas(flechas);
            status.GanharMoedas(moedas);
        }
    }

    /// <summary>
    /// Floresta: "essa plataforma faz que todo loop tenha lobos nessa area".
    /// A cada volta fechada ela repovoa a propria casa, se estiver vazia.
    /// </summary>
    [CreateAssetMenu(fileName = "Floresta", menuName = "Loopia/Plataforma/Floresta")]
    public class PlataformaFlorestaDef : PlataformaDef
    {
        [Header("Lobos")]
        public InimigoDef loboQueNasce;

        [Tooltip("Quantos lobos esta casa mantem. O GDD diz que 'todo loop tenha lobos nessa " +
                 "area', e nao que se acumule mais um por volta: a floresta repoe ate este " +
                 "numero, em vez de empilhar para sempre.")]
        [Min(1)] public int lobosNaCasa = 1;

        [Tooltip("Ligado: ja nasce um lobo no instante em que a carta e colocada.")]
        public bool nascerAoColocar = true;

        public override void AoColocar(Plataforma plataforma)
        {
            if (nascerAoColocar) Povoar(plataforma);
        }

        public override void AoFecharVolta(Plataforma plataforma) => Povoar(plataforma);

        void Povoar(Plataforma plataforma)
        {
            if (loboQueNasce == null || plataforma == null || plataforma.Mapa == null) return;

            InimigoSpawner spawner = plataforma.Mapa.inimigos;
            if (spawner == null) return;

            // Repoe so o que falta: sem isto, cada volta empilharia mais um lobo eternamente.
            int faltam = lobosNaCasa - spawner.QuantosEm(plataforma.Casa);
            for (int i = 0; i < faltam; i++)
            {
                spawner.Nascer(loboQueNasce, plataforma.Casa);
            }
        }
    }
}
