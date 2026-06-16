using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider2D))]
public class Door : MonoBehaviour
{
    private string openTriggerName = "Open";
    [SerializeField] private string doorId = "Door";
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private string interactionMessage = "Presione E para abrir";
    [SerializeField] private string interactionMessageOpen = "Presione E para Entrar";
    [SerializeField] private float promptYOffset = 0.6f;
    [SerializeField] private AudioClip doorOpenClip;

    private Animator animator;
    private Collider2D doorCollider;
    private bool isOpen;
    private bool playerNearby;
    private string persistentId;

#if UNITY_EDITOR
    private const string DoorOpenClipPath = "Assets/Sound/DoorOpen 5.wav";
#endif

    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<Collider2D>();
        persistentId = BuildPersistentId();
        AutoAssignDoorOpenClip();
    }

    private void Start()
    {
        ApplyPersistentState();
    }

    private void AutoAssignDoorOpenClip()
    {
#if UNITY_EDITOR
        if (doorOpenClip == null)
        {
            doorOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorOpenClipPath);
        }
#endif
    }

    private void PlayDoorOpenSound()
    {
        if (doorOpenClip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(doorOpenClip, transform.position, 1f);
    }

    private string BuildPersistentId()
    {
        Vector3 position = transform.position;
        int x = Mathf.RoundToInt(position.x * 1000f);
        int y = Mathf.RoundToInt(position.y * 1000f);
        int z = Mathf.RoundToInt(position.z * 1000f);
        return $"{gameObject.scene.name}:{gameObject.name}:{x}:{y}:{z}";
    }

    private void ApplyPersistentState()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsWorldObjectActivated(persistentId))
        {
            return;
        }

        isOpen = true;

        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }
    }

    private void Update()
    {
        playerNearby = IsPlayerNearby();

        if (playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!isOpen)
            {
                OpenDoor();
            }
            else
            {
                TeleportPlayerToMatchingDoor();
            }
        }
    }

    private void OnGUI()
    {
        if (!playerNearby || Camera.main == null)
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

        string messageToShow = isOpen ? interactionMessageOpen : interactionMessage;
        GUI.Label(labelRect, messageToShow, style);
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

        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterWorldObjectActivated(persistentId);
        }

        PlayDoorOpenSound();

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }
    }

    private void TeleportPlayerToMatchingDoor()
    {
        if (string.IsNullOrEmpty(doorId))
        {
            return;
        }

        Player player = GetNearbyPlayer();
        if (player == null)
        {
            return;
        }

        Door[] doors = FindObjectsOfType<Door>();
        Door targetDoor = null;
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] == null || doors[i] == this)
            {
                continue;
            }

            if (doors[i].doorId == doorId)
            {
                targetDoor = doors[i];
                break;
            }
        }

        if (targetDoor == null)
        {
            return;
        }

        Vector3 teleportPosition = targetDoor.transform.position + Vector3.up * 1f;
        player.transform.position = teleportPosition;
    }

    private Player GetNearbyPlayer()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                Player player = colliders[i].GetComponentInParent<Player>();
                if (player != null)
                {
                    return player;
                }
            }
        }

        return null;
    }
}
