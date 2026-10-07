using Unity.AI.Navigation;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Centraliza modelos com pivôs diferentes e mantém a arte fora do bake.</summary>
    public static class VisualDeIlha
    {
        public static GameObject Criar(GameObject prefab, Transform pai, float tamanho, bool topo = false)
        {
            if (prefab == null) return null;
            var raiz = new GameObject(prefab.name).transform;
            raiz.SetParent(pai, false);
            var modelo = Object.Instantiate(prefab, raiz);
            modelo.transform.localPosition = Vector3.zero;
            modelo.transform.localRotation = Quaternion.identity;
            modelo.transform.localScale = Vector3.one;
            Bounds limites = LimitesLocais(raiz);
            float medida = topo ? Mathf.Max(limites.size.x, limites.size.z) :
                Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
            float escala = tamanho / Mathf.Max(0.001f, medida);
            modelo.transform.localScale *= escala;
            modelo.transform.localPosition = new Vector3(-limites.center.x,
                -(topo ? limites.max.y : limites.min.y), -limites.center.z) * escala;
            raiz.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            foreach (var colisor in raiz.GetComponentsInChildren<Collider>()) colisor.enabled = false;
            return raiz.gameObject;
        }

        public static Bounds LimitesLocais(Transform raiz)
        {
            var limites = new Bounds();
            bool primeiro = true;
            foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>())
            {
                if (filtro.sharedMesh == null) continue;
                Bounds b = filtro.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var canto = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 ponto = raiz.InverseTransformPoint(filtro.transform.TransformPoint(canto));
                    if (primeiro) { limites = new Bounds(ponto, Vector3.zero); primeiro = false; }
                    else limites.Encapsulate(ponto);
                }
            }
            return limites;
        }
    }
}
