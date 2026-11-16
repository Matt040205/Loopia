// Play Mode. Replica HexIsoCamera.MedirIlhas para varias populacoes de renderers e aspectos.
// A = todos os renderers (formula real); B = sem decoracoes; C = so modelo da ilha; D = so MeshRenderer do hexagono.
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
var res = new System.Collections.Generic.List<object>();
var rot = UnityEngine.Quaternion.Euler(hic.inclinacao, hic.giro, 0f);
var inv = UnityEngine.Quaternion.Inverse(rot);
UnityEngine.Vector3 soma = UnityEngine.Vector3.zero; int n = 0;
foreach (var t in mundo.Ilhas) { soma += t.transform.position; n++; }
var centro = soma / n;
// calc(filtro) -> {maiorX, maiorY, rendererDoMaiorX, rendererDoMaiorY}
System.Func<int, float[]> calc = modo => {
    float mx = 0f, my = 0f;
    foreach (var ilha in mundo.Ilhas) {
        foreach (var r in ilha.GetComponentsInChildren<UnityEngine.Renderer>()) {
            bool emDeco = false;
            var p = r.transform;
            while (p != null && p != ilha.transform) { if (p.name == "Decoracoes") { emDeco = true; break; } p = p.parent; }
            bool ehHex = r.gameObject == ilha.gameObject;
            if (modo == 1 && emDeco) continue;
            if (modo == 2 && (emDeco || ehHex)) continue;
            if (modo == 3 && !ehHex) continue;
            if (modo == 4 && !emDeco) continue;
            var b = r.bounds;
            for (int i = 0; i < 8; i++) {
                var pt = b.center + UnityEngine.Vector3.Scale(b.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var c = inv * (pt - centro);
                mx = System.Math.Max(mx, System.Math.Abs(c.x)); my = System.Math.Max(my, System.Math.Abs(c.y));
            }
        }
    }
    return new float[]{ mx, my };
};
var prevTarget = cam.targetTexture;
var sizes = new int[][]{ new int[]{1920,1080}, new int[]{2520,1080} };
var vars = new string[]{ "A_todos_renderers", "B_sem_decoracoes", "C_so_modelo_da_ilha", "D_so_hex_mesh", "E_so_decoracoes" };
try {
    foreach (var s in sizes) {
        var rt = new UnityEngine.RenderTexture(s[0], s[1], 24); rt.Create();
        cam.targetTexture = rt; cam.aspect = (float)s[0] / s[1];
        hic.Enquadrar();
        float real = cam.orthographicSize;
        float aspecto = cam.aspect;
        float folga = mundo.tamanhoDoHexagono;
        var d = new System.Collections.Generic.Dictionary<string, object>();
        d["resolucao"] = s[0] + "x" + s[1]; d["aspect"] = aspecto; d["orthoSizeReal_Enquadrar"] = real; d["margem"] = hic.margem; d["folga"] = folga;
        for (int m = 0; m < 5; m++) {
            var mm = calc(m);
            float mxx = mm[0] + folga, myy = mm[1] + folga;
            float o = System.Math.Max(myy, mxx / aspecto) * hic.margem;
            d[vars[m]] = new { maiorX = mm[0], maiorY = mm[1], comFolgaX = mxx, comFolgaY = myy, orthoSize = o, limitadoPor = (myy >= mxx / aspecto) ? "altura(Y)" : "largura(X)" };
        }
        res.Add(d);
        cam.targetTexture = prevTarget;
        rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    }
} finally {
    cam.targetTexture = prevTarget;
    cam.ResetAspect();
}
return Newtonsoft.Json.JsonConvert.SerializeObject(new { seed = %%SEED%%, centro = new float[]{centro.x, centro.y, centro.z}, aspectos = res });
