// Pass C: gera (de forma reproduzivel) os marcadores das 7 cartas sem modelo e os prefabs de vegetacao baixa,
// e configura as cartas (PlataformaDef.marcador) e o perfil de escala (vegetacaoBaixa).
// Menu: Loopia/Pass C/Gerar marcadores e vegetacao
//
// Marcadores em escala de mundo (1 u = altura do Lucca; ilha de raio 2,1), pivo no centro/topo da ilha, eixos do mundo.
// Cartas do CAMINHO (Basica, Floresta, Ilha alta): no centro so coisa baixa (<= 0,1 u); o que e alto fica nos vertices do
// lado LONGE da camera (a camera olha para +X+Z), entao nunca cobre o Lucca. Cartas FORA do caminho (Torre, Cabana,
// Campo Pacifico, Area de Treino): "construcao" no centro, 1,0-2,4 u (Scene Bible: 1,5-2,5 para torre/cabana).
// Pano/detalhe na cor da carta (cor de gameplay, fora da paleta ambiental); resto em madeira/pedra/palha.
using System.Collections.Generic;
using System.IO;
using Loopia.Hex;
using UnityEditor;
using UnityEngine;

public static class MarcadoresDeCarta
{
    const string PastaMarcadores = "Assets/Modelos/Marcadores";
    const string PastaDressing = "Assets/Modelos/Dressing";
    const string PastaCartas = "Assets/Dados/Plataformas";
    const string Perfil = "Assets/Dados/Arte/EscalaDeArte.asset";
    const string KayKit = "Assets/ThirdParty/KayKit_ForestNature_Free";

    static readonly Vector3 Direita = new Vector3(0.70710678f, 0f, -0.70710678f); // direita da tela no chao
    static readonly Vector3 Fundo = new Vector3(0.70710678f, 0f, 0.70710678f);    // para longe da camera
    static readonly Vector3 ParaACamera = new Vector3(-0.6123724f, 0.5f, -0.6123724f);

    static Material _madeira, _pedra, _laje, _terra, _palha, _branco, _flor, _florAmarela, _dressing, _vegetacao;

    [MenuItem("Loopia/Pass C/Gerar marcadores e vegetacao")]
    public static void Gerar()
    {
        Directory.CreateDirectory(PastaMarcadores + "/Materiais");
        Directory.CreateDirectory(PastaDressing + "/Materiais");
        _madeira = Mat(PastaMarcadores, "Madeira", new Color(0.42f, 0.28f, 0.17f));
        _pedra = Mat(PastaMarcadores, "Pedra", new Color(0.60f, 0.60f, 0.62f));
        _laje = Mat(PastaMarcadores, "Laje", new Color(0.42f, 0.41f, 0.38f)); // escura: nao pode ser o ponto mais claro sob o Lucca
        _terra = Mat(PastaMarcadores, "Terra", new Color(0.34f, 0.23f, 0.15f));
        _palha = Mat(PastaMarcadores, "Palha", new Color(0.86f, 0.74f, 0.42f));
        _branco = Mat(PastaMarcadores, "Branco", new Color(0.90f, 0.88f, 0.82f));
        _flor = Mat(PastaDressing, "Flor_Creme", new Color(0.95f, 0.92f, 0.78f));
        _florAmarela = Mat(PastaDressing, "Flor_Amarela", new Color(0.95f, 0.84f, 0.45f));
        _dressing = Mat(PastaDressing, "Dressing_KayKit", Color.white,
            AssetDatabase.LoadAssetAtPath<Texture2D>(KayKit + "/Texturas/forest_texture.png"));
        // vegetacao baixa: verde mais fundo que o topo das ilhas (o atlas original se camuflava no verde-limao do topo)
        _vegetacao = Mat(PastaDressing, "Dressing_Vegetacao", Color.white, AtlasDeVegetacao());

        var cartas = new Dictionary<string, PlataformaDef>();
        foreach (var nome in new[] { "Basica", "Floresta", "IlhaAlta", "Torre", "Cabana", "CampoPacifico", "AreaDeTreino" })
            cartas[nome] = AssetDatabase.LoadAssetAtPath<PlataformaDef>(PastaCartas + "/" + nome + ".asset");

        Atribuir(cartas["Basica"], Basica(cartas["Basica"]), false);
        Atribuir(cartas["Floresta"], Floresta(cartas["Floresta"]), true);
        Atribuir(cartas["IlhaAlta"], IlhaAlta(cartas["IlhaAlta"]), false);
        Atribuir(cartas["Torre"], Torre(cartas["Torre"]), false);
        Atribuir(cartas["Cabana"], Cabana(cartas["Cabana"]), false);
        Atribuir(cartas["CampoPacifico"], Campo(cartas["CampoPacifico"]), true); // as leiras ocupam a ilha: a decoracao cobria
        Atribuir(cartas["AreaDeTreino"], Treino(cartas["AreaDeTreino"]), false);

        ConfigurarPerfil();
        AssetDatabase.SaveAssets();
        Debug.Log("[Pass C] marcadores, vegetacao baixa, cartas e perfil configurados");
    }

