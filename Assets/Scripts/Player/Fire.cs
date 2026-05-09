using UnityEngine;

public class Fire : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string attackTag = "Player_Attack";

    private int direction = 1;
    private Rigidbody2D rb;
    private Animator animator;

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

    // Update is called once per frame
    void Update()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Stop movement
        }

        transform.position += Vector3.right * (direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag) && !other.CompareTag("Object_Destroyable"))
        {
            return;
        }

        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true; // Ensure it doesn't move further
        }

        // Handle damage for enemies
        Enemy1 enemy = other.GetComponent<Enemy1>();
        if (enemy == null)
        {
            enemy = other.GetComponentInParent<Enemy1>();
        }

        if (enemy != null)
        {
            enemy.RecibirDanio(damage);
        }

        // Trigger explosion animation
        if (animator != null)
        {
            animator.SetTrigger("Target");
        }
        else
        {
            Destroy(gameObject); // Fallback in case animator is missing
        }
    }

    // This method will be called by the animation event Burst_End
    public void Burst_End()
    {
        Destroy(gameObject);
    }
}
