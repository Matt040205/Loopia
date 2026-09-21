using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header("Cena")]
        [Tooltip("Nome da cena da jornada. Ela precisa estar no Build Settings.")]
        public string cenaDaJornada = "HexPrototipo";

        DadosDoJogador _dados;

        void Start()
        {
            _dados = DadosDoJogador.Instancia;
            _dados.AoMudar += Atualizar;
            Atualizar();
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
