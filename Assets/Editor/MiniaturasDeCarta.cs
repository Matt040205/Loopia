using System.Collections.Generic;
using UnityEngine;

internal sealed class MiniaturasDeCarta
{
    readonly List<Material> _temporarios;
    readonly List<Mesh> _malhas;
    readonly Transform _raiz;
    readonly Material _pedra;
    readonly Material _creme;
    readonly Material _madeira;
    readonly Material _couro;
    readonly Material _metal;
    readonly Material _agua;

    public MiniaturasDeCarta(Transform raiz, List<Material> materiais, List<Mesh> malhas)
    {
        _raiz = raiz;
        _temporarios = materiais;
        _malhas = malhas;
        _pedra = Mat(new Color32(113, 121, 126, 255));
        _creme = Mat(new Color32(194, 184, 151, 255));
        _madeira = Mat(new Color32(168, 119, 72, 255));
        _couro = Mat(new Color32(109, 62, 58, 255));
        _metal = Mat(new Color32(220, 178, 91, 255));
        _agua = Mat(new Color32(99, 206, 193, 255), .36f);
    }

    public void Montar(string nome)
    {
        switch (nome)
        {
            case "Santuario": Santuario(); break;
            case "FonteRestauradora": Fonte(); break;
            case "OficinaDeArqueiro": Oficina(); break;
        }
    }

    public void Aljava(Vector3 p, float s, GameObject flecha)
    {
        var conjunto = new GameObject("Aljava");
        conjunto.transform.SetParent(_raiz, false);
        Vector3 eixo = new Vector3(-.20f, .97f, .16f).normalized;
        var giro = Quaternion.FromToRotation(Vector3.up, eixo);
        Solido(PrimitiveType.Cylinder, p + giro * Vector3.up * s * .48f,
            new Vector3(.49f, .49f, .41f) * s, _couro, giro, conjunto.transform);
        for (int i = 0; i < 3; i++)
        {
            float altura = new[] { .09f, .31f, .96f }[i];
            Solido(PrimitiveType.Cylinder, p + giro * Vector3.up * s * altura,
                new Vector3(.52f, i == 2 ? .057f : .024f, .45f) * s, _metal, giro, conjunto.transform);
        }

        var disco = Solido(PrimitiveType.Cylinder, p + giro * Vector3.up * s * 1.024f,
            new Vector3(.415f, .013f, .35f) * s, _couro, giro, conjunto.transform);
        disco.sharedMaterial = Mat(new Color32(54, 35, 47, 255));

        for (int i = 0; i < 4; i++)
        {
            var baseDaFlecha = p + giro * new Vector3((i - 1.45f) * .094f, .83f, i % 2 * .115f - .05f) * s;
            Vector3 direcao = giro * new Vector3((i - 1.5f) * .12f, 1f, (i % 2 - .5f) * .17f).normalized;
            float altura = (.64f + (i + 1) % 3 * .095f) * s;
            if (flecha != null)
            {
                var modelo = Object.Instantiate(flecha, conjunto.transform);
                Bounds b = Loopia.Hex.VisualDeIlha.LimitesLocais(modelo.transform);
                Vector3 e = b.size;
                Vector3 principal = e.z >= e.x && e.z >= e.y ? Vector3.forward : e.y >= e.x ? Vector3.up : Vector3.right;
                float escala = altura / Mathf.Max(e.x, e.y, e.z, .001f);
                // Penas expostas, pontas guardadas, como numa aljava em uso.
                modelo.transform.localRotation = Quaternion.FromToRotation(principal, -direcao);
                modelo.transform.localScale *= escala;
                modelo.transform.localPosition = baseDaFlecha + direcao * altura * .43f -
                    modelo.transform.localRotation * (b.center * escala);
            }
            else
            {
                Vareta(baseDaFlecha, baseDaFlecha + direcao * altura, .021f * s, _madeira, conjunto.transform);
                Vector3 penas = baseDaFlecha + direcao * altura * .88f;
                Solido(PrimitiveType.Cube, penas, new Vector3(.12f, .19f, .018f) * s, _creme,
                    Quaternion.FromToRotation(Vector3.up, direcao), conjunto.transform);
            }
        }
    }

