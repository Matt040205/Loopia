// Pass B: gera (de forma reproduzivel) os atlas e materiais do FundoDePaisagem e configura o componente.
// Menu: Loopia/Paisagem/Gerar texturas e materiais  |  Loopia/Paisagem/Configurar FundoDePaisagem selecionado
// Os atlas sao copias do atlas do KayKit Forest Nature Pack (CC0) com as faixas de folhagem, tronco e pedra repintadas
// em tons de paisagem (menos saturados e mais escuros que o dressing das ilhas). Cores = previa v6 aprovada.
using System.IO;
using Loopia.Hex;
using UnityEditor;
using UnityEngine;

public static class MateriaisDaPaisagem
{
    public const string PastaKayKit = "Assets/ThirdParty/KayKit_ForestNature_Free";
    public const string Pasta = "Assets/Modelos/Paisagem";

    static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    static readonly Color32[] Pedra = { C(128, 130, 122), C(74, 78, 74) };
    static readonly Color32[] Tronco = { C(122, 94, 72), C(78, 58, 46) };

    [MenuItem("Loopia/Paisagem/Gerar texturas e materiais")]
    public static void Gerar()
    {
        Directory.CreateDirectory(Pasta + "/Texturas");
        Directory.CreateDirectory(Pasta + "/Materiais");
        Mat("Paisagem_Folhagem_Base", Atlas("atlas_folhagem_base", new[] { C(106, 130, 82), C(68, 96, 60), C(38, 64, 42) }), 0.05f);
        Mat("Paisagem_Folhagem_Oliva", Atlas("atlas_folhagem_oliva", new[] { C(118, 128, 74), C(80, 92, 52), C(46, 58, 34) }), 0.05f);
        Mat("Paisagem_Folhagem_Fria", Atlas("atlas_folhagem_fria", new[] { C(90, 122, 92), C(56, 88, 70), C(30, 56, 46) }), 0.05f);
        Mat("Paisagem_Copa_Base", Atlas("atlas_copa_base", new[] { C(78, 100, 62), C(46, 72, 46), C(24, 46, 32) }), 0.05f);
        Mat("Paisagem_Copa_Oliva", Atlas("atlas_copa_oliva", new[] { C(90, 98, 56), C(58, 68, 38), C(32, 42, 24) }), 0.05f);
        Mat("Paisagem_Copa_Fria", Atlas("atlas_copa_fria", new[] { C(66, 94, 72), C(40, 66, 52), C(20, 40, 34) }), 0.05f);
        Mat("Paisagem_Acento", Atlas("atlas_acento", new[] { C(132, 120, 80), C(104, 84, 56), C(70, 54, 40) }), 0.05f);
        Mat("Paisagem_Solo", Gradiente("gradiente_solo", new[] { C(76, 82, 64), C(44, 66, 42), C(84, 104, 62), C(110, 122, 74) }), 0f);
        Mat("Paisagem_Agua", null, 0.45f, new Color(0.22f, 0.32f, 0.37f));
        LigarLeituraDosModelos();
        AssetDatabase.SaveAssets();
        Debug.Log("[Paisagem] texturas e materiais gerados em " + Pasta);
    }

