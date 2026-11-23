using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Loopia.Hex
{
    /// <summary>
    /// Luz dinamica com cookie (criterio de render): o Sun e uma luz Realtime com a textura
    /// <c>CookieSombraDasNuvens</c> no campo Cookie do componente Light. Este componente so faz a
    /// sombra das nuvens andar com o vento, mudando o deslocamento do cookie no dado URP da luz.
    /// </summary>
    [RequireComponent(typeof(Light))]
    [DisallowMultipleComponent]
    public class SombraDasNuvens : MonoBehaviour
    {
        [Tooltip("Textura do cookie. Vazio = usa a que ja esta no componente Light.")]
        public Texture cookie;

        [Tooltip("Area de mundo (unidades) que uma repeticao da textura cobre.")]
        public Vector2 tamanho = new Vector2(48f, 48f);

        [Tooltip("Direcao e velocidade (unidades por segundo) das sombras, no plano da luz.")]
        public Vector2 vento = new Vector2(0.9f, 0.25f);

        Light _luz;
        UniversalAdditionalLightData _dadosUrp;
        Vector2 _deslocamento;

        public Vector2 Deslocamento => _deslocamento;

        void OnEnable()
        {
            _luz = GetComponent<Light>();
            _dadosUrp = _luz.GetUniversalAdditionalLightData();
            if (cookie != null) _luz.cookie = cookie;
            Aplicar();
        }

        void Update() => Avancar(Time.deltaTime);

        /// <summary>Anda o cookie pelo vento. O deslocamento da a volta a cada repeticao, sem salto visivel.</summary>
        public void Avancar(float deltaTime)
        {
            _deslocamento += vento * deltaTime;
            _deslocamento.x = tamanho.x > 0f ? Mathf.Repeat(_deslocamento.x, tamanho.x) : 0f;
            _deslocamento.y = tamanho.y > 0f ? Mathf.Repeat(_deslocamento.y, tamanho.y) : 0f;
            Aplicar();
        }

        void Aplicar()
        {
            if (_dadosUrp == null)
            {
                _luz = GetComponent<Light>();
                _dadosUrp = _luz.GetUniversalAdditionalLightData();
            }
            _dadosUrp.lightCookieSize = tamanho;
            _dadosUrp.lightCookieOffset = _deslocamento;
        }
    }
}
