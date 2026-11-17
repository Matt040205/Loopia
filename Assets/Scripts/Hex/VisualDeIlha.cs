using Unity.AI.Navigation;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Qual dimensao do modelo e igualada ao tamanho pedido em <see cref="VisualDeIlha.Criar"/>.</summary>
    public enum EixoDeMedida
    {
        /// <summary>Maior dos tres eixos (com topo=true, so x e z). Comportamento original.</summary>
        MaiorEixo,

        /// <summary>Altura (y): para objetos verticais como arvores e cogumelos.</summary>
        Altura,

        /// <summary>Maior entre x e z: para objetos deitados, como troncos.</summary>
        Comprimento,
    }

    /// <summary>Centraliza modelos com pivôs diferentes e mantém a arte fora do bake.</summary>
    public static class VisualDeIlha
    {
        public static GameObject Criar(GameObject prefab, Transform pai, float tamanho, bool topo = false)
            => Criar(prefab, pai, tamanho, topo, EixoDeMedida.MaiorEixo);

        public static GameObject Criar(GameObject prefab, Transform pai, float tamanho, bool topo, EixoDeMedida eixo)
        {
            if (prefab == null) return null;
            var raiz = new GameObject(prefab.name).transform;
            raiz.SetParent(pai, false);
            var modelo = Object.Instantiate(prefab, raiz);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;
            Bounds limites = LimitesLocais(raiz);
            float medida = Medida(limites.size, topo, eixo);
            float escala = tamanho / Mathf.Max(0.001f, medida);
            modelo.transform.localScale *= escala;
            modelo.transform.localPosition = new Vector3(-limites.center.x,
                -(topo ? limites.max.y : limites.min.y), -limites.center.z) * escala;
            PrepararArte(raiz.gameObject);
            return raiz.gameObject;
        }

        /// <summary>
        /// Criatura animada (Sprint 5B): instancia o prefab sem recentrar pelos limites, porque o pivo do modelo ja e a
        /// ancora certa (centro do voo no morcego, chao entre as patas no lobo) e os limites mudam a cada pose. So
        /// iguala o maior eixo da malha em repouso a <paramref name="tamanho"/>, numa escala uniforme aplicada uma vez
        /// na raiz do prefab. Plataformas e decoracao continuam usando <see cref="Criar(GameObject, Transform, float, bool)"/>.
        /// </summary>
        public static GameObject CriarCriatura(GameObject prefab, Transform pai, float tamanho, out float escala)
        {
            escala = 1f;
            if (prefab == null) return null;
            var raiz = Object.Instantiate(prefab, pai);
            raiz.name = prefab.name;
            raiz.transform.localPosition = Vector3.zero;
            raiz.transform.localRotation = Quaternion.identity;
            raiz.transform.localScale = Vector3.one;
            float medida = Medida(LimitesLocais(raiz.transform).size, false, EixoDeMedida.MaiorEixo);
            escala = tamanho / Mathf.Max(0.001f, medida);
            raiz.transform.localScale = Vector3.one * escala;
            PrepararArte(raiz);
            return raiz;
        }

        /// <summary>Arte pura: fora do bake do NavMesh (vale para os filhos) e sem collider ligado.</summary>
        public static void PrepararArte(GameObject arte)
        {
            var modificador = arte.GetComponent<NavMeshModifier>();
            if (modificador == null) modificador = arte.AddComponent<NavMeshModifier>();
            modificador.ignoreFromBuild = true;
            foreach (var colisor in arte.GetComponentsInChildren<Collider>(true)) colisor.enabled = false;
        }

        /// <summary>Dimensao do modelo que vale como "tamanho" para o eixo escolhido.</summary>
        public static float Medida(Vector3 tamanho, bool topo, EixoDeMedida eixo)
        {
            switch (eixo)
            {
                case EixoDeMedida.Altura: return tamanho.y;
                case EixoDeMedida.Comprimento: return Mathf.Max(tamanho.x, tamanho.z);
                default:
                    return topo ? Mathf.Max(tamanho.x, tamanho.z) : Mathf.Max(tamanho.x, tamanho.y, tamanho.z);
            }
        }

        /// <summary>
        /// Limites da malha no espaco da raiz. Considera MeshFilter e SkinnedMeshRenderer (sharedMesh.bounds,
        /// ou seja, a pose original da malha, sem animacao).
        /// </summary>
        public static Bounds LimitesLocais(Transform raiz)
        {
            var limites = new Bounds();
            bool primeiro = true;
            foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
            {
                if (filtro.sharedMesh == null) continue;
                Acumular(raiz, filtro.transform, filtro.sharedMesh.bounds, ref limites, ref primeiro);
            }
            foreach (var skinned in raiz.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinned.sharedMesh == null) continue;
                Acumular(raiz, skinned.transform, skinned.sharedMesh.bounds, ref limites, ref primeiro);
            }
            return limites;
        }

        static void Acumular(Transform raiz, Transform origem, Bounds b, ref Bounds limites, ref bool primeiro)
        {
            for (int i = 0; i < 8; i++)
            {
                var canto = b.center + Vector3.Scale(b.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 ponto = raiz.InverseTransformPoint(origem.TransformPoint(canto));
                if (primeiro) { limites = new Bounds(ponto, Vector3.zero); primeiro = false; }
                else limites.Encapsulate(ponto);
            }
        }
    }
}
