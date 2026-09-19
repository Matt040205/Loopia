using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// O combate automatico do GDD: "Inimigo a Frente (Com Municao): Ataca automaticamente com
    /// projeteis, ele continua andando e atirando seus projeteis, se nao tiver, ele os ignora e
    /// continua andando."
    ///
    /// Ou seja: o Lucca nunca para para lutar, e ficar sem flecha nao trava nada — ele so deixa
    /// de atirar. Cada tiro gasta uma flecha.
    /// </summary>
    [DisallowMultipleComponent]
    public class LuccaCombate : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public HexPlayer player;
        public LuccaStatus status;
        public InimigoSpawner inimigos;

        [Tooltip("De onde vem o bonus de dano das torres e areas de treino. Opcional.")]
        public MapaDePlataformas plataformas;

        [Header("Tiro")]
        [Tooltip("Alcance do tiro, em hexagonos.")]
        [Min(1)] public int alcance = 3;

        [Tooltip("Segundos entre um tiro e outro. O GDD deixou o atk SPD do Lucca em aberto ('? atk SPD').")]
        [Min(0.05f)] public float intervaloDeTiro = 0.8f;

        [Min(1f)] public float velocidadeDoProjetil = 14f;

        [Tooltip("Altura de onde a flecha sai, medida do pe do Lucca.")]
        public float alturaDoTiro = 0.6f;

        public Color corDaFlecha = new Color(1f, 0.85f, 0.35f);

        [Header("Mira")]
        [Tooltip("Ligado: so atira em quem esta a frente, como diz o GDD. " +
                 "Desligado: atira no mais proximo, esteja onde estiver.")]
        public bool somenteAFrente = true;

        float _proximoTiro;

        void Start()
        {
            if (player == null) player = GetComponent<HexPlayer>();
            if (player == null) player = FindFirstObjectByType<HexPlayer>();
            if (status == null) status = GetComponent<LuccaStatus>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();
            if (plataformas == null) plataformas = FindFirstObjectByType<MapaDePlataformas>();

            if (player == null || status == null || inimigos == null)
            {
                Debug.LogError("[LuccaCombate] Faltou referencia: precisa de HexPlayer, LuccaStatus e InimigoSpawner.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!status.EstaVivo) return;
            if (Time.time < _proximoTiro) return;

            Inimigo alvo = EscolherAlvo();
            if (alvo == null) return;

            // Sem flecha ele ignora o inimigo e segue andando: o intervalo nem chega a reiniciar,
            // entao no instante em que ganhar municao ele ja volta a atirar.
            if (!status.GastarFlecha()) return;

            _proximoTiro = Time.time + intervaloDeTiro;

            Vector3 origem = transform.position + Vector3.up * alturaDoTiro;
            Projetil.Disparar(origem, alvo, DanoDoTiro(), velocidadeDoProjetil, corDaFlecha);
        }

        /// <summary>ATK do Lucca mais o que torres e areas de treino somam.</summary>
        public int DanoDoTiro()
        {
            float bonus = plataformas != null ? plataformas.BonusDeDanoTotal : 0f;
            return Mathf.Max(1, Mathf.RoundToInt(status.ataque * (1f + bonus)));
        }

        /// <summary>Inimigo vivo mais proximo dentro do alcance, preferindo quem esta a frente.</summary>
        Inimigo EscolherAlvo()
        {
            Inimigo melhor = null;
            int menorDistancia = int.MaxValue;

            var vivos = inimigos.Vivos;
            for (int i = 0; i < vivos.Count; i++)
            {
                Inimigo inimigo = vivos[i];
                if (inimigo == null || !inimigo.EstaVivo) continue;

                int distancia = HexCoord.Distance(player.Coord, inimigo.Casa);
                if (distancia > alcance) continue;
                if (distancia >= menorDistancia) continue;
                if (!EstaAFrente(inimigo)) continue;

                menorDistancia = distancia;
                melhor = inimigo;
            }

            return melhor;
        }

        bool EstaAFrente(Inimigo inimigo)
        {
            if (!somenteAFrente) return true;

            // Quem esta na mesma casa conta sempre: nao da para ignorar quem esta em cima de voce.
            if (inimigo.Casa == player.Coord) return true;

            Vector3 paraOInimigo = inimigo.transform.position - transform.position;
            paraOInimigo.y = 0f;
            if (paraOInimigo.sqrMagnitude < 0.0001f) return true;

            return Vector3.Dot(transform.forward, paraOInimigo.normalized) > 0f;
        }
    }
}
