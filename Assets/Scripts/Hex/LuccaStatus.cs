using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Os status iniciais do Lucca conforme o GDD: 50 PV, 35 ATK, 15 flechas, 0 moedas, 0 XP.
    /// </summary>
    [DisallowMultipleComponent]
    public class LuccaStatus : MonoBehaviour
    {
        [Header("Vida")]
        [Min(1)] public int pvMaximo = 50;

        [Header("Ataque")]
        [Min(0)] public int ataque = 35;

        [Header("Municao")]
        [Min(0)] public int flechasMaximo = 15;

        [Header("Experiencia")]
        [Tooltip("GDD: do nivel 0 para o 1 custa 100 XP.")]
        [Min(1)] public int xpDoPrimeiroNivel = 100;

        [Tooltip("Quanto o custo do proximo nivel cresce. 1.75 = cada nivel custa 75% a mais " +
                 "que o anterior. O GDD escreveu '75% do valor passado', que ao pe da letra " +
                 "seria 0.75 e deixaria os niveis cada vez mais baratos — confirmar com o time.")]
        [Min(0.1f)] public float crescimentoDoNivel = 1.75f;

        /// <summary>PV maximo somado pelas plataformas colocadas (campo pacifico, cabana, vila...).</summary>
        public int BonusDePvMaximo { get; private set; }

        /// <summary>O teto de vida de verdade: o base do GDD mais o que as plataformas deram.</summary>
        public int PvMaximoTotal => pvMaximo + BonusDePvMaximo;

        public int PvAtual { get; private set; }
        public int FlechasAtuais { get; private set; }
        public int Moedas { get; private set; }
        public int Nivel { get; private set; }
        public int Xp { get; private set; }
        public int XpParaOProximoNivel { get; private set; }

        public bool EstaVivo => PvAtual > 0;

        /// <summary>Disparado a cada mudanca de PV, flechas, moedas ou XP. A HUD do GDD vai ouvir isso.</summary>
        public event Action AoMudar;

        /// <summary>GDD: "Ao atingir um novo nivel, cartas de cenario sao disponibilizadas".</summary>
        public event Action<int> AoSubirDeNivel;

        /// <summary>Disparado quando o PV chega a zero (Game Over do GDD).</summary>
        public event Action AoMorrer;

        void Awake()
        {
            PvAtual = PvMaximoTotal;
            FlechasAtuais = flechasMaximo;
            Moedas = 0;
            Nivel = 0;
            Xp = 0;
            XpParaOProximoNivel = xpDoPrimeiroNivel;
        }

        /// <summary>
        /// O beneficio do loop: "ele recupera toda a sua Vida e recarrega sua Municao".
        ///
        /// Nao ressuscita: no GDD, PV zero encerra a partida. Sem esta guarda, morrer em cima do
        /// acampamento devolvia a vida cheia no mesmo frame em que o Game Over ja tinha disparado.
        /// </summary>
        public void RestaurarVidaEMunicao()
        {
            if (!EstaVivo) return;

            PvAtual = PvMaximoTotal;
            FlechasAtuais = flechasMaximo;
            AoMudar?.Invoke();
        }

        /// <summary>
        /// Define o bonus de PV vindo das plataformas. O PV atual sobe junto com o teto, senao
        /// colocar uma cabana daria vida maxima maior sem dar vida nenhuma na hora.
        /// </summary>
        public void DefinirBonusDePvMaximo(int bonus)
        {
            bonus = Mathf.Max(0, bonus);
            if (bonus == BonusDePvMaximo) return;

            int diferenca = bonus - BonusDePvMaximo;
            BonusDePvMaximo = bonus;

            if (EstaVivo && diferenca > 0) PvAtual += diferenca;
            PvAtual = Mathf.Clamp(PvAtual, 0, PvMaximoTotal);

            AoMudar?.Invoke();
        }

        public void ReceberDano(int dano)
        {
            if (dano <= 0 || !EstaVivo) return;

            PvAtual = Mathf.Max(0, PvAtual - dano);
            AoMudar?.Invoke();

            if (PvAtual == 0) AoMorrer?.Invoke();
        }

        /// <summary>Coracao do GDD: +5 de PV, sem passar do maximo. Tambem nao ressuscita.</summary>
        public void Curar(int quanto)
        {
            if (!EstaVivo) return;
            if (quanto <= 0 || PvAtual >= PvMaximoTotal) return;

            PvAtual = Mathf.Min(PvMaximoTotal, PvAtual + quanto);
            AoMudar?.Invoke();
        }

        public bool GastarFlecha()
        {
            if (FlechasAtuais <= 0) return false;

            FlechasAtuais--;
            AoMudar?.Invoke();
            return true;
        }

        /// <summary>Item flecha do GDD: +10. Limitado ao maximo, senao a recarga da volta viraria castigo.</summary>
        public void GanharFlechas(int quantidade)
        {
            if (quantidade <= 0 || FlechasAtuais >= flechasMaximo) return;

            FlechasAtuais = Mathf.Min(flechasMaximo, FlechasAtuais + quantidade);
            AoMudar?.Invoke();
        }

        /// <summary>
        /// Moedas vao para dois lugares: o contador desta jornada (Moedas, que a HUD mostra) e o
        /// total que sobrevive entre partidas, guardado no singleton DadosDoJogador e no JSON.
        /// </summary>
        public void GanharMoedas(int quantidade)
        {
            if (quantidade <= 0) return;

            Moedas += quantidade;
            DadosDoJogador.Instancia.AdicionarMoedas(quantidade);
            AoMudar?.Invoke();
        }

        /// <summary>GDD: "Derrotar inimigos concede XP ao jogador".</summary>
        public void GanharXp(int quantidade)
        {
            if (quantidade <= 0) return;

            Xp += quantidade;

            // While, e nao if: um inimigo pode dar XP suficiente para mais de um nivel de uma vez.
            while (Xp >= XpParaOProximoNivel)
            {
                Xp -= XpParaOProximoNivel;
                Nivel++;
                XpParaOProximoNivel = Mathf.Max(1, Mathf.RoundToInt(XpParaOProximoNivel * crescimentoDoNivel));
                AoSubirDeNivel?.Invoke(Nivel);
            }

            AoMudar?.Invoke();
        }
    }
}
