// Sprint 5B. Play Mode. Hora fixa no DayNightController. %%HOUR%%
var dn = UnityEngine.Object.FindFirstObjectByType<DayNightController>();
dn.autoAdvanceTime = false; dn.timeOfDay = %%HOUR%%f;
return "ok";
