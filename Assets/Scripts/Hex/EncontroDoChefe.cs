using System;
using UnityEngine;
using UnityEngine.Events;

namespace Loopia.Hex
{
    /// <summary>
    /// O confronto final do GDD ("Apos o jogador completar 4 loops, um Boss e gerado"). Quem decide QUANDO
    /// e o LoopDirector (dono do estado da partida); este componente so cuida do chefe:
    ///
    /// - nasce UMA vez, no centro do miolo que o gerador reservou, pelo InimigoSpawner (entao o ataque dele
    ///   segue o mesmo caminho por UnityEvent ate o LuccaStatus.ReceberDano, e a morte da XP como qualquer
    ///   inimigo);
    /// - vira "alvo grande": o tiro automatico do Lucca alcanca o centro de qualquer casa do anel e de
    ///   qualquer angulo (RegrasDoChefe);
    /// - ataque basico: o mergulho do morcego, mais rapido e de longe;
    /// - ataque especial "Revoada": avisa (AoAvisarEspecial, para o indicador de perigo) e depois chama
    ///   morcegos comuns no anel, que disputam as flechas do Lucca.
    ///
    /// Sem asset proprio: se "chefe" ficar vazio, monta uma variante ampliada do morcego do spawner em
    /// memoria (e a destroi ao sair).
    /// </summary>
    [DisallowMultipleComponent]
    public class EncontroDoChefe : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public LoopDirector diretor;
        public InimigoSpawner inimigos;
        public HexLoopGenerator gerador;
        public LuccaCombate combate;
        public LuccaStatus status;

        [Header("Chefe")]
        [Tooltip("Opcional. Vazio: usa a variante abaixo, montada a partir do morcego do InimigoSpawner.")]
        public InimigoDef chefe;
        public VarianteDoChefe variante = new VarianteDoChefe();

        [Header("Revoada (ataque especial)")]
        public bool usarRevoada = true;
        [Tooltip("Segundos entre uma Revoada e outra, contados do nascimento do chefe.")]
        [Min(1f)] public float intervaloDaRevoada = 14f;
        [Tooltip("Quanto tempo antes o aviso de perigo aparece.")]
        [Min(0f)] public float avisoDaRevoada = 2f;
        [Min(0)] public int morcegosPorRevoada = 2;
        [Tooltip("A Revoada nao passa deste total de morcegos vivos (o maximo do spawn comum, se 0).")]
        [Min(0)] public int maximoDeMorcegosNaRevoada = 0;

        [Header("Durante o confronto")]
        [Tooltip("Pausa o spawn comum de morcegos enquanto o chefe vive. Quem ja existe continua.")]
        public bool pausarSpawnComum = true;

        [Header("Eventos")]
        public UnityEvent aoChefeNascer = new UnityEvent();
        public UnityEvent aoChefeDerrotado = new UnityEvent();

        /// <summary>O chefe vivo, ou null antes de nascer / depois de cair.</summary>
        public Inimigo Chefe { get; private set; }

        /// <summary>O tipo usado (o asset do campo "chefe" ou a variante em memoria).</summary>
        public InimigoDef DefDoChefe { get; private set; }

        /// <summary>Ja nasceu nesta partida. Nunca volta a false: o chefe nasce uma vez so.</summary>
        public bool Iniciado { get; private set; }

        public bool Derrotado { get; private set; }

        public bool AvisandoEspecial => _avisando;

        public event Action<Inimigo> AoChefeNascer;

        /// <summary>O ataque especial vai acontecer daqui a N segundos. Para o indicador de perigo.</summary>
        public event Action<float> AoAvisarEspecial;

        /// <summary>O ataque especial aconteceu (ou foi cancelado), com quantos morcegos nasceram.</summary>
        public event Action<int> AoUsarEspecial;

        public event Action AoChefeDerrotado;

        InimigoDef _varianteEmMemoria;
        float _proximaRevoada;
        bool _avisando;

        void AcharReferencias()
        {
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (combate == null) combate = FindFirstObjectByType<LuccaCombate>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
        }

        /// <summary>Diz se da para comecar o confronto agora e, se nao, por que.</summary>
        public bool PodeIniciar(out string motivo)
        {
            AcharReferencias();
            motivo = null;

            if (Iniciado) motivo = "o chefe ja nasceu nesta partida";
            else if (inimigos == null) motivo = "falta o InimigoSpawner";
            else if (gerador == null || gerador.Loop == null || gerador.Loop.Count == 0) motivo = "o anel ainda nao existe";
            else if (gerador.Miolo == null || gerador.Miolo.Count == 0) motivo = "o anel nao tem miolo para o chefe";
            else if (chefe == null && inimigos.morcego == null) motivo = "sem InimigoDef do chefe nem morcego para a variante";

            return motivo == null;
        }

