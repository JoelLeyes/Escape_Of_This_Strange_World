using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

    private enum AttackType
    {
        None,
        Magic,
        Sword,
        Bow
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

    [Header("Vigor")]
    [SerializeField] private float vigorMaximo = 100f;
    [SerializeField] private float vigorActual = 100f;
    [SerializeField] private float costoEspada = 20f;
    [SerializeField] private float costoDash = 25f;
    [SerializeField] private float recargaVigorPorSegundo = 15f;
    [SerializeField] private float retrasoRecargaVigor = 0.35f;
    [SerializeField] private Vector2 hudVigorSize = new Vector2(190f, 18f);

    [Header("Mana")]
    [SerializeField] private float manaMaximo = 100f;
    [SerializeField] private float manaActual = 100f;
    [SerializeField] private float costoFuego = 25f;
    [SerializeField] private float recargaManaPorSegundo = 12f;
    [SerializeField] private float retrasoRecargaMana = 0.45f;
    [SerializeField] private Vector2 hudManaSize = new Vector2(190f, 18f);
    [SerializeField] private Sprite manaPotionSprite;

    [Header("Items UI")]
    [SerializeField] private Vector2 hudItemsOffset = new Vector2(20f, -80f);

    [Header("Suelo (Raycasts)")]
    [SerializeField] private float groundRayLength = 0.34f;
    [SerializeField] private float groundRayOffsetX = 0.15f;
    [SerializeField] private float groundRayStartOffsetFromColliderBottom = 0.02f;

    public float JumpForce;
    public float Speed;
    public float JumpCooldown = 0.1f;
    public float DashSpeed = 16f;
    public float DashDuration = 0.12f;
    public float DashCooldown = 0.6f;
    [SerializeField] private string dashAnimatorBool = "Dashing";
    [SerializeField] private string swordAnimatorTrigger = "Sword";
    [SerializeField] private Fire firePrefab;
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private PlayerAttackHitbox swordHitbox;
    [SerializeField] private AudioClip swordSwingClip;
    [SerializeField] private AudioClip fireAttackClip;
    [SerializeField] private AudioClip arrowAttackClip;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip runningClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip dashClip;

    private Rigidbody2D Rigidbody2D;  //defino una variable global(puedo acceder de cualquier parte del script)
    private Collider2D PlayerCollider;
    private BoxCollider2D bodyCollider;
    private Animator Animator;
    private AudioSource audioSource;
    private AudioSource runningAudioSource;
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
    private bool waitingForCheckpointLanding;
    private int corazonesActuales;
    private float nextDamageTime;
    private bool nivelPerdido;
    private const float danoPorCorazon = 20f;
    private Vector2 defaultBodyColliderSize;
    private Vector2 defaultBodyColliderOffset;
    private bool defaultBodyColliderEnabled;
    private bool defaultBodyColliderIsTrigger;
    private bool bodyColliderDefaultsCached;

    // Items
    private bool tieneArco;
    private bool tieneKeyBoss;
    private readonly HashSet<int> collectedKeyIds = new HashSet<int>();
    private int cantidadFlechas;
    private Sprite bowSprite;
    private Sprite arrowSprite;
    private Sprite keyBossSprite;
    private UI.ItemDisplay itemDisplay;
    private UI.VigorDisplay vigorDisplay;
    private UI.ManaDisplay manaDisplay;
    private float nextVigorRegenTime;
    private float nextManaRegenTime;

#if UNITY_EDITOR
    private const string SwordSwingClipPath = "Assets/Sound/SFX_swordSwing.wav";
    private const string FireClipPath = "Assets/Sound/SFX_flameShot1.wav";
    private const string ArrowClipPath = "Assets/Sound/Arrow.mp3";
    private const string DamageClipPath = "Assets/Sound/SFX_hit&damage3.wav";
    private const string RunningClipPath = "Assets/Sound/RuningStone.wav";
    private const string JumpClipPath = "Assets/Sound/jump.wav";
    private const string DashClipPath = "Assets/Sound/dash.wav";
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Teletransportar la instancia persistente a la posición de inicio de este nuevo nivel
            Instance.transform.position = transform.position;

            Rigidbody2D persistentRb = Instance.GetComponent<Rigidbody2D>();
            if (persistentRb != null)
            {
                persistentRb.position = transform.position;
                persistentRb.linearVelocity = Vector2.zero;
                persistentRb.angularVelocity = 0f;
            }
            Physics2D.SyncTransforms();

            SyncColliderSettingsToPersistentInstance();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void ForceEndActionLocks()
    {
        // Si una animación de ataque se interrumpe (por ejemplo por Damage), los eventos de animación
        // como `Attack_End` / `BoxAttack_AnimationEnd` pueden no ejecutarse y el player queda
        // trabado con `canMove=false`. Este método garantiza que siempre se recupera el control.
        if (swordHitbox != null)
        {
            swordHitbox.EndAttack();
        }

        attackActive = false;
        canMove = true;
        currentAttackType = AttackType.None;
    }

    private Vector3 GetGroundRayOrigin()
    {
        Collider2D col = PlayerCollider != null ? PlayerCollider : GetComponent<Collider2D>();
        if (col == null)
        {
            return transform.position;
        }

        Bounds b = col.bounds;
        float y = b.min.y + groundRayStartOffsetFromColliderBottom;
        return new Vector3(transform.position.x, y, transform.position.z);
    }

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
        if (other == null)
        {
            return;
        }

        if (other.CompareTag("Trap"))
        {
            RecibirDanio(corazonesMaximos * danoPorCorazon);
            return;
        }

        if (other.CompareTag("Item"))
        {
            PickupItem(other);
            return;
        }

        Key key = other.GetComponentInParent<Key>();
        if (key != null)
        {
            PickupKey(other);
            return;
        }
    }

    public void AjustarColliderMuerte()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        if (bodyCollider == null)
        {
            return;
        }

        // Cambia el tamaño del collider
        bodyCollider.size = new Vector2(0.5f, 0.2f);

        // Cambia la posición del collider
        bodyCollider.offset = Vector2.zero;
    }

    private void PickupItem(GameObject itemObj)
    {
        if (itemObj == null)
        {
            return;
        }

        // Try detect item type by component
        bool isArrow = itemObj.GetComponentInChildren<Arrow_Item>() != null || itemObj.name.ToLower().Contains("arrow");
        bool isBox = itemObj.GetComponentInChildren<Box>() != null || itemObj.name.ToLower().Contains("box");

        Sprite sprite = null;
        SpriteRenderer sr = itemObj.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sprite = sr.sprite;
        }

        EnsureItemDisplay();

        if (isBox)
        {
            // Grant bow
            tieneArco = true;
            bowSprite = sprite;
            if (itemDisplay != null)
            {
                itemDisplay.SetBowSprite(bowSprite);
            }
            Destroy(itemObj);
            return;
        }

        if (isArrow)
        {
            int arrowAmount = 10;
            Arrow_Item arrowItem = itemObj.GetComponentInChildren<Arrow_Item>();
            if (arrowItem != null && arrowItem.amount > 0)
            {
                arrowAmount = arrowItem.amount;
            }

            cantidadFlechas += arrowAmount;
            arrowSprite = sprite;
            if (itemDisplay != null)
            {
                if (itemDisplay.transform.childCount > 0)
                {
                    itemDisplay.SetArrowSprite(arrowSprite);
                }

                itemDisplay.SetArrowCount(cantidadFlechas);
            }

            Destroy(itemObj);
            return;
        }
    }

    private void PickupKey(GameObject keyObj)
    {
        if (keyObj == null)
        {
            return;
        }

        Key key = keyObj.GetComponent<Key>();
        if (key == null)
        {
            key = keyObj.GetComponentInParent<Key>();
        }

        if (key == null)
        {
            return;
        }

        CollectKey(key.id);
        Destroy(key.gameObject);
    }

    public void CollectKeyBoss(Sprite sprite)
    {
        tieneKeyBoss = true;
        keyBossSprite = sprite;

        if (itemDisplay != null)
        {
            itemDisplay.SetKeyBossSprite(keyBossSprite);
        }
    }

    public void CollectKey(int keyId)
    {
        if (keyId < 0)
        {
            return;
        }

        collectedKeyIds.Add(keyId);
    }

    public bool HasKey(int keyId)
    {
        return collectedKeyIds.Contains(keyId);
    }

    public bool HasKeyBoss()
    {
        return tieneKeyBoss;
    }

    public float GetVigorCurrent()
    {
        return vigorActual;
    }

    public float GetVigorMax()
    {
        return vigorMaximo;
    }

    public float GetManaCurrent()
    {
        return manaActual;
    }

    public float GetManaMax()
    {
        return manaMaximo;
    }

    private bool TrySpendVigor(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (vigorActual < amount)
        {
            return false;
        }

        vigorActual = Mathf.Max(0f, vigorActual - amount);
        nextVigorRegenTime = Time.time + retrasoRecargaVigor;
        RefreshVigorDisplay();
        return true;
    }

    private void RegenerateVigor()
    {
        if (vigorActual >= vigorMaximo)
        {
            return;
        }

        if (Time.time < nextVigorRegenTime)
        {
            return;
        }

        vigorActual = Mathf.Min(vigorMaximo, vigorActual + recargaVigorPorSegundo * Time.deltaTime);
        RefreshVigorDisplay();
    }

    private bool TrySpendMana(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (manaActual < amount)
        {
            return false;
        }

        manaActual = Mathf.Max(0f, manaActual - amount);
        nextManaRegenTime = Time.time + retrasoRecargaMana;
        RefreshManaDisplay();
        return true;
    }

    private void RegenerateMana()
    {
        if (manaActual >= manaMaximo)
        {
            return;
        }

        if (Time.time < nextManaRegenTime)
        {
            return;
        }

        manaActual = Mathf.Min(manaMaximo, manaActual + recargaManaPorSegundo * Time.deltaTime);
        RefreshManaDisplay();
    }

    void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>(); //esta funcion mete el componente Rigidbody dentro del script
        PlayerCollider = GetComponent<Collider2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        CacheBodyColliderDefaults();
        Animator = GetComponent<Animator>();
        if (swordHitbox == null)
        {
            swordHitbox = GetComponentInChildren<PlayerAttackHitbox>(true);
        }

        EnsureAudioSources();
        AutoAssignAudioClips();

        corazonesActuales = corazonesMaximos;
        vigorActual = Mathf.Clamp(vigorActual, 0f, vigorMaximo);
        manaActual = Mathf.Clamp(manaActual, 0f, manaMaximo);
        ActualizarCorazones();
        
        string currentScene = SceneManager.GetActiveScene().name;
        if (GameManager.Instance == null || !GameManager.Instance.IsMenuOrGameOverScene(currentScene))
        {
            EnsureHeartDisplay();
            EnsureVigorDisplay();
            EnsureManaDisplay();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyCheckpoint(transform, Rigidbody2D);
        }

        if (Animator == null)
        {
            Debug.LogWarning("No Animator found on Player. Animation states will be skipped.", this);
        }

        RefreshVigorDisplay();
        RefreshManaDisplay();
    }

    private void CacheBodyColliderDefaults()
    {
        if (bodyCollider == null || bodyColliderDefaultsCached)
        {
            return;
        }

        defaultBodyColliderSize = bodyCollider.size;
        defaultBodyColliderOffset = bodyCollider.offset;
        defaultBodyColliderEnabled = bodyCollider.enabled;
        defaultBodyColliderIsTrigger = bodyCollider.isTrigger;
        bodyColliderDefaultsCached = true;
    }

    private void RestoreBodyCollider()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        if (bodyCollider == null || !bodyColliderDefaultsCached)
        {
            return;
        }

        bodyCollider.size = defaultBodyColliderSize;
        bodyCollider.offset = defaultBodyColliderOffset;
        bodyCollider.enabled = defaultBodyColliderEnabled;
        bodyCollider.isTrigger = defaultBodyColliderIsTrigger;
    }

    private void SyncColliderSettingsToPersistentInstance()
    {
        if (Instance == null || Instance == this)
        {
            return;
        }

        Transform targetTransform = Instance.transform;
        targetTransform.SetPositionAndRotation(transform.position, transform.rotation);
        targetTransform.localScale = transform.localScale;

        Rigidbody2D sourceRigidbody = GetComponent<Rigidbody2D>();
        Rigidbody2D targetRigidbody = Instance.GetComponent<Rigidbody2D>();

        if (sourceRigidbody != null && targetRigidbody != null)
        {
            targetRigidbody.position = sourceRigidbody.position;
            targetRigidbody.linearVelocity = sourceRigidbody.linearVelocity;
            targetRigidbody.angularVelocity = sourceRigidbody.angularVelocity;
        }

        BoxCollider2D sourceCollider = GetComponent<BoxCollider2D>();
        BoxCollider2D targetCollider = Instance.GetComponent<BoxCollider2D>();

        if (sourceCollider == null || targetCollider == null)
        {
            return;
        }

        targetCollider.size = sourceCollider.size;
        targetCollider.offset = sourceCollider.offset;
        targetCollider.enabled = sourceCollider.enabled;
        targetCollider.isTrigger = sourceCollider.isTrigger;
        targetCollider.sharedMaterial = sourceCollider.sharedMaterial;

        Instance.bodyCollider = targetCollider;
        Instance.defaultBodyColliderSize = sourceCollider.size;
        Instance.defaultBodyColliderOffset = sourceCollider.offset;
        Instance.defaultBodyColliderEnabled = sourceCollider.enabled;
        Instance.defaultBodyColliderIsTrigger = sourceCollider.isTrigger;
        Instance.bodyColliderDefaultsCached = true;

        Physics2D.SyncTransforms();
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length > 0)
        {
            audioSource = sources[0];
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        sources = GetComponents<AudioSource>();
        if (sources.Length > 1)
        {
            runningAudioSource = sources[1];
        }

        if (runningAudioSource == null)
        {
            runningAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource(audioSource);
        ConfigureAudioSource(runningAudioSource);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
    }

    private void AutoAssignAudioClips()
    {
#if UNITY_EDITOR
        if (swordSwingClip == null)
        {
            swordSwingClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SwordSwingClipPath);
        }

        if (fireAttackClip == null)
        {
            fireAttackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(FireClipPath);
        }

        if (arrowAttackClip == null)
        {
            arrowAttackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ArrowClipPath);
        }

        if (damageClip == null)
        {
            damageClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DamageClipPath);
        }

        if (runningClip == null)
        {
            runningClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RunningClipPath);
        }

        if (jumpClip == null)
        {
            jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>(JumpClipPath);
        }

        if (dashClip == null)
        {
            dashClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DashClipPath);
        }