    // ---------------- cartas do caminho ----------------

    static GameObject Basica(PlataformaDef carta)
    {
        var r = Raiz("Marcador_Basica");
        // laje baixa no centro (o Lucca pisa nela) + seis pedrinhas nos vertices
        Prim(PrimitiveType.Cylinder, r, new Vector3(0, 0.02f, 0), new Vector3(0.95f, 0.02f, 0.95f), Quaternion.identity, _laje, "Laje");
        var pedra = KK("Rock_2_B");
        int k = 0;
        foreach (var v in Vertices())
            Modelo(pedra, r, v * 1.2f, 0.2f, EixoDeMedida.Altura, 40f * k++, _dressing);
        return Salvar(r, PastaMarcadores + "/Marcador_Basica.prefab");
    }

    static GameObject Floresta(PlataformaDef carta)
    {
        var r = Raiz("Marcador_Floresta");
        // vertices ordenados pelo cosseno com o "fundo": 0,97 / 0,71 / 0,26 (longe) e -0,26 / -0,71 / -0,97 (perto da camera)
        var v = Vertices();
        Modelo(KK("Tree_1_A"), r, v[0] * 1.35f, 1.9f, EixoDeMedida.Altura, 20f, _dressing);
        Modelo(KK("Tree_3_A"), r, v[1] * 1.45f, 1.5f, EixoDeMedida.Altura, 110f, _dressing);
        Modelo(KK("Tree_1_A"), r, v[2] * 1.5f, 1.2f, EixoDeMedida.Altura, 200f, _dressing);
        Modelo(KK("Bush_1_C"), r, (v[1] + v[2]).normalized * 1.6f, 0.5f, EixoDeMedida.Altura, 30f, _dressing);
        // lado da camera: so coisa baixa (nao cobre o Lucca nem o lobo)
        Modelo(KK("Bush_1_A"), r, v[3] * 1.55f, 0.28f, EixoDeMedida.Altura, 70f, _dressing);
        Modelo(TufoDeCapim(), r, v[4] * 1.5f, 0.22f, EixoDeMedida.Altura, 0f, null);
        return Salvar(r, PastaMarcadores + "/Marcador_Floresta.prefab");
    }

    static GameObject IlhaAlta(PlataformaDef carta)
    {
        var r = Raiz("Marcador_IlhaAlta");
        var v = Vertices();
        var cor = Mat(PastaMarcadores, "Pano_IlhaAlta", carta.cor);
        // mojao de pedras (lugar alto) e bandeirola, os dois no lado longe
        Vector3 m = v[0] * 1.45f;
        Modelo(KK("Rock_2_D"), r, m, 0.58f, EixoDeMedida.Altura, 0f, _dressing);
        Modelo(KK("Rock_2_B"), r, m + Vector3.up * 0.5f, 0.4f, EixoDeMedida.Altura, 50f, _dressing);
        Modelo(KK("Rock_1_A"), r, m + Vector3.up * 0.84f, 0.26f, EixoDeMedida.Altura, 120f, _dressing);
        Bandeira(r, v[1] * 1.4f, 1.45f, cor);
        Modelo(TufoDeCapim(), r, v[2] * 1.5f, 0.2f, EixoDeMedida.Altura, 0f, null);
        Modelo(TufoDeCapim(), r, v[3] * 1.5f, 0.18f, EixoDeMedida.Altura, 90f, null);
        return Salvar(r, PastaMarcadores + "/Marcador_IlhaAlta.prefab");
    }

