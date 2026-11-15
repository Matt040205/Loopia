// Play Mode. Close de um ponto do mundo com a Main Camera (mesma rotacao, ortografica menor) e volta ao enquadramento.
// Placeholders: %%X%% %%Y%% %%Z%% (alvo), %%SIZE%% (orthographicSize), %%W%% %%H%%, %%OUT%% (jpg)
var cam = UnityEngine.Camera.main;
var hic = cam.GetComponent<Loopia.Hex.HexIsoCamera>();
hic.enabled = false;
var pos0 = cam.transform.position; float s0 = cam.orthographicSize;
int W = %%W%%, H = %%H%%;
var rt = new UnityEngine.RenderTexture(W, H, 24, UnityEngine.RenderTextureFormat.ARGB32, UnityEngine.RenderTextureReadWrite.sRGB);
rt.Create();
try {
    var alvo = new UnityEngine.Vector3(%%X%%f, %%Y%%f, %%Z%%f);
    cam.transform.position = alvo - cam.transform.forward * 70f;
    cam.orthographicSize = %%SIZE%%f;
    cam.aspect = (float)W / H;
    cam.targetTexture = rt;
    cam.Render();
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(W, H, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, W, H), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = prev;
    System.IO.File.WriteAllBytes("%%OUT%%", tex.EncodeToJPG(90));
    UnityEngine.Object.DestroyImmediate(tex);
} finally {
    cam.targetTexture = null; cam.ResetAspect();
    cam.transform.position = pos0; cam.orthographicSize = s0;
    hic.enabled = true;
    rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
}
return "ok";
