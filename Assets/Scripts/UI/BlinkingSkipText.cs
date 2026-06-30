using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class BlinkingSkipText : MonoBehaviour
{
    private TMP_Text textComponent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (SceneManager.GetActiveScene().name == "IntroEscena")
        {
            CreateRunner();
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "IntroEscena")
        {
            CreateRunner();
        }
    }

    private static void CreateRunner()
    {
        GameObject runner = new GameObject("SkipTextRunner");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<CoroutineRunner>();
    }

    private class CoroutineRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Esperar a que toda la escena termine de inicializarse
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

            CreateSkipText();

            Destroy(gameObject);
        }
    }

    private static void CreateSkipText()
    {
        if (GameObject.Find("SkipIntroText") != null)
            return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        Canvas canvas = null;

        foreach (Canvas c in canvases)
        {
            if (c.isActiveAndEnabled)
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject textObj = new GameObject("SkipIntroText");
        textObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 35f);
        rect.sizeDelta = new Vector2(800f, 60f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Presione ESC para saltar la intro";
        tmp.fontSize = 26f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0f);

        textObj.AddComponent<BlinkingSkipText>();
    }

    private void Start()
    {
        textComponent = GetComponent<TMP_Text>();

        if (textComponent != null)
        {
            StartCoroutine(BlinkSequence());
        }
    }

    private IEnumerator BlinkSequence()
    {
        yield return new WaitForSeconds(15f);

        float fadeInDuration = 0.5f;
        float stayDuration = 1f;
        float fadeOutDuration = 0.5f;

        for (int i = 0; i < 2; i++)
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(0f, 1f, elapsed / fadeInDuration));
                yield return null;
            }

            SetAlpha(1f);

            yield return new WaitForSeconds(stayDuration);

            elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration));
                yield return null;
            }

            SetAlpha(0f);

            yield return new WaitForSeconds(0.2f);
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (textComponent == null)
            return;

        Color c = textComponent.color;
        c.a = alpha;
        textComponent.color = c;
    }
}