using System.Collections.Generic;
using UnityEngine;

public class PlataformaCampoBarbaro : MonoBehaviour
{
    [Header("Configuração de Spawn")]
    public GameObject prefabInimigo;
    public float intervaloSpawn = 10f;
    public int maxInimigos = 3;

    [Header("Ajuste de Altura")]
    [Tooltip("Altura extra para o inimigo aparecer no topo da plataforma/pilar.")]
    public float alturaSpawnInimigo = 1.5f;

    private List<GameObject> inimigosAtivos = new List<GameObject>();
    private float tempoProximoSpawn;
    private bool playerNaPlataforma = false;

    void Start()
    {
        // Garante que a plataforma possa ser detectada por raycasts e triggers
        gameObject.layer = LayerMask.NameToLayer("Gameplay");
    }

    void Update()
    {
        // Limpa inimigos mortos da lista
        inimigosAtivos.RemoveAll(inimigo => inimigo == null);

        // Só tenta spawnar se o player estiver na plataforma e o tempo tiver passado
        if (playerNaPlataforma && Time.time >= tempoProximoSpawn && inimigosAtivos.Count < maxInimigos)
        {
            SpawnarInimigo();
            // Reseta o timer para o próximo spawn
            tempoProximoSpawn = Time.time + intervaloSpawn;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNaPlataforma = true;
            Debug.Log("Player entrou na área do Acampamento. Spawner ativado.");

            // Spawna o primeiro inimigo imediatamente, se a plataforma estiver vazia
            if (inimigosAtivos.Count == 0)
            {
                SpawnarInimigo();
            }
            // Define o timer para o próximo spawn
            tempoProximoSpawn = Time.time + intervaloSpawn;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNaPlataforma = false;
            Debug.Log("Player saiu da área do Acampamento. Spawner pausado.");
        }
    }

    void SpawnarInimigo()
    {
        if (prefabInimigo == null) return;

        Vector3 pos = transform.position;
        // CORREÇÃO DA ALTURA: Garante que o inimigo apareça no topo
        pos.y = transform.position.y + alturaSpawnInimigo;

        GameObject novoInimigo = Instantiate(prefabInimigo, pos, Quaternion.identity);

        // Debuff da floresta profunda: lobos nascem com menos vida se o efeito estiver ativo
        InimigoBase inimigo = novoInimigo.GetComponent<InimigoBase>();
        if (inimigo != null && PlataformaFlorestaProfunda.MultiplicadorGlobalDeVidaDosLobos != 1f)
        {
            inimigo.AplicarModificadorDeVida(PlataformaFlorestaProfunda.MultiplicadorGlobalDeVidaDosLobos);
        }

        inimigosAtivos.Add(novoInimigo);
    }
}