        /// <summary>
        /// Faz o chefe nascer. Devolve false (sem efeito) se ja nasceu ou se falta algo. Chamado pelo
        /// LoopDirector na transicao para o estado Chefe.
        /// </summary>
        public bool Iniciar()
        {
            if (!PodeIniciar(out string motivo))
            {
                Debug.LogWarning("[EncontroDoChefe] Nao deu para iniciar: " + motivo, this);
                return false;
            }

            DefDoChefe = chefe;
            if (DefDoChefe == null)
            {
                _varianteEmMemoria = variante.Montar(inimigos.morcego);
                DefDoChefe = _varianteEmMemoria;
            }

            HexCoord centro = gerador.CentroDoMiolo;
            Inimigo novo = inimigos.Nascer(DefDoChefe, centro);
            if (novo == null)
            {
                Debug.LogWarning("[EncontroDoChefe] O spawner nao criou o chefe.", this);
                return false;
            }

            Iniciado = true;
            Chefe = novo;

            int alcanceDoTiro = combate != null ? combate.alcance : 3;
            Chefe.TornarAlvoGrande(RegrasDoChefe.AlcanceExtraParaCobrirOAnel(gerador.Loop.Casas, centro, alcanceDoTiro));
            Chefe.aoSerDerrotado.AddListener(ChefeCaiu);

            if (pausarSpawnComum) inimigos.pausarSpawnComum = true;

            _proximaRevoada = Time.time + intervaloDaRevoada;
            _avisando = false;

            Debug.Log("[EncontroDoChefe] " + DefDoChefe.nome + " nasceu em " + centro + " com " + Chefe.PvMaximo +
                      " PV (alcance extra como alvo: " + Chefe.AlcanceExtraComoAlvo + ").", this);

            AoChefeNascer?.Invoke(Chefe);
            aoChefeNascer.Invoke();
            return true;
        }

        void Update()
        {
            if (!Iniciado || Derrotado || Chefe == null || !Chefe.EstaVivo) return;
            if (diretor != null && diretor.Terminou) return;
            if (status != null && !status.EstaVivo) return;
            if (!usarRevoada || morcegosPorRevoada <= 0) return;

            if (!_avisando && Time.time >= _proximaRevoada - avisoDaRevoada)
            {
                _avisando = true;
                AoAvisarEspecial?.Invoke(Mathf.Max(0f, _proximaRevoada - Time.time));
            }

            if (Time.time < _proximaRevoada) return;

            _avisando = false;
            _proximaRevoada = Time.time + intervaloDaRevoada;
            AoUsarEspecial?.Invoke(Revoada());
        }

        /// <summary>Chama morcegos comuns no anel, respeitando o teto. Devolve quantos nasceram.</summary>
        int Revoada()
        {
            InimigoDef morcego = inimigos != null ? inimigos.morcego : null;
            if (morcego == null) return 0;

            int teto = maximoDeMorcegosNaRevoada > 0 ? maximoDeMorcegosNaRevoada : inimigos.maximoDeMorcegos;
            int nasceram = 0;
            for (int i = 0; i < morcegosPorRevoada; i++)
            {
                if (inimigos.QuantosVivos(morcego) >= teto) break;
                if (inimigos.NascerNoAnel(morcego) != null) nasceram++;
            }
            return nasceram;
        }

        void ChefeCaiu(Inimigo quem)
        {
            if (Derrotado || quem != Chefe) return;

            Derrotado = true;
            Chefe.aoSerDerrotado.RemoveListener(ChefeCaiu);
            Chefe = null;

            if (_avisando)
            {
                _avisando = false;
                AoUsarEspecial?.Invoke(0);
            }

            AoChefeDerrotado?.Invoke();
            aoChefeDerrotado.Invoke();
        }

        void OnDestroy()
        {
            if (Chefe != null) Chefe.aoSerDerrotado.RemoveListener(ChefeCaiu);
            if (_varianteEmMemoria != null)
            {
                if (Application.isPlaying) Destroy(_varianteEmMemoria);
                else DestroyImmediate(_varianteEmMemoria);
                _varianteEmMemoria = null;
            }
        }
    }
}
