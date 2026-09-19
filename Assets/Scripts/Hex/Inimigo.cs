using System;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Um inimigo vivo, parado na casa em que nasceu. Ele ataca o Lucca sempre que o Lucca
    /// estiver dentro do alcance dele — para o lobo do GDD isso quer dizer a mesma casa,
    /// para o morcego, que voa, vale a casa do lado tambem.
    ///
    /// Os numeros ja nascem multiplicados pela escala da volta: e aqui que o "+10% de PV,
    /// ATK e XP por volta" do GDD vira efeito de verdade.
    /// </summary>
    [DisallowMultipleComponent]
    public class Inimigo : MonoBehaviour
    {
        public InimigoDef Def { get; private set; }
        public HexCoord Casa { get; private set; }

        public int PvMaximo { get; private set; }
        public int PvAtual { get; private set; }
        public int Ataque { get; private set; }
        public int Xp { get; private set; }

        public bool EstaVivo => PvAtual > 0;

        /// <summary>Disparado quando o inimigo morre. Quem ouve concede o XP e limpa a lista.</summary>
        public event Action<Inimigo> AoMorrer;

        HexWorld _mundo;
        HexPlayer _player;
        LuccaStatus _status;

        float _proximoAtaque;
        float _faseDoBalanco;
        Vector3 _posicaoBase;

        public void Nascer(
            InimigoDef def, HexCoord casa, float escalaDaVolta,
            HexWorld mundo, HexPlayer player, LuccaStatus status)
        {
            Def = def;
            Casa = casa;
            _mundo = mundo;
            _player = player;
            _status = status;

            PvMaximo = Mathf.Max(1, Mathf.RoundToInt(def.pv * escalaDaVolta));
            PvAtual = PvMaximo;
            Ataque = Mathf.RoundToInt(def.ataque * escalaDaVolta);
            Xp = Mathf.RoundToInt(def.xp * escalaDaVolta);

            name = def.nome + " " + casa;

            _posicaoBase = mundo.PosicaoDe(casa) + Vector3.up * (def.voa ? def.alturaDeVoo : 0f);
            transform.position = _posicaoBase;

            // Fases diferentes para os morcegos nao balancarem todos juntos.
            _faseDoBalanco = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            // Espera um ciclo antes do primeiro golpe, para nao machucar no mesmo frame em que nasce.
            _proximoAtaque = Time.time + def.intervaloDeAtaque;

            MontarVisual(def);
        }

        void Update()
        {
            if (!EstaVivo) return;

            if (Def.voa && Def.balanco > 0f)
            {
                float altura = Mathf.Sin(Time.time * 2.5f + _faseDoBalanco) * Def.balanco;
                transform.position = _posicaoBase + Vector3.up * altura;
            }

            if (_player == null || _status == null) return;
            if (Time.time < _proximoAtaque) return;

            if (HexCoord.Distance(Casa, _player.Coord) > Def.alcance) return;

            _proximoAtaque = Time.time + Def.intervaloDeAtaque;
            _status.ReceberDano(Ataque);
        }

        public void ReceberDano(int dano)
        {
            if (!EstaVivo || dano <= 0) return;

            PvAtual = Mathf.Max(0, PvAtual - dano);
            if (PvAtual > 0) return;

            AoMorrer?.Invoke(this);
            Destroy(gameObject);
        }

        void MontarVisual(InimigoDef def)
        {
            GameObject corpo;
            if (def.prefab != null)
            {
                corpo = Instantiate(def.prefab, transform);
            }
            else
            {
                // Sem arte ainda: bolinha para quem voa, caixa para quem anda.
                corpo = GameObject.CreatePrimitive(def.voa ? PrimitiveType.Sphere : PrimitiveType.Cube);
                Collider colisor = corpo.GetComponent<Collider>();
                if (colisor != null) Destroy(colisor);
                corpo.transform.SetParent(transform, false);
            }

            corpo.name = "Corpo";
            corpo.transform.localScale = Vector3.one * def.escala;
            corpo.transform.localPosition = Vector3.up * (def.voa ? 0f : def.escala * 0.5f);

            if (def.prefab != null) return;

            var renderizador = corpo.GetComponent<MeshRenderer>();
            if (renderizador == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = def.nome };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", def.cor);
            if (material.HasProperty("_Color")) material.SetColor("_Color", def.cor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            renderizador.sharedMaterial = material;
        }
    }
}
