// Sprint 5B. Play Mode, mapa gerado. Monta a carga maxima prevista: %%MORCEGOS%% morcegos (maximoDeMorcegos), %%LOBOS%%
// lobos e o chefe, nas casas reais do anel, com o visual %%VISUAL%% = novo (modelos animados) | antigo (esfera / Lobo.prefab).
// A logica dos inimigos fica congelada (enabled=false: ninguem ataca nem morre durante a medida); os visuais animados
// continuam animando (Idle/voo), que e o custo que interessa. Lucca parado.
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var sp = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.InimigoSpawner>();
var enc = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.EncontroDoChefe>();
var corr = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopRunner>();
corr.andando = false;
sp.pausarSpawnComum = true; sp.nascerMorcegos = false;
sp.LimparTodos();
bool novo = "%%VISUAL%%" == "novo";
System.Func<Loopia.Hex.InimigoDef, Loopia.Hex.InimigoDef> copia = d => {
    var c = UnityEngine.Object.Instantiate(d);
    if (!novo) c.prefab = d == sp.lobo ? UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Modelos/PreFabs/Lobo.prefab") : null;
    if (!novo && d == sp.morcego) c.escala = 0.6f;
    return c;
};
var defM = copia(sp.morcego); var defL = copia(sp.lobo);
var defC = enc.variante.Montar(sp.morcego);
if (!novo) { defC.prefab = null; defC.escala = 3f; }
int nM = %%MORCEGOS%%, nL = %%LOBOS%%, n = g.Loop.Count, k = 0;
for (int i = 0; i < nM; i++) { var x = sp.Nascer(defM, g.Loop[(2 + i * n / nM) % n]); x.enabled = false; k++; }
for (int i = 0; i < nL; i++) { var x = sp.Nascer(defL, g.Loop[(5 + i * n / System.Math.Max(1, nL)) % n]); x.enabled = false; k++; }
var chefe = sp.Nascer(defC, g.CentroDoMiolo); chefe.enabled = false; k++;
int skinned = 0; foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.SkinnedMeshRenderer>(UnityEngine.FindObjectsSortMode.None)) if (r.GetComponentInParent<Loopia.Hex.Inimigo>() != null) skinned++;
int animators = 0; foreach (var a in UnityEngine.Object.FindObjectsByType<UnityEngine.Animator>(UnityEngine.FindObjectsSortMode.None)) if (a.GetComponentInParent<Loopia.Hex.Inimigo>() != null) animators++;
UnityEngine.Time.timeScale = 1f;
return Newtonsoft.Json.JsonConvert.SerializeObject(new { visual = "%%VISUAL%%", inimigos = k, skinned, animators });
