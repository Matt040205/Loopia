using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Loopia.Hex
{
    /// <summary>
    /// Nuvens toon abaixo das ilhas (Sprint 6). Malhas de <see cref="MalhaDeNuvem"/> com o shader
    /// <c>Loopia/Nuvem Toon</c>, que anima cada gomo.
    /// <para>Composicao: a tela tem uma <b>zona protegida</b> (envoltoria convexa das ilhas na vista da camera, com
    /// margem) e nenhuma nuvem passa por ela. As nuvens contornam a zona (<see cref="ComposicaoDasNuvens.Desviar"/>);
    /// se ficarem presas na borda, ou se uma ilha nova surgir em cima delas, se desfazem e renascem em area livre.</para>
    /// <para>Vida: cada nuvem nasce (cresce e se materializa), vive, se dissipa (encolhe, afunda e se desfaz) e
    /// renasce, em fases desencontradas. A quantidade acompanha a area livre da tela.</para>
    /// </summary>
    [ExecuteAlways, DefaultExecutionOrder(230)]
    [DisallowMultipleComponent]
    public class CamadaDeNuvens : MonoBehaviour
    {
        public Camera camera3d;
        public HexWorld mundo;
        [Tooltip("Material com o shader Loopia/Nuvem Toon.")]
        public Material material;
        [Tooltip("Nuvens ativas numa tela 16:9 com o tabuleiro tipico; a quantidade acompanha a area livre de verdade.")]
        [Range(4, 30)] public int quantidade = 20;
        public int semente = 751;
        [Tooltip("Formatos diferentes de nuvem (de todos os portes).")]
        [Range(3, 16)] public int formatos = 10;
        [Tooltip("Subdivisoes de cada gomo (3 = contorno liso).")]
        [Range(1, 4)] public int subdivisoes = 3;
        [Tooltip("Largura da menor e da maior nuvem, como fracao da altura visivel da camera.")]
        public Vector2 tamanho = new Vector2(0.08f, 0.24f);
        [Tooltip("Folga entre a base das ilhas e o topo da nuvem mais alta.")]
        [Min(0.1f)] public float distanciaDasIlhas = 1.4f;
        [Tooltip("Espalhamento vertical das nuvens; as grandes ficam no fundo.")]
        [Min(0f)] public float profundidade = 2.6f;
        [Tooltip("Direcao do vento no enquadramento.")]
        public Vector2 vento = new Vector2(1f, 0.18f);
        [Min(0f)] public float velocidade = 0.0035f;
        [Tooltip("Variacao lenta da forca do vento.")]
        [Range(0f, 1f)] public float rajadas = 0.3f;
        [Tooltip("Quanto as nuvens se afastam quando se encostam na tela (sem isso, uma entra na outra e as duas viram uma massa so).")]
        [Range(0f, 1f)] public float separacao = 0.25f;

        [Header("Zona protegida (tabuleiro)")]
        [Tooltip("Folga entre a nuvem e a envoltoria das ilhas, em fracao da altura da tela.")]
        [Range(0f, 0.2f)] public float margemDaZona = 0.04f;
        [Tooltip("Distancia (fracao da altura da tela) em que a nuvem comeca a desviar da zona.")]
        [Range(0.01f, 0.4f)] public float faixaDeDesvio = 0.12f;
        [Tooltip("Segundos encostada na zona antes de se desfazer (evita acumulo na borda).")]
        [Min(1f)] public float tempoMaximoNaBorda = 12f;

        [Header("Vida")]
        public Vector2 duracaoAoNascer = new Vector2(6f, 10f);
        public Vector2 duracaoDaVida = new Vector2(40f, 90f);
        public Vector2 duracaoAoDissipar = new Vector2(5f, 8f);
        [Tooltip("Chance de a nuvem renascer fora da tela, do lado de onde vem o vento, e entrar deslizando.")]
        [Range(0f, 1f)] public float chanceDeEntrarPelaBorda = 0.2f;
        [Tooltip("Balanco vertical como fracao da largura da nuvem.")]
        [Range(0f, 0.2f)] public float balanco = 0.05f;
        [Tooltip("Variacao lenta do tamanho (respiracao).")]
        [Range(0f, 0.1f)] public float respiracao = 0.025f;
        [Tooltip("Giro lento de ida e volta, em graus.")]
        [Range(0f, 30f)] public float giroLento = 8f;
        [Tooltip("Quanto os gomos incham e brotam (0 = parados).")]
        [Range(0f, 1f)] public float animacaoDosGomos = 1f;
        [Tooltip("Ao nascer e sumir, a nuvem se materializa/desfaz com ruido nas bordas.")]
        public bool dissolverComRuido = true;

        [Header("Sombra no chao")]
        [Tooltip("Material de decal (Shader Graphs/Decal) com a mancha suave. Vazio = sem sombra.")]
        public Material materialDaSombra;
        [Range(0f, 1f)] public float forcaDaSombra = 0.85f;
        [Tooltip("Tamanho da mancha em relacao a pegada da nuvem.")]
        [Range(0.5f, 3f)] public float tamanhoDaSombra = 1.7f;
        [Tooltip("Ate onde a sombra desce abaixo da nuvem (precisa alcancar o chao).")]
        [Min(1f)] public float alcanceDaSombra = 26f;
        [Tooltip("Quanto o chao fica abaixo da base das ilhas (para saber onde a sombra cai na tela e apaga-la na zona).")]
        [Min(1f)] public float quedaAteOChao = 12.5f;

        public enum Estado { Dormindo, Nascendo, Vivendo, Dissipando }

        class Nuvem
        {
            public Transform transform;
            public MeshFilter filtro;
            public MeshRenderer renderer;
            public DecalProjector sombra;
            public int formato;
            public Vector2 uv;
            public float largura, altura, profundidade, velocidade, fase, fase2, frequencia, camada, giro, lado;
            public Estado estado;
            public float progresso, duracao, tempoNaBorda, raioNaTela, tempoForaDaTela;
            public bool apareceu;
        }

        readonly List<Nuvem> _nuvens = new List<Nuvem>();
        readonly List<Mesh> _malhas = new List<Mesh>();
        readonly List<int> _portes = new List<int>();
        readonly List<Vector2> _pontosDaZona = new List<Vector2>();
        List<Vector2> _zona = new List<Vector2>();
        readonly Vector3[] _cantos = new Vector3[4];
        MaterialPropertyBlock _bloco;
        System.Random _sorteio;
        int _sementeAtual, _formatosAtuais, _subdivisoesAtuais, _quantidadeAtual;
        Material _materialAtual, _sombraAtual;
        float _baseDasIlhas = -3.5f;
        float _proximaMedicao, _proximoAjuste;
        int _ilhasMedidas = -1;
        Vector2 _centroDaZona;
        readonly float[] _capacidade = { 1f, 1f, 1f, 1f };
        HexIsoCamera _iso;
        bool _temVistaBase;
        Vector3 _origemBase;
        Quaternion _rotacaoBase = Quaternion.identity;
        float _meiaAlturaBase, _aspecto = 16f / 9f, _alturaDaVista = 1f, _aspectoDaZona = -1f;
        const float Margem = 0.28f;
        const float AreaLivreDeReferencia = 1.7778f * 0.62f;
        static readonly int IdTempo = Shader.PropertyToID("_TempoDasNuvens");
        static readonly int IdFase = Shader.PropertyToID("_FaseDaNuvem");
        static readonly int IdDissolver = Shader.PropertyToID("_Dissolver");
        static readonly int IdAnimacao = Shader.PropertyToID("_Animacao");

        /// <summary>Tamanho do conjunto (nuvens ativas + adormecidas).</summary>
        public int TotalDeNuvens => _nuvens.Count;
        public int NuvensAtivas { get; private set; }
        public int AlvoDeNuvens { get; private set; }
        /// <summary>Envoltoria das ilhas em coordenadas de tela (unidade = altura da tela), sem a margem.</summary>
        public IReadOnlyList<Vector2> ZonaProtegida => _zona;
        /// <summary>Menor distancia, neste quadro, entre a borda de uma nuvem visivel e a zona (negativa = por cima).</summary>
        public float MenorFolgaVisivel { get; private set; } = float.PositiveInfinity;
        /// <summary>Quantas vezes, desde o inicio, uma nuvem se desfez por encostar na zona (presa ou ilha nova).</summary>
        public int DesfeitasPelaZona { get; private set; }
        /// <summary>
        /// Neste quadro, a menor distancia entre uma nuvem nascendo e outra nuvem, em fracao da soma dos raios na tela
        /// (abaixo de ~0,7 a nova "brota de dentro" da vizinha e as duas parecem uma so). Sem ninguem nascendo: +infinito.
        /// </summary>
        public float MenorDistanciaAoNascer { get; private set; } = float.PositiveInfinity;
        public int Renascimentos { get; private set; }

        void OnEnable() => _proximaMedicao = 0f;
        void LateUpdate() => Atualizar(Application.isPlaying ? Time.deltaTime : 0f);

        public void Atualizar(float deltaTime)
        {
            if (camera3d == null) camera3d = Camera.main;
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (camera3d == null || material == null) return;
            if (_nuvens.Count == 0 || _sementeAtual != semente || _materialAtual != material || _sombraAtual != materialDaSombra ||
                _formatosAtuais != formatos || _subdivisoesAtuais != subdivisoes || _quantidadeAtual != quantidade)
                CriarNuvens();

            MedirVista();
            // Janela redimensionada: as nuvens ficam presas a tela, entao a zona tem de ser refeita na hora.
            bool ilhasMudaram = (mundo != null && mundo.TotalDeIlhas != _ilhasMedidas) || Mathf.Abs(_aspecto - _aspectoDaZona) > 0.005f;
            if (!Application.isPlaying || Time.unscaledTime >= _proximaMedicao || ilhasMudaram)
            {
                _proximaMedicao = Time.unscaledTime + 0.5f;
                MedirIlhas();
            }

            float tempo = Application.isPlaying ? Time.time : 0f;
            Shader.SetGlobalFloat(IdTempo, tempo);
            float rajada = 1f + rajadas * (Mathf.Sin(tempo * 0.071f) * 0.6f + Mathf.Sin(tempo * 0.029f + 1.3f) * 0.4f);
            Vector2 direcao = vento.sqrMagnitude > 0.001f ? vento.normalized : Vector2.right;

            float areaLivre = _zona.Count >= 3 ? Mathf.Max(0.2f, _aspecto - ComposicaoDasNuvens.Area(_zona)) : AreaLivreDeReferencia;
            AlvoDeNuvens = Mathf.Clamp(Mathf.RoundToInt(quantidade * areaLivre / AreaLivreDeReferencia), 4, _nuvens.Count);
            NuvensAtivas = 0;
            foreach (Nuvem n in _nuvens) if (n.estado != Estado.Dormindo) NuvensAtivas++;
            // Sobrou nuvem (a tela livre encolheu): uma por segundo se desfaz de mansinho.
            if (Application.isPlaying && NuvensAtivas > AlvoDeNuvens && Time.time >= _proximoAjuste)
            {
                _proximoAjuste = Time.time + 1f;
                foreach (Nuvem n in _nuvens)
                    if (n.estado == Estado.Vivendo) { Dissipar(n); break; }
            }

            float giroDaCamera = (_temVistaBase ? _rotacaoBase : camera3d.transform.rotation).eulerAngles.y;
            float menorFolga = float.PositiveInfinity;
            float menorAoNascer = float.PositiveInfinity;
            foreach (Nuvem n in _nuvens)
            {
                if (n.estado == Estado.Dormindo)
                {
                    if (Application.isPlaying && NuvensAtivas < AlvoDeNuvens)
                    {
                        Renascer(n, false);
                        NuvensAtivas++;
                    }
                    else
                    {
                        Esconder(n);
                        continue;
                    }
                }

                AvancarVida(n, deltaTime);
                if (n.estado == Estado.Dormindo) { Esconder(n); continue; }
                float vida = VidaDe(n);

                // Movimento em coordenadas de tela, desviando da zona protegida.
                Vector2 velocidadeUv = direcao * (velocidade * n.velocidade * rajada);
                var velocidadeTela = new Vector2(velocidadeUv.x * _aspecto, velocidadeUv.y);
                velocidadeTela += Afastamento(n) * separacao;
                float folga = FolgaDaZona(new Vector2(n.uv.x * _aspecto, n.uv.y), n.raioNaTela + margemDaZona, out Vector2 normal);
                velocidadeTela = ComposicaoDasNuvens.Desviar(velocidadeTela, folga, normal, faixaDeDesvio, n.lado);
                Vector2 antes = n.uv;
                n.uv.x = ReciclarCoordenada(n.uv.x + velocidadeTela.x / _aspecto * deltaTime);
                n.uv.y = ReciclarCoordenada(n.uv.y + velocidadeTela.y * deltaTime);
                // Saiu inteira da tela e esta se afastando: renasce ja (em vez de vagar invisivel ate dar a volta e
                // reentrar na mesma altura, o que esvazia o lado para onde o vento leva).
                var centro = new Vector2(n.uv.x * _aspecto, n.uv.y);
                bool foraDaTela = centro.x - n.raioNaTela > _aspecto || centro.x + n.raioNaTela < 0f ||
                    centro.y - n.raioNaTela > 1f || centro.y + n.raioNaTela < 0f;
                bool seAfastando = Vector2.Dot(velocidadeTela, centro - new Vector2(_aspecto * 0.5f, 0.5f)) > 0f;
                bool deuAVolta = Mathf.Abs(n.uv.x - antes.x) > 0.5f || Mathf.Abs(n.uv.y - antes.y) > 0.5f;
                if (!foraDaTela) n.apareceu = true;
                n.tempoForaDaTela = foraDaTela ? n.tempoForaDaTela + deltaTime : 0f;
                // So renasce ao sair quem chegou a aparecer (a que esta entrando pela borda nao conta); a que ficou
                // presa fora da tela por muito tempo (empurrada pelas vizinhas) tambem renasce.
                if (deuAVolta || (foraDaTela && seAfastando && n.apareceu) || n.tempoForaDaTela > 20f) Renascer(n, false);

                if (folga < faixaDeDesvio * 0.3f) n.tempoNaBorda += deltaTime;
                else n.tempoNaBorda = Mathf.Max(0f, n.tempoNaBorda - deltaTime);
                if (n.estado != Estado.Dissipando && (n.tempoNaBorda > tempoMaximoNaBorda || folga < -n.raioNaTela * 0.5f))
                {
                    Dissipar(n);
                    DesfeitasPelaZona++;
                }

                Posicionar(n, vida, tempo, giroDaCamera, ref menorFolga);
                if (n.estado == Estado.Nascendo) menorAoNascer = Mathf.Min(menorAoNascer, DistanciaRelativaAsOutras(n));
            }
            MenorFolgaVisivel = menorFolga;
            MenorDistanciaAoNascer = menorAoNascer;
        }

        void Posicionar(Nuvem n, float vida, float tempo, float giroDaCamera, ref float menorFolga)
        {
            float width = _alturaDaVista * Mathf.Lerp(tamanho.x, tamanho.y, n.largura);
            float respira = 1f + respiracao * Mathf.Sin(tempo * n.frequencia * 0.7f + n.fase2);
            float suave = ComposicaoDasNuvens.Suave(vida);
            float escalaDaVida = dissolverComRuido ? Mathf.Lerp(0.55f, 1f, suave) : Mathf.Lerp(0.02f, 1f, suave);
            var cheia = new Vector3(width, width * n.altura, width * n.profundidade);
            Vector3 scale = cheia * (respira * escalaDaVida);
            Mesh malha = n.filtro.sharedMesh;
            Bounds limites = malha.bounds;
            float topo = limites.max.y * cheia.y;
            float height = _baseDasIlhas - distanciaDasIlhas - topo - n.camada;
            // Nascendo, sobe ate o lugar; dissipando, afunda na nevoa.
            if (n.estado == Estado.Nascendo) height -= (1f - suave) * width * 0.12f;
            else if (n.estado == Estado.Dissipando) height -= (1f - suave) * width * 0.25f;
            if (!PontoNoPlano(n.uv, height, out Vector3 position)) { Esconder(n); return; }
            position.y += Mathf.Sin(tempo * n.frequencia + n.fase) * balanco * width;
            float giro = giroDaCamera + n.giro + Mathf.Sin(tempo * 0.05f + n.fase2) * giroLento;
            Quaternion rotacao = Quaternion.Euler(0f, giro, 0f);
            n.transform.SetPositionAndRotation(position, rotacao);
            n.transform.localScale = scale;
            n.renderer.enabled = true;

            // Raio na tela com o tamanho cheio (+ gomos inchados): a nuvem nasce pequena mas nao cresce para dentro da zona.
            Vector2 centroTela = ParaTela(position);
            n.raioNaTela = RaioNaTela(limites, position, rotacao, cheia * (1f + respiracao) * 1.12f, centroTela);
            float raioAgora = n.raioNaTela * escalaDaVida;
            if (vida > 0.05f)
                menorFolga = Mathf.Min(menorFolga, FolgaDaZona(centroTela, raioAgora, out _));

            if (_bloco == null) _bloco = new MaterialPropertyBlock();
            _bloco.Clear();
            _bloco.SetFloat(IdFase, n.fase2 * 3f);
            _bloco.SetFloat(IdDissolver, dissolverComRuido ? 1f - suave : 0f);
            _bloco.SetFloat(IdAnimacao, animacaoDosGomos);
            n.renderer.SetPropertyBlock(_bloco);

            if (n.sombra != null)
            {
                // Sombra reta para baixo, do fundo da nuvem ate o chao; some onde cairia dentro da zona.
                float fundo = position.y + limites.min.y * scale.y;
                n.sombra.transform.SetPositionAndRotation(new Vector3(position.x, fundo, position.z), Quaternion.Euler(90f, giro, 0f));
                n.sombra.size = new Vector3(limites.size.x * scale.x * tamanhoDaSombra,
                    limites.size.z * scale.z * tamanhoDaSombra * 1.2f, alcanceDaSombra);
                n.sombra.pivot = new Vector3(0f, 0f, alcanceDaSombra * 0.5f);
                Vector2 sombraNaTela = ParaTela(new Vector3(position.x, _baseDasIlhas - quedaAteOChao, position.z));
                float folgaDaSombra = FolgaDaZona(sombraNaTela, n.raioNaTela * tamanhoDaSombra * 0.5f + margemDaZona, out _);
                n.sombra.fadeFactor = forcaDaSombra * suave * Mathf.Clamp01(folgaDaSombra / 0.06f);
                n.sombra.enabled = true;
            }
        }

        // ---------- vida ----------

        void AvancarVida(Nuvem n, float deltaTime)
        {
            if (deltaTime <= 0f) return;
            n.progresso += deltaTime / Mathf.Max(0.1f, n.duracao);
            if (n.progresso < 1f) return;
            switch (n.estado)
            {
                case Estado.Nascendo:
                    n.estado = Estado.Vivendo;
                    n.progresso = 0f;
                    n.duracao = Entre(duracaoDaVida);
                    break;
                case Estado.Vivendo:
                    Dissipar(n);
                    break;
                case Estado.Dissipando:
                    if (NuvensAtivas > AlvoDeNuvens) { n.estado = Estado.Dormindo; NuvensAtivas--; }
                    else Renascer(n, false);
                    break;
            }
        }

        void Dissipar(Nuvem n)
        {
            float vida = VidaDe(n);
            n.estado = Estado.Dissipando;
            n.duracao = Entre(duracaoAoDissipar);
            // Continua do tamanho em que estava (uma nuvem nascendo que precisa sumir nao da salto).
            n.progresso = 1f - vida;
        }

        static float VidaDe(Nuvem n)
        {
            switch (n.estado)
            {
                case Estado.Nascendo: return Mathf.Clamp01(n.progresso);
                case Estado.Vivendo: return 1f;
                case Estado.Dissipando: return 1f - Mathf.Clamp01(n.progresso);
                default: return 0f;
            }
        }

        float Entre(Vector2 faixa) => Mathf.Lerp(faixa.x, faixa.y, (float)_sorteio.NextDouble());

        /// <summary>Nova nuvem no lugar desta: formato, tamanho e lugar novos (area livre ou entrando pela borda).</summary>
        void Renascer(Nuvem n, bool inicial)
        {
            Sortear(n);
            bool pelaBorda = !inicial && _sorteio.NextDouble() < chanceDeEntrarPelaBorda;
            if (!pelaBorda && !MelhorLugarLivre(n, out n.uv)) pelaBorda = true;
            if (pelaBorda) n.uv = PontoContraOVento(n);
            n.tempoNaBorda = 0f;
            n.tempoForaDaTela = 0f;
            n.apareceu = false;
            if (inicial)
            {
                // Fases desencontradas: a tela nunca nasce nem some inteira de uma vez.
                double sorteio = _sorteio.NextDouble();
                if (sorteio < 0.2) { n.estado = Estado.Nascendo; n.duracao = Entre(duracaoAoNascer); n.progresso = (float)_sorteio.NextDouble(); }
                else if (sorteio < 0.9) { n.estado = Estado.Vivendo; n.duracao = Entre(duracaoDaVida); n.progresso = (float)_sorteio.NextDouble() * 0.9f; }
                else { n.estado = Estado.Dissipando; n.duracao = Entre(duracaoAoDissipar); n.progresso = (float)_sorteio.NextDouble() * 0.6f; }
            }
            else if (pelaBorda)
            {
                n.estado = Estado.Vivendo;
                n.duracao = Entre(duracaoDaVida);
                n.progresso = 0f;
            }
            else
            {
                n.estado = Estado.Nascendo;
                n.duracao = Entre(duracaoAoNascer);
                n.progresso = 0f;
            }
            Renascimentos++;
        }

        void Sortear(Nuvem n)
        {
            System.Random random = _sorteio;
            // 30% pequenas, 50% medias, 20% grandes.
            double sorteio = random.NextDouble();
            int porte = sorteio < 0.3 ? 0 : sorteio < 0.8 ? 1 : 2;
            n.formato = EscolherFormato(porte);
            Mesh malha = _malhas[n.formato];
            n.filtro.sharedMesh = malha;
            n.renderer.localBounds = new Bounds(malha.bounds.center, malha.bounds.size * 1.3f);
            n.largura = porte == 0 ? Aleatorio(random, 0f, 0.22f) : porte == 1 ? Aleatorio(random, 0.28f, 0.62f) : Aleatorio(random, 0.7f, 1f);
            n.camada = profundidade * (porte == 0 ? Aleatorio(random, 0f, 0.35f) : porte == 1 ? Aleatorio(random, 0.2f, 0.65f) : Aleatorio(random, 0.55f, 1f));
            n.altura = Aleatorio(random, 0.85f, 1.1f);
            n.profundidade = Aleatorio(random, 0.8f, 1.05f);
            // As grandes e fundas andam mais devagar: parece paralaxe.
            n.velocidade = Aleatorio(random, 0.6f, 1.3f) * (porte == 0 ? 1.15f : porte == 2 ? 0.8f : 1f);
            n.fase = Aleatorio(random, 0f, Mathf.PI * 2f);
            n.fase2 = Aleatorio(random, 0f, Mathf.PI * 2f);
            n.frequencia = Aleatorio(random, 0.18f, 0.4f);
            // Lado comprido sempre de frente para a camera (de ponta a nuvem vira uma pilha de bolas);
            // metade vira 180 graus e mostra a outra face, o que dobra a variedade de silhuetas.
            n.giro = (random.NextDouble() < 0.5 ? 0f : 180f) + Aleatorio(random, -35f, 35f);
            n.lado = random.NextDouble() < 0.5 ? -1f : 1f;
            n.raioNaTela = RaioEstimado(n);
        }

        // Raio na tela de uma nuvem cheia (medido: ~0,75 da largura relativa, contando gomos inchados).
        float RaioEstimado(Nuvem n) => 0.75f * Mathf.Lerp(tamanho.x, tamanho.y, n.largura);

        // Nasce longe o bastante para nao comecar ja encostada na zona (senao conta como presa e se desfaz logo).
        float FolgaParaNascer => faixaDeDesvio * 0.3f + 0.02f;

        /// <summary>
        /// Lugar livre para nascer: vai para o quadrante da tela mais vazio em relacao ao espaco que ele tem (assim a
        /// moldura fica equilibrada, mesmo com o vento e o tabuleiro represando nuvens de um lado) e, nele, para o melhor
        /// candidato fora da zona e longe das outras nuvens. Sem lugar no quadrante, tenta a tela inteira.
        /// </summary>
        bool MelhorLugarLivre(Nuvem n, out Vector2 melhor)
        {
            var porQuadrante = new int[4];
            foreach (Nuvem outra in _nuvens)
                if (outra != n && outra.estado != Estado.Dormindo && outra.estado != Estado.Dissipando &&
                    outra.uv.x >= 0f && outra.uv.x <= 1f && outra.uv.y >= 0f && outra.uv.y <= 1f)
                    porQuadrante[Quadrante(outra.uv)]++;
            // Quadrante mais vazio em relacao ao que cabe nele.
            int alvo = -1;
            float menor = float.MaxValue;
            for (int q = 0; q < 4; q++)
            {
                if (_capacidade[q] < 1f) continue;
                float ocupacao = (porQuadrante[q] + (float)_sorteio.NextDouble() * 0.5f) / _capacidade[q];
                if (ocupacao < menor) { menor = ocupacao; alvo = q; }
            }
            if (alvo < 0) return MelhorLugarLivre(n, -1, out melhor);
            return MelhorLugarLivre(n, alvo, out melhor) || MelhorLugarLivre(n, -1, out melhor);
        }

        static int Quadrante(Vector2 uv) => (uv.x < 0.5f ? 0 : 1) + (uv.y < 0.5f ? 0 : 2);

        bool MelhorLugarLivre(Nuvem n, int quadrante, out Vector2 melhor)
        {
            melhor = Vector2.zero;
            float melhorNota = float.NegativeInfinity;
            float raio = RaioEstimado(n);
            Vector2 direcao = vento.sqrMagnitude > 0.001f ? vento.normalized : Vector2.right;
            for (int c = 0; c < 28; c++)
            {
                // Centro sempre dentro da tela (a nuvem pode ficar cortada pela borda, como numa moldura).
                float x0 = quadrante < 0 || quadrante % 2 == 0 ? 0.02f : 0.5f, x1 = quadrante < 0 || quadrante % 2 == 1 ? 0.98f : 0.5f;
                float y0 = quadrante < 0 || quadrante < 2 ? 0.02f : 0.5f, y1 = quadrante < 0 || quadrante >= 2 ? 0.98f : 0.5f;
                var uv = new Vector2(Aleatorio(_sorteio, x0, x1), Aleatorio(_sorteio, y0, y1));
                var tela = new Vector2(uv.x * _aspecto, uv.y);
                float folga = FolgaDaZona(tela, raio + margemDaZona, out _);
                if (folga < FolgaParaNascer) continue;
                // Nunca nasce encostada em outra nuvem: as duas sao opacas e a nova "brotaria de dentro" da vizinha.
                if (EncostaEmOutra(n, tela, raio)) continue;
                float perto = 1f;
                foreach (Nuvem outra in _nuvens)
                {
                    if (outra == n || outra.estado == Estado.Dormindo) continue;
                    perto = Mathf.Min(perto, Vector2.Distance(tela, new Vector2(outra.uv.x * _aspecto, outra.uv.y)) - outra.raioNaTela - raio);
                }
                float nota = perto * (0.75f + 0.25f * Mathf.Clamp01(folga / 0.25f));
                // A montante do tabuleiro a nuvem so andaria ate a borda e ficaria represada: evita nascer ali.
                for (int passo = 1; passo <= 6; passo++)
                {
                    Vector2 adiante = uv + direcao * (passo * 0.05f);
                    if (FolgaDaZona(new Vector2(adiante.x * _aspecto, adiante.y), raio + margemDaZona, out _) < 0f)
                    {
                        nota = nota > 0f ? nota * 0.3f : nota * 3f;
                        break;
                    }
                }
                if (nota > melhorNota) { melhorNota = nota; melhor = uv; }
            }
            return melhorNota > float.NegativeInfinity;
        }

        /// <summary>
        /// Entrada fora da tela, do lado de onde vem o vento, preferindo uma faixa em que o caminho reto ate o outro
        /// lado nao cruza a zona (a nuvem atravessa a tela pela moldura em vez de encalhar no tabuleiro).
        /// </summary>
        Vector2 PontoContraOVento(Nuvem n)
        {
            Vector2 direcao = vento.sqrMagnitude > 0.001f ? vento.normalized : Vector2.right;
            float raio = RaioEstimado(n);
            Vector2 melhor = Vector2.zero;
            float melhorFolga = float.NegativeInfinity;
            for (int tentativa = 0; tentativa < 10; tentativa++)
            {
                // Logo depois da borda: entra na tela em poucos segundos em vez de vagar invisivel.
                float foraX = raio / _aspecto + 0.01f, foraY = raio + 0.01f;
                Vector2 uv = Mathf.Abs(direcao.x) >= Mathf.Abs(direcao.y)
                    ? new Vector2(direcao.x > 0f ? -foraX : 1f + foraX, Aleatorio(_sorteio, 0.03f, 0.97f))
                    : new Vector2(Aleatorio(_sorteio, 0.03f, 0.97f), direcao.y > 0f ? -foraY : 1f + foraY);
                float pior = float.PositiveInfinity;
                for (int passo = 0; passo <= 12; passo++)
                {
                    Vector2 adiante = uv + direcao * (passo * 0.12f);
                    pior = Mathf.Min(pior, FolgaDaZona(new Vector2(adiante.x * _aspecto, adiante.y), raio + margemDaZona, out _));
                }
                // Entrar encostada em outra nuvem conta como caminho ruim.
                if (EncostaEmOutra(n, new Vector2(uv.x * _aspecto, uv.y), raio)) pior -= 1f;
                if (pior > melhorFolga) { melhorFolga = pior; melhor = uv; }
                if (pior > 0.03f) break;
            }
            return melhor;
        }

        float DistanciaRelativaAsOutras(Nuvem n)
        {
            var aqui = new Vector2(n.uv.x * _aspecto, n.uv.y);
            float menor = float.PositiveInfinity;
            foreach (Nuvem outra in _nuvens)
            {
                if (outra == n || outra.estado == Estado.Dormindo) continue;
                float soma = n.raioNaTela + outra.raioNaTela;
                if (soma > 1e-4f) menor = Mathf.Min(menor, Vector2.Distance(aqui, new Vector2(outra.uv.x * _aspecto, outra.uv.y)) / soma);
            }
            return menor;
        }

        bool EncostaEmOutra(Nuvem n, Vector2 tela, float raio)
        {
            foreach (Nuvem outra in _nuvens)
            {
                if (outra == n || outra.estado == Estado.Dormindo) continue;
                if (Vector2.Distance(tela, new Vector2(outra.uv.x * _aspecto, outra.uv.y)) < (outra.raioNaTela + raio) * 0.9f) return true;
            }
            return false;
        }

        /// <summary>Empurrao para longe das nuvens que encostam nesta na tela (proporcional ao quanto se sobrepoem).</summary>
        Vector2 Afastamento(Nuvem n)
        {
            var aqui = new Vector2(n.uv.x * _aspecto, n.uv.y);
            Vector2 empurrao = Vector2.zero;
            foreach (Nuvem outra in _nuvens)
            {
                if (outra == n || outra.estado == Estado.Dormindo) continue;
                Vector2 entre = aqui - new Vector2(outra.uv.x * _aspecto, outra.uv.y);
                float distancia = entre.magnitude, minimo = (n.raioNaTela + outra.raioNaTela) * 0.85f;
                if (distancia >= minimo) continue;
                empurrao += (distancia > 1e-4f ? entre / distancia : new Vector2(n.lado, 0f)) * (minimo - distancia);
            }
            return empurrao;
        }

        int EscolherFormato(int porte)
        {
            var candidatos = new List<int>();
            for (int f = 0; f < _portes.Count; f++) if (_portes[f] == porte) candidatos.Add(f);
            if (candidatos.Count == 0) return _sorteio.Next(_malhas.Count);
            return candidatos[_sorteio.Next(candidatos.Count)];
        }

        // ---------- zona e tela ----------

        void MedirVista()
        {
            if (_iso == null || _iso.gameObject != camera3d.gameObject) _iso = camera3d.GetComponent<HexIsoCamera>();
            // Zoom do jogador (HexIsoCamera, Sprint 4): as nuvens se medem pela vista do anel inteiro, nao pela vista
            // aproximada; senao ficariam presas a tela (mesmo tamanho e lugar) enquanto o mundo cresce e anda por baixo.
            _temVistaBase = _iso != null && _iso.TryVistaBase(out _origemBase, out _rotacaoBase, out _meiaAlturaBase);
            _aspecto = Mathf.Max(0.1f, camera3d.aspect);
            _alturaDaVista = _temVistaBase ? _meiaAlturaBase * 2f :
                camera3d.orthographic ? camera3d.orthographicSize * 2f :
                2f * Vector3.Distance(camera3d.transform.position, transform.position) *
                Mathf.Tan(camera3d.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>Base das ilhas e envoltoria delas na tela. Mapa novo reposiciona; ilha nova desfaz quem ficou por baixo.</summary>
        void MedirIlhas()
        {
            _baseDasIlhas = mundo != null ? mundo.transform.position.y - 3.5f : -3.5f;
            _pontosDaZona.Clear();
            if (mundo != null && mundo.TotalDeIlhas > 0)
            {
                float min = float.MaxValue;
                foreach (var ilha in mundo.Ilhas)
                {
                    bool tem = false;
                    var caixa = new Bounds();
                    foreach (var renderer in ilha.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (!renderer.enabled) continue;
                        min = Mathf.Min(min, renderer.bounds.min.y);
                        if (!tem) { caixa = renderer.bounds; tem = true; } else caixa.Encapsulate(renderer.bounds);
                    }
                    if (!tem) continue;
                    for (int c = 0; c < 8; c++)
                        _pontosDaZona.Add(ParaTela(caixa.center + Vector3.Scale(caixa.extents,
                            new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1))));
                }
                if (min < float.MaxValue) _baseDasIlhas = min;
            }
            List<Vector2> nova = _pontosDaZona.Count >= 3 ? ComposicaoDasNuvens.Envoltoria(_pontosDaZona) : new List<Vector2>();
            Vector2 centro = Vector2.zero;
            foreach (Vector2 p in nova) centro += p;
            if (nova.Count > 0) centro /= nova.Count;
            int ilhas = mundo != null ? mundo.TotalDeIlhas : 0;
            bool aspectoMudou = Mathf.Abs(_aspecto - _aspectoDaZona) > 0.005f;
            bool mudou = ilhas != _ilhasMedidas || aspectoMudou || Vector2.Distance(centro, _centroDaZona) > 0.01f || (nova.Count >= 3) != (_zona.Count >= 3);
            bool mapaNovo = _zona.Count < 3 || aspectoMudou || Mathf.Abs(ilhas - _ilhasMedidas) > 2 || Vector2.Distance(centro, _centroDaZona) > 0.05f;
            _aspectoDaZona = _aspecto;
            _zona = nova;
            _centroDaZona = centro;
            _ilhasMedidas = ilhas;
            MedirCapacidade();
            if (!mudou || _zona.Count < 3 || _sorteio == null) return;

            foreach (Nuvem n in _nuvens)
            {
                if (n.estado == Estado.Dormindo) continue;
                float folga = FolgaDaZona(new Vector2(n.uv.x * _aspecto, n.uv.y), n.raioNaTela + margemDaZona, out _);
                if (folga >= 0f) continue;
                if (mapaNovo || !Application.isPlaying)
                {
                    // Mapa novo (inicio da partida, nova semente): reposiciona de imediato, sem animacao.
                    if (MelhorLugarLivre(n, out Vector2 livre)) n.uv = livre;
                    else { n.estado = Estado.Dormindo; Esconder(n); }
                }
                else if (n.estado != Estado.Dissipando)
                {
                    // Ilha nova por carta: quem ficou por baixo se desfaz de mansinho (e e empurrado para fora).
                    Dissipar(n);
                    DesfeitasPelaZona++;
                }
            }
        }

        /// <summary>
        /// Quanto cabe de nuvem de tamanho tipico em cada quadrante da tela, fora da zona: e a meta de ocupacao de cada
        /// um (o tabuleiro raramente deixa a mesma folga nos quatro cantos).
        /// </summary>
        void MedirCapacidade()
        {
            float raio = 0.75f * Mathf.Lerp(tamanho.x, tamanho.y, 0.45f);
            for (int q = 0; q < 4; q++) _capacidade[q] = 0f;
            for (int i = 0; i < 24; i++)
                for (int j = 0; j < 16; j++)
                {
                    var uv = new Vector2((i + 0.5f) / 24f, (j + 0.5f) / 16f);
                    if (FolgaDaZona(new Vector2(uv.x * _aspecto, uv.y), raio + margemDaZona, out _) > FolgaParaNascer) _capacidade[Quadrante(uv)] += 1f;
                }
        }

        /// <summary>Ponto do mundo na tela da vista-base: viewport com x vezes o aspecto (unidade = altura da tela).</summary>
        public Vector2 ParaTela(Vector3 ponto)
        {
            if (_temVistaBase)
            {
                Vector3 relativo = ponto - _origemBase;
                return new Vector2(Vector3.Dot(relativo, _rotacaoBase * Vector3.right) / (2f * _meiaAlturaBase) + _aspecto * 0.5f,
                    Vector3.Dot(relativo, _rotacaoBase * Vector3.up) / (2f * _meiaAlturaBase) + 0.5f);
            }
            Vector3 vp = camera3d.WorldToViewportPoint(ponto);
            return new Vector2(vp.x * _aspecto, vp.y);
        }

        /// <summary>Distancia entre um circulo na tela e a zona protegida (negativa = sobrepondo). Sem zona: +infinito.</summary>
        public float FolgaDaZona(Vector2 centroNaTela, float raio, out Vector2 normal)
        {
            float d = ComposicaoDasNuvens.Distancia(centroNaTela, _zona, out normal);
            return float.IsPositiveInfinity(d) ? d : d - raio;
        }

        float RaioNaTela(Bounds limites, Vector3 posicao, Quaternion rotacao, Vector3 escala, Vector2 centro)
        {
            float raio = 0f;
            for (int c = 0; c < 8; c++)
            {
                var canto = new Vector3(c & 1, (c >> 1) & 1, (c >> 2) & 1);
                Vector3 local = limites.min + Vector3.Scale(limites.size, canto);
                Vector2 tela = ParaTela(posicao + rotacao * Vector3.Scale(local, escala));
                raio = Mathf.Max(raio, Vector2.Distance(tela, centro));
            }
            return raio;
        }

        bool PontoNoPlano(Vector2 uv, float altura, out Vector3 ponto)
        {
            ponto = Vector3.zero;
            var plano = new Plane(Vector3.up, new Vector3(0f, altura, 0f));
            for (int i = 0; i < 3; i++)
            {
                var viewport = new Vector3(i % 2, i / 2, 0f);
                Ray raio = _temVistaBase
                    ? RaioDaVistaBase(viewport, _origemBase, _rotacaoBase, _meiaAlturaBase, camera3d.aspect)
                    : camera3d.ViewportPointToRay(viewport);
                if (!plano.Raycast(raio, out float distancia)) return false;
                _cantos[i] = raio.GetPoint(distancia);
            }
            ponto = _cantos[0] + (_cantos[1] - _cantos[0]) * uv.x + (_cantos[2] - _cantos[0]) * uv.y;
            return true;
        }

        /// <summary>Raio de um ponto da vista ortografica do anel inteiro (igual a ViewportPointToRay sem zoom).</summary>
        public static Ray RaioDaVistaBase(Vector3 viewport, Vector3 origem, Quaternion rotacao, float meiaAltura, float aspecto)
        {
            Vector3 lado = rotacao * Vector3.right * ((viewport.x * 2f - 1f) * meiaAltura * aspecto);
            Vector3 cima = rotacao * Vector3.up * ((viewport.y * 2f - 1f) * meiaAltura);
            return new Ray(origem + lado + cima, rotacao * Vector3.forward);
        }

        public static float ReciclarCoordenada(float coordinate)
        {
            return Mathf.Repeat(coordinate + Margem, 1f + Margem * 2f) - Margem;
        }

        /// <summary>Porte do formato <paramref name="indice"/>: 0 pequena, 1 media, 2 grande (3 : 5 : 2 a cada 10).</summary>
        public static int PorteDoFormato(int indice)
        {
            int[] ciclo = { 1, 0, 1, 2, 1, 0, 1, 2, 1, 0 };
            return ciclo[indice % ciclo.Length];
        }

        static float Aleatorio(System.Random random, float a, float b) => a + (float)random.NextDouble() * (b - a);

        // ---------- criacao ----------

        void CriarNuvens()
        {
            Limpar();
            _sementeAtual = semente; _materialAtual = material; _sombraAtual = materialDaSombra;
            _formatosAtuais = formatos; _subdivisoesAtuais = subdivisoes; _quantidadeAtual = quantidade;
            _sorteio = new System.Random(semente);
            for (int f = 0; f < Mathf.Max(3, formatos); f++)
            {
                int porte = PorteDoFormato(f);
                Mesh malha = MalhaDeNuvem.Criar(_sorteio, subdivisoes, porte);
                malha.name = "Nuvem toon " + (f + 1) + " (" + (porte == 0 ? "pequena" : porte == 1 ? "media" : "grande") + ")";
                malha.hideFlags = HideFlags.DontSave;
                _malhas.Add(malha);
                _portes.Add(porte);
            }
            MedirVista();
            _zona = new List<Vector2>();
            _ilhasMedidas = -1;
            _aspectoDaZona = -1f;
            int conjunto = Mathf.Min(30, Mathf.CeilToInt(quantidade * 1.8f));
            for (int i = 0; i < conjunto; i++)
            {
                var go = new GameObject("Nuvem toon " + (i + 1));
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(transform, false);
                var n = new Nuvem
                {
                    transform = go.transform,
                    filtro = go.AddComponent<MeshFilter>(),
                    renderer = go.AddComponent<MeshRenderer>(),
                    estado = Estado.Dormindo
                };
                n.renderer.sharedMaterial = material;
                n.renderer.shadowCastingMode = ShadowCastingMode.Off;
                n.renderer.receiveShadows = false;
                n.renderer.lightProbeUsage = LightProbeUsage.Off;
                n.renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (materialDaSombra != null)
                {
                    var goSombra = new GameObject("Sombra");
                    goSombra.hideFlags = HideFlags.DontSave;
                    goSombra.transform.SetParent(go.transform, false);
                    n.sombra = goSombra.AddComponent<DecalProjector>();
                    n.sombra.material = materialDaSombra;
                    n.sombra.scaleMode = DecalScaleMode.ScaleInvariant;
                    n.sombra.drawDistance = 1000f;
                }
                _nuvens.Add(n);
                Sortear(n);
                Esconder(n);
            }
            for (int i = 0; i < Mathf.Min(quantidade, _nuvens.Count); i++) Renascer(_nuvens[i], true);
        }

        static void Esconder(Nuvem n)
        {
            if (n.renderer != null) n.renderer.enabled = false;
            if (n.sombra != null) n.sombra.enabled = false;
        }

        void Limpar()
        {
            foreach (Nuvem nuvem in _nuvens)
                if (nuvem.transform != null)
                {
                    // Destroy aguarda o fim do quadro; evite sobrepor a camada antiga a nova.
                    nuvem.transform.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(nuvem.transform.gameObject);
                    else DestroyImmediate(nuvem.transform.gameObject);
                }
            _nuvens.Clear();
            foreach (Mesh malha in _malhas)
                if (malha != null)
                {
                    if (Application.isPlaying) Destroy(malha);
                    else DestroyImmediate(malha);
                }
            _malhas.Clear();
            _portes.Clear();
        }
        void OnDisable() => Limpar();
        void OnDestroy() => Limpar();

        void OnDrawGizmosSelected()
        {
            // Zona protegida desenhada no plano das nuvens (visivel na Scene com a camada selecionada).
            if (_zona.Count < 3 || camera3d == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            float altura = _baseDasIlhas - distanciaDasIlhas;
            for (int i = 0; i < _zona.Count; i++)
            {
                if (!PontoNoPlano(new Vector2(_zona[i].x / _aspecto, _zona[i].y), altura, out Vector3 a)) return;
                Vector2 proximo = _zona[(i + 1) % _zona.Count];
                if (!PontoNoPlano(new Vector2(proximo.x / _aspecto, proximo.y), altura, out Vector3 b)) return;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}