    // ---------------- cartas fora do caminho ----------------

    static GameObject Torre(PlataformaDef carta)
    {
        var r = Raiz("Marcador_Torre");
        var cor = Mat(PastaMarcadores, "Pano_Torre", carta.cor);
        var torre = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Cenário/Torre.obj");
        var t = Modelo(torre, r, Vector3.zero, 2.0f, EixoDeMedida.Altura, 0f, _pedra);
        float topo = VisualDeIlha.LimitesLocais(t.transform).max.y;
        Bandeira(r, Vector3.zero + Vector3.up * topo, 0.4f, cor);
        return Salvar(r, PastaMarcadores + "/Marcador_Torre.prefab");
    }

    static GameObject Cabana(PlataformaDef carta)
    {
        var r = Raiz("Marcador_Cabana");
        var cor = Mat(PastaMarcadores, "Pano_Cabana", carta.cor);
        var barraca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/HexPrototype/BarracaArmada.prefab");
        Modelo(barraca, r, Fundo * 0.15f, 1.5f, EixoDeMedida.MaiorEixo, 225f, null);
        Prim(PrimitiveType.Cube, r, Direita * 0.9f - Fundo * 0.35f + Vector3.up * 0.13f, Vector3.one * 0.26f, Quaternion.LookRotation(Fundo), _madeira, "Caixote");
        var lenha = Quaternion.FromToRotation(Vector3.up, Direita);
        Prim(PrimitiveType.Cylinder, r, -Direita * 0.85f - Fundo * 0.3f + Vector3.up * 0.06f, new Vector3(0.12f, 0.2f, 0.12f), lenha, _madeira, "Lenha");
        Prim(PrimitiveType.Cylinder, r, -Direita * 0.85f - Fundo * 0.16f + Vector3.up * 0.06f, new Vector3(0.12f, 0.2f, 0.12f), lenha, _madeira, "Lenha");
        Bandeira(r, Vertices()[1] * 1.35f, 1.1f, cor);
        return Salvar(r, PastaMarcadores + "/Marcador_Cabana.prefab");
    }

    static GameObject Campo(PlataformaDef carta)
    {
        var r = Raiz("Marcador_CampoPacifico");
        var cor = Mat(PastaMarcadores, "Pano_CampoPacifico", carta.cor);
        var broto = KK("Grass_1_A");
        var giro = Quaternion.LookRotation(Fundo); // x local = direita da tela
        for (int i = 0; i < 4; i++)
        {
            Vector3 c = Fundo * (-0.75f + i * 0.5f) - Direita * 0.15f;
            Prim(PrimitiveType.Cube, r, c + Vector3.up * 0.04f, new Vector3(1.7f, 0.08f, 0.24f), giro, _terra, "Leira");
            for (int j = 0; j < 5; j++)
                Modelo(broto, r, c + Direita * (-0.64f + j * 0.32f) + Vector3.up * 0.08f, 0.17f, EixoDeMedida.Altura, 37f * (i + j), _dressing);
        }
        // espantalho no canto longe
        Vector3 e = Fundo * 0.95f + Direita * 1.0f;
        Prim(PrimitiveType.Cylinder, r, e + Vector3.up * 0.5f, new Vector3(0.07f, 0.5f, 0.07f), Quaternion.identity, _madeira, "Poste");
        Prim(PrimitiveType.Cylinder, r, e + Vector3.up * 0.74f, new Vector3(0.05f, 0.3f, 0.05f), Quaternion.FromToRotation(Vector3.up, Direita), _madeira, "Bracos");
        Prim(PrimitiveType.Cube, r, e + Vector3.up * 0.62f, new Vector3(0.26f, 0.32f, 0.1f), giro, cor, "Roupa");
        Prim(PrimitiveType.Sphere, r, e + Vector3.up * 0.98f, Vector3.one * 0.2f, Quaternion.identity, _palha, "Cabeca");
        Prim(PrimitiveType.Cylinder, r, e + Vector3.up * 1.08f, new Vector3(0.3f, 0.01f, 0.3f), Quaternion.identity, _palha, "Aba");
        Prim(PrimitiveType.Cylinder, r, e + Vector3.up * 1.13f, new Vector3(0.15f, 0.05f, 0.15f), Quaternion.identity, _palha, "Copa");
        return Salvar(r, PastaMarcadores + "/Marcador_CampoPacifico.prefab");
    }

