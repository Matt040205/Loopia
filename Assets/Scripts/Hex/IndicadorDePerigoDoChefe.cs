using UnityEngine;

namespace Loopia.Hex
{
    /// <summary>
    /// Aviso visual do ataque especial do chefe: um disco vermelho sob ele que pulsa durante o aviso.
    /// So apresentacao: ouve os eventos do EncontroDoChefe e nao muda regra nenhuma, entao pode ser trocado
    /// por VFX de verdade depois sem mexer no combate.
    ///
    /// O disco e filho do chefe (fora do HexWorld e sem collider: nunca entra no bake do NavMesh) e usa um
    /// unico material compartilhado, criado uma vez; o pulso e so escala do transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class IndicadorDePerigoDoChefe : MonoBehaviour
    {
        [Tooltip("Deixe vazio para achar automaticamente na cena.")]
        public EncontroDoChefe encontro;

        [Header("Aparencia")]
        public Color cor = new Color(1f, 0.15f, 0.1f, 1f);
        [Tooltip("Diametro do disco, em unidades de mundo.")]
        [Min(0.1f)] public float diametro = 3.2f;
        [Tooltip("Quanto abaixo do chefe o disco fica.")]
        public float alturaAbaixoDoChefe = 1.6f;
        [Min(0.1f)] public float pulsosPorSegundo = 3f;

        static Material _material;

        GameObject _disco;
        float _fimDoAviso;
        bool _inscrito;

        /// <summary>O aviso esta aparecendo agora.</summary>
        public bool Visivel => _disco != null && _disco.activeSelf;

        void Start()
        {
            if (encontro == null) encontro = GetComponent<EncontroDoChefe>();
            if (encontro == null) encontro = FindFirstObjectByType<EncontroDoChefe>();
            if (encontro == null) return;

            encontro.AoAvisarEspecial += Avisar;
            encontro.AoUsarEspecial += Esconder;
            encontro.AoChefeDerrotado += Esconder;
            _inscrito = true;
        }

        void OnDestroy()
        {
            if (_inscrito && encontro != null)
            {
                encontro.AoAvisarEspecial -= Avisar;
                encontro.AoUsarEspecial -= Esconder;
                encontro.AoChefeDerrotado -= Esconder;
            }
            _inscrito = false;
            if (_disco != null) Destroy(_disco);
        }

        void Avisar(float segundos)
        {
            if (encontro.Chefe == null) return;

            if (_disco == null) _disco = CriarDisco(encontro.Chefe.transform);
            _disco.SetActive(true);
            _fimDoAviso = Time.time + Mathf.Max(0.1f, segundos);
        }

        void Esconder(int _) => Esconder();

        void Esconder()
        {
            if (_disco != null) _disco.SetActive(false);
        }

        void Update()
        {
            if (!Visivel) return;
            if (encontro == null || encontro.Chefe == null || Time.time > _fimDoAviso + 0.5f)
            {
                Esconder();
                return;
            }

            float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * pulsosPorSegundo * Mathf.PI * 2f);
            _disco.transform.localScale = new Vector3(diametro * pulso, 0.02f, diametro * pulso);
        }

        GameObject CriarDisco(Transform chefe)
        {
            var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disco.name = "Aviso de perigo";
            Collider colisor = disco.GetComponent<Collider>();
            if (colisor != null) Destroy(colisor);

            // Filho do chefe, mas sem herdar a escala dele: o disco tem tamanho de mundo.
            disco.transform.SetParent(chefe, false);
            Vector3 escalaDoPai = chefe.lossyScale;
            disco.transform.localPosition = Vector3.down * alturaAbaixoDoChefe /
                                            Mathf.Max(0.0001f, Mathf.Abs(escalaDoPai.y));
            disco.transform.localScale = new Vector3(diametro, 0.02f, diametro);

            var renderizador = disco.GetComponent<MeshRenderer>();
            if (renderizador != null)
            {
                renderizador.sharedMaterial = Material(cor);
                renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderizador.receiveShadows = false;
            }

            return disco;
        }

        static Material Material(Color cor)
        {
            if (_material != null) return _material;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            _material = new Material(shader) { name = "Aviso de perigo do chefe", hideFlags = HideFlags.DontSave };
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", cor);
            if (_material.HasProperty("_Color")) _material.SetColor("_Color", cor);
            return _material;
        }
    }
}
