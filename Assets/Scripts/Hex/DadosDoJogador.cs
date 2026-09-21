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

        /// <summary>O formato do arquivo. Os nomes dos campos sao as chaves do JSON.</summary>
        [Serializable]
        class Save
        {
            public int moedas;
            public int recordeDeVoltas;
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

        public void Salvar()
        {
            var dados = new Save { moedas = MoedasTotais, recordeDeVoltas = RecordeDeVoltas };

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

            if (!File.Exists(CaminhoDoArquivo)) return;

            try
            {
                Save dados = JsonUtility.FromJson<Save>(File.ReadAllText(CaminhoDoArquivo));
                if (dados == null) return;

                MoedasTotais = Mathf.Max(0, dados.moedas);
                RecordeDeVoltas = Mathf.Max(0, dados.recordeDeVoltas);
            }
            catch (Exception erro)
            {
                // Arquivo corrompido: melhor comecar do zero do que travar o jogo na abertura.
                Debug.LogWarning("[DadosDoJogador] Save ilegivel, comecando do zero: " + erro.Message, this);
            }
        }

        /// <summary>Zera o progresso salvo. Util para testar a partir do inicio.</summary>
        [ContextMenu("Apagar progresso")]
        public void ApagarProgresso()
        {
            MoedasTotais = 0;
            RecordeDeVoltas = 0;
            Salvar();
            AoMudar?.Invoke();
        }
    }
}
