using UnityEngine;

namespace Loopia.Hex
{
    [ExecuteAlways, DefaultExecutionOrder(210)]
    public class CenarioContinuo : MonoBehaviour
    {
        public Camera camera3d;
        public FundoContinuo agua;
        [Min(1f)] public float margem = 2f;
        Bounds _limites;
        [SerializeField, HideInInspector] Vector3 _proporcao;
        [SerializeField, HideInInspector] float _teto;
        [SerializeField, HideInInspector] bool _referenciaSalva;
        [System.NonSerialized] bool _pronto;
        void OnEnable() => _pronto = false;
        void LateUpdate() => Ajustar();
        public void Ajustar()
        {
            if (camera3d == null) camera3d = Camera.main;
            if (camera3d == null) return;
            if (!_pronto)
            {
                _limites = VisualDeIlha.LimitesLocais(transform);
                if (!_referenciaSalva && transform.localScale.y > 0.001f)
                {
                    _proporcao = transform.localScale / transform.localScale.y;
                    _teto = transform.position.y + _limites.max.y * transform.localScale.y;
                    _referenciaSalva = true;
                }
                _pronto = true;
            }
            if (_limites.size.x < 0.001f || _limites.size.z < 0.001f ||
                _proporcao.x <= 0f || _proporcao.z <= 0f) return;
            if (!ProjetarTela(out Vector3 min, out Vector3 max)) return;
            float fator = Mathf.Max((max.x - min.x) / (_limites.size.x * _proporcao.x),
                (max.z - min.z) / (_limites.size.z * _proporcao.z)) * margem;
            if (float.IsNaN(fator) || float.IsInfinity(fator) || fator <= 0f) return;
            var escala = _proporcao * fator;
            transform.localScale = escala;
            Vector3 posicao = transform.position;
            posicao.y = _teto - _limites.max.y * escala.y;
            transform.position = posicao;
            // Mudar a altura muda o centro da projeção isométrica, mesmo sem mudar o zoom.
            if (!ProjetarTela(out min, out max)) return;
            var centro = (max + min) * 0.5f;
            centro.y = 0f;
            var offset = centro - new Vector3(_limites.center.x * escala.x, 0f, _limites.center.z * escala.z);
            transform.position += transform.rotation * offset;
            if (agua != null)
            {
                var posicaoAgua = agua.transform.position;
                posicaoAgua.y = transform.position.y + _limites.min.y * escala.y - 0.5f;
                agua.transform.position = posicaoAgua;
                agua.Ajustar();
            }
        }

        bool ProjetarTela(out Vector3 min, out Vector3 max)
        {
            var plano = new Plane(Vector3.up, transform.position);
            min = Vector3.one * float.MaxValue;
            max = Vector3.one * float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var ray = camera3d.ViewportPointToRay(new Vector3(i % 2, i / 2));
                if (!plano.Raycast(ray, out float distancia)) return false;
                Vector3 p = Quaternion.Inverse(transform.rotation) * (ray.GetPoint(distancia) - transform.position);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
            return true;
        }
    }
}
