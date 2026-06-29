using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class vida_potion : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRestoreHealth(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        TryRestoreHealth(collision.collider);
    }

    private void TryRestoreHealth(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        player.RestaurarSaludCompleta();
        Destroy(gameObject);
    }
}
