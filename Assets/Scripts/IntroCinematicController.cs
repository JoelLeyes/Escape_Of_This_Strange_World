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

                StartCoroutine(AppearPortalCiudad());
            }
        }
        
        if (enteringPortal)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            transform.localScale -= Vector3.one * 0.5f * Time.deltaTime;

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

                // Iniciar la espera y luego caminar hacia el castillo
                StartCoroutine(EsperarYCaminarAlCastillo());
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

    IEnumerator InicioIntro()
    {
        pantallaNegra.SetActive(true);

        // Nos aseguramos de que los textos inicien desactivados para ir mostrándolos secuencialmente
        if (textoInicio != null) textoInicio.SetActive(false);
        if (textoInicio2 != null) textoInicio2.SetActive(false);
        if (textoInicio3 != null) textoInicio3.SetActive(false);

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

        yield return new WaitForSeconds(0.20f);

        introComenzo = true;
    }

        void Start()
    {
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

        enteringPortal = true;

        GetComponent<Animator>().enabled = true;
    }

    IEnumerator MostrarCastillo()
    {
        pantallaNegra.SetActive(true);

        yield return new WaitForSeconds(1f);

        fondoCiudad.SetActive(false);

        portalCiudad.SetActive(false);

        fondoCastillo.SetActive(true);

        pantallaNegra.SetActive(false);

        yield return new WaitForSeconds(1.5f);

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
}