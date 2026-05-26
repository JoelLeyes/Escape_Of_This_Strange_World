using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    private enum AttackType
    {
        None,
        Magic,
        Sword
    }

    [Header("Vida")]
    [SerializeField] private int corazonesMaximos = 10;
    [SerializeField] private float tiempoInvulnerable = 0.5f;
    [SerializeField] private Slider barraVida;
    [SerializeField] private Image[] corazones;
    [SerializeField] private Sprite corazonLleno;
    [SerializeField] private Sprite corazonVacio;
    [Header("UI Corazones")]
    [SerializeField] private bool autoCrearHUD = true;
    [SerializeField] private Vector2 hudOffset = new Vector2(20f, -20f);
    [SerializeField] private Vector2 hudCorazonSize = new Vector2(32f, 32f);
    [SerializeField] private float hudEspacioCorazones = 6f;

    public float JumpForce;
    public float Speed;
    public float JumpCooldown = 0.1f;
    public float DashSpeed = 16f;
    public float DashDuration = 0.12f;
    public float DashCooldown = 0.6f;
    [SerializeField] private string dashAnimatorBool = "Dashing";
    [SerializeField] private string swordAnimatorTrigger = "Sword";
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
    private readonly List<Collider2D> ignoredEnemyColliders = new List<Collider2D>();
    private bool attackActive;
    private AttackType currentAttackType = AttackType.None;

    private bool canMove = true;
    private int corazonesActuales;
    private float nextDamageTime;
    private bool nivelPerdido;
    private const float danoPorCorazon = 20f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHazardCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHazardCollision(other.gameObject);
    }

    private void HandleHazardCollision(GameObject other)
    {
        if (other.CompareTag("Trap"))
        {
            RecibirDanio(corazonesMaximos * danoPorCorazon);
        }
    }

    void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>(); //esta funcion mete el componente Rigidbody dentro del script
        PlayerCollider = GetComponent<Collider2D>();
        Animator = GetComponent<Animator>();
        if (swordHitbox == null)
        {
            swordHitbox = GetComponentInChildren<PlayerAttackHitbox>(true);
        }

        corazonesActuales = corazonesMaximos;
        ActualizarCorazones();
        EnsureHeartDisplay();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyCheckpoint(transform, Rigidbody2D);
        }

        if (Animator == null)
        {
            Debug.LogWarning("No Animator found on Player. Animation states will be skipped.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // No procesar input si el juego está pausado
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused())
        {
            return;
        }

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
            SetDashEnemyCollisionIgnore(true);
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
        SetDashEnemyCollisionIgnore(false);
    }

    private void SetDashVisual(bool active)
    {
        if (Animator == null || string.IsNullOrEmpty(dashAnimatorBool))
        {
            return;
        }

        Animator.SetBool(dashAnimatorBool, active);
    }

    private void SetDashEnemyCollisionIgnore(bool ignore)
    {
        if (PlayerCollider == null)
        {
            return;
        }

        if (ignore)
        {
            if (ignoredEnemyColliders.Count > 0)
            {
                SetDashEnemyCollisionIgnore(false);
            }

            Enemy1[] enemies = FindObjectsByType<Enemy1>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                {
                    continue;
                }

                Collider2D[] enemyColliders = enemies[i].GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < enemyColliders.Length; j++)
                {
                    Collider2D enemyCollider = enemyColliders[j];
                    if (enemyCollider == null || enemyCollider == PlayerCollider)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(PlayerCollider, enemyCollider, true);
                    ignoredEnemyColliders.Add(enemyCollider);
                }
            }

            return;
        }

        for (int i = 0; i < ignoredEnemyColliders.Count; i++)
        {
            Collider2D enemyCollider = ignoredEnemyColliders[i];
            if (enemyCollider != null)
            {
                Physics2D.IgnoreCollision(PlayerCollider, enemyCollider, false);
            }
        }

        ignoredEnemyColliders.Clear();
    }

    private void OnDisable()
    {
        SetDashVisual(false);
        SetDashEnemyCollisionIgnore(false);
    }

    public void RecibirDanio(float danio)
    {
        if (nivelPerdido || isDashing || Time.time < nextDamageTime)
        {
            return;
        }

        int corazonesAPerdidos = Mathf.CeilToInt(danio / danoPorCorazon);
        corazonesActuales -= corazonesAPerdidos;
        nextDamageTime = Time.time + tiempoInvulnerable;
        ActualizarCorazones();

        if (corazonesActuales > 0 && Animator != null)
        {
            Animator.SetTrigger("Damage");
        }

        if (corazonesActuales <= 0)
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
        // Desactivar control y asegurar que el jugador no siga actuando
        canMove = false;
        attackActive = false;

        if (Animator != null)
        {
            Animator.SetTrigger("Dead");
            // La transición a la pantalla de fin ocurrirá desde el evento de animación Dead_End
            return;
        }

        // Si no hay Animator, caemos a la ruta de respaldo y cargamos GameOver inmediatamente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("Player: no se encontro GameManager para cargar la escena de derrota.", this);
        }
    }

    // Este método será llamado por el evento de animación `Dead_End` al finalizar la animación de muerte
    public void Dead_End()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            Debug.LogError("Player: no se encontro GameManager para cargar la escena de derrota.", this);
        }
    }

    private void ActualizarCorazones()
    {
        if (corazones == null || corazones.Length == 0)
        {
            return;
        }

        corazonesActuales = Mathf.Max(corazonesActuales, 0);

        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] == null)
            {
                continue;
            }

            if (i < corazonesActuales)
            {
                corazones[i].sprite = corazonLleno;
            }
            else
            {
                corazones[i].sprite = corazonVacio;
            }
        }
    }

    public int GetCorazonesActuales()
    {
        return corazonesActuales;
    }

    public int GetCorazonesMaximos()
    {
        return corazonesMaximos;
    }

    private void EnsureHeartDisplay()
    {
        if (!autoCrearHUD)
        {
            return;
        }

        if (corazones != null && corazones.Length > 0)
        {
            return;
        }

        HeartDisplay display = FindFirstObjectByType<HeartDisplay>();
        if (display == null)
        {
            display = CreateHeartDisplay();
        }

        if (display == null)
        {
            return;
        }

        display.SetPlayer(this);
        display.SetSprites(corazonLleno, corazonVacio);
        display.SetLayout(hudCorazonSize, hudEspacioCorazones);
    }

    private HeartDisplay CreateHeartDisplay()
    {
        Canvas canvas = FindOrCreateHudCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject displayObject = new GameObject("HeartDisplay", typeof(RectTransform), typeof(HeartDisplay));
        displayObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = displayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = hudOffset;

        return displayObject.GetComponent<HeartDisplay>();
    }

    private Canvas FindOrCreateHudCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].name == "HUDCanvas")
            {
                return canvases[i];
            }
        }

        GameObject hud = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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
