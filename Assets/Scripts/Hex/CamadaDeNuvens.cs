using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Loopia.Hex
{
    /// <summary>Distribui volumes de nuvem abaixo das ilhas; recicla somente fora da tela.</summary>
    [ExecuteAlways, DefaultExecutionOrder(230)]
    [DisallowMultipleComponent]
    public class CamadaDeNuvens : MonoBehaviour
    {
        public Camera camera3d;
        public HexWorld mundo;
        public Material material;
        public Mesh malhaQuad;
        [Range(4, 32)] public int quantidade = 18;
        public int semente = 751;
        [Tooltip("Tamanho como fracao da altura visivel da camera.")]
        public Vector2 tamanho = new Vector2(0.13f, 0.21f);
        [Min(0.1f)] public float distanciaDasIlhas = 0.85f;
        [Tooltip("Direcao do vento no enquadramento.")]
        public Vector2 vento = new Vector2(1f, 0.18f);
        [Min(0f)] public float velocidade = 0.018f;
        [Range(0f, 1f)] public float opacidade = 0.8f;

        class Nuvem
        {
            public CloudController controle;
            public Vector2 uv;
            public float tamanho, proporcao, profundidade, velocidade, fase, camada;
        }
        readonly List<Nuvem> _nuvens = new List<Nuvem>();
        readonly Vector3[] _cantos = new Vector3[4];
        int _sementeAtual;
        Material _materialAtual;
        Mesh _malhaAtual;
        float _baseDasIlhas = -3.5f;
        float _proximaMedicao;
        const float Margem = 0.28f;

        void OnEnable() => _proximaMedicao = 0f;
        void LateUpdate() => Atualizar(Application.isPlaying ? Time.deltaTime : 0f);

        public void Atualizar(float deltaTime)
        {
            if (camera3d == null) camera3d = Camera.main;
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();
            if (camera3d == null || material == null || malhaQuad == null) return;
            if (_nuvens.Count != quantidade || _sementeAtual != semente ||
                _materialAtual != material || _malhaAtual != malhaQuad) CriarNuvens();
            if (!Application.isPlaying || Time.unscaledTime >= _proximaMedicao)
            {
                _proximaMedicao = Time.unscaledTime + 0.5f;
                _baseDasIlhas = mundo != null ? mundo.transform.position.y - 3.5f : -3.5f;
                if (mundo != null && mundo.TotalDeIlhas > 0)
                {
                    float min = float.MaxValue;
                    foreach (var ilha in mundo.Ilhas)
                        foreach (var renderer in ilha.GetComponentsInChildren<MeshRenderer>())
                            if (renderer.enabled) min = Mathf.Min(min, renderer.bounds.min.y);
                    if (min < float.MaxValue) _baseDasIlhas = min;
                }
            }
            float viewHeight = camera3d.orthographic ? camera3d.orthographicSize * 2f :
                2f * Vector3.Distance(camera3d.transform.position, transform.position) *
                Mathf.Tan(camera3d.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector2 direction = vento.sqrMagnitude > 0.001f ? vento.normalized : Vector2.right;
            foreach (Nuvem nuvem in _nuvens)
            {
                nuvem.uv.x = ReciclarCoordenada(nuvem.uv.x + direction.x * velocidade * nuvem.velocidade * deltaTime);
                nuvem.uv.y = ReciclarCoordenada(nuvem.uv.y + direction.y * velocidade * nuvem.velocidade * deltaTime);
                float width = viewHeight * Mathf.Lerp(tamanho.x, tamanho.y, nuvem.tamanho);
                var scale = new Vector3(width, width * nuvem.proporcao, width * nuvem.profundidade);
                Quaternion rotation = camera3d.transform.rotation * Quaternion.Euler(0f, 0f, nuvem.controle.billboardTilt);
                float halfHeight = Mathf.Abs((rotation * Vector3.right).y) * scale.x * 0.5f +
                    Mathf.Abs((rotation * Vector3.up).y) * scale.y * 0.5f +
                    Mathf.Abs((rotation * Vector3.forward).y) * scale.z * 0.5f;
                float height = _baseDasIlhas - distanciaDasIlhas - halfHeight - nuvem.camada;
                var plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
                bool valid = true;
                for (int i = 0; i < 4; i++)
                {
                    Ray ray = camera3d.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
                    if (!plane.Raycast(ray, out float distance)) { valid = false; break; }
                    _cantos[i] = ray.GetPoint(distance);
                }
                if (!valid) continue;
                Vector3 position = _cantos[0] + (_cantos[1] - _cantos[0]) * nuvem.uv.x +
                    (_cantos[2] - _cantos[0]) * nuvem.uv.y;
                position.y += Mathf.Sin(Time.time * 0.35f + nuvem.fase) * 0.1f;
                Transform cloud = nuvem.controle.transform;
                cloud.SetPositionAndRotation(position, rotation);
                cloud.localScale = scale;
                nuvem.controle.opacityMultiplier = opacidade * Mathf.Lerp(0.72f, 1f, nuvem.tamanho);
                nuvem.controle.AtualizarAparencia();
            }
        }

        public static float ReciclarCoordenada(float coordinate)
        {
            return Mathf.Repeat(coordinate + Margem, 1f + Margem * 2f) - Margem;
        }

        void CriarNuvens()
        {
            Limpar();
            _sementeAtual = semente; _materialAtual = material; _malhaAtual = malhaQuad;
            var random = new System.Random(semente);
            for (int i = 0; i < quantidade; i++)
            {
                var go = new GameObject("Nuvem volumetrica " + (i + 1));
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = malhaQuad;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var controle = go.AddComponent<CloudController>();
                controle.controlledByLayer = true;
                controle.cloudSeed = 7f + i * 3.17f + (float)random.NextDouble() * 10f;
                controle.billboardTilt = Mathf.Lerp(-12f, 12f, (float)random.NextDouble());
                _nuvens.Add(new Nuvem
                {
                    controle = controle,
                    // Grade com deslocamento para cobrir o mapa sem um agrupamento no centro.
                    uv = new Vector2((i % 6 + 0.15f + (float)random.NextDouble() * .7f) / 6f,
                        (i / 6 + 0.15f + (float)random.NextDouble() * .7f) / Mathf.Ceil(quantidade / 6f)),
                    tamanho = (float)random.NextDouble(),
                    proporcao = Mathf.Lerp(.36f, .5f, (float)random.NextDouble()),
                    profundidade = Mathf.Lerp(.38f, .55f, (float)random.NextDouble()),
                    velocidade = Mathf.Lerp(.6f, 1.3f, (float)random.NextDouble()),
                    fase = (float)random.NextDouble() * Mathf.PI * 2f,
                    camada = (i % 3) * .55f
                });
            }
        }

        void Limpar()
        {
            foreach (Nuvem nuvem in _nuvens)
                if (nuvem.controle != null)
                {
                    // Destroy aguarda o fim do quadro; evite sobrepor a camada antiga à nova.
                    nuvem.controle.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(nuvem.controle.gameObject);
                    else DestroyImmediate(nuvem.controle.gameObject);
                }
            _nuvens.Clear();
        }
        void OnDisable() => Limpar();
        void OnDestroy() => Limpar();
    }
}
