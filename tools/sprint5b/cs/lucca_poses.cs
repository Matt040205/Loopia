// Sprint 5B (bracos da Lucca). Edit Mode. Renderiza N poses de um clipe Humanoid da Lucca numa prancha PNG:
// PlayableGraph avalia o clipe no prefab LuccaVisual, cada SkinnedMeshRenderer e assado (o PreviewRenderUtility nao
// desenha malha com skin) e o PreviewRenderUtility renderiza de lado e de tres quartos.
// Placeholders: %%CLIPE%% (nome do .anim em Assets/Modelos/Lucca/Animacoes), %%N%% (poses), %%OUT%% (png)
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Modelos/Lucca/LuccaVisual.prefab");
var clipe = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Modelos/Lucca/Animacoes/%%CLIPE%%.anim");
int N = %%N%%, W = 256, H = 320;
var prancha = new Texture2D(W * N, H * 2, TextureFormat.RGB24, false);
var inst = (GameObject)UnityEngine.Object.Instantiate(prefab);
inst.hideFlags = HideFlags.HideAndDontSave;
var malhas = new System.Collections.Generic.List<Mesh>();
try {
    var an = inst.GetComponentInChildren<Animator>();
    var grafo = UnityEngine.Playables.PlayableGraph.Create("poses");
    var saida = UnityEngine.Animations.AnimationPlayableOutput.Create(grafo, "s", an);
    var tocador = UnityEngine.Animations.AnimationClipPlayable.Create(grafo, clipe);
    tocador.SetApplyFootIK(false);
    UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(saida, tocador); // sem using no execute_code
    grafo.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
    for (int i = 0; i < N; i++) {
        float t = clipe.length * i / N;
        UnityEngine.Playables.PlayableExtensions.SetTime(tocador, (double)t); grafo.Evaluate(0f);
        for (int vista = 0; vista < 2; vista++) {
            var previa = new PreviewRenderUtility();
            var raiz = new GameObject("pose");
            try {
                previa.AddSingleGO(raiz);
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    if (!smr.enabled || !smr.gameObject.activeInHierarchy) continue;
                    var m = new Mesh(); smr.BakeMesh(m, false); malhas.Add(m);
                    var go = new GameObject(smr.name); go.transform.SetParent(raiz.transform, false);
                    go.transform.position = inst.transform.InverseTransformPoint(smr.transform.position);
                    go.transform.rotation = Quaternion.Inverse(inst.transform.rotation) * smr.transform.rotation;
                    // BakeMesh(false) ja devolve em unidades do mundo (o FBX vem em cm com escala 66 nos pais)
                    go.AddComponent<MeshFilter>().sharedMesh = m;
                    go.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                }
                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>()) {
                    var mf = mr.GetComponent<MeshFilter>(); if (mf == null || !mr.enabled) continue;
                    var go = new GameObject(mr.name); go.transform.SetParent(raiz.transform, false);
                    go.transform.position = inst.transform.InverseTransformPoint(mr.transform.position);
                    go.transform.rotation = Quaternion.Inverse(inst.transform.rotation) * mr.transform.rotation;
                    go.transform.localScale = mr.transform.lossyScale;
                    go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    go.AddComponent<MeshRenderer>().sharedMaterials = mr.sharedMaterials;
                }
                var cam = previa.camera;
                cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.78f, .82f, .86f);
                cam.nearClipPlane = .05f; cam.farClipPlane = 50f;
                Vector3 dir = vista == 0 ? new Vector3(1f, .12f, 0f).normalized : new Vector3(.55f, .35f, .76f).normalized;
                Vector3 alvo = new Vector3(0f, 0.55f, 0f);
                cam.transform.position = alvo + dir * 10f; cam.transform.rotation = Quaternion.LookRotation(-dir);
                cam.orthographicSize = .7f;
                previa.lights[0].intensity = 1.6f; previa.lights[0].transform.rotation = Quaternion.LookRotation(new Vector3(-.4f, -.8f, -.4f));
                previa.lights[1].intensity = .8f;
                previa.ambientColor = new Color(.55f, .58f, .62f);
                previa.BeginPreview(new Rect(0, 0, W, H), GUIStyle.none);
                previa.Render(true, false);
                var rt = (RenderTexture)previa.EndPreview();
                var antes = RenderTexture.active; RenderTexture.active = rt;
                prancha.ReadPixels(new Rect(0, 0, W, H), i * W, (1 - vista) * H);
                RenderTexture.active = antes;
            } finally { previa.Cleanup(); UnityEngine.Object.DestroyImmediate(raiz); }
        }
    }
    grafo.Destroy();
    prancha.Apply();
    System.IO.File.WriteAllBytes("%%OUT%%", prancha.EncodeToPNG());
} finally {
    UnityEngine.Object.DestroyImmediate(inst);
    foreach (var m in malhas) UnityEngine.Object.DestroyImmediate(m);
    UnityEngine.Object.DestroyImmediate(prancha);
}
return "ok";
