using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Muestra los controles en pantalla al inicio del Level1 durante 20 segundos.
/// Usa IMGUI (OnGUI) para garantizar que el texto se vea sin depender de fuentes TMP.
/// Se instancia automáticamente al cargar Level1.
/// </summary>
public class ControlsOverlay : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level1" && mode != LoadSceneMode.Additive)
        {
            // Evitar duplicados
            if (FindObjectOfType<ControlsOverlay>() != null) return;
            GameObject go = new GameObject("ControlsOverlay");
            go.AddComponent<ControlsOverlay>();
        }
    }

    private float timeLeft = 20f;
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private bool stylesReady = false;

    private void InitStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.65f));
        boxStyle.padding = new RectOffset(14, 14, 10, 10);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 20;
        labelStyle.normal.textColor = Color.white;
        labelStyle.richText = true;
        stylesReady = true;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void Update()
    {
        timeLeft -= Time.unscaledDeltaTime;
        if (timeLeft <= 0f)
            Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (!stylesReady) InitStyles();

        float alpha = Mathf.Clamp01(timeLeft); // fade out en el último segundo
        GUI.color = new Color(1f, 1f, 1f, alpha);

        string texto =
            "<b>Controles</b>\n" +
            "Saltar:                  W  /  Espacio  /  ← \n" +
            "Avanzar:            A / D  ó  ← / →\n" +
            "Ataque con Espada:  J\n" +
            "Ataque Mágico:      K\n" +
            "Arco (flechas):      P\n" +
            "Dash:                SHIFT";
        float w = 420f;
        float h = 200f;
        float x = 20f;
        float y = Screen.height - h - 20f;

        GUI.Box(new Rect(x, y, w, h), "", boxStyle);
        GUI.Label(new Rect(x + 14f, y + 10f, w - 28f, h - 20f), texto, labelStyle);

        GUI.color = Color.white;
    }
}
