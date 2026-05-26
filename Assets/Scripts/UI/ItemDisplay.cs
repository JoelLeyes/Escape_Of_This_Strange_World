using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ItemDisplay : MonoBehaviour
    {
        private Image bowImage;
        private Image arrowImage;
        private Text arrowCountText;

        private void Awake()
        {
            // Create layout: Bow on left, Arrow + count on right
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 64f);

            GameObject bowObj = new GameObject("Bow", typeof(RectTransform), typeof(Image));
            bowObj.transform.SetParent(transform, false);
            RectTransform br = bowObj.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = new Vector2(0f, 1f);
            br.pivot = new Vector2(0f, 1f);
            br.anchoredPosition = Vector2.zero;
            br.sizeDelta = new Vector2(56f, 56f);
            bowImage = bowObj.GetComponent<Image>();
            bowImage.enabled = false;

            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(transform, false);
            RectTransform ar = arrowObj.GetComponent<RectTransform>();
            ar.anchorMin = new Vector2(0f, 1f);
            ar.anchorMax = new Vector2(0f, 1f);
            ar.pivot = new Vector2(0f, 1f);
            ar.anchoredPosition = new Vector2(74f, 0f);
            ar.sizeDelta = new Vector2(48f, 48f);
            arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.enabled = false;

            GameObject countObj = new GameObject("ArrowCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            countObj.transform.SetParent(transform, false);
            RectTransform cr = countObj.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 1f);
            cr.anchorMax = new Vector2(0f, 1f);
            cr.pivot = new Vector2(0f, 1f);
            cr.anchoredPosition = new Vector2(130f, -8f);
            cr.sizeDelta = new Vector2(96f, 32f);
            arrowCountText = countObj.GetComponent<Text>();
            arrowCountText.alignment = TextAnchor.MiddleLeft;
            // Ensure a usable font is assigned when creating text at runtime.
            Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                // Try OS Arial
                f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            }

            if (f == null)
            {
                // Prefer project fonts: LiberationSans, then Ancient Medium
                Font[] found = Resources.FindObjectsOfTypeAll<Font>();
                if (found != null && found.Length > 0)
                {
                    Font preferred = null;
                    for (int i = 0; i < found.Length; i++)
                    {
                        string n = found[i].name.ToLower();
                        if (n.Contains("liberation") || n.Contains("liberationsans"))
                        {
                            preferred = found[i];
                            break;
                        }
                    }

                    if (preferred == null)
                    {
                        for (int i = 0; i < found.Length; i++)
                        {
                            string n = found[i].name.ToLower();
                            if (n.Contains("ancient") || n.Contains("ancient medium"))
                            {
                                preferred = found[i];
                                break;
                            }
                        }
                    }

                    f = preferred != null ? preferred : found[0];
                }
            }

            // Try immediate font assignment; if assets aren't ready, a coroutine will retry next frame
            TryAssignFont(f);
            StartCoroutine(TryAssignFontNextFrame());

            // Add outline to improve contrast against backgrounds
            Outline outline = countObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);
            arrowCountText.horizontalOverflow = HorizontalWrapMode.Overflow;
            arrowCountText.verticalOverflow = VerticalWrapMode.Overflow;
            arrowCountText.raycastTarget = false;
            // Start hidden until we have arrows
            arrowCountText.gameObject.SetActive(false);
            arrowCountText.color = Color.white;
            arrowCountText.text = "";
        }

        private void TryAssignFont(Font f)
        {
            if (arrowCountText == null)
                return;

            if (f != null)
            {
                arrowCountText.font = f;
                arrowCountText.fontStyle = FontStyle.Bold;
                arrowCountText.fontSize = 32;
                try { arrowCountText.material = f.material; } catch { }
                return;
            }

            // f was null; attempt to find preferred fonts immediately in loaded assets
            Font[] found = Resources.FindObjectsOfTypeAll<Font>();
            if (found != null && found.Length > 0)
            {
                Font preferred = null;
                for (int i = 0; i < found.Length; i++)
                {
                    string n = found[i].name.ToLower();
                    if (n.Contains("liberation") || n.Contains("liberationsans"))
                    {
                        preferred = found[i];
                        break;
                    }
                }

                if (preferred == null)
                {
                    for (int i = 0; i < found.Length; i++)
                    {
                        string n = found[i].name.ToLower();
                        if (n.Contains("ancient") || n.Contains("ancient medium"))
                        {
                            preferred = found[i];
                            break;
                        }
                    }
                }

                Font pick = preferred != null ? preferred : found[0];
                arrowCountText.font = pick;
                arrowCountText.fontStyle = FontStyle.Bold;
                arrowCountText.fontSize = 32;
                try { arrowCountText.material = pick.material; } catch { }
            }
        }

        private IEnumerator TryAssignFontNextFrame()
        {
            yield return null; // wait a frame
            if (arrowCountText == null)
                yield break;

            if (arrowCountText.font == null)
            {
                // Try builtin Arial from Resources
                Font f = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (f == null)
                {
                    f = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }

                TryAssignFont(f);
            }

            if (arrowCountText.font == null)
            {
                // Copy a font from any existing Text in the scene
                Text[] texts = FindObjectsOfType<Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i].font != null)
                    {
                        arrowCountText.font = texts[i].font;
                        try { arrowCountText.material = texts[i].material; } catch { }
                        break;
                    }
                }
            }
        }

        public void SetBowSprite(Sprite s)
        {
            if (s == null)
            {
                bowImage.enabled = false;
                bowImage.sprite = null;
                return;
            }

            bowImage.sprite = s;
            bowImage.preserveAspect = true;
            bowImage.enabled = true;
        }

        public void SetArrowSprite(Sprite s)
        {
            if (s == null)
            {
                arrowImage.enabled = false;
                arrowImage.sprite = null;
                arrowCountText.text = "";
                return;
            }

            arrowImage.sprite = s;
            arrowImage.preserveAspect = true;
            arrowImage.enabled = true;
        }

        public void SetArrowCount(int count)
        {
            if (count <= 0)
            {
                if (arrowImage != null) arrowImage.enabled = (arrowImage.sprite != null);
                if (arrowCountText != null) arrowCountText.gameObject.SetActive(false);
                return;
            }

            if (arrowCountText != null)
            {
                arrowCountText.gameObject.SetActive(true);
                arrowCountText.text = "x" + count.ToString();
                arrowCountText.color = new Color(1f, 1f, 1f, 1f);
            }
            if (arrowImage != null) arrowImage.enabled = (arrowImage.sprite != null);
        }
    }
}
