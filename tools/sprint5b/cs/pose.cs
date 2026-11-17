// Sprint 5B. Play Mode. Poe os visuais animados num estado/tempo e avalia o Animator na hora (funciona com timeScale 0).
// %%ESTADO%% (Idle, Fly, Attack, Hit, Death, Revoada, Locomocao), %%T%% (0..1 normalizado), %%VEL%% (Velocidade do lobo),
// %%FAMILIA%%: filtra pelo nome do prefab (vazio = todos).
int mexidos = 0;
foreach (var a in UnityEngine.Object.FindObjectsByType<Loopia.Hex.AnimacaoDoInimigo>(UnityEngine.FindObjectsSortMode.None)) {
    if (!string.IsNullOrEmpty("%%FAMILIA%%") && !a.name.StartsWith("%%FAMILIA%%")) continue;
    a.enabled = false;
    var an = a.animator; if (an == null) continue;
    string est = "%%ESTADO%%"; float t = %%T%%f;
    bool patas = a.locomocao == Loopia.Hex.AnimacaoDoInimigo.TipoDeLocomocao.Patas;
    if (est == "Revoada" && !a.temRevoada) est = "Fly";
    if (est == "Fly" && patas) est = "Locomocao";
    if (est == "Locomocao" && !patas) est = "Fly";
    if (patas) { an.SetFloat("Velocidade", %%VEL%%f); an.SetFloat("Ritmo", 1f); }
    an.SetFloat("TempoDoAtaque", est == "Attack" ? t : 0f);
    if (a.temRevoada) an.SetFloat("TempoDaRevoada", est == "Revoada" ? t : 0f);
    if (est == "Hit") { an.Play("Idle", 0, 0.1f); an.Play("Hit", 1, t); }
    else { an.Play(est, 0, t); an.Play("Vazio", 1, 0f); }
    an.Update(0f);
    mexidos++;
}
return mexidos;
