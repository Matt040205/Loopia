using TMPro;
using UnityEngine;

namespace Loopia.Hex
{
    [DisallowMultipleComponent]
    public sealed class ResultadoDaJornadaVisual : MonoBehaviour
    {
        [SerializeField] LoopDirector diretor;
        [SerializeField] LuccaStatus status;
        [SerializeField] TMP_Text titulo;
        [SerializeField] TMP_Text resumo;
        [SerializeField] TMP_Text moedas;

        public void Configurar(LoopDirector origem, LuccaStatus jogador, TMP_Text cabecalho,
            TMP_Text contexto, TMP_Text obtidas)
        {
            diretor = origem;
            status = jogador;
            titulo = cabecalho;
            resumo = contexto;
            moedas = obtidas;
        }

        void OnEnable()
        {
            if (diretor == null || titulo == null) return;
            if (diretor.Venceu)
            {
                titulo.text = "Chefe derrotado";
                titulo.color = TemaDaJornada.Ouro;
                resumo.text = $"Lucca venceu depois de {diretor.VoltasCompletas} voltas.";
            }
            else if (diretor.DerrotaNoChefe)
            {
                titulo.text = "Derrota diante do chefe";
                titulo.color = TemaDaJornada.Perigo;
                resumo.text = "Lucca chegou ao confronto final e caiu diante do chefe.";
            }
            else
            {
                titulo.text = "Fim da jornada";
                titulo.color = TemaDaJornada.Perigo;
                resumo.text = $"Lucca caiu na volta {diretor.VoltasCompletas + 1}.";
            }
            moedas.text = $"Moedas nesta jornada · {(status != null ? status.Moedas : 0)}";
        }
    }
}
