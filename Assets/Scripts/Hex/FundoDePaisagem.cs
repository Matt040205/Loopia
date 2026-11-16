using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loopia.Hex
{
    /// <summary>
    /// Fundo de paisagem (Pass B): vale em miniatura visto de muito alto, gerado ao carregar a cena a partir dos modelos do
    /// KayKit Forest Nature Pack (CC0). Substitui a maquete "Cenario background" + "Rio de fundo".
    ///
    /// Regras:
    /// - Nada aqui toca ilhas, NavMesh, camera ou gameplay. O objeto fica fora de "Mundo" (o NavMeshSurface coleta so
    ///   os filhos de Mundo), sem collider, sem sombra.
    /// - Camera ortografica: distancia nao encolhe nada na tela. Profundidade vem de (1) escala de mundo menor que o
    ///   dressing das ilhas, (2) materiais de paisagem dessaturados e (3) altura (y = -24): a nevoa linear do
    ///   DayNightController da perspectiva aerea de baixo para cima da tela.
    /// - Detalhe por zona da tela: mais calmo atras do anel e na faixa de baixo (mao de cartas), denso nas laterais,
    ///   rio no vale da faixa de cima.
    /// - Deterministico (semente fixa). As malhas sao geradas em memoria (nada de malha versionada no Git) e
    ///   combinadas por material e por bloco (poucos draw calls). Os FBX usados precisam de Read/Write ligado.
    /// Origem: previa v6 aprovada pelo responsavel em 2026-11-15 (docs/art/pass-b/RELATORIO_PASS_B.md).
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class FundoDePaisagem : MonoBehaviour
    {
        public const string NomeDoGerado = "Paisagem (gerada)";

        [Header("Geracao")]
        public int semente = 20261115;
        [Tooltip("Gera tambem fora do Play (para ver no Editor). Nunca e salvo na cena.")]
        public bool gerarNoEditor = true;

        [Header("Geometria do vale (u = direita da tela, v = para longe da camera)")]
        public float alturaDoChao = -24f;
        public float meiaLargura = 64f;
        public float meiaProfundidade = 52f;
        public float celula = 2f;

        [Tooltip("Faixa so de chao (sem arvores, pedras nem lagoas) em volta do vale. A vista do anel inteiro pode passar " +
                 "de +-52 no chao (4:3, 16:10, anel grande); alem da borda aparecia a cor de fundo da camera (a faixa rosa " +
                 "do fim de tarde). O detalhe do vale continua so dentro de meiaLargura x meiaProfundidade. 0 = vale antigo.")]
        [Min(0f)] public float bordaDeChao = 24f;

        /// <summary>Meia largura do chao gerado (vale + borda).</summary>
        public float MeiaLarguraDoChao => meiaLargura + bordaDeChao;

        /// <summary>Meia profundidade do chao gerado (vale + borda).</summary>
        public float MeiaProfundidadeDoChao => meiaProfundidade + bordaDeChao;

        [Header("Modelos (KayKit Forest Nature Pack, CC0)")]
        public GameObject[] arvores;        // borda de mata, bosquetes, solitarias
        public GameObject[] arvoresAltas;   // emergentes
        public GameObject[] secas;
        public GameObject[] copaLeve;       // miolo da mata (repeticoes = peso)
        public GameObject[] moitas;
        public GameObject[] capim;
        public GameObject[] pedrasPequenas;
        public GameObject[] pedrasMedias;

        [Header("Materiais")]
        public Material[] folhas = new Material[3];   // base, oliva, fria
        public Material[] copas = new Material[3];    // base, oliva, fria (mais escuras)
        public Material acento;
        public Material solo;
        public Material agua;

        [System.NonSerialized] public long triangulosGerados;
        [System.NonSerialized] public float milissegundosDaGeracao;

        static readonly Vector3 Direita = new Vector3(0.70710678f, 0f, -0.70710678f);
        static readonly Vector3 Fundo = new Vector3(0.70710678f, 0f, 0.70710678f);
        const float NivelDaAgua = -0.55f;

        float _ox, _oz;
        readonly List<Vector3> _lagoas = new List<Vector3>(); // (u, v, raio)
        readonly List<Mesh> _malhas = new List<Mesh>();
        GameObject _gerado;

        void OnEnable()
        {
            if (!Application.isPlaying && !gerarNoEditor) return;
            Gerar();
        }

        void OnDisable() => Limpar();

        public GameObject Gerado => _gerado;

        public void Limpar()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                if (c.name == NomeDoGerado) Destruir(c);
            }
            foreach (var m in _malhas) if (m != null) Destruir(m);
            _malhas.Clear();
            _gerado = null;
        }

        static void Destruir(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        bool Configurado()
        {
            bool ok = arvores != null && arvores.Length > 0 && arvoresAltas != null && arvoresAltas.Length > 0 &&
                      secas != null && secas.Length > 0 && copaLeve != null && copaLeve.Length > 0 &&
                      moitas != null && moitas.Length > 0 && capim != null && capim.Length > 0 &&
                      pedrasPequenas != null && pedrasPequenas.Length > 0 && pedrasMedias != null && pedrasMedias.Length > 0 &&
                      folhas != null && folhas.Length == 3 && copas != null && copas.Length == 3 &&
                      acento != null && solo != null && agua != null;
            if (ok) foreach (var m in folhas) ok &= m != null;
            if (ok) foreach (var m in copas) ok &= m != null;
            return ok;
        }

        public void Gerar()
        {
            Limpar();
            if (!Configurado())
            {
                Debug.LogWarning("[FundoDePaisagem] Modelos ou materiais nao atribuidos; fundo nao gerado.", this);
                return;
            }
            var relogio = System.Diagnostics.Stopwatch.StartNew();
            _gerado = new GameObject(NomeDoGerado) { hideFlags = HideFlags.DontSave };
            _gerado.transform.SetParent(transform, false);
            _gerado.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _gerado.transform.localScale = Vector3.one;
            triangulosGerados = 0;

            var centro = new Vector3(0f, alturaDoChao, 0f) + Fundo * (-alturaDoChao * 1.7320508f);
            var rnd = new System.Random(semente);
            _ox = (float)rnd.NextDouble() * 100f; _oz = (float)rnd.NextDouble() * 100f;

            // ---------- lagoas (antes do terreno: o terreno escava em volta) ----------
            _lagoas.Clear();
            for (int tent = 0; tent < 400 && _lagoas.Count < 4; tent++)
            {
                float u = (float)(rnd.NextDouble() * 2 - 1) * (meiaLargura - 10), v = (float)(rnd.NextDouble() * 2 - 1) * (meiaProfundidade - 12);
                float raio = 1.6f + (float)rnd.NextDouble() * 1.8f;
                if (Densidade(u, v) < 0.85f || v < -16f || Mathf.Abs(v - RioV(u)) < 9f) continue; // fora do anel e da faixa da HUD
                bool longe = true;
                foreach (var l in _lagoas) if (new Vector2(l.x - u, l.y - v).magnitude < 18f) longe = false;
                if (longe) _lagoas.Add(new Vector3(u, v, raio));
            }

            // ---------- terreno low-poly (cantos deslocados, um tom por faceta) ----------
            // O chao vai alem do vale (bordaDeChao) para a vista nunca alcancar a borda; o resto do detalhe fica no vale.
            float mu = MeiaLarguraDoChao, mv = MeiaProfundidadeDoChao;
            int nu = Mathf.RoundToInt(2 * mu / celula), nv = Mathf.RoundToInt(2 * mv / celula);
            var vert = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i < nu; i++)
                for (int j = 0; j < nv; j++)
                {
                    var a = Canto(i, j, nu, nv, mu, mv); var b = Canto(i + 1, j, nu, nv, mu, mv);
                    var c = Canto(i + 1, j + 1, nu, nv, mu, mv); var d = Canto(i, j + 1, nu, nv, mu, mv);
                    bool diag = ((i + j) & 1) == 0;
                    Triangulo(diag ? new[] { a, d, c } : new[] { a, d, b }, centro, vert, uv, tri);
                    Triangulo(diag ? new[] { a, c, b } : new[] { b, d, c }, centro, vert, uv, tri);
                }
            NovoRenderer("Terreno", NovaMalha("terreno", vert, uv, tri), solo);

            // ---------- agua: rio + lagoas ----------
            vert.Clear(); uv.Clear(); tri.Clear();
            for (float u = -mu; u <= mu + 0.01f; u += 1f) // o rio segue ate a borda do chao, sem leito seco nas laterais
            {
                float vr = RioV(u), w = RioLargura(u) * 0.5f;
                var nrm = new Vector2(-(RioV(u + 0.5f) - RioV(u - 0.5f)), 1f).normalized;
                var e = new Vector2(u, vr) - nrm * w; var f = new Vector2(u, vr) + nrm * w;
                vert.Add(Mundo(centro, e.x, e.y, NivelDaAgua)); vert.Add(Mundo(centro, f.x, f.y, NivelDaAgua));
                uv.Add(new Vector2(0, u)); uv.Add(new Vector2(1, u));
                int k = vert.Count;
                if (k >= 4) tri.AddRange(new[] { k - 4, k - 3, k - 2, k - 3, k - 1, k - 2 });
            }
            foreach (var l in _lagoas)
            {
                int c0 = vert.Count; int lados = 20;
                vert.Add(Mundo(centro, l.x, l.y, NivelDaAgua)); uv.Add(Vector2.zero);
                for (int s = 0; s < lados; s++)
                {
                    float ang = s * Mathf.PI * 2f / lados;
                    float rr = l.z * (0.7f + 0.55f * Mathf.PerlinNoise(l.x * 0.37f + Mathf.Cos(ang) * 1.3f, l.y * 0.37f + Mathf.Sin(ang) * 1.3f));
                    vert.Add(Mundo(centro, l.x + Mathf.Cos(ang) * rr, l.y + Mathf.Sin(ang) * rr, NivelDaAgua)); uv.Add(Vector2.zero);
                }
                for (int s = 0; s < lados; s++) tri.AddRange(new[] { c0, c0 + 1 + (s + 1) % lados, c0 + 1 + s });
            }
            NovoRenderer("Agua", NovaMalha("agua", vert, uv, tri), agua);

            // ---------- vegetacao e pedras ----------
            var mArvores = Modelos(arvores); var mAltas = Modelos(arvoresAltas); var mSecas = Modelos(secas);
            var mCopa = Modelos(copaLeve); var mMoitas = Modelos(moitas); var mCapim = Modelos(capim);
            var mPedrasP = Modelos(pedrasPequenas); var mPedrasM = Modelos(pedrasMedias);

            var lotes = new Dictionary<(Material, int), List<CombineInstance>>();
            void Por(Modelo m, Material mat, float u, float v, float altura, float giro, float deitar = 0f)
            {
                if (u < -meiaLargura + 0.5f || u > meiaLargura - 0.5f || v < -meiaProfundidade + 0.5f || v > meiaProfundidade - 0.5f) return;
                if (NaAgua(u, v, 0.35f)) return;
                float h = Altura(u, v);
                float s = altura / Mathf.Max(1e-4f, m.limites.size.y);
                var mtx = Matrix4x4.TRS(Mundo(centro, u, v, h - 0.05f), Quaternion.Euler(0, giro, 0) * Quaternion.Euler(deitar, 0, 0), Vector3.one)
                          * Matrix4x4.Translate(new Vector3(-m.limites.center.x * s, -m.limites.min.y * s, -m.limites.center.z * s))
                          * Matrix4x4.Scale(Vector3.one * s);
                int bloco = Mathf.Clamp(Mathf.FloorToInt((u + meiaLargura) / (2 * meiaLargura) * 4), 0, 3)
                            + 4 * Mathf.Clamp(Mathf.FloorToInt((v + meiaProfundidade) / (2 * meiaProfundidade) * 3), 0, 2);
                if (!lotes.TryGetValue((mat, bloco), out var lista)) lotes[(mat, bloco)] = lista = new List<CombineInstance>();
                foreach (var (malha, local) in m.partes)
                    for (int sm = 0; sm < malha.subMeshCount; sm++)
                        lista.Add(new CombineInstance { mesh = malha, subMeshIndex = sm, transform = mtx * local });
            }
            T Um<T>(List<T> l) => l[rnd.Next(l.Count)];
            float F(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            int Tom(float u, float v) { float t = Mathf.PerlinNoise(_ox + 300 + u / 17f, _oz + 200 + v / 17f); return t < 0.4f ? 2 : t < 0.62f ? 0 : 1; }

            // 1) matas com volume: macico (copa + copa secundaria + emergente) no miolo; arvore/copa + arbusto na borda
            float passo = 1.65f;
            for (float u = -meiaLargura + 1; u < meiaLargura - 1; u += passo)
                for (float v = -meiaProfundidade + 1; v < meiaProfundidade - 1; v += passo)
                {
                    float ju = u + F(-0.45f, 0.45f) * passo, jv = v + F(-0.45f, 0.45f) * passo;
                    float dens = Densidade(ju, jv);
                    float flo = Floresta(ju, jv) * dens;
                    double r = rnd.NextDouble();
                    int tom = Tom(ju, jv);
                    if (r < flo * 0.78f)
                    {
                        if (flo > 0.5f)
                        {
                            Por(Um(mCopa), copas[tom], ju, jv, F(0.7f, 1.2f), F(0, 360));
                            if (rnd.NextDouble() < 0.5) Por(Um(mCopa), copas[tom], ju + F(-0.5f, 0.5f), jv + F(-0.55f, 0.55f), F(0.5f, 0.9f), F(0, 360));
                            if (rnd.NextDouble() < 0.07)
                            {
                                bool ac = rnd.NextDouble() < 0.05;
                                Por(Um(mAltas), ac ? acento : folhas[tom], ju, jv, F(1.15f, 1.6f), F(0, 360));
                            }
                        }
                        else
                        {
                            bool ac = rnd.NextDouble() < 0.03;
                            if (rnd.NextDouble() < 0.3) Por(Um(mArvores), ac ? acento : folhas[tom], ju, jv, F(0.85f, 1.35f), F(0, 360));
                            else Por(Um(mCopa), copas[tom], ju, jv, F(0.55f, 0.95f), F(0, 360));
                            if (rnd.NextDouble() < 0.5) Por(Um(mMoitas), copas[tom], ju + F(-0.7f, 0.7f), jv + F(-0.7f, 0.7f), F(0.3f, 0.5f), F(0, 360));
                        }
                    }
                }

            // 2) vida no campo (mais rara atras do anel e na faixa da HUD)
            for (int k = 0; k < 46; k++) // bosquetes
            {
                float u = F(-meiaLargura + 3, meiaLargura - 3), v = F(-meiaProfundidade + 3, meiaProfundidade - 3);
                { float dd = Densidade(u, v); if (rnd.NextDouble() > dd * dd * 0.9f + 0.05f || Floresta(u, v) > 0.5f) continue; }
                int n = rnd.Next(3, 9); float raio = F(1.2f, 2.6f); int tom = Tom(u, v);
                for (int i = 0; i < n; i++)
                {
                    float a = F(0, Mathf.PI * 2), d = raio * Mathf.Sqrt((float)rnd.NextDouble());
                    if (rnd.NextDouble() < 0.6) Por(Um(mArvores), folhas[tom], u + Mathf.Cos(a) * d, v + Mathf.Sin(a) * d, F(0.8f, 1.3f), F(0, 360));
                    else Por(Um(mMoitas), copas[tom], u + Mathf.Cos(a) * d, v + Mathf.Sin(a) * d, F(0.3f, 0.55f), F(0, 360));
                }
            }
            for (int k = 0; k < 900; k++) // arvores solitarias, moitas e tufos de capim
            {
                float u = F(-meiaLargura + 1, meiaLargura - 1), v = F(-meiaProfundidade + 1, meiaProfundidade - 1);
                float dens = Densidade(u, v);
                if (Floresta(u, v) > 0.55f || rnd.NextDouble() > 0.1f + 0.9f * dens * dens) continue;
                double r = rnd.NextDouble(); int tom = Tom(u, v);
                if (r < 0.10) Por(Um(mArvores), rnd.NextDouble() < 0.04 ? acento : folhas[tom], u, v, F(0.8f, 1.25f), F(0, 360));
                else if (r < 0.40) Por(Um(mMoitas), copas[tom], u, v, F(0.25f, 0.5f), F(0, 360));
                else
                {
                    int n = rnd.Next(2, 5);
                    for (int i = 0; i < n; i++) Por(Um(mCapim), folhas[tom], u + F(-0.4f, 0.4f), v + F(-0.4f, 0.4f), F(0.25f, 0.42f), F(0, 360));
                }
            }
            for (int k = 0; k < 40; k++) // montes de pedra
            {
                float u = F(-meiaLargura + 2, meiaLargura - 2), v = F(-meiaProfundidade + 2, meiaProfundidade - 2);
                if (rnd.NextDouble() > Densidade(u, v) || Floresta(u, v) > 0.6f) continue;
                Por(Um(mPedrasM), folhas[0], u, v, F(0.45f, 0.8f), F(0, 360));
                int n = rnd.Next(2, 6);
                for (int i = 0; i < n; i++) Por(Um(mPedrasP), folhas[0], u + F(-0.9f, 0.9f), v + F(-0.9f, 0.9f), F(0.15f, 0.35f), F(0, 360));
            }
            for (int k = 0; k < 34; k++) // arvores secas em pe e troncos caidos
            {
                float u = F(-meiaLargura + 2, meiaLargura - 2), v = F(-meiaProfundidade + 2, meiaProfundidade - 2);
                if (rnd.NextDouble() > Densidade(u, v)) continue;
                bool caida = rnd.NextDouble() < 0.45;
                Por(Um(mSecas), folhas[0], u, v, caida ? F(0.6f, 0.95f) : F(0.8f, 1.15f), F(0, 360), caida ? 88f : 0f);
            }
            // 3) beira d'agua: juncos e pedras no rio; margem das lagoas
            for (float u = -meiaLargura + 1; u < meiaLargura - 1; u += 0.7f)
                foreach (var lado in new[] { -1f, 1f })
                {
                    if (rnd.NextDouble() > 0.55) continue;
                    float v = RioV(u) + lado * (RioLargura(u) * 0.5f + F(0.4f, 1.1f));
                    if (rnd.NextDouble() < 0.6) Por(Um(mCapim), folhas[2], u, v, F(0.3f, 0.5f), F(0, 360));
                    else if (rnd.NextDouble() < 0.5) Por(Um(mPedrasP), folhas[0], u, v, F(0.2f, 0.4f), F(0, 360));
                    else Por(Um(mMoitas), copas[2], u, v, F(0.3f, 0.5f), F(0, 360));
                }
            foreach (var l in _lagoas)
                for (int i = 0; i < 22; i++)
                {
                    float a = F(0, Mathf.PI * 2), d = l.z * 1.12f + F(0.1f, 0.9f);
                    float u = l.x + Mathf.Cos(a) * d, v = l.y + Mathf.Sin(a) * d;
                    double r = rnd.NextDouble();
                    if (r < 0.4) Por(Um(mCapim), folhas[2], u, v, F(0.3f, 0.5f), F(0, 360));
                    else if (r < 0.7) Por(Um(mPedrasP), folhas[0], u, v, F(0.2f, 0.45f), F(0, 360));
                    else Por(Um(mMoitas), copas[Tom(u, v)], u, v, F(0.3f, 0.55f), F(0, 360));
                }

            foreach (var kv in lotes)
            {
                var malha = new Mesh { indexFormat = IndexFormat.UInt32, name = "paisagem_" + kv.Key.Item1.name + "_" + kv.Key.Item2, hideFlags = HideFlags.DontSave };
                malha.CombineMeshes(kv.Value.ToArray(), true, true);
                malha.RecalculateBounds();
                malha.UploadMeshData(true);
                _malhas.Add(malha);
                NovoRenderer(malha.name, malha, kv.Key.Item1);
            }
            relogio.Stop();
            milissegundosDaGeracao = (float)relogio.Elapsed.TotalMilliseconds;
        }

        // ---------- campos ----------
        static float RioV(float u) => 25f + 4.5f * Mathf.Sin(u / 11f + 0.4f) + 2.2f * Mathf.Sin(u / 4.7f + 1.3f);
        static float RioLargura(float u) => 2.6f + 0.9f * Mathf.Sin(u / 8.3f + 2f);

        bool NaAgua(float u, float v, float folga)
        {
            if (Mathf.Abs(v - RioV(u)) < RioLargura(u) * 0.5f + folga) return true;
            foreach (var l in _lagoas) if (new Vector2(u - l.x, v - l.y).magnitude < l.z * 1.15f + folga) return true;
            return false;
        }

        float Morro(float u, float v) =>
            Mathf.PerlinNoise(_ox + u / 19f, _oz + v / 19f) * 0.75f + Mathf.PerlinNoise(_ox + 40 + u / 7f, _oz + v / 7f) * 0.25f;

        float Altura(float u, float v)
        {
            float lateral = Mathf.Clamp01((Mathf.Abs(u) - 14f) / 22f);
            float h = (Morro(u, v) - 0.45f) * (1.2f + 6f * lateral);
            float serra = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - 27f) / 16f));
            h += serra * (3f + 4f * Mathf.PerlinNoise(_ox + 7 + u / 9f, _oz + 3 + v / 9f));
            float dRio = Mathf.Abs(v - RioV(u));
            float vale = Mathf.Clamp01(1f - (dRio - RioLargura(u) * 0.5f) / 5f);
            h = Mathf.Lerp(h, -1.1f, vale * vale);
            foreach (var l in _lagoas)
            {
                float d = new Vector2(u - l.x, v - l.y).magnitude;
                float bacia = Mathf.Clamp01(1f - (d - l.z) / 3f);
                h = Mathf.Lerp(h, -1.1f, bacia * bacia);
            }
            return h;
        }

        /// <summary>0..1: quanto detalhe a zona da tela pode ter (anel e faixa da HUD mais calmos).</summary>
        public static float Densidade(float u, float v)
        {
            float anel = Mathf.Clamp01(1f - Mathf.Sqrt((u * u) / (17f * 17f) + (v * v) / (24f * 24f)));
            float hud = Mathf.Clamp01((-v - 22f) / 8f);
            return Mathf.Clamp01(1f - 0.72f * Mathf.SmoothStep(0, 1, anel * 1.6f) - 0.6f * hud);
        }

        float Floresta(float u, float v)
        {
            float f = Mathf.PerlinNoise(_ox + 70 + u / 13f, _oz + 11 + v / 13f) * 0.8f + Mathf.PerlinNoise(_ox + u / 4f, _oz + 90 + v / 4f) * 0.2f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((f - 0.32f) / 0.32f));
        }

        Vector2 Canto(int i, int j, int nu, int nv, float mu, float mv)
        {
            float u = -mu + i * celula, v = -mv + j * celula;
            if (i == 0 || j == 0 || i == nu || j == nv) return new Vector2(u, v);
            float hx = Mathf.Repeat(Mathf.Sin(i * 12.9898f + j * 78.233f) * 43758.5453f, 1f) - 0.5f;
            float hz = Mathf.Repeat(Mathf.Sin(i * 39.3468f + j * 11.135f) * 24634.6345f, 1f) - 0.5f;
            return new Vector2(u + hx * celula * 0.64f, v + hz * celula * 0.64f);
        }

        static Vector3 Mundo(Vector3 centro, float u, float v, float h) => centro + Direita * u + Fundo * v + Vector3.up * h;

        /// <summary>
        /// Menor folga, em unidades do vale, entre os cantos de uma vista ortografica (projetados no plano do chao) e a
        /// borda do chao gerado. Negativa = a vista passa da borda e aparece a cor de fundo da camera. O relevo da
        /// borda (morros de ~3 u) come uns 5 u disso, entao quem usa deve exigir folga acima disso.
        /// </summary>
        public static float FolgaDaVista(Vector3 posicaoDaCamera, Quaternion rotacao, float meiaAltura, float aspecto,
            float alturaDoChao, float meiaLarguraDoChao, float meiaProfundidadeDoChao)
        {
            Vector3 centro = new Vector3(0f, alturaDoChao, 0f) + Fundo * (-alturaDoChao * 1.7320508f);
            var plano = new Plane(Vector3.up, new Vector3(0f, alturaDoChao, 0f));
            float folga = float.MaxValue;
            for (int c = 0; c < 4; c++)
            {
                float x = (c % 2) * 2f - 1f, y = (c / 2) * 2f - 1f;
                var raio = new Ray(posicaoDaCamera + rotacao * new Vector3(x * meiaAltura * aspecto, y * meiaAltura, 0f),
                    rotacao * Vector3.forward);
                if (!plano.Raycast(raio, out float distancia)) return float.NegativeInfinity;
                Vector3 p = raio.GetPoint(distancia) - centro;
                float u = Vector3.Dot(p, Direita), v = Vector3.Dot(p, Fundo);
                folga = Mathf.Min(folga, Mathf.Min(meiaLarguraDoChao - Mathf.Abs(u), meiaProfundidadeDoChao - Mathf.Abs(v)));
            }
            return folga;
        }

        float Retalho(float u, float v)
        {
            float wu = u + (Mathf.PerlinNoise(u * 0.11f + _ox, v * 0.11f) - 0.5f) * 6f;
            float wv = v + (Mathf.PerlinNoise(u * 0.11f, v * 0.11f + _oz) - 0.5f) * 6f;
            int cu = Mathf.FloorToInt(wu / 9f), cv = Mathf.FloorToInt(wv / 7f);
            return Mathf.Repeat(Mathf.Sin(cu * 127.1f + cv * 311.7f) * 43758.5453f, 1f) - 0.5f;
        }

        void Triangulo(Vector2[] p, Vector3 centro, List<Vector3> vert, List<Vector2> uv, List<int> tri)
        {
            float hs = 0f;
            foreach (var q in p) { float h = Altura(q.x, q.y); hs += h; vert.Add(Mundo(centro, q.x, q.y, h)); tri.Add(vert.Count - 1); }
            var m = (p[0] + p[1] + p[2]) / 3f;
            float soma = hs / 3f, flo = Floresta(m.x, m.y) * Densidade(m.x, m.y);
            float t = NaAgua(m.x, m.y, 0.7f) ? 0.05f : Mathf.Clamp(0.42f + soma * 0.09f - flo * 0.22f
                      + (Mathf.PerlinNoise(m.x * 0.31f + _ox, m.y * 0.31f + _oz) - 0.5f) * 0.08f
                      + Retalho(m.x, m.y) * 0.2f * (1f - flo), 0.14f, 0.98f);
            for (int i = 0; i < 3; i++) uv.Add(new Vector2(0.5f, t));
        }

        // ---------- malhas e renderers ----------
        class Modelo { public List<(Mesh, Matrix4x4)> partes = new List<(Mesh, Matrix4x4)>(); public Bounds limites; }

        static List<Modelo> Modelos(GameObject[] prefabs)
        {
            var lista = new List<Modelo>();
            foreach (var go in prefabs)
            {
                var m = new Modelo(); bool tem = false;
                var w2l = go.transform.worldToLocalMatrix;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var local = w2l * mf.transform.localToWorldMatrix;
                    m.partes.Add((mf.sharedMesh, local));
                    var b = mf.sharedMesh.bounds;
                    for (int i = 0; i < 8; i++)
                    {
                        var c = local.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                        if (!tem) { m.limites = new Bounds(c, Vector3.zero); tem = true; } else m.limites.Encapsulate(c);
                    }
                }
                lista.Add(m);
            }
            return lista;
        }

        Mesh NovaMalha(string nome, List<Vector3> v, List<Vector2> uv, List<int> t)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32, name = "paisagem_" + nome, hideFlags = HideFlags.DontSave };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            m.UploadMeshData(true);
            _malhas.Add(m);
            return m;
        }

        void NovoRenderer(string nome, Mesh malha, Material mat)
        {
            var go = new GameObject(nome) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_gerado.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = malha;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;   // fora da distancia de sombra (50) de qualquer forma
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            for (int i = 0; i < malha.subMeshCount; i++) triangulosGerados += malha.GetIndexCount(i) / 3;
        }
    }
}
