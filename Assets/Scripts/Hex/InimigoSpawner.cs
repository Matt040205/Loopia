using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Coloca os inimigos comuns no caminho.
    ///
    /// Morcego: o GDD diz que "voa e aparece no caminho em qualquer parte dele", entao nasce
    /// numa casa sorteada do anel, longe o bastante do Lucca para nao surgir em cima dele.
    ///
    /// Lobo: no GDD ele vem da carta de floresta ("faz que todo loop tenha lobos nessa area"),
    /// que ainda nao existe. Entao aqui tem um nascimento provisorio por tempo, so para dar
    /// para testar; quando a carta chegar, ela chama Nascer() na casa da floresta e este
    /// provisorio pode ser desligado no Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class InimigoSpawner : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexWorld mundo;
        public HexLoopGenerator gerador;
        public HexPlayer player;
        public LuccaStatus status;
        public LoopDirector diretor;

        [Header("Tipos")]
        public InimigoDef morcego;
        public InimigoDef lobo;

        [Header("Morcegos")]
        public bool nascerMorcegos = true;
        [Min(0.5f)] public float intervaloDeMorcegos = 5f;
        [Min(0)] public int maximoDeMorcegos = 6;

        [Tooltip("Distancia minima do Lucca, em casas, para um morcego nao brotar em cima dele.")]
        [Min(0)] public int distanciaMinimaDoPlayer = 3;

        [Header("Lobos (provisorio, ate existir a carta de floresta)")]
        public bool nascerLobosPorTempo = true;
        [Min(0.5f)] public float intervaloDeLobos = 14f;
        [Min(0)] public int maximoDeLobos = 2;

        readonly List<Inimigo> _vivos = new List<Inimigo>();

        float _proximoMorcego;
        float _proximoLobo;
        bool _inscritoNoGerador;

        public IReadOnlyList<Inimigo> Vivos => _vivos;

        public int QuantosVivos(InimigoDef def)
        {
            int total = 0;
            for (int i = 0; i < _vivos.Count; i++)
            {
                if (_vivos[i] != null && _vivos[i].Def == def) total++;
            }
            return total;
        }

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();

            if (mundo == null || gerador == null || player == null || status == null)
            {
                Debug.LogError("[InimigoSpawner] Faltou referencia na cena.", this);
                enabled = false;
                return;
            }

            gerador.AoGerarMapa += LimparTodos;
            _inscritoNoGerador = true;

            _proximoMorcego = Time.time + intervaloDeMorcegos;
            _proximoLobo = Time.time + intervaloDeLobos;
        }

        void OnDestroy()
        {
            if (_inscritoNoGerador && gerador != null) gerador.AoGerarMapa -= LimparTodos;
            _inscritoNoGerador = false;
        }

        void Update()
        {
            if (gerador.Loop == null || gerador.Loop.Count == 0) return;
            if (status != null && !status.EstaVivo) return;

            if (nascerMorcegos && morcego != null && Time.time >= _proximoMorcego)
            {
                _proximoMorcego = Time.time + intervaloDeMorcegos;
                if (QuantosVivos(morcego) < maximoDeMorcegos) NascerEmCasaSorteada(morcego);
            }

            if (nascerLobosPorTempo && lobo != null && Time.time >= _proximoLobo)
            {
                _proximoLobo = Time.time + intervaloDeLobos;
                if (QuantosVivos(lobo) < maximoDeLobos) NascerEmCasaSorteada(lobo);
            }
        }

        /// <summary>
        /// Faz nascer um inimigo na casa informada. E por aqui que a carta de floresta vai
        /// povoar a area dela quando existir.
        /// </summary>
        public Inimigo Nascer(InimigoDef def, HexCoord casa)
        {
            if (def == null) return null;

            var go = new GameObject(def.nome);
            go.transform.SetParent(transform, false);

            var inimigo = go.AddComponent<Inimigo>();
            float escala = diretor != null ? diretor.MultiplicadorDosInimigos : 1f;
            inimigo.Nascer(def, casa, escala, mundo, player, status);

            inimigo.AoMorrer += Morreu;
            _vivos.Add(inimigo);
            return inimigo;
        }

        public void LimparTodos()
        {
            for (int i = 0; i < _vivos.Count; i++)
            {
                if (_vivos[i] == null) continue;

                _vivos[i].AoMorrer -= Morreu;
                Destroy(_vivos[i].gameObject);
            }
            _vivos.Clear();
        }

        void Morreu(Inimigo inimigo)
        {
            _vivos.Remove(inimigo);

            // GDD: "Derrotar inimigos concede XP ao jogador".
            if (status != null) status.GanharXp(inimigo.Xp);
        }

        /// <summary>Sorteia uma casa do anel que esteja livre e longe o bastante do Lucca.</summary>
        void NascerEmCasaSorteada(InimigoDef def)
        {
            HexLoopPath loop = gerador.Loop;

            const int tentativas = 24;
            for (int i = 0; i < tentativas; i++)
            {
                HexCoord casa = loop[Random.Range(0, loop.Count)];

                if (HexCoord.Distance(casa, player.Coord) < distanciaMinimaDoPlayer) continue;
                if (TemInimigoEm(casa)) continue;

                Nascer(def, casa);
                return;
            }
        }

        bool TemInimigoEm(HexCoord casa) => QuantosEm(casa) > 0;

        /// <summary>Quantos inimigos vivos estao nesta casa.</summary>
        public int QuantosEm(HexCoord casa)
        {
            int total = 0;
            for (int i = 0; i < _vivos.Count; i++)
            {
                if (_vivos[i] != null && _vivos[i].Casa == casa) total++;
            }
            return total;
        }
    }
}
