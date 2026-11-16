using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loopia.Hex
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class InterfaceDaJornada : MonoBehaviour
    {
        [SerializeField] LoopDirector diretor;
        [SerializeField] EncontroDoChefe encontro;
        [SerializeField] BarraDeVidaDoChefe barraAntiga;
        [SerializeField] HexIsoCamera cameraIso;

        [SerializeField] RectTransform jornada;
        [SerializeField] RectTransform chefe;
        [SerializeField] RectTransform dica;
        [SerializeField] TMP_Text titulo;
        [SerializeField] TMP_Text voltas;
        [SerializeField] RectTransform blocoPassos;
        [SerializeField] TMP_Text passos;
        [SerializeField] Image progresso;
        [SerializeField] TMP_Text pv;
        [SerializeField] TMP_Text flechas;
        [SerializeField] TMP_Text nivel;
        [SerializeField] TMP_Text moedas;
        [SerializeField] RectTransform linhaInimigos;
        [SerializeField] TMP_Text inimigos;
        [SerializeField] RectTransform[] linhas;
        [SerializeField] TMP_Text chefeNome;
        [SerializeField] TMP_Text chefePv;
        [SerializeField] Image chefeVida;
        [SerializeField] TMP_Text revoada;

        bool _painelAnterior;
        bool _barraAnterior;
        bool _dicaAnterior;
        bool _depuracao;
        bool _temLayout;
        (int volta, int meta, bool chefe, int passo, int ciclo, int hp, int hpMax, int muni, int muniMax,
            int nivel, int xp, int xpMax, int moedas, int total, int vivos, float escala) _leitura;
        (Inimigo chefe, int hp, int hpMax, bool aviso) _leituraChefe;

        public void Montar(CatalogoVisualDeCartas tema, LoopDirector origem, EncontroDoChefe encontroDaCena,
            BarraDeVidaDoChefe barra, HexIsoCamera camera)
        {
            diretor = origem;
            encontro = encontroDaCena;
            barraAntiga = barra;
            cameraIso = camera;
            TemaDaJornada.ConfigurarCanvas(gameObject, 5);

            jornada = TemaDaJornada.Janela(transform, "Painel da jornada", Vector2.up, Vector2.up,
                new Rect(16, -16, 246, 221));
            TemaDaJornada.Bloco(jornada, "Faixa superior", TemaDaJornada.Cor("#30273AF5"), 2, 2, 242, 44);
            TemaDaJornada.Bloco(jornada, "Filete", TemaDaJornada.Ouro, 1, 1, 46, 2);
            titulo = Rotulo(jornada, tema.fonteTitulo, "Estado", "Voltas", 21, TemaDaJornada.Ouro,
                false, new Rect(13, 8, 120, 31));
            voltas = Rotulo(jornada, tema.fonteTitulo, "Voltas", "0 / 4", 21, TemaDaJornada.Ouro,
                true, new Rect(130, 8, 103, 31));

            blocoPassos = ElementosDaCarta.Caixa(jornada, "Progresso da volta");
            TemaDaJornada.NoTopo(blocoPassos, 0, 48, 246, 34);
            passos = Indicador(blocoPassos, tema, "Passos", false, 0, out _);
            var trilho = TemaDaJornada.Bloco(blocoPassos, "Trilho", TemaDaJornada.Cor("#75634F72"), 14, 26, 218, 2);
            progresso = Preenchimento(trilho.transform, TemaDaJornada.Ouro);

            linhas = new RectTransform[5];
            pv = Indicador(jornada, tema, "PV", true, 86, out linhas[0]);
            flechas = Indicador(jornada, tema, "Flechas", true, 108, out linhas[1]);
            linhas[2] = TemaDaJornada.Bloco(jornada, "Divisor", TemaDaJornada.Cor("#9A805659"), 14, 137, 218, 1).rectTransform;
            nivel = Indicador(jornada, tema, "Nível", false, 145, out linhas[3]);
            moedas = Indicador(jornada, tema, "Moedas", false, 167, out linhas[4]);
            inimigos = Indicador(jornada, tema, "Inimigos", false, 189, out linhaInimigos);
            MontarChefe(tema);
            MontarDica(tema);
            chefe.gameObject.SetActive(false);
            dica.gameObject.SetActive(false);
        }

        void MontarChefe(CatalogoVisualDeCartas tema)
        {
            var centroTopo = new Vector2(.5f, 1);
            chefe = TemaDaJornada.Janela(transform, "Barra do chefe", centroTopo, centroTopo,
                new Rect(0, -16, 486, 68));
            chefeNome = Rotulo(chefe, tema.fonteTitulo, "Nome do chefe", "", 22, TemaDaJornada.Ouro, false,
                new Rect(16, 7, 276, 32));
            ElementosDaCarta.AjustarFonte(chefeNome, 15, 22);
            chefePv = Rotulo(chefe, tema.fonteCorpo, "PV do chefe", "", 15, TemaDaJornada.Marfim, true,
                new Rect(343, 7, 127, 32));
            var trilho = TemaDaJornada.Bloco(chefe, "Trilho do chefe", TemaDaJornada.Cor("#493546"), 16, 46, 454, 10);
            chefeVida = Preenchimento(trilho.transform, TemaDaJornada.Cor("#CD766C"));
            revoada = Rotulo(chefe, tema.fonteCorpo, "Aviso de Revoada", "Revoada", 14, TemaDaJornada.Aviso,
                true, new Rect(278, 7, 76, 32));
            TemaDaJornada.NoTopo(chefePv.rectTransform, 365, 7, 105, 32);
        }

        void MontarDica(CatalogoVisualDeCartas tema)
        {
            dica = TemaDaJornada.Janela(transform, "Dica de câmera", Vector2.zero, Vector2.zero,
                new Rect(16, 16, 224, 52));
            TemaDaJornada.Rotulo(dica, "Controle de zoom", tema.fonteCorpo, "Roda do mouse · Zoom",
                13, TemaDaJornada.Marfim, TextAlignmentOptions.MidlineLeft, new Rect(11, 5, 202, 20));
            TemaDaJornada.Rotulo(dica, "Controle de movimento", tema.fonteCorpo, "Botão direito · Mover a câmera",
                13, TemaDaJornada.Marfim, TextAlignmentOptions.MidlineLeft, new Rect(11, 26, 202, 20));
        }

        static TMP_Text Indicador(RectTransform pai, CatalogoVisualDeCartas tema, string nome,
            bool recurso, float y, out RectTransform linha)
        {
            linha = ElementosDaCarta.Caixa(pai, nome);
            TemaDaJornada.NoTopo(linha, 0, y, 246, 21);
            Rotulo(linha, tema.fonteCorpo, nome, nome, 14, TemaDaJornada.Secundario, false,
                new Rect(14, 0, 74, 21));
            float tamanho = recurso ? 17 : 15;
            var valor = Rotulo(linha, tema.fonteCorpo, "Valor " + nome, "", tamanho, TemaDaJornada.Marfim,
                true, new Rect(89, 0, 143, 21));
            if (recurso) valor.fontStyle = FontStyles.Bold;
            ElementosDaCarta.AjustarFonte(valor, 11, tamanho);
            return valor;
        }

        static TMP_Text Rotulo(Transform pai, TMP_FontAsset fonte, string nome, string texto, float tamanho,
            Color cor, bool direita, Rect area)
        {
            var rotulo = TemaDaJornada.Rotulo(pai, nome, fonte, texto, tamanho, cor, direita
                ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft, area);
            rotulo.textWrappingMode = TextWrappingModes.NoWrap;
            return rotulo;
        }

        static Image Preenchimento(Transform pai, Color cor)
        {
            var imagem = ElementosDaCarta.Painel(pai, "Preenchimento", cor);
            imagem.rectTransform.anchorMin = Vector2.zero;
            imagem.rectTransform.offsetMin = Vector2.zero;
            imagem.rectTransform.offsetMax = Vector2.zero;
            return imagem;
        }

        static void Progresso(Image imagem, float fracao) =>
            imagem.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fracao), 1);

        void OnEnable()
        {
            if (diretor == null || jornada == null) return;
            _painelAnterior = diretor.mostrarPainelNaTela;
            diretor.mostrarPainelNaTela = false;
            if (barraAntiga != null)
            {
                _barraAnterior = barraAntiga.enabled;
                barraAntiga.enabled = false;
            }
            if (cameraIso != null)
            {
                _dicaAnterior = cameraIso.DesenharDicaImGui;
                cameraIso.DesenharDicaImGui = false;
            }
            _temLayout = false;
            _leituraChefe = default;
        }

        void OnDisable()
        {
            if (diretor != null && jornada != null) diretor.mostrarPainelNaTela = _painelAnterior;
            if (barraAntiga != null) barraAntiga.enabled = _barraAnterior;
            if (cameraIso != null) cameraIso.DesenharDicaImGui = _dicaAnterior;
        }

        void LateUpdate()
        {
            if (diretor == null || jornada == null) return;
            bool emJogo = !diretor.Terminou;
            Ativo(jornada.gameObject, emJogo && _painelAnterior);
            Ativo(dica.gameObject, emJogo && _dicaAnterior && cameraIso != null && cameraIso.DicaDeZoomVisivel);
            AtualizarChefe(emJogo);
            if (emJogo && _painelAnterior) AtualizarJornada();
        }

        void AtualizarJornada()
        {
            bool depuracao = diretor.MostrandoDepuracao;
            bool mudouLayout = !_temLayout || _depuracao != depuracao;
            if (mudouLayout) AlternarDepuracao(depuracao);

            var status = diretor.status;
            var corredor = diretor.corredor;
            if (status == null || corredor == null) return;
            var dados = (volta: diretor.VoltasCompletas, meta: diretor.voltasParaOBoss,
                chefe: diretor.Estado == EstadoDaPartida.Chefe, passo: corredor.PassosNaVoltaAtual,
                ciclo: corredor.TamanhoDoLoop, hp: status.PvAtual, hpMax: status.PvMaximoTotal,
                muni: status.FlechasAtuais, muniMax: status.FlechasMaximoTotal, nivel: status.Nivel,
                xp: status.Xp, xpMax: status.XpParaOProximoNivel, moedas: status.Moedas,
                total: depuracao ? DadosDoJogador.Instancia.MoedasTotais : 0,
                vivos: depuracao && diretor.inimigos != null ? diretor.inimigos.Vivos.Count : 0,
                escala: diretor.MultiplicadorDosInimigos);
            if (!mudouLayout && _leitura.Equals(dados)) return;
            _leitura = dados;

            titulo.text = dados.chefe ? "Chefe" : "Voltas";
            voltas.text = $"{dados.volta} / {dados.meta}";
            pv.text = $"{dados.hp} / {dados.hpMax}";
            pv.color = dados.hp <= dados.hpMax * .25f ? TemaDaJornada.Perigo : TemaDaJornada.Sucesso;
            flechas.text = $"{dados.muni} / {dados.muniMax}";
            nivel.text = depuracao ? $"{dados.nivel} · XP {dados.xp} / {dados.xpMax}" : dados.nivel.ToString();
            moedas.text = depuracao ? $"{dados.moedas} · total {dados.total}" : dados.moedas.ToString();
            if (!depuracao) return;

            passos.text = $"{dados.passo} / {dados.ciclo}";
            Progresso(progresso, dados.ciclo > 0 ? (float)dados.passo / dados.ciclo : 0);
            inimigos.text = $"{dados.vivos} vivos · ×{dados.escala.ToString("F2", CultureInfo.GetCultureInfo("pt-BR"))}";
        }

        void AlternarDepuracao(bool depuracao)
        {
            _depuracao = depuracao;
            _temLayout = true;
            jornada.sizeDelta = new Vector2(246, depuracao ? 221 : 160);
            Ativo(blocoPassos.gameObject, depuracao);
            Ativo(linhaInimigos.gameObject, depuracao);
            float y = depuracao ? 0 : 40;
            TemaDaJornada.NoTopo(linhas[0], 0, 86 - y, 246, 21);
            TemaDaJornada.NoTopo(linhas[1], 0, 108 - y, 246, 21);
            TemaDaJornada.NoTopo(linhas[2], 14, 137 - y, 218, 1);
            TemaDaJornada.NoTopo(linhas[3], 0, 145 - y, 246, 21);
            TemaDaJornada.NoTopo(linhas[4], 0, 167 - y, 246, 21);
        }

        void AtualizarChefe(bool emJogo)
        {
            Inimigo inimigo = encontro != null ? encontro.Chefe : null;
            bool visivel = emJogo && _barraAnterior && inimigo != null && inimigo.EstaVivo;
            Ativo(chefe.gameObject, visivel);
            if (!visivel)
            {
                _leituraChefe = default;
                return;
            }
            var dados = (chefe: inimigo, hp: inimigo.PvAtual, hpMax: inimigo.PvMaximo, aviso: encontro.AvisandoEspecial);
            if (_leituraChefe.Equals(dados)) return;
            _leituraChefe = dados;
            chefeNome.text = inimigo.Def != null ? inimigo.Def.nome : "Chefe";
            chefePv.text = $"{dados.hp} / {dados.hpMax}";
            chefeVida.color = dados.aviso ? TemaDaJornada.Aviso : TemaDaJornada.Cor("#CD766C");
            Progresso(chefeVida, (float)dados.hp / Mathf.Max(1, dados.hpMax));
            Ativo(revoada.gameObject, dados.aviso);
        }

        static void Ativo(GameObject objeto, bool ativo)
        {
            if (objeto.activeSelf != ativo) objeto.SetActive(ativo);
        }
    }
}
