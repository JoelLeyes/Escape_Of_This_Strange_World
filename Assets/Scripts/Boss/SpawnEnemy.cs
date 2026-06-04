using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Timing")]
    [SerializeField] private float minSpawnDelay = 2f;
    [SerializeField] private float maxSpawnDelay = 5f;
    [SerializeField] private int maxAlive = 0;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float delay = GetRandomDelay();
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }

            CleanupAlive();
            if (maxAlive > 0 && alive.Count >= maxAlive)
            {
                continue;
            }

            SpawnOne();
        }
    }

    private float GetRandomDelay()
    {
        float min = Mathf.Max(0f, minSpawnDelay);
        float max = Mathf.Max(min, maxSpawnDelay);
        return Random.Range(min, max);
    }

    private void SpawnOne()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity);
        alive.Add(instance);
    }

    private void CleanupAlive()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
            }
        }
    }
}
