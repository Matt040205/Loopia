using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Loopia.Hex.Tests
{
    /// <summary>
    /// Confere a cena HexPrototipo sem entrar em Play Mode e sem modificar nem salvar nada.
    /// A cena e aberta em modo Single e a cena original e reaberta no fim.
    /// </summary>
    [Category("SceneStatic")]
    public class HexPrototipoSceneTests
    {
        const BindingFlags Campos = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Scene _cena;

        [OneTimeSetUp]
        public void AbrirCena()
        {
            CenaDoTeste.AbrirCenaPrincipal();
            _cena = SceneManager.GetActiveScene();
        }

        [OneTimeTearDown]
        public void Restaurar() => CenaDoTeste.Restaurar();

        static T[] Todos<T>() where T : UnityEngine.Object
            => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        static T Unico<T>() where T : UnityEngine.Object
        {
            T[] todos = Todos<T>();
            Assert.AreEqual(1, todos.Length, "esperava exatamente um " + typeof(T).Name + " na cena");
            return todos[0];
        }

        [Test]
        public void CenaPrincipalAbriu_ENaoFoiModificada()
        {
            Assert.AreEqual(CenaDoTeste.CenaPrincipal, _cena.path);
            Assert.IsFalse(_cena.isDirty);
        }

        [Test]
        public void TemExatamenteUmDeCadaComponenteDeGameplay()
        {
            Unico<HexWorld>();
            Unico<HexLoopGenerator>();
            Unico<NavegacaoDoMundo>();
            Unico<HexLoopRunner>();
            Unico<LoopDirector>();
            Unico<MapaDePlataformas>();
            Unico<MaoDeCartas>();
            Unico<HexPlayer>();
        }

        [Test]
        public void NavMeshSurface_EstaNoObjetoDoHexWorld_ENavegacaoApontaParaEla()
        {
            var mundo = Unico<HexWorld>();
            var surface = mundo.GetComponent<NavMeshSurface>();
            Assert.IsNotNull(surface, "NavMeshSurface tem que estar no mesmo objeto do HexWorld");

            var nav = Unico<NavegacaoDoMundo>();
            Assert.IsTrue(nav.superficie == null || nav.superficie == surface,
                "NavegacaoDoMundo.superficie, se atribuida, tem que ser a do HexWorld");
        }

        [Test]
        public void ParametrosSerializados_BatemComOQueOsTestesAssumem()
        {
            var mundo = Unico<HexWorld>();
            Assert.AreEqual(3f, mundo.tamanhoDoHexagono, 1e-4f);
            Assert.AreEqual(0.3f, mundo.folgaEntreHexagonos, 1e-4f);
            Assert.AreEqual(0.6f, mundo.espessura, 1e-4f);

            var g = Unico<HexLoopGenerator>();
            Assert.AreEqual(4, g.raioMinimo);
            // Sprint 4 (legibilidade): 6 gerava aneis de 36 casas, que nunca foram validados e deixavam as ilhas com
            // ~60% do tamanho medido no Pass A (anel 24-30). Com 5 todos os aneis ficam na faixa validada.
            Assert.AreEqual(5, g.raioMaximo);
            Assert.AreEqual(400, g.deformacoes);
            Assert.AreEqual(9, g.mioloMinimo);
            Assert.IsFalse(g.usarSementeFixa, "o jogo sorteia um mapa novo a cada partida");
            Assert.AreEqual(12345, g.semente);

            var d = Unico<LoopDirector>();
            Assert.AreEqual(4, d.voltasParaOBoss);
            Assert.AreEqual(0.10f, d.crescimentoDosInimigos, 1e-4f);

            var mao = Unico<MaoDeCartas>();
            Assert.AreEqual(4, mao.espacos);
        }

        // --- UnityEvents ---

        static bool MetodoExiste(UnityEngine.Object alvo, string nome)
        {
            for (Type t = alvo.GetType(); t != null; t = t.BaseType)
                foreach (MethodInfo m in t.GetMethods(Campos | BindingFlags.DeclaredOnly))
                    if (m.Name == nome) return true;
            return false;
        }

        [Test]
        public void LoopDirector_AoGameOver_TemListenerValido()
        {
            var d = Unico<LoopDirector>();
            int n = d.aoGameOver.GetPersistentEventCount();
            Assert.GreaterOrEqual(n, 1, "aoGameOver perdeu o listener (TelaDeMorte.Mostrar)");
            for (int i = 0; i < n; i++)
            {
                UnityEngine.Object alvo = d.aoGameOver.GetPersistentTarget(i);
                string metodo = d.aoGameOver.GetPersistentMethodName(i);
                Assert.IsNotNull(alvo, "listener " + i + " com alvo nulo");
                Assert.IsTrue(MetodoExiste(alvo, metodo), "metodo '" + metodo + "' nao existe em " + alvo.GetType().Name);
            }

            // Hoje o listener e TelaDeMorte.Mostrar.
            Assert.IsInstanceOf<TelaDeMorte>(d.aoGameOver.GetPersistentTarget(0));
            Assert.AreEqual("Mostrar", d.aoGameOver.GetPersistentMethodName(0));
        }

        [Test]
        public void UnityEventsDosComponentesLoopiaHex_NaoTemChamadasQuebradas()
        {
            int eventos = 0;
            foreach (MonoBehaviour mb in Todos<MonoBehaviour>())
            {
                if (mb == null || mb.GetType().Namespace != "Loopia.Hex") continue;
                for (Type t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                    foreach (FieldInfo f in t.GetFields(Campos | BindingFlags.DeclaredOnly))
                    {
                        if (!typeof(UnityEventBase).IsAssignableFrom(f.FieldType)) continue;
                        var ev = f.GetValue(mb) as UnityEventBase;
                        if (ev == null) continue;
                        eventos++;
                        for (int i = 0; i < ev.GetPersistentEventCount(); i++)
                        {
                            UnityEngine.Object alvo = ev.GetPersistentTarget(i);
                            string metodo = ev.GetPersistentMethodName(i);
                            string onde = mb.GetType().Name + "." + f.Name + "[" + i + "]";
                            Assert.IsNotNull(alvo, onde + ": alvo nulo (listener perdido)");
                            Assert.IsFalse(string.IsNullOrEmpty(metodo), onde + ": metodo vazio");
                            Assert.IsTrue(MetodoExiste(alvo, metodo), onde + ": metodo '" + metodo + "' nao existe em " + alvo.GetType().Name);
                        }
                    }
            }
            Assert.GreaterOrEqual(eventos, 1, "o scan nao achou nenhum UnityEvent (reflexao quebrou?)");
        }

        [Test]
        public void TodosOsPersistentCallsSerializadosDaCena_TemAlvo()
        {
            // Varre o YAML serializado de TODOS os componentes (inclui UI/TMP): m_Calls com m_Target nulo e
            // metodo preenchido e um listener quebrado (script movido/renomeado).
            int chamadas = 0;
            var quebrados = new List<string>();
            foreach (UnityEngine.Object o in Todos<Component>())
            {
                var so = new SerializedObject(o);
                SerializedProperty it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.name != "m_Calls" || !it.isArray) continue;
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        chamadas++;
                        SerializedProperty c = it.GetArrayElementAtIndex(i);
                        bool semAlvo = c.FindPropertyRelative("m_Target").objectReferenceValue == null;
                        string metodo = c.FindPropertyRelative("m_MethodName").stringValue;
                        if (semAlvo && !string.IsNullOrEmpty(metodo))
                            quebrados.Add(o.name + "/" + o.GetType().Name + " -> " + metodo);
                    }
                }
            }
            Debug.Log("[Testes] persistent calls serializados na cena: " + chamadas);
            CollectionAssert.IsEmpty(quebrados, "listeners sem alvo: " + string.Join("; ", quebrados));
            Assert.GreaterOrEqual(chamadas, 1);
        }

        // --- Scripts faltando e assets ---

        [Test]
        public void NenhumObjetoDaCenaTemScriptFaltando()
        {
            var problemas = new List<string>();
            foreach (GameObject raiz in _cena.GetRootGameObjects())
                foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                {
                    int faltando = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (faltando > 0) problemas.Add(t.name + " (" + faltando + ")");
                }
            CollectionAssert.IsEmpty(problemas, "objetos com script faltando: " + string.Join(", ", problemas));
        }

        [Test]
        public void TodosOsAssetsDePlataformaCarregamComoPlataformaDef()
        {
            string[] arquivos = Directory.GetFiles("Assets/Dados/Plataformas", "*.asset");
            Assert.GreaterOrEqual(arquivos.Length, 11, "esperava as 11 cartas do GDD");

            var semModelo = new List<string>();
            var quebrados = new List<string>();
            foreach (string arquivo in arquivos)
            {
                string caminho = arquivo.Replace('\\', '/');
                var def = AssetDatabase.LoadAssetAtPath<PlataformaDef>(caminho);
                if (def == null) { quebrados.Add(caminho); continue; }
                if (def.modelo == null) semModelo.Add(def.name);
            }

            // Informativo: cartas sem modelo 3D ainda funcionam (so nao mostram item no centro).
            Debug.Log("[Testes] cartas sem 'modelo' (informativo, nao e falha): " +
                      (semModelo.Count == 0 ? "(nenhuma)" : string.Join(", ", semModelo)));
            CollectionAssert.IsEmpty(quebrados, "assets que nao carregam como PlataformaDef (m_Script quebrado?): " + string.Join(", ", quebrados));
        }

        [Test]
        public void BaralhoDaMao_SoTemCartasValidas()
        {
            var mao = Unico<MaoDeCartas>();
            Assert.Greater(mao.baralho.Count, 0, "baralho vazio");
            for (int i = 0; i < mao.baralho.Count; i++) Assert.IsNotNull(mao.baralho[i], "baralho[" + i + "] nulo");
            foreach (PlataformaDef g in mao.cartasGarantidas) Assert.IsNotNull(g, "carta garantida nula");
        }
    }
}
