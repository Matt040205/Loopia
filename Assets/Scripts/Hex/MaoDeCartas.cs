using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// A mao do jogador: "espaco das cartas (...) com 4 espacos disponiveis para vir as cartas
    /// de plataforma". Carta nova chega ao subir de nivel, como pede o GDD em Progressao:
    /// "Ao atingir um novo nivel, cartas de cenario sao disponibilizadas".
    /// </summary>
    [DisallowMultipleComponent]
    public class MaoDeCartas : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public LuccaStatus status;

        [Header("Baralho")]
        [Tooltip("Todas as cartas que podem sair. Por enquanto o sorteio e uniforme.")]
        public List<PlataformaDef> baralho = new List<PlataformaDef>();

        [Header("Mao")]
        [Tooltip("GDD: 4 espacos disponiveis na base da tela.")]
        [Min(1)] public int espacos = 4;

        [Tooltip("Quantas cartas o jogador ja comeca segurando.")]
        [Min(0)] public int cartasIniciais = 2;

        [Tooltip("Quantas cartas chegam a cada nivel novo.")]
        [Min(0)] public int cartasPorNivel = 1;

        PlataformaDef[] _mao;
        bool _inscrito;

        /// <summary>A mao, com null nos espacos vazios.</summary>
        public IReadOnlyList<PlataformaDef> Mao => _mao;

        public int EspacosLivres
        {
            get
            {
                int livres = 0;
                for (int i = 0; i < _mao.Length; i++) if (_mao[i] == null) livres++;
                return livres;
            }
        }

        public event Action AoMudar;

        void Awake()
        {
            _mao = new PlataformaDef[Mathf.Max(1, espacos)];
        }

        void Start()
        {
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();

            if (status != null)
            {
                status.AoSubirDeNivel += SubiuDeNivel;
                _inscrito = true;
            }

            for (int i = 0; i < cartasIniciais; i++) Comprar();
        }

        void OnDestroy()
        {
            if (_inscrito && status != null) status.AoSubirDeNivel -= SubiuDeNivel;
            _inscrito = false;
        }

        public PlataformaDef CartaNoEspaco(int espaco)
        {
            if (_mao == null || espaco < 0 || espaco >= _mao.Length) return null;
            return _mao[espaco];
        }

        /// <summary>Tira a carta do espaco, depois de ela ter sido colocada no mapa.</summary>
        public bool Gastar(int espaco)
        {
            if (_mao == null || espaco < 0 || espaco >= _mao.Length) return false;
            if (_mao[espaco] == null) return false;

            _mao[espaco] = null;
            AoMudar?.Invoke();
            return true;
        }

        /// <summary>Sorteia uma carta do baralho para o primeiro espaco livre.</summary>
        public bool Comprar()
        {
            if (baralho == null || baralho.Count == 0) return false;

            for (int i = 0; i < _mao.Length; i++)
            {
                if (_mao[i] != null) continue;

                _mao[i] = baralho[UnityEngine.Random.Range(0, baralho.Count)];
                AoMudar?.Invoke();
                return true;
            }

            // Mao cheia: a carta simplesmente nao vem.
            return false;
        }

        void SubiuDeNivel(int nivel)
        {
            for (int i = 0; i < cartasPorNivel; i++) Comprar();
        }
    }
}
