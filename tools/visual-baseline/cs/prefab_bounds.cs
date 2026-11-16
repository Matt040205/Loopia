// Edit Mode. Mede bounds crus (sem escala) de prefabs/FBX sem instanciar nada na cena.
// Placeholders: %%PATHS%% = lista de caminhos separados por |
var outList = new System.Collections.Generic.List<object>();
var paths = "%%PATHS%%".Split('|');
foreach (var p in paths) {
    var go = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p);
    if (go == null) { outList.Add(new System.Collections.Generic.Dictionary<string, object>{{"path",p},{"erro","nao carregou"}}); continue; }
    bool anyMF = false, anySMR = false, firstAll = true, firstMF = true;
    UnityEngine.Bounds bAll = new UnityEngine.Bounds(), bMF = new UnityEngine.Bounds();
    int tris = 0, meshes = 0;
    var mats = new System.Collections.Generic.List<string>();
    var shaders = new System.Collections.Generic.HashSet<string>();
    var stack = new System.Collections.Generic.Stack<object[]>();
    stack.Push(new object[]{ go.transform, UnityEngine.Matrix4x4.identity });
    var rootScale = go.transform.localScale;
    while (stack.Count > 0) {
        var it = stack.Pop();
        var t = (UnityEngine.Transform)it[0];
        var parentM = (UnityEngine.Matrix4x4)it[1];
        var m = t == go.transform ? UnityEngine.Matrix4x4.identity : parentM * UnityEngine.Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
        UnityEngine.Mesh mesh = null; bool isSkin = false; UnityEngine.Bounds lb = new UnityEngine.Bounds(); bool has = false;
        var mf = t.GetComponent<UnityEngine.MeshFilter>();
        var smr = t.GetComponent<UnityEngine.SkinnedMeshRenderer>();
        var mr = t.GetComponent<UnityEngine.Renderer>();
        if (mf != null && mf.sharedMesh != null) { mesh = mf.sharedMesh; lb = mesh.bounds; has = true; }
        else if (smr != null && smr.sharedMesh != null) { mesh = smr.sharedMesh; lb = smr.localBounds; isSkin = true; has = true; }
        if (has) {
            meshes++; for (int s = 0; s < mesh.subMeshCount; s++) tris += (int)(mesh.GetIndexCount(s) / 3);
            if (isSkin) anySMR = true; else anyMF = true;
            if (mr != null) foreach (var sm in mr.sharedMaterials) { if (sm != null) { mats.Add(sm.name); if (sm.shader != null) shaders.Add(sm.shader.name); } }
            for (int i = 0; i < 8; i++) {
                var c = lb.center + UnityEngine.Vector3.Scale(lb.extents, new UnityEngine.Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var w = m.MultiplyPoint3x4(c);
                if (firstAll) { bAll = new UnityEngine.Bounds(w, UnityEngine.Vector3.zero); firstAll = false; } else bAll.Encapsulate(w);
                if (!isSkin) { if (firstMF) { bMF = new UnityEngine.Bounds(w, UnityEngine.Vector3.zero); firstMF = false; } else bMF.Encapsulate(w); }
            }
        }
        foreach (UnityEngine.Transform ch in t) stack.Push(new object[]{ ch, m });
    }
    var d = new System.Collections.Generic.Dictionary<string, object>();
    d["path"] = p; d["nome"] = go.name;
    d["rootScale"] = new float[]{rootScale.x, rootScale.y, rootScale.z};
    d["usaMeshFilter"] = anyMF; d["usaSkinnedMeshRenderer"] = anySMR; d["malhas"] = meshes; d["triangulos"] = tris;
    d["boundsCru"] = firstAll ? null : new { center = new float[]{bAll.center.x,bAll.center.y,bAll.center.z}, size = new float[]{bAll.size.x,bAll.size.y,bAll.size.z}, min = new float[]{bAll.min.x,bAll.min.y,bAll.min.z}, max = new float[]{bAll.max.x,bAll.max.y,bAll.max.z} };
    d["boundsSoMeshFilter_usadoPorVisualDeIlha"] = firstMF ? null : new { center = new float[]{bMF.center.x,bMF.center.y,bMF.center.z}, size = new float[]{bMF.size.x,bMF.size.y,bMF.size.z}, min = new float[]{bMF.min.x,bMF.min.y,bMF.min.z}, max = new float[]{bMF.max.x,bMF.max.y,bMF.max.z} };
    if (!firstAll) {
        float h = bAll.size.y;
        string pv = System.Math.Abs(bAll.min.y) < 0.02f * System.Math.Max(h, 0.0001f) ? "base (min.y ~ 0)" : (System.Math.Abs(bAll.center.y) < 0.02f * System.Math.Max(h, 0.0001f) ? "centro (center.y ~ 0)" : (System.Math.Abs(bAll.max.y) < 0.02f * System.Math.Max(h, 0.0001f) ? "topo (max.y ~ 0)" : "outro"));
        d["pivoVerticalRelativoAoBounds"] = pv;
        d["pivoHorizontalDesvio_xz"] = new float[]{bAll.center.x, bAll.center.z};
    }
    d["materiais"] = mats; d["shaders"] = shaders;
    outList.Add(d);
}
return Newtonsoft.Json.JsonConvert.SerializeObject(outList);
