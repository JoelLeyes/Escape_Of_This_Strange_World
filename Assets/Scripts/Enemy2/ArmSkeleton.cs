using UnityEngine;

public class ArmSkeleton : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float rotationSpeed = 1080f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private float damage = 40f;
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D rb;
    private int direction = 1;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;
    }

    private void Update()
    {
        if (hasHit)
        {
            return;
        }

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * speed, 0f);
        }
        else
        {
            transform.position += Vector3.right * (direction * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || other == null)
        {
            return;
        }

        Transform root = other.transform.root;
        bool esPlayer = other.CompareTag(playerTag) || (root != null && root.CompareTag(playerTag));
        if (!esPlayer)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null && root != null)
        {
            player = root.GetComponentInChildren<Player>();
        }

        if (player == null)
        {
            return;
        }

        hasHit = true;
        player.RecibirDanio(damage);
        Destroy(gameObject);
    }
}
