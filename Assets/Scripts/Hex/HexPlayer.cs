using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// O player sobre o mundo. Nasce no acampamento, que fica no anel, e
    /// anda de hexagono em hexagono ate o destino recebido por MoverPara.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexPlayer : MonoBehaviour
    {
        [Header("Mundo")]
        [Tooltip("Deixe vazio para achar o HexWorld da cena automaticamente.")]
        public HexWorld mundo;

        [Header("Movimento")]
        [Tooltip("Unidades por segundo.")]
        [Min(0.1f)] public float velocidade = 7f;

        [Tooltip("Quao rapido o player vira para a direcao em que anda.")]
        [Min(0.1f)] public float velocidadeDeGiro = 14f;

        [Tooltip("Quanto o player fica acima da superficie do hexagono.")]
        public float alturaSobreOHexagono = 0f;

        readonly List<HexCoord> _caminho = new List<HexCoord>();
        readonly List<HexCoord> _buffer = new List<HexCoord>();

        /// <summary>Hexagono que o player ocupa agora.</summary>
        public HexCoord Coord { get; private set; }

        public bool EstaAndando => _caminho.Count > 0;

        /// <summary>Passos que ainda faltam, na ordem. Somente leitura.</summary>
        public IReadOnlyList<HexCoord> CaminhoAtual => _caminho;

        /// <summary>Disparado toda vez que o player termina de entrar num hexagono.</summary>
        public event Action<HexCoord> AoEntrarNoHexagono;

        /// <summary>Disparado quando o player chega ao ultimo hexagono do caminho.</summary>
        public event Action<HexCoord> AoChegarNoDestino;

        void Start()
        {
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();

            if (mundo == null)
            {
                Debug.LogError("[HexPlayer] Nenhum HexWorld encontrado na cena.", this);
                enabled = false;
                return;
            }

            NascerNoAcampamento();
        }

        /// <summary>Coloca o player no acampamento, sem animacao, e cancela o caminho.</summary>
        [ContextMenu("Nascer no acampamento")]
        public void NascerNoAcampamento()
        {
            if (mundo == null) return;
            ColocarEm(mundo.Acampamento);
        }

        /// <summary>Teleporta o player para um hexagono e limpa o caminho.</summary>
        public void ColocarEm(HexCoord coord)
        {
            _caminho.Clear();
            Coord = coord;
            transform.position = PosicaoDe(coord);
        }

        /// <summary>
        /// Manda o player andar ate o hexagono informado. Se ele ja estiver andando,
        /// termina o passo atual antes de seguir pelo novo caminho, para nao girar no meio do hexagono.
        /// Devolve false se nao existir caminho ate la.
        /// </summary>
        public bool MoverPara(HexCoord destino)
        {
            if (mundo == null) return false;

            // Enquanto anda, o proximo hexagono ja esta comprometido: e dali que o novo caminho sai.
            HexCoord origem = _caminho.Count > 0 ? _caminho[0] : Coord;

            if (!mundo.TryFindPath(origem, destino, _buffer)) return false;

            _caminho.Clear();
            if (origem != Coord) _caminho.Add(origem);
            _caminho.AddRange(_buffer);
            return true;
        }

        /// <summary>
        /// Ultimo hexagono da fila de movimento, ou seja, de onde o proximo passo tem que sair.
        /// Com a fila vazia, e o hexagono atual.
        /// </summary>
        public HexCoord FimDaFila => _caminho.Count > 0 ? _caminho[_caminho.Count - 1] : Coord;

        /// <summary>
        /// Acrescenta UM passo no fim da fila, sem pathfinding: o hexagono precisa ser vizinho
        /// do fim da fila. E assim que o percurso automatico do loop alimenta o player, mantendo
        /// a fila sempre cheia para ele nunca parar entre um hexagono e outro.
        /// </summary>
        public bool EnfileirarPasso(HexCoord vizinho)
        {
            if (mundo == null) return false;
            if (HexCoord.Distance(FimDaFila, vizinho) != 1) return false;
            if (!mundo.IsWalkable(vizinho)) return false;

            _caminho.Add(vizinho);
            return true;
        }

        /// <summary>Caminho que o player faria ate o destino, sem comecar a andar.</summary>
        public bool PreverCaminho(HexCoord destino, List<HexCoord> resultado)
        {
            if (mundo == null)
            {
                resultado.Clear();
                return false;
            }
            return mundo.TryFindPath(Coord, destino, resultado);
        }

        /// <summary>Para no proximo hexagono, sem cortar o passo pela metade.</summary>
        public void Parar()
        {
            if (_caminho.Count > 1) _caminho.RemoveRange(1, _caminho.Count - 1);
        }

        void Update()
        {
            if (_caminho.Count == 0) return;

            Vector3 alvo = PosicaoDe(_caminho[0]);

            Vector3 direcao = alvo - transform.position;
            direcao.y = 0f;
            if (direcao.sqrMagnitude > 0.0001f)
            {
                Quaternion giroAlvo = Quaternion.LookRotation(direcao);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, giroAlvo, velocidadeDeGiro * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, alvo, velocidade * Time.deltaTime);

            if ((transform.position - alvo).sqrMagnitude > 0.000001f) return;

            transform.position = alvo;
            Coord = _caminho[0];
            _caminho.RemoveAt(0);

            AoEntrarNoHexagono?.Invoke(Coord);
            if (_caminho.Count == 0) AoChegarNoDestino?.Invoke(Coord);
        }

        Vector3 PosicaoDe(HexCoord coord)
        {
            return mundo.PosicaoDe(coord) + mundo.transform.up * alturaSobreOHexagono;
        }
    }
}