    void Santuario()
    {
        Solido(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(1.88f,.15f,1.68f), _pedra);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.31f,0), new Vector3(1.57f,.06f,1.41f), _creme);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.55f,0), new Vector3(.48f,.21f,.48f), _pedra);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.80f,0), new Vector3(.68f,.09f,.68f), _creme);
        var cristal = Mat(new Color32(141, 242, 212, 255), .52f);
        cristal.EnableKeyword("_EMISSION");
        cristal.SetColor("_EmissionColor", new Color(.15f,.53f,.39f));
        Cristal(new Vector3(0,1.28f,0), .29f, .51f, cristal);
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120 + 12) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)) * .67f;
            Solido(PrimitiveType.Cube, p + Vector3.up * .45f, new Vector3(.20f,.25f,.20f), _pedra,
                Quaternion.Euler(0f, i * 120, 0f));
            Cristal(p + Vector3.up * .64f, .069f, .122f, cristal);
        }
    }

    void Fonte()
    {
        Solido(PrimitiveType.Cylinder, new Vector3(0,.12f,0), new Vector3(1.98f,.15f,1.98f), _pedra);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.25f,0), new Vector3(1.79f,.051f,1.79f), _agua);
        Borda(.91f, .295f, .094f, _creme);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.57f,0), new Vector3(.24f,.30f,.24f), _creme);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.87f,0), new Vector3(.91f,.077f,.91f), _pedra);
        Solido(PrimitiveType.Cylinder, new Vector3(0,.94f,0), new Vector3(.85f,.034f,.85f), _agua);
        Borda(.44f, .97f, .044f, _creme);
        Solido(PrimitiveType.Sphere, new Vector3(0,1.08f,0), new Vector3(.22f,.28f,.22f), _agua);
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72 + 15) * Mathf.Deg2Rad;
            var fora = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            Vector3 anterior = Vector3.up * .98f + fora * .43f;
            for (int j = 1; j <= 4; j++)
            {
                float p = j / 4f;
                Vector3 seguinte = fora * (.43f + p * .23f) + Vector3.up * (.98f - p * p * .71f);
                Vareta(anterior, seguinte, .036f - p * .007f, _agua);
                anterior = seguinte;
            }
        }
    }

    void Oficina()
    {
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3((i % 2 - .5f) * 1.20f,.35f,(i / 2 - .5f) * .62f);
            Solido(PrimitiveType.Cube, p, new Vector3(.16f,.74f,.16f), _couro);
        }
        for (int i = 0; i < 4; i++)
            Solido(PrimitiveType.Cube, new Vector3(0,.75f,(i-1.5f)*.23f), new Vector3(1.64f,.14f,.224f), _madeira);
        Solido(PrimitiveType.Cube, new Vector3(0,.59f,-.34f), new Vector3(1.2f,.16f,.13f), _couro);
        Aljava(new Vector3(.72f,.05f,-.55f), .60f, null);
        var baseDoArco = new Vector3(-.27f, .84f, .11f);
        Vector3 anterior = baseDoArco + new Vector3(-.47f, .03f, 0f);
        Vector3 inicio = anterior;
        for (int i = 1; i <= 13; i++)
        {
            float t = i / 13f;
            Vector3 proximo = baseDoArco + new Vector3(-.47f + t * .94f,
                Mathf.Sin(t * Mathf.PI) * .82f + .03f, 0f);
            Vareta(anterior, proximo, .021f + Mathf.Sin(t * Mathf.PI) * .022f, _metal);
            anterior = proximo;
        }
        Vareta(inicio, anterior, .008f, _creme);
        Vareta(new Vector3(-.55f,.85f,-.32f), new Vector3(.10f,.85f,-.26f), .022f, _couro);
        Solido(PrimitiveType.Cube, new Vector3(-.55f,.86f,-.31f), new Vector3(.09f,.08f,.29f), _pedra);
        for (int i = 0; i < 3; i++)
            Vareta(new Vector3(-.44f,.84f,.29f+i*.05f), new Vector3(.67f,.84f,.25f+i*.06f), .013f, _creme);
    }

    void Cristal(Vector3 centro, float largura, float altura, Material mat)
    {
        Vector3[] v = { Vector3.up*altura, Vector3.right*largura, Vector3.forward*largura,
            Vector3.left*largura, Vector3.back*largura, Vector3.down*altura*.53f };
        int[] faces = {0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};
        var pontos = new Vector3[faces.Length];
        for (int i = 0; i < faces.Length; i++) pontos[i] = v[faces[i]];
        var indices = new int[faces.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        var malha = new Mesh { vertices = pontos, triangles = indices };
        malha.RecalculateNormals();
        _malhas.Add(malha);
        var cristal = new GameObject("Cristal");
        cristal.transform.SetParent(_raiz, false);
        cristal.transform.localPosition = centro;
        cristal.transform.localRotation = Quaternion.Euler(0, 24f, 0);
        cristal.AddComponent<MeshFilter>().sharedMesh = malha;
        cristal.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    void Borda(float raio, float y, float espessura, Material mat)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12;
            var p = new Vector3(Mathf.Cos(a)*raio,y,Mathf.Sin(a)*raio);
            var q = new Vector3(Mathf.Cos(b)*raio,y,Mathf.Sin(b)*raio);
            Vareta(p, q, espessura, mat);
        }
    }

    void Vareta(Vector3 p, Vector3 q, float diametro, Material mat, Transform pai = null)
    {
        Vector3 delta = q - p;
        Solido(PrimitiveType.Cylinder, (p+q)*.5f, new Vector3(diametro,delta.magnitude*.51f,diametro),
            mat, Quaternion.FromToRotation(Vector3.up, delta), pai);
    }

    Renderer Solido(PrimitiveType tipo, Vector3 p, Vector3 tamanho, Material mat,
        Quaternion? orientacao = null, Transform pai = null)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.transform.SetParent(pai != null ? pai : _raiz, false);
        go.transform.localPosition = p;
        go.transform.localScale = tamanho;
        go.transform.localRotation = orientacao ?? Quaternion.identity;
        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = mat;
        return renderer;
    }

    Material Mat(Color cor, float rugosidade = .17f)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = cor;
        mat.SetFloat("_Smoothness", rugosidade);
        _temporarios.Add(mat);
        return mat;
    }
}
