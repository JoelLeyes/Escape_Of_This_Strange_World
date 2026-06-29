using UnityEngine;
//using UnityEngine.InputSystem;
using System.Collections;

public class IntroCinematicController : MonoBehaviour
{
    public float speed = 1f;
    public float stopPositionX = 1f;

    public GameObject fondoCastillo;
    public GameObject fondoCiudad;

    public GameObject portalCastillo;

    public GameObject portalCiudad;

    [Header("Sonidos de Portales")]
    [SerializeField] private AudioClip portalWarningSound; // Un segundo antes de aparecer
    [SerializeField] private AudioClip portalCastleSound;  // Cuando aparece en el castillo

    public GameObject pantallaNegra;

    public Transform spawnCastillo;

    [Header("Configuración del Camino al Castillo")]
    // Waypoints: arrastrá GameObjects vacíos en la escena para definir el camino curvo
    public Transform[] waypoints;

    // Tiempo de espera en spawnCastillo antes de caminar al castillo (para mostrar textos)
    public float tiempoEsperaEnCastillo = 3f;

    // Velocidad al caminar hacia el castillo
    public float speedHaciaCastillo = 1.5f;

    // Escala inicial del personaje (se usa al salir del portal y comenzar a caminar)
    public Vector3 escalaInicial = new Vector3(2f, 2f, 1f);

    // Escala final al llegar al último waypoint (más chico = más lejos)
    public Vector3 escalaFinal = new Vector3(0.5f, 0.5f, 1f);

    private bool saliendoDelPortal = false;
    private bool caminandoHaciaCastillo = false;

    // Control de waypoints
    private int waypointActual = 0;
    private float distanciaTotalCaminata;
    private float distanciaRecorrida;

    public Sprite idleSprite;
    private bool stopped = false;
    private bool enteringPortal = false;

    private bool introComenzo = false;

    public GameObject textoInicio;

    public GameObject textoInicio2;
    
    public GameObject textoInicio3;

    [Header("Textos del Personaje (Ciudad)")]
    public GameObject textoCI1;
    public GameObject textoCI2;

    public GameObject[] textosProfesor;
    void Update()
    {
        if (!introComenzo)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaltarIntro();
        }
     
