using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Materiais URP/Lit criados em runtime e reaproveitados por cor (Pass D2). Antes, cada inimigo sem prefab e cada flecha
    /// criavam um `new Material` que nunca era destruido (vazamento a cada spawn/tiro). Aqui ha no maximo um material por
    /// combinacao de cor/suavidade/metal, vivo enquanto o jogo roda.
    /// </summary>
    public static class MateriaisCompartilhados
    {
        static readonly Dictionary<(Color32, int, int, string), Material> _cache = new Dictionary<(Color32, int, int, string), Material>();

        /// <summary>Quantos materiais distintos ja foram criados (para testes/diagnostico).</summary>
        public static int Quantidade => _cache.Count;

        public static Material Lit(string nome, Color cor, float suavidade = 0.5f, float metal = 0f)
        {
            var chave = ((Color32)cor, Mathf.RoundToInt(suavidade * 100f), Mathf.RoundToInt(metal * 100f), nome);
            if (_cache.TryGetValue(chave, out Material existente) && existente != null) return existente;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = nome + " (compartilhado)" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", suavidade);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metal);
            _cache[chave] = material;
            return material;
        }
    }
}
