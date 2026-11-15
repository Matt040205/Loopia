using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Loopia.Hex
{
    /// <summary>
    /// Numeros de uma jornada, para balanceamento (docs/gameplay/BALANCE_BASELINE.md §8). So contagem: nao
    /// conhece a cena (os atributos sao so para o Inspector); o MedidorDaPartida alimenta. Tempos em segundos de jogo (Time.time), entao
    /// rodar com timeScale alto nao distorce as duracoes.
    /// </summary>
    [Serializable]
    public class MetricasDaPartida
    {
        [Serializable]
        public class Volta
        {
            public int numero;
            public float duracao;
            public int flechasDisparadas;
            public int danoRecebido;
            public int inimigosDerrotados;
            public int pvAoFechar;
        }

        public int casasNoAnel;
        public List<Volta> voltas = new List<Volta>();

        [UnityEngine.Header("Confronto")]
        public bool chefeComecou;
        public float duracaoDoChefe;
        public int flechasNoChefe;
        public int flechasEmOutrosNoChefe;
        public int recargasNoChefe;
        public int danoRecebidoNoChefe;
        public int revoadas;
        public int morcegosDaRevoada;
        public int pvNoInicioDoChefe;

        [UnityEngine.Tooltip("Tempo com o chefe vivo e o Lucca sem nenhuma flecha.")]
        public float tempoSemFlechasNoChefe;

        [UnityEngine.Tooltip("Maior trecho continuo sem flechas com o chefe vivo. Trecho longo = sinal de impasse.")]
        public float maiorTrechoSemFlechasNoChefe;

        public string resultado = "";
        public float duracaoTotal;

        float _inicio;
        float _inicioDaVolta;
        float _inicioDoChefe;
        float _trechoSemFlechas;
        bool _terminou;
        Volta _volta = new Volta { numero = 1 };

        public bool Terminou => _terminou;
        public bool NoChefe => chefeComecou && !_terminou;

        public void Comecar(float agora, int casas)
        {
            _inicio = agora;
            _inicioDaVolta = agora;
            casasNoAnel = casas;
        }

        public void RegistrarDisparo(bool noChefe)
        {
            if (_terminou) return;
            if (NoChefe)
            {
                if (noChefe) flechasNoChefe++;
                else flechasEmOutrosNoChefe++;
                return;
            }
            _volta.flechasDisparadas++;
        }

        public void RegistrarDano(int dano)
        {
            if (_terminou || dano <= 0) return;
            if (NoChefe) danoRecebidoNoChefe += dano;
            else _volta.danoRecebido += dano;
        }

        public void RegistrarAbate()
        {
            if (_terminou || NoChefe) return;
            _volta.inimigosDerrotados++;
        }

        public void FecharVolta(int numero, float agora, int pv)
        {
            if (_terminou || NoChefe) return;
            _volta.numero = numero;
            _volta.duracao = agora - _inicioDaVolta;
            _volta.pvAoFechar = pv;
            voltas.Add(_volta);
            _volta = new Volta { numero = numero + 1 };
            _inicioDaVolta = agora;
        }

        public void ComecarChefe(float agora, int pv)
        {
            if (chefeComecou || _terminou) return;
            chefeComecou = true;
            _inicioDoChefe = agora;
            pvNoInicioDoChefe = pv;
        }

        public void RegistrarRecarga()
        {
            if (NoChefe) recargasNoChefe++;
        }

        public void RegistrarRevoada(int morcegos)
        {
            if (!NoChefe) return;
            revoadas++;
            morcegosDaRevoada += Math.Max(0, morcegos);
        }

        /// <summary>Chamado todo frame com o tempo do frame.</summary>
        public void Avancar(float dt, bool chefeVivo, int flechas)
        {
            if (!NoChefe || !chefeVivo || dt <= 0f) return;
            if (flechas > 0)
            {
                _trechoSemFlechas = 0f;
                return;
            }
            tempoSemFlechasNoChefe += dt;
            _trechoSemFlechas += dt;
            if (_trechoSemFlechas > maiorTrechoSemFlechasNoChefe) maiorTrechoSemFlechasNoChefe = _trechoSemFlechas;
        }

        public void Terminar(float agora, EstadoDaPartida estado)
        {
            if (_terminou) return;
            _terminou = true;
            resultado = estado.ToString();
            duracaoTotal = agora - _inicio;
            if (chefeComecou) duracaoDoChefe = agora - _inicioDoChefe;
        }

        /// <summary>Trecho sem flechas com o chefe vivo maior que o limite (segundos de jogo).</summary>
        public bool PossivelImpasse(float limite) => maiorTrechoSemFlechasNoChefe >= limite;

        /// <summary>Uma linha por volta e uma do chefe, facil de colar num relatorio.</summary>
        public string Resumo()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("[Metricas] anel=").Append(casasNoAnel).Append(" casas, resultado=").Append(resultado)
              .Append(", total=").Append(duracaoTotal.ToString("F1", c)).Append("s\n");
            foreach (Volta v in voltas)
            {
                sb.Append("  volta ").Append(v.numero).Append(": ").Append(v.duracao.ToString("F1", c)).Append("s, flechas ")
                  .Append(v.flechasDisparadas).Append(", dano ").Append(v.danoRecebido).Append(", abates ")
                  .Append(v.inimigosDerrotados).Append(", PV ao fechar ").Append(v.pvAoFechar).Append('\n');
            }
            if (chefeComecou)
            {
                sb.Append("  chefe: ").Append(duracaoDoChefe.ToString("F1", c)).Append("s, flechas no chefe ")
                  .Append(flechasNoChefe).Append(", em outros ").Append(flechasEmOutrosNoChefe).Append(", recargas ")
                  .Append(recargasNoChefe).Append(", dano recebido ").Append(danoRecebidoNoChefe).Append(" (PV inicial ")
                  .Append(pvNoInicioDoChefe).Append("), revoadas ").Append(revoadas).Append(" (")
                  .Append(morcegosDaRevoada).Append(" morcegos), sem flechas ")
                  .Append(tempoSemFlechasNoChefe.ToString("F1", c)).Append("s (maior trecho ")
                  .Append(maiorTrechoSemFlechasNoChefe.ToString("F1", c)).Append("s)");
            }
            return sb.ToString();
        }
    }
}
