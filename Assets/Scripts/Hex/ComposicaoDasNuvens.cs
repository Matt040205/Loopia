using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Geometria de tela das nuvens (Sprint 6): a "zona protegida" e a envoltoria convexa do tabuleiro (ilhas,
    /// miolo e vaos) vista pela camera; as nuvens contornam essa zona e nunca passam por cima do jogo.
    /// Coordenadas "de tela" = viewport com x multiplicado pelo aspecto, ou seja, a unidade e a altura da tela.
    /// </summary>
    public static class ComposicaoDasNuvens
    {
        /// <summary>Envoltoria convexa (cadeia monotona de Andrew), em sentido anti-horario e sem pontos colineares.</summary>
        public static List<Vector2> Envoltoria(List<Vector2> pontos)
        {
            var ordenados = new List<Vector2>(pontos);
            ordenados.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var casco = new List<Vector2>();
            if (ordenados.Count < 3) { casco.AddRange(ordenados); return casco; }
            for (int i = 0; i < ordenados.Count; i++)
            {
                while (casco.Count >= 2 && Cruz(casco[casco.Count - 2], casco[casco.Count - 1], ordenados[i]) <= 0f)
                    casco.RemoveAt(casco.Count - 1);
                casco.Add(ordenados[i]);
            }
            int inferior = casco.Count + 1;
            for (int i = ordenados.Count - 2; i >= 0; i--)
            {
                while (casco.Count >= inferior && Cruz(casco[casco.Count - 2], casco[casco.Count - 1], ordenados[i]) <= 0f)
                    casco.RemoveAt(casco.Count - 1);
                casco.Add(ordenados[i]);
            }
            casco.RemoveAt(casco.Count - 1);
            return casco;
        }

        static float Cruz(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        /// <summary>
        /// Distancia com sinal de <paramref name="p"/> ate o poligono convexo anti-horario: negativa dentro.
        /// <paramref name="normal"/> aponta para fora da zona, a partir do ponto mais proximo da borda.
        /// Sem poligono, devolve +infinito e normal zero.
        /// </summary>
        public static float Distancia(Vector2 p, List<Vector2> poligono, out Vector2 normal)
        {
            normal = Vector2.zero;
            if (poligono == null || poligono.Count < 3) return float.PositiveInfinity;
            bool dentro = true;
            float menor = float.PositiveInfinity;
            Vector2 pontoDaBorda = p, normalDaAresta = Vector2.up;
            for (int i = 0; i < poligono.Count; i++)
            {
                Vector2 a = poligono[i], b = poligono[(i + 1) % poligono.Count];
                Vector2 ab = b - a;
                if (Cruz(a, b, p) < 0f) dentro = false;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-8f, ab.sqrMagnitude));
                Vector2 maisPerto = a + ab * t;
                float d = Vector2.Distance(p, maisPerto);
                if (d < menor)
                {
                    menor = d;
                    pontoDaBorda = maisPerto;
                    // Poligono anti-horario: o lado de fora fica a direita de cada aresta.
                    normalDaAresta = new Vector2(ab.y, -ab.x).normalized;
                }
            }
            if (dentro)
            {
                normal = normalDaAresta;
                return -menor;
            }
            Vector2 paraFora = p - pontoDaBorda;
            normal = paraFora.sqrMagnitude > 1e-10f ? paraFora.normalized : normalDaAresta;
            return menor;
        }

        /// <summary>Area do poligono (formula do cadarco), em unidades de tela ao quadrado.</summary>
        public static float Area(List<Vector2> poligono)
        {
            if (poligono == null || poligono.Count < 3) return 0f;
            float soma = 0f;
            for (int i = 0; i < poligono.Count; i++)
            {
                Vector2 a = poligono[i], b = poligono[(i + 1) % poligono.Count];
                soma += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(soma) * 0.5f;
        }

        /// <summary>
        /// Velocidade de uma nuvem perto da zona: tira a parte que entra (toda na borda, nada no fim da faixa), empurra
        /// pela borda quando ela chega de frente (senao estacionaria) e, se ja estiver dentro, empurra para fora.
        /// <paramref name="folga"/> = distancia entre a borda da nuvem e a zona (negativa = sobrepondo).
        /// </summary>
        public static Vector2 Desviar(Vector2 velocidade, float folga, Vector2 normal, float faixa, float ladoPreferido)
        {
            if (folga >= faixa || normal.sqrMagnitude < 1e-8f) return velocidade;
            float k = 1f - Mathf.Clamp01(folga / Mathf.Max(1e-4f, faixa));
            float rapidez = velocidade.magnitude;
            float entrando = Vector2.Dot(velocidade, normal);
            Vector2 v = velocidade;
            if (entrando < 0f) v -= normal * entrando * k;
            Vector2 tangente = new Vector2(-normal.y, normal.x);
            float lado = Vector2.Dot(velocidade, tangente);
            float sinal = Mathf.Abs(lado) > 0.05f * rapidez ? Mathf.Sign(lado) : (ladoPreferido >= 0f ? 1f : -1f);
            float deFrente = rapidez > 1e-6f ? Mathf.Clamp01(-entrando / rapidez) : 0f;
            v += tangente * (sinal * rapidez * deFrente * k * 1.3f);
            if (folga < 0f) v += normal * (-folga * 0.6f + rapidez * 0.5f);
            return v;
        }

        /// <summary>Curva suave de 0 a 1 (nascer e dissipar sem trancos).</summary>
        public static float Suave(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
