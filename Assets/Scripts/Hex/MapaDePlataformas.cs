using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Guarda o que foi construido em cada casa e faz valer os efeitos.
    ///
    /// E o unico lugar que decide se uma carta pode ser solta ali, entao a UI de arraste so
    /// pergunta e obedece. Os bonus de PV e de dano sao recalculados a cada mudanca, porque
    /// plataformas como a cabana dependem de quem esta em volta: colocar uma vizinha muda o
    /// bonus de quem ja estava no mapa.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapaDePlataformas : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;
        public HexPlayer player;
        public LuccaStatus status;
        public HexLoopRunner corredor;
        public HexLoopGenerator gerador;
        public InimigoSpawner inimigos;

        readonly Dictionary<HexCoord, Plataforma> _plataformas = new Dictionary<HexCoord, Plataforma>();

        Transform _raiz;
        bool _inscrito;

        /// <summary>Soma do bonus de dano de todas as plataformas, em fracao (0.16 = +16%).</summary>
        public float BonusDeDanoTotal { get; private set; }

        /// <summary>Soma do bonus de PV maximo de todas as plataformas.</summary>
        public int BonusDePvTotal { get; private set; }

        public int Quantas => _plataformas.Count;
        public IEnumerable<Plataforma> Todas => _plataformas.Values;
        public bool TemMola
        {
            get
            {
                foreach (var p in _plataformas.Values) if (p != null && p.Def is PlataformaMolaDef) return true;
                return false;
            }
        }

        public event Action AoMudar;

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (corredor == null) corredor = FindFirstObjectByType<HexLoopRunner>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();

            if (mundo == null || player == null)
            {
                Debug.LogError("[MapaDePlataformas] Faltou HexWorld ou HexPlayer na cena.", this);
                enabled = false;
                return;
            }

            _raiz = new GameObject("Plataformas").transform;
            _raiz.SetParent(transform, false);

            player.AoEntrarNoHexagono += PlayerPassouPor;
            if (corredor != null) corredor.AoCompletarVolta += VoltaFechou;
            if (gerador != null) gerador.AoGerarMapa += LimparTudo;
            _inscrito = true;
        }

        void OnDestroy()
        {
            if (!_inscrito) return;

            if (player != null) player.AoEntrarNoHexagono -= PlayerPassouPor;
            if (corredor != null) corredor.AoCompletarVolta -= VoltaFechou;
            if (gerador != null) gerador.AoGerarMapa -= LimparTudo;
            _inscrito = false;
        }

        // --- Consulta ---

        public bool TryGet(HexCoord casa, out Plataforma plataforma) => _plataformas.TryGetValue(casa, out plataforma);

        public bool Tem(HexCoord casa) => _plataformas.ContainsKey(casa);

        /// <summary>
        /// Quantas plataformas do tipo pedido existem em volta da casa, dentro do raio.
        /// A propria casa do centro nao entra na conta.
        /// </summary>
        public int ContarPorTipo(PlataformaDef tipo, HexCoord centro, int raio)
        {
            if (tipo == null) return 0;

            int total = 0;
            foreach (KeyValuePair<HexCoord, Plataforma> par in _plataformas)
            {
                if (par.Key == centro) continue;
                if (par.Value == null || par.Value.Def != tipo) continue;
                if (HexCoord.Distance(centro, par.Key) > raio) continue;

                total++;
            }
            return total;
        }

        /// <summary>
        /// Diz se a carta pode ser solta nesta casa e, se nao puder, por que. O motivo vai
        /// direto para a UI, para o jogador entender o que esta errado.
        /// </summary>
        public bool PodeColocar(PlataformaDef def, HexCoord casa, out string motivo)
        {
            motivo = null;
            if (def == null)
            {
                motivo = "sem carta";
                return false;
            }

            bool noAnel = mundo.TryGetIlha(casa, out HexTile ilha) && ilha.EstaNoCaminho;
            if (!def.DisponivelParaCompra(this))
            {
                motivo = "coloque uma ilha de mola primeiro";
                return false;
            }
            bool alteraAltura = def is PlataformaRelevoDef ||
                (_plataformas.TryGetValue(casa, out Plataforma anterior) && anterior.Def is PlataformaRelevoDef);
            if (alteraAltura && player != null &&
                (player.Coord == casa || (player.CaminhoAtual.Count > 0 && player.CaminhoAtual[0] == casa)))
            {
                motivo = "espere o Lucca sair desta ilha";
                return false;
            }
            if (alteraAltura && player != null && player.EstaPulando)
            {
                motivo = "espere o Lucca terminar o salto";
                return false;
            }

            // GDD: a barraca "nao pode ser removida ou substituida".
            if (noAnel && ilha.EhAcampamento)
            {
                motivo = "o acampamento nao pode ser substituido";
                return false;
            }

            switch (def.onde)
            {
                case ColocacaoPermitida.NoCaminho:
                    if (!noAnel)
                    {
                        motivo = "esta carta so vai no caminho";
                        return false;
                    }
                    break;

                case ColocacaoPermitida.ForaDoCaminho:
                    if (noAnel)
                    {
                        motivo = "esta carta so vai fora do caminho";
                        return false;
                    }
                    if (mundo.EstaBloqueada(casa))
                    {
                        motivo = "o miolo do anel e reservado ao Boss";
                        return false;
                    }
                    break;

                case ColocacaoPermitida.Qualquer:
                    if (!noAnel && mundo.EstaBloqueada(casa))
                    {
                        motivo = "o miolo do anel e reservado ao Boss";
                        return false;
                    }
                    break;
            }

            if (_plataformas.TryGetValue(casa, out Plataforma existente))
            {
                if (!existente.Def.podeSerSubstituida)
                {
                    motivo = existente.Def.nomeExibido + " nao pode ser substituida";
                    return false;
                }
            }

            if (!PercursoCompativel(def, casa))
            {
                motivo = "o Lucca precisa passar por uma mola antes de cada subida alta";
                return false;
            }
            return true;
        }

        // --- Colocar e remover ---

        public Plataforma Colocar(PlataformaDef def, HexCoord casa)
        {
            if (!PodeColocar(def, casa, out string motivo))
            {
                Debug.LogWarning("[MapaDePlataformas] Nao da para colocar " +
                                 (def != null ? def.nomeExibido : "null") + " em " + casa + ": " + motivo, this);
                return null;
            }

            Remover(casa);

            // Fora do anel a carta cria uma ilha nova; no anel ela so veste a ilha que ja existe.
            HexTile ilha;
            if (!mundo.TryGetIlha(casa, out ilha))
            {
                ilha = mundo.CriarIlha(casa, HexTileKind.Construida);
            }
            ilha.DefinirCor(def.cor);

            var go = new GameObject(def.nomeExibido);
            go.transform.SetParent(_raiz, false);
            go.transform.position = mundo.PosicaoDe(casa);

            var plataforma = go.AddComponent<Plataforma>();
            plataforma.Montar(def, casa, this);
            _plataformas[casa] = plataforma;

            def.AoColocar(plataforma);
            AtualizarRelevo();
            Recalcular();
            return plataforma;
        }

        public void Remover(HexCoord casa)
        {
            if (!_plataformas.TryGetValue(casa, out Plataforma plataforma)) return;

            _plataformas.Remove(casa);

            if (plataforma != null)
            {
                plataforma.Def.AoRemover(plataforma);
                Destroy(plataforma.gameObject);
            }

            // A ilha do anel fica; a que a carta criou some junto com ela.
            if (mundo.TryGetIlha(casa, out HexTile ilha))
            {
                if (ilha.Kind == HexTileKind.Construida) mundo.RemoverIlha(casa);
                else ilha.LimparCor();
            }

            AtualizarRelevo();
            Recalcular();
        }

        public void LimparTudo()
        {
            var casas = new List<HexCoord>(_plataformas.Keys);
            for (int i = 0; i < casas.Count; i++) Remover(casas[i]);

            _plataformas.Clear();
            Recalcular();
        }

        // Simula uma volta a partir do acampamento para impedir cartas que travariam o percurso.
        bool PercursoCompativel(PlataformaDef nova, HexCoord casa)
        {
            if (gerador == null || gerador.Loop == null) return true;
            if (!SimularPercurso(nova, casa, 0, 0f)) return false;
            if (player == null || player.EstaPulando || !gerador.Loop.TryIndiceDe(player.Coord, out int indice)) return true;
            return SimularPercurso(nova, casa, indice, player.AlturaDoImpulso);
        }

        PlataformaDef DefEm(HexCoord coord, PlataformaDef nova, HexCoord casa)
        {
            return coord == casa ? nova :
                (_plataformas.TryGetValue(coord, out Plataforma p) && p != null ? p.Def : null);
        }

        bool SimularPercurso(PlataformaDef nova, HexCoord casa, int inicio, float impulso)
        {
            int sentido = corredor != null && corredor.inverterSentido ? -1 : 1;
            var origem = DefEm(gerador.Loop[inicio], nova, casa);
            float alturaAnterior = origem is PlataformaRelevoDef alta ? alta.altura : 0f;
            for (int i = 1; i <= gerador.Loop.Count; i++)
            {
                HexCoord coord = gerador.Loop[inicio + i * sentido];
                PlataformaDef def = DefEm(coord, nova, casa);
                float altura = def is PlataformaRelevoDef relevo ? relevo.altura : 0f;
                float subida = altura - alturaAnterior;
                if (subida > (player != null ? player.alturaMaximaSemMola : 0.7f))
                {
                    if (impulso < subida) return false;
                    impulso = 0f;
                }
                if (def is PlataformaMolaDef mola) impulso = mola.alturaDoImpulso;
                alturaAnterior = altura;
            }
            return true;
        }

        void AtualizarRelevo()
        {
            bool mudou = false;
            bool molaPresente = TemMola;
            foreach (var ilha in mundo.Ilhas)
            {
                float altura = molaPresente && _plataformas.TryGetValue(ilha.Coord, out Plataforma p) &&
                    p.Def is PlataformaRelevoDef relevo ? relevo.altura : 0f;
                if (Mathf.Approximately(ilha.Altura, altura)) continue;
                ilha.DefinirAltura(altura);
                mudou = true;
            }
            foreach (var p in _plataformas.Values)
                if (p != null) p.transform.position = mundo.PosicaoDe(p.Casa);
            if (mudou)
            {
                var navegacao = mundo.GetComponent<NavegacaoDoMundo>();
                if (navegacao != null) navegacao.Reconstruir();
                if (inimigos != null)
                    foreach (var inimigo in inimigos.Vivos)
                        if (inimigo != null) inimigo.AjustarAoRelevo();
                var camera = FindFirstObjectByType<HexIsoCamera>();
                if (camera != null) camera.Enquadrar();
            }
        }

        // --- Efeitos ---

        void PlayerPassouPor(HexCoord casa)
        {
            if (!_plataformas.TryGetValue(casa, out Plataforma plataforma)) return;
            if (plataforma == null) return;

            plataforma.Def.AoPlayerPassar(plataforma, status);
        }

        void VoltaFechou(int volta)
        {
            // Copia porque um efeito de volta pode mexer no mapa enquanto a gente percorre.
            var todas = new List<Plataforma>(_plataformas.Values);
            for (int i = 0; i < todas.Count; i++)
            {
                if (todas[i] != null) todas[i].Def.AoFecharVolta(todas[i]);
            }
        }

        /// <summary>
        /// Refaz as somas. Precisa rodar a cada colocacao ou remocao porque o bonus de uma
        /// plataforma pode depender das vizinhas.
        /// </summary>
        void Recalcular()
        {
            int pv = 0;
            float dano = 0f;

            foreach (Plataforma plataforma in _plataformas.Values)
            {
                if (plataforma == null) continue;

                pv += plataforma.Def.BonusDePv(plataforma);
                dano += plataforma.Def.BonusDeDano(plataforma);
            }

            BonusDePvTotal = pv;
            BonusDeDanoTotal = dano;

            if (status != null) status.DefinirBonusDePvMaximo(pv);
            AoMudar?.Invoke();
        }
    }
}
