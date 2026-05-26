using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    private string openTriggerName = "Open";
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private string interactionMessage = "Presione E para abrir";
    [SerializeField] private float promptYOffset = 0.6f;

    private Animator animator;
    private bool isOpen;
    private bool playerNearby;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isOpen)
        {
            playerNearby = false;
            return;
        }

        playerNearby = IsPlayerNearby();

        if (playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenDoor();
        }
    }

    private void OnGUI()
    {
        if (!playerNearby || isOpen || Camera.main == null)
        {
            return;
        }

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * promptYOffset);
        if (screenPosition.z < 0f)
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        float labelWidth = 220f;
        float labelHeight = 30f;
        Rect labelRect = new Rect(
            screenPosition.x - labelWidth * 0.5f,
            Screen.height - screenPosition.y - labelHeight,
            labelWidth,
            labelHeight);

        GUI.Label(labelRect, interactionMessage, style);
    }

    private bool IsPlayerNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].GetComponentInParent<Player>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OpenDoor()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }
    }
}
