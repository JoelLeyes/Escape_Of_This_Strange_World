using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy1 : MonoBehaviour
{
    // Referencia al jugador (se puede asignar manualmente o buscar por tag)
    [Header("Objetivo")]
    public GameObject objetivo;
    [SerializeField] private string targetTag = "Player";

    [Header("Movimiento")]
    public float speed = 0.3f;
    public bool debePerseguir;
    public float distancia = 5f;
    [SerializeField] private bool invertFacing = false;

    [Header("Rutina")]
    public float cronometro;
    public int rutina;
    public int direccion;

    [Header("Combate")]
    public float vida = 100f;
    public GameObject hitbox;
    [SerializeField] private float attackLockDuration = 1.0f;
    [SerializeField] private float attackCooldown = 1.25f;
    [SerializeField] private float attackDistance = 1.2f;
    [SerializeField] private float attackDistanceY = 1.5f;
    [SerializeField] private float stopDistance = 0.20f;
    [SerializeField] private float danioGolpe = 25f;
    [SerializeField] private float radioGolpe = 1.3f;
    [SerializeField] private Vector2 offsetGolpe = new Vector2(0.7f, 0.2f);
    [SerializeField] private AudioClip damageClip;

    private float distanciaDelObjetivo;
    private float distanciaDelObjetivoEjeY;
    private float distanciaAbsoluta;
    private float distanciaAbsolutaEjeY;

    private bool canMove = true;
    private bool puedeAtacar = true;

    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource damageAudioSource;
    private float baseScaleX;
    private SpriteRenderer[] spriteRenderers;
    private float attackUnlockTime;
    private float nextAttackTime;
    private bool danioAplicadoEnAtaque;

#if UNITY_EDITOR
    private const string DamageClipPath = "Assets/Sound/SFX_hit&damageEsqueleto13.wav";
#endif

    /***************** EVENTOS DE ANIMACION ******************/
    // Evento de animacion: inicio del golpe
    public void golpeInicio()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true);
        }
        AplicarDanioAtaque();
    }

    // Evento de animacion: fin del golpe
    public void golpeFin()
    {
        EndAttackState();
    }

    /***************** DAÑO Y COLISIONES ******************/
    public void RecibirDanio(float danio)
    {
        vida -= danio;
        PlayDamageSound();

        if (animator != null)
        {
            animator.SetTrigger("hurt");
            return;
        }
    }

    private void EnsureDamageAudioSource()
    {
        if (damageAudioSource == null)
        {
            damageAudioSource = GetComponent<AudioSource>();
        }

        if (damageAudioSource == null)
        {
            damageAudioSource = gameObject.AddComponent<AudioSource>();
        }

        damageAudioSource.playOnAwake = false;
        damageAudioSource.loop = false;
        damageAudioSource.spatialBlend = 0f;
        damageAudioSource.volume = 1f;
    }

    private void AutoAssignDamageClip()
    {
#if UNITY_EDITOR
        if (damageClip == null)
        {
            damageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DamageClipPath);
        }
