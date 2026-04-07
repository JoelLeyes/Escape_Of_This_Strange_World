using UnityEngine;

public class Enemy1 : MonoBehaviour
{
    // Referencia al jugador (se puede asignar manualmente o buscar por tag)
    [Header("Objetivo")]
    public GameObject objetivo;
    [SerializeField] private string targetTag = "Player";

    [Header("Movimiento")]
    public float speed = 2f;
    public bool debePerseguir;
    public float distancia = 5f;

    [Header("Rutina")]
    public float cronometro;
    public int rutina;
    public int direccion;

    [Header("Combate")]
    public float vida = 100f;
    public GameObject hitbox;

    private float distanciaDelObjetivo;
    private float distanciaDelObjetivoEjeY;
    private float distanciaAbsoluta;
    private float distanciaAbsolutaEjeY;

    private bool canMove = true;
    private bool puedeAtacar = true;

    private Rigidbody2D rb;
    private Animator animator;

    /***************** EVENTOS DE ANIMACION ******************/
    // Evento de animacion: inicio del golpe
    public void golpeInicio()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true);
        }
    }

    // Evento de animacion: fin del golpe
    public void golpeFin()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }

        canMove = true;
        puedeAtacar = true;
        if (animator != null)
        {
            animator.SetBool("melee", false);
        }
    }

    /***************** DAÑO Y COLISIONES ******************/
    // Desactivado temporalmente hasta implementar el sistema de proyectiles/daño.
    // public float friccion_Bala = 0.4f;
    // [SerializeField] private string projectileTag = "WeaponPJ";
    //
    // private void OnCollisionEnter2D(Collision2D collision)
    // {
    //     if (collision.gameObject.CompareTag(projectileTag))
    //     {
    //         rb.linearVelocity *= friccion_Bala;
    //         if (animator != null)
    //         {
    //             animator.SetTrigger("Hurt");
    //         }
    //     }
    // }
    //
    // public float tomarDaño(float daño)
    // {
    //     return daño;
    // }
    /******************************************************/

    private void Start()
    {
        // Obtiene los componentes necesarios del enemigo
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (objetivo == null)
        {
            // Primero intenta encontrar el objetivo por tag
            GameObject foundByTag = GameObject.FindWithTag(targetTag);
            if (foundByTag != null)
            {
                objetivo = foundByTag;
            }
            else
            {
                // Fallback: busca cualquier objeto con script Player en la escena
                Player player = FindFirstObjectByType<Player>();
                if (player != null)
                {
                    objetivo = player.gameObject;
                }
            }
        }

        if (objetivo == null)
        {
            Debug.LogWarning("Enemy1: no se encontro objetivo. Asigna 'objetivo' o configura 'targetTag'.");
        }
    }

    private void Update()
    {
        // Si la vida llega a cero, se destruye el enemigo
        if (vida <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Si no hay objetivo o esta bloqueado por animacion de ataque, no procesa IA
        if (objetivo == null || !canMove)
        {
            return;
        }

        /********************************* PERSEGUIR JUGADOR **********************************/
        distanciaDelObjetivo = objetivo.transform.position.x - transform.position.x;
        distanciaAbsoluta = Mathf.Abs(distanciaDelObjetivo);
        distanciaDelObjetivoEjeY = objetivo.transform.position.y - transform.position.y;
        distanciaAbsolutaEjeY = Mathf.Abs(distanciaDelObjetivoEjeY);

        if (debePerseguir)
        {
            transform.position = Vector2.MoveTowards(transform.position, objetivo.transform.position, speed * Time.deltaTime);
        }

        /********************************* GOLPEAR JUGADOR **********************************/
        if (puedeAtacar && distanciaAbsoluta < 0.6f && distanciaAbsolutaEjeY < 0.6f)
        {
            // Frena antes de iniciar el golpe para evitar deslizamientos
            rb.linearVelocity *= 0.95f;
            canMove = false;

            if (rb.linearVelocity.magnitude < 0.1f)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (animator != null)
            {
                animator.SetBool("melee", true);
            }

            puedeAtacar = false;
            return;
        }
        /************************************************************************************/

        if (distanciaAbsoluta < distancia)
        {
            // Voltea el sprite para mirar al jugador
            if (distanciaDelObjetivo < 0)
            {
                transform.localScale = new Vector3(1, 1, 1);
            }

            if (distanciaDelObjetivo > 0)
            {
                transform.localScale = new Vector3(-1, 1, 1);
            }

            debePerseguir = true;
            if (animator != null)
            {
                animator.SetBool("perseguir", true);
            }

            cronometro = 0;
            rutina = 0;
        }
        else
        {
            /************************************** RUTINA ***************************************/
            debePerseguir = false;
            if (animator != null)
            {
                animator.SetBool("perseguir", false);
            }

            cronometro += Time.deltaTime;
            if (cronometro >= 4)
            {
                rutina = Random.Range(0, 2);
                cronometro = 0;
            }

            switch (rutina)
            {
                case 0:
                    if (animator != null)
                    {
                        animator.SetBool("perseguir", false);
                    }
                    break;
                case 1:
                    direccion = Random.Range(0, 2);
                    rutina++;
                    break;
                case 2:
                    switch (direccion)
                    {
                        case 0:
                            transform.localScale = new Vector3(-1, 1, 1);
                            transform.Translate(Vector3.right * speed * Time.deltaTime);
                            break;
                        case 1:
                            transform.localScale = new Vector3(1, 1, 1);
                            transform.Translate(Vector3.left * speed * Time.deltaTime);
                            break;
                    }

                    if (animator != null)
                    {
                        animator.SetBool("perseguir", true);
                    }
                    break;
            }
            /**************************************************************************************/
        }
    }
}
