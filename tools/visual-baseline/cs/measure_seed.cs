// Play Mode (mapa ja gerado com a semente). Mede ilhas, decoracoes, Lucca, inimigos, cartas-modelo e enquadramento.
// Placeholder: %%SEED%%. Tudo e lido do Editor; nada e salvo.
var U = new System.Collections.Generic.Dictionary<string, object>();
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
var prevTarget = cam.targetTexture;
int W = 1920, H = 1080;
var rt = new UnityEngine.RenderTexture(W, H, 24);
rt.Create();
System.Func<UnityEngine.Bounds, UnityEngine.Vector3[]> corners = b => {
    var a = new UnityEngine.Vector3[8];
    for (int i = 0; i < 8; i++) a[i] = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
    return a;
};
System.Func<System.Collections.Generic.IEnumerable<UnityEngine.Vector3>, float[]> px = pts => {
    float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
    foreach (var p in pts) { var s = cam.WorldToScreenPoint(p); x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); y0 = System.Math.Min(y0, s.y); y1 = System.Math.Max(y1, s.y); }
    return new float[]{ x1 - x0, y1 - y0 };
};
System.Func<UnityEngine.Renderer[], UnityEngine.Bounds?> uni = rs => {
    UnityEngine.Bounds? r = null;
    foreach (var q in rs) { if (!q.enabled || !q.gameObject.activeInHierarchy) continue; if (r == null) r = q.bounds; else { var t = r.Value; t.Encapsulate(q.bounds); r = t; } }
    return r;
};
System.Func<UnityEngine.Bounds, float[]> v3 = b => new float[]{ b.size.x, b.size.y, b.size.z };
System.Func<UnityEngine.Vector3, float[]> f3 = v => new float[]{ v.x, v.y, v.z };
try {
    cam.targetTexture = rt;
    cam.aspect = (float)W / H;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    U["seed"] = %%SEED%%;
    U["camera1080p"] = new { orthoSize = cam.orthographicSize, pos = f3(cam.transform.position), pixelW = cam.pixelWidth, pixelH = cam.pixelHeight };
    U["pixelsPorUnidadeDeMundo_ortho"] = cam.pixelHeight / (2f * cam.orthographicSize);

    // ---------- Ilhas ----------
    var ilhas = new System.Collections.Generic.List<object>();
    var decos = new System.Collections.Generic.List<object>();
    foreach (var tile in mundo.Ilhas) {
        var tr = tile.transform;
        var d = new System.Collections.Generic.Dictionary<string, object>();
        d["nome"] = tile.name; d["tipo"] = tile.Kind.ToString(); d["posMundo"] = f3(tr.position); d["altura"] = tile.Altura;
        var hexR = tile.GetComponent<UnityEngine.MeshRenderer>();
        d["hexMeshRendererEnabled"] = hexR.enabled; d["hexMeshBounds"] = v3(hexR.bounds);
        var col = tile.GetComponent<UnityEngine.MeshCollider>();
        d["meshColliderBounds"] = v3(col.bounds);
        float colTop = float.MinValue; var colTopPts = new System.Collections.Generic.List<UnityEngine.Vector3>();
        foreach (var v in col.sharedMesh.vertices) { var w = tr.TransformPoint(v); if (w.y > colTop) colTop = w.y; }
        foreach (var v in col.sharedMesh.vertices) { var w = tr.TransformPoint(v); if (w.y > colTop - 0.001f) colTopPts.Add(w); }
        d["colliderTopY"] = colTop; d["colliderTopRelativoAoTile"] = colTop - tr.position.y;
        d["colliderTopoPx"] = px(colTopPts);
        UnityEngine.Transform modeloRaiz = null;
        foreach (UnityEngine.Transform ch in tr) if (ch.name == mundo.modeloDaIlha.name) modeloRaiz = ch;
        if (modeloRaiz != null) {
            var rs = modeloRaiz.GetComponentsInChildren<UnityEngine.Renderer>();
            var ub = uni(rs);
            if (ub.HasValue) {
                d["modeloBoundsMundo"] = new { center = f3(ub.Value.center), size = v3(ub.Value), min = f3(ub.Value.min), max = f3(ub.Value.max) };
                d["modeloTopoY"] = ub.Value.max.y; d["modeloTopoRelativoAoTile"] = ub.Value.max.y - tr.position.y;
                d["modeloPx"] = px(corners(ub.Value));
                var top = new System.Collections.Generic.List<UnityEngine.Vector3>();
                foreach (var mf in modeloRaiz.GetComponentsInChildren<UnityEngine.MeshFilter>()) foreach (var v in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(v); if (w.y > ub.Value.max.y - 0.03f) top.Add(w); }
                d["modeloSuperficieTopoPx"] = px(top); d["modeloVerticesNoTopo"] = top.Count;
            }
        }
        ilhas.Add(d);
        // ---------- Decoracoes ----------
        UnityEngine.Transform dec = null;
        foreach (UnityEngine.Transform ch in tr) if (ch.name == "Decoracoes") dec = ch;
        if (dec != null) {
            foreach (UnityEngine.Transform go in dec) {
                var dd = new System.Collections.Generic.Dictionary<string, object>();
                dd["ilha"] = tile.name; dd["fonte"] = go.name; dd["ativo"] = go.gameObject.activeInHierarchy;
                var lb = Loopia.Hex.VisualDeIlha.LimitesLocais(go);
                dd["tamanhoNormalizado"] = System.Math.Max(lb.size.x, System.Math.Max(lb.size.y, lb.size.z));
                dd["escalaDoModeloInterno"] = go.childCount > 0 ? go.GetChild(0).localScale.x : -1f;
                dd["localBoundsSize"] = v3(lb);
                var rs = go.GetComponentsInChildren<UnityEngine.Renderer>();
                var ub = uni(rs);
                if (ub.HasValue) {
                    dd["boundsMundoAABB"] = v3(ub.Value);
                    dd["baseYRelativaAoTopoDoTile"] = ub.Value.min.y - tr.position.y;
                    dd["pxAABB"] = px(corners(ub.Value));
                    var obb = new System.Collections.Generic.List<UnityEngine.Vector3>();
                    foreach (var c in corners(lb)) obb.Add(go.TransformPoint(c));
                    dd["pxOBB"] = px(obb);
                }
                decos.Add(dd);
            }
        }
    }
    U["ilhas"] = ilhas; U["decoracoes"] = decos;
    float tamanho = mundo.tamanhoDoHexagono * (1f - mundo.folgaEntreHexagonos);
    U["modeloDaIlhaTamanhoPedido_diametro"] = tamanho * 2f;

    // ---------- Lucca ----------
    var lucca = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>();
    var L = new System.Collections.Generic.Dictionary<string, object>();
    L["pos"] = f3(lucca.transform.position); L["lossyScale"] = f3(lucca.transform.lossyScale); L["casa"] = lucca.Coord.ToString();
    var lrs = lucca.GetComponentsInChildren<UnityEngine.Renderer>(true);
    var lr = new System.Collections.Generic.List<object>();
    foreach (var r in lrs) lr.Add(new { nome = r.name, tipo = r.GetType().Name, enabled = r.enabled, ativo = r.gameObject.activeInHierarchy, size = v3(r.bounds), min = f3(r.bounds.min), max = f3(r.bounds.max), shader = r.sharedMaterial != null ? r.sharedMaterial.shader.name : "-" });
    L["renderers"] = lr;
    var lub = uni(lrs);
    if (lub.HasValue) { L["boundsUniao"] = new { size = v3(lub.Value), min = f3(lub.Value.min), max = f3(lub.Value.max), baseRelativaAoPivo = lub.Value.min.y - lucca.transform.position.y }; L["px"] = px(corners(lub.Value)); }
    var ag = lucca.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null) { L["agenteRaio"] = ag.radius; L["agenteAltura"] = ag.height; L["agenteBaseOffset"] = ag.baseOffset; }
    var smr = lucca.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>();
    if (smr != null) { L["smrLocalBoundsSize"] = v3(smr.localBounds); L["smrRootBone"] = smr.rootBone != null ? smr.rootBone.name : "-"; }
    U["lucca"] = L;

    // ---------- Inimigos (nascidos temporariamente; descartados ao sair do Play) ----------
    var sp = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.InimigoSpawner>();
    var E = new System.Collections.Generic.List<object>();
    U["inimigosVivosAntes"] = sp.Vivos.Count;
    var defs = new Loopia.Hex.InimigoDef[]{ sp.morcego, sp.lobo };
    int idx = 6;
    foreach (var def in defs) {
        if (def == null) continue;
        var casa = g.Loop[idx]; idx += 5;
        var ini = sp.Nascer(def, casa);
        if (ini == null) { E.Add(new { def = def.name, erro = "Nascer devolveu null" }); continue; }
        var ers = ini.GetComponentsInChildren<UnityEngine.Renderer>();
        var eub = uni(ers);
        var ed = new System.Collections.Generic.Dictionary<string, object>();
        ed["def"] = def.name; ed["escalaDef"] = def.escala; ed["voa"] = def.voa; ed["alturaDeVoo"] = def.alturaDeVoo; ed["prefab"] = def.prefab != null ? def.prefab.name : "-";
        ed["pos"] = f3(ini.transform.position);
        Loopia.Hex.HexTile tileE = null;
        mundo.TryGetIlha(casa, out tileE);
        if (tileE != null) ed["alturaAcimaDoTopoDoTile"] = ini.transform.position.y - tileE.transform.position.y;
        if (eub.HasValue) { ed["boundsSize"] = v3(eub.Value); ed["baseRelativaAoPivo"] = eub.Value.min.y - ini.transform.position.y; ed["px"] = px(corners(eub.Value)); }
        ed["renderers"] = ers.Length;
        ed["usaSkinned"] = ini.GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>() != null;
        E.Add(ed);
    }
    U["inimigos"] = E;
    sp.LimparTodos();

    // ---------- Modelos das cartas aplicados do jeito do jogo (temporarios) ----------
    var P = new System.Collections.Generic.List<object>();
    Loopia.Hex.HexTile camp = null;
    mundo.TryGetIlha(g.Loop[0], out camp);
    foreach (var nome in new string[]{"Coracao","Flechas","Moedas","Mola"}) {
        var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Loopia.Hex.PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");
        if (def == null || def.modelo == null) { P.Add(new { nome = nome, erro = "sem def/modelo" }); continue; }
        var tmp = new UnityEngine.GameObject("__tmp_plat");
        tmp.transform.position = camp.transform.position;
        var m = Loopia.Hex.VisualDeIlha.Criar(def.modelo, tmp.transform, def.tamanhoDoModelo);
        m.transform.localPosition = UnityEngine.Vector3.up * def.alturaDoModelo;
        var prs = m.GetComponentsInChildren<UnityEngine.Renderer>();
        var pub = uni(prs);
        var pd = new System.Collections.Generic.Dictionary<string, object>();
        pd["carta"] = nome; pd["prefab"] = def.modelo.name; pd["tamanhoDoModelo"] = def.tamanhoDoModelo; pd["alturaDoModelo"] = def.alturaDoModelo; pd["girar"] = def.girarModelo;
        if (pub.HasValue) { pd["boundsSize"] = v3(pub.Value); pd["baseAcimaDoTopoDoTile"] = pub.Value.min.y - camp.transform.position.y; pd["px"] = px(corners(pub.Value)); }
        P.Add(pd);
        UnityEngine.Object.Destroy(tmp);
    }
    U["modelosDeCartaNoJogo"] = P;
    return Newtonsoft.Json.JsonConvert.SerializeObject(U);
} finally {
    cam.targetTexture = prevTarget;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    cam.ResetAspect();
}
