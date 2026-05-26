using UnityEngine;
using UnityEngine.UI;

public class HeartDisplay : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image[] corazones;
    [SerializeField] private Sprite corazonLleno;
    [SerializeField] private Sprite corazonVacio;

    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }

    private void Update()
    {
        ActualizarCorazones();
    }

    private void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0)
        {
            return;
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
            }
            else
            {
                corazones[i].sprite = corazonVacio;
            }
        }
    }
}
