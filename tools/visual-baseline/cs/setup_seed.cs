// Play Mode. Aplica semente fixa, regenera o mapa e congela o tempo. Placeholder: %%SEED%%
UnityEngine.Application.runInBackground = true;
UnityEngine.Time.timeScale = 1f;
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
g.usarSementeFixa = true;
g.semente = %%SEED%%;
bool ok = g.Gerar();
var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
dn.autoAdvanceTime = false;
UnityEngine.Time.timeScale = 0f;
return Newtonsoft.Json.JsonConvert.SerializeObject(new { gerou = ok, ilhas = g.mundo.TotalDeIlhas, volta = g.Loop != null ? g.Loop.Count : -1 });
