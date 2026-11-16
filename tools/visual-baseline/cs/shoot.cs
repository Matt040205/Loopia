// Play Mode. Renderiza a Main Camera num RenderTexture exato e salva JPG. Devolve metadados em JSON.
// Placeholders: %%W%%, %%H%%, %%OUT%% (caminho absoluto .jpg), %%SEED%%, %%HOUR%%
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
int W = %%W%%, H = %%H%%;
var prevTarget = cam.targetTexture;
int msaa = 1; var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; if (urp != null) msaa = urp.msaaSampleCount;
var rt = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
rt.antiAliasing = msaa;
rt.Create();
string metodo = "Camera.Render";
try {
    cam.aspect = (float)W / H;
    cam.targetTexture = rt;
    hic.Enquadrar();
    foreach (var f in UnityEngine.Object.FindObjectsByType<Loopia.Hex.FundoContinuo>(UnityEngine.FindObjectsSortMode.None)) f.Ajustar();
    foreach (var c in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CenarioContinuo>(UnityEngine.FindObjectsSortMode.None)) c.Ajustar();
    foreach (var n in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CamadaDeNuvens>(UnityEngine.FindObjectsSortMode.None)) n.Atualizar(0f);
    foreach (var r in UnityEngine.Object.FindObjectsByType<Loopia.Hex.RioDeFundo>(UnityEngine.FindObjectsSortMode.None)) r.Ajustar();
    cam.Render();
    var prevActive = UnityEngine.RenderTexture.active;
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = prevActive;
    System.IO.File.WriteAllBytes("%%OUT%%", tex.EncodeToJPG(85));
    // luminancia media para detectar imagem vazia
    var px = tex.GetPixels32(); long sum = 0; for (int i = 0; i < px.Length; i += 97) sum += px[i].r + px[i].g + px[i].b;
    float lumMedia = sum / (3f * (px.Length / 97 + 1));
    UnityEngine.Object.DestroyImmediate(tex);
    var meta = new System.Collections.Generic.Dictionary<string, object>();
    var t = cam.transform;
    meta["metodo"] = metodo;
    meta["seed"] = %%SEED%%; meta["hora"] = %%HOUR%%f; meta["largura"] = W; meta["altura"] = H;
    meta["aspect"] = cam.aspect; meta["orthographic"] = cam.orthographic; meta["orthographicSize"] = cam.orthographicSize;
    meta["cameraPos"] = new float[]{t.position.x, t.position.y, t.position.z};
    meta["cameraRotEuler"] = new float[]{t.eulerAngles.x, t.eulerAngles.y, t.eulerAngles.z};
    meta["near"] = cam.nearClipPlane; meta["far"] = cam.farClipPlane;
    meta["msaaRT"] = msaa;
    meta["timeScale"] = UnityEngine.Time.timeScale; meta["realtimeSinceStartup"] = UnityEngine.Time.realtimeSinceStartup; meta["frameCount"] = UnityEngine.Time.frameCount;
    var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
    meta["comprimentoDaVolta"] = g.Loop != null ? g.Loop.Count : -1; meta["ilhas"] = g.mundo.TotalDeIlhas;
    meta["inimigosVivos"] = UnityEngine.Object.FindObjectsByType<Loopia.Hex.Inimigo>(UnityEngine.FindObjectsSortMode.None).Length;
    meta["luminanciaMedia0a255"] = lumMedia;
    return Newtonsoft.Json.JsonConvert.SerializeObject(meta);
} finally {
    cam.targetTexture = prevTarget;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
    cam.ResetAspect();
}
