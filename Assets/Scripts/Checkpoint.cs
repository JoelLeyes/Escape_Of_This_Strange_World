using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class Checkpoint : MonoBehaviour
{
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

        GameManager.Instance.RegisterCheckpoint(transform.position, SceneManager.GetActiveScene().name);
    }
}