        if (!stopped)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            if (transform.position.x >= stopPositionX)
            {
                stopped = true;

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                
                sr.sprite = idleSprite;

                GetComponent<Animator>().enabled = false;

                StartCoroutine(SecuenciaProfesorYPortal());
            }
        }
        
        if (enteringPortal)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            if (transform.position.x >= portalCiudad.transform.position.x)
            {
                enteringPortal = false;

                StartCoroutine(MostrarCastillo());

                GetComponent<SpriteRenderer>().enabled = false;
            }
        }

        if (saliendoDelPortal)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                spawnCastillo.position,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, spawnCastillo.position) < 0.05f)
            {
                saliendoDelPortal = false;

                GetComponent<Animator>().enabled = false;

                GetComponent<SpriteRenderer>().sprite = idleSprite;

                // Desaparecer el portal del castillo achicándose
                StartCoroutine(DesaparecerPortalCastillo());

                // Diálogo del castillo y luego caminar
                StartCoroutine(SecuenciaDialogoCastillo());
            }
        }

        // Fase de caminar hacia el castillo siguiendo los waypoints y achicándose
        if (caminandoHaciaCastillo && waypoints != null && waypoints.Length > 0)
        {
            float pasoRestante = speedHaciaCastillo * Time.deltaTime;

            // Procesar movimiento del frame actual, pudiendo completar múltiples waypoints si la velocidad es alta
            while (pasoRestante > 0f && waypointActual < waypoints.Length)
            {
                Transform destino = waypoints[waypointActual];
                
                if (destino == null)
                {
                    waypointActual++;
                    continue;
                }

                // Mantenemos el Z del personaje para evitar problemas de profundidad en 2D
                Vector3 targetPos = destino.position;
                targetPos.z = transform.position.z;

                float distToTarget = Vector2.Distance(transform.position, targetPos);

                if (distToTarget <= pasoRestante)
                {
                    // Llegó al waypoint en este frame
                    Vector3 posAnterior = transform.position;
                    transform.position = targetPos;
                    distanciaRecorrida += Vector2.Distance(posAnterior, targetPos);

                    pasoRestante -= distToTarget;
                    waypointActual++;

                    // Actualizar orientación para el siguiente tramo
                    if (waypointActual < waypoints.Length && waypoints[waypointActual] != null)
                    {
                        ActualizarDireccionDeMirada(waypoints[waypointActual].position);
                    }
                }
                else
                {
                    // Se mueve hacia el waypoint
                    Vector3 posAnterior = transform.position;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, pasoRestante);
                    distanciaRecorrida += Vector2.Distance(posAnterior, transform.position);
                    
                    pasoRestante = 0f;
                }
            }

            // Interpolar escala suavemente basada en la distancia recorrida total sobre la planificada
            if (distanciaTotalCaminata > 0.01f)
            {
                float progreso = Mathf.Clamp01(distanciaRecorrida / distanciaTotalCaminata);
                transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, progreso);
            }

            if (waypointActual >= waypoints.Length)
            {
                caminandoHaciaCastillo = false;
                transform.localScale = escalaFinal;
                OnLlegoAlCastillo();
            }
        }
    }

    private void LateUpdate()
    {
        // Si el texto 6 (textop6) está activo y asignado, hacemos que siga al profesor mientras camina
        if (caminandoHaciaCastillo && textosProfesor != null && textosProfesor.Length > 6 && textosProfesor[6] != null && textosProfesor[6].activeInHierarchy)
        {
            RectTransform rectTransform = textosProfesor[6].GetComponent<RectTransform>();
            
            // Offset dinámico escalado con el tamaño del profesor
            float scaleRatio = transform.localScale.y / escalaInicial.y;
            Vector3 offset = new Vector3(0f, 1.8f * scaleRatio, 0f);
            
            if (rectTransform != null && rectTransform.GetComponentInParent<Canvas>() != null && rectTransform.GetComponentInParent<Canvas>().renderMode != RenderMode.WorldSpace)
            {
                if (Camera.main != null)
                {
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + offset);
                    rectTransform.position = screenPos;
                }
            }
            else
            {
                textosProfesor[6].transform.position = transform.position + offset;
            }
        }
    }

    IEnumerator InicioIntro()
    {
        pantallaNegra.SetActive(true);

        // Nos aseguramos de que los textos inicien desactivados para ir mostrándolos secuencialmente
        if (textoInicio != null) textoInicio.SetActive(false);
        if (textoInicio2 != null) textoInicio2.SetActive(false);
        if (textoInicio3 != null) textoInicio3.SetActive(false);
        if (textoCI1 != null) textoCI1.SetActive(false);
        if (textoCI2 != null) textoCI2.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 1. Mostrar y escribir el primer texto
        if (textoInicio != null)
        {
            textoInicio.SetActive(true);
            EfectoMaquinaEscribir effect1 = textoInicio.GetComponent<EfectoMaquinaEscribir>();
            if (effect1 != null)
            {
                effect1.IniciarEfecto();
                while (effect1.EstaEscribiendo())
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 2. Mostrar y escribir el segundo texto
        if (textoInicio2 != null)
        {
            textoInicio2.SetActive(true);
            EfectoMaquinaEscribir effect2 = textoInicio2.GetComponent<EfectoMaquinaEscribir>();
            if (effect2 != null)
            {
                effect2.IniciarEfecto();
                while (effect2.EstaEscribiendo())
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 3. Mostrar y escribir el tercer texto
        if (textoInicio3 != null)
        {
            textoInicio3.SetActive(true);
            EfectoMaquinaEscribir effect3 = textoInicio3.GetComponent<EfectoMaquinaEscribir>();
            if (effect3 != null)
            {
                effect3.IniciarEfecto();
                while (effect3.EstaEscribiendo())
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        // Tiempo de espera al final para que el jugador termine de leer todo
        yield return new WaitForSeconds(2.0f);

        if (pantallaNegra != null) pantallaNegra.SetActive(false);

        if (textoInicio != null) textoInicio.SetActive(false);
        if (textoInicio2 != null) textoInicio2.SetActive(false);
        if (textoInicio3 != null) textoInicio3.SetActive(false);

        // 1.5 segundos después de que se muestre la ciudad, aparece textoCI1
        yield return new WaitForSeconds(1.5f);

        if (textoCI1 != null)
        {
            textoCI1.SetActive(true);
            EfectoMaquinaEscribir effectCI1 = textoCI1.GetComponent<EfectoMaquinaEscribir>();
            if (effectCI1 != null)
            {
                effectCI1.IniciarEfecto();
                while (effectCI1.EstaEscribiendo())
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        // Cuando termina de completarse, desaparece
        if (textoCI1 != null) textoCI1.SetActive(false);

        // 0.5 segundos después aparece textoCI2
        yield return new WaitForSeconds(0.5f);

        if (textoCI2 != null)
        {
            textoCI2.SetActive(true);
            EfectoMaquinaEscribir effectCI2 = textoCI2.GetComponent<EfectoMaquinaEscribir>();
            if (effectCI2 != null)
            {
                effectCI2.IniciarEfecto();
                while (effectCI2.EstaEscribiendo())
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        // Esperar un momento corto antes de que empiece a caminar para poder leer textoCI2
        yield return new WaitForSeconds(1.5f);
        if (textoCI2 != null) textoCI2.SetActive(false);

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
        }

        introComenzo = true;
    }

    IEnumerator SecuenciaProfesorYPortal()
    {
        // --- textop[0]: aparece cuando el profesor se detiene ---
        if (textosProfesor != null && textosProfesor.Length > 0 && textosProfesor[0] != null)
        {
            textosProfesor[0].SetActive(true);
            EfectoMaquinaEscribir effectP1 = textosProfesor[0].GetComponent<EfectoMaquinaEscribir>();
            if (effectP1 != null)
            {
                effectP1.IniciarEfecto();
                while (effectP1.EstaEscribiendo())
                    yield return null;
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
        }

        // Reproducir sonido un segundo antes de que aparezca el portal
        PlayPortalSound(portalWarningSound);

        // 1 segundo de espera antes de que aparezca el portal
        yield return new WaitForSeconds(1f);

        if (textosProfesor != null && textosProfesor.Length > 0 && textosProfesor[0] != null)
            textosProfesor[0].SetActive(false);

        // Aparece el portal
        portalCiudad.SetActive(true);

        // --- textop[1]: aparece cuando aparece el portal ---
        if (textosProfesor != null && textosProfesor.Length > 1 && textosProfesor[1] != null)
        {
            yield return new WaitForSeconds(2f);
            textosProfesor[1].SetActive(true);
            EfectoMaquinaEscribir effectP2 = textosProfesor[1].GetComponent<EfectoMaquinaEscribir>();
            if (effectP2 != null)
            {
                effectP2.IniciarEfecto();
                while (effectP2.EstaEscribiendo())
                    yield return null;
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
        }

        // 1 segundo de espera antes de que empiece a caminar hacia el portal
        yield return new WaitForSeconds(2f);

        if (textosProfesor != null && textosProfesor.Length > 1 && textosProfesor[1] != null)
            textosProfesor[1].SetActive(false);

        // El profesor camina hacia el portal
        enteringPortal = true;
        GetComponent<Animator>().enabled = true;
    }

    void Start()
    {
        // Asegurar que comience en Idle y con el Animator desactivado
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && idleSprite != null)
        {
            sr.sprite = idleSprite;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
        }

        StartCoroutine(InicioIntro());
    }

    // Controla si mira a la izquierda o derecha basándose en el siguiente destino
    void ActualizarDireccionDeMirada(Vector3 destino)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Si el destino está a la izquierda, flipX = true. Si está a la derecha, flipX = false.
            if (destino.x < transform.position.x)
            {
                sr.flipX = true;
            }
            else if (destino.x > transform.position.x)
            {
                sr.flipX = false;
            }
        }
    }

    IEnumerator AppearPortalCiudad()
    {
        yield return new WaitForSeconds(2f);

        portalCiudad.SetActive(true);

        yield return new WaitForSeconds(2f);
    }

    IEnumerator MostrarCastillo()
    {
        pantallaNegra.SetActive(true);

        yield return new WaitForSeconds(1f);

        fondoCiudad.SetActive(false);

        portalCiudad.SetActive(false);

        fondoCastillo.SetActive(true);

        pantallaNegra.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // Reproducir sonido un segundo antes de que aparezca en el castillo
        PlayPortalSound(portalCastleSound);

        yield return new WaitForSeconds(1.0f);

        portalCastillo.SetActive(true);

        yield return new WaitForSeconds(3.25f);

        GetComponent<SpriteRenderer>().enabled = true;

        transform.position = portalCastillo.transform.position;

        transform.localScale = escalaInicial;

        GetComponent<Animator>().enabled = true;

        saliendoDelPortal = true;
    }

    void SaltarIntro()
    {
        // Si la intro termina en el nivel principal:
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }

    // Desaparece el portal achicándose
    IEnumerator DesaparecerPortalCastillo()
    {
        if (portalCastillo != null)
        {
            // Desactivamos el Animator para que no sobrescriba la escala del portal
            Animator portalAnim = portalCastillo.GetComponent<Animator>();
            if (portalAnim != null)
            {
                portalAnim.enabled = false;
            }

            Vector3 escalaOriginalPortal = portalCastillo.transform.localScale;
            float tiempo = 1f; // duración en segundos del achicado
            float transcurrido = 0f;

            while (transcurrido < tiempo)
            {
                transcurrido += Time.deltaTime;
                portalCastillo.transform.localScale = Vector3.Lerp(escalaOriginalPortal, Vector3.zero, transcurrido / tiempo);
                yield return null;
            }

            portalCastillo.SetActive(false);
            // Restauramos su escala por si acaso se reinicia el nivel
            portalCastillo.transform.localScale = escalaOriginalPortal;

            // Reactivamos el Animator para que esté listo la próxima vez
            if (portalAnim != null)
            {
                portalAnim.enabled = true;
            }
        }
    }
       // Secuencia de diálogo en el castillo y luego caminata
    IEnumerator SecuenciaDialogoCastillo()
    {
        // Esperar a que el portal termine de achicarse (1 segundo) + 0.5s extra
        yield return new WaitForSeconds(1.5f);

        // Mostrar textosProfesor[2] al [5] en secuencia
        for (int i = 2; i <= 5; i++)
        {
            if (textosProfesor == null || textosProfesor.Length <= i || textosProfesor[i] == null)
                continue;

            textosProfesor[i].SetActive(true);

            // Esperar la duración del efecto máquina de escribir
            EfectoMaquinaEscribir efecto = textosProfesor[i].GetComponent<EfectoMaquinaEscribir>();
            if (efecto != null)
            {
                float duracion = efecto.textoCompleto.Length * efecto.velocidadEscritura;
                yield return new WaitForSeconds(duracion);
            }

            // Mantener el texto visible 1 segundo más
            yield return new WaitForSeconds(1f);

            // Ocultar este texto e inmediatamente mostrar el siguiente
            textosProfesor[i].SetActive(false);
        }

        // Iniciar la caminata hacia el castillo
        GetComponent<Animator>().enabled = true;

        waypointActual = 0;
        distanciaRecorrida = 0f;

        if (waypoints != null && waypoints.Length > 0)
        {
            if (waypoints[0] != null)
                ActualizarDireccionDeMirada(waypoints[0].position);

            distanciaTotalCaminata = 0f;
            Vector3 puntoAnterior = transform.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    distanciaTotalCaminata += Vector2.Distance(puntoAnterior, waypoints[i].position);
                    puntoAnterior = waypoints[i].position;
                }
            }
        }
        else
        {
            distanciaTotalCaminata = 0f;
        }

        caminandoHaciaCastillo = true;
        StartCoroutine(MostrarTextoP6());
    }

    IEnumerator MostrarTextoP6()
    {
        yield return new WaitForSeconds(1f);

        if (textosProfesor != null && textosProfesor.Length > 6 && textosProfesor[6] != null)
        {
            textosProfesor[6].SetActive(true);
            EfectoMaquinaEscribir efecto = textosProfesor[6].GetComponent<EfectoMaquinaEscribir>();
            if (efecto != null)
            {
                float duracion = efecto.textoCompleto.Length * efecto.velocidadEscritura;
                yield return new WaitForSeconds(duracion);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            yield return new WaitForSeconds(1.5f);
            textosProfesor[6].SetActive(false);
        }
    }
    
    // Espera en spawnCastillo y luego inicia la caminata
    IEnumerator EsperarYCaminarAlCastillo()
    {
        yield return new WaitForSeconds(tiempoEsperaEnCastillo);

        GetComponent<Animator>().enabled = true;

        waypointActual = 0;
        distanciaRecorrida = 0f;

        if (waypoints != null && waypoints.Length > 0)
        {
            // Mirar hacia el primer waypoint
            if (waypoints[0] != null)
            {
                ActualizarDireccionDeMirada(waypoints[0].position);
            }

            // Calcular distancia total en 2D (evita errores si los waypoints tienen Z distintos)
            distanciaTotalCaminata = 0f;
            Vector3 puntoAnterior = transform.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    distanciaTotalCaminata += Vector2.Distance(puntoAnterior, waypoints[i].position);
                    puntoAnterior = waypoints[i].position;
                }
            }
        }
        else
        {
            distanciaTotalCaminata = 0f;
        }

        caminandoHaciaCastillo = true;
    }

    void OnLlegoAlCastillo()
    {
        GetComponent<Animator>().enabled = false;
        GetComponent<SpriteRenderer>().sprite = idleSprite;

        StartCoroutine(TransicionAlJuego());
    }

    // Transición de pantalla a negro y carga del juego
    IEnumerator TransicionAlJuego()
    {
        if (pantallaNegra != null)
        {
            pantallaNegra.SetActive(true);
        }

        yield return new WaitForSeconds(1.5f); // 1.5 segundos en negro para el fadeout de audio/ambiente si hiciera falta

        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }

    private void PlayPortalSound(AudioClip clip)
    {
        if (clip == null) return;
        
        Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, playPos);
    }
}