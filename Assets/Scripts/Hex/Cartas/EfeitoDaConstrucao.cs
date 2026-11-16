using UnityEngine;
using UnityEngine.Rendering;

namespace Loopia.Hex
{
    internal sealed class EfeitoDaConstrucao
    {
        static readonly int IdCor = Shader.PropertyToID("_Cor");
        readonly Mesh _malha;
        readonly MeshRenderer _anel;
        readonly MaterialPropertyBlock _bloco = new MaterialPropertyBlock();
        float _tempo;
        Color _cor;

        public EfeitoDaConstrucao(Transform pai, Material material)
        {
            var go = new GameObject("Pulso da carta");
            go.transform.SetParent(pai, false);
            go.AddComponent<Unity.AI.Navigation.NavMeshModifier>().ignoreFromBuild = true;
            _malha = CriarAnel();
            go.AddComponent<MeshFilter>().sharedMesh = _malha;
            _anel = go.AddComponent<MeshRenderer>();
            _anel.sharedMaterial = material;
            _anel.shadowCastingMode = ShadowCastingMode.Off;
            _anel.receiveShadows = false;
            go.SetActive(false);
        }

        public void Iniciar(HexWorld mundo, HexCoord casa, Color destaque)
        {
            _tempo = 0f;
            _cor = Color.Lerp(new Color32(250, 217, 144, 255), destaque, .42f);
            _anel.transform.SetPositionAndRotation(mundo.PosicaoDe(casa) + mundo.transform.up * .075f,
                mundo.transform.rotation);
            _anel.gameObject.SetActive(true);
            Atualizar(0f, mundo.tamanhoDoHexagono);
        }

        public void Atualizar(float dt, float raio)
        {
            if (!_anel.gameObject.activeSelf) return;
            _tempo += dt;
            float t = Mathf.Clamp01(_tempo / .46f);
            if (t >= 1f)
            {
                _anel.gameObject.SetActive(false);
                return;
            }

            float tamanho = raio * Mathf.Lerp(.35f, 1.08f, 1f - (1f - t) * (1f - t));
            _anel.transform.localScale = new Vector3(tamanho, 1f, tamanho);
            Color cor = _cor;
            cor.a = .72f * (1f - t);
            _bloco.SetColor(IdCor, cor);
            _anel.SetPropertyBlock(_bloco);
        }

        public void Apagar()
        {
            if (_anel != null) _anel.gameObject.SetActive(false);
        }

        public void Liberar()
        {
            if (_anel != null) Object.Destroy(_anel.gameObject);
            Object.Destroy(_malha);
        }

        static Mesh CriarAnel()
        {
            var vertices = new Vector3[12];
            var indices = new int[36];
            for (int i = 0; i < 6; i++)
            {
                float a = (30 + 60 * i) * Mathf.Deg2Rad;
                var ponto = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = ponto;
                vertices[i * 2 + 1] = ponto * .91f;

                int seguinte = (i + 1) % 6 * 2;
                int k = i * 6;
                indices[k] = indices[k + 3] = i * 2;
                indices[k + 1] = i * 2 + 1;
                indices[k + 2] = indices[k + 4] = seguinte + 1;
                indices[k + 5] = seguinte;
            }

            var malha = new Mesh { name = "Pulso de construcao", vertices = vertices, triangles = indices };
            malha.RecalculateBounds();
            return malha;
        }
    }
}
