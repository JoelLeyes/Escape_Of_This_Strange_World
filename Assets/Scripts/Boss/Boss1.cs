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

    private Animator animator;
    private Transform currentTarget;
    private int currentIndex = -1;
    private bool isWaiting;
    private float waitEndTime;

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
}
