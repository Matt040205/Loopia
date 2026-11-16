// Play Mode. Forca hora e aspecto/enquadramento; depois deixe alguns frames rodarem.
// Placeholders: %%HOUR%% (float, ponto decimal), %%W%%, %%H%%
var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
dn.autoAdvanceTime = false;
dn.timeOfDay = %%HOUR%%f;
var cam = UnityEngine.Camera.main;
cam.aspect = (float)%%W%% / %%H%%;
cam.GetComponent<Loopia.Hex.HexIsoCamera>().Enquadrar();
return "ok";
