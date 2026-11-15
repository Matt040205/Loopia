using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Coleta as MetricasDaPartida ouvindo eventos que ja existem (diretor, corredor, combate, spawner,
    /// encontro). Opcional e so de medicao: nao muda regra nenhuma. Os testes de medicao o adicionam em tempo
    /// de execucao; na cena pode ficar desligado. No fim da jornada escreve o resumo no console.
    /// </summary>
    [DisallowMultipleComponent]
    public class MedidorDaPartida : MonoBehaviour
    {
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public LoopDirector diretor;
        public HexLoopRunner corredor;
        public LuccaStatus status;
        public LuccaCombate combate;
        public InimigoSpawner inimigos;
        public HexLoopGenerator gerador;
        public EncontroDoChefe encontro;

        public bool escreverResumoNoFim = true;

        [SerializeField] MetricasDaPartida metricas = new MetricasDaPartida();

        public MetricasDaPartida Metricas => metricas;

        bool _inscrito;
        int _pv;
        int _bonusDePv;

        void Start()
        {
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
            if (corredor == null) corredor = FindFirstObjectByType<HexLoopRunner>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();
            if (combate == null) combate = FindFirstObjectByType<LuccaCombate>();
            if (inimigos == null) inimigos = FindFirstObjectByType<InimigoSpawner>();
            if (gerador == null) gerador = FindFirstObjectByType<HexLoopGenerator>();
            if (encontro == null) encontro = FindFirstObjectByType<EncontroDoChefe>();

            if (diretor == null || corredor == null || status == null)
            {
                Debug.LogWarning("[MedidorDaPartida] Sem diretor/corredor/status: nada a medir.", this);
                enabled = false;
                return;
            }

            metricas.Comecar(Time.time, gerador != null && gerador.Loop != null ? gerador.Loop.Count : 0);

            diretor.AoMudarEstado += MudouEstado;
            diretor.AoCompletarVolta += FechouVolta;
            corredor.AoPassarPeloAcampamento += PassouPeloAcampamento;
            if (combate != null) combate.AoDisparar += Disparou;
            if (inimigos != null) inimigos.AoInimigoDerrotado += Abateu;
            if (encontro != null) encontro.AoUsarEspecial += Revoada;

            // Dano pela queda do PV (o AoMudar vem antes do AoMorrer, entao o golpe final tambem conta).
            _pv = status.PvAtual;
            _bonusDePv = status.BonusDePvMaximo;
            status.AoMudar += MudouStatus;
            _inscrito = true;
        }

        void OnDestroy()
        {
            if (!_inscrito) return;
            if (diretor != null)
            {
                diretor.AoMudarEstado -= MudouEstado;
                diretor.AoCompletarVolta -= FechouVolta;
            }
            if (corredor != null) corredor.AoPassarPeloAcampamento -= PassouPeloAcampamento;
            if (combate != null) combate.AoDisparar -= Disparou;
            if (inimigos != null) inimigos.AoInimigoDerrotado -= Abateu;
            if (encontro != null) encontro.AoUsarEspecial -= Revoada;
            if (status != null) status.AoMudar -= MudouStatus;
            _inscrito = false;
        }

        void Update()
        {
            bool chefeVivo = encontro != null && encontro.Chefe != null && encontro.Chefe.EstaVivo;
            metricas.Avancar(Time.deltaTime, chefeVivo, status.FlechasAtuais);
        }

        void MudouEstado(EstadoDaPartida anterior, EstadoDaPartida novo)
        {
            if (novo == EstadoDaPartida.Chefe) metricas.ComecarChefe(Time.time, status.PvAtual);
            if (!CicloDaPartida.EhTerminal(novo)) return;

            metricas.Terminar(Time.time, novo);
            if (escreverResumoNoFim) Debug.Log(metricas.Resumo(), this);
        }

        void FechouVolta(int volta) => metricas.FecharVolta(volta, Time.time, status.PvAtual);

        void PassouPeloAcampamento() => metricas.RegistrarRecarga();

        void Disparou(Inimigo alvo, Vector3 _) =>
            metricas.RegistrarDisparo(encontro != null && alvo != null && alvo == encontro.Chefe);

        void MudouStatus()
        {
            // Queda de PV com o mesmo bonus = dano. Tirar uma Cabana tambem baixa o PV, mas muda o bonus.
            if (status.BonusDePvMaximo == _bonusDePv && status.PvAtual < _pv) metricas.RegistrarDano(_pv - status.PvAtual);
            _pv = status.PvAtual;
            _bonusDePv = status.BonusDePvMaximo;
        }

        void Abateu(Inimigo _) => metricas.RegistrarAbate();

        void Revoada(int morcegos) => metricas.RegistrarRevoada(morcegos);
    }
}
