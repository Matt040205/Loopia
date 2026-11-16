// Edit Mode (fora do Play). Gera um build Windows x64 com as cenas habilitadas do Build Settings (Acampamento e
// HexPrototipo). Placeholders: %%PASTA%% (pasta nova, nunca a do fallback), %%DEV%% (true = Development Build).
var cenas = new System.Collections.Generic.List<string>();
foreach (var c in UnityEditor.EditorBuildSettings.scenes) if (c.enabled) cenas.Add(c.path);
var opcoes = new UnityEditor.BuildPlayerOptions
{
    scenes = cenas.ToArray(),
    locationPathName = System.IO.Path.Combine(@"%%PASTA%%", "Loopia.exe"),
    target = UnityEditor.BuildTarget.StandaloneWindows64,
    options = %%DEV%% ? UnityEditor.BuildOptions.Development : UnityEditor.BuildOptions.None,
};
if (System.IO.Directory.Exists(@"%%PASTA%%")) return @"ERRO: a pasta ja existe, nao sobrescrevo: %%PASTA%%";
var relatorio = UnityEditor.BuildPipeline.BuildPlayer(opcoes);
var s = relatorio.summary;
return Newtonsoft.Json.JsonConvert.SerializeObject(new {
    resultado = s.result.ToString(), cenas, s.totalErrors, s.totalWarnings,
    tamanhoMB = s.totalSize / (1024.0 * 1024.0), segundos = s.totalTime.TotalSeconds, pasta = @"%%PASTA%%" });
