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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        transform.position += Vector3.right * (direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag))
        {
            return;
        }

        Enemy1 enemy = other.GetComponent<Enemy1>();
        if (enemy == null)
        {
            enemy = other.GetComponentInParent<Enemy1>();
        }

        if (enemy == null)
        {
            return;
        }

        enemy.RecibirDanio(damage);
        Destroy(gameObject);
    }
}
