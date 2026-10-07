using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Cobre a projeção inteira da câmera no plano do fundo, em qualquer zoom.</summary>
    [ExecuteAlways, DefaultExecutionOrder(200)]
    [RequireComponent(typeof(MeshFilter))]
    public class FundoContinuo : MonoBehaviour
    {
        public Camera camera3d;
        [Min(1f)] public float margem = 1.2f;

        void LateUpdate() => Ajustar();

        public void Ajustar()
        {
            if (camera3d == null) camera3d = Camera.main;
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            if (camera3d == null || mesh == null) return;
            var plano = new Plane(transform.up, transform.position);
            Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = camera3d.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
                if (!plano.Raycast(ray, out float distancia)) return;
                Vector3 local = Quaternion.Inverse(transform.rotation) * (ray.GetPoint(distancia) - transform.position);
                min = Vector3.Min(min, local); max = Vector3.Max(max, local);
            }
            Bounds bounds = mesh.bounds;
            Vector3 escala = transform.localScale;
            escala.x = (max.x - min.x) * margem / Mathf.Max(0.001f, bounds.size.x);
            escala.z = (max.z - min.z) * margem / Mathf.Max(0.001f, bounds.size.z);
            transform.localScale = escala;
            Vector3 centro = (min + max) * 0.5f;
            centro.y = 0f;
            Vector3 offset = centro - new Vector3(bounds.center.x * escala.x, 0f, bounds.center.z * escala.z);
            transform.position += transform.rotation * offset;
        }
    }
}
