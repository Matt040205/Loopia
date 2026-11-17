// Sprint 5B. Play Mode. Tamanho na tela (px no enquadramento oficial %%W%%x%%H%%) da malha deformada de cada inimigo e
// da Lucca (BakeMesh dos SkinnedMeshRenderer; MeshFilter para a esfera antiga).
var cam = UnityEngine.Camera.main; var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
cam.aspect = (float)%%W%% / %%H%%; hic.Enquadrar();
float escalaPx = %%H%% / (2f * cam.orthographicSize);
var lista = new System.Collections.Generic.List<object>();
System.Func<UnityEngine.GameObject, float[]> medir = go => {
    float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
    var vs = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var r in go.GetComponentsInChildren<UnityEngine.Renderer>()) {
        if (!r.enabled) continue;
        UnityEngine.Mesh m = null;
        var smr = r as UnityEngine.SkinnedMeshRenderer;
        if (smr != null) { m = new UnityEngine.Mesh(); smr.BakeMesh(m, true); }
        else { var mf = r.GetComponent<UnityEngine.MeshFilter>(); if (mf != null) m = mf.sharedMesh; }
        if (m == null) continue;
        m.GetVertices(vs);
        foreach (var v in vs) {
            var w = r.transform.TransformPoint(v);
            var s = cam.transform.InverseTransformPoint(w) * escalaPx;
            x0 = System.Math.Min(x0, s.x); x1 = System.Math.Max(x1, s.x); y0 = System.Math.Min(y0, s.y); y1 = System.Math.Max(y1, s.y);
        }
        if (smr != null) UnityEngine.Object.DestroyImmediate(m);
    }
    return new float[]{ x1 - x0, y1 - y0 };
};
var lucca = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>();
lista.Add(new { nome = "Lucca", escala = 0f, px = medir(lucca.gameObject) });
foreach (var i in UnityEngine.Object.FindObjectsByType<Loopia.Hex.Inimigo>(UnityEngine.FindObjectsSortMode.InstanceID))
    lista.Add(new { nome = i.name, escala = i.Def.escala, px = medir(i.gameObject) });
cam.ResetAspect();
return Newtonsoft.Json.JsonConvert.SerializeObject(new { ortho = cam.orthographicSize, pxPorUnidade = escalaPx, itens = lista });
