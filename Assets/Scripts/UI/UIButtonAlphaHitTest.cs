using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIButtonAlphaHitTest : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float alphaThreshold = 0.3f;

    private Image targetImage;

    private void Awake()
    {
        ApplyThreshold();
    }

    private void OnValidate()
    {
        ApplyThreshold();
    }

    private void ApplyThreshold()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetImage == null)
        {
            return;
        }

        targetImage.alphaHitTestMinimumThreshold = alphaThreshold;
    }
}