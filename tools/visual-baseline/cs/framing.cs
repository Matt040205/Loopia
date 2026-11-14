// Play Mode (mapa ja gerado com a semente). Para cada margem de %%MARGINS%% (separadas por |) e cada resolucao
// de %%SIZES%% (WxH separados por |): enquadra e mede ortho, altura do Lucca em px, largura mediana do topo da ilha
// em px e a MENOR distancia (px) de qualquer canto de bounds ao limite do quadro (modelo da ilha, decoracoes, hex).
// Restaura a margem original no fim. Nada e salvo.
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
var lucca = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>();
var prevTarget = cam.targetTexture;
float margemOriginal = hic.margem;
var res = new System.Collections.Generic.List<object>();
System.Func<UnityEngine.Bounds, UnityEngine.Vector3[]> corners = b => {
    var a = new UnityEngine.Vector3[8];
    for (int i = 0; i < 8; i++) a[i] = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
    return a;
};
try {
    foreach (var sz in "%%SIZES%%".Split('|')) {
        var wh = sz.Split('x'); int W = int.Parse(wh[0]), H = int.Parse(wh[1]);
        var rt = new UnityEngine.RenderTexture(W, H, 24); rt.Create();
        cam.targetTexture = rt; cam.aspect = (float)W / H;
        foreach (var ms in "%%MARGINS%%".Split('|')) {
            hic.margem = float.Parse(ms, System.Globalization.CultureInfo.InvariantCulture);
            hic.Enquadrar();
            float minModelo = float.MaxValue, minDeco = float.MaxValue, minHex = float.MaxValue;
            string quemModelo = "", quemDeco = "";
            var larguras = new System.Collections.Generic.List<float>();
            int nDeco = 0, ilhasComDeco = 0;
            foreach (var tile in mundo.Ilhas) {
                int decoAtivas = 0;
                foreach (var r in tile.GetComponentsInChildren<UnityEngine.Renderer>()) {
                    if (!r.gameObject.activeInHierarchy) continue;
                    bool emDeco = false; var p = r.transform;
                    while (p != null && p != tile.transform) { if (p.name == "Decoracoes") { emDeco = true; break; } p = p.parent; }
                    if (emDeco) decoAtivas++;
                    float m = float.MaxValue;
                    foreach (var c in corners(r.bounds)) { var s = cam.WorldToScreenPoint(c); m = System.Math.Min(m, System.Math.Min(System.Math.Min(s.x, W - s.x), System.Math.Min(s.y, H - s.y))); }
                    if (emDeco) { if (m < minDeco) { minDeco = m; quemDeco = tile.name + "/" + r.transform.parent.name; } }
                    else if (r.gameObject == tile.gameObject) { }
                    else if (m < minModelo) { minModelo = m; quemModelo = tile.name; }
                }
                nDeco += decoAtivas; if (decoAtivas > 0) ilhasComDeco++;
                var mf = tile.GetComponent<UnityEngine.MeshFilter>();
                foreach (var c in corners(new UnityEngine.Bounds(mf.sharedMesh.bounds.center, mf.sharedMesh.bounds.size))) {
                    var s = cam.WorldToScreenPoint(tile.transform.TransformPoint(c));
                    minHex = System.Math.Min(minHex, System.Math.Min(System.Math.Min(s.x, W - s.x), System.Math.Min(s.y, H - s.y)));
                }
                // topo do modelo (superficie) em px
                UnityEngine.Transform modeloRaiz = null;
                foreach (UnityEngine.Transform ch in tile.transform) if (mundo.modeloDaIlha != null && ch.name == mundo.modeloDaIlha.name) modeloRaiz = ch;
                if (modeloRaiz != null) {
                    UnityEngine.Bounds? ub = null;
                    foreach (var q in modeloRaiz.GetComponentsInChildren<UnityEngine.Renderer>()) { if (ub == null) ub = q.bounds; else { var t = ub.Value; t.Encapsulate(q.bounds); ub = t; } }
                    float x0 = float.MaxValue, x1 = float.MinValue;
                    foreach (var mf2 in modeloRaiz.GetComponentsInChildren<UnityEngine.MeshFilter>())
                        foreach (var v in mf2.sharedMesh.vertices) { var w = mf2.transform.TransformPoint(v); if (w.y > ub.Value.max.y - 0.03f) { var s = cam.WorldToScreenPoint(w); x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); } }
                    larguras.Add(x1 - x0);
                }
            }
            larguras.Sort();
            float luccaH = 0f, luccaW = 0f;
            UnityEngine.Bounds? lb = null;
            foreach (var q in lucca.GetComponentsInChildren<UnityEngine.Renderer>()) { if (lb == null) lb = q.bounds; else { var t = lb.Value; t.Encapsulate(q.bounds); lb = t; } }
            { float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue; foreach (var c in corners(lb.Value)) { var s = cam.WorldToScreenPoint(c); x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); y0 = System.Math.Min(y0, s.y); y1 = System.Math.Max(y1, s.y); } luccaW = x1 - x0; luccaH = y1 - y0; }
            res.Add(new { resolucao = sz, margem = hic.margem, orthoSize = cam.orthographicSize, pxPorUnidade = cam.pixelHeight / (2f * cam.orthographicSize), luccaAlturaPx = luccaH, luccaLarguraPx = luccaW,
                topoDaIlhaLarguraPxMediana = larguras.Count > 0 ? larguras[larguras.Count / 2] : -1f, topoDaIlhaLarguraPxMin = larguras.Count > 0 ? larguras[0] : -1f,
                minDistPx_modeloDaIlha = minModelo, quemModelo = quemModelo, minDistPx_decoracoes = minDeco, quemDeco = quemDeco, minDistPx_hexMesh = minHex,
                ilhas = mundo.TotalDeIlhas, decoracoesAtivas = nDeco, ilhasComDecoracao = ilhasComDeco });
        }
        cam.targetTexture = prevTarget; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
} finally {
    hic.margem = margemOriginal;
    cam.targetTexture = prevTarget;
    cam.ResetAspect();
    hic.Enquadrar();
}
return Newtonsoft.Json.JsonConvert.SerializeObject(new { seed = %%SEED%%, medidas = res });
