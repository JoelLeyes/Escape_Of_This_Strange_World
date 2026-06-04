using UnityEngine;

public class FireBossProjectile : MonoBehaviour
{
    [SerializeField] private bool alignToDirection = true;

    private Vector2 direction = Vector2.right;
    private float speed = 3f;
    private float lifeTime = 3f;
    private Rigidbody2D rb;
    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Initialize(Vector2 newDirection, float newSpeed, float newLifeTime)
    {
        direction = newDirection.sqrMagnitude > 0f ? newDirection.normalized : Vector2.right;
        speed = newSpeed;
        lifeTime = newLifeTime;
        initialized = true;
        ApplyRotation();

        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        ApplyRotation();

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void ApplyRotation()
    {
        if (!alignToDirection)
        {
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
