// Play Mode, logo depois de carregar a HexPrototipo. Fixa a semente do mapa, coloca as cartas do cenario pelo
// mesmo MapaDePlataformas.Colocar da UI (gastando da mao quando a carta esta nela) e anexa o MedidorDaPartida.
// Placeholders: %%SEMENTE%% (int), %%CARTAS%% (nomes de asset separados por virgula), %%MORCEGOS%% (true/false)
UnityEngine.Application.runInBackground = true;
UnityEngine.Time.timeScale = 0f;
var g = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopGenerator>();
var mapa = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.MapaDePlataformas>();
var inimigos = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.InimigoSpawner>();
var mao = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.MaoDeCartas>();
var encontro = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.EncontroDoChefe>();
g.usarSementeFixa = true;
g.semente = %%SEMENTE%%;
bool gerou = g.Gerar();
// Como no comeco normal: o Lucca parte do acampamento (senao, se ele cair numa casa do anel novo, a 1a volta fecha logo).
UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexPlayer>().ColocarEm(g.mundo.Acampamento);
UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopRunner>().ReiniciarContagem();
inimigos.nascerMorcegos = %%MORCEGOS%%;
inimigos.LimparTodos();
var medidor = new UnityEngine.GameObject("Medidor (balanceamento)").AddComponent<Loopia.Hex.MedidorDaPartida>();
medidor.encontro = encontro;

var colocadas = new System.Collections.Generic.List<string>();
Loopia.Hex.HexCoord torre = default(Loopia.Hex.HexCoord);
bool temTorre = false;
int indiceNoCaminho = 3;
foreach (string nome in "%%CARTAS%%".Split(','))
{
    if (string.IsNullOrEmpty(nome)) continue;
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Loopia.Hex.PlataformaDef>("Assets/Dados/Plataformas/" + nome + ".asset");
    if (def == null) { colocadas.Add(nome + ":sem asset"); continue; }
    var candidatos = new System.Collections.Generic.List<Loopia.Hex.HexCoord>();
    if (nome == "AreaDeTreino" && temTorre) for (int d = 0; d < 6; d++) candidatos.Add(torre.Neighbor(d));
    for (int k = 0; k < g.Loop.Count; k++) candidatos.Add(g.Loop[(indiceNoCaminho + k) % g.Loop.Count]);
    foreach (var c in g.Loop.Casas) for (int d = 0; d < 6; d++) candidatos.Add(c.Neighbor(d));
    bool feito = false;
    foreach (var c in candidatos)
    {
        string motivo;
        // Casa ocupada: colocar ali SUBSTITUI a carta (regra do jogo); o cenario quer as cartas juntas, nao trocadas.
        Loopia.Hex.Plataforma existente;
        if (mapa.TryGet(c, out existente) && existente != null) continue;
        if (!mapa.PodeColocar(def, c, out motivo)) continue;
        if (mapa.Colocar(def, c) == null) continue;
        feito = true;
        int i;
        bool noCaminho = g.Loop.TryIndiceDe(c, out i);
        if (noCaminho) indiceNoCaminho = i + 5;
        if (nome == "Torre") { torre = c; temTorre = true; }
        colocadas.Add(nome + "@" + c + (noCaminho ? "(caminho)" : "(fora)"));
        for (int espaco = 0; espaco < mao.Mao.Count; espaco++)
            if (mao.CartaNoEspaco(espaco) == def) { mao.Gastar(espaco); break; }
        break;
    }
    if (!feito) colocadas.Add(nome + ":nao coube");
}
var naMao = new System.Collections.Generic.List<string>();
for (int espaco = 0; espaco < mao.Mao.Count; espaco++) { var c = mao.CartaNoEspaco(espaco); naMao.Add(c != null ? c.name : "-"); }
return Newtonsoft.Json.JsonConvert.SerializeObject(new { gerou, volta = g.Loop.Count, colocadas, mao = naMao, pendentes = mao.Pendentes });
