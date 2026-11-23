using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loopia.Hex
{
    /// <summary>
    /// Malha de uma nuvem toon (Sprint 6): gomos (esferas) de base em fila irregular, gomos por cima e, nas grandes,
    /// gomos atras. Fundo achatado; largura normalizada em ~1 unidade.
    /// <para>Canais lidos pelo shader <c>Loopia/Nuvem Toon</c> para animar cada gomo sem refazer a malha:
    /// cor R = abertura (0 nas dobras entre gomos); UV2 = centro do gomo; UV3 = (fase, tipo, amplitude, frequencia).
    /// Gomo <see cref="Estavel"/> so incha (escala 1 a 1+amplitude); gomo <see cref="Brotando"/> nasce e recolhe
    /// (escala de "amplitude" ate 1,08).</para>
    /// <para>Faces escondidas saem, mas so as que ficam dentro de um gomo estavel com folga maior que o quanto o dono
    /// da face pode crescer: assim nenhuma animacao abre buraco.</para>
    /// </summary>
    public static class MalhaDeNuvem
    {
        public const float Estavel = 0f, Brotando = 1f;
        const float MaiorEscalaDoBroto = 1.08f;

        struct Gomo
        {
            public Vector3 centro;
            public float raio, tipo, fase, amplitude, frequencia;
        }

        static float Aleatorio(System.Random random, float a, float b) => a + (float)random.NextDouble() * (b - a);

        /// <summary>Porte 0 = pequena, 1 = media, 2 = grande.</summary>
        public static Mesh Criar(System.Random random, int subdivisoes, int porte)
        {
            var gomos = new List<Gomo>();
            int naBase = porte == 0 ? 3 : porte == 1 ? 4 + random.Next(2) : 6 + random.Next(3);
            float pico = Aleatorio(random, 0.25f, 0.75f);
            float rMin = porte == 0 ? 0.17f : porte == 1 ? 0.12f : 0.1f;
            float rMax = porte == 0 ? 0.3f : porte == 1 ? 0.32f : 0.3f;
            float espalhaZ = porte == 2 ? 0.14f : 0.08f;
            // Gomos vizinhos se sobrepoem ~40% do raio: a nuvem le como um volume so, nao como bolhas soltas.
            float x = 0f, raioAnterior = 0f;
            for (int k = 0; k < naBase; k++)
            {
                float t = naBase == 1 ? 0.5f : (float)k / (naBase - 1);
                float sino = Mathf.Exp(-Mathf.Pow((t - pico) / 0.3f, 2f));
                float raio = Mathf.Lerp(rMin, rMax, sino) * Aleatorio(random, 0.85f, 1.15f);
                if (k > 0) x += (raioAnterior + raio) * Aleatorio(random, 0.55f, 0.68f);
                raioAnterior = raio;
                gomos.Add(new Gomo
                {
                    centro = new Vector3(x, raio * 0.25f + sino * 0.05f, Aleatorio(random, -espalhaZ, espalhaZ)),
                    raio = raio, tipo = Estavel, fase = Aleatorio(random, 0f, Mathf.PI * 2f),
                    amplitude = Aleatorio(random, 0.06f, 0.14f), frequencia = Aleatorio(random, 0.25f, 0.55f)
                });
            }
            for (int k = 0; k < naBase; k++)
            {
                Gomo g = gomos[k];
                g.centro.x -= x * 0.5f;
                gomos[k] = g;
            }
            int emCima = porte == 0 ? 1 : porte == 1 ? 2 + random.Next(2) : 3 + random.Next(3);
            for (int j = 0; j < emCima; j++)
            {
                int k = Mathf.Clamp(Mathf.RoundToInt(pico * (naBase - 1) + Aleatorio(random, -1.2f, 1.2f)), 0, naBase - 1);
                Gomo b = gomos[k];
                // O centro do broto fica dentro do gomo de base: ao recolher, ele afunda no volume sem abrir buraco.
                gomos.Add(new Gomo
                {
                    centro = b.centro + new Vector3(Aleatorio(random, -0.35f, 0.35f) * b.raio, b.raio * Aleatorio(random, 0.4f, 0.62f),
                        Aleatorio(random, -0.3f, 0.3f) * b.raio),
                    raio = b.raio * Aleatorio(random, 0.55f, 0.78f), tipo = Brotando, fase = Aleatorio(random, 0f, Mathf.PI * 2f),
                    amplitude = Aleatorio(random, 0.45f, 0.7f), frequencia = Aleatorio(random, 0.15f, 0.35f)
                });
            }
            if (porte == 2)
            {
                int atras = 1 + random.Next(2);
                for (int j = 0; j < atras; j++)
                    gomos.Add(new Gomo
                    {
                        centro = new Vector3(Aleatorio(random, -0.3f, 0.3f) * x, Aleatorio(random, 0.04f, 0.1f),
                            Aleatorio(random, 0.12f, 0.2f) * (random.NextDouble() < 0.5 ? -1f : 1f)),
                        raio = Aleatorio(random, 0.18f, 0.24f), tipo = Estavel, fase = Aleatorio(random, 0f, Mathf.PI * 2f),
                        amplitude = Aleatorio(random, 0.05f, 0.1f), frequencia = Aleatorio(random, 0.2f, 0.45f)
                    });
            }

            var unidade = Icosfera(subdivisoes, out List<int> facesDaUnidade);
            var vertices = new List<Vector3>();
            var aberturas = new List<float>();
            var donos = new List<int>();
            var escondido = new List<bool>();
            var triangulos = new List<int>();
            for (int e = 0; e < gomos.Count; e++)
            {
                Gomo g = gomos[e];
                // Quanto a face deste gomo pode avancar ao animar.
                float avanco = g.raio * (g.tipo == Estavel ? g.amplitude : MaiorEscalaDoBroto - 1f) + 0.01f;
                int inicio = vertices.Count;
                foreach (Vector3 u in unidade)
                {
                    Vector3 p = g.centro + u * g.raio * (1f + Aleatorio(random, -0.015f, 0.015f));
                    float folga = float.MaxValue;
                    bool dentro = false;
                    for (int o = 0; o < gomos.Count; o++)
                    {
                        if (o == e) continue;
                        Gomo q = gomos[o];
                        float distancia = Vector3.Distance(p, q.centro) - q.raio;
                        // Dobras do gomo estavel so contra gomos estaveis: um broto recolhido nao deixa mancha escura.
                        if (g.tipo == Brotando || q.tipo == Estavel) folga = Mathf.Min(folga, distancia);
                        if (q.tipo == Estavel && distancia < -avanco) dentro = true;
                    }
                    vertices.Add(p);
                    donos.Add(e);
                    escondido.Add(dentro);
                    aberturas.Add(folga == float.MaxValue ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(folga / 0.09f)));
                }
                for (int f = 0; f < facesDaUnidade.Count; f += 3)
                {
                    int a = inicio + facesDaUnidade[f], b = inicio + facesDaUnidade[f + 1], c = inicio + facesDaUnidade[f + 2];
                    if (escondido[a] && escondido[b] && escondido[c]) continue;
                    triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
                }
            }

            // Largura normalizada em ~1 unidade (a camada escala pela vista), qualquer que seja o numero de gomos.
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (Gomo g in gomos) { minX = Mathf.Min(minX, g.centro.x - g.raio); maxX = Mathf.Max(maxX, g.centro.x + g.raio); }
            float normaliza = 1f / Mathf.Max(0.1f, maxX - minX);
            float meioX = (minX + maxX) * 0.5f;

            var novoIndice = new int[vertices.Count];
            for (int i = 0; i < novoIndice.Length; i++) novoIndice[i] = -1;
            var finais = new List<Vector3>();
            var cores = new List<Color32>();
            var centros = new List<Vector4>();
            var dados = new List<Vector4>();
            for (int i = 0; i < triangulos.Count; i++)
            {
                int v = triangulos[i];
                if (novoIndice[v] < 0)
                {
                    novoIndice[v] = finais.Count;
                    finais.Add(Achatar(vertices[v], meioX) * normaliza);
                    cores.Add(new Color32((byte)Mathf.RoundToInt(aberturas[v] * 255f), 255, 255, 255));
                    Gomo g = gomos[donos[v]];
                    Vector3 c = Achatar(g.centro, meioX) * normaliza;
                    centros.Add(new Vector4(c.x, c.y, c.z, g.raio * normaliza));
                    dados.Add(new Vector4(g.fase, g.tipo, g.amplitude, g.frequencia));
                }
                triangulos[i] = novoIndice[v];
            }
            var malha = new Mesh { indexFormat = IndexFormat.UInt32 };
            malha.SetVertices(finais);
            malha.SetColors(cores);
            malha.SetUVs(2, centros);
            malha.SetUVs(3, dados);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }

        static Vector3 Achatar(Vector3 p, float meioX)
        {
            if (p.y < 0f) p.y *= 0.3f;
            p.x -= meioX;
            return p;
        }

        static List<Vector3> Icosfera(int subdivisoes, out List<int> faces)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            faces = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            for (int s = 0; s < subdivisoes; s++)
            {
                var meios = new Dictionary<long, int>();
                var novas = new List<int>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i += 3)
                {
                    int a = Meio(v, meios, faces[i], faces[i + 1]);
                    int b = Meio(v, meios, faces[i + 1], faces[i + 2]);
                    int c = Meio(v, meios, faces[i + 2], faces[i]);
                    novas.AddRange(new[] { faces[i], a, c, faces[i + 1], b, a, faces[i + 2], c, b, a, b, c });
                }
                faces = novas;
            }
            return v;
        }

        static int Meio(List<Vector3> v, Dictionary<long, int> meios, int a, int b)
        {
            long chave = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (meios.TryGetValue(chave, out int indice)) return indice;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            meios[chave] = v.Count - 1;
            return v.Count - 1;
        }
    }
}
