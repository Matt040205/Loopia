using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// G2-C: diz se o Lucca esta num Mirante (Ilha alta levantada) e quanto isso muda o tiro. So leitura: nao
    /// atira, nao mexe no NavMesh nem no percurso. Reavalia quando o Lucca entra numa casa e quando o mapa muda
    /// (ex.: a Mola sai e a ilha baixa); nada por frame.
    ///
    /// Ligado ao LuccaCombate na integracao do MVP: AlcanceAtual (escolha e revalidacao do tiro reservado) e
    /// EstaAFrente consultam este componente.
    /// </summary>
    [DisallowMultipleComponent]
    public class MiranteDoLucca : MonoBehaviour
    {
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexPlayer player;
        public MapaDePlataformas mapa;
        public HexWorld mundo;

        bool _inscrito;
        HexCoord _casa;
        bool _temCasa;

        /// <summary>O Lucca esta agora num Mirante ativo.</summary>
        public bool Ativo { get; private set; }

        /// <summary>Casas de alcance a mais agora (0 fora do Mirante).</summary>
        public int BonusDeAlcance { get; private set; }

        /// <summary>Agora pode atirar em quem nao esta a frente.</summary>
        public bool TiroEmQualquerDirecao { get; private set; }

        /// <summary>Disparado quando entra ou sai do Mirante (true = entrou).</summary>
        public event Action<bool> AoMudar;

        void Start()
        {
            if (player == null) player = GetComponent<HexPlayer>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (mapa == null) mapa = FindFirstObjectByType<MapaDePlataformas>();
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (player == null || mapa == null || mundo == null) return;

            player.AoEntrarNoHexagono += Avaliar;
            mapa.AoMudar += Reavaliar;
            _inscrito = true;
            Avaliar(player.Coord);
        }

        void OnDestroy()
        {
            if (!_inscrito) return;
            if (player != null) player.AoEntrarNoHexagono -= Avaliar;
            if (mapa != null) mapa.AoMudar -= Reavaliar;
            _inscrito = false;
        }

        /// <summary>Alcance do tiro ja com o Mirante, a partir do alcance base do LuccaCombate.</summary>
        public int AlcanceDoTiro(int alcanceBase) => RegrasDoMirante.AlcanceDoTiro(alcanceBase, Ativo, BonusDeAlcance);

        void Reavaliar()
        {
            if (_temCasa) Avaliar(_casa);
        }

        /// <summary>
        /// Reavalia so se a casa mudou desde a ultima avaliacao. O LuccaCombate chama antes de mirar: cobre o
        /// teleporte (HexPlayer.ColocarEm nao dispara AoEntrarNoHexagono) com uma comparacao barata.
        /// </summary>
        public void SincronizarCom(HexCoord casa)
        {
            // Pode ser chamado antes do proprio Start (componente criado em tempo de execucao pelo LuccaCombate).
            if (mapa == null) mapa = FindFirstObjectByType<MapaDePlataformas>();
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (!_temCasa || casa != _casa) Avaliar(casa);
        }

        /// <summary>Reavalia o Mirante para o Lucca parado nesta casa.</summary>
        public void Avaliar(HexCoord casa)
        {
            _casa = casa;
            _temCasa = true;

            PlataformaRelevoDef relevo = null;
            float altura = 0f;
            if (mapa != null && mapa.TryGet(casa, out Plataforma p) && p != null) relevo = p.Def as PlataformaRelevoDef;
            if (relevo != null && mundo != null && mundo.TryGetIlha(casa, out HexTile ilha) && ilha != null)
                altura = ilha.Altura;

            bool ativo = RegrasDoMirante.EhMirante(relevo, altura);
            BonusDeAlcance = ativo ? relevo.bonusDeAlcance : 0;
            TiroEmQualquerDirecao = ativo && relevo.tiroEmQualquerDirecao;

            if (ativo == Ativo) return;
            Ativo = ativo;
            AoMudar?.Invoke(ativo);
        }
    }
}
