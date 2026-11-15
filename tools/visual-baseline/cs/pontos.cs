// Play Mode. Pontos de amostragem da metrica de legibilidade (pixels da imagem, origem no canto superior esquerdo):
//  - topos: varios pontos dentro do topo de cada ilha (centro + 6 em 55% da apotema), so os nao ocluidos por outro renderer;
//  - cobertura: mascara em grade (passo %%PASSO%% px) dos pixels cobertos por renderers de ilha/heroi/inimigo (AABB, conservador).
// O fundo imediato e calculado no Python (faixa de pixels nao cobertos perto de pixels cobertos).
// Placeholders: %%W%%, %%H%%, %%PASSO%%
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
int W = %%W%%, H = %%H%%, PASSO = %%PASSO%%;
var prevTarget = cam.targetTexture;
var rt = new UnityEngine.RenderTexture(W, H, 24); rt.Create();
try {
    cam.targetTexture = rt; cam.aspect = (float)W / H;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    foreach (var r in UnityEngine.Object.FindObjectsByType<Loopia.Hex.RioDeFundo>(UnityEngine.FindObjectsSortMode.None)) r.Ajustar();
    var fundoRaizes = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (var nome in new string[]{ "Cenario background", "Rio de fundo", "Camada de nuvens", "Fundo de paisagem" }) { var go = UnityEngine.GameObject.Find(nome); if (go != null) fundoRaizes.Add(go.transform); }
    System.Func<UnityEngine.Transform, bool> ehFundo = tr => { foreach (var fr in fundoRaizes) if (tr.IsChildOf(fr)) return true; return false; };
    var oclusores = new System.Collections.Generic.List<UnityEngine.Renderer>();
    foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None)) {
        if (!(r is UnityEngine.MeshRenderer) && !(r is UnityEngine.SkinnedMeshRenderer)) continue;
        if (!r.enabled || !r.gameObject.activeInHierarchy || ehFundo(r.transform)) continue;
        if (r.bounds.size.magnitude > 30f) continue;
        oclusores.Add(r);
    }
    var fwd = cam.transform.forward;
    System.Func<UnityEngine.Vector3, UnityEngine.Vector2> aTela = p => { var s = cam.WorldToScreenPoint(p); return new UnityEngine.Vector2(s.x, H - s.y); };
    var topos = new System.Collections.Generic.List<float[]>();
    int tilesSemPonto = 0;
    foreach (var t in mundo.Ilhas) {
        var rs = t.GetComponentsInChildren<UnityEngine.Renderer>();
        UnityEngine.Renderer hex = t.GetComponent<UnityEngine.MeshRenderer>();
        if (hex == null) continue;
        var hb = hex.bounds; float yTopo = hb.max.y; float ap = hb.extents.x;
        var centro = new UnityEngine.Vector3(hb.center.x, yTopo, hb.center.z);
        var cand = new System.Collections.Generic.List<UnityEngine.Vector3>(); cand.Add(centro);
        for (int k = 0; k < 6; k++) { float a = k * 60f * UnityEngine.Mathf.Deg2Rad; cand.Add(centro + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a)) * ap * 0.55f); }
        int bons = 0;
        foreach (var p in cand) {
            bool ocl = false;
            var ray = new UnityEngine.Ray(p + (-fwd) * 0.02f, -fwd);
            foreach (var o in oclusores) {
                if (o == hex || (o.name == "IlhaFlutuante" && o.transform.IsChildOf(t.transform))) continue;
                float d; if (o.bounds.IntersectRay(ray, out d) && d >= 0f) { ocl = true; break; }
            }
            if (ocl) continue;
            var s = aTela(p);
            if (s.x < 4 || s.y < 4 || s.x > W - 4 || s.y > H - 4) continue;
            topos.Add(new float[]{ s.x, s.y }); bons++;
        }
        if (bons == 0) tilesSemPonto++;
    }
    // cobertura
    int gw = (W + PASSO - 1) / PASSO, gh = (H + PASSO - 1) / PASSO;
    var cob = new bool[gw * gh];
    foreach (var o in oclusores) {
        if (o.name == "Plane" || o.name == "Cube") continue;
        var b = o.bounds; float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < 8; i++) {
            var pt = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            var s = aTela(pt); x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); y0 = System.Math.Min(y0, s.y); y1 = System.Math.Max(y1, s.y);
        }
        int cx0 = System.Math.Max(0, (int)(x0 / PASSO)), cx1 = System.Math.Min(gw - 1, (int)(x1 / PASSO));
        int cy0 = System.Math.Max(0, (int)(y0 / PASSO)), cy1 = System.Math.Min(gh - 1, (int)(y1 / PASSO));
        for (int cy = cy0; cy <= cy1; cy++) for (int cx = cx0; cx <= cx1; cx++) {
            if (cob[cy * gw + cx]) continue;
            var ray = cam.ScreenPointToRay(new UnityEngine.Vector3((cx + 0.5f) * PASSO, H - (cy + 0.5f) * PASSO, 0f));
            float d; if (b.IntersectRay(ray, out d)) cob[cy * gw + cx] = true;
        }
    }
    var sb = new System.Text.StringBuilder();
    for (int cy = 0; cy < gh; cy++) { for (int cx = 0; cx < gw; cx++) sb.Append(cob[cy * gw + cx] ? '1' : '0'); sb.Append('\n'); }
    var res = new System.Collections.Generic.Dictionary<string, object>();
    res["w"] = W; res["h"] = H; res["passo"] = PASSO; res["gw"] = gw; res["gh"] = gh; res["topos"] = topos; res["tilesSemPonto"] = tilesSemPonto; res["ilhas"] = mundo.TotalDeIlhas;
    res["cobertura"] = sb.ToString(); res["oclusores"] = oclusores.Count;
    return Newtonsoft.Json.JsonConvert.SerializeObject(res);
} finally {
    cam.targetTexture = prevTarget; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); cam.ResetAspect();
}