    /// <summary>CombineMeshes em runtime (build) exige malhas com Read/Write ligado.</summary>
    public static void LigarLeituraDosModelos()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { PastaKayKit + "/Modelos" }))
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (imp.isReadable) continue;
            imp.isReadable = true;
            imp.SaveAndReimport();
        }
    }

    [MenuItem("Loopia/Paisagem/Configurar FundoDePaisagem selecionado")]
    static void ConfigurarSelecionado()
    {
        var f = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<FundoDePaisagem>() : null;
        if (f == null) { Debug.LogWarning("[Paisagem] selecione um objeto com FundoDePaisagem"); return; }
        Configurar(f);
    }

    /// <summary>Mesmas listas da previa v6 (a ordem e as repeticoes importam: a semente consome o RNG por indice).</summary>
    public static void Configurar(FundoDePaisagem f)
    {
        Undo.RecordObject(f, "Configurar FundoDePaisagem");
        f.arvores = M("Tree_1_A");
        f.arvoresAltas = M("Tree_1_A");
        f.secas = M("Tree_Bare_1_A", "Tree_Bare_1_B", "Tree_Bare_2_A");
        f.copaLeve = M("Bush_1_A", "Bush_1_A", "Bush_1_A", "Bush_1_B");
        f.moitas = M("Bush_1_A", "Bush_1_B");
        f.capim = M("Grass_1_A", "Grass_2_A");
        f.pedrasPequenas = M("Rock_2_B", "Rock_2_C", "Rock_1_A", "Rock_1_D");
        f.pedrasMedias = M("Rock_2_D", "Rock_2_E", "Rock_2_G");
        f.folhas = new[] { L("Paisagem_Folhagem_Base"), L("Paisagem_Folhagem_Oliva"), L("Paisagem_Folhagem_Fria") };
        f.copas = new[] { L("Paisagem_Copa_Base"), L("Paisagem_Copa_Oliva"), L("Paisagem_Copa_Fria") };
        f.acento = L("Paisagem_Acento");
        f.solo = L("Paisagem_Solo");
        f.agua = L("Paisagem_Agua");
        EditorUtility.SetDirty(f);
    }

    static GameObject[] M(params string[] nomes)
    {
        var r = new GameObject[nomes.Length];
        for (int i = 0; i < nomes.Length; i++)
        {
            r[i] = AssetDatabase.LoadAssetAtPath<GameObject>(PastaKayKit + "/Modelos/" + nomes[i] + "_Color1.fbx");
            if (r[i] == null) throw new System.Exception("KayKit nao encontrado: " + nomes[i]);
        }
        return r;
    }

    static Material L(string nome)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(Pasta + "/Materiais/" + nome + ".mat");
        if (m == null) throw new System.Exception("Material de paisagem ausente (rode Loopia/Paisagem/Gerar texturas e materiais): " + nome);
        return m;
    }

    static Texture2D Atlas(string nome, Color32[] folha)
    {
        var src = new Texture2D(2, 2);
        src.LoadImage(File.ReadAllBytes(PastaKayKit + "/Texturas/forest_texture.png"));
        int w = src.width, h = src.height;
        var px = src.GetPixels32();
        for (int y = 0; y < 768; y++)
        {
            float t = (y % 256) / 255f;
            Color32 cor = y < 256 ? Tres(folha, t) : y < 512 ? Color32.Lerp(Tronco[0], Tronco[1], t) : Color32.Lerp(Pedra[0], Pedra[1], t);
            int linha = h - 1 - y;
            for (int x = 0; x < 128; x++) px[linha * w + x] = cor;
        }
        src.SetPixels32(px); src.Apply();
        var r = Salvar(nome, src.EncodeToPNG(), FilterMode.Bilinear);
        Object.DestroyImmediate(src);
        return r;
    }

    static Color32 Tres(Color32[] c, float t) => t < 0.5f ? Color32.Lerp(c[0], c[1], t * 2f) : Color32.Lerp(c[1], c[2], (t - 0.5f) * 2f);

    static Texture2D Gradiente(string nome, Color32[] faixas)
    {
        var tex = new Texture2D(4, 256);
        for (int y = 0; y < 256; y++)
        {
            float t = y / 255f;
            Color32 cor = t < 0.12f ? faixas[0]
                : t < 0.6f ? Color32.Lerp(faixas[1], faixas[2], (t - 0.12f) / 0.48f)
                : Color32.Lerp(faixas[2], faixas[3], (t - 0.6f) / 0.4f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, cor);
        }
        tex.Apply();
        var r = Salvar(nome, tex.EncodeToPNG(), FilterMode.Point);
        Object.DestroyImmediate(tex);
        return r;
    }

    static Texture2D Salvar(string nome, byte[] png, FilterMode filtro)
    {
        var p = Pasta + "/Texturas/" + nome + ".png";
        File.WriteAllBytes(p, png);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = filtro; imp.sRGBTexture = true; imp.mipmapEnabled = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static void Mat(string nome, Texture2D tex, float suavidade, Color? cor = null)
    {
        var p = Pasta + "/Materiais/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", cor ?? Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Smoothness", suavidade);
        m.SetFloat("_EnvironmentReflections", suavidade > 0.5f ? 1f : 0f);
        m.SetFloat("_SpecularHighlights", suavidade > 0.5f ? 1f : 0f);
        m.enableInstancing = false;
        EditorUtility.SetDirty(m);
    }
}
