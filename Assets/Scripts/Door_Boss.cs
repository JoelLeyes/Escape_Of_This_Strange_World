using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Door_Boss : MonoBehaviour
{
    private string openTriggerName = "Open";
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private string interactionMessage = "Presione E para abrir";
    [SerializeField] private string missingKeyMessage = "Necesitas la KeyBoss";
    [SerializeField] private float missingKeyMessageDuration = 1.5f;
    [SerializeField] private float promptYOffset = 1.55f;

    private Animator animator;
    private bool isOpen;
    private bool playerNearby;
    private bool openAnimationEnded;
    private bool hasLoadedScene;
    private float missingKeyMessageEndTime;

    [Header("Scene")]
    [SerializeField] private string bossSceneName = "Level1Boss";

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
            if (HasKeyBossPlayerNearby())
            {
                OpenDoorBoss();
            }
            else
            {
                ShowMissingKeyMessage();
            }
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
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.2f, 0.2f, 1f) }
        };

        string messageToShow = Time.time < missingKeyMessageEndTime ? missingKeyMessage : interactionMessage;

        float labelWidth = 320f;
        float labelHeight = 42f;
        Rect labelRect = new Rect(
            screenPosition.x - labelWidth * 0.5f,
            Screen.height - screenPosition.y - labelHeight,
            labelWidth,
            labelHeight);

        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        Rect shadowRect = new Rect(labelRect.x + 3f, labelRect.y + 3f, labelRect.width, labelRect.height);
        GUI.Label(shadowRect, messageToShow, shadowStyle);
        GUI.Label(labelRect, messageToShow, style);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryEnterBossScene(other != null ? other.gameObject : null);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
            return;

        TryEnterBossScene(collision.gameObject);
    }

    private void TryEnterBossScene(GameObject other)
    {
        if (hasLoadedScene || !openAnimationEnded || other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();
        if (player == null || !player.HasKeyBoss())
        {
            return;
        }

        hasLoadedScene = true;
        SceneManager.LoadScene(bossSceneName);
    }

    private bool HasKeyBossPlayerNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null)
            {
                continue;
            }

            Player player = colliders[i].GetComponentInParent<Player>();
            if (player != null && player.HasKeyBoss())
            {
                return true;
            }
        }

        return false;
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

    private void OpenDoorBoss()
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

    private void ShowMissingKeyMessage()
    {
        missingKeyMessageEndTime = Time.time + Mathf.Max(0.1f, missingKeyMessageDuration);
    }

    // Called by the animation event `Door_Boss_AnimationEnd`
    public void Door_Boss_AnimationEnd()
    {
        openAnimationEnded = true;
    }
}
