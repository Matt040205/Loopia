// Play Mode. Metricas do MedidorDaPartida no fim da jornada.
var medidor = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.MedidorDaPartida>();
var encontro = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.EncontroDoChefe>();
var m = medidor.Metricas;
var voltas = new System.Collections.Generic.List<object>();
foreach (var v in m.voltas) voltas.Add(new { v.numero, v.duracao, v.flechasDisparadas, v.danoRecebido, v.inimigosDerrotados, v.pvAoFechar });
return Newtonsoft.Json.JsonConvert.SerializeObject(new {
    m.resultado, m.duracaoTotal, m.casasNoAnel, m.chefeComecou, m.duracaoDoChefe, m.flechasNoChefe,
    m.flechasEmOutrosNoChefe, m.recargasNoChefe, m.danoRecebidoNoChefe, m.pvNoInicioDoChefe, m.revoadas,
    m.morcegosDaRevoada, m.tempoSemFlechasNoChefe, m.maiorTrechoSemFlechasNoChefe,
    chefePvNoFim = encontro != null && encontro.Chefe != null ? encontro.Chefe.PvAtual : -1,
    voltas, resumo = m.Resumo() });
