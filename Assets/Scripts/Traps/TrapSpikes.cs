using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapSpikes : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 999f;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
        {
            TryKillPlayer(collision.collider);
        }
    }

    private void TryKillPlayer(Collider2D other)
    {
        if (other == null) return;

        Transform root = other.transform.root;
        bool isPlayer = other.CompareTag(playerTag) || (root != null && root.CompareTag(playerTag));
        if (!isPlayer) return;

        Player player = other.GetComponentInParent<Player>();
        if (player == null && root != null)
        {
            player = root.GetComponentInChildren<Player>();
        }

        if (player == null) return;

        // Aquí puedes decidir si quieres que muera instantáneamente
        // o simplemente reciba daño.
        player.RecibirDanio(damage);
    }
}
