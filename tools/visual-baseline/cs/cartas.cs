// Play Mode (Pass C). Coloca as 7 cartas com marcador (+ a Mola que a Ilha alta exige) no mapa ja gerado.
// Cartas do caminho em casas do anel; cartas de fora em casas vazias vizinhas do anel (nunca no miolo).
// Devolve JSON com a casa e a posicao de mundo de cada carta (para closes). Sem placeholders.
var mapa = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.MapaDePlataformas>();
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mundo = g.mundo;
mapa.LimparTudo();
System.Func<string, Loopia.Hex.PlataformaDef> D = nomeDaCarta => UnityEditor.AssetDatabase.LoadAssetAtPath<Loopia.Hex.PlataformaDef>("Assets/Dados/Plataformas/" + nomeDaCarta + ".asset");
var loop = g.Loop;
int n = loop.Count;
var res = new System.Collections.Generic.List<object>();
System.Action<string, Loopia.Hex.HexCoord> Por = (nome, casa) => {
    var p = mapa.Colocar(D(nome), casa);
    var pos = mundo.PosicaoDe(casa);
    res.Add(new { carta = nome, casa = casa.ToString(), ok = p != null, x = pos.x, y = pos.y, z = pos.z, marcador = p != null && p.Marcador != null });
};
// caminho: mola antes da ilha alta (no sentido da volta)
Por("Mola", loop[n / 4]);
Por("IlhaAlta", loop[n / 4 + 2]);
Por("Basica", loop[n / 2]);
Por("Floresta", loop[n / 2 + 3]);
// fora: vizinha livre de quatro casas espalhadas
var usadas = new System.Collections.Generic.HashSet<Loopia.Hex.HexCoord>();
string[] fora = { "Torre", "Cabana", "CampoPacifico", "AreaDeTreino" };
int k = 0;
for (int i = 1; i < n && k < fora.Length; i += System.Math.Max(1, n / 5)) {
    var c = loop[i];
    for (int d = 0; d < 6; d++) {
        var v = c.Neighbor(d);
        if (loop.Contem(v) || mundo.EstaBloqueada(v) || mundo.TryGetIlha(v, out _) || usadas.Contains(v)) continue;
        bool vizinhaDeUsada = false;
        foreach (var u in usadas) for (int e = 0; e < 6; e++) if (u.Neighbor(e).Equals(v)) vizinhaDeUsada = true;
        if (vizinhaDeUsada) continue;
        usadas.Add(v);
        Por(fora[k++], v);
        break;
    }
}
return Newtonsoft.Json.JsonConvert.SerializeObject(res);
