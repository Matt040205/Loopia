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

        /// <summary>O que as plataformas somam agora (G2). Definido pelo MapaDePlataformas.</summary>
        public ModificadoresDoLucca Modificadores { get; private set; }

        /// <summary>PV maximo somado pelas plataformas colocadas (campo pacifico, cabana, santuario...).</summary>
        public int BonusDePvMaximo => Modificadores.PvMaximo;

        /// <summary>O teto de vida de verdade: o base do GDD mais o que as plataformas deram.</summary>
        public int PvMaximoTotal => pvMaximo + BonusDePvMaximo;

        /// <summary>Flechas maximas somadas pelas plataformas (oficina de arqueiro).</summary>
        public int BonusDeFlechasMaximo => Modificadores.FlechasMaximo;

        /// <summary>O teto de flechas de verdade: base mais plataformas. Recargas e itens respeitam este teto.</summary>
        public int FlechasMaximoTotal => flechasMaximo + BonusDeFlechasMaximo;

        /// <summary>PV extra a cada coracao coletado (fonte restauradora).</summary>
        public int BonusDeCuraPorCoracao => Modificadores.CuraPorCoracao;

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
            FlechasAtuais = FlechasMaximoTotal;
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
            FlechasAtuais = FlechasMaximoTotal;
            AoMudar?.Invoke();
        }

        /// <summary>
        /// Define tudo o que as plataformas somam (G2). Valores negativos viram 0.
        ///
        /// PV e flechas seguem a mesma regra: se o teto sobe, o valor atual sobe a mesma diferenca (senao
        /// colocar uma cabana daria vida maxima maior sem dar vida nenhuma na hora); se o teto desce, o valor
        /// atual so e limitado ao novo teto, sem cura nem dano extra. Morto, so o teto muda.
        /// </summary>
        public void DefinirModificadores(ModificadoresDoLucca modificadores)
        {
            var novo = new ModificadoresDoLucca(
                Mathf.Max(0, modificadores.PvMaximo),
                Mathf.Max(0f, modificadores.Dano),
                Mathf.Max(0, modificadores.FlechasMaximo),
                Mathf.Max(0, modificadores.CuraPorCoracao));
            if (novo == Modificadores) return;

            int difPv = novo.PvMaximo - Modificadores.PvMaximo;
            int difFlechas = novo.FlechasMaximo - Modificadores.FlechasMaximo;
            Modificadores = novo;

            if (EstaVivo && difPv > 0) PvAtual += difPv;
            PvAtual = Mathf.Clamp(PvAtual, 0, PvMaximoTotal);

            if (EstaVivo && difFlechas > 0) FlechasAtuais += difFlechas;
            FlechasAtuais = Mathf.Clamp(FlechasAtuais, 0, FlechasMaximoTotal);

            AoMudar?.Invoke();
        }

        /// <summary>Atalho anterior a G2: troca so o bonus de PV, mantendo os outros modificadores.</summary>
        public void DefinirBonusDePvMaximo(int bonus)
        {
            DefinirModificadores(Modificadores.ComPvMaximo(bonus));
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

        /// <summary>
        /// Cura de coracao coletado: a cura do item mais o bonus das Fontes (G2). So para coracoes; as
        /// outras curas usam Curar. Item sem cura (cura &lt;= 0) nao ganha bonus. Nao ressuscita.
        /// </summary>
        public void CurarComCoracao(int cura)
        {
            if (cura <= 0) return;
            Curar(cura + BonusDeCuraPorCoracao);
        }

        public bool GastarFlecha()
        {
            if (FlechasAtuais <= 0) return false;

            FlechasAtuais--;
            AoMudar?.Invoke();
            return true;
        }

        /// <summary>Item flecha do GDD: +10. Limitado ao maximo efetivo, senao a recarga da volta viraria castigo.</summary>
        public void GanharFlechas(int quantidade)
        {
            if (quantidade <= 0 || FlechasAtuais >= FlechasMaximoTotal) return;

            FlechasAtuais = Mathf.Min(FlechasMaximoTotal, FlechasAtuais + quantidade);
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
