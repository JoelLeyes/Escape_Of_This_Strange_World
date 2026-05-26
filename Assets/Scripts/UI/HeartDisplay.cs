using UnityEngine;
using UnityEngine.UI;

public class HeartDisplay : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image[] corazones;
    [SerializeField] private Sprite corazonLleno;
    [SerializeField] private Sprite corazonVacio;
    [Header("Auto UI")]
    [SerializeField] private bool autoGenerar = true;
    [SerializeField] private int corazonesMaximosFallback = 10;
    [SerializeField] private Vector2 corazonSize = new Vector2(32f, 32f);
    [SerializeField] private float espacio = 6f;
    [SerializeField] private Color colorLleno = Color.white;
    [SerializeField] private Color colorVacio = new Color(1f, 1f, 1f, 0.35f);

    private bool inicializado;

    private void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void Start()
    {
        InicializarSiEsNecesario();
    }

    private void Update()
    {
        ActualizarCorazones();
    }

    public void SetPlayer(Player nuevoPlayer)
    {
        if (nuevoPlayer == null)
        {
            return;
        }

        player = nuevoPlayer;
        InicializarSiEsNecesario();
    }

    public void SetSprites(Sprite lleno, Sprite vacio)
    {
        if (lleno != null)
        {
            corazonLleno = lleno;
        }

        if (vacio != null)
        {
            corazonVacio = vacio;
        }
    }

    public void SetLayout(Vector2 size, float spacing)
    {
        corazonSize = size;
        espacio = spacing;
    }

    private void InicializarSiEsNecesario()
    {
        if (inicializado)
        {
            return;
        }

        if (autoGenerar)
        {
            GenerarCorazones();
        }
        else if (corazones == null || corazones.Length == 0)
        {
            corazones = GetComponentsInChildren<Image>(true);
        }

        inicializado = true;
    }

    private void GenerarCorazones()
    {
        int maximo = corazonesMaximosFallback;
        if (player != null)
        {
            maximo = Mathf.Max(player.GetCorazonesMaximos(), 1);
        }

        if (maximo <= 0)
        {
            maximo = 1;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        Sprite spriteLleno = corazonLleno != null ? corazonLleno : GetSpritePorDefecto();
        Sprite spriteVacio = corazonVacio != null ? corazonVacio : spriteLleno;
        corazonLleno = spriteLleno;
        corazonVacio = spriteVacio;

        corazones = new Image[maximo];
        for (int i = 0; i < maximo; i++)
        {
            GameObject corazon = new GameObject($"Corazon_{i + 1}", typeof(RectTransform), typeof(Image));
            corazon.transform.SetParent(transform, false);

            RectTransform rect = corazon.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = corazonSize;
            rect.anchoredPosition = new Vector2(i * (corazonSize.x + espacio), 0f);

            Image img = corazon.GetComponent<Image>();
            img.sprite = spriteLleno;
            img.color = colorLleno;
            img.preserveAspect = true;
            corazones[i] = img;
        }
    }

    private Sprite GetSpritePorDefecto()
    {
        Sprite sprite = Resources.Load<Sprite>("heart");
        if (sprite != null)
        {
            return sprite;
        }

        sprite = Resources.Load<Sprite>("corazon");
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (corazonLleno == null)
        {
            corazonLleno = GetSpritePorDefecto();
        }

        if (corazonVacio == null)
        {
            corazonVacio = corazonLleno;
        }

        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] == null)
            {
                continue;
            }

            if (i < player.GetCorazonesActuales())
            {
                corazones[i].sprite = corazonLleno;
                corazones[i].color = colorLleno;
            }
            else
            {
                corazones[i].sprite = corazonVacio;
                corazones[i].color = colorVacio;
            }
        }
    }
}
