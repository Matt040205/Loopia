// Play Mode, Editor. Amostra UnityStats + contadores de frame. Placeholders: %%MODE%% = "start" | "sample"
var o = new System.Collections.Generic.Dictionary<string, object>();
o["frameCount"] = UnityEngine.Time.frameCount; o["realtime"] = UnityEngine.Time.realtimeSinceStartupAsDouble;
o["unscaledDeltaTime"] = UnityEngine.Time.unscaledDeltaTime; o["smoothDeltaTime"] = UnityEngine.Time.smoothDeltaTime;
o["batches"] = UnityEditor.UnityStats.batches; o["drawCalls"] = UnityEditor.UnityStats.drawCalls; o["setPassCalls"] = UnityEditor.UnityStats.setPassCalls;
o["triangles"] = UnityEditor.UnityStats.triangles; o["vertices"] = UnityEditor.UnityStats.vertices; o["shadowCasters"] = UnityEditor.UnityStats.shadowCasters;
o["renderTextureChanges"] = UnityEditor.UnityStats.renderTextureChanges; o["dynamicBatchedDrawCalls"] = UnityEditor.UnityStats.dynamicBatchedDrawCalls; o["staticBatchedDrawCalls"] = UnityEditor.UnityStats.staticBatchedDrawCalls;
o["frameTimeMs_UnityStats"] = UnityEditor.UnityStats.frameTime * 1000f; o["renderTimeMs_UnityStats"] = UnityEditor.UnityStats.renderTime * 1000f;
o["screenRes"] = UnityEditor.UnityStats.screenRes; o["usedTextureMemorySize"] = UnityEditor.UnityStats.usedTextureMemorySize;
return Newtonsoft.Json.JsonConvert.SerializeObject(o);
