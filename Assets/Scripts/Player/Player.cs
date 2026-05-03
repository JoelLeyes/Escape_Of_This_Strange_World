using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    private enum AttackType
    {
        None,
        Magic,
        Sword
    }

    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private float tiempoInvulnerable = 0.5f;
    [SerializeField] private Slider barraVida;

    public float JumpForce;
    public float Speed;
    public float JumpCooldown = 0.1f;
    public float DashSpeed = 16f;
    public float DashDuration = 0.12f;
    public float DashCooldown = 0.6f;
    [SerializeField] private string dashAnimatorBool = "Dashing";
    [SerializeField] private string swordAnimatorTrigger = "Sword";
    [SerializeField] private LayerMask dashIgnoreCollisionLayers;
    [SerializeField] private Fire firePrefab;
    [SerializeField] private PlayerAttackHitbox swordHitbox;

    private Rigidbody2D Rigidbody2D;  //defino una variable global(puedo acceder de cualquier parte del script)
    private Collider2D PlayerCollider;
    private Animator Animator;
    private float Horizontal;
    private bool Grounded;
    private float nextJumpTime;
    private float nextAttackTime;
    private float nextDashTime;
    private float dashEndTime;
    private int dashDirection = 1;
    private bool isDashing;
    private bool attackActive;
    private AttackType currentAttackType = AttackType.None;

    private bool canMove = true;
    private float vidaActual;
    private float nextDamageTime;
    private bool nivelPerdido;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>(); //esta funcion mete el componente Rigidbody dentro del script
        PlayerCollider = GetComponent<Collider2D>();
        Animator = GetComponent<Animator>();
        if (swordHitbox == null)
        {
            swordHitbox = GetComponentInChildren<PlayerAttackHitbox>(true);
        }

        vidaActual = vidaMaxima;
        ActualizarBarraVida();

        if (Animator == null)
        {
            Debug.LogWarning("No Animator found on Player. Animation states will be skipped.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDashing && Time.time >= dashEndTime)
        {
            StopDash();
        }

        //RAYOS
        RaycastHit2D centerHit = Physics2D.Raycast(transform.position, Vector3.down, 0.24f); //rayo central
        RaycastHit2D rightHit = Physics2D.Raycast(transform.position + Vector3.right * 0.15f, Vector3.down, 0.24f); //rayo derecha
        RaycastHit2D leftHit = Physics2D.Raycast(transform.position + Vector3.left * 0.15f, Vector3.down, 0.24f); //rayo izquierda

        if ((centerHit.collider != null && centerHit.collider != PlayerCollider) ||
            (rightHit.collider != null && rightHit.collider != PlayerCollider) ||
            (leftHit.collider != null && leftHit.collider != PlayerCollider))
        {
            Grounded = true;
            Animator.SetBool("Jumping", false);
        }
        else {
            Grounded = false;
            Animator.SetBool("Jumping", true);
        }

        if (isDashing)
        {
            Horizontal = 0f;
            if (Animator != null)
            {
                Animator.SetBool("Running", false);
            }
            return;
        }

        //ROTAR SPRITE
        if (canMove) {
            Horizontal = GetHorizontalInput(); //valores de -1 0 1
            if (Horizontal < 0.0f)
            {
                transform.eulerAngles = new Vector3(0, 180, 0);
            }
            else if (Horizontal > 0.0f)
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
            }
            if (Grounded && Animator != null)
                Animator.SetBool("Running", Horizontal != 0.0f);
        }

        if (canMove && !isDashing && IsDashPressed() && Time.time >= nextDashTime)
        {
            dashDirection = Horizontal != 0f ? (Horizontal > 0f ? 1 : -1) : (transform.right.x >= 0f ? 1 : -1);
            isDashing = true;
            dashEndTime = Time.time + DashDuration;
            nextDashTime = Time.time + DashCooldown;
            SetDashVisual(true);
            SetDashCollisionIgnore(true);
        }

        //SALTO
        if (canMove && IsJumpPressed() && Grounded && Time.time >= nextJumpTime) {
            Jump();
            nextJumpTime = Time.time + JumpCooldown;
        }

        SwordAttack();
        MagicAttack();
    }
    private float GetHorizontalInput()
    {
        if (Keyboard.current == null)
        {
            return 0f;
        }

        bool left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
        bool right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;

        if (left == right)
        {
            return 0f;
        }

        return right ? 1f : -1f;
    }

    private bool IsJumpPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.wKey.wasPressedThisFrame
               || Keyboard.current.upArrowKey.wasPressedThisFrame
               || Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private bool IsDashPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        return Keyboard.current.leftShiftKey.wasPressedThisFrame
               || Keyboard.current.rightShiftKey.wasPressedThisFrame;
    }

    private void MagicAttack()
    {
        if (Keyboard.current == null
            || !Keyboard.current.qKey.wasPressedThisFrame
            || !Grounded
            || Animator == null
            || attackActive
            || !Cooldown(0.35f))
        {
            return;
        }

        canMove = false;
        attackActive = true;
        currentAttackType = AttackType.Magic;
        Animator.SetTrigger("Magic");

        if (firePrefab == null)
        {
            Debug.LogError("Fire prefab is not assigned in Player Inspector.", this);
            return;
        }

        int direction = transform.right.x >= 0f ? 1 : -1;
        Fire fire = Instantiate(firePrefab, transform.position, Quaternion.identity);
        fire.Initialize(direction);
    }

    public void Magic_Cast()
    {
        // Se deja por compatibilidad con eventos de animación, pero la magia vuelve a crear el fuego al presionar Q.
    }

    private void SwordAttack()
    {
        if (Keyboard.current == null
            || !Keyboard.current.jKey.wasPressedThisFrame
            || !Grounded
            || Animator == null
            || attackActive
            || !canMove)
        {
            return;
        }

        canMove = false;
        attackActive = true;
        currentAttackType = AttackType.Sword;
        Animator.SetTrigger(swordAnimatorTrigger);
    }

    public void Attack_Begin()
    {
        canMove = false;
        attackActive = true;

        if (Animator != null)
        {
            Animator.SetBool("Running", false);
        }

        if (currentAttackType == AttackType.Sword && swordHitbox != null)
        {
            swordHitbox.BeginAttack();
        }
    }

    public void Attack_End()
    {
        if (currentAttackType == AttackType.Sword && swordHitbox != null)
        {
            swordHitbox.EndAttack();
        }

        attackActive = false;
        canMove = true;
        currentAttackType = AttackType.None;
    }

    private bool Cooldown(float cooldown)
    {
        if (Time.time < nextAttackTime)
        {
            return false;
        }

        nextAttackTime = Time.time + cooldown;
        return true;
    }

    private void Jump()
    {
        Rigidbody2D.AddForce(Vector2.up * JumpForce);
    }

    private void StopDash()
    {
        isDashing = false;
        SetDashVisual(false);
        SetDashCollisionIgnore(false);
    }

    private void SetDashVisual(bool active)
    {
        if (Animator == null || string.IsNullOrEmpty(dashAnimatorBool))
        {
            return;
        }

        Animator.SetBool(dashAnimatorBool, active);
    }

    private void SetDashCollisionIgnore(bool ignore)
    {
        int layersToIgnore = dashIgnoreCollisionLayers.value;
        if (layersToIgnore == 0)
        {
            return;
        }

        int playerLayer = gameObject.layer;
        for (int layer = 0; layer < 32; layer++)
        {
            if ((layersToIgnore & (1 << layer)) != 0)
            {
                Physics2D.IgnoreLayerCollision(playerLayer, layer, ignore);
            }
        }
    }

    private void OnDisable()
    {
        SetDashVisual(false);
        SetDashCollisionIgnore(false);
    }

    public void RecibirDanio(float danio)
    {
        if (nivelPerdido || Time.time < nextDamageTime)
        {
            return;
        }

        vidaActual -= danio;
        nextDamageTime = Time.time + tiempoInvulnerable;
        ActualizarBarraVida();

        if (vidaActual <= 0f)
        {
            PerderNivel();
        }
    }

    private void PerderNivel()
    {
        if (nivelPerdido)
        {
            return;
        }

        nivelPerdido = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("Player: no se encontro GameManager para cargar la escena de derrota.", this);
        }
    }

    private void ActualizarBarraVida()
    {
        if (barraVida == null)
        {
            return;
        }

        barraVida.maxValue = vidaMaxima;
        barraVida.value = Mathf.Max(vidaActual, 0f);
    }

    private void FixedUpdate()
    /*el fixed update se actualiza mucho mas rapido que el update y esto es necesario para las fisicas
    es una funcion incorporada en Unity que se llama automaticamente en intervalos de tiempo fijos
    para manejar operaciones relacionadas con la fisica del juego.*/
    {
        if (isDashing)
        {
            Rigidbody2D.linearVelocity = new Vector2(dashDirection * DashSpeed, 0f);
            return;
        }

        if (canMove) {
            float horizontalMovement = Horizontal * Speed * Time.deltaTime;
            Vector2 velocity = new Vector2(horizontalMovement, Rigidbody2D.linearVelocity.y);
            Rigidbody2D.linearVelocity = velocity;
        }
        
    }

    private void OnDrawGizmos() //dibujamos los 3 rayos
    {
        Debug.DrawRay(transform.position + Vector3.right * 0.15f, Vector3.down * 0.24f, Color.red);
        Debug.DrawRay(transform.position + Vector3.left * 0.15f, Vector3.down * 0.24f, Color.red);
        Debug.DrawRay(transform.position, Vector3.down * 0.24f, Color.red);
    }
}