    static GameObject Treino(PlataformaDef carta)
    {
        var r = Raiz("Marcador_AreaDeTreino");
        var cor = Mat(PastaMarcadores, "Pano_AreaDeTreino", carta.cor);
        Alvo(r, Fundo * 0.9f - Direita * 0.6f, cor);
        Alvo(r, Fundo * 0.55f + Direita * 0.75f, cor);
        // boneco de palha
        Vector3 b = -Fundo * 0.35f - Direita * 0.35f;
        Prim(PrimitiveType.Cylinder, r, b + Vector3.up * 0.3f, new Vector3(0.06f, 0.3f, 0.06f), Quaternion.identity, _madeira, "Poste");
        Prim(PrimitiveType.Capsule, r, b + Vector3.up * 0.72f, new Vector3(0.26f, 0.22f, 0.26f), Quaternion.identity, _palha, "Corpo");
        Prim(PrimitiveType.Cylinder, r, b + Vector3.up * 0.8f, new Vector3(0.06f, 0.24f, 0.06f), Quaternion.FromToRotation(Vector3.up, Direita), _palha, "Bracos");
        Prim(PrimitiveType.Sphere, r, b + Vector3.up * 1.05f, Vector3.one * 0.19f, Quaternion.identity, _palha, "Cabeca");
        // fardo de feno deitado
        Prim(PrimitiveType.Cylinder, r, Direita * 0.6f - Fundo * 0.5f + Vector3.up * 0.21f, new Vector3(0.36f, 0.24f, 0.36f),
            Quaternion.FromToRotation(Vector3.up, Direita), _palha, "Feno");
        return Salvar(r, PastaMarcadores + "/Marcador_AreaDeTreino.prefab");
    }

    static void Alvo(Transform r, Vector3 p, Material cor)
    {
        Prim(PrimitiveType.Cylinder, r, p + Vector3.up * 0.42f, new Vector3(0.06f, 0.42f, 0.06f), Quaternion.identity, _madeira, "Suporte");
        var eixo = ParaACamera.normalized;
        var giro = Quaternion.FromToRotation(Vector3.up, eixo);
        Vector3 c = p + Vector3.up * 0.86f + eixo * 0.04f;
        Prim(PrimitiveType.Cylinder, r, c, new Vector3(0.62f, 0.02f, 0.62f), giro, _branco, "Alvo");
        Prim(PrimitiveType.Cylinder, r, c + eixo * 0.012f, new Vector3(0.42f, 0.02f, 0.42f), giro, cor, "Anel");
        Prim(PrimitiveType.Cylinder, r, c + eixo * 0.024f, new Vector3(0.22f, 0.02f, 0.22f), giro, _branco, "Anel");
        Prim(PrimitiveType.Cylinder, r, c + eixo * 0.036f, new Vector3(0.09f, 0.02f, 0.09f), giro, cor, "Centro");
    }

    static void Bandeira(Transform r, Vector3 p, float altura, Material cor)
    {
        Prim(PrimitiveType.Cylinder, r, p + Vector3.up * altura * 0.5f, new Vector3(0.05f, altura * 0.5f, 0.05f), Quaternion.identity, _madeira, "Mastro");
        Prim(PrimitiveType.Cube, r, p + Vector3.up * (altura - 0.15f) + Direita * 0.21f, new Vector3(0.4f, 0.26f, 0.02f),
            Quaternion.LookRotation(Fundo), cor, "Pano");
    }

