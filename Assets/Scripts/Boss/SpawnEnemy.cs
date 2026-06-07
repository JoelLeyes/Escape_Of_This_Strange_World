using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool useThisTransformAsSpawnPoint = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject enemy1Prefab;
    [SerializeField] private GameObject enemy2Prefab;

    [Header("Timing")]
    [SerializeField] private float minSpawnDelay = 2f;
    [SerializeField] private float maxSpawnDelay = 5f;
    [SerializeField] private int maxAlive = 0;

    private readonly List<GameObject> alive = new List<GameObject>();
    private Coroutine spawnRoutine;

    private void Start()
    {
        if (spawnPoint == null && useThisTransformAsSpawnPoint)
        {
            spawnPoint = transform;
        }

        if (enemy1Prefab == null && enemy2Prefab == null)
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
        GameObject prefab = PickRandomPrefab();
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        alive.Add(instance);
    }

    private GameObject PickRandomPrefab()
    {
        if (enemy1Prefab != null && enemy2Prefab != null)
        {
            return Random.value < 0.5f ? enemy1Prefab : enemy2Prefab;
        }

        if (enemy1Prefab != null)
        {
            return enemy1Prefab;
        }

        return enemy2Prefab;
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
