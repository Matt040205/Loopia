using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>Categorias de arte da Scene Bible (docs/art/SCENE_BIBLE.md, secao 2).</summary>
    public enum CategoriaDeArte
    {
        ArvoreGrande,
        ArvorePequena,
        Arbusto,
        Capim,
        Pedra,
        Cogumelo,
        Tronco,
        Outro,
    }

    /// <summary>Faixa de tamanho de uma categoria, em unidades de L (altura do Lucca).</summary>
    [Serializable]
    public class RegraDeCategoria
    {
        public CategoriaDeArte categoria;

        [Tooltip("Tamanho minimo, em L (altura do Lucca).")]
        [Min(0.01f)] public float tamanhoMinimo = 0.28f;

        [Tooltip("Tamanho maximo, em L (altura do Lucca).")]
        [Min(0.01f)] public float tamanhoMaximo = 0.65f;

        [Tooltip("Dimensao do modelo que recebe o tamanho: altura para objetos verticais, comprimento para deitados.")]
        public EixoDeMedida eixo = EixoDeMedida.MaiorEixo;

        [Tooltip("So nasce nos vertices do lado oposto a camera (nao cobre o caminho nem o Lucca na tela).")]
        public bool soNoLadoLongeDaCamera;

        [Tooltip("Para 'so no lado longe': a posicao so vale se Dot(posicao normalizada, frente da camera) for maior que isto. " +
                 "0.2 era o valor original; maior = so os vertices mais afastados da camera.")]
        [Range(0f, 0.95f)] public float limiarDoLadoLonge = 0.2f;

        [Tooltip("Rejeita posicoes em que a caixa do modelo, vista pela camera, cobriria algum ponto dos corredores da ilha " +
                 "(ate duas ilhas de distancia, a 0.5 u de altura: o Lucca). Conservador (caixa alinhada ao mundo).")]
        public bool evitarCobrirCaminho;

        [Tooltip("Fracao da meia-diagonal da caixa do modelo usada como pegada ao reservar espaco (centro, corredores, " +
                 "borda e outras decoracoes). Copas largas so precisam reservar o tronco; 1 = caixa inteira.")]
        [Range(0.1f, 1f)] public float fracaoDaPegada = 1f;

        public RegraDeCategoria() { }

        public RegraDeCategoria(CategoriaDeArte categoria, float minimo, float maximo, EixoDeMedida eixo,
            bool soLonge = false, float fracaoDaPegada = 1f, bool evitarCobrirCaminho = false)
        {
            this.categoria = categoria;
            tamanhoMinimo = minimo;
            tamanhoMaximo = maximo;
            this.eixo = eixo;
            soNoLadoLongeDaCamera = soLonge;
            this.fracaoDaPegada = fracaoDaPegada;
            this.evitarCobrirCaminho = evitarCobrirCaminho;
        }
    }

    /// <summary>Um prefab do perfil: de que categoria e com que peso entra no sorteio.</summary>
    [Serializable]
    public class ModeloDeArte
    {
        public GameObject prefab;
        public CategoriaDeArte categoria = CategoriaDeArte.Outro;
        [Min(0f)] public float peso = 1f;
    }

    /// <summary>
    /// Perfil unico de escala da arte de ilha: tamanhos relativos a L (altura do Lucca), eixo de medida,
    /// regra de posicao e o mapeamento prefab -> categoria. Usado por <see cref="DecoracaoDeIlha"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EscalaDeArte", menuName = "Loopia/Perfil de escala de arte")]
    public class ArtScaleProfile : ScriptableObject
    {
        [Tooltip("L: altura do Lucca em unidades de mundo. Todos os tamanhos abaixo sao multiplos disto.")]
        [Min(0.1f)] public float alturaDeReferencia = 1f;

        [Tooltip("Tem que ser igual ao HexIsoCamera.giro (um teste confere).")]
        public float giroDaCamera = 45f;

        [Tooltip("Tem que ser igual ao HexIsoCamera.inclinacao (um teste confere). Usado por 'evitarCobrirCaminho'.")]
        [Range(10f, 89f)] public float inclinacaoDaCamera = 30f;

        public List<RegraDeCategoria> regras = RegrasPadrao();
        public List<ModeloDeArte> modelos = new List<ModeloDeArte>();

        [Header("Vegetacao baixa (Pass C): segunda camada, sorteio proprio")]
        [Tooltip("Arbustos, tufos de capim/flores. Vai para o filho 'VegetacaoBaixa' da ilha, depois da decoracao principal.")]
        public List<ModeloDeArte> vegetacaoBaixa = new List<ModeloDeArte>();
        [Tooltip("Quantas pecas por ilha (sorteado entre minimo e maximo, por coordenada). 0/0 desliga a camada.")]
        [Range(0, 12)] public int vegetacaoMinima = 0;
        [Range(0, 12)] public int vegetacaoMaxima = 0;
        [Tooltip("Variacao da mistura por ilha: cada peso e multiplicado por 1 +- isto (sorteado por ilha).")]
        [Range(0f, 1f)] public float variacaoDaMistura = 0f;

        /// <summary>Tabela da Scene Bible, secao 2.</summary>
        public static List<RegraDeCategoria> RegrasPadrao() => new List<RegraDeCategoria>
        {
            new RegraDeCategoria(CategoriaDeArte.ArvoreGrande, 1.8f, 2.4f, EixoDeMedida.Altura, true, 0.35f, true),
            new RegraDeCategoria(CategoriaDeArte.ArvorePequena, 1.1f, 1.5f, EixoDeMedida.Altura, false, 0.4f),
            new RegraDeCategoria(CategoriaDeArte.Arbusto, 0.3f, 0.6f, EixoDeMedida.Altura),
            new RegraDeCategoria(CategoriaDeArte.Capim, 0.1f, 0.25f, EixoDeMedida.Altura),
            new RegraDeCategoria(CategoriaDeArte.Pedra, 0.3f, 0.8f, EixoDeMedida.MaiorEixo),
            new RegraDeCategoria(CategoriaDeArte.Cogumelo, 0.2f, 0.45f, EixoDeMedida.Altura),
            new RegraDeCategoria(CategoriaDeArte.Tronco, 0.5f, 1.0f, EixoDeMedida.Comprimento),
            new RegraDeCategoria(CategoriaDeArte.Outro, 0.28f, 0.65f, EixoDeMedida.MaiorEixo),
        };

        /// <summary>Direcao em que a camera olha, projetada no plano XZ (+forward girado por <see cref="giroDaCamera"/>).</summary>
        public Vector3 DirecaoDaCamera => Quaternion.Euler(inclinacaoDaCamera, giroDaCamera, 0f) * Vector3.forward;

        public Vector3 FrenteDaCameraXZ => Quaternion.Euler(0f, giroDaCamera, 0f) * Vector3.forward;

        public bool TryRegra(CategoriaDeArte categoria, out RegraDeCategoria regra)
        {
            if (regras != null)
            {
                for (int i = 0; i < regras.Count; i++)
                {
                    if (regras[i] == null || regras[i].categoria != categoria) continue;
                    regra = regras[i];
                    return true;
                }
            }
            regra = null;
            return false;
        }

        /// <summary>Tamanho sorteado na faixa da regra, ja em unidades de mundo (faixa em L x altura de referencia).</summary>
        public float SortearTamanho(RegraDeCategoria regra, System.Random random)
        {
            float t = (float)random.NextDouble();
            return Mathf.Lerp(regra.tamanhoMinimo, regra.tamanhoMaximo, t) * alturaDeReferencia;
        }

        /// <summary>Sorteio ponderado pelo peso. Ignora entradas sem prefab ou com peso zero; null se nao sobrar nenhuma.</summary>
        public ModeloDeArte Sortear(System.Random random) => SortearDe(modelos, random, null);

        /// <summary>Sorteio ponderado numa lista qualquer; multiplicadores (opcional, mesmo tamanho) ajustam o peso de cada item.</summary>
        public static ModeloDeArte SortearDe(List<ModeloDeArte> lista, System.Random random, float[] multiplicadores)
        {
            if (lista == null) return null;
            double Peso(int i) => lista[i].peso * (multiplicadores != null && i < multiplicadores.Length ? Mathf.Max(0f, multiplicadores[i]) : 1f);
            double total = 0.0;
            for (int i = 0; i < lista.Count; i++)
                if (Valido(lista[i])) total += Peso(i);
            if (total <= 0.0) return null;

            double alvo = random.NextDouble() * total;
            ModeloDeArte ultimo = null;
            for (int i = 0; i < lista.Count; i++)
            {
                if (!Valido(lista[i])) continue;
                ultimo = lista[i];
                alvo -= Peso(i);
                if (alvo < 0.0) return lista[i];
            }
            return ultimo;
        }

        static bool Valido(ModeloDeArte m) => m != null && m.prefab != null && m.peso > 0f;
    }
}
