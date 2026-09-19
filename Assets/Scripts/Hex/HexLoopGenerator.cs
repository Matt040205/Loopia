using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Gera o anel do GDD: um caminho fechado de 1 hexagono de largura, com formato sorteado a
    /// cada partida — pode sair redondo, quadradao ou uma forma sem nome — desde que sobre miolo
    /// suficiente para o Boss caber (no minimo uma area de 3x3 hexagonos).
    ///
    /// Como funciona:
    /// 1. parte de um anel hexagonal perfeito, que ja e um ciclo valido;
    /// 2. deforma esse anel muitas vezes, trocando "quinas" de lugar;
    /// 3. confere se o miolo que sobrou da para o Boss; se nao der, sorteia tudo de novo.
    ///
    /// A troca de quina e o que mantem tudo correto. Uma casa B do ciclo, entre A e C, so pode
    /// ser trocada pela OUTRA casa vizinha comum de A e C. Assim toda casa continua tendo
    /// exatamente 2 vizinhas no caminho, ou seja: o anel nunca se cruza, nunca se parte e
    /// continua sendo uma volta unica, por mais torto que fique.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexLoopGenerator : MonoBehaviour
    {
        [Header("Mundo")]
        [Tooltip("Deixe vazio para achar o HexWorld da cena automaticamente.")]
        public HexWorld mundo;

        [Header("Tamanho do anel")]
        [Tooltip("Raio minimo do anel de partida. O caminho sai com 6 x raio casas.")]
        [Min(2)] public int raioMinimo = 4;

        [Tooltip("Raio maximo do anel de partida.")]
        [Min(2)] public int raioMaximo = 6;

        [Header("Formato")]
        [Tooltip("Quantas trocas de quina tentar. 0 deixa um hexagono perfeito; " +
                 "quanto mais, mais irregular fica o formato.")]
        [Min(0)] public int deformacoes = 400;

        [Tooltip("Quantos hexagonos o miolo precisa ter para o Boss caber. O GDD pede ~3x3.")]
        [Min(1)] public int mioloMinimo = 9;

        [Header("Aleatoriedade")]
        [Tooltip("Ligue para sempre gerar o mesmo mapa: util para comparar testes.")]
        public bool usarSementeFixa = false;
        public int semente = 12345;

        readonly List<HexCoord> _miolo = new List<HexCoord>();

        /// <summary>O caminho gerado. Fica null enquanto a geracao nao rodar.</summary>
        public HexLoopPath Loop { get; private set; }

        /// <summary>As casas do miolo, reservadas para o Boss.</summary>
        public IReadOnlyList<HexCoord> Miolo => _miolo;

        /// <summary>Casa mais ao centro do miolo: onde o Boss deve nascer.</summary>
        public HexCoord CentroDoMiolo { get; private set; }

        /// <summary>
        /// Disparado sempre que um mapa novo e sorteado. Quem guardar indice ou posicao do anel
        /// antigo precisa ouvir isso para se reamarrar, senao fica apontando para casas que
        /// deixaram de existir.
        /// </summary>
        public event System.Action AoGerarMapa;

        void Awake()
        {
            // Roda no Awake para que o HexPlayer ja encontre o acampamento no Start dele.
            Gerar();
        }

        [ContextMenu("Gerar mapa")]
        public bool Gerar()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (mundo == null)
            {
                Debug.LogError("[HexLoopGenerator] Nenhum HexWorld encontrado na cena.", this);
                return false;
            }

            mundo.GarantirPreparado();

            System.Random sorteio = usarSementeFixa
                ? new System.Random(semente)
                : new System.Random();

            List<HexCoord> ciclo = null;
            List<HexCoord> miolo = null;

            const int maxTentativas = 40;
            for (int tentativa = 0; tentativa < maxTentativas; tentativa++)
            {
                int raio = sorteio.Next(Mathf.Max(2, raioMinimo), Mathf.Max(raioMinimo, raioMaximo) + 1);

                List<HexCoord> candidato = Anel(HexCoord.Zero, raio);
                Deformar(candidato, sorteio);

                string erro;
                if (!CicloEhValido(candidato, out erro)) continue;

                List<HexCoord> mioloCandidato = CalcularMiolo(candidato);
                if (!MioloCabeOBoss(mioloCandidato)) continue;

                ciclo = candidato;
                miolo = mioloCandidato;
                break;
            }

            if (ciclo == null)
            {
                Debug.LogError(
                    "[HexLoopGenerator] Nao consegui gerar um anel com miolo suficiente. " +
                    "Aumente raioMinimo ou diminua mioloMinimo.", this);
                return false;
            }

            // O acampamento cai num ponto aleatorio do anel, e a volta passa a comecar por ele.
            GirarParaComecarEm(ciclo, sorteio.Next(ciclo.Count));

            Loop = new HexLoopPath();
            Loop.Definir(ciclo);

            _miolo.Clear();
            _miolo.AddRange(miolo);
            CentroDoMiolo = AcharCentro(_miolo);

            MontarNoMundo();

            AoGerarMapa?.Invoke();
            return true;
        }

        // --- Anel inicial ---

        /// <summary>As 6R casas a exatamente "raio" de distancia do centro, ja em ordem de caminhada.</summary>
        static List<HexCoord> Anel(HexCoord centro, int raio)
        {
            var casas = new List<HexCoord>(6 * raio);

            // Comeca numa quina e anda pelos 6 lados. Estando na quina da direcao L,
            // o lado seguinte do anel e percorrido na direcao L+2.
            HexCoord atual = centro + HexCoord.Directions[0] * raio;
            for (int lado = 0; lado < 6; lado++)
            {
                for (int passo = 0; passo < raio; passo++)
                {
                    casas.Add(atual);
                    atual = atual.Neighbor((lado + 2) % 6);
                }
            }

            return casas;
        }

        // --- Deformacao ---

        void Deformar(List<HexCoord> ciclo, System.Random sorteio)
        {
            var noCaminho = new HashSet<HexCoord>(ciclo);

            for (int tentativa = 0; tentativa < deformacoes; tentativa++)
            {
                TentarTrocarQuina(ciclo, noCaminho, sorteio.Next(ciclo.Count));
            }
        }

        /// <summary>
        /// Troca a casa da posicao "i" pela outra vizinha comum das casas ao lado dela,
        /// empurrando aquela quina para dentro ou para fora. Devolve false quando a troca
        /// nao existe ou quebraria o ciclo.
        /// </summary>
        static bool TentarTrocarQuina(List<HexCoord> ciclo, HashSet<HexCoord> noCaminho, int i)
        {
            int total = ciclo.Count;
            HexCoord b = ciclo[i];
            HexCoord a = ciclo[(i - 1 + total) % total];
            HexCoord c = ciclo[(i + 1) % total];

            // A outra vizinha comum de A e C. Em trecho reto so existe B, e nao da para trocar.
            HexCoord m = default(HexCoord);
            int candidatas = 0;
            for (int d = 0; d < 6; d++)
            {
                HexCoord candidata = a.Neighbor(d);
                if (candidata == b) continue;
                if (HexCoord.Distance(candidata, c) != 1) continue;

                m = candidata;
                candidatas++;
            }
            if (candidatas != 1) return false;
            if (noCaminho.Contains(m)) return false;

            // M so pode encostar em A e em C. Encostando em mais alguem, o caminho se cruzaria.
            int vizinhasNoCaminho = 0;
            for (int d = 0; d < 6; d++)
            {
                HexCoord vizinha = m.Neighbor(d);
                if (vizinha == b) continue; // B sai do caminho nesta mesma troca
                if (noCaminho.Contains(vizinha)) vizinhasNoCaminho++;
            }
            if (vizinhasNoCaminho != 2) return false;

            ciclo[i] = m;
            noCaminho.Remove(b);
            noCaminho.Add(m);
            return true;
        }

        static void GirarParaComecarEm(List<HexCoord> ciclo, int indice)
        {
            if (indice <= 0 || indice >= ciclo.Count) return;

            var girado = new List<HexCoord>(ciclo.Count);
            for (int i = 0; i < ciclo.Count; i++)
            {
                girado.Add(ciclo[(indice + i) % ciclo.Count]);
            }

            ciclo.Clear();
            ciclo.AddRange(girado);
        }

        // --- Miolo ---

        /// <summary>
        /// Descobre o que ficou dentro do anel. Em vez de tentar adivinhar o "dentro", inunda o
        /// lado de FORA a partir da borda de uma caixa folgada: o que a inundacao nao alcancar e
        /// nao for o proprio anel esta, por eliminacao, cercado.
        /// </summary>
        static List<HexCoord> CalcularMiolo(List<HexCoord> ciclo)
        {
            var anel = new HashSet<HexCoord>(ciclo);

            int minQ = int.MaxValue, maxQ = int.MinValue;
            int minR = int.MaxValue, maxR = int.MinValue;
            for (int i = 0; i < ciclo.Count; i++)
            {
                minQ = Mathf.Min(minQ, ciclo[i].q);
                maxQ = Mathf.Max(maxQ, ciclo[i].q);
                minR = Mathf.Min(minR, ciclo[i].r);
                maxR = Mathf.Max(maxR, ciclo[i].r);
            }

            // Folga de 3 casas: garante que a borda da caixa esteja toda fora do anel e ligada.
            minQ -= 3; maxQ += 3;
            minR -= 3; maxR += 3;

            var deFora = new HashSet<HexCoord>();
            var fila = new Queue<HexCoord>();

            var inicio = new HexCoord(minQ, minR);
            deFora.Add(inicio);
            fila.Enqueue(inicio);

            while (fila.Count > 0)
            {
                HexCoord atual = fila.Dequeue();
                for (int d = 0; d < 6; d++)
                {
                    HexCoord vizinha = atual.Neighbor(d);

                    if (vizinha.q < minQ || vizinha.q > maxQ) continue;
                    if (vizinha.r < minR || vizinha.r > maxR) continue;
                    if (anel.Contains(vizinha)) continue;
                    if (!deFora.Add(vizinha)) continue;

                    fila.Enqueue(vizinha);
                }
            }

            var miolo = new List<HexCoord>();
            for (int q = minQ; q <= maxQ; q++)
            {
                for (int r = minR; r <= maxR; r++)
                {
                    var casa = new HexCoord(q, r);
                    if (anel.Contains(casa)) continue;
                    if (deFora.Contains(casa)) continue;

                    miolo.Add(casa);
                }
            }

            return miolo;
        }

        /// <summary>
        /// O Boss precisa de espaco de verdade, nao de uma fresta. Alem da contagem minima,
        /// exige pelo menos uma casa com as 6 vizinhas tambem no miolo — o equivalente hexagonal
        /// de caber um bloco 3x3.
        /// </summary>
        bool MioloCabeOBoss(List<HexCoord> miolo)
        {
            if (miolo.Count < mioloMinimo) return false;

            var casas = new HashSet<HexCoord>(miolo);
            for (int i = 0; i < miolo.Count; i++)
            {
                bool cercada = true;
                for (int d = 0; d < 6 && cercada; d++)
                {
                    if (!casas.Contains(miolo[i].Neighbor(d))) cercada = false;
                }
                if (cercada) return true;
            }

            return false;
        }

        /// <summary>Casa do miolo mais proxima da media de todas: o lugar natural para o Boss.</summary>
        static HexCoord AcharCentro(List<HexCoord> casas)
        {
            if (casas.Count == 0) return HexCoord.Zero;

            float somaQ = 0f, somaR = 0f;
            for (int i = 0; i < casas.Count; i++)
            {
                somaQ += casas[i].q;
                somaR += casas[i].r;
            }

            HexCoord media = HexCoord.Round(somaQ / casas.Count, somaR / casas.Count);

            HexCoord melhor = casas[0];
            int menorDistancia = int.MaxValue;
            for (int i = 0; i < casas.Count; i++)
            {
                int distancia = HexCoord.Distance(media, casas[i]);
                if (distancia >= menorDistancia) continue;

                menorDistancia = distancia;
                melhor = casas[i];
            }

            return melhor;
        }

        // --- Conferencia ---

        /// <summary>
        /// Confere que o resultado e mesmo uma volta unica e andavel: sem casa repetida, cada casa
        /// vizinha da seguinte, a ultima vizinha da primeira, e ninguem com mais de duas vizinhas
        /// dentro do proprio caminho (o que seria um atalho).
        /// </summary>
        public static bool CicloEhValido(IList<HexCoord> ciclo, out string erro)
        {
            erro = null;

            if (ciclo == null || ciclo.Count < 6)
            {
                erro = "o caminho tem menos de 6 casas";
                return false;
            }

            var casas = new HashSet<HexCoord>(ciclo);
            if (casas.Count != ciclo.Count)
            {
                erro = "tem casa repetida no caminho";
                return false;
            }

            for (int i = 0; i < ciclo.Count; i++)
            {
                HexCoord atual = ciclo[i];
                HexCoord proxima = ciclo[(i + 1) % ciclo.Count];

                if (HexCoord.Distance(atual, proxima) != 1)
                {
                    erro = "as casas " + atual + " e " + proxima + " nao sao vizinhas";
                    return false;
                }

                int vizinhas = 0;
                for (int d = 0; d < 6; d++)
                {
                    if (casas.Contains(atual.Neighbor(d))) vizinhas++;
                }
                if (vizinhas != 2)
                {
                    erro = "a casa " + atual + " tem " + vizinhas + " vizinhas no caminho (deveria ter 2)";
                    return false;
                }
            }

            return true;
        }

        // --- Mundo ---

        void MontarNoMundo()
        {
            mundo.LimparMundo();

            // Indice 0 e o acampamento: foi para la que o ciclo foi girado.
            for (int i = 0; i < Loop.Count; i++)
            {
                HexTileKind tipo = i == 0 ? HexTileKind.Acampamento : HexTileKind.Caminho;
                mundo.CriarIlha(Loop.Casas[i], tipo);
            }

            mundo.DefinirAcampamento(Loop[0]);
            mundo.DefinirBloqueadas(_miolo);
        }
    }
}
