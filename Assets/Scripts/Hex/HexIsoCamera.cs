using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Camera isometrica nos valores que o time definiu: ortografica, inclinacao 30 e giro 45.
    /// Por padrao ela mede o anel gerado e se afasta o suficiente para mostrar a volta inteira;
    /// desligando o enquadramento automatico, o zoom passa a ser o valor manual.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class HexIsoCamera : MonoBehaviour
    {
        [Header("Alvo")]
        [Tooltip("Deixe vazio para achar o HexWorld da cena automaticamente.")]
        public HexWorld mundo;

        [Header("Angulo")]
        [Tooltip("Quanto a camera olha para baixo. O time definiu 30.")]
        [Range(10f, 89f)] public float inclinacao = 30f;

        [Tooltip("Rotacao em torno do mapa. O time definiu 45.")]
        public float giro = 45f;

        [Header("Enquadramento")]
        public bool ortografica = true;

        [Tooltip("Ligado: mede o anel e mostra a volta inteira. Desligado: usa o zoom manual.")]
        public bool enquadrarAutomaticamente = true;

        [Tooltip("Folga em volta do anel. 1 = colado nas bordas.")]
        [Range(1f, 2f)] public float margem = 1.15f;

        [Tooltip("Zoom usado quando o enquadramento automatico esta desligado.")]
        [Min(0.5f)] public float zoomManual = 5f;

        [Tooltip("Distancia da camera ate o centro. So muda a projecao em perspectiva.")]
        [Min(1f)] public float distancia = 40f;

        Camera _camera;

        Camera Cam
        {
            get
            {
                if (_camera == null) _camera = GetComponent<Camera>();
                return _camera;
            }
        }

        void OnEnable() => Enquadrar();

        void Start() => Enquadrar();

        void OnValidate() => Enquadrar();

        [ContextMenu("Enquadrar mapa")]
        public void Enquadrar()
        {
            if (Cam == null) return;
            if (mundo == null) mundo = FindFirstObjectByType<HexWorld>();

            Quaternion rotacao = Quaternion.Euler(inclinacao, giro, 0f);

            Vector3 centro = mundo != null ? mundo.transform.position : Vector3.zero;
            float meiaAltura = zoomManual;

            bool temIlhas = mundo != null && mundo.TotalDeIlhas > 0;
            if (enquadrarAutomaticamente && temIlhas)
            {
                centro = CentroDasIlhas(mundo);
                meiaAltura = MedirIlhas(mundo, centro, rotacao);
            }

            Cam.orthographic = ortografica;

            float distanciaReal = distancia;
            if (ortografica)
            {
                Cam.orthographicSize = meiaAltura;
            }
            else
            {
                float tangente = Mathf.Tan(Cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                distanciaReal = meiaAltura / Mathf.Max(0.01f, tangente);
            }

            transform.rotation = rotacao;
            transform.position = centro - rotacao * Vector3.forward * distanciaReal;

            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = Mathf.Max(1000f, distanciaReal * 4f);
        }

        static Vector3 CentroDasIlhas(HexWorld mundo)
        {
            Vector3 soma = Vector3.zero;
            int quantas = 0;

            foreach (HexTile ilha in mundo.Ilhas)
            {
                soma += ilha.transform.position;
                quantas++;
            }

            return quantas > 0 ? soma / quantas : mundo.transform.position;
        }

        /// <summary>Mede as ilhas no espaco da camera para saber quanto precisa caber na tela.</summary>
        float MedirIlhas(HexWorld mundo, Vector3 centro, Quaternion rotacao)
        {
            Quaternion inversa = Quaternion.Inverse(rotacao);

            float maiorX = 0f;
            float maiorY = 0f;
            foreach (HexTile ilha in mundo.Ilhas)
            {
                Vector3 noEspacoDaCamera = inversa * (ilha.transform.position - centro);
                maiorX = Mathf.Max(maiorX, Mathf.Abs(noEspacoDaCamera.x));
                maiorY = Mathf.Max(maiorY, Mathf.Abs(noEspacoDaCamera.y));
            }

            // Uma casa de folga, senao as ilhas da borda ficam cortadas ao meio.
            float folga = mundo.tamanhoDoHexagono;
            maiorX += folga;
            maiorY += folga;

            float aspecto = Cam.aspect > 0.01f ? Cam.aspect : 16f / 9f;
            return Mathf.Max(maiorY, maiorX / aspecto) * margem;
        }
    }
}
