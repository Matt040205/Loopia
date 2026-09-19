using UnityEngine;

namespace Loopia.Hex
{
    public enum HexTileKind
    {
        /// <summary>Uma casa do anel: o caminho que o Lucca percorre.</summary>
        Caminho,

        /// <summary>A ilha do acampamento. Fica no anel e e onde a volta fecha.</summary>
        Acampamento,

        /// <summary>Ilha que o jogador colocou com uma carta.</summary>
        Construida,
    }

    /// <summary>
    /// Uma ilha do mundo: um hexagono que existe de verdade e e desenhado.
    /// Resolve sozinho a cor que deve mostrar, em ordem de prioridade:
    /// previa do trajeto > mouse em cima > cor do proprio tipo.
    /// </summary>
    [DisallowMultipleComponent]
    public class HexTile : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        HexPalette _palette;
        bool _hovered;
        bool _onPath;

        // Cor que a plataforma colocada nesta casa pede. Sem plataforma, fica no null.
        bool _temCorPropria;
        Color _corPropria;

        public HexCoord Coord { get; private set; }
        public HexTileKind Kind { get; private set; }

        /// <summary>Se false, o pathfinding nao passa por aqui.</summary>
        public bool Walkable { get; set; } = true;

        public bool EhAcampamento => Kind == HexTileKind.Acampamento;

        /// <summary>O acampamento tambem conta: ele fica sobre o anel.</summary>
        public bool EstaNoCaminho => Kind == HexTileKind.Caminho || Kind == HexTileKind.Acampamento;

        public void Initialize(HexCoord coord, HexTileKind kind, HexPalette palette)
        {
            Coord = coord;
            _palette = palette;
            _renderer = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();

            Kind = kind;
            name = NomeParaTipo(kind, coord);
            Refresh();
        }

        /// <summary>Troca o tipo da ilha depois de criada.</summary>
        public void DefinirTipo(HexTileKind kind)
        {
            if (Kind == kind) return;

            Kind = kind;
            name = NomeParaTipo(kind, Coord);
            Refresh();
        }

        /// <summary>A plataforma colocada aqui manda na cor da ilha.</summary>
        public void DefinirCor(Color cor)
        {
            _temCorPropria = true;
            _corPropria = cor;
            Refresh();
        }

        /// <summary>Volta para a cor do tipo da ilha.</summary>
        public void LimparCor()
        {
            if (!_temCorPropria) return;
            _temCorPropria = false;
            Refresh();
        }

        public void SetHovered(bool value)
        {
            if (_hovered == value) return;
            _hovered = value;
            Refresh();
        }

        public void SetOnPath(bool value)
        {
            if (_onPath == value) return;
            _onPath = value;
            Refresh();
        }

        public void ClearHighlights()
        {
            if (!_hovered && !_onPath) return;
            _hovered = false;
            _onPath = false;
            Refresh();
        }

        static string NomeParaTipo(HexTileKind kind, HexCoord coord)
        {
            switch (kind)
            {
                case HexTileKind.Acampamento: return $"Acampamento {coord}";
                case HexTileKind.Construida: return $"Ilha {coord}";
                default: return $"Caminho {coord}";
            }
        }

        Color CurrentColor()
        {
            if (_onPath) return _palette.previa;
            if (_hovered) return _palette.hover;
            if (_temCorPropria) return _corPropria;

            switch (Kind)
            {
                case HexTileKind.Acampamento: return _palette.acampamento;
                case HexTileKind.Construida: return _palette.construida;
                default: return _palette.caminho;
            }
        }

        void Refresh()
        {
            if (_renderer == null) return;

            Color color = CurrentColor();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color); // URP Lit
            _block.SetColor(ColorId, color);     // Built-in Standard
            _renderer.SetPropertyBlock(_block);
        }
    }

    /// <summary>Cores das ilhas, agrupadas para passar de uma vez para cada hexagono.</summary>
    [System.Serializable]
    public struct HexPalette
    {
        /// <summary>Casa do anel.</summary>
        public Color caminho;

        /// <summary>A ilha do acampamento.</summary>
        public Color acampamento;

        /// <summary>Ilha colocada pelo jogador.</summary>
        public Color construida;

        /// <summary>Ilha embaixo do mouse.</summary>
        public Color hover;

        /// <summary>Previa do trajeto, usada so pelo controle manual de depuracao.</summary>
        public Color previa;
    }
}