    // ---------------- vegetacao baixa (C2) ----------------

    static GameObject _tufo;
    static GameObject TufoDeCapim()
    {
        if (_tufo != null && AssetDatabase.Contains(_tufo)) return _tufo;
        var r = Raiz("Tufo_Capim");
        var a = KK("Grass_1_A"); var b = KK("Grass_2_A");
        Modelo(a, r, Vector3.zero, 0.56f, EixoDeMedida.Altura, 0f, _vegetacao);
        Modelo(b, r, new Vector3(0.18f, 0, 0.1f), 0.7f, EixoDeMedida.Altura, 60f, _vegetacao);
        Modelo(a, r, new Vector3(-0.12f, 0, 0.16f), 0.45f, EixoDeMedida.Altura, 140f, _vegetacao);
        _tufo = Salvar(r, PastaDressing + "/Tufo_Capim.prefab");
        return _tufo;
    }

    static GameObject TufoDeFlores()
    {
        var r = Raiz("Tufo_Flores");
        var b = KK("Grass_2_A");
        Modelo(b, r, Vector3.zero, 0.7f, EixoDeMedida.Altura, 0f, _vegetacao);
        Modelo(b, r, new Vector3(0.2f, 0, 0.08f), 0.55f, EixoDeMedida.Altura, 90f, _vegetacao);
        var flores = new[] { new Vector3(0.02f, 0.72f, 0.03f), new Vector3(0.2f, 0.58f, 0.1f), new Vector3(-0.1f, 0.6f, -0.06f), new Vector3(0.12f, 0.66f, -0.1f) };
        for (int i = 0; i < flores.Length; i++)
            Prim(PrimitiveType.Cube, r, flores[i], Vector3.one * 0.09f, Quaternion.Euler(45f, 45f, 0f), i % 2 == 0 ? _flor : _florAmarela, "Flor"); // cubo (12 tris), nao esfera (768)
        return Salvar(r, PastaDressing + "/Tufo_Flores.prefab");
    }

    static GameObject Moita(string kaykit)
    {
        var r = Raiz("Moita_" + kaykit);
        var m = KK(kaykit);
        float h = VisualDeIlha.LimitesLocais(m.transform).size.y; // medida nativa (o perfil normaliza depois)
        Modelo(m, r, Vector3.zero, h, EixoDeMedida.Altura, 0f, _vegetacao);
        return Salvar(r, PastaDressing + "/Moita_" + kaykit + ".prefab");
    }

    static void ConfigurarPerfil()
    {
        var p = AssetDatabase.LoadAssetAtPath<ArtScaleProfile>(Perfil);
        Undo.RecordObject(p, "Pass C: vegetacao baixa");
        p.vegetacaoBaixa = new List<ModeloDeArte>
        {
            new ModeloDeArte { prefab = Moita("Bush_1_A"), categoria = CategoriaDeArte.Arbusto, peso = 1.4f },
            new ModeloDeArte { prefab = Moita("Bush_1_B"), categoria = CategoriaDeArte.Arbusto, peso = 1.2f },
            new ModeloDeArte { prefab = Moita("Bush_3_A"), categoria = CategoriaDeArte.Arbusto, peso = 0.8f },
            new ModeloDeArte { prefab = TufoDeCapim(), categoria = CategoriaDeArte.Capim, peso = 1.2f },
            new ModeloDeArte { prefab = TufoDeFlores(), categoria = CategoriaDeArte.Capim, peso = 1.0f },
        };
        // arbusto com pegada cheia nao cabia entre os corredores (6 de 83 pecas eram arbustos): mesma ideia das arvores
        if (p.TryRegra(CategoriaDeArte.Arbusto, out RegraDeCategoria arbusto)) arbusto.fracaoDaPegada = 0.5f;
        p.vegetacaoMinima = 3;
        p.vegetacaoMaxima = 8;   // 4 da decoracao principal + 8 = 12 por ilha no maximo (plano do Pass C)
        p.variacaoDaMistura = 0.6f;
        EditorUtility.SetDirty(p);
    }

