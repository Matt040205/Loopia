// Play Mode. Emula o DayNightController ANTERIOR ao Pass D1 (para o "antes" na mesma sessao e geometria):
// desliga o componente e aplica uma vez a logica antiga com os gradientes/curvas atuais da cena:
// sol girando de 0 a 360 (abaixo do horizonte depois das 18h), fog Exponential (densidade %%DENS%%) com cor = ceu,
// ambient Trilight = ceu / mistura / ceu*0.5. Placeholders: %%HOUR%%, %%DENS%%
var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
dn.enabled = false; dn.autoAdvanceTime = false; dn.timeOfDay = %%HOUR%%f;
float timeOfDay = dn.timeOfDay; float t = timeOfDay / 24f; var sun = dn.sunLight;
sun.color = dn.sunColorGradient.Evaluate(t); sun.intensity = dn.sunIntensityCurve.Evaluate(t);
float ax;
if (timeOfDay >= dn.sunriseHour && timeOfDay <= dn.sunsetHour) ax = UnityEngine.Mathf.Lerp(0f, 180f, UnityEngine.Mathf.InverseLerp(dn.sunriseHour, dn.sunsetHour, timeOfDay));
else {
    float nl = 24f - (dn.sunsetHour - dn.sunriseHour);
    float hs = timeOfDay < dn.sunriseHour ? (timeOfDay + 24f - dn.sunsetHour) : (timeOfDay - dn.sunsetHour);
    ax = UnityEngine.Mathf.Lerp(180f, 360f, hs / nl);
}
sun.transform.rotation = UnityEngine.Quaternion.Euler(ax, dn.sunAzimuth, 0f);
var sky = dn.skyColorGradient.Evaluate(t);
var cam = dn.targetCamera != null ? dn.targetCamera : UnityEngine.Camera.main;
if (cam != null && cam.clearFlags == UnityEngine.CameraClearFlags.SolidColor) cam.backgroundColor = sky;
UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
UnityEngine.RenderSettings.ambientSkyColor = sky;
UnityEngine.RenderSettings.ambientEquatorColor = UnityEngine.Color.Lerp(sky, sun.color, 0.3f);
UnityEngine.RenderSettings.ambientGroundColor = sky * 0.5f;
UnityEngine.RenderSettings.fog = true; UnityEngine.RenderSettings.fogMode = UnityEngine.FogMode.Exponential;
UnityEngine.RenderSettings.fogDensity = %%DENS%%f; UnityEngine.RenderSettings.fogColor = sky;
return "antigo hora=" + timeOfDay + " solX=" + ax + " int=" + sun.intensity;
