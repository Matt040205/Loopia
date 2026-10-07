using UnityEngine;
using UnityEngine.VFX;

namespace Loopia.Hex
{
    public class FumacaDeMorte : MonoBehaviour
    {
        public float emissao = 0.18f;
        public float duracao = 5f;
        VisualEffect _efeito;
        void Start()
        {
            _efeito = GetComponent<VisualEffect>();
            if (_efeito != null) _efeito.Play();
            Invoke(nameof(Parar), emissao);
            Destroy(gameObject, duracao);
        }
        void Parar() { if (_efeito != null) _efeito.Stop(); }
    }
}
