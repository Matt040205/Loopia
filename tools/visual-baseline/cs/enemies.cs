// Play Mode (mapa ja gerado com a semente, timeScale 0). Faz nascer UM Lobo e UM Morcego em casas do caminho
// perto do acampamento (InimigoSpawner.Nascer), renderiza o enquadramento normal e um close-up
// (ortho ~6 centrado nos inimigos + Lucca), salva 2 JPGs e devolve as medidas em JSON.
// Placeholders: %%W%%, %%H%%, %%OUT_NORMAL%%, %%OUT_CLOSE%% (caminhos absolutos .jpg), %%CLOSE_ORTHO%%
var U = new System.Collections.Generic.Dictionary<string, object>();
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
var sp = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.InimigoSpawner>();
var lucca = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>();
int W = %%W%%, H = %%H%%;
var prevTarget = cam.targetTexture;
int msaa = 1; var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; if (urp != null) msaa = urp.msaaSampleCount;
var rt = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
rt.antiAliasing = msaa; rt.Create();
System.Func<UnityEngine.Bounds, UnityEngine.Vector3[]> corners = b => {
    var a = new UnityEngine.Vector3[8];
    for (int i = 0; i < 8; i++) a[i] = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
    return a;
};
System.Func<UnityEngine.Bounds, float[]> px = b => {
    float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
    foreach (var p in corners(b)) { var s = cam.WorldToScreenPoint(p); x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); y0 = System.Math.Min(y0, s.y); y1 = System.Math.Max(y1, s.y); }
    return new float[]{ x1 - x0, y1 - y0 };
};
System.Func<UnityEngine.GameObject, UnityEngine.Bounds?> uni = go => {
    UnityEngine.Bounds? r = null;
    foreach (var q in go.GetComponentsInChildren<UnityEngine.Renderer>()) { if (!q.enabled || !q.gameObject.activeInHierarchy) continue; if (r == null) r = q.bounds; else { var t = r.Value; t.Encapsulate(q.bounds); r = t; } }
    return r;
};
System.Func<UnityEngine.Bounds, float[]> v3 = b => new float[]{ b.size.x, b.size.y, b.size.z };
System.Func<UnityEngine.Vector3, float[]> f3 = v => new float[]{ v.x, v.y, v.z };
System.Action<string> salvar = caminho => {
    cam.Render();
    var prevActive = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prevActive;
    System.IO.File.WriteAllBytes(caminho, tex.EncodeToJPG(85));
    UnityEngine.Object.DestroyImmediate(tex);
};
bool hicAtivo = hic.enabled;
try {
    cam.targetTexture = rt; cam.aspect = (float)W / H;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    foreach (var n in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CamadaDeNuvens>(UnityEngine.FindObjectsSortMode.None)) n.Atualizar(0f);
    foreach (var r in UnityEngine.Object.FindObjectsByType<Loopia.Hex.RioDeFundo>(UnityEngine.FindObjectsSortMode.None)) r.Ajustar();
    // Lucca no acampamento (so Play Mode): a referencia de escala do close-up fica ao lado do lobo e do morcego.
    var agenteDoLucca = lucca.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (agenteDoLucca != null && agenteDoLucca.isOnNavMesh) agenteDoLucca.Warp(mundo.PosicaoDe(g.Loop[0]));
    else lucca.transform.position = mundo.PosicaoDe(g.Loop[0]);
    var lobo = sp.Nascer(sp.lobo, g.Loop[1]);
    var morcego = sp.Nascer(sp.morcego, g.Loop[2]);
    U["timeScale"] = UnityEngine.Time.timeScale;
    U["loopLobo"] = g.Loop[1].ToString(); U["loopMorcego"] = g.Loop[2].ToString();
    var alvos = new System.Collections.Generic.List<UnityEngine.GameObject>{ lucca.gameObject, lobo.gameObject, morcego.gameObject };
    var nomes = new string[]{ "lucca", "lobo", "morcego" };
    System.Action<string> medir = fase => {
        var d = new System.Collections.Generic.Dictionary<string, object>();
        d["orthoSize"] = cam.orthographicSize; d["pixelsPorUnidade"] = cam.pixelHeight / (2f * cam.orthographicSize);
        for (int i = 0; i < alvos.Count; i++) {
            var ub = uni(alvos[i]);
            if (!ub.HasValue) continue;
            var e = new System.Collections.Generic.Dictionary<string, object>();
            e["boundsMundo"] = v3(ub.Value); e["alturaMundo"] = ub.Value.size.y; e["pxLarguraAltura"] = px(ub.Value);
            e["baseRelativaAoPivo"] = ub.Value.min.y - alvos[i].transform.position.y;
            e["pos"] = f3(alvos[i].transform.position);
            e["usaSkinned"] = alvos[i].GetComponentInChildren<UnityEngine.SkinnedMeshRenderer>() != null;
            if (i > 0) e["escalaDef"] = i == 1 ? lobo.Def.escala : morcego.Def.escala;
            d[nomes[i]] = e;
        }
        U[fase] = d;
    };
    medir("enquadramentoNormal");
    salvar("%%OUT_NORMAL%%");
    // close-up: tira o enquadramento automatico do caminho e centra em Lucca + inimigos
    hic.enabled = false;
    var soma = UnityEngine.Vector3.zero;
    foreach (var a in alvos) { var u = uni(a); soma += u.HasValue ? u.Value.center : a.transform.position; }
    var centroDoClose = soma / alvos.Count;
    var rot = UnityEngine.Quaternion.Euler(hic.inclinacao, hic.giro, 0f);
    cam.orthographicSize = %%CLOSE_ORTHO%%f;
    float dist = System.Math.Max(hic.distancia, cam.orthographicSize * 3f);
    cam.transform.rotation = rot;
    cam.transform.position = centroDoClose - rot * UnityEngine.Vector3.forward * dist;
    medir("closeUp");
    salvar("%%OUT_CLOSE%%");
    U["lobo_def"] = new { def = lobo.Def.name, escala = lobo.Def.escala, prefab = lobo.Def.prefab != null ? lobo.Def.prefab.name : "-" };
    U["morcego_def"] = new { def = morcego.Def.name, escala = morcego.Def.escala, prefab = morcego.Def.prefab != null ? morcego.Def.prefab.name : "-" };
    U["largura"] = W; U["altura"] = H;
    return Newtonsoft.Json.JsonConvert.SerializeObject(U);
} finally {
    sp.LimparTodos();
    hic.enabled = hicAtivo;
    cam.targetTexture = prevTarget;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    cam.ResetAspect();
    hic.Enquadrar();
}
