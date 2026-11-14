using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Confere os perfis de escala de arte (ArtScaleProfile) e os InimigoDef do projeto.
    /// Menu: Loopia/Validar escala de arte. Tambem pode ser chamado por codigo (testes) com <see cref="Validar"/>.
    /// Erro = quebra o perfil; Aviso = vale revisar; Info = so para saber (VisualDeIlha ja compensa).
    /// </summary>
    public static class ValidadorDeEscala
    {
        public sealed class Resultado
        {
            public int perfis;
            public int erros;
            public int avisos;
            public int infos;
            public readonly List<string> linhas = new List<string>();

            public override string ToString() => string.Join("\n", linhas);
        }

        [MenuItem("Loopia/Validar escala de arte")]
        public static void ValidarPeloMenu()
        {
            Resultado r = Validar();
            Debug.Log("[ValidadorDeEscala] " + r.perfis + " perfil(is): " + r.erros + " erro(s), " + r.avisos + " aviso(s), " +
                      r.infos + " info(s)\n" + r);
        }

        public static Resultado Validar()
        {
            var r = new Resultado();
            string[] guids = AssetDatabase.FindAssets("t:ArtScaleProfile");
            foreach (string guid in guids)
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var perfil = AssetDatabase.LoadAssetAtPath<ArtScaleProfile>(caminho);
                if (perfil == null) continue;
                r.perfis++;
                ValidarPerfil(perfil, caminho, r);
            }
            ListarInimigos(r);
            return r;
        }

        static void Erro(Resultado r, string msg) { r.erros++; r.linhas.Add("ERRO   " + msg); }
        static void Aviso(Resultado r, string msg) { r.avisos++; r.linhas.Add("AVISO  " + msg); }
        static void Info(Resultado r, string msg) { r.infos++; r.linhas.Add("INFO   " + msg); }

        static void ValidarPerfil(ArtScaleProfile perfil, string caminho, Resultado r)
        {
            string p = caminho + ": ";
            if (perfil.alturaDeReferencia <= 0f) Erro(r, p + "alturaDeReferencia <= 0");

            if (perfil.regras == null) { Erro(r, p + "lista de regras nula"); return; }
            var categoriasComRegra = new HashSet<CategoriaDeArte>();
            foreach (RegraDeCategoria regra in perfil.regras)
            {
                if (regra == null) { Erro(r, p + "regra nula na lista"); continue; }
                if (!categoriasComRegra.Add(regra.categoria)) Aviso(r, p + "mais de uma regra para " + regra.categoria + " (vale a primeira)");
                if (regra.tamanhoMinimo <= 0f || regra.tamanhoMaximo < regra.tamanhoMinimo)
                    Erro(r, p + "faixa invalida em " + regra.categoria + ": " + regra.tamanhoMinimo + " a " + regra.tamanhoMaximo);
            }

            if (perfil.modelos == null || perfil.modelos.Count == 0) { Aviso(r, p + "nenhum modelo no perfil"); return; }

            for (int i = 0; i < perfil.modelos.Count; i++)
            {
                ModeloDeArte m = perfil.modelos[i];
                string quem = p + "modelos[" + i + "]";
                if (m == null) { Erro(r, quem + " nulo"); continue; }
                if (m.prefab == null) { Erro(r, quem + " (" + m.categoria + ") sem prefab"); continue; }
                quem += " " + m.prefab.name + " (" + m.categoria + ")";

                if (m.peso <= 0f) Aviso(r, quem + ": peso <= 0, nunca sera sorteado");
                if (!perfil.TryRegra(m.categoria, out RegraDeCategoria regra)) Erro(r, quem + ": categoria sem regra");

                var filtros = m.prefab.GetComponentsInChildren<MeshFilter>(true);
                var skinneds = m.prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                bool temFiltro = false, temSkinned = false;
                foreach (var f in filtros) if (f.sharedMesh != null) temFiltro = true;
                foreach (var s in skinneds) if (s.sharedMesh != null) temSkinned = true;
                if (!temFiltro && !temSkinned)
                {
                    Erro(r, quem + ": prefab sem MeshFilter nem SkinnedMeshRenderer com malha (sem bounds para normalizar)");
                    continue;
                }
                if (!temFiltro && temSkinned)
                    Info(r, quem + ": so tem SkinnedMeshRenderer (bounds de sharedMesh, pose original; sem animacao)");

                Bounds b = VisualDeIlha.LimitesLocais(m.prefab.transform);
                if (b.size.y > 0.0001f)
                {
                    float base_ = b.min.y;
                    if (Mathf.Abs(base_) > 0.1f * b.size.y)
                        Info(r, quem + ": pivo longe da base (min.y = " + base_.ToString("F3") + " para altura " + b.size.y.ToString("F3") +
                                  "); VisualDeIlha poe a base em y = 0");
                }

                foreach (var c in m.prefab.GetComponentsInChildren<Collider>(true))
                    if (c.enabled)
                    {
                        Aviso(r, quem + ": collider ativo no prefab (" + c.GetType().Name + " em '" + c.name + "'); VisualDeIlha desliga ao instanciar");
                        break;
                    }

                if (regra != null)
                {
                    float referencia = VisualDeIlha.Medida(b.size, false, regra.eixo);
                    if (referencia <= 0.0001f) Erro(r, quem + ": medida zero no eixo " + regra.eixo);
                    else Info(r, quem + ": bruto " + b.size.x.ToString("F3") + " x " + b.size.y.ToString("F3") + " x " + b.size.z.ToString("F3") +
                                  "; medido em " + regra.eixo + " de " + (regra.tamanhoMinimo * perfil.alturaDeReferencia).ToString("F2") + " a " +
                                  (regra.tamanhoMaximo * perfil.alturaDeReferencia).ToString("F2") + " u");
                }
            }
        }

        /// <summary>Tamanho final do corpo de cada InimigoDef (o que Inimigo.MontarVisual produz).</summary>
        static void ListarInimigos(Resultado r)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InimigoDef"))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<InimigoDef>(caminho);
                if (def == null) continue;
                if (def.prefab == null)
                {
                    Info(r, caminho + ": InimigoDef '" + def.nome + "' sem prefab; forma simples de lado " + def.escala.ToString("F2") + " u");
                    continue;
                }
                Bounds b = VisualDeIlha.LimitesLocais(def.prefab.transform);
                float maior = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (maior <= 0f) { Erro(r, caminho + ": InimigoDef '" + def.nome + "' com prefab sem malha"); continue; }
                float f = def.escala / maior;
                Info(r, caminho + ": InimigoDef '" + def.nome + "' escala " + def.escala.ToString("F2") + " => " +
                          (b.size.x * f).ToString("F2") + " x " + (b.size.y * f).ToString("F2") + " x " + (b.size.z * f).ToString("F2") +
                          " u (x, y = altura, z = comprimento; pela malha)");
            }
        }
    }
}
