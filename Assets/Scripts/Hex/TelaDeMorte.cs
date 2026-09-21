using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Loopia.Hex
{
    /// <summary>
    /// A tela que aparece quando o Lucca morre, com o botao "Volte ao acampamento".
    ///
    /// Tudo aqui e ligado por UnityEvent no Inspector: o LoopDirector.aoGameOver chama Mostrar(),
    /// e o onClick do botao chama VoltarAoAcampamento().
    /// </summary>
    [DisallowMultipleComponent]
    public class TelaDeMorte : MonoBehaviour
    {
        [Header("Tela")]
        [Tooltip("O painel inteiro. Comeca escondido.")]
        public GameObject painel;

        [Tooltip("Texto com o resumo da jornada que acabou.")]
        public TMP_Text textoResumo;

        [Header("Referencias")]
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public LoopDirector diretor;
        public LuccaStatus status;

        [Header("Cena")]
        [Tooltip("Nome da cena do acampamento. Ela precisa estar no Build Settings.")]
        public string cenaDoAcampamento = "Acampamento";

        void Awake()
        {
            if (painel != null) painel.SetActive(false);
        }

        public void Mostrar()
        {
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();

            if (textoResumo != null)
            {
                int volta = diretor != null ? diretor.VoltasCompletas + 1 : 1;
                int moedas = status != null ? status.Moedas : 0;
                textoResumo.text = "O Lucca caiu na volta " + volta + ".\n" +
                                   "Moedas nesta jornada: " + moedas;
            }

            if (painel != null) painel.SetActive(true);
        }

        public void VoltarAoAcampamento()
        {
            // As moedas ja foram salvas a cada coleta; isto so garante o recorde de voltas.
            DadosDoJogador.Instancia.Salvar();
            SceneManager.LoadScene(cenaDoAcampamento);
        }
    }
}
