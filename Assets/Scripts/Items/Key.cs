using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Key : MonoBehaviour
{
    public int id = 0;

    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        ConfigurePickupCollider();
    }

    private void OnValidate()
    {
        ConfigurePickupCollider();
    }

    private void ConfigurePickupCollider()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            return;
        }

        boxCollider.isTrigger = true;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                boxCollider.size = spriteSize;
                boxCollider.offset = Vector2.zero;
                return;
            }
        }

        if (boxCollider.size.x <= 0.01f || boxCollider.size.y <= 0.01f)
        {
            boxCollider.size = new Vector2(0.26f, 0.14f);
            boxCollider.offset = Vector2.zero;
        }
    }

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

        player.CollectKey(id);
        Destroy(gameObject);
    }
}
