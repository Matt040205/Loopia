using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    /// <summary>
    /// A cena do acampamento: mostra quantas moedas o jogador tem no total e o recorde de voltas,
    /// lidos do singleton DadosDoJogador (que carrega do JSON), e comeca uma jornada nova.
    /// </summary>
    [DisallowMultipleComponent]
    public class TelaDoAcampamento : MonoBehaviour
    {
        [Header("Textos")]
        public TMP_Text textoMoedas;
        public TMP_Text textoRecorde;

        [Tooltip("Opcional: mostra onde o JSON esta salvo.")]
        public TMP_Text textoArquivo;

        [Tooltip("Quando marcado, o caminho do save comeca escondido e F3 alterna (como a depuracao da jornada).")]
        public bool arquivoSoComF3;

        [Header("Cena")]
        [Tooltip("Nome da cena da jornada. Ela precisa estar no Build Settings.")]
        public string cenaDaJornada = "HexPrototipo";

        DadosDoJogador _dados;

        void Start()
        {
            _dados = DadosDoJogador.Instancia;
            _dados.AoMudar += Atualizar;
            if (arquivoSoComF3 && textoArquivo != null) textoArquivo.gameObject.SetActive(false);
            Atualizar();
        }

        void Update()
        {
            if (!arquivoSoComF3 || textoArquivo == null || !TeclaDeDepuracao()) return;
            textoArquivo.gameObject.SetActive(!textoArquivo.gameObject.activeSelf);
        }

        static bool TeclaDeDepuracao()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F3);
#endif
        }

        void OnDestroy()
        {
            if (_dados != null) _dados.AoMudar -= Atualizar;
        }

        public void Atualizar()
        {
            if (_dados == null) return;

            if (textoMoedas != null) textoMoedas.text = "Moedas: " + _dados.MoedasTotais;
            if (textoRecorde != null) textoRecorde.text = "Recorde de voltas: " + _dados.RecordeDeVoltas;
            if (textoArquivo != null) textoArquivo.text = "Salvo em " + _dados.CaminhoDoArquivo;
        }

        public void NovaJornada()
        {
            SceneManager.LoadScene(cenaDaJornada);
        }
    }
}