#endif
    }

    private void PlaySwordSwingSound()
    {
        if (audioSource == null || swordSwingClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(swordSwingClip);
    }

    private void PlayFireAttackSound()
    {
        if (audioSource == null || fireAttackClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(fireAttackClip);
    }

    private void PlayArrowAttackSound()
    {
        if (audioSource == null || arrowAttackClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(arrowAttackClip);
    }

    private void PlayDamageSound()
    {
        if (audioSource == null || damageClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(damageClip);
    }

    private void PlayJumpSound()
    {
        if (audioSource == null || jumpClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(jumpClip);
    }

    private void PlayDashSound()
    {
        if (audioSource == null || dashClip == null)
        {
            return;
        }

        audioSource.PlayOneShot(dashClip);
    }

    private void UpdateRunningSound()
    {
        if (runningAudioSource == null || runningClip == null)
        {
            return;
        }

        bool shouldPlay = canMove && Grounded && !isDashing && !attackActive && Mathf.Abs(Horizontal) > 0f;

        if (shouldPlay)
        {
            if (!runningAudioSource.isPlaying || runningAudioSource.clip != runningClip)
            {
                runningAudioSource.clip = runningClip;
                runningAudioSource.loop = true;
                runningAudioSource.Play();
            }

            return;
        }

        if (runningAudioSource.isPlaying && runningAudioSource.clip == runningClip)
        {
            runningAudioSource.Stop();
        }
    }

    private void StopRunningSound()
    {
        if (runningAudioSource == null || runningClip == null)
        {
            return;
        }

        if (runningAudioSource.isPlaying && runningAudioSource.clip == runningClip)
        {
            runningAudioSource.Stop();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // No procesar input si el juego está pausado
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused())
        {
            StopRunningSound();
            return;
        }

        if (isDashing && Time.time >= dashEndTime)
        {
            StopDash();
        }

        RegenerateVigor();
        RegenerateMana();

        //RAYOS
        // Un poco más largos para detectar el suelo más consistente
        // y con origen en los pies (abajo del collider) para evitar que salgan desde la panza.
        Vector3 rayOrigin = GetGroundRayOrigin();
        RaycastHit2D centerHit = Physics2D.Raycast(rayOrigin, Vector3.down, groundRayLength); //rayo central
        RaycastHit2D rightHit = Physics2D.Raycast(rayOrigin + Vector3.right * groundRayOffsetX, Vector3.down, groundRayLength); //rayo derecha
        RaycastHit2D leftHit = Physics2D.Raycast(rayOrigin + Vector3.left * groundRayOffsetX, Vector3.down, groundRayLength); //rayo izquierda

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

        if (waitingForCheckpointLanding)
        {
            Animator.SetBool("Running", false);

            if (!Grounded)
            {
                Horizontal = 0f;
                return;
            }

            waitingForCheckpointLanding = false;
            canMove = true;
            Animator.SetBool("Jumping", false);
        }

        if (isDashing)
        {
            Horizontal = 0f;
            if (Animator != null)
            {
                Animator.SetBool("Running", false);
            }

            StopRunningSound();
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
            if (!TrySpendVigor(costoDash))
            {
                return;
            }

            dashDirection = Horizontal != 0f ? (Horizontal > 0f ? 1 : -1) : (transform.right.x >= 0f ? 1 : -1);
            isDashing = true;
            dashEndTime = Time.time + DashDuration;
            nextDashTime = Time.time + DashCooldown;
            SetDashVisual(true);
            SetDashEnemyCollisionIgnore(true);
            PlayDashSound();
        }

        //SALTO
        if (canMove && IsJumpPressed() && Grounded && Time.time >= nextJumpTime) {
            Jump();
            nextJumpTime = Time.time + JumpCooldown;
        }

        SwordAttack();
        MagicAttack();
        BowAttack();

        UpdateRunningSound();
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

        if (firePrefab == null)
        {
            Debug.LogError("Fire prefab is not assigned in Player Inspector.", this);
            return;
        }

        if (!TrySpendMana(costoFuego))
        {
            return;
        }

        canMove = false;
        attackActive = true;
        currentAttackType = AttackType.Magic;
        Animator.SetTrigger("Magic");
        PlayFireAttackSound();

        int direction = transform.right.x >= 0f ? 1 : -1;
        Fire fire = Instantiate(firePrefab, transform.position, Quaternion.identity);
        fire.Initialize(direction);
    }

    private void BowAttack()
    {
        if (Keyboard.current == null
            || !Keyboard.current.lKey.wasPressedThisFrame
            || !Grounded
            || Animator == null
            || attackActive
            || !canMove)
        {
            return;
        }

        // Only allow bow attack if player actually has the bow and at least one arrow
        if (!tieneArco || cantidadFlechas <= 0)
        {
            return;
        }

        canMove = false;
        attackActive = true;
        currentAttackType = AttackType.Bow;
        Animator.SetTrigger("BoxAttack");
    }

    // Called by the animation event `BoxAttack_AnimationEnd`
    public void BoxAttack_AnimationEnd()
    {
        // Ensure player actually has bow and arrows before spawning
        if (!tieneArco || cantidadFlechas <= 0)
        {
            // End attack state without firing
            ForceEndActionLocks();
            return;
        }

        if (arrowPrefab == null)
        {
            Debug.LogError("Arrow prefab is not assigned in Player Inspector.", this);
        }
        else
        {
            int direction = transform.right.x >= 0f ? 1 : -1;
            Arrow arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            arrow.Initialize(direction);
            PlayArrowAttackSound();

            cantidadFlechas = Mathf.Max(0, cantidadFlechas - 1);
            if (itemDisplay != null)
            {
                itemDisplay.SetArrowCount(cantidadFlechas);
            }
        }

        // End attack state
        ForceEndActionLocks();
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

        if (!TrySpendVigor(costoEspada))
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
            PlaySwordSwingSound();
            swordHitbox.BeginAttack();
        }
    }

    public void Attack_End()
    {
        ForceEndActionLocks();
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
        PlayJumpSound();
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

            Enemy2[] enemies2 = FindObjectsByType<Enemy2>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies2.Length; i++)
            {
                if (enemies2[i] == null)
                {
                    continue;
                }

                Collider2D[] enemyColliders = enemies2[i].GetComponentsInChildren<Collider2D>(true);
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

            ArmSkeleton[] armSkeletons = FindObjectsByType<ArmSkeleton>(FindObjectsSortMode.None);
            for (int i = 0; i < armSkeletons.Length; i++)
            {
                if (armSkeletons[i] == null)
                {
                    continue;
                }

                Collider2D[] armColliders = armSkeletons[i].GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < armColliders.Length; j++)
                {
                    Collider2D armCollider = armColliders[j];
                    if (armCollider == null || armCollider == PlayerCollider)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(PlayerCollider, armCollider, true);
                    ignoredEnemyColliders.Add(armCollider);
                }
            }

            FireBossProjectile[] fireBossProjectiles = FindObjectsByType<FireBossProjectile>(FindObjectsSortMode.None);
            for (int i = 0; i < fireBossProjectiles.Length; i++)
            {
                if (fireBossProjectiles[i] == null)
                {
                    continue;
                }

                Collider2D[] projectileColliders = fireBossProjectiles[i].GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < projectileColliders.Length; j++)
                {
                    Collider2D projectileCollider = projectileColliders[j];
                    if (projectileCollider == null || projectileCollider == PlayerCollider)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(PlayerCollider, projectileCollider, true);
                    ignoredEnemyColliders.Add(projectileCollider);
                }
            }

            Boss1[] boss1s = FindObjectsByType<Boss1>(FindObjectsSortMode.None);
            for (int i = 0; i < boss1s.Length; i++)
            {
                if (boss1s[i] == null)
                {
                    continue;
                }

                Collider2D[] bossColliders = boss1s[i].GetComponentsInChildren<Collider2D>(true);
                for (int j = 0; j < bossColliders.Length; j++)
                {
                    Collider2D bossCollider = bossColliders[j];
                    if (bossCollider == null || bossCollider == PlayerCollider)
                    {
                        continue;
                    }

                    Physics2D.IgnoreCollision(PlayerCollider, bossCollider, true);
                    ignoredEnemyColliders.Add(bossCollider);
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
        StopRunningSound();
    }

    private void OnDestroy()
    {
        StopRunningSound();

        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this == null || mode == LoadSceneMode.Additive)
        {
            return;
        }

        corazones = null;
        itemDisplay = null;
        vigorDisplay = null;
        manaDisplay = null;

        if (GameManager.Instance != null && GameManager.Instance.IsMenuOrGameOverScene(scene.name))
        {
            return;
        }

        EnsureHeartDisplay();
        EnsureItemDisplay();
        EnsureVigorDisplay();
        EnsureManaDisplay();
        RefreshItemDisplay();
        RefreshVigorDisplay();
        RefreshManaDisplay();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyCheckpoint(transform, Rigidbody2D);
        }
    }

    public void ReviveFromCheckpoint()
    {
        nivelPerdido = false;
        nextDamageTime = 0f;
        canMove = false;
        waitingForCheckpointLanding = true;
        attackActive = false;
        currentAttackType = AttackType.None;
        isDashing = false;
        dashEndTime = 0f;
        nextDashTime = 0f;
        Horizontal = 0f;
        Grounded = false;

        RestoreBodyCollider();

        if (swordHitbox != null)
        {
            swordHitbox.EndAttack();
        }

        SetDashVisual(false);
        SetDashEnemyCollisionIgnore(false);
        StopRunningSound();

        if (Rigidbody2D != null)
        {
            Rigidbody2D.linearVelocity = Vector2.zero;
            Rigidbody2D.angularVelocity = 0f;
        }

        if (Animator != null)
        {
            Animator.Rebind();
            Animator.Update(0f);
            Animator.SetBool("Running", false);
            Animator.SetBool("Jumping", false);
        }

        corazonesActuales = corazonesMaximos;
        vigorActual = vigorMaximo;
        manaActual = manaMaximo;
        ActualizarCorazones();
        RefreshVigorDisplay();
        RefreshManaDisplay();
        RefreshItemDisplay();
    }

    public void RecibirDanio(float danio)
    {
        if (nivelPerdido || isDashing || Time.time < nextDamageTime)
        {
            return;
        }

        // Si el daño interrumpe un ataque, el evento de fin de ataque puede no ejecutarse.
        // Cancelamos locks aquí para evitar que el player quede inmóvil para siempre.
        if (attackActive || !canMove)
        {
            ForceEndActionLocks();
        }

        int corazonesAPerdidos = Mathf.CeilToInt(danio / danoPorCorazon);
        corazonesActuales -= corazonesAPerdidos;
        nextDamageTime = Time.time + tiempoInvulnerable;
        PlayDamageSound();
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

        EnsureItemDisplay();
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

    private void EnsureItemDisplay()
    {
        if (!autoCrearHUD)
        {
            return;
        }

        if (itemDisplay != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();
        if (canvas == null)
        {
            return;
        }

        GameObject displayObject = new GameObject("ItemDisplay", typeof(RectTransform), typeof(UI.ItemDisplay));
        displayObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = displayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = hudItemsOffset;

        itemDisplay = displayObject.GetComponent<UI.ItemDisplay>();
    }

    private void EnsureVigorDisplay()
    {
        if (!autoCrearHUD)
        {
            return;
        }

        if (vigorDisplay != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();
        if (canvas == null)
        {
            return;
        }

        GameObject displayObject = new GameObject("VigorDisplay", typeof(RectTransform), typeof(UI.VigorDisplay));
        displayObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = displayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(hudOffset.x + 430f, hudOffset.y - 8f);

        vigorDisplay = displayObject.GetComponent<UI.VigorDisplay>();
        if (vigorDisplay != null)
        {
            vigorDisplay.SetPlayer(this);
            vigorDisplay.SetLayout(hudVigorSize);
        }
    }

    private void RefreshVigorDisplay()
    {
        if (vigorDisplay == null)
        {
            return;
        }

        vigorDisplay.SetPlayer(this);
    }

    private void EnsureManaDisplay()
    {
        if (!autoCrearHUD)
        {
            return;
        }

        if (manaDisplay != null)
        {
            return;
        }

        Canvas canvas = FindOrCreateHudCanvas();
        if (canvas == null)
        {
            return;
        }

        GameObject displayObject = new GameObject("ManaDisplay", typeof(RectTransform), typeof(UI.ManaDisplay));
        displayObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = displayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(hudOffset.x + 705f, hudOffset.y - 8f);

        manaDisplay = displayObject.GetComponent<UI.ManaDisplay>();
        if (manaDisplay != null)
        {
            manaDisplay.SetPlayer(this);
            manaDisplay.SetLayout(hudManaSize);
            manaDisplay.SetIconSprite(manaPotionSprite);
        }
    }

    private void RefreshManaDisplay()
    {
        if (manaDisplay == null)
        {
            return;
        }

        manaDisplay.SetPlayer(this);
        manaDisplay.SetIconSprite(manaPotionSprite);
    }

    private void RefreshItemDisplay()
    {
        if (itemDisplay == null)
        {
            return;
        }

        itemDisplay.SetBowSprite(tieneArco ? bowSprite : null);
        itemDisplay.SetArrowSprite(cantidadFlechas > 0 ? arrowSprite : null);
        itemDisplay.SetArrowCount(cantidadFlechas);
        itemDisplay.SetKeyBossSprite(tieneKeyBoss ? keyBossSprite : null);
    }

    public static void ResetPersistentInstance()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
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
        Vector3 rayOrigin = GetGroundRayOrigin();
        Debug.DrawRay(rayOrigin + Vector3.right * groundRayOffsetX, Vector3.down * groundRayLength, Color.red);
        Debug.DrawRay(rayOrigin + Vector3.left * groundRayOffsetX, Vector3.down * groundRayLength, Color.red);
        Debug.DrawRay(rayOrigin, Vector3.down * groundRayLength, Color.red);
    }
}
