using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MenuManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log($"ESC detectado. GameManager.Instance: {GameManager.Instance}, pauseMenuCanvasGroup: {pauseMenuCanvasGroup}");
            TogglePause();
        }
    }

    public void Play()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.StartGame();
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