#endif
    }

    private void PlayDamageSound()
    {
        if (damageAudioSource == null || damageClip == null)
        {
            return;
        }

        damageAudioSource.PlayOneShot(damageClip);
    }

    private void Awake()
    {
        EnsureDamageAudioSource();
        AutoAssignDamageClip();
    }

    private void Start()
    {
        // Obtiene los componentes necesarios del enemigo
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX <= 0f)
        {
            baseScaleX = 1f;
        }

        if (animator == null)
        {
            Debug.LogWarning("Enemy1: falta Animator en el objeto enemigo, por eso no entra en animacion Run.", this);
        }

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

        if (objetivo != null)
        {
            float deltaX = objetivo.transform.position.x - transform.position.x;
            SetFacingToTarget(deltaX);
        }

        if (!canMove && Time.time >= attackUnlockTime)
        {
            EndAttackState();
        }

        if (!puedeAtacar && Time.time >= nextAttackTime)
        {
            puedeAtacar = true;
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
            if (distanciaAbsoluta > stopDistance)
            {
                transform.position = Vector2.MoveTowards(transform.position, objetivo.transform.position, speed * Time.deltaTime);
            }
        }

        /********************************* GOLPEAR JUGADOR **********************************/
        if (puedeAtacar
            && distanciaAbsoluta <= attackDistance + 0.35f
            && IsTargetInsideAttackRange(distanciaAbsolutaEjeY))
        {
            // Frena antes de iniciar el golpe para evitar deslizamientos
            rb.linearVelocity *= 0.95f;
            canMove = false;
            attackUnlockTime = Time.time + attackLockDuration;
            danioAplicadoEnAtaque = false;

            if (rb.linearVelocity.magnitude < 0.1f)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (animator != null)
            {
                // IMPORTANTE: En el Animator de Enemy1, la transición a Attack sale desde "Run".
                // Si apagamos "perseguir" (Run) en el mismo frame en que activamos "melee",
                // el Animator prioriza la transición Run -> idle y nunca entra a Attack.
                animator.SetBool("perseguir", true);
                animator.SetBool("melee", true);
            }

            AplicarDanioAtaque();
            puedeAtacar = false;
            nextAttackTime = Time.time + attackCooldown;
            return;
        }
        /************************************************************************************/

        if (distanciaAbsoluta < distancia)
        {
            SetFacingToTarget(distanciaDelObjetivo);

            debePerseguir = true;
            SetRunAnimation(true);

            cronometro = 0;
            rutina = 0;
        }
        else
        {
            /************************************** RUTINA ***************************************/
            debePerseguir = false;
            SetRunAnimation(false);

            cronometro += Time.deltaTime;
            if (cronometro >= 4)
            {
                rutina = Random.Range(0, 2);
                cronometro = 0;
            }

            switch (rutina)
            {
                case 0:
                    SetRunAnimation(false);
                    break;
                case 1:
                    direccion = Random.Range(0, 2);
                    rutina++;
                    break;
                case 2:
                    switch (direccion)
                    {
                        case 0:
                            SetFacingToTarget(1f);
                            transform.Translate(Vector3.right * speed * Time.deltaTime);
                            break;
                        case 1:
                            SetFacingToTarget(-1f);
                            transform.Translate(Vector3.left * speed * Time.deltaTime);
                            break;
                    }

                    SetRunAnimation(true);
                    break;
            }
            /**************************************************************************************/
        }
    }

    private void AplicarDanioAtaque()
    {
        if (danioAplicadoEnAtaque)
        {
            return;
        }

        Player playerObjetivo = null;
        if (objetivo != null)
        {
            playerObjetivo = objetivo.GetComponent<Player>();
            if (playerObjetivo == null)
            {
                playerObjetivo = objetivo.GetComponentInParent<Player>();
            }
        }

        if (playerObjetivo != null)
        {
            float dx = Mathf.Abs(objetivo.transform.position.x - transform.position.x);
            float dy = Mathf.Abs(objetivo.transform.position.y - transform.position.y);
            if (dx <= attackDistance + 0.35f && IsTargetInsideAttackRange(dy))
            {
                playerObjetivo.RecibirDanio(danioGolpe);
                danioAplicadoEnAtaque = true;
                return;
            }
        }

        int direccionGolpe = 1;
        if (objetivo != null && objetivo.transform.position.x < transform.position.x)
        {
            direccionGolpe = -1;
        }

        Vector2 centroGolpe = (Vector2)transform.position + new Vector2(offsetGolpe.x * direccionGolpe, offsetGolpe.y);
        Collider2D[] impactos = Physics2D.OverlapBoxAll(centroGolpe, new Vector2(radioGolpe * 2f, attackDistanceY), 0f);
        for (int i = 0; i < impactos.Length; i++)
        {
            if (impactos[i] != null && impactos[i].GetComponentInParent<Player>() != null)
            {
                impactos[i].GetComponentInParent<Player>().RecibirDanio(danioGolpe);
                danioAplicadoEnAtaque = true;
                return;
            }
        }
    }

    private bool IsTargetInsideAttackRange(float deltaY)
    {
        float maxDeltaY = attackDistanceY * 0.5f;
        return Mathf.Abs(deltaY - offsetGolpe.y) <= maxDeltaY;
    }

    private void SetFacingToTarget(float deltaX)
    {
        if (Mathf.Abs(deltaX) < 0.001f)
        {
            return;
        }

        bool lookRight = deltaX > 0f;
        if (invertFacing)
        {
            lookRight = !lookRight;
        }

        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].flipX = lookRight;
                }
            }

            // Si estamos usando flipX, mantenemos escala positiva para no anular el giro.
            transform.localScale = new Vector3(baseScaleX, transform.localScale.y, transform.localScale.z);
            return;
        }

        float facingScaleX = lookRight ? -baseScaleX : baseScaleX;
        transform.localScale = new Vector3(facingScaleX, transform.localScale.y, transform.localScale.z);
    }

    private void SetRunAnimation(bool isRunning)
    {
        if (animator != null)
        {
            animator.SetBool("perseguir", isRunning);
        }
    }

    private void EndAttackState()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }

        canMove = true;
        puedeAtacar = false;
        danioAplicadoEnAtaque = false;
        attackUnlockTime = 0f;
        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
        {
            animator.SetBool("melee", false);
        }
    }
}
