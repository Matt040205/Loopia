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
        public static Projetil Disparar(Vector3 origem, Inimigo alvo, int dano, float velocidade, Color cor,
            GameObject visual = null)
        {
            if (alvo == null || !alvo.EstaVivo) return null;

            var go = new GameObject("Flecha");
            Vector3 direcao = alvo.transform.position - origem;
            go.transform.SetPositionAndRotation(origem,
                direcao.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direcao) : Quaternion.identity);
            if (visual != null) Instantiate(visual, go.transform, false);
            else CriarVisualProvisorio(go.transform, cor);

            var projetil = go.AddComponent<Projetil>();
            projetil._alvo = alvo;
            projetil._dano = dano;
            projetil._velocidade = velocidade;

            // Rede de seguranca: nenhuma flecha fica viva para sempre se algo sair do esperado.
            projetil._morreEm = Time.time + 5f;

            return projetil;
        }

        static void CriarVisualProvisorio(Transform pai, Color cor)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(pai, false);
            visual.transform.localScale = new Vector3(0.08f, 0.08f, 0.34f);
            var colisor = visual.GetComponent<Collider>();
            colisor.enabled = false;
            Destroy(colisor);
            var renderizador = visual.GetComponent<MeshRenderer>();
            renderizador.sharedMaterial = MateriaisCompartilhados.Lit("Flecha", cor);
            renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
