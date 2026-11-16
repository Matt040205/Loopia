// Play Mode. Profundidade de camera (distancia ao longo do forward, igual ao eye depth) dos renderers das ilhas
// (inclui decoracoes e o modelo flutuante) e do fundo (Cenario background, rio, nuvens), para escolher fogInicio/fogFim.
// Placeholders: %%W%%, %%H%%
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
int W = %%W%%, H = %%H%%;
var prevTarget = cam.targetTexture;
var rt = new UnityEngine.RenderTexture(W, H, 24); rt.Create();
try {
    cam.targetTexture = rt; cam.aspect = (float)W / H;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    foreach (var r in UnityEngine.Object.FindObjectsByType<Loopia.Hex.RioDeFundo>(UnityEngine.FindObjectsSortMode.None)) r.Ajustar();
    foreach (var n in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CamadaDeNuvens>(UnityEngine.FindObjectsSortMode.None)) n.Atualizar(0f);
    var cp = cam.transform.position; var cf = cam.transform.forward;
    System.Func<UnityEngine.Renderer, float[]> faixa = r => {
        var b = r.bounds; float mn = float.MaxValue, mx = float.MinValue;
        for (int i = 0; i < 8; i++) {
            var pt = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            float d = UnityEngine.Vector3.Dot(pt - cp, cf); mn = System.Math.Min(mn, d); mx = System.Math.Max(mx, d);
        }
        return new float[]{ mn, mx };
    };
    float iMin = float.MaxValue, iMax = float.MinValue; string iMaxNome = ""; int nR = 0;
    foreach (var t in mundo.Ilhas) foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>()) {
        if (!r.enabled) continue; nR++;
        var f = faixa(r); if (f[0] < iMin) iMin = f[0]; if (f[1] > iMax) { iMax = f[1]; iMaxNome = r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name; }
    }
    // Lucca/inimigos tambem ficam sobre o anel
    float eMax = float.MinValue; foreach (var e in UnityEngine.Object.FindObjectsByType<Loopia.Hex.Inimigo>(UnityEngine.FindObjectsSortMode.None)) foreach (var r in e.GetComponentsInChildren<UnityEngine.Renderer>()) eMax = System.Math.Max(eMax, faixa(r)[1]);
    var o = new System.Collections.Generic.Dictionary<string, object>();
    o["resolucao"] = W + "x" + H; o["ilhasRenderers"] = nR; o["ilhasMin"] = iMin; o["ilhasMax"] = iMax; o["ilhasMaxQuem"] = iMaxNome; o["inimigosMax"] = eMax == float.MinValue ? (float?)null : eMax;
    o["cameraPos"] = new float[]{ cp.x, cp.y, cp.z }; o["ortho"] = cam.orthographicSize;
    System.Func<UnityEngine.GameObject, object> grupo = go => {
        if (go == null) return null;
        float mn = float.MaxValue, mx = float.MinValue; int n = 0;
        foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) { if (!r.enabled || !r.gameObject.activeInHierarchy) continue; var f = faixa(r); mn = System.Math.Min(mn, f[0]); mx = System.Math.Max(mx, f[1]); n++; }
        return new { renderers = n, min = mn, max = mx };
    };
    o["cenarioBackground"] = grupo(UnityEngine.GameObject.Find("Cenario background"));
    o["rio"] = grupo(UnityEngine.GameObject.Find("Rio de fundo"));
    o["nuvens"] = grupo(UnityEngine.GameObject.Find("Camada de nuvens"));
    var filhos = new System.Collections.Generic.List<object>();
    var cen = UnityEngine.GameObject.Find("Cenario background");
    if (cen != null) foreach (UnityEngine.Transform ch in cen.transform) { var q = grupo(ch.gameObject); filhos.Add(new { nome = ch.name, d = q }); }
    o["cenarioFilhos"] = filhos;
    return Newtonsoft.Json.JsonConvert.SerializeObject(o);
} finally {
    cam.targetTexture = prevTarget; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); cam.ResetAspect();
}
