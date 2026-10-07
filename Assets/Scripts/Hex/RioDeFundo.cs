using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Mantém o rio no espaço do cenário e as ondas em unidades de mundo.</summary>
    [ExecuteAlways, DefaultExecutionOrder(220)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class RioDeFundo : MonoBehaviour
    {
        public Transform cenario;
        [Min(0f)] public float alturaDasOndas = 0.07f;
        [Min(0.01f)] public float profundidadeDaCor = 0.6f;

        static readonly int Altura = Shader.PropertyToID("_Height");
        static readonly int Profundidade = Shader.PropertyToID("_Depth_fade");
        MeshFilter _filtro;
        MeshRenderer _renderizador;
        MaterialPropertyBlock _propriedades;

        void OnEnable() => Ajustar();
        void LateUpdate() => Ajustar();

        public void Ajustar()
        {
            if (cenario == null) return;
            // A malha foi criada nas coordenadas locais do cenário. O rio fica na raiz
            // para não participar do cálculo de escala feito por CenarioContinuo.
            transform.SetPositionAndRotation(cenario.position, cenario.rotation);
            transform.localScale = cenario.lossyScale;
            if (_filtro == null) _filtro = GetComponent<MeshFilter>();
            if (_renderizador == null) _renderizador = GetComponent<MeshRenderer>();
            if (_filtro.sharedMesh == null) return;
            float escalaY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
            float alturaLocal = alturaDasOndas / escalaY;
            if (_propriedades == null) _propriedades = new MaterialPropertyBlock();
            _renderizador.GetPropertyBlock(_propriedades);
            _propriedades.SetFloat(Altura, alturaLocal);
            _propriedades.SetFloat(Profundidade, profundidadeDaCor * escalaY);
            _renderizador.SetPropertyBlock(_propriedades);
            // O culling precisa considerar os vértices deslocados pelo shader.
            Bounds limites = _filtro.sharedMesh.bounds;
            limites.Expand(new Vector3(0f, 2f * alturaLocal, 0f));
            _renderizador.localBounds = limites;
        }

        void OnDisable()
        {
            if (_renderizador == null) return;
            _renderizador.SetPropertyBlock(null);
            _renderizador.ResetLocalBounds();
        }
    }
}
