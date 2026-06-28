using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class ChestItem
{
    public GameObject prefab;       // El objeto que puede salir del cofre
    public int minAmount = 1;       // Cantidad mínima
    public int maxAmount = 1;       // Cantidad máxima
}

[RequireComponent(typeof(Collider2D))]
public class Chest : MonoBehaviour
{
    private string openTriggerName = "Open";
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private string interactionMessage = "Presione E para abrir";
    [SerializeField] private float promptYOffset = 0.6f;
    [SerializeField] private AudioClip doorOpenClip;

    // Lista de objetos posibles con sus cantidades
    [SerializeField] private List<ChestItem> possibleItems;

    private Animator animator;
    private Collider2D chestCollider;
    private bool isOpen;
    private bool playerNearby;
    private string persistentId;

#if UNITY_EDITOR
    private const string DoorOpenClipPath = "Assets/Sound/DoorOpen 5.wav";
#endif

    private void Awake()
    {
        animator = GetComponent<Animator>();
        chestCollider = GetComponent<Collider2D>();
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
        if (chestCollider != null)
        {
            chestCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(openTriggerName);
        }
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
            OpenChest();
        }
    }

    private void OnGUI()
    {
        if (!playerNearby || isOpen || Camera.main == null || (GameManager.Instance != null && GameManager.Instance.IsGamePaused()))
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

    private void OpenChest()
    {
        if (isOpen) return;
        isOpen = true;

        if (chestCollider != null)
        {
            chestCollider.enabled = false;
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

        // Generar solo un item visible por tipo y transmitir la cantidad real al pickup
        if (possibleItems != null && possibleItems.Count > 0)
        {
            foreach (ChestItem item in possibleItems)
            {
                if (item.prefab != null)
                {
                    int amount = Random.Range(item.minAmount, item.maxAmount + 1);
                    if (amount <= 0)
                    {
                        continue;
                    }

                    Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                    GameObject spawned = Instantiate(item.prefab, spawnPos, Quaternion.identity);
                    spawned.SetActive(true);

                    Arrow_Item arrowItem = spawned.GetComponentInChildren<Arrow_Item>();
                    if (arrowItem != null)
                    {
                        // Mantener la lógica de 10 flechas por unidad de cantidad de item
                        arrowItem.amount = amount * 10;
                    }
                }
            }
        }
    }
}
