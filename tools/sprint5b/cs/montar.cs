// Sprint 5B. Play Mode, mapa ja gerado (setup_seed.cs), timeScale 0. Limpa os inimigos e monta uma fileira para
// comparacao: Lucca na casa Loop[0] e os itens de %%ITENS%% a direita da tela, um ao lado do outro.
// ITENS: lista "tipo:escala" separada por ';' (tipo = morcego | esfera | chefe | esferachefe | lobo | loboantigo);
// escala 0 = a do asset. %%PASSO%% = distancia na tela entre itens (u). Devolve JSON com o centro do grupo.
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var sp = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.InimigoSpawner>();
var lucca = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>();
var enc = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.EncontroDoChefe>();
var mundo = g.mundo;
sp.pausarSpawnComum = true;
sp.LimparTodos();
foreach (var a in UnityEngine.Object.FindObjectsByType<Loopia.Hex.AnimacaoDoInimigo>(UnityEngine.FindObjectsSortMode.None))
    if (a.Destacado) UnityEngine.Object.DestroyImmediate(a.gameObject);
var cam = UnityEngine.Camera.main;
var direita = cam.transform.right; direita.y = 0; direita.Normalize();
var paraCamera = -cam.transform.forward; paraCamera.y = 0; paraCamera.Normalize();
var p0 = mundo.PosicaoDe(g.Loop[0]) + direita * %%DESLOCAMENTO%%f;
var ag = lucca.GetComponent<UnityEngine.AI.NavMeshAgent>();
if (ag != null && ag.isOnNavMesh) ag.Warp(p0); else lucca.transform.position = p0;
lucca.transform.rotation = UnityEngine.Quaternion.LookRotation(paraCamera + direita * 0.6f);
var itens = "%%ITENS%%".Split(';');
float passo = %%PASSO%%f;
var res = new System.Collections.Generic.List<object>();
UnityEngine.Vector3 soma = p0; int n = 1;
for (int i = 0; i < itens.Length; i++) {
    var partes = itens[i].Split(':');
    string tipo = partes[0]; float esc = float.Parse(partes[1], System.Globalization.CultureInfo.InvariantCulture);
    Loopia.Hex.InimigoDef baseDef = tipo.StartsWith("lobo") ? sp.lobo : sp.morcego;
    Loopia.Hex.InimigoDef def;
    if (tipo == "chefe" || tipo == "esferachefe") def = enc.variante.Montar(sp.morcego);
    else def = UnityEngine.Object.Instantiate(baseDef);
    if (tipo == "esfera" || tipo == "esferachefe") def.prefab = null;
    if (tipo == "loboantigo") def.prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Modelos/PreFabs/Lobo.prefab");
    if (esc > 0) def.escala = esc;
    var inim = sp.Nascer(def, g.Loop[0]);
    var pos = p0 + direita * passo * (i + 1) + UnityEngine.Vector3.up * (def.voa ? def.alturaDeVoo : 0f);
    var agI = inim.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (agI != null) agI.enabled = false;
    inim.enabled = false;
    inim.transform.position = pos;
    inim.transform.rotation = UnityEngine.Quaternion.LookRotation(paraCamera - direita * 0.35f);
    var anim = inim.GetComponentInChildren<Loopia.Hex.AnimacaoDoInimigo>();
    if (anim != null) { anim.enabled = false; anim.transform.rotation = inim.transform.rotation; }
    soma += pos; n++;
    res.Add(new { tipo, escala = def.escala, nome = inim.name, x = pos.x, y = pos.y, z = pos.z, animado = anim != null });
}
var centro = soma / n;
return Newtonsoft.Json.JsonConvert.SerializeObject(new { centro = new float[]{ centro.x, centro.y, centro.z }, itens = res });
