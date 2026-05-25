using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapAxe : MonoBehaviour
{
    [Header("Swing")]
    [SerializeField] private float swingAngle = 60f;
    [SerializeField] private float swingSpeed = 2f;

    [Header("Damage")]
    [SerializeField] private float damage = 999f;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private string playerTag = "Player";

    private Quaternion initialRotation;
    private float nextHitTime;

    private void Awake()
    {
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
        {
            TryDamagePlayer(collision.collider);
        }
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (other == null || Time.time < nextHitTime)
        {
            return;
        }

        Transform root = other.transform.root;
        bool isPlayer = other.CompareTag(playerTag) || (root != null && root.CompareTag(playerTag));
        if (!isPlayer)
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

        nextHitTime = Time.time + hitCooldown;
        player.RecibirDanio(damage);
    }
}