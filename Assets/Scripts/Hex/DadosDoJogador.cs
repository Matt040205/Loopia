using System;
using System.IO;
using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Singleton com o que sobrevive entre partidas: o total de moedas e o recorde de voltas.
    /// Atravessa as cenas (DontDestroyOnLoad) e grava tudo num JSON em
    /// Application.persistentDataPath, que e o lugar onde a build tem permissao de escrever.
    ///
    /// Pode estar colocado na cena ou nao: se alguem pedir a Instancia e ela ainda nao existir,
    /// ela se cria sozinha. Um segundo objeto que aparecer (ao trocar de cena) se destroi.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class DadosDoJogador : MonoBehaviour
    {
        const string NomeDoArquivo = "loopia_save.json";

        /// <summary>
        /// O formato do arquivo. Os nomes dos campos sao as chaves do JSON. Campo novo precisa ter 0 como
        /// padrao valido: o JsonUtility deixa o padrao quando a chave nao existe, e e isso que mantem os
        /// saves antigos (sem "vitorias") carregando sem erro.
        /// </summary>
        [Serializable]
        class Save
        {
            public int moedas;
            public int recordeDeVoltas;
            public int vitorias;
        }

        static DadosDoJogador _instancia;

        public static DadosDoJogador Instancia
        {
            get
            {
                if (_instancia != null) return _instancia;

                DadosDoJogador existente = FindFirstObjectByType<DadosDoJogador>();
                if (existente == null)
                {
                    existente = new GameObject("DadosDoJogador").AddComponent<DadosDoJogador>();
                }

                existente.Inicializar();
                return _instancia;
            }
        }

        public int MoedasTotais { get; private set; }
        public int RecordeDeVoltas { get; private set; }

        /// <summary>Quantas jornadas terminaram com o chefe derrotado.</summary>
        public int Vitorias { get; private set; }

        public string CaminhoDoArquivo => Path.Combine(Application.persistentDataPath, NomeDoArquivo);

        public event Action AoMudar;

        void Awake() => Inicializar();

        void Inicializar()
        {
            if (_instancia == this) return;

            if (_instancia != null)
            {
                // Ja existe um vindo da cena anterior: este aqui sobrou.
                Destroy(gameObject);
                return;
            }

            _instancia = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            Carregar();
        }

        void OnApplicationQuit() => Salvar();

        public void AdicionarMoedas(int quantidade)
        {
            if (quantidade <= 0) return;

            MoedasTotais += quantidade;
            Salvar();
            AoMudar?.Invoke();
        }

        /// <summary>Guarda o numero de voltas se ele bater o recorde. Devolve true quando bateu.</summary>
        public bool RegistrarVoltas(int voltas)
        {
            if (voltas <= RecordeDeVoltas) return false;

            RecordeDeVoltas = voltas;
            Salvar();
            AoMudar?.Invoke();
            return true;
        }

        /// <summary>Uma jornada terminou com o chefe derrotado. Grava na hora.</summary>
        public void RegistrarVitoria()
        {
            Vitorias++;
            Salvar();
            AoMudar?.Invoke();
        }

        public void Salvar()
        {
            var dados = new Save { moedas = MoedasTotais, recordeDeVoltas = RecordeDeVoltas, vitorias = Vitorias };

            try
            {
                File.WriteAllText(CaminhoDoArquivo, JsonUtility.ToJson(dados, true));
            }
            catch (Exception erro)
            {
                Debug.LogWarning("[DadosDoJogador] Nao consegui salvar em " + CaminhoDoArquivo + ": " + erro.Message, this);
            }
        }

        public void Carregar()
        {
            MoedasTotais = 0;
            RecordeDeVoltas = 0;
            Vitorias = 0;

            if (!File.Exists(CaminhoDoArquivo)) return;

            string json;
            try
            {
                json = File.ReadAllText(CaminhoDoArquivo);
            }
            catch (Exception erro)
            {
                Debug.LogWarning("[DadosDoJogador] Nao consegui ler " + CaminhoDoArquivo + ": " + erro.Message, this);
                return;
            }

            if (!TentarLer(json, out int moedas, out int recorde, out int vitorias, out string motivo))
            {
                // Arquivo corrompido: melhor comecar do zero do que travar o jogo na abertura.
                Debug.LogWarning("[DadosDoJogador] Save ilegivel, comecando do zero: " + motivo, this);
                return;
            }

            MoedasTotais = moedas;
            RecordeDeVoltas = recorde;
            Vitorias = vitorias;
        }

        /// <summary>
        /// Interpreta o conteudo do save sem tocar em disco nem no singleton (os testes usam isto).
        /// Valores negativos viram 0; chaves ausentes (saves antigos) ficam 0.
        /// </summary>
        public static bool TentarLer(string json, out int moedas, out int recordeDeVoltas, out int vitorias,
                                     out string motivo)
        {
            moedas = 0;
            recordeDeVoltas = 0;
            vitorias = 0;
            motivo = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                motivo = "arquivo vazio";
                return false;
            }

            Save dados;
            try
            {
                dados = JsonUtility.FromJson<Save>(json);
            }
            catch (Exception erro)
            {
                motivo = erro.Message;
                return false;
            }

            if (dados == null)
            {
                motivo = "conteudo nulo";
                return false;
            }

            moedas = Mathf.Max(0, dados.moedas);
            recordeDeVoltas = Mathf.Max(0, dados.recordeDeVoltas);
            vitorias = Mathf.Max(0, dados.vitorias);
            return true;
        }

        /// <summary>Zera o progresso salvo. Util para testar a partir do inicio.</summary>
        [ContextMenu("Apagar progresso")]
        public void ApagarProgresso()
        {
            MoedasTotais = 0;
            RecordeDeVoltas = 0;
            Vitorias = 0;
            Salvar();
            AoMudar?.Invoke();
        }
    }
}
