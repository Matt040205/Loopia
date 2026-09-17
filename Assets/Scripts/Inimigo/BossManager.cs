using UnityEngine;

public class BossManager : MonoBehaviour
{
    [Header("Configuração do Chefão")]
    [Tooltip("Após completar este número de loops, o chefão aparece no centro do grid.")]
    public int loopsParaBoss = 2;
    [Tooltip("Prefab do chefão (InimigoBase com stats altos).")]
    public GameObject bossPrefab;
    [Tooltip("Multiplicador extra aplicado aos stats do boss.")]
    public float fatorBoss = 8f;
    [Tooltip("Altura extra do spawn em relação ao centro do grid.")]
    public float alturaDoSpawn = 1f;

    private LoopGenerator loopGenerator;
    private bool bossSpawnou = false;

    void OnEnable()
    {
        GameManager.OnLoopAvancado += VerificarSpawn;
    }

    void OnDisable()
    {
        GameManager.OnLoopAvancado -= VerificarSpawn;
    }

    void Start()
    {
        loopGenerator = FindFirstObjectByType<LoopGenerator>();
        VerificarSpawn();
    }

    private void VerificarSpawn()
    {
        if (bossSpawnou || bossPrefab == null) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.numeroDoLoop <= loopsParaBoss) return;

        SpawnarBoss();
    }

    private void SpawnarBoss()
    {
        bossSpawnou = true;

        if (loopGenerator == null)
        {
            loopGenerator = FindFirstObjectByType<LoopGenerator>();
        }

        // Centro do loop (hexágono 0,0)
        Vector3 centro = loopGenerator != null
            ? loopGenerator.HexToWorld(0, 0)
            : transform.position;
        centro.y += alturaDoSpawn;

        GameObject boss = Instantiate(bossPrefab, centro, Quaternion.identity);
        InimigoBase inimigoBoss = boss.GetComponent<InimigoBase>();
        if (inimigoBoss != null)
        {
            inimigoBoss.AplicarMultiplicadorExtra(fatorBoss);
        }

        Debug.Log($"<color=red><b>CHEFÃO SPAWNOU no centro do loop!</b> Loop {GameManager.Instance.numeroDoLoop}.</color>");
    }
}
