using UnityEngine;
using UnityEngine.VFX;

namespace Loopia.Hex
{
    /// <summary>
    /// Efeito de VFX Graph que nasce, toca uma vez e some (brilho da moeda, fagulhas da construcao).
    /// O prefab leva um <see cref="VisualEffect"/>; a emissao para depois de <see cref="emissao"/> e o
    /// objeto e destruido depois de <see cref="duracao"/>, quando as particulas ja morreram.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    [DisallowMultipleComponent]
    public class EfeitoPassageiro : MonoBehaviour
    {
        [Tooltip("Segundos emitindo. Para sistemas em rajada (Burst) basta um valor pequeno.")]
        [Min(0f)] public float emissao = 0.3f;

        [Tooltip("Segundos ate destruir o objeto. Maior que a vida maxima das particulas.")]
        [Min(0.1f)] public float duracao = 2.5f;

        /// <summary>Toca o efeito em <paramref name="ponto"/>. Fora do Play Mode nao cria nada.</summary>
        public static GameObject Tocar(GameObject prefab, Vector3 ponto)
        {
            if (prefab == null || !Application.isPlaying) return null;
            return Instantiate(prefab, ponto, Quaternion.identity);
        }

        void Start()
        {
            var efeito = GetComponent<VisualEffect>();
            efeito.Play();
            if (emissao > 0f) Invoke(nameof(Parar), emissao);
            Destroy(gameObject, duracao);
        }

        void Parar() => GetComponent<VisualEffect>().Stop();
    }
}
