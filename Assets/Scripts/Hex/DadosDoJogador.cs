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
        /// Argumento de linha de comando que escolhe um save separado, para builds de QA e validacoes nao mexerem
        /// no progresso do jogador: <c>Loopia.exe -loopia-perfil qa</c> grava em <c>loopia_save_qa.json</c>.
        /// Sem o argumento o arquivo e o de sempre: a versao final nao muda de lugar.
        /// </summary>
        public const string ArgumentoDoPerfil = "-loopia-perfil";

        /// <summary>
        /// productName ate a estabilizacao do MVP. Ao renomear o produto para Loopia, a pasta do
        /// Application.persistentDataPath muda; o save antigo fica em .../companyName/GameJam13-06/.
        /// </summary>
        public const string PastaDoProdutoAntigo = "GameJam13-06";

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

        /// <summary>Perfil escolhido por <see cref="ArgumentoDoPerfil"/>; null = save do jogador.</summary>
        public string Perfil { get; private set; }

        public string CaminhoDoArquivo => Path.Combine(Application.persistentDataPath, NomeDoArquivoDoPerfil(Perfil));

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
            Perfil = PerfilDaLinhaDeComando(Environment.GetCommandLineArgs());
            if (Perfil != null) Debug.Log("[DadosDoJogador] Perfil '" + Perfil + "': save em " + CaminhoDoArquivo, this);
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

            // Produto renomeado (GameJam13-06 -> Loopia): na primeira abertura o save antigo e copiado para a pasta
            // nova. So para o save do jogador; perfis de QA comecam vazios.
            if (Perfil == null)
            {
                string antigo = CaminhoDoSaveAntigo(Application.persistentDataPath);
                ResultadoDaMigracao migracao = MigrarSaveAntigo(CaminhoDoArquivo, antigo, out string erroDaMigracao);
                if (migracao == ResultadoDaMigracao.Copiado)
                    Debug.Log("[DadosDoJogador] Save copiado de " + antigo + " para " + CaminhoDoArquivo +
                              " (o original continua la).", this);
                else if (migracao == ResultadoDaMigracao.Falhou)
                    Debug.LogWarning("[DadosDoJogador] Nao consegui copiar o save antigo de " + antigo + ": " +
                                     erroDaMigracao, this);
            }

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

        /// <summary>Nome do arquivo do perfil: null/vazio = <c>loopia_save.json</c>; "qa" = <c>loopia_save_qa.json</c>.</summary>
        public static string NomeDoArquivoDoPerfil(string perfil) =>
            string.IsNullOrEmpty(perfil) ? NomeDoArquivo : "loopia_save_" + perfil + ".json";

        /// <summary>
        /// Le <c>-loopia-perfil nome</c>. O nome vira parte do arquivo, entao so aceita letras/numeros ASCII, '-' e '_'
        /// (ate 32); fora disso, ou sem o argumento, devolve null (save do jogador).
        /// </summary>
        public static string PerfilDaLinhaDeComando(string[] argumentos)
        {
            if (argumentos == null) return null;
            for (int i = 0; i < argumentos.Length - 1; i++)
            {
                if (!string.Equals(argumentos[i], ArgumentoDoPerfil, StringComparison.OrdinalIgnoreCase)) continue;
                string nome = argumentos[i + 1];
                if (string.IsNullOrEmpty(nome) || nome.Length > 32) return null;
                foreach (char c in nome)
                {
                    bool valido = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                    if (!valido) return null;
                }
                return nome.ToLowerInvariant();
            }
            return null;
        }

        /// <summary>
        /// Save do produto antigo (<see cref="PastaDoProdutoAntigo"/>), irmao da pasta atual. Null se a pasta atual ja e a
        /// antiga (produto ainda nao renomeado) ou se nao der para montar o caminho.
        /// </summary>
        public static string CaminhoDoSaveAntigo(string pastaAtual)
        {
            if (string.IsNullOrEmpty(pastaAtual)) return null;
            string limpa = pastaAtual.TrimEnd('/', '\\');
            DirectoryInfo pai = Directory.GetParent(limpa);
            if (pai == null) return null;
            if (string.Equals(Path.GetFileName(limpa), PastaDoProdutoAntigo, StringComparison.OrdinalIgnoreCase)) return null;
            return Path.Combine(pai.FullName, PastaDoProdutoAntigo, NomeDoArquivo);
        }

        public enum ResultadoDaMigracao { NadaAFazer, Copiado, Falhou }

        /// <summary>
        /// Copia o save antigo para o caminho novo quando so o antigo existe. Nunca apaga nem altera o antigo e nunca
        /// sobrescreve um save novo que ja exista.
        /// </summary>
        public static ResultadoDaMigracao MigrarSaveAntigo(string caminhoNovo, string caminhoAntigo, out string erro)
        {
            erro = null;
            if (string.IsNullOrEmpty(caminhoNovo) || string.IsNullOrEmpty(caminhoAntigo)) return ResultadoDaMigracao.NadaAFazer;
            if (File.Exists(caminhoNovo) || !File.Exists(caminhoAntigo)) return ResultadoDaMigracao.NadaAFazer;
            try
            {
                string pasta = Path.GetDirectoryName(caminhoNovo);
                if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
                File.Copy(caminhoAntigo, caminhoNovo, false);
                return ResultadoDaMigracao.Copiado;
            }
            catch (Exception e)
            {
                erro = e.Message;
                return ResultadoDaMigracao.Falhou;
            }
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