    static Texture2D AtlasDeVegetacao()
    {
        var src = new Texture2D(2, 2);
        src.LoadImage(File.ReadAllBytes(KayKit + "/Texturas/forest_texture.png"));
        int w = src.width, h = src.height;
        var px = src.GetPixels32();
        var c0 = new Color32(92, 158, 58, 255); var c1 = new Color32(50, 116, 46, 255); var c2 = new Color32(24, 72, 36, 255);
        for (int y = 0; y < 256; y++)
        {
            float t = y / 255f;
            Color32 cor = t < 0.5f ? Color32.Lerp(c0, c1, t * 2f) : Color32.Lerp(c1, c2, (t - 0.5f) * 2f);
            for (int x = 0; x < 128; x++) px[(h - 1 - y) * w + x] = cor;
        }
        src.SetPixels32(px); src.Apply();
        var p = PastaDressing + "/Materiais/atlas_vegetacao.png";
        File.WriteAllBytes(p, src.EncodeToPNG());
        Object.DestroyImmediate(src);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(p);
        imp.wrapMode = TextureWrapMode.Clamp; imp.sRGBTexture = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    // ---------------- utilitarios ----------------

    static void Atribuir(PlataformaDef carta, GameObject prefab, bool ocultarDecoracao)
    {
        Undo.RecordObject(carta, "Pass C: marcador");
        carta.marcador = prefab;
        carta.ocultarDecoracao = ocultarDecoracao;
        EditorUtility.SetDirty(carta);
    }

    /// <summary>Direcoes dos 6 vertices do hexagono (entre os corredores), da mais longe da camera para a mais perto.</summary>
    static List<Vector3> Vertices()
    {
        var lista = new List<Vector3>();
        for (int k = 0; k < 6; k++)
        {
            Vector3 corredor = new HexLayout(1f).ToWorld(new HexCoord(0, 0).Neighbor(k)).normalized;
            lista.Add(Quaternion.Euler(0f, 30f, 0f) * corredor);
        }
        lista.Sort((a, b) => Vector3.Dot(b, Fundo).CompareTo(Vector3.Dot(a, Fundo)));
        return lista;
    }

    static Transform Raiz(string nome) => new GameObject(nome).transform;

    static GameObject KK(string nome)
    {
        var g = AssetDatabase.LoadAssetAtPath<GameObject>(KayKit + "/Modelos/" + nome + "_Color1.fbx");
        if (g == null) throw new System.Exception("KayKit nao encontrado: " + nome);
        return g;
    }

    static GameObject Prim(PrimitiveType tipo, Transform pai, Vector3 pos, Vector3 escala, Quaternion giro, Material mat, string nome)
    {
        var g = GameObject.CreatePrimitive(tipo);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = nome;
        g.transform.SetParent(pai, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = giro;
        g.transform.localScale = escala;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    /// <summary>Instancia (aninhado) com a base em pos.y e o centro XZ em pos, medido pelo eixo pedido.</summary>
    static GameObject Modelo(GameObject origem, Transform pai, Vector3 pos, float tamanho, EixoDeMedida eixo, float giroY, Material mat)
    {
        var suporte = new GameObject(origem.name).transform;
        suporte.SetParent(pai, false);
        suporte.localPosition = pos;
        suporte.localRotation = Quaternion.Euler(0f, giroY, 0f);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(origem, suporte);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        var escala0 = inst.transform.localScale;
        Bounds b = VisualDeIlha.LimitesLocais(suporte);
        float s = tamanho / Mathf.Max(0.0001f, VisualDeIlha.Medida(b.size, false, eixo));
        inst.transform.localScale = escala0 * s;
        inst.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z) * s;
        if (mat != null)
            foreach (var rr in inst.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[rr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                rr.sharedMaterials = mats;
            }
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        return suporte.gameObject;
    }

    static GameObject Salvar(Transform raiz, string caminho)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz.gameObject, caminho);
        Object.DestroyImmediate(raiz.gameObject);
        return prefab;
    }

    static Material Mat(string pasta, string nome, Color cor, Texture2D tex = null)
    {
        var p = pasta + "/Materiais/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", cor);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Smoothness", 0.1f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }
}
