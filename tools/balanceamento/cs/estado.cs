// Play Mode. Estado da jornada para o driver acompanhar (uma leitura, nada muda).
var diretor = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.LoopDirector>();
var corredor = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.HexLoopRunner>();
var status = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.LuccaStatus>();
var mao = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.MaoDeCartas>();
var encontro = UnityEngine.Object.FindFirstObjectByType<Loopia.Hex.EncontroDoChefe>();
int livres = 0;
for (int i = 0; i < mao.Mao.Count; i++) if (mao.CartaNoEspaco(i) == null) livres++;
return Newtonsoft.Json.JsonConvert.SerializeObject(new {
    estado = diretor.Estado.ToString(), terminou = diretor.Terminou, voltas = corredor.VoltasCompletas,
    passos = corredor.PassosNaVoltaAtual, pv = status.PvAtual, pvMax = status.PvMaximoTotal,
    flechas = status.FlechasAtuais, flechasMax = status.FlechasMaximoTotal, nivel = status.Nivel,
    pendentes = mao.Pendentes, livres,
    chefePv = encontro != null && encontro.Chefe != null ? encontro.Chefe.PvAtual : -1,
    tempo = UnityEngine.Time.time });
