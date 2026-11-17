using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Uma plataforma colocada no mapa. E so o par "que carta" + "em que casa", mais o estado
    /// que o efeito precisa guardar; toda a regra mora no PlataformaDef.
    /// </summary>
    [DisallowMultipleComponent]
    public class Plataforma : MonoBehaviour
    {
        public PlataformaDef Def { get; private set; }
        public HexCoord Casa { get; private set; }
        public MapaDePlataformas Mapa { get; private set; }

        /// <summary>
        /// Usado pelas plataformas de recompensa: fica false depois que o Lucca pega o item
        /// e volta a true quando a volta fecha.
        /// </summary>
        bool _carregada;
        GameObject _modelo;
        GameObject _marcador;
        HexTile _ilha;
        public bool Carregada
        {
            get => _carregada;
            set
            {
                _carregada = value;
                if (_modelo != null && Def is PlataformaRecompensaDef) _modelo.SetActive(value);
            }
        }

        public void Montar(PlataformaDef def, HexCoord casa, MapaDePlataformas mapa)
        {
            Def = def;
            Casa = casa;
            Mapa = mapa;
            Carregada = true;
            name = def.nomeExibido + " " + casa;
            _modelo = VisualDeIlha.Criar(def.modelo, transform, def.tamanhoDoModelo);
            if (_modelo != null)
            {
                _modelo.name = "Item " + def.nomeExibido;
                _modelo.transform.localPosition = Vector3.up * def.alturaDoModelo;
                if (def.girarModelo) _modelo.AddComponent<ItemGiratorio>();
            }
            MontarMarcador();
        }

        public GameObject Marcador => _marcador;

        /// <summary>A carta mostra um item no centro da ilha agora (Coracao, Moedas, Flechas...).</summary>
        public bool TemItemNoCentro => _modelo != null && _modelo.activeSelf;

        /// <summary>O marcador e filho da ilha (sobe junto com o relevo), alinhado ao mundo e fora do bake.</summary>
        void MontarMarcador()
        {
            if (Def.marcador == null || Mapa == null || Mapa.mundo == null) return;
            if (!Mapa.mundo.TryGetIlha(Casa, out _ilha) || _ilha == null) return;
            _marcador = Instantiate(Def.marcador, _ilha.transform);
            _marcador.name = "Marcador " + Def.nomeExibido;
            _marcador.transform.localPosition = Vector3.zero;
            _marcador.transform.rotation = Quaternion.identity;
            _marcador.transform.localScale = Vector3.one;
            VisualDeIlha.PrepararArte(_marcador);
            if (Def.ocultarDecoracao) MostrarDecoracao(false);
        }

        void MostrarDecoracao(bool mostrar)
        {
            if (_ilha == null) return;
            foreach (var nome in new[] { "Decoracoes", "VegetacaoBaixa" })
            {
                var t = _ilha.transform.Find(nome);
                if (t != null) t.gameObject.SetActive(mostrar);
            }
        }

        void OnDestroy()
        {
            if (_marcador != null)
            {
                if (Application.isPlaying) Destroy(_marcador); else DestroyImmediate(_marcador);
            }
            if (Def != null && Def.ocultarDecoracao) MostrarDecoracao(true);
        }
    }
}
