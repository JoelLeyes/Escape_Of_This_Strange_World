using UnityEngine;

public class ArmSkeleton : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D rb;
    private int direction = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

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
}
