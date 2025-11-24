using UnityEngine;

public class GranaderoSpawner : MonoBehaviour
{
    [Header("Configuración del Spawner")]
    public GameObject prefab;        
    public int maxSpawnCount = 10;   
    public float spawnInterval = 2f; 
    public Vector3 spawnAreaSize = new Vector3(5, 0, 5); 

    private int currentCount = 0;
    private float timer = 0f;

    void Update()
    {
        if (prefab == null || !GameManagerBDC .Instance.isBDCMode) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval && currentCount < maxSpawnCount)
        {
            SpawnPrefab();
            timer = 0f;
        }
    }

    void SpawnPrefab()
    {
        // Calcula una posición aleatoria dentro del área definida
        Vector3 randomPos = transform.position + new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        Instantiate(prefab, randomPos, Quaternion.identity);
        currentCount++;

        Debug.Log($"Spawned {prefab.name} en {randomPos}. Total: {currentCount}");
    }

    // Visualiza el área en la escena
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}

