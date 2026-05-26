using UnityEngine;

public class Enemy2 : MonoBehaviour
{
    [Header("Objetivo")]
    public GameObject objetivo;
    [SerializeField] private string targetTag = "Player";

    [Header("Movimiento")]
    public float speed = 0.12f;
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
    [SerializeField] private float meleeDistance = 1.2f;
    [SerializeField] private float attackDistanceY = 1.5f;
    [SerializeField] private float stopDistance = 0.20f;
    [SerializeField] private float danioGolpe = 40f;
    [SerializeField] private float radioGolpe = 1.3f;
    [SerializeField] private Vector2 offsetGolpe = new Vector2(0.7f, 0.2f);

    [Header("Ataque a distancia")]
    [SerializeField] private float rangedDistance = 1.5f;
    [SerializeField] private GameObject armPrefab;
    [SerializeField] private Vector2 rangedSpawnOffset = new Vector2(0.5f, 0.3f);
    [SerializeField] private string attackAnimatorBool = "attack";

    private float distanciaDelObjetivo;
    private float distanciaDelObjetivoEjeY;
    private float distanciaAbsoluta;
    private float distanciaAbsolutaEjeY;

    private bool canMove = true;
    private bool puedeAtacar = true;
    private bool puedeAtacarRanged = true;
    private bool estaAtacandoRanged;
    private bool estaAtacando;

    private Rigidbody2D rb;
    private Animator animator;
    private float baseScaleX;
    private SpriteRenderer[] spriteRenderers;
    private float attackUnlockTime;
    private bool danioAplicadoEnAtaque;

    /***************** EVENTOS DE ANIMACION ******************/
    public void golpeInicio()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true);
        }

        AplicarDanioAtaque();
    }

    public void golpeFin()
    {
        EndAttackState();
    }

    public void Arm_Attack_End()
    {
        if (armPrefab == null)
        {
            EndAttackState();
            return;
        }

        int direccionArm = 1;
        if (objetivo != null && objetivo.transform.position.x < transform.position.x)
        {
            direccionArm = -1;
        }

        Vector3 spawnPos = transform.position + new Vector3(rangedSpawnOffset.x * direccionArm, rangedSpawnOffset.y, 0f);
        GameObject arm = Instantiate(armPrefab, spawnPos, Quaternion.identity);

        ArmSkeleton armSkeleton = arm.GetComponent<ArmSkeleton>();
        if (armSkeleton == null)
        {
            armSkeleton = arm.GetComponentInChildren<ArmSkeleton>();
        }

        if (armSkeleton != null)
        {
            armSkeleton.Initialize(direccionArm);
        }

        Vector3 scale = arm.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direccionArm;
        arm.transform.localScale = scale;

        estaAtacandoRanged = false;
        estaAtacando = false;
        EndAttackState();
    }

    /***************** DAÑO Y COLISIONES ******************/
    public void RecibirDanio(float danio)
    {
        vida -= danio;

        if (animator != null)
        {
            animator.SetTrigger("hurt");
        }
    }

    private void Start()
    {
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
            Debug.LogWarning("Enemy2: falta Animator en el objeto enemigo.", this);
        }

        if (objetivo == null)
        {
            GameObject foundByTag = GameObject.FindWithTag(targetTag);
            if (foundByTag != null)
            {
                objetivo = foundByTag;
            }
            else
            {
                Player player = FindFirstObjectByType<Player>();
                if (player != null)
                {
                    objetivo = player.gameObject;
                }
            }
        }

        if (objetivo == null)
        {
            Debug.LogWarning("Enemy2: no se encontro objetivo. Asigna 'objetivo' o configura 'targetTag'.");
        }
    }

    private void Update()
    {
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

        if (estaAtacando && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (!canMove && Time.time >= attackUnlockTime && !estaAtacando)
        {
            EndAttackState();
        }

        if (objetivo == null)
        {
            return;
        }

        if (!canMove)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            return;
        }

        distanciaDelObjetivo = objetivo.transform.position.x - transform.position.x;
        distanciaAbsoluta = Mathf.Abs(distanciaDelObjetivo);
        distanciaDelObjetivoEjeY = objetivo.transform.position.y - transform.position.y;
        distanciaAbsolutaEjeY = Mathf.Abs(distanciaDelObjetivoEjeY);

        bool dentroDeMelee = distanciaAbsoluta <= meleeDistance && distanciaAbsolutaEjeY <= attackDistanceY;
        bool dentroDeRanged = distanciaAbsoluta <= rangedDistance && distanciaAbsoluta > meleeDistance && distanciaAbsolutaEjeY <= attackDistanceY;

        if (canMove && puedeAtacar && dentroDeMelee)
        {
            canMove = false;
            estaAtacando = true;
            attackUnlockTime = Time.time + attackLockDuration;
            danioAplicadoEnAtaque = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (animator != null)
            {
                animator.SetBool("perseguir", true);
                animator.SetBool("melee", true);
                animator.SetBool("ranged", false);
                animator.SetBool("debeAtacar", false);
                animator.SetBool(attackAnimatorBool, false);
            }

            AplicarDanioAtaque();
            puedeAtacar = false;
            return;
        }

        if (canMove && puedeAtacarRanged && dentroDeRanged)
        {
            canMove = false;
            estaAtacandoRanged = true;
            estaAtacando = true;
            attackUnlockTime = Time.time + attackLockDuration;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (animator != null)
            {
                animator.SetBool("perseguir", true);
                animator.SetBool("melee", false);
                animator.SetBool("ranged", true);
                animator.SetBool("debeAtacar", true);
                animator.SetBool(attackAnimatorBool, true);
            }

            puedeAtacarRanged = false;
            return;
        }

        if (distanciaAbsoluta <= rangedDistance)
        {
            debePerseguir = true;
            SetRunAnimation(true);

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            cronometro = 0;
            rutina = 0;
            return;
        }

        if (distanciaAbsoluta < distancia)
        {
            debePerseguir = true;
            SetRunAnimation(true);

            if (distanciaAbsoluta > stopDistance)
            {
                transform.position = Vector2.MoveTowards(transform.position, objetivo.transform.position, speed * Time.deltaTime);
            }

            cronometro = 0;
            rutina = 0;
        }
        else
        {
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
        }

        if (debePerseguir && canMove)
        {
            if (distanciaAbsoluta > stopDistance)
            {
                transform.position = Vector2.MoveTowards(transform.position, objetivo.transform.position, speed * Time.deltaTime);
            }
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
            if (dx <= meleeDistance + 0.35f && dy <= attackDistanceY + 0.6f)
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
        Collider2D[] impactos = Physics2D.OverlapCircleAll(centroGolpe, radioGolpe);
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
        puedeAtacar = true;
        puedeAtacarRanged = true;
        estaAtacandoRanged = false;
        estaAtacando = false;
        danioAplicadoEnAtaque = false;
        attackUnlockTime = 0f;

        if (animator != null)
        {
            animator.SetBool("melee", false);
            animator.SetBool("ranged", false);
            animator.SetBool("debeAtacar", false);
            animator.SetBool(attackAnimatorBool, false);
        }
    }
}
