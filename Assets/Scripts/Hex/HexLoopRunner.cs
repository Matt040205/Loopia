using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Faz o Lucca percorrer o anel sozinho, como manda o GDD ("o jogador nao controla
    /// diretamente o personagem"), e avisa toda vez que ele fecha uma volta ao passar de novo
    /// pelo acampamento.
    ///
    /// A fila de movimento do player e mantida com alguns passos adiantados, para ele nunca
    /// parar entre um hexagono e outro esperando a proxima ordem.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexLoopRunner : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexPlayer player;
        public HexLoopGenerator gerador;

        [Header("Percurso")]
        [Tooltip("Desligue para o Lucca parar no proximo hexagono.")]
        public bool andando = true;

        [Tooltip("Quantos passos manter na fila. 1 ja funciona; mais deixa o movimento mais liso.")]
        [Range(1, 6)] public int passosAdiantados = 3;

        [Tooltip("Inverte o sentido da volta.")]
        public bool inverterSentido = false;

        /// <summary>Quantas voltas inteiras o Lucca ja fechou.</summary>
        public int VoltasCompletas { get; private set; }

        /// <summary>Quantos hexagonos ele ja andou desde a ultima passagem pelo acampamento.</summary>
        public int PassosNaVoltaAtual { get; private set; }

        /// <summary>Tamanho da volta, em hexagonos.</summary>
        public int TamanhoDoLoop => gerador != null && gerador.Loop != null ? gerador.Loop.Count : 0;

        /// <summary>Disparado ao fechar a volta. O argumento e o numero da volta recem-completada.</summary>
        public event Action<int> AoCompletarVolta;

        bool _pronto;
        bool _inscritoNoPlayer;
        bool _inscritoNoGerador;
        int _indiceNaFila;
        HexCoord _casaDoAcampamento;

        void Update()
        {
            if (!_pronto && !Sincronizar()) return;
            if (!andando) return;

            AlimentarFila();
        }

        void OnDisable() => Desinscrever();

        void OnDestroy() => Desinscrever();

        void Desinscrever()
        {
            if (_inscritoNoPlayer && player != null) player.AoEntrarNoHexagono -= AoEntrarNoHexagono;
            if (_inscritoNoGerador && gerador != null) gerador.AoGerarMapa -= MapaFoiGerado;

            _inscritoNoPlayer = false;
            _inscritoNoGerador = false;
            _pronto = false;
        }

        /// <summary>Zera a contagem de voltas. Use ao comecar uma partida nova.</summary>
        public void ReiniciarContagem()
        {
            VoltasCompletas = 0;
            PassosNaVoltaAtual = 0;
        }

        /// <summary>
        /// Amarra o percurso ao anel que existe agora. Roda no primeiro Update (o mapa nasce no
        /// Awake do gerador) e de novo toda vez que um mapa novo e sorteado, porque nesse caso o
        /// indice e a fila do anel antigo nao valem mais nada e o Lucca travaria.
        /// </summary>
        bool Sincronizar()
        {
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();

            if (player == null || gerador == null) return false;
            if (gerador.Loop == null || gerador.Loop.Count == 0) return false;
            if (gerador.mundo == null) return false;

            if (!_inscritoNoGerador)
            {
                gerador.AoGerarMapa += MapaFoiGerado;
                _inscritoNoGerador = true;
            }

            _casaDoAcampamento = gerador.mundo.Acampamento;

            // ColocarEm tambem limpa a fila, que pode ter passos de um anel que nem existe mais.
            if (gerador.Loop.TryIndiceDe(player.Coord, out _indiceNaFila))
            {
                player.ColocarEm(player.Coord);
            }
            else
            {
                player.ColocarEm(_casaDoAcampamento);
                if (!gerador.Loop.TryIndiceDe(_casaDoAcampamento, out _indiceNaFila)) return false;
            }

            if (!_inscritoNoPlayer)
            {
                player.AoEntrarNoHexagono += AoEntrarNoHexagono;
                _inscritoNoPlayer = true;
            }

            PassosNaVoltaAtual = 0;
            _pronto = true;
            return true;
        }

        void MapaFoiGerado()
        {
            // Nao sincroniza agora: o proximo Update faz isso, ja com o mundo inteiro montado.
            _pronto = false;
        }

        void AlimentarFila()
        {
            HexLoopPath loop = gerador.Loop;
            int sentido = inverterSentido ? -1 : 1;

            // O limite so existe para nunca travar o frame se algo sair do esperado.
            int protecao = 0;
            while (player.CaminhoAtual.Count < passosAdiantados && protecao++ < 64)
            {
                int proximo = loop.Normalizar(_indiceNaFila + sentido);
                if (!player.EnfileirarPasso(loop[proximo])) break;

                _indiceNaFila = proximo;
            }
        }

        void AoEntrarNoHexagono(HexCoord casa)
        {
            PassosNaVoltaAtual++;

            if (casa != _casaDoAcampamento) return;

            // Chegou de novo no acampamento: volta fechada.
            PassosNaVoltaAtual = 0;
            VoltasCompletas++;
            AoCompletarVolta?.Invoke(VoltasCompletas);
        }
    }
}
