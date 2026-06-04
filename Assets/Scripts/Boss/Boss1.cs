using UnityEngine;

public class Boss1 : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private Transform[] points;
    [SerializeField] private string pointsTag = "BossPoint";
    [SerializeField] private float speed = 2f;
    [SerializeField] private float arrivalDistance = 0.1f;
    [SerializeField] private bool pickDifferentPoint = true;
    [SerializeField] private float waitAtPointSeconds = 10f;

    [Header("Ataque Magico")]
    [SerializeField, Range(0f, 1f)] private float magicChance = 0.4f;
    [SerializeField] private float magicMinDelay = 0f;
    [SerializeField] private float magicMaxDelay = 10f;
    [SerializeField] private GameObject fireBossPrefab;
    [SerializeField] private Vector2 fireBossSpawnOffset = Vector2.zero;
    [SerializeField] private float fireBossSpeed = 3f;
    [SerializeField] private float fireBossLifeTime = 3f;

    [Header("Angulos FireBoss")]
    [SerializeField] private float fireBossAngle0 = 0f;
    [SerializeField] private float fireBossAngle1 = 45f;
    [SerializeField] private float fireBossAngle2 = 90f;
    [SerializeField] private float fireBossAngle3 = 135f;
    [SerializeField] private float fireBossAngle4 = 180f;

    private Animator animator;
    private Transform currentTarget;
    private int currentIndex = -1;
    private bool isWaiting;
    private float waitEndTime;
    private bool magicScheduled;
    private float magicTriggerTime;

    private void Start()
    {
        animator = GetComponent<Animator>();
        EnsurePoints();
        PickNextTarget();
    }

    private void Update()
    {
        if (isWaiting)
        {
            if (magicScheduled && Time.time >= magicTriggerTime)
            {
                magicScheduled = false;
                if (Random.value <= magicChance)
                {
                    TriggerMagic();
                }
                ScheduleNextMagic();
            }

            if (Time.time >= waitEndTime)
            {
                isWaiting = false;
                PickNextTarget();
            }
            return;
        }

        if (currentTarget == null)
        {
            if (points == null || points.Length == 0)
            {
                return;
            }

            PickNextTarget();
            return;
        }

        Vector3 pos = transform.position;
        Vector3 targetPos = currentTarget.position;
        Vector2 nextPos = Vector2.MoveTowards(new Vector2(pos.x, pos.y), new Vector2(targetPos.x, targetPos.y), speed * Time.deltaTime);
        transform.position = new Vector3(nextPos.x, nextPos.y, pos.z);

        float remaining = Vector2.Distance(new Vector2(nextPos.x, nextPos.y), new Vector2(targetPos.x, targetPos.y));
        if (remaining <= arrivalDistance)
        {
            StartWaitAtPoint();
        }
    }

    private void StartWaitAtPoint()
    {
        if (waitAtPointSeconds <= 0f)
        {
            PickNextTarget();
            return;
        }

        isWaiting = true;
        waitEndTime = Time.time + waitAtPointSeconds;
        TriggerIdle();
        ScheduleNextMagic();
    }

    private void ScheduleNextMagic()
    {
        magicScheduled = false;

        if (waitAtPointSeconds <= 0f || magicChance <= 0f)
        {
            return;
        }

        float remaining = waitEndTime - Time.time;
        if (remaining <= 0f)
        {
            return;
        }

        float maxDelay = Mathf.Max(magicMinDelay, magicMaxDelay);
        maxDelay = Mathf.Min(remaining, maxDelay);
        float minDelay = Mathf.Clamp(magicMinDelay, 0f, maxDelay);

        float delay = maxDelay <= 0f ? 0f : Random.Range(minDelay, maxDelay);
        magicTriggerTime = Time.time + delay;
        magicScheduled = true;
    }

    private void EnsurePoints()
    {
        if (points != null && points.Length > 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(pointsTag))
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(pointsTag);
            if (tagged != null && tagged.Length > 0)
            {
                points = new Transform[tagged.Length];
                for (int i = 0; i < tagged.Length; i++)
                {
                    points[i] = tagged[i] != null ? tagged[i].transform : null;
                }
                return;
            }
        }

        Transform[] named = new Transform[3];
        for (int i = 0; i < named.Length; i++)
        {
            GameObject go = GameObject.Find("Point" + (i + 1));
            named[i] = go != null ? go.transform : null;
        }

        points = named;
    }

    private void PickNextTarget()
    {
        if (points == null || points.Length == 0)
        {
            currentTarget = null;
            return;
        }

        int nextIndex = Random.Range(0, points.Length);
        if (pickDifferentPoint && points.Length > 1)
        {
            int safety = 0;
            while (nextIndex == currentIndex && safety < 10)
            {
                nextIndex = Random.Range(0, points.Length);
                safety++;
            }
        }

        currentIndex = nextIndex;
        currentTarget = points[currentIndex];
        TriggerMove();
    }

    private void TriggerMove()
    {
        if (animator != null)
        {
            animator.SetTrigger("move");
        }
    }

    private void TriggerMagic()
    {
        if (animator != null)
        {
            animator.SetTrigger("magic");
        }
    }

    private void TriggerIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger("idle");
        }
    }

    public void Boss_Magic5_AnimationEnd()
    {
        SpawnFireBossAtAngle(fireBossAngle0);
        SpawnFireBossAtAngle(fireBossAngle1);
        SpawnFireBossAtAngle(fireBossAngle2);
        SpawnFireBossAtAngle(fireBossAngle3);
        SpawnFireBossAtAngle(fireBossAngle4);
    }

    private void SpawnFireBossAtAngle(float angleDeg)
    {
        if (fireBossPrefab == null)
        {
            Debug.LogWarning("Boss1: FireBoss prefab no asignado.", this);
            return;
        }

        Vector3 spawnPos = transform.position + (Vector3)fireBossSpawnOffset;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleDeg);
        GameObject instance = Instantiate(fireBossPrefab, spawnPos, rotation);

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector2.right;
        }

        FireBossProjectile projectile = instance.GetComponent<FireBossProjectile>();
        if (projectile == null)
        {
            projectile = instance.AddComponent<FireBossProjectile>();
        }

        projectile.Initialize(dir, fireBossSpeed, fireBossLifeTime);
    }
}
