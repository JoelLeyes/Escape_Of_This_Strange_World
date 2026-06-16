using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class Checkpoint : MonoBehaviour
{
    [SerializeField] private float respawnYOffset = 0.4f;

    private void Reset()
    {
        Collider2D collider2D = GetComponent<Collider2D>();
        collider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        Debug.Log($"Checkpoint activado por {other.name}", this);

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("Checkpoint: no se encontro GameManager.", this);
            return;
        }

        Vector3 checkpointPosition = transform.position;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            checkpointPosition.y += playerCollider.bounds.extents.y + respawnYOffset;
        }
        else
        {
            checkpointPosition.y += respawnYOffset;
        }

        checkpointPosition.z = 0f;
        GameManager.Instance.RegisterCheckpoint(checkpointPosition, SceneManager.GetActiveScene().name);
    }
}