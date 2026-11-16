using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Loopia.Hex
{
    [Serializable]
    public sealed class ArteDeCarta
    {
        public PlataformaDef carta;
        public string titulo;
        public string resumo;
        public string valor;
        public string condicao;
        public Texture2D fundo;
        public Texture2D atmosfera;
        public Texture2D objeto;
        public Texture2D poseA;
        public Texture2D poseB;
        public Texture2D categoria;
        public Color destaque = Color.white;
    }

    [CreateAssetMenu(menuName = "Loopia/Cartas/Catalogo visual")]
    public sealed class CatalogoVisualDeCartas : ScriptableObject
    {
        public Texture2D moldura;
        public Sprite recorte;
        public Material ouro;
        public Material pulsoNoMapa;
        public TMP_FontAsset fonteTitulo;
        public TMP_FontAsset fonteCorpo;
        public List<ArteDeCarta> cartas = new List<ArteDeCarta>();

        public bool Completo => moldura != null && recorte != null && ouro != null &&
            pulsoNoMapa != null && fonteTitulo != null && fonteCorpo != null;

        public ArteDeCarta Buscar(PlataformaDef carta)
        {
            if (carta == null) return null;
            foreach (ArteDeCarta arte in cartas)
                if (arte != null && arte.carta == carta) return arte;
            return null;
        }

        public bool Cobre(PlataformaDef carta)
        {
            ArteDeCarta arte = Buscar(carta);
            return arte != null && arte.fundo != null && arte.objeto != null;
        }
    }
}
