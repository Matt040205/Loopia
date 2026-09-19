using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// A flecha do Lucca. Persegue o inimigo que foi mirado e aplica o dano ao encostar.
    /// Se o alvo morrer no caminho (outra flecha chegou antes), a flecha simplesmente some.
    /// </summary>
    [DisallowMultipleComponent]
    public class Projetil : MonoBehaviour
    {
        Inimigo _alvo;
        int _dano;
        float _velocidade;
        float _morreEm;

        /// <summary>Cria e lanca uma flecha. Devolve null se nao houver alvo vivo.</summary>
        public static Projetil Disparar(Vector3 origem, Inimigo alvo, int dano, float velocidade, Color cor)
        {
            if (alvo == null || !alvo.EstaVivo) return null;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Flecha";

            Collider colisor = go.GetComponent<Collider>();
            if (colisor != null) Destroy(colisor);

            go.transform.position = origem;
            go.transform.localScale = new Vector3(0.08f, 0.08f, 0.34f);

            var renderizador = go.GetComponent<MeshRenderer>();
            if (renderizador != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                var material = new Material(shader) { name = "Flecha" };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
                if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
                renderizador.sharedMaterial = material;
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var projetil = go.AddComponent<Projetil>();
            projetil._alvo = alvo;
            projetil._dano = dano;
            projetil._velocidade = velocidade;

            // Rede de seguranca: nenhuma flecha fica viva para sempre se algo sair do esperado.
            projetil._morreEm = Time.time + 5f;

            return projetil;
        }

        void Update()
        {
            if (_alvo == null || !_alvo.EstaVivo || Time.time > _morreEm)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 destino = _alvo.transform.position;
            Vector3 direcao = destino - transform.position;

            if (direcao.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direcao);

            float passo = _velocidade * Time.deltaTime;
            if (direcao.magnitude <= passo)
            {
                _alvo.ReceberDano(_dano);
                Destroy(gameObject);
                return;
            }

            transform.position += direcao.normalized * passo;
        }
    }
}
