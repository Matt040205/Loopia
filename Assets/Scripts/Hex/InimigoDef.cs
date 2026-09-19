using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Os numeros de um tipo de inimigo, direto do GDD. Fica num asset para o time ajustar
    /// balanceamento sem mexer em codigo.
    ///
    /// GDD: morcego 50 PV / 5 ATK / 50 XP, lobo 100 PV / 15 ATK / 100 XP.
    /// </summary>
    [CreateAssetMenu(fileName = "Inimigo", menuName = "Loopia/Inimigo")]
    public class InimigoDef : ScriptableObject
    {
        [Header("Identidade")]
        public string nome = "Morcego";

        [Tooltip("GDD: inimigos comuns em tons contrastantes (verde musgo, prata, roxo sutil).")]
        public Color cor = new Color(0.45f, 0.33f, 0.55f);

        [Tooltip("Opcional. Sem prefab, o inimigo vira uma forma simples gerada na hora.")]
        public GameObject prefab;

        [Min(0.05f)] public float escala = 0.45f;

        [Header("Numeros do GDD")]
        [Min(1)] public int pv = 50;
        [Min(0)] public int ataque = 5;
        [Min(0)] public int xp = 50;

        [Header("Comportamento")]
        [Tooltip("Segundos entre um ataque e outro. O GDD nao definiu isso para os comuns.")]
        [Min(0.1f)] public float intervaloDeAtaque = 2f;

        [Tooltip("A quantos hexagonos ele alcanca o Lucca. 0 = so na mesma casa, como o lobo do GDD.")]
        [Min(0)] public int alcance = 1;

        [Tooltip("Morcego voa e fica acima da ilha; lobo fica no chao.")]
        public bool voa = false;

        [Min(0f)] public float alturaDeVoo = 1.1f;

        [Tooltip("Quanto ele balanca no ar. So vale para quem voa.")]
        [Min(0f)] public float balanco = 0.15f;
    }
}
