using UnityEngine;

public sealed class MenuManager : MonoBehaviour
{
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
    }

    public void ResumeGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MenuManager: no se encontro GameManager.");
            return;
        }

        GameManager.Instance.ResumeGame();
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

        GameManager.Instance.TogglePause();
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
