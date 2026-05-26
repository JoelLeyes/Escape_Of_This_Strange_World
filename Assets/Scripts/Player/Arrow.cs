using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string attackTag = "Player_Attack";

    private int direction = 1;
    private Rigidbody2D rb;
    private Animator animator;
    private bool hasCollided = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        gameObject.tag = attackTag;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Initialize(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (hasCollided)
        {
            return;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        transform.position += Vector3.right * (direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        TryHit(collision.collider);
    }

    private void TryHit(Collider2D other)
    {
        if (hasCollided || other == null)
        {
            return;
        }

        Transform root = other.transform.root;
        bool esEnemyPorTag = other.CompareTag(enemyTag) || (root != null && root.CompareTag(enemyTag));

        if (!esEnemyPorTag)
        {
            return;
        }

        Enemy2 enemy2 = other.GetComponentInParent<Enemy2>();
        Enemy1 enemy1 = other.GetComponentInParent<Enemy1>();

        if (enemy2 == null && enemy1 == null)
        {
            return;
        }

        hasCollided = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            rb.simulated = false;
        }

        if (enemy2 != null)
        {
            enemy2.RecibirDanio(damage);
        }
        else
        {
            enemy1.RecibirDanio(damage);
        }

        if (animator != null)
        {
            animator.SetTrigger("Target");
        }
        else
        {
            // If there's no animator on the prefab, destroy immediately after applying damage
            Destroy(gameObject);
        }
    }

    // Called by animation event
    public void Burst_End()
    {
        Destroy(gameObject);
    }
}
