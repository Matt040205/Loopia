using System;
using System.IO;
using Loopia.Hex;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MontagemDasCartas
{
    const string Pasta = "Assets/Arte/Cartas/";
    const string FonteCorpo = "Assets/Settings/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    sealed class Linha
    {
        public readonly string Id, Titulo, Resumo, Valor, Paisagem, Icone, Condicao;

        public Linha(string id, string titulo, string resumo, string valor, string paisagem,
            string icone, string condicao = null)
        {
            Id = id;
            Titulo = titulo;
            Resumo = resumo;
            Valor = valor;
            Paisagem = paisagem;
            Icone = icone;
            Condicao = condicao;
        }
    }

    static readonly Linha[] Linhas =
    {
        new Linha("Basica", "Básica", "Caminho comum", "", "Bosque", "Caminho"),
        new Linha("Floresta", "Floresta", "Lobo por volta", "1", "Bosque", "Risco"),
        new Linha("IlhaAlta", "Ilha Alta", "Relevo e mirante", "2,2 m", "Altura", "Caminho",
            "Lucca precisa passar por uma Mola antes da subida."),
        new Linha("Mola", "Mola", "Impulso", "", "Brasa", "Caminho"),
        new Linha("Flechas", "Flechas", "Flechas por volta", "+10", "Ouro", "Dano"),
        new Linha("Coracao", "Coração", "Cura por volta", "+5", "Brasa", "Vida"),
        new Linha("Moedas", "Moedas", "Moedas por volta", "+3", "Ouro", "Recurso"),
        new Linha("Torre", "Torre", "Dano de flechas", "+8%", "Altura", "Dano"),
        new Linha("AreaDeTreino", "Área de Treino", "Dano e adjacência", "+8%", "Ouro", "Dano"),
        new Linha("Cabana", "Cabana", "PV e adjacência", "+2", "Brasa", "Construcao"),
        new Linha("CampoPacifico", "Campo Pacífico", "PV máximo", "+2", "Bosque", "Vida"),
        new Linha("Santuario", "Santuário", "PV máximo", "+3", "Altura", "Vida"),
        new Linha("FonteRestauradora", "Fonte Restauradora", "Cura por coração", "+2", "Agua", "Vida"),
        new Linha("OficinaDeArqueiro", "Oficina de Arqueiro", "Limite de flechas", "+2", "Ouro", "Dano"),
    };

    [MenuItem("Loopia/Cartas/Regravar catalogo e prefabs")]
    public static void Gerar()
    {
        ImportarTexturas();
        var catalogo = ObterCatalogo();
        catalogo.moldura = Arte("T_Card_Border_Blank");
        catalogo.recorte = AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "Arte/Silhueta.png");
        catalogo.ouro = Material("Ouro", "Loopia/UI/Ouro da carta");
        catalogo.ouro.SetTexture("_Ouro", Arte("T_Rainbow_Gold"));
        EditorUtility.SetDirty(catalogo.ouro);
        catalogo.pulsoNoMapa = Material("Pulso", "Loopia/URP/Pulso de carta");
        catalogo.fonteTitulo = FonteTitulo();
        catalogo.fonteCorpo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FonteCorpo);
        catalogo.cartas.Clear();

        foreach (Linha linha in Linhas)
        {
            var def = AssetDatabase.LoadAssetAtPath<PlataformaDef>("Assets/Dados/Plataformas/" + linha.Id + ".asset");
            var objeto = Arte(linha.Id + "_Arte");
            if (def == null || objeto == null)
                throw new InvalidOperationException("Carta ou ilustracao ausente: " + linha.Id);
            catalogo.cartas.Add(new ArteDeCarta
            {
                carta = def,
                titulo = linha.Titulo,
                resumo = linha.Resumo,
                valor = linha.Valor,
                condicao = linha.Condicao,
                fundo = Arte("Ceu_" + linha.Paisagem),
                atmosfera = Arte("Atmosfera_" + linha.Paisagem),
                objeto = objeto,
                poseA = Arte(linha.Id + "_PoseA"),
                poseB = Arte(linha.Id + "_PoseB"),
                categoria = Arte("Icone_" + linha.Icone),
                destaque = Color.Lerp(new Color32(251, 209, 124, 255), def.cor, .30f),
            });
        }

        EditorUtility.SetDirty(catalogo);
        var raiz = new GameObject("Carta", typeof(RectTransform), typeof(CartaVisual));
        CartaVisual prefab;
        try
        {
            var visual = raiz.GetComponent<CartaVisual>();
            visual.Montar(catalogo);
            visual.Exibir(null, null);
            prefab = PrefabUtility.SaveAsPrefabAsset(raiz, Pasta + "Carta.prefab").GetComponent<CartaVisual>();
        }
        finally { Object.DestroyImmediate(raiz); }
        CriarHud(catalogo, prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Cartas] {catalogo.cartas.Count} apresentacoes gravadas. Shader ouro: {catalogo.ouro.shader.isSupported}.");
    }

    static void CriarHud(CatalogoVisualDeCartas catalogo, CartaVisual prefab)
    {
        var raiz = new GameObject("HUD de Cartas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(HudDeCartas));
        try
        {
            raiz.GetComponent<HudDeCartas>().Configurar(catalogo, prefab);
            PrefabUtility.SaveAsPrefabAsset(raiz, Pasta + "HUD de Cartas.prefab");
        }
        finally { Object.DestroyImmediate(raiz); }
    }

    static Texture2D Arte(string arquivo) => AssetDatabase.LoadAssetAtPath<Texture2D>(Pasta + "Arte/" + arquivo + ".png");

    static CatalogoVisualDeCartas ObterCatalogo()
    {
        const string caminho = Pasta + "Catalogo.asset";
        var existente = AssetDatabase.LoadAssetAtPath<CatalogoVisualDeCartas>(caminho);
        if (existente != null) return existente;
        var catalogo = ScriptableObject.CreateInstance<CatalogoVisualDeCartas>();
        AssetDatabase.CreateAsset(catalogo, caminho);
        return catalogo;
    }

    static Material Material(string nome, string shader)
    {
        string caminho = Pasta + nome + ".mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (existente != null) return existente;
        var material = new Material(Shader.Find(shader));
        AssetDatabase.CreateAsset(material, caminho);
        return material;
    }

    static TMP_FontAsset FonteTitulo()
    {
        const string caminho = Pasta + "Fontes/Marcellus SDF.asset";
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(caminho);
        if (existente != null) return existente;
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(Pasta + "Fontes/Marcellus-Regular.ttf");
        var fonte = TMP_FontAsset.CreateFontAsset(ttf, 72, 8, GlyphRenderMode.SDFAA,
            1024, 1024, AtlasPopulationMode.Dynamic);
        string caracteres = "";
        for (char c = ' '; c <= '~'; c++) caracteres += c;
        caracteres += "ÀÁÂÃÄÇÉÊÍÓÔÕÚÜàáâãäçéêíóôõúü–—’º";
        if (!fonte.TryAddCharacters(caracteres, out string ausentes))
            Debug.LogWarning("Glifos ausentes em Marcellus: " + ausentes);
        fonte.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(fonte, caminho);
        foreach (var atlas in fonte.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, fonte);
        AssetDatabase.AddObjectToAsset(fonte.material, fonte);
        EditorUtility.SetDirty(fonte);
        return fonte;
    }

    static void ImportarTexturas()
    {
        foreach (string caminho in Directory.GetFiles(Pasta + "Arte", "*.png"))
        {
            var importador = (TextureImporter)AssetImporter.GetAtPath(caminho.Replace('\\', '/'));
            bool recorte = Path.GetFileNameWithoutExtension(caminho) == "Silhueta";
            importador.textureType = recorte ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (recorte)
            {
                importador.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importador.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importador.SetTextureSettings(settings);
            }
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.filterMode = FilterMode.Bilinear;
            bool referencia = Path.GetFileName(caminho).StartsWith("T_", StringComparison.Ordinal);
            importador.textureCompression = referencia || recorte
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.CompressedHQ;
            importador.SaveAndReimport();
        }
    }
}
