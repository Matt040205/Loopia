using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Como transformar um inimigo comum (o morcego) no chefe, sem InimigoDef novo: multiplicadores sobre os numeros
    /// do GDD e o comportamento do mergulho ajustado para quem fica no centro do anel. O visual pode ser proprio
    /// (campo prefab); os numeros continuam todos derivados do morcego base.
    ///
    /// Todos os valores sao hipoteses de balanceamento (docs/gameplay/BALANCE_BASELINE.md §7), nao numeros
    /// medidos. O resultado ainda passa pela escala da volta no InimigoSpawner (x1,4641 na 4a volta).
    /// </summary>
    [Serializable]
    public class VarianteDoChefe
    {
        public string nome = "Morcego Ancião";
        public Color cor = new Color(0.45f, 0.08f, 0.12f);

        [Tooltip("Visual proprio do chefe (Sprint 5B: Bat_AncientBoss). Vazio: herda o prefab do inimigo base.")]
        public GameObject prefab;

        [Tooltip("Maior dimensao do visual proprio (u). So vale com prefab proprio: o modelo do Anciao ja tem o tamanho " +
                 "de chefe, entao nao se multiplica a escala do morcego de novo. Sem prefab: escala do base x multiplicador.")]
        [Min(0.1f)] public float tamanhoDoVisual = 4.5f;

        [Header("Multiplicadores sobre o inimigo base")]
        [Min(1f)] public float multiplicadorDePv = 10f;
        [Min(0f)] public float multiplicadorDeAtaque = 1.2f;
        [Min(0f)] public float multiplicadorDeXp = 4f;
        [Min(1f)] public float multiplicadorDeEscala = 5f;

        [Header("Mergulho a partir do centro do miolo")]
        [Tooltip("Mais alto que o morcego comum, para ler como 'acima do anel'.")]
        [Min(0f)] public float alturaDeVoo = 2.2f;
        [Min(0.1f)] public float intervaloDeAtaque = 4f;
        [Tooltip("Do centro ate a borda do anel sao ~15-30 u: precisa ser rapido e durar mais que o do morcego.")]
        [Min(0.5f)] public float velocidadeDoRasante = 14f;
        [Min(0.1f)] public float duracaoMaximaDoRasante = 3.5f;
        [Min(0.05f)] public float distanciaDoAcerto = 0.7f;

        /// <summary>
        /// Cria uma copia em memoria do inimigo base com os numeros do chefe. Quem chama e dono da instancia
        /// e deve destrui-la. O asset base nao e tocado.
        /// </summary>
        public InimigoDef Montar(InimigoDef baseDef)
        {
            if (baseDef == null) return null;

            var def = ScriptableObject.CreateInstance<InimigoDef>();
            def.name = nome + " (variante de " + baseDef.name + ")";
            def.hideFlags = HideFlags.DontSave;

            def.nome = nome;
            def.cor = cor;
            def.prefab = prefab != null ? prefab : baseDef.prefab;
            def.escala = prefab != null ? tamanhoDoVisual : baseDef.escala * multiplicadorDeEscala;

            def.pv = Mathf.Max(1, Mathf.RoundToInt(baseDef.pv * multiplicadorDePv));
            def.ataque = Mathf.Max(0, Mathf.RoundToInt(baseDef.ataque * multiplicadorDeAtaque));
            def.xp = Mathf.Max(0, Mathf.RoundToInt(baseDef.xp * multiplicadorDeXp));

            def.intervaloDeAtaque = intervaloDeAtaque;
            // Do centro ele alcanca qualquer casa do anel.
            def.alcance = 999;
            def.voa = true;
            def.alturaDeVoo = alturaDeVoo;
            def.balanco = baseDef.balanco * 2f;
            def.estilo = EstiloDeAtaque.Rasante;

            def.velocidadeDePerseguicao = baseDef.velocidadeDePerseguicao;
            def.raioDeDeteccao = baseDef.raioDeDeteccao;
            def.distanciaCorpoACorpo = baseDef.distanciaCorpoACorpo;
            def.efeitoDeMorte = baseDef.efeitoDeMorte;

            def.velocidadeDoRasante = velocidadeDoRasante;
            def.duracaoMaximaDoRasante = duracaoMaximaDoRasante;
            def.distanciaDoAcerto = distanciaDoAcerto;
            return def;
        }
    }
}
