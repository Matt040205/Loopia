using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Loopia.Hex
{
    /// <summary>
    /// Marca de fuligem que um inimigo deixa no chao ao morrer (criterio de render: instanciamento de
    /// Decal Projector). O prefab tem um <see cref="DecalProjector"/> apontado para baixo; este componente
    /// segura a marca visivel e depois a apaga pelo <c>fadeFactor</c> antes de destruir o objeto.
    /// </summary>
    [RequireComponent(typeof(DecalProjector))]
    [DisallowMultipleComponent]
    public class MarcaNoChao : MonoBehaviour
    {
        [Tooltip("Segundos com a marca inteira antes de comecar a sumir.")]
        [Min(0f)] public float tempoVisivel = 6f;

        [Tooltip("Segundos que a marca leva para sumir de vez.")]
        [Min(0.01f)] public float tempoSumindo = 3f;

        DecalProjector _projetor;
        float _idade;

        public DecalProjector Projetor => _projetor != null ? _projetor : (_projetor = GetComponent<DecalProjector>());

        /// <summary>
        /// Cria a marca no chao em <paramref name="ponto"/>, projetando para baixo com giro aleatorio. Fora do Play
        /// Mode nao cria nada (o Update que apaga a marca nao roda no Editor parado).
        /// </summary>
        public static GameObject Criar(GameObject prefab, Vector3 ponto)
        {
            if (prefab == null || !Application.isPlaying) return null;
            return Instantiate(prefab, ponto, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
        }

        void OnEnable()
        {
            _idade = 0f;
            Projetor.fadeFactor = 1f;
        }

        void Update() => Envelhecer(Time.deltaTime);

        /// <summary>Avanca a idade da marca. Devolve false quando ela ja sumiu (e foi destruida).</summary>
        public bool Envelhecer(float deltaTime)
        {
            _idade += deltaTime;
            Projetor.fadeFactor = 1f - Mathf.Clamp01((_idade - tempoVisivel) / tempoSumindo);
            if (_idade < tempoVisivel + tempoSumindo) return true;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
            return false;
        }
    }
}
