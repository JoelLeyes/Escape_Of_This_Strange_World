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
        SceneManager.sceneLoaded -= OnSceneLoaded; // Evitar doble registro
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (SceneManager.GetActiveScene().name == "IntroEscena")
        {
            CreateSkipText();
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "IntroEscena")
        {
            CreateSkipText();
        }
    }

    private static void CreateSkipText()
    {
        if (GameObject.Find("SkipIntroText") != null)
        {
            return;
        }

        // Buscar el Canvas existente en la escena
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Crear el GameObject del Texto
        GameObject textObj = new GameObject("SkipIntroText");
        textObj.transform.SetParent(canvas.transform, false);

        // Configurar el RectTransform
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 35f); // Posicionado más abajo
        rect.sizeDelta = new Vector2(800f, 60f);

        // Añadir el componente TextMeshProUGUI
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Presione ESC para saltar la intro";
        tmp.fontSize = 18f; // Más chico
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0f); // Transparente inicialmente para el fade-in

        // Añadir este componente para manejar el parpadeo
        textObj.AddComponent<BlinkingSkipText>();
        Debug.Log("SkipIntroText creado dinámicamente en IntroEscena.");
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
        float fadeInDuration = 0.5f;   // Sube gradualmente en 0.5 segundos (más rápido)
        float stayDuration = 1.0f;     // Se mantiene al 100% durante 1.0 segundo (más rápido)
        float fadeOutDuration = 0.5f;  // Baja al 0% en 0.5 segundos (más rápido)

        for (int i = 0; i < 2; i++) // Dos veces seguidas
        {
            // 1. Aparecer (0 a 1)
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(1f);

            // 2. Mantener al 100%
            yield return new WaitForSeconds(stayDuration);

            // 3. Desvanecer (1 a 0)
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                SetAlpha(alpha);
                yield return null;
            }
            SetAlpha(0f);

            // Breve espera antes del segundo ciclo (0.2s)
            yield return new WaitForSeconds(0.2f);
        }

        // Destruir el GameObject después de terminar las dos repeticiones
        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (textComponent != null)
        {
            Color color = textComponent.color;
            color.a = alpha;
            textComponent.color = color;
        }
    }
}
