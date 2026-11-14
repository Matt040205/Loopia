// Play Mode. Snapshot de iluminacao/fog/volume. Rode depois de forcar a hora e deixar frames passarem.
var cam = UnityEngine.Camera.main;
var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
var sun = dn.sunLight;
System.Func<UnityEngine.Color, float[]> c4 = c => new float[]{ c.r, c.g, c.b, c.a };
var o = new System.Collections.Generic.Dictionary<string, object>();
o["timeOfDay"] = dn.timeOfDay; o["sunriseHour"] = dn.sunriseHour; o["sunsetHour"] = dn.sunsetHour; o["autoAdvance"] = dn.autoAdvanceTime; o["syncFog"] = dn.syncFog; o["syncAmbient"] = dn.syncAmbientLight; o["fogDensityDoController"] = dn.fogDensity;
o["sol"] = new { nome = sun.name, tipo = sun.type.ToString(), rotEuler = new float[]{ sun.transform.eulerAngles.x, sun.transform.eulerAngles.y, sun.transform.eulerAngles.z }, cor = c4(sun.color), intensidade = sun.intensity, sombras = sun.shadows.ToString(), forcaSombra = sun.shadowStrength, temperatura = sun.useColorTemperature };
var rs = UnityEngine.RenderSettings.ambientMode;
o["ambiente"] = new { modo = rs.ToString(), sky = c4(UnityEngine.RenderSettings.ambientSkyColor), equator = c4(UnityEngine.RenderSettings.ambientEquatorColor), ground = c4(UnityEngine.RenderSettings.ambientGroundColor), intensidade = UnityEngine.RenderSettings.ambientIntensity, luzAmbienteFlat = c4(UnityEngine.RenderSettings.ambientLight), skybox = UnityEngine.RenderSettings.skybox != null ? UnityEngine.RenderSettings.skybox.name : "null", sunSource = UnityEngine.RenderSettings.sun != null ? UnityEngine.RenderSettings.sun.name : "null" };
o["fog"] = new { ligado = UnityEngine.RenderSettings.fog, modo = UnityEngine.RenderSettings.fogMode.ToString(), cor = c4(UnityEngine.RenderSettings.fogColor), densidade = UnityEngine.RenderSettings.fogDensity, inicio = UnityEngine.RenderSettings.fogStartDistance, fim = UnityEngine.RenderSettings.fogEndDistance };
o["camera"] = new { clearFlags = cam.clearFlags.ToString(), background = c4(cam.backgroundColor), fov = cam.fieldOfView, orthographic = cam.orthographic, allowHDR = cam.allowHDR, allowMSAA = cam.allowMSAA };
var ac = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
if (ac != null) o["cameraURP"] = new { renderPostProcessing = ac.renderPostProcessing, antialiasing = ac.antialiasing.ToString(), renderShadows = ac.renderShadows, volumeMask = ac.volumeLayerMask.value };
var vols = new System.Collections.Generic.List<object>();
foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None)) {
    var prof = v.sharedProfile;
    var comps = new System.Collections.Generic.List<object>();
    if (prof != null) foreach (var vc in prof.components) {
        var pars = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var f in vc.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
            var p = f.GetValue(vc) as UnityEngine.Rendering.VolumeParameter;
            if (p == null) continue;
            string val = "?";
            var pv = p.GetType().GetProperty("value");
            if (pv != null) { var x = pv.GetValue(p, null); val = x == null ? "null" : (x is UnityEngine.Color ? ((UnityEngine.Color)x).ToString("F3") : System.Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture)); }
            pars[f.Name + (p.overrideState ? " [override]" : " [default]")] = val;
        }
        comps.Add(new { tipo = vc.GetType().Name, ativo = vc.active, parametros = pars });
    }
    vols.Add(new { objeto = v.name, global = v.isGlobal, peso = v.weight, prioridade = v.priority, perfil = prof != null ? prof.name : "null", componentes = comps });
}
o["volumes"] = vols;
return Newtonsoft.Json.JsonConvert.SerializeObject(o);
