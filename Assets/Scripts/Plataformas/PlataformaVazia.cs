using UnityEngine;

public class PlataformaVazia : MonoBehaviour
{
    [Tooltip("Prefab da plataforma simples para recriar")]
    public GameObject plataformaSimplesPrefab;

    [Tooltip("Referência ao LoopGenerator para atualizar dicionário")]
    public LoopGenerator loopGenerator;

    // Quando quiser limpar esta plataforma (ex.: ao sinal de evento)
    public void TornarVazia()
    {
        Vector3 pos = transform.position;
        Transform parent = transform.parent;

        Destroy(gameObject);  // remove a plataforma complexa

        // Instancia uma plataforma simples no mesmo local
        GameObject simples = Instantiate(plataformaSimplesPrefab, pos, Quaternion.identity, parent);
        loopGenerator.UpdatePlatformReference(pos, simples);
    }
}
