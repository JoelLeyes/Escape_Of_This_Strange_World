using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Chest : MonoBehaviour
{
    // Tag que deben tener los ataques del jugador (por ejemplo, el prefab de Fire).
    // Lo dejamos como constante para evitar que Unity lo "guarde" en el Inspector y se desincronice.
    private const string AttackTag = "Player_Attack";
    private string openTriggerName = "Open";

    private Animator animator;
    private bool isOpen;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen)
        {
            return;
        }

        // Detecta el ataque aunque el collider esté en un hijo del prefab.
        string otherTag = other.tag;
        string rootTag = other.transform.root.tag;

        if (otherTag != AttackTag && rootTag != AttackTag)
        {
            return;
        }

        OpenChest();

        // Destruir el proyectil al impactar el cofre.
        GameObject projectileRoot = other.transform.root.gameObject;
        if (projectileRoot != gameObject)
        {
            Destroy(projectileRoot);
        }
    }

    private void OpenChest()
    {
        isOpen = true;

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }
    }
}
