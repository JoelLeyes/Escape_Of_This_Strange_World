using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyBoss : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        TryCollect(collision.collider);
    }

    private void TryCollect(Collider2D other)
    {
        if (other == null)
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

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        player.CollectKeyBoss(sprite);
        Destroy(gameObject);
    }
}
