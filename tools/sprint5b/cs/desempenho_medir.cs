// Sprint 5B. Play Mode. %%ACAO%% = iniciar | ler. "iniciar" abre ProfilerRecorders (marcadores de animacao, skinning,
// frame do main thread, GC por frame) guardados no AppDomain entre chamadas; "ler" devolve medias das ultimas amostras e
// fecha os recorders. Tambem le UnityStats e, se houver, o FrameTimingManager (CPU/GPU). Uma chamada nao renderiza nada.
var dom = System.AppDomain.CurrentDomain;
var nomes = new[] {
    "Main Thread", "PlayerLoop", "Animators.Update", "Animator.Update", "PreLateUpdate.DirectorUpdateAnimationBegin",
    "PreLateUpdate.DirectorUpdateAnimationEnd", "MeshSkinning.Update", "Skinning.Update", "MeshSkinning.SkinOnGPU",
    "Animators.ProcessGraphJob", "Animators.WriteJob", "GC Allocated In Frame", "System Used Memory", "Gfx Used Memory",
    "Total Used Memory", "SetPass Calls Count", "Draw Calls Count", "Batches Count", "Triangles Count"
};
if ("%%ACAO%%" == "iniciar") {
    var lista = new System.Collections.Generic.List<object>();
    var handles = new System.Collections.Generic.List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
    Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
    foreach (var h in handles) {
        string n = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h).Name;
        if (System.Array.IndexOf(nomes, n) < 0 || dom.GetData("s5b_" + n) != null) continue;
        var r = new Unity.Profiling.ProfilerRecorder(h, 240, Unity.Profiling.ProfilerRecorderOptions.Default);
        r.Start();
        dom.SetData("s5b_" + n, r);
        lista.Add(n);
    }
    UnityEngine.FrameTimingManager.CaptureFrameTimings();
    return Newtonsoft.Json.JsonConvert.SerializeObject(lista);
}
var o = new System.Collections.Generic.Dictionary<string, object>();
foreach (var nome in nomes) {
    var obj = dom.GetData("s5b_" + nome);
    if (obj == null) continue;
    var r = (Unity.Profiling.ProfilerRecorder)obj;
    if (!r.Valid || r.Count == 0) { o[nome] = "indisponivel"; r.Dispose(); dom.SetData("s5b_" + nome, null); continue; }
    double soma = 0; long max = 0; int n = r.Count;
    for (int i = 0; i < n; i++) { long v = r.GetSample(i).Value; soma += v; if (v > max) max = v; }
    bool tempo = r.UnitType == Unity.Profiling.ProfilerMarkerDataUnit.TimeNanoseconds;
    o[nome] = new { amostras = n, media = tempo ? soma / n / 1e6 : soma / n, max = tempo ? max / 1e6 : max, unidade = tempo ? "ms" : r.UnitType.ToString() };
    r.Dispose(); dom.SetData("s5b_" + nome, null);
}
UnityEngine.FrameTimingManager.CaptureFrameTimings();
var ft = new UnityEngine.FrameTiming[30];
uint got = UnityEngine.FrameTimingManager.GetLatestTimings(30, ft);
if (got > 0) {
    double cpu = 0, gpu = 0; for (int i = 0; i < got; i++) { cpu += ft[i].cpuFrameTime; gpu += ft[i].gpuFrameTime; }
    o["FrameTimingManager"] = new { quadros = got, cpuMs = cpu / got, gpuMs = gpu / got };
} else o["FrameTimingManager"] = "sem dados (desligado no Editor/plataforma)";
o["UnityStats"] = new { batches = UnityEditor.UnityStats.batches, drawCalls = UnityEditor.UnityStats.drawCalls,
    setPass = UnityEditor.UnityStats.setPassCalls, triangulos = UnityEditor.UnityStats.triangles,
    vertices = UnityEditor.UnityStats.vertices, frameMs = UnityEditor.UnityStats.frameTime * 1000f,
    renderMs = UnityEditor.UnityStats.renderTime * 1000f, skinnedMeshesVisiveis = UnityEditor.UnityStats.visibleSkinnedMeshes };
o["memoriaAlocadaMB"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576.0;
o["materiais"] = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Material>().Length;
return Newtonsoft.Json.JsonConvert.SerializeObject(o);
