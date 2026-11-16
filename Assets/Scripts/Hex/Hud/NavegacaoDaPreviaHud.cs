#if UNITY_EDITOR
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Loopia.Hex
{
    public sealed class NavegacaoDaPreviaHud : MonoBehaviour
    {
        public const string Pasta = "Assets/Previa/Hud/";
        public TMP_Text diagnostico;

        public void NovaJornada() => Carregar("HexPrototipo");

        public void Voltar()
        {
            DadosDoJogador.Instancia.Salvar();
            Carregar("Acampamento");
        }

        static void Carregar(string cena)
        {
            EditorSceneManager.LoadSceneInPlayMode(Pasta + cena + ".unity",
                new LoadSceneParameters(LoadSceneMode.Single));
        }

        void Update()
        {
            if (diagnostico == null) return;
#if ENABLE_INPUT_SYSTEM
            bool alternar = Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
            bool alternar = Input.GetKeyDown(KeyCode.F3);
#endif
            if (alternar) diagnostico.gameObject.SetActive(!diagnostico.gameObject.activeSelf);
        }
    }
}
#endif
