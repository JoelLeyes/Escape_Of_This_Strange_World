using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra una imagen de controles al presionar el botón "Controles" en el menú.
/// La imagen ocupa toda la pantalla y se cierra con ESC, volviendo al menú principal.
/// Asignar desde el inspector: controlesSprite (imagen a mostrar).
/// </summary>
public class ControlsImageViewer : MonoBehaviour
{
    [Header("Imagen de Controles")]
    [Tooltip("Arrastrá aquí la imagen/sprite que muestra los controles del juego")]
    [SerializeField] private Sprite controlesSprite;

    private GameObject overlayGO;
    private bool isShowing = false;

    private void Update()
    {
        if (isShowing && Input.GetKeyDown(KeyCode.Escape))
        {
            HideControles();
        }
    }

    /// <summary>
    /// Llamar desde el botón "Controles" del menú (asignar en el Inspector del botón).
    /// </summary>
    public void ShowControles()
    {
        if (controlesSprite == null)
        {
            Debug.LogWarning("ControlsImageViewer: no hay sprite asignado en el inspector.");
            return;
        }

        if (overlayGO != null)
            Destroy(overlayGO);

        // Canvas overlay
        overlayGO = new GameObject("ControlsImageOverlay");

        Canvas canvas = overlayGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = overlayGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        overlayGO.AddComponent<GraphicRaycaster>();

        // Fondo negro
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(overlayGO.transform, false);

        Image bg = bgGO.AddComponent<Image>();
        bg.color = Color.black;

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Imagen de controles
        GameObject imgGO = new GameObject("ControlesImage");
        imgGO.transform.SetParent(overlayGO.transform, false);

        Image img = imgGO.AddComponent<Image>();
        img.sprite = controlesSprite;
        img.preserveAspect = true;

        RectTransform imgRect = img.GetComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = imgRect.offsetMax = Vector2.zero;

        // Texto de ayuda
        GameObject hintGO = new GameObject("HintText");
        hintGO.transform.SetParent(overlayGO.transform, false);

        Text hint = hintGO.AddComponent<Text>();
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 18;
        hint.color = new Color(1f, 1f, 1f, 0.7f);
        hint.alignment = TextAnchor.LowerCenter;

        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 12f);
        hintRect.sizeDelta = new Vector2(0f, 30f);

        isShowing = true;
    }

    private void HideControles()
    {
        if (overlayGO != null)
            Destroy(overlayGO);

        overlayGO = null;
        isShowing = false;
    }
}