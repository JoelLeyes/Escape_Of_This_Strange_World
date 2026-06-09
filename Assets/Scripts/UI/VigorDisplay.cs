using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class VigorDisplay : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [Header("Auto UI")]
        [SerializeField] private bool autoBuild = true;
        [SerializeField] private Vector2 size = new Vector2(190f, 18f);
        [SerializeField] private Color borderColor = Color.black;
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.12f, 0.06f, 0.9f);
        [SerializeField] private Color fillColor = new Color(0.15f, 0.8f, 0.15f, 1f);
        [SerializeField] private float borderPadding = 3f;

        private bool initialized;

        private void Awake()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<Player>();
            }
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            UpdateBar();
        }

        public void SetPlayer(Player nuevoPlayer)
        {
            if (nuevoPlayer == null)
            {
                return;
            }

            player = nuevoPlayer;
            InitializeIfNeeded();
        }

        public void SetLayout(Vector2 newSize)
        {
            size = newSize;
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            if (autoBuild)
            {
                BuildBar();
            }

            initialized = true;
        }

        private void BuildBar()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(size.x + borderPadding * 2f, size.y + borderPadding * 2f);
            }

            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(transform, false);
            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 1f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 1f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(size.x + borderPadding * 2f, size.y + borderPadding * 2f);
            borderImage = border.GetComponent<Image>();
            borderImage.color = borderColor;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(size.x, size.y);
            backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = backgroundColor;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 1f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 1f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(size.x, size.y);
            fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;
        }

        private void UpdateBar()
        {
            if (fillImage == null || player == null)
            {
                return;
            }

            float max = Mathf.Max(player.GetVigorMax(), 1f);
            float current = Mathf.Clamp(player.GetVigorCurrent(), 0f, max);
            float width = size.x * (current / max);

            RectTransform fillRect = fillImage.rectTransform;
            fillRect.sizeDelta = new Vector2(width, size.y);
        }
    }
}