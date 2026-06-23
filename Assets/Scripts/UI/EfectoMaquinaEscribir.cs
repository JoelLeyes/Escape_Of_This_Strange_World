using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EfectoMaquinaEscribir : MonoBehaviour
{
    [Header("Configuración de Texto")]
    [Tooltip("Velocidad de escritura en segundos por caracter")]
    public float velocidadEscritura = 0.05f;

    [Tooltip("¿Comenzar a escribir automáticamente al activarse?")]
    public bool iniciarAlActivar = true;

    [TextArea(3, 10)]
    [Tooltip("Texto que se escribirá. Si se deja vacío, se usará el texto actual del componente.")]
    public string textoCompleto = "";

    [Header("Efecto de Audio (Bucle Continuo)")]
    [Tooltip("AudioSource para reproducir el sonido de escritura")]
    public AudioSource audioSource;
    [Tooltip("Sonido de escritura continua (se reproducirá en bucle mientras se escribe)")]
    public AudioClip sonidoTeclado;
    [Range(0f, 1f)]
    public float volumenSonido = 0.5f;

    private Text legacyText;
    private TMP_Text tmpText;
    private Coroutine corrutinaEscritura;
    private bool originalLoop;

    private void Awake()
    {
        legacyText = GetComponent<Text>();
        tmpText = GetComponent<TMP_Text>();

        if (legacyText == null && tmpText == null)
        {
            Debug.LogError("EfectoMaquinaEscribir requiere un componente Text o TextMeshPro (TMP_Text) en el mismo GameObject.", gameObject);
            return;
        }

        // Configuración automática de AudioSource si no se asignó en el Inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && sonidoTeclado != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (audioSource != null)
        {
            originalLoop = audioSource.loop;
        }

        // Si no se especificó textoCompleto, tomamos el inicial del componente
        if (string.IsNullOrEmpty(textoCompleto))
        {
            textoCompleto = ObtenerTextoActual();
        }

        // Dejar el texto vacío al iniciar
        EstablecerTexto("");
    }

    private void OnEnable()
    {
        if (iniciarAlActivar)
        {
            IniciarEfecto();
        }
    }

    private void OnDisable()
    {
        DetenerEfecto();
    }

    public void IniciarEfecto()
    {
        if (legacyText == null && tmpText == null)
        {
            legacyText = GetComponent<Text>();
            tmpText = GetComponent<TMP_Text>();
        }

        if (legacyText == null && tmpText == null) return;

        DetenerEfecto();
        corrutinaEscritura = StartCoroutine(EscribirTexto());
    }

    public void DetenerEfecto()
    {
        if (corrutinaEscritura != null)
        {
            StopCoroutine(corrutinaEscritura);
            corrutinaEscritura = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = originalLoop;
        }
    }

    private IEnumerator EscribirTexto()
    {
        EstablecerTexto("");

        // Iniciar sonido en bucle mientras escribe
        if (audioSource != null && sonidoTeclado != null)
        {
            audioSource.clip = sonidoTeclado;
            audioSource.loop = true;
            audioSource.volume = volumenSonido;
            audioSource.pitch = 1f;
            audioSource.Play();
        }
        
        foreach (char caracter in textoCompleto)
        {
            EstablecerTexto(ObtenerTextoActual() + caracter);
            yield return new WaitForSeconds(velocidadEscritura);
        }

        // Detener el sonido al terminar
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = originalLoop;
        }

        corrutinaEscritura = null;
    }

    private string ObtenerTextoActual()
    {
        if (tmpText != null) return tmpText.text;
        if (legacyText != null) return legacyText.text;
        return "";
    }

    private void EstablecerTexto(string nuevoTexto)
    {
        if (tmpText != null) tmpText.text = nuevoTexto;
        else if (legacyText != null) legacyText.text = nuevoTexto;
    }

    public bool EstaEscribiendo()
    {
        return corrutinaEscritura != null;
    }
}
