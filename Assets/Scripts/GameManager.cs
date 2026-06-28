using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string menuSceneName = "Menú";
    [SerializeField] private string gameplaySceneName = "IntroEscena";
    [SerializeField] private string level1SceneName = "Level1";
    [SerializeField] private string gameOverSceneName = "PantallaNivelPerdido";

    [Header("Mensajes")]
    [SerializeField] private float checkpointMessageDuration = 2f;
    [SerializeField] private float checkpointMessageTopOffset = 24f;

    [Header("Derrota")]
    [SerializeField] private float gameOverDelay = 3f;

    [Header("Musica")]
    [SerializeField] private AudioClip level1MusicClip;
    [SerializeField] private AudioClip level1BossMusicClip;

    private const string CheckpointSavedMessage = "Punto salvado";

    private bool isPaused;
    private bool hasCheckpoint;
    private Vector3 checkpointPosition;
    private string checkpointSceneName;
    private readonly HashSet<string> activatedWorldObjectIds = new HashSet<string>();
    private string activeCheckpointMessage;
    private float activeCheckpointMessageEndTime;
    private GUIStyle checkpointMessageStyle;
    private AudioSource musicAudioSource;
    private string lastGameplaySceneName;
    private Coroutine gameOverRoutine;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject gameManagerObject = new GameObject(nameof(GameManager));
        gameManagerObject.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureMusicAudioSource();
        AutoAssignMusicClips();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResumeGame();
        UpdateSceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName != menuSceneName && currentSceneName != gameOverSceneName && currentSceneName != gameplaySceneName)
            {
                if (isPaused)
                {
                    Scene menuScene = SceneManager.GetSceneByName(menuSceneName);
                    if (menuScene.isLoaded)
                    {
                        SceneManager.UnloadSceneAsync(menuSceneName);
                        ResumeGame();
                    }
                }
                else
                {
                    Debug.Log($"Tecla ESC presionada en {currentSceneName}. Volviendo al menú de pausa.");
                    PauseToMenu();
                }
            }
        }
    }

    public void PlayFromMenu()
    {
        Scene menuScene = SceneManager.GetSceneByName(menuSceneName);
        if (IsGamePaused() && menuScene.isLoaded && SceneManager.GetActiveScene().name != menuSceneName)
        {
            SceneManager.UnloadSceneAsync(menuSceneName);
            ResumeGame();
        }
        else if (!string.IsNullOrEmpty(lastGameplaySceneName))
        {
            ContinueFromCheckpoint();
        }
        else
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        ClearCheckpoint();
        ClearWorldObjectStates();
        ClearCheckpointMessage();
        Player.ResetPersistentInstance();
        lastGameplaySceneName = null;
        LoadScene(gameplaySceneName);
    }

    public void PauseToMenu()
    {
        if (isPaused) return;

        PauseGame();
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Additive);
    }

    public void BackToMenu()
    {
        ClearCheckpoint();
        ClearWorldObjectStates();
        ClearCheckpointMessage();
        Player.ResetPersistentInstance();
        lastGameplaySceneName = null;
        LoadScene(menuSceneName);
    }

    public void GameOver()
    {
        ClearCheckpointMessage();

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
        }

        gameOverRoutine = StartCoroutine(LoadGameOverAfterDelay());
    }

    public void ContinueFromCheckpoint()
    {
        if (hasCheckpoint && !string.IsNullOrWhiteSpace(checkpointSceneName))
        {
            if (Player.Instance != null)
            {
                Player.Instance.ReviveFromCheckpoint();
            }

            LoadScene(checkpointSceneName);
            return;
        }

        Player.ResetPersistentInstance();
        if (!string.IsNullOrEmpty(lastGameplaySceneName))
        {
            LoadScene(lastGameplaySceneName);
        }
        else
        {
            LoadScene(gameplaySceneName);
        }
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        SetHUDVisible(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetHUDVisible(true);
    }

    public void TogglePause()
    {
        if (isPaused || Time.timeScale == 0f)
        {
            ResumeGame();
            return;
        }

        PauseGame();
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

    public bool IsGamePaused()
    {
        return isPaused || Time.timeScale == 0f;
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("GameManager: scene name is empty.", this);
            return;
        }

        ClearCheckpointMessage();
        ResumeGame();
        SceneManager.LoadScene(sceneName);
    }

    private System.Collections.IEnumerator LoadGameOverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, gameOverDelay));
        gameOverRoutine = null;
        LoadScene(gameOverSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive)
        {
            return;
        }

        UpdateSceneMusic(scene.name);
        if (scene.name != menuSceneName && scene.name != gameOverSceneName)
        {
            lastGameplaySceneName = scene.name;
        }
        EnsureMenuManagerInScene();
    }

    public bool IsMenuOrGameOverScene(string sceneName)
    {
        return sceneName == menuSceneName || sceneName == gameOverSceneName;
    }

    private void EnsureMenuManagerInScene()
    {
        GameObject pauseCanvasObj = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "PauseMenuCanvas")
            {
                pauseCanvasObj = obj;
                break;
            }
        }

        if (pauseCanvasObj != null)
        {
            MenuManager menuManager = pauseCanvasObj.GetComponent<MenuManager>();
            if (menuManager == null)
            {
                menuManager = pauseCanvasObj.AddComponent<MenuManager>();
                menuManager.MarkAsDynamic();
            }
        }
    }

    private void OnGUI()
    {
        if (string.IsNullOrWhiteSpace(activeCheckpointMessage) || Time.unscaledTime > activeCheckpointMessageEndTime || IsGamePaused())
        {
            return;
        }

        EnsureCheckpointMessageStyle();

        float width = Mathf.Min(Screen.width - 40f, 320f);
        float height = 36f;
        float x = (Screen.width - width) * 0.5f;
        float y = checkpointMessageTopOffset;
        Rect rect = new Rect(x, y, width, height);

        Color previousBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(rect, GUIContent.none);
        GUI.backgroundColor = previousBackgroundColor;

        GUI.Label(rect, activeCheckpointMessage, checkpointMessageStyle);
    }

    private void ShowCheckpointSavedMessage()
    {
        activeCheckpointMessage = CheckpointSavedMessage;
        activeCheckpointMessageEndTime = Time.unscaledTime + checkpointMessageDuration;
    }

    private void ClearCheckpointMessage()
    {
        activeCheckpointMessage = string.Empty;
        activeCheckpointMessageEndTime = 0f;
    }

    private void EnsureMusicAudioSource()
    {
        if (musicAudioSource == null)
        {
            musicAudioSource = GetComponent<AudioSource>();
        }

        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
        }

        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.volume = 0.85f;
    }

    private void AutoAssignMusicClips()
    {
#if UNITY_EDITOR
        if (level1MusicClip == null)
        {
            level1MusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/AmbientMusica.mp3");
        }

        if (level1BossMusicClip == null)
        {
            level1BossMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sound/AmbientMusicaBOSS.mp3");
        }
#endif
    }

    private void UpdateSceneMusic(string sceneName)
    {
        if (musicAudioSource == null)
        {
            EnsureMusicAudioSource();
        }

        AudioClip targetClip = null;

        if (sceneName == gameplaySceneName || sceneName == level1SceneName)
        {
            targetClip = level1MusicClip;
        }
        else if (sceneName == "Level1Boss")
        {
            targetClip = level1BossMusicClip;
        }

        if (targetClip == null)
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = null;
            return;
        }

        if (musicAudioSource.clip == targetClip && musicAudioSource.isPlaying)
        {
            return;
        }

        musicAudioSource.clip = targetClip;
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }

    private void EnsureCheckpointMessageStyle()
    {
        if (checkpointMessageStyle != null)
        {
            return;
        }

        checkpointMessageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            padding = new RectOffset(12, 12, 6, 6)
        };
        checkpointMessageStyle.normal.textColor = Color.white;
    }

    public void RegisterCheckpoint(Vector3 position, string sceneName)
    {
        checkpointPosition = position;
        checkpointSceneName = sceneName;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint guardado en posición {position} en escena {sceneName}");
        ShowCheckpointSavedMessage();
    }

    public bool IsWorldObjectActivated(string worldObjectId)
    {
        return !string.IsNullOrWhiteSpace(worldObjectId) && activatedWorldObjectIds.Contains(worldObjectId);
    }

    public void RegisterWorldObjectActivated(string worldObjectId)
    {
        if (string.IsNullOrWhiteSpace(worldObjectId))
        {
            return;
        }

        activatedWorldObjectIds.Add(worldObjectId);
    }

    public void ApplyCheckpoint(Transform target, Rigidbody2D rigidbody2D)
    {
        if (!hasCheckpoint || target == null)
        {
            Debug.Log($"ApplyCheckpoint: No se aplico. HasCheckpoint: {hasCheckpoint}, Target null: {target == null}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(checkpointSceneName) && SceneManager.GetActiveScene().name != checkpointSceneName)
        {
            Debug.Log($"ApplyCheckpoint: Escenas no coinciden. Guardada: {checkpointSceneName}, Actual: {SceneManager.GetActiveScene().name}");
            return;
        }

        Debug.Log($"Aplicando checkpoint en posición {checkpointPosition}");
        Vector3 positionWithZero = new Vector3(checkpointPosition.x, checkpointPosition.y, 0f);

        if (rigidbody2D != null)
        {
            rigidbody2D.position = positionWithZero;
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

        target.position = positionWithZero;
        Physics2D.SyncTransforms();

        if (rigidbody2D != null)
        {
            rigidbody2D.WakeUp();
        }
    }

    private void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointPosition = default;
        checkpointSceneName = string.Empty;
    }

    private void ClearWorldObjectStates()
    {
        activatedWorldObjectIds.Clear();
    }
}
