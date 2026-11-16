using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Loopia.Hex
{
    /// <summary>
    /// A tela de fim de jornada, com o botao "Volte ao acampamento". Serve aos dois finais: o titulo
    /// ("Fim da jornada") e neutro e so o resumo muda.
    ///
    /// Tudo aqui e ligado por UnityEvent no Inspector: o LoopDirector.aoGameOver chama Mostrar(), o
    /// LoopDirector.aoVencer chama MostrarVitoria() e o onClick do botao chama VoltarAoAcampamento().
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

        [Tooltip("Segundos (tempo real) para o painel aparecer por completo. O painel ja fica ativo e clicavel desde o " +
                 "inicio; so a opacidade sobe. 0 = aparece de uma vez.")]
        [Min(0f)] public float fade = 0.35f;

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
                int moedas = status != null ? status.Moedas : 0;
                string queda = diretor != null && diretor.DerrotaNoChefe
                    ? "O Lucca caiu diante do chefe."
                    : "O Lucca caiu na volta " + (diretor != null ? diretor.VoltasCompletas + 1 : 1) + ".";
                textoResumo.text = queda + "\n" + "Moedas nesta jornada: " + moedas;
            }

            AbrirPainel();
        }

        /// <summary>Final bom: o chefe caiu. Mesmo painel (o titulo "Fim da jornada" e neutro), outro resumo.</summary>
        public void MostrarVitoria()
        {
            if (diretor == null) diretor = FindFirstObjectByType<LoopDirector>();
            if (status == null) status = FindFirstObjectByType<LuccaStatus>();

            if (textoResumo != null)
            {
                int voltas = diretor != null ? diretor.VoltasCompletas : 0;
                int moedas = status != null ? status.Moedas : 0;
                textoResumo.text = "O Lucca derrotou o chefe depois de " + voltas + " voltas!\n" +
                                   "Moedas nesta jornada: " + moedas;
            }

            AbrirPainel();
        }

        void AbrirPainel()
        {
            if (painel == null) return;
            painel.SetActive(true);
            if (fade <= 0f || !isActiveAndEnabled) return;
            var grupo = painel.GetComponent<CanvasGroup>();
            if (grupo == null) grupo = painel.AddComponent<CanvasGroup>();
            StartCoroutine(Aparecer(grupo));
        }

        /// <summary>Fade discreto em tempo real; o botao funciona desde o primeiro frame.</summary>
        IEnumerator Aparecer(CanvasGroup grupo)
        {
            float t = 0f;
            grupo.alpha = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                grupo.alpha = Mathf.Clamp01(t / fade);
                yield return null;
            }
            grupo.alpha = 1f;
        }

        public void VoltarAoAcampamento()
        {
            // As moedas ja foram salvas a cada coleta; isto so garante o recorde de voltas.
            DadosDoJogador.Instancia.Salvar();
            SceneManager.LoadScene(cenaDoAcampamento);
        }
    }
}
