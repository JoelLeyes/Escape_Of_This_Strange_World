using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;


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
    [SerializeField] private AudioClip finalSceneMusicClip;

    [Header("Portada")]
    [SerializeField] private Sprite portadaSprite;

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

    // WebGL: el navegador bloquea el audio hasta que el usuario interactúa.
    // Guardamos el clip pendiente para reintentarlo tras el primer input.
    private bool audioContextUnlocked = false;
    private AudioClip pendingMusicClip = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.CopyFieldsFrom(this);
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

    public void CopyFieldsFrom(GameManager other)
    {
        if (other == null) return;

        if (other.level1MusicClip != null) this.level1MusicClip = other.level1MusicClip;
        if (other.level1BossMusicClip != null) this.level1BossMusicClip = other.level1BossMusicClip;
        if (other.finalSceneMusicClip != null) this.finalSceneMusicClip = other.finalSceneMusicClip;
        if (other.portadaSprite != null) this.portadaSprite = other.portadaSprite;

        if (!string.IsNullOrEmpty(other.menuSceneName)) this.menuSceneName = other.menuSceneName;
        if (!string.IsNullOrEmpty(other.gameplaySceneName)) this.gameplaySceneName = other.gameplaySceneName;
        if (!string.IsNullOrEmpty(other.gameOverSceneName)) this.gameOverSceneName = other.gameOverSceneName;
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
        // WebGL: intentar desbloquear el contexto de audio tras la primera interacción del usuario.
        if (!audioContextUnlocked)
        {
            bool userInteracted = Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (userInteracted)
            {
                audioContextUnlocked = true;
                // Si había un clip pendiente (bloqueado por el navegador), lo reproducimos ahora.
                if (pendingMusicClip != null && musicAudioSource != null && !musicAudioSource.isPlaying)
                {
                    musicAudioSource.clip = pendingMusicClip;
                    musicAudioSource.loop = true;
                    musicAudioSource.Play();
                    pendingMusicClip = null;
                }
            }
        }

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
        else if (hasCheckpoint && !string.IsNullOrWhiteSpace(checkpointSceneName))
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

        if (SceneManager.GetActiveScene().name == menuSceneName && portadaSprite != null)
        {
            StartCoroutine(PortadaTransitionSequence());
        }
        else
        {
            LoadScene(gameplaySceneName);
        }
    }

    private System.Collections.IEnumerator PortadaTransitionSequence()
    {
        // Crear Canvas temporal para la portada
        GameObject canvasObj = new GameObject("PortadaTransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Crear fondo negro
        GameObject bgObj = new GameObject("BlackBackground");
        bgObj.transform.SetParent(canvas.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Crear imagen de portada
        GameObject portadaObj = new GameObject("PortadaImage");
        portadaObj.transform.SetParent(canvas.transform, false);
        Image portadaImage = portadaObj.AddComponent<Image>();
        portadaImage.sprite = portadaSprite;
        portadaImage.preserveAspect = true;
        
        RectTransform portadaRect = portadaObj.GetComponent<RectTransform>();
        portadaRect.anchorMin = Vector2.zero;
        portadaRect.anchorMax = Vector2.one;
        portadaRect.sizeDelta = Vector2.zero;

        // Opacidad a 0 inicialmente
        portadaImage.color = new Color(1f, 1f, 1f, 0f);

        // Desvanecimiento 0 a 100% (1 segundo)
        float duration = 1.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            portadaImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        portadaImage.color = new Color(1f, 1f, 1f, 1f);

        // Mostrar 2 segundos al 100%
        yield return new WaitForSeconds(2f);

        // Desvanecimiento 100% a 0% (1 segundo)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            portadaImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        portadaImage.color = new Color(1f, 1f, 1f, 0f);
        portadaObj.SetActive(false);

        // Esperar 1 segundo con pantalla en negro
        yield return new WaitForSeconds(1f);

        // Cargar escena
        LoadScene(gameplaySceneName);

        // Esperar un instante y destruir Canvas
        yield return new WaitForSeconds(0.5f);
        Destroy(canvasObj);
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

    public void WinGame()
    {
        ClearCheckpoint();
        ClearWorldObjectStates();
        ClearCheckpointMessage();
        Player.ResetPersistentInstance();
        lastGameplaySceneName = null;
        LoadScene("Final");
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

        StartGame();
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
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            BackToMenu();
        }
        else
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
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

        if (scene.name == "Final")
        {
            SetupFinalSceneButtons();
        }
    }

    private void SetupFinalSceneButtons()
    {
        Debug.Log("GameManager: Configurando botones en la escena Final.");
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in buttons)
        {
            if (btn.gameObject.scene != SceneManager.GetActiveScene())
            {
                continue;
            }

            if (btn.gameObject.name == "PlayButton")
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log("PlayButton presionado en la escena Final. Iniciando juego/cinematica.");
                    StopFinalSceneMusic();
                    StartGame();
                });
                Debug.Log("PlayButton configurado exitosamente.");
            }
            else if (btn.gameObject.name == "QuitButton")
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    Debug.Log("QuitButton presionado en la escena Final. Cerrando juego.");
                    StopFinalSceneMusic();
                    QuitGame();
                });
                Debug.Log("QuitButton configurado exitosamente.");
            }
        }
    }

    private void StopFinalSceneMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
        }
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
        musicAudioSource.volume = 0.75f;
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

        if (portadaSprite == null)
        {
            portadaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/imagenPortada.png");
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
        else if (sceneName == "Final")
        {
            targetClip = finalSceneMusicClip;
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

        musicAudioSource.Stop();
        musicAudioSource.clip = targetClip;
        musicAudioSource.loop = true;

        // En WebGL el navegador bloquea el audio hasta la primera interacción del usuario.
        // Si el contexto ya fue desbloqueado reproducimos de inmediato; si no, guardamos el clip para reproducirlo en cuanto el usuario toque algo.
#if UNITY_WEBGL && !UNITY_EDITOR
        if (audioContextUnlocked)
        {
            musicAudioSource.Play();
            pendingMusicClip = null;
        }
        else
        {
            pendingMusicClip = targetClip;
        }
#else
        musicAudioSource.Play();
#endif
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
