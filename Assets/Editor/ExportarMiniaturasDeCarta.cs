using System.IO;
using Loopia.Hex;
using UnityEditor;
using UnityEngine;

public static class ExportarMiniaturasDeCarta
{
    [MenuItem("Loopia/Cartas/Exportar miniaturas ortograficas")]
    public static void Renderizar()
    {
        string destino = System.Environment.GetEnvironmentVariable("LOOPIA_CARD_QA") ??
            Path.Combine(Path.GetTempPath(), "Loopia-Cards-QA");
        Directory.CreateDirectory(destino);
        string nome = System.Environment.GetEnvironmentVariable("LOOPIA_CARD_ART") ?? "Tudo";
        foreach (string guid in AssetDatabase.FindAssets("t:PlataformaDef", new[] { "Assets/Dados/Plataformas" }))
        {
            var def = AssetDatabase.LoadAssetAtPath<PlataformaDef>(AssetDatabase.GUIDToAssetPath(guid));
            if (nome != "Tudo" && def.name != nome) continue;
            Renderizar(def, Path.Combine(destino, def.name + ".png"));
        }
    }

    static void Renderizar(PlataformaDef def, string destino)
    {
        var previa = new PreviewRenderUtility();
        var raiz = new GameObject(def.name);
        var materiais = new System.Collections.Generic.List<Material>();
        var malhas = new System.Collections.Generic.List<Mesh>();
        try
        {
            previa.AddSingleGO(raiz);
            if (def.name == "Flechas")
            {
                new MiniaturasDeCarta(raiz.transform, materiais, malhas).Aljava(Vector3.zero, 1f, def.modelo);
            }
            else if (def.modelo != null)
            {
                var objeto = Object.Instantiate(def.modelo, raiz.transform);
                Bounds limites = VisualDeIlha.LimitesLocais(objeto.transform);
                objeto.transform.localPosition -= Vector3.up * limites.min.y;
                objeto.transform.localRotation = Quaternion.Euler(0f, def.name == "Coracao" ? 26f : 15f, 0f);
                if (def.name == "Moedas") AdicionarMoedas(objeto, limites, raiz.transform);
            }
            else if (def.marcador != null)
            {
                Object.Instantiate(def.marcador, raiz.transform);
                float raio = def.name == "Torre" ? 1.19f : 1.94f;
                bool alta = def.name == "IlhaAlta";
                AddIlha(raiz.transform, raio, alta ? 1.64f : .47f, materiais, malhas, def.name);
                if (def.name == "Floresta") AdicionarLobo(raiz.transform, malhas);
            }
            else
            {
                new MiniaturasDeCarta(raiz.transform, materiais, malhas).Montar(def.name);
            }
            var camera = previa.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 70f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            Vector3 dir = new Vector3(-.65f, .53f, -.74f).normalized;
            camera.transform.rotation = Quaternion.LookRotation(-dir);
            var limite = new Bounds();
            bool primeiro = true;
            foreach (var renderer in raiz.GetComponentsInChildren<Renderer>())
            {
                Bounds b = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 ponto = b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 naCamera = Quaternion.Inverse(camera.transform.rotation) * ponto;
                    if (primeiro) { limite = new Bounds(naCamera, Vector3.zero); primeiro = false; }
                    else limite.Encapsulate(naCamera);
                }
            }
            camera.transform.position = camera.transform.rotation * limite.center + dir * 15f;
            camera.orthographicSize = Mathf.Max(limite.extents.x, limite.extents.y) * 1.07f;
            previa.lights[0].intensity = 2.2f;
            previa.lights[0].color = new Color(1f,.90f,.73f);
            previa.lights[0].transform.rotation = Quaternion.LookRotation(new Vector3(.39f, -.83f, .63f));
            previa.lights[1].intensity = .99f;
            previa.lights[1].color = new Color(.70f,.84f,1f);
            previa.lights[1].transform.rotation = Quaternion.LookRotation(new Vector3(-.7f, -.47f, .62f));
            previa.ambientColor = new Color(.51f,.56f,.64f);
            previa.BeginPreview(new Rect(0, 0, 896, 896), GUIStyle.none);
            previa.Render(true, false);
            var rt = (RenderTexture)previa.EndPreview();
            var antes = RenderTexture.active;
            RenderTexture.active = rt;
            var png = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            png.Apply();
            File.WriteAllBytes(destino, png.EncodeToPNG());
            RenderTexture.active = antes;
            Object.DestroyImmediate(png);
            Debug.Log($"[Cartas/Arte] {destino}");
        }
        finally
        {
            previa.Cleanup();
            foreach (var mat in materiais) Object.DestroyImmediate(mat);
            foreach (var mesh in malhas) Object.DestroyImmediate(mesh);
        }
    }

    // Sprint 5B: a Floresta "faz nascer lobos" — a ilustracao mostra o lobo de verdade (Wolf_Common na pose do Wolf_Idle),
    // na frente das arvores (que ficam no lado longe da camera), voltado de tres quartos para quem olha a carta.
    public static readonly Vector3 PosicaoDoLobo = new Vector3(-.05f, 0f, -.55f);
    public const float TamanhoDoLobo = 1.75f;

    static void AdicionarLobo(Transform raiz, System.Collections.Generic.List<Mesh> malhas)
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Inimigos/Wolf_Common/Wolf_Common.fbx");
        if (modelo == null) return;
        // Pose do Wolf_Idle assada numa malha estatica: o PreviewRenderUtility nao desenha SkinnedMeshRenderer.
        var instancia = Object.Instantiate(modelo);
        instancia.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            foreach (var clipe in AssetDatabase.LoadAllAssetsAtPath("Assets/Modelos/Inimigos/Wolf_Common/Wolf_Common_Animacoes.fbx"))
                if (clipe is AnimationClip c && c.name == "Wolf_Idle") c.SampleAnimation(instancia, .4f);
            var smr = instancia.GetComponentInChildren<SkinnedMeshRenderer>();
            var malha = new Mesh { name = "Lobo da floresta" };
            smr.BakeMesh(malha, true);
            malhas.Add(malha);
            var lobo = new GameObject("Lobo da floresta");
            lobo.transform.SetParent(raiz, false);
            var corpo = new GameObject("Corpo");
            corpo.transform.SetParent(lobo.transform, false);
            corpo.transform.localPosition = instancia.transform.InverseTransformPoint(smr.transform.position);
            corpo.transform.localRotation = Quaternion.Inverse(instancia.transform.rotation) * smr.transform.rotation;
            corpo.AddComponent<MeshFilter>().sharedMesh = malha;
            corpo.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
            Bounds b = VisualDeIlha.LimitesLocais(lobo.transform);
            float escala = TamanhoDoLobo / Mathf.Max(b.size.x, b.size.y, b.size.z);
            lobo.transform.localScale = Vector3.one * escala;
            lobo.transform.localPosition = PosicaoDoLobo;
            lobo.transform.localRotation = Quaternion.LookRotation(new Vector3(-.21f, 0f, -1.13f), Vector3.up);
        }
        finally
        {
            Object.DestroyImmediate(instancia);
        }
    }

    static void AdicionarMoedas(GameObject primeira, Bounds limites, Transform raiz)
    {
        float diametro = Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
        var segunda = Object.Instantiate(primeira, raiz);
        segunda.transform.localPosition += new Vector3(.62f, .60f, .20f) * diametro;
        segunda.transform.localRotation = Quaternion.Euler(-13f, 34f, -13f);
        segunda.transform.localScale *= .84f;
        var terceira = Object.Instantiate(primeira, raiz);
        terceira.transform.localPosition += new Vector3(.55f, .05f, -.63f) * diametro;
        terceira.transform.localRotation = Quaternion.Euler(58f, 0f, 40f);
        terceira.transform.localScale *= .78f;
    }

    static void AddIlha(Transform raiz, float raio, float altura,
        System.Collections.Generic.List<Material> materiais, System.Collections.Generic.List<Mesh> malhas, string carta)
    {
        bool alta = carta == "IlhaAlta";
        var topo = new Color32(144, 166, 97,255);
        if (carta == "Floresta") topo = new Color32(114, 145, 92, 255);
        if (carta == "CampoPacifico") topo = new Color32(161, 172, 112, 255);
        var cores = new Color[] { topo, new Color32(93,92,83,255), new Color32(135,113,81,255),
            new Color32(102,99,85,255), new Color32(114,95,81,255), new Color32(92,111,79,255) };
        foreach (var cor in cores)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = cor;
            material.SetFloat("_Smoothness", .13f);
            materiais.Add(material);
        }
        var vert = new System.Collections.Generic.List<Vector3>();
        var faces = new System.Collections.Generic.List<int>[materiais.Count];
        for (int i=0; i < faces.Length; i++) faces[i] = new System.Collections.Generic.List<int>();
        Vector3 Anel(int i, float r, float y)
        {
            float a = Mathf.Deg2Rad * (30 + i * 60);
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }
        void Tri(int mat, Vector3 a, Vector3 b, Vector3 c)
        {
            faces[mat].Add(vert.Count);
            faces[mat].Add(vert.Count+1);
            faces[mat].Add(vert.Count+2);
            vert.Add(a); vert.Add(b); vert.Add(c);
        }
        for (int i = 0; i < 6; i++)
        {
            Vector3 a = Anel(i, raio, -.035f), b = Anel(i + 1, raio, -.035f);
            Vector3 c = Anel(i, raio * .97f, -.13f), d = Anel(i+1, raio * .97f, -.13f);
            Vector3 e = Anel(i, raio * (.61f + .065f * (i % 3)), -altura * (.80f + .075f * (i % 3)));
            Vector3 f = Anel((i+1)%6, raio * (.61f + .065f * ((i+1)%6 % 3)), -altura * (.80f + .075f * ((i+1)%6 % 3)));
            Tri(0, Vector3.up * -.032f, b, a);
            Tri(5, a, b, d); Tri(5, a, d, c);
            Tri(1 + i % 4, c, d, f); Tri(1 + (i+1)%4, c, f, e);
            Tri(1, e, f, Vector3.up * -altura * (alta ? 1.06f : 1.2f));
        }
        var mesh = new Mesh();
        mesh.SetVertices(vert);
        mesh.subMeshCount = faces.Length;
        for (int i=0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
        mesh.RecalculateNormals();
        malhas.Add(mesh);
        var obj = new GameObject("Ilha de ilustracao");
        obj.transform.SetParent(raiz, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterials = materiais.ToArray();
    }
}
