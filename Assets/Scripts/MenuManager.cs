using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuManager : MonoBehaviour
{
    [Header("Escenas (Build Settings)")]
    [SerializeField] private int playSceneBuildIndex = 1;
    [SerializeField] private int menuSceneBuildIndex = 0;

    private void Awake()
    {
        // Por si vienes de un pause/slowmo, el menú debe quedar normal.
        Time.timeScale = 1f;
    }

    public void Play()
    {
        if (playSceneBuildIndex < 0)
        {
            Debug.LogWarning("MenuManager: 'playSceneBuildIndex' invalido.");
            return;
        }

        SceneManager.LoadScene(playSceneBuildIndex);
    }

    public void BackToMenu()
    {
        if (menuSceneBuildIndex < 0)
        {
            Debug.LogWarning("MenuManager: 'menuSceneBuildIndex' invalido.");
            return;
        }

        SceneManager.LoadScene(menuSceneBuildIndex);
    }

    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        // Para que el boton funcione cuando pruebas en el Editor.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
