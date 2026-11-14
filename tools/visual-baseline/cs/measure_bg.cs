// Play Mode. Posicao/bounds do cenario de fundo, rio e nuvens em relacao ao anel de ilhas e a camera.
// Placeholders: %%W%%, %%H%% (resolucao usada para enquadrar), %%SEED%%
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
int W = %%W%%, H = %%H%%;
var prevTarget = cam.targetTexture;
var rt = new UnityEngine.RenderTexture(W, H, 24); rt.Create();
System.Func<UnityEngine.Vector3, float[]> f3 = v => new float[]{ v.x, v.y, v.z };
System.Func<UnityEngine.Bounds, float[]> v3 = b => new float[]{ b.size.x, b.size.y, b.size.z };
System.Func<UnityEngine.Renderer[], UnityEngine.Bounds?> uni = rs => {
    UnityEngine.Bounds? r = null;
    foreach (var q in rs) { if (!q.enabled || !q.gameObject.activeInHierarchy) continue; if (r == null) r = q.bounds; else { var t = r.Value; t.Encapsulate(q.bounds); r = t; } }
    return r;
};
try {
    cam.targetTexture = rt; cam.aspect = (float)W / H;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    foreach (var r in UnityEngine.Object.FindObjectsByType<Loopia.Hex.RioDeFundo>(UnityEngine.FindObjectsSortMode.None)) r.Ajustar();
    foreach (var n in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CamadaDeNuvens>(UnityEngine.FindObjectsSortMode.None)) n.Atualizar(0f);

    // anel de ilhas (bounds de todos os renderers de ilha, inclusive decoracoes)
    UnityEngine.Bounds? ring = null; float minY = float.MaxValue, maxY = float.MinValue; float ringRadius = 0f;
    UnityEngine.Vector3 soma = UnityEngine.Vector3.zero; int cnt = 0;
    foreach (var t in mundo.Ilhas) { soma += t.transform.position; cnt++; }
    var centro = soma / cnt;
    float camMinZ = float.MaxValue, camMaxZ = float.MinValue;
    var camInv = cam.transform.worldToLocalMatrix;
    foreach (var t in mundo.Ilhas) {
        foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) {
            var b = r.bounds;
            if (ring == null) ring = b; else { var q = ring.Value; q.Encapsulate(b); ring = q; }
            minY = System.Math.Min(minY, b.min.y); maxY = System.Math.Max(maxY, b.max.y);
            var cz = camInv.MultiplyPoint3x4(b.center).z; camMinZ = System.Math.Min(camMinZ, cz); camMaxZ = System.Math.Max(camMaxZ, cz);
        }
        var dxz = new UnityEngine.Vector2(t.transform.position.x - centro.x, t.transform.position.z - centro.z).magnitude;
        ringRadius = System.Math.Max(ringRadius, dxz);
    }
    var o = new System.Collections.Generic.Dictionary<string, object>();
    o["seed"] = %%SEED%%; o["resolucao"] = W + "x" + H;
    o["anel"] = new { centro = f3(centro), raioMaxIlhasCentroXZ = ringRadius, boundsMin = f3(ring.Value.min), boundsMax = f3(ring.Value.max), ySuperiorDasIlhas = maxY, yInferiorDasIlhas = minY, profundidadeCamZ_centroDosRenderers_min = camMinZ, profundidadeCamZ_max = camMaxZ };
    o["camera"] = new { pos = f3(cam.transform.position), orthoSize = cam.orthographicSize, forward = f3(cam.transform.forward) };

    var list = new System.Collections.Generic.List<object>();
    var cenario = UnityEngine.GameObject.Find("Cenario background");
    System.Func<string, UnityEngine.GameObject, object> info = (rotulo, go) => {
        var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
        var ub = uni(rs);
        var d = new System.Collections.Generic.Dictionary<string, object>();
        d["objeto"] = rotulo; d["posMundo"] = f3(go.transform.position); d["escalaMundo"] = f3(go.transform.lossyScale); d["rotEuler"] = f3(go.transform.eulerAngles);
        d["filhos"] = go.transform.childCount; d["renderers"] = rs.Length;
        if (ub.HasValue) {
            var b = ub.Value;
            d["boundsMin"] = f3(b.min); d["boundsMax"] = f3(b.max); d["boundsSize"] = v3(b);
            d["topoRelativoAoTopoDasIlhas"] = b.max.y - 0f;
            d["yTopoMenosYInferiorDasIlhas"] = b.max.y - minY;
            d["yTopoMenosYSuperiorDasIlhas"] = b.max.y - maxY;
            var cz = camInv.MultiplyPoint3x4(b.center).z;
            d["profundidadeCamZ_centro"] = cz;
            d["distanciaXZ_centroParaCentroDoAnel"] = new UnityEngine.Vector2(b.center.x - centro.x, b.center.z - centro.z).magnitude;
            // sobreposicao na tela
            var minS = new UnityEngine.Vector2(float.MaxValue, float.MaxValue); var maxS = new UnityEngine.Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++) { var pt = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)); var s = cam.WorldToScreenPoint(pt); minS = UnityEngine.Vector2.Min(minS, s); maxS = UnityEngine.Vector2.Max(maxS, s); }
            d["telaPx_min"] = new float[]{minS.x, minS.y}; d["telaPx_max"] = new float[]{maxS.x, maxS.y};
        }
        return d;
    };
    if (cenario != null) {
        list.Add(info("Cenario background (raiz)", cenario));
        var comp = cenario.GetComponent<Loopia.Hex.CenarioContinuo>();
        var filhos = new System.Collections.Generic.List<object>();
        foreach (UnityEngine.Transform ch in cenario.transform) {
            var rs = ch.GetComponentsInChildren<UnityEngine.Renderer>();
            var ub = uni(rs);
            if (!ub.HasValue) { filhos.Add(new { nome = ch.name, pos = f3(ch.position), semRenderer = true }); continue; }
            var b = ub.Value;
            var cz = camInv.MultiplyPoint3x4(b.center).z;
            var cd = new System.Collections.Generic.Dictionary<string, object>();
            cd["nome"] = ch.name; cd["posMundo"] = f3(ch.position); cd["boundsSize"] = v3(b); cd["yMin"] = b.min.y; cd["yMax"] = b.max.y; cd["camZ"] = cz;
            cd["distXZ_aoCentroDoAnel"] = new UnityEngine.Vector2(ch.position.x - centro.x, ch.position.z - centro.z).magnitude;
            var minS = new UnityEngine.Vector2(float.MaxValue, float.MaxValue); var maxS = new UnityEngine.Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++) { var pt = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)); var s = cam.WorldToScreenPoint(pt); minS = UnityEngine.Vector2.Min(minS, s); maxS = UnityEngine.Vector2.Max(maxS, s); }
            cd["telaPx_min"] = new float[]{minS.x, minS.y}; cd["telaPx_max"] = new float[]{maxS.x, maxS.y};
            filhos.Add(cd);
        }
        o["cenarioFilhos"] = filhos;
        o["cenarioComponente"] = new { margem = comp.margem, aguaAtribuida = comp.agua != null };
    }
    var rio = UnityEngine.GameObject.Find("Rio de fundo");
    if (rio != null) {
        list.Add(info("Rio de fundo", rio));
        var mf = rio.GetComponent<UnityEngine.MeshFilter>();
        float distMin = float.MaxValue; string ilhaMaisPerto = ""; float dy = 0f; float distMinXZ = float.MaxValue;
        var verts = mf.sharedMesh.vertices; var ltw = rio.transform.localToWorldMatrix;
        float rioY = float.MinValue; foreach (var v in verts) { rioY = System.Math.Max(rioY, ltw.MultiplyPoint3x4(v).y); }
        foreach (var t in mundo.Ilhas) {
            var c2 = new UnityEngine.Vector2(t.transform.position.x, t.transform.position.z);
            foreach (var v in verts) { var w = ltw.MultiplyPoint3x4(v); float dd = new UnityEngine.Vector2(w.x, w.z).Equals(c2) ? 0f : (new UnityEngine.Vector2(w.x, w.z) - c2).magnitude; if (dd < distMinXZ) { distMinXZ = dd; ilhaMaisPerto = t.name; } }
        }
        o["rio"] = new { vertices = verts.Length, yDaSuperficie = rioY, yInferiorDasIlhas = minY, yDasIlhasTopo = maxY, distanciaXZ_verticeMaisPertoDeCentroDeIlha = distMinXZ, ilhaMaisPerto = ilhaMaisPerto, malhaBoundsLocal = v3(mf.sharedMesh.bounds), ondas = rio.GetComponent<Loopia.Hex.RioDeFundo>().alturaDasOndas };
        // ilhas cujo centro projeta dentro do contorno 2D do rio (em XZ): usa AABB do rio
        var rb = rio.GetComponent<UnityEngine.Renderer>().bounds;
        int emCima = 0; foreach (var t in mundo.Ilhas) { var p = t.transform.position; if (p.x >= rb.min.x && p.x <= rb.max.x && p.z >= rb.min.z && p.z <= rb.max.z) emCima++; }
        o["rioIlhasDentroDoAABBxz"] = emCima; o["rioAABB"] = new { min = f3(rb.min), max = f3(rb.max) };
        // quantas ilhas ficam sobre pixels de agua? (projecao do centro da ilha cai no AABB de tela do rio) -- so aproximado
    }
    var nuv = UnityEngine.GameObject.Find("Camada de nuvens");
    if (nuv != null) {
        var cl = new System.Collections.Generic.List<object>();
        float zmin = float.MaxValue, zmax = float.MinValue, ymin = float.MaxValue, ymax = float.MinValue; int atrasDoAnel = 0, frenteDoAnel = 0;
        foreach (UnityEngine.Transform ch in nuv.transform) {
            var cz = camInv.MultiplyPoint3x4(ch.position).z; zmin = System.Math.Min(zmin, cz); zmax = System.Math.Max(zmax, cz);
            ymin = System.Math.Min(ymin, ch.position.y); ymax = System.Math.Max(ymax, ch.position.y);
            if (cz > camMaxZ) atrasDoAnel++; else if (cz < camMinZ) frenteDoAnel++;
            var rr = ch.GetComponent<UnityEngine.Renderer>();
            cl.Add(new { nome = ch.name, pos = f3(ch.position), escala = f3(ch.lossyScale), camZ = cz, enabled = rr != null && rr.enabled, ativo = ch.gameObject.activeInHierarchy });
        }
        o["nuvens"] = new { quantidade = nuv.transform.childCount, yMin = ymin, yMax = ymax, camZ_min = zmin, camZ_max = zmax, yInferiorDasIlhas = minY, abaixoDoAnel_distancia = minY - ymax, atrasEmProfundidadeDoAnel = atrasDoAnel, frenteEmProfundidadeDoAnel = frenteDoAnel, detalhe = cl };
        o["nuvensComponente"] = new { distanciaDasIlhas = nuv.GetComponent<Loopia.Hex.CamadaDeNuvens>().distanciaDasIlhas, opacidade = nuv.GetComponent<Loopia.Hex.CamadaDeNuvens>().opacidade, tamanho = f3(new UnityEngine.Vector3(nuv.GetComponent<Loopia.Hex.CamadaDeNuvens>().tamanho.x, nuv.GetComponent<Loopia.Hex.CamadaDeNuvens>().tamanho.y, 0)) };
    }
    o["objetos"] = list;
    return Newtonsoft.Json.JsonConvert.SerializeObject(o);
} finally {
    cam.targetTexture = prevTarget;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    cam.ResetAspect();
}
