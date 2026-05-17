using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Chest : MonoBehaviour
{
    private string openTriggerName = "Open";

    private Animator animator;
    private bool isOpen;

    private void Awake()
    {
        animator = GetComponent<Animator>();
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
