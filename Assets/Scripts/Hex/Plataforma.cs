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
        }
    }
}
