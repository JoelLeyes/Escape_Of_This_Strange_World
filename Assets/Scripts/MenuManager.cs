using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;

    private bool wasAddedDynamically;

    public void MarkAsDynamic()
    {
        wasAddedDynamically = true;
    }

    private void Awake()
    {
        if (pauseMenuCanvasGroup == null)
        {
            pauseMenuCanvasGroup = GetComponent<CanvasGroup>();
        }
        if (pauseMenuCanvasGroup == null)
        {
            pauseMenuCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        }
    }

    private void Start()
    {
        if (wasAddedDynamically)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.gameObject.name == "PlayButton")
                {
                    button.onClick.AddListener(ResumeGame);
                }
                else if (button.gameObject.name == "QuitButton")
                {
                    button.onClick.AddListener(BackToMenu);
                }
            }
        }
    }

    public void Play()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.PlayFromMenu();
    }

    public void BackToMenu()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.BackToMenu();
    }

    public void PauseGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.PauseGame();

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.alpha = 1f;
            pauseMenuCanvasGroup.blocksRaycasts = true;
            pauseMenuCanvasGroup.interactable = true;
        }

        SetHUDVisible(false);
    }

    public void ResumeGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.ResumeGame();

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.alpha = 0f;
            pauseMenuCanvasGroup.blocksRaycasts = false;
            pauseMenuCanvasGroup.interactable = false;
        }

        SetHUDVisible(true);
    }

    public void ContinueGame()
    {
        Debug.Log("MenuManager.ContinueGame() fue llamado");
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.ContinueFromCheckpoint();
    }

    public void TogglePause()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        bool willBePaused = !GameManager.Instance.IsGamePaused();

        GameManager.Instance.TogglePause();

        if (pauseMenuCanvasGroup != null)
        {
            pauseMenuCanvasGroup.alpha = willBePaused ? 1f : 0f;
            pauseMenuCanvasGroup.blocksRaycasts = willBePaused;
            pauseMenuCanvasGroup.interactable = willBePaused;
        }

        SetHUDVisible(!willBePaused);
    }

    private void SetHUDVisible(bool visible)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "HUDCanvas")
            {
                canvas.enabled = visible;
            }
        }
    }

    public void Quit()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            Application.Quit();
            return;
        }

        GameManager.Instance.QuitGame();
    }
}
