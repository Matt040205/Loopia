using System;
using System.Linq;
using Loopia.Hex;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MontarPreviaDaInterface
{
    const string Pasta = NavegacaoDaPreviaHud.Pasta;
    const string PastaArte = "Assets/Arte/Cartas/";

    const string PastaCenas = "Assets/Scenes/";

    /// <summary>
    /// Instala a interface aprovada (HUD de cartas, painel da jornada, barra do chefe, acampamento e
    /// resultado) nas cenas oficiais. Os botoes continuam nos metodos runtime (TelaDoAcampamento.NovaJornada
    /// e TelaDeMorte.VoltarAoAcampamento); nada Editor-only entra nas cenas. Roda uma vez: recusa se a cena
    /// ja tiver a interface.
    /// </summary>
    [MenuItem("Loopia/Interface/Aplicar nas cenas oficiais")]
    public static void AplicarNasCenasOficiais()
    {
        var tema = Preparar();
        Scene jogo = EditorSceneManager.OpenScene(PastaCenas + "HexPrototipo.unity");
        if (InterfaceInstalada())
            throw new InvalidOperationException("A HexPrototipo ja tem a interface da jornada.");
        MontarJogo(jogo, tema);
        Gravar(jogo, PastaCenas);

        Scene camp = EditorSceneManager.OpenScene(PastaCenas + "Acampamento.unity");
        if (Object.FindFirstObjectByType<TelaDoAcampamento>().arquivoSoComF3)
            throw new InvalidOperationException("O Acampamento ja tem a interface nova.");
        MontarAcampamento(tema);
        Gravar(camp, PastaCenas);
        Debug.Log("[HUD] Interface aplicada nas cenas oficiais.");
    }

    /// <summary>
    /// Copias locais (ignoradas pelo Git) das cenas oficiais com navegacao entre as copias. Desde a
    /// consolidacao as cenas oficiais ja trazem a interface; o gerador so a monta se faltar.
    /// </summary>
    [MenuItem("Loopia/Prévia da interface/Recriar cópias locais")]
    public static void Montar()
    {
        var tema = Preparar();
        if (!AssetDatabase.IsValidFolder("Assets/Previa"))
            AssetDatabase.CreateFolder("Assets", "Previa");
        if (!AssetDatabase.IsValidFolder("Assets/Previa/Hud"))
            AssetDatabase.CreateFolder("Assets/Previa", "Hud");

        Scene jogo = Copiar("HexPrototipo");
        if (!InterfaceInstalada()) MontarJogo(jogo, tema);
        var tela = Object.FindFirstObjectByType<TelaDeMorte>();
        TrocarAcao(tela.painel.GetComponentInChildren<Button>(true), "VoltarAoAcampamento",
            tela.gameObject.AddComponent<NavegacaoDaPreviaHud>().Voltar);
        Gravar(jogo, Pasta);

        Scene camp = Copiar("Acampamento");
        var acampamento = Object.FindFirstObjectByType<TelaDoAcampamento>();
        if (!acampamento.arquivoSoComF3) MontarAcampamento(tema);
        TrocarAcao(acampamento.GetComponentInChildren<Button>(true), "NovaJornada",
            acampamento.gameObject.AddComponent<NavegacaoDaPreviaHud>().NovaJornada);
        Gravar(camp, Pasta);
        Debug.Log("[HUD] Prévia local do Acampamento e da jornada pronta.");
    }

    static CatalogoVisualDeCartas Preparar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Saia do Play antes de montar a interface.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Salve as cenas abertas antes de montar a interface.");
        var tema = AssetDatabase.LoadAssetAtPath<CatalogoVisualDeCartas>(PastaArte + "Catalogo.asset");
        if (tema == null || !tema.Completo) throw new InvalidOperationException("Catalogo de arte ausente.");
        return tema;
    }

    static bool InterfaceInstalada() =>
        Object.FindFirstObjectByType<InterfaceDaJornada>(FindObjectsInactive.Include) != null ||
        Object.FindFirstObjectByType<HudDeCartas>(FindObjectsInactive.Include) != null;

    static Scene Copiar(string nome)
    {
        Scene baseDaCena = EditorSceneManager.OpenScene("Assets/Scenes/" + nome + ".unity");
        if (!EditorSceneManager.SaveScene(baseDaCena, Pasta + nome + ".unity", true))
            throw new InvalidOperationException("Falha ao copiar " + nome);
        return EditorSceneManager.OpenScene(Pasta + nome + ".unity");
    }

    static void Gravar(Scene cena, string pastaPermitida)
    {
        if (!cena.path.StartsWith(pastaPermitida, StringComparison.Ordinal))
            throw new InvalidOperationException("Caminho de cena inesperado: " + cena.path);
        EditorSceneManager.MarkSceneDirty(cena);
        if (!EditorSceneManager.SaveScene(cena))
            throw new InvalidOperationException("Falha ao salvar " + cena.path);
    }

    static void MontarJogo(Scene cena, CatalogoVisualDeCartas tema)
    {
        var diretor = Object.FindFirstObjectByType<LoopDirector>();
        var encontro = Object.FindFirstObjectByType<EncontroDoChefe>();
        var origem = Object.FindFirstObjectByType<CartasUI>();
        var barraAntiga = Object.FindFirstObjectByType<BarraDeVidaDoChefe>();
        var camera = Object.FindFirstObjectByType<HexIsoCamera>();
        if (diretor == null || encontro == null || origem == null || barraAntiga == null || camera == null)
            throw new InvalidOperationException("Referências da jornada ausentes.");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaArte + "HUD de Cartas.prefab");
        var hudCartas = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, cena)).GetComponent<HudDeCartas>();
        hudCartas.origem = origem;
        PrefabUtility.RecordPrefabInstancePropertyModifications(hudCartas);

        var painel = new GameObject("Interface da jornada", typeof(RectTransform), typeof(InterfaceDaJornada));
        painel.GetComponent<InterfaceDaJornada>().Montar(tema, diretor, encontro, barraAntiga, camera);
        MontarResultado(tema, diretor);
    }

    static void MontarResultado(CatalogoVisualDeCartas tema, LoopDirector diretor)
    {
        var tela = Object.FindFirstObjectByType<TelaDeMorte>();
        TemaDaJornada.ConfigurarCanvas(tela.gameObject, 10);
        var titulo = Texto(tela.painel.transform, "Titulo");
        var botao = tela.painel.GetComponentInChildren<Button>(true);
        var rotulo = botao.GetComponentInChildren<TMP_Text>(true);

        tela.painel.GetComponent<Image>().color = TemaDaJornada.Cor("#121020C4");
        var moldura = TemaDaJornada.Janela(tela.painel.transform, "Resultado", new Vector2(.5f,.5f),
            new Vector2(.5f,.5f), new Rect(0, 0, 604, 331));
        Acabamento(moldura, 604);
        PosicionarTexto(titulo, moldura, tema.fonteTitulo, 42, TemaDaJornada.Ouro, new Rect(30, 38, 544, 66));
        titulo.enableAutoSizing = true;
        titulo.fontSizeMin = 28;
        titulo.fontSizeMax = 42;
        tela.textoResumo.gameObject.SetActive(false);
        var resumo = TemaDaJornada.Rotulo(moldura, "Desfecho", tema.fonteCorpo, "", 21, TemaDaJornada.Marfim,
            TextAlignmentOptions.Center, new Rect(34, 117, 536, 53));
        var colecao = TemaDaJornada.Bloco(moldura, "Faixa de moedas", TemaDaJornada.Cor("#332B40"), 56, 183, 492, 42);
        var moedas = TemaDaJornada.Rotulo(colecao.transform, "Moedas da jornada", tema.fonteCorpo, "", 18,
            TemaDaJornada.Ouro, TextAlignmentOptions.Center, new Rect(10, 0, 472, 42));
        TemaDaJornada.EstilizarBotao(botao, rotulo, tema.fonteCorpo, moldura, new Rect(150, 250, 304, 51));
        tela.painel.AddComponent<ResultadoDaJornadaVisual>().Configurar(diretor, diretor.status, titulo, resumo, moedas);
        tela.painel.SetActive(false);
    }

    static void MontarAcampamento(CatalogoVisualDeCartas tema)
    {
        var tela = Object.FindFirstObjectByType<TelaDoAcampamento>();
        TemaDaJornada.ConfigurarCanvas(tela.gameObject, 10);
        var titulo = Texto(tela.transform, "Titulo");
        var subtitulo = Texto(tela.transform, "Subtitulo");
        var botao = tela.GetComponentInChildren<Button>(true);
        var rotulo = botao.GetComponentInChildren<TMP_Text>(true);
        var fundo = tela.GetComponentsInChildren<Image>(true).FirstOrDefault(img => img.name == "Fundo");
        if (fundo != null) fundo.color = TemaDaJornada.Cor("#181524");
        tela.arquivoSoComF3 = true;
        if (tela.textoArquivo != null)
        {
            var diagnostico = tela.textoArquivo;
            diagnostico.font = tema.fonteCorpo;
            diagnostico.color = TemaDaJornada.Secundario;
            diagnostico.fontSize = 14;
            diagnostico.gameObject.SetActive(false);
        }

        var moldura = TemaDaJornada.Janela(tela.transform, "Painel do acampamento", new Vector2(.5f,.5f),
            new Vector2(.5f,.5f), new Rect(0, 0, 632, 380));
        Acabamento(moldura, 632);
        PosicionarTexto(titulo, moldura, tema.fonteTitulo, 48, TemaDaJornada.Ouro, new Rect(26, 34, 580, 80));
        titulo.text = "Loopia";
        PosicionarTexto(subtitulo, moldura, tema.fonteCorpo, 20, TemaDaJornada.Secundario, new Rect(50, 116, 532, 55));
        subtitulo.text = "Lucca descansa perto da fogueira antes da próxima jornada.";
        var faixa = TemaDaJornada.Bloco(moldura, "Recursos e recorde", TemaDaJornada.Cor("#332B40"), 48, 190, 536, 64);
        TemaDaJornada.Bloco(faixa.transform, "Divisor de estatísticas", TemaDaJornada.Cor("#927A54"), 268, 14, 1, 36);
        Estatistica(tela.textoMoedas, faixa.rectTransform, tema.fonteCorpo, TemaDaJornada.Ouro, new Rect(9, 2, 251, 60));
        Estatistica(tela.textoRecorde, faixa.rectTransform, tema.fonteCorpo, TemaDaJornada.Marfim, new Rect(278, 2, 248, 60));
        TemaDaJornada.EstilizarBotao(botao, rotulo, tema.fonteCorpo, moldura, new Rect(164, 288, 304, 51));
        if (tela.textoArquivo != null) tela.textoArquivo.transform.SetAsLastSibling();
        Selection.activeGameObject = moldura.gameObject;
    }

    static void Estatistica(TMP_Text texto, RectTransform pai, TMP_FontAsset fonte, Color cor, Rect area)
    {
        PosicionarTexto(texto, pai, fonte, 23, cor, area);
        texto.fontStyle = FontStyles.Bold;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 15;
        texto.fontSizeMax = 23;
    }

    static void Acabamento(RectTransform pai, float largura)
    {
        TemaDaJornada.Bloco(pai, "Faixa de abertura", TemaDaJornada.Cor("#30273AF5"), 2, 2, largura - 4, 104);
        TemaDaJornada.Bloco(pai, "Filete de abertura", TemaDaJornada.Ouro, largura * .5f - 44, 1, 88, 2);
    }

    static TMP_Text Texto(Transform raiz, string nome) =>
        raiz.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == nome);

    static void PosicionarTexto(TMP_Text texto, RectTransform pai, TMP_FontAsset fonte,
        float tamanho, Color cor, Rect area)
    {
        texto.transform.SetParent(pai, false);
        TemaDaJornada.NoTopo(texto.rectTransform, area.x, area.y, area.width, area.height);
        texto.font = fonte;
        texto.fontStyle = FontStyles.Normal;
        texto.enableAutoSizing = false;
        texto.fontSize = tamanho;
        texto.color = cor;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
    }

    static void TrocarAcao(Button botao, string metodo, UnityAction acao)
    {
        for (int i = botao.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            if (botao.onClick.GetPersistentMethodName(i) == metodo)
                UnityEventTools.RemovePersistentListener(botao.onClick, i);
        UnityEventTools.AddPersistentListener(botao.onClick, acao);
    }
}
