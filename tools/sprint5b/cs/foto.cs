// Sprint 5B. Play Mode. UM render da Main Camera por chamada (varios Camera.Render() num execute_code ja derrubaram o
// D3D12). %%MODO%% = cheio (enquadramento oficial) ou close (mesma rotacao, ortografica %%SIZE%% centrada em
// %%X%%,%%Y%%,%%Z%%). %%W%%x%%H%%, %%OUT%% (.png ou .jpg).
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
var pos0 = cam.transform.position; float s0 = cam.orthographicSize; bool hicAntes = hic.enabled;
int W = %%W%%, H = %%H%%;
int msaa = 1; var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset; if (urp != null) msaa = urp.msaaSampleCount;
var rt = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
rt.antiAliasing = msaa; rt.Create();
try {
    cam.aspect = (float)W / H;
    if ("%%MODO%%" == "close") {
        hic.enabled = false;
        var alvo = new UnityEngine.Vector3(%%X%%f, %%Y%%f, %%Z%%f);
        cam.transform.position = alvo - cam.transform.forward * 70f;
        cam.orthographicSize = %%SIZE%%f;
    } else hic.Enquadrar();
    foreach (var nv in UnityEngine.Object.FindObjectsByType<Loopia.Hex.CamadaDeNuvens>(UnityEngine.FindObjectsSortMode.None)) nv.Atualizar(0f);
    cam.targetTexture = rt;
    cam.Render();
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    string saida = "%%OUT%%";
    System.IO.File.WriteAllBytes(saida, saida.EndsWith(".png") ? tex.EncodeToPNG() : tex.EncodeToJPG(90));
    UnityEngine.Object.DestroyImmediate(tex);
} finally {
    cam.targetTexture = null; cam.ResetAspect();
    cam.transform.position = pos0; cam.orthographicSize = s0; hic.enabled = hicAntes;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
}
return "ok";
