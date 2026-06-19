using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Boss1 : MonoBehaviour
{
    [Header("Vida")]
    public float vida = 200f;
    [SerializeField] private string damageTrigger = "hurt";
    [SerializeField] private Vector2 healthBarOffset = new Vector2(0f, 1.2f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(1.6f, 0.18f);
    [SerializeField] private float healthBarBorderPadding = 0.04f;
    [SerializeField] private Color healthBarBorderColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private Color healthBarBackgroundColor = new Color(0.12f, 0.05f, 0.05f, 0.9f);
    [SerializeField] private Color healthBarFillColor = new Color(0.55f, 0.05f, 0.05f, 1f);
    [SerializeField] private string bossName = "NecroMancer";
    [SerializeField] private Color bossNameColor = new Color(0.85f, 0.1f, 0.1f, 1f);
    [SerializeField] private Vector2 bossNameOffset = new Vector2(0f, 0.40f);
    [SerializeField] private int bossNameFontSize = 64;
    [SerializeField] private float bossNameCharacterSize = 0.08f;
    [SerializeField] private int healthBarSortingOrder = 10;
    [SerializeField] private string healthBarSortingLayer = "Default";
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip levitatingClip;

    [Header("Movimiento")]
    [SerializeField] private Transform[] points;
    [SerializeField] private string pointsTag = "BossPoint";
    [SerializeField] private float speed = 2f;
    [SerializeField] private float arrivalDistance = 0.1f;
    [SerializeField] private bool pickDifferentPoint = true;
    [SerializeField] private float waitAtPointSeconds = 10f;

    [Header("Ataque Magico")]
    [SerializeField, Range(0f, 1f)] private float magicChance = 0.4f;
    [SerializeField] private float magicMinDelay = 0f;
    [SerializeField] private float magicMaxDelay = 10f;
    [SerializeField] private GameObject fireBossPrefab;
    [SerializeField] private Vector2 fireBossSpawnOffset = Vector2.zero;
    [SerializeField] private float fireBossSpeed = 3f;
    [SerializeField] private float fireBossLifeTime = 3f;

    [Header("Angulos FireBoss")]
    [SerializeField] private float fireBossAngle0 = 0f;
    [SerializeField] private float fireBossAngle1 = 45f;
    [SerializeField] private float fireBossAngle2 = 90f;
    [SerializeField] private float fireBossAngle3 = 135f;
    [SerializeField] private float fireBossAngle4 = 180f;

    private Animator animator;
    private Transform currentTarget;
    private int currentIndex = -1;
    private bool isWaiting;
    private float waitEndTime;
    private bool magicScheduled;
    private float magicTriggerTime;
    private float vidaMax;
    private Transform healthBarRoot;
    private Transform healthBarFill;
    private static Sprite sharedBarSprite;
    private AudioSource damageAudioSource;

#if UNITY_EDITOR
    private const string DamageClipPath = "Assets/Sound/SFX_hit&damageEsqueleto13.wav";
    private const string LevitatingClipPath = "Assets/Sound/levitando.wav";
#endif

    private void Awake()
    {
        EnsureDamageAudioSource();
        AutoAssignDamageClip();
        AutoAssignLevitatingClip();
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        vidaMax = Mathf.Max(vida, 1f);
        CreateHealthBar();
        UpdateHealthBar();
        EnsurePoints();
        PickNextTarget();
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

    private void AutoAssignLevitatingClip()
    {
#if UNITY_EDITOR
        if (levitatingClip == null)
        {
            levitatingClip = AssetDatabase.LoadAssetAtPath<AudioClip>(LevitatingClipPath);
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

    private void PlayMovementSound()
    {
        if (damageAudioSource == null || levitatingClip == null)
        {
            return;
        }

        if (!damageAudioSource.isPlaying || damageAudioSource.clip != levitatingClip)
        {
            damageAudioSource.clip = levitatingClip;
            damageAudioSource.loop = true;
            damageAudioSource.Play();
        }
    }

    private void StopMovementSound()
    {
        if (damageAudioSource == null || levitatingClip == null)
        {
            return;
        }

        if (damageAudioSource.isPlaying && damageAudioSource.clip == levitatingClip)
        {
            damageAudioSource.Stop();
        }

        if (damageAudioSource.clip == levitatingClip)
        {
            damageAudioSource.loop = false;
        }
    }

    private void Update()
    {
        if (vida <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (isWaiting)
        {
            if (magicScheduled && Time.time >= magicTriggerTime)
            {
                magicScheduled = false;
                if (Random.value <= magicChance)
                {
                    TriggerMagic();
                }
                ScheduleNextMagic();
            }

            if (Time.time >= waitEndTime)
            {
                isWaiting = false;
                PickNextTarget();
            }
            return;
        }

        if (currentTarget == null)
        {
            if (points == null || points.Length == 0)
            {
                return;
            }

            PickNextTarget();
            return;
        }

        Vector3 pos = transform.position;
        Vector3 targetPos = currentTarget.position;
        Vector2 nextPos = Vector2.MoveTowards(new Vector2(pos.x, pos.y), new Vector2(targetPos.x, targetPos.y), speed * Time.deltaTime);
        transform.position = new Vector3(nextPos.x, nextPos.y, pos.z);

        float remaining = Vector2.Distance(new Vector2(nextPos.x, nextPos.y), new Vector2(targetPos.x, targetPos.y));
        if (remaining <= arrivalDistance)
        {
            StartWaitAtPoint();
        }
    }

    private void StartWaitAtPoint()
    {
        if (waitAtPointSeconds <= 0f)
        {
            PickNextTarget();
            return;
        }

        isWaiting = true;
        waitEndTime = Time.time + waitAtPointSeconds;
        StopMovementSound();
        TriggerIdle();
        ScheduleNextMagic();
    }

    private void ScheduleNextMagic()
    {
        magicScheduled = false;

        if (waitAtPointSeconds <= 0f || magicChance <= 0f)
        {
            return;
        }

        float remaining = waitEndTime - Time.time;
        if (remaining <= 0f)
        {
            return;
        }

        float maxDelay = Mathf.Max(magicMinDelay, magicMaxDelay);
        maxDelay = Mathf.Min(remaining, maxDelay);
        float minDelay = Mathf.Clamp(magicMinDelay, 0f, maxDelay);

        float delay = maxDelay <= 0f ? 0f : Random.Range(minDelay, maxDelay);
        magicTriggerTime = Time.time + delay;
        magicScheduled = true;
    }

    private void EnsurePoints()
    {
        if (points != null && points.Length > 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(pointsTag))
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(pointsTag);
            if (tagged != null && tagged.Length > 0)
            {
                points = new Transform[tagged.Length];
                for (int i = 0; i < tagged.Length; i++)
                {
                    points[i] = tagged[i] != null ? tagged[i].transform : null;
                }
                return;
            }
        }

        Transform[] named = new Transform[3];
        for (int i = 0; i < named.Length; i++)
        {
            GameObject go = GameObject.Find("Point" + (i + 1));
            named[i] = go != null ? go.transform : null;
        }

        points = named;
    }

    private void PickNextTarget()
    {
        if (points == null || points.Length == 0)
        {
            currentTarget = null;
            return;
        }

        int nextIndex = Random.Range(0, points.Length);
        if (pickDifferentPoint && points.Length > 1)
        {
            int safety = 0;
            while (nextIndex == currentIndex && safety < 10)
            {
                nextIndex = Random.Range(0, points.Length);
                safety++;
            }
        }

        currentIndex = nextIndex;
        currentTarget = points[currentIndex];
        TriggerMove();
    }

    private void TriggerMove()
    {
        if (animator != null)
        {
            animator.SetTrigger("move");
        }

        PlayMovementSound();
    }

    private void TriggerMagic()
    {
        if (animator != null)
        {
            animator.SetTrigger("magic");
        }
    }

    private void TriggerIdle()
    {
        if (animator != null)
        {
            animator.SetTrigger("idle");
        }
    }

    public void RecibirDanio(float danio)
    {
        vida = Mathf.Max(vida - danio, 0f);
        UpdateHealthBar();
        PlayDamageSound();

        if (vida <= 0f)
        {
            StopMovementSound();
            Destroy(gameObject);
            return;
        }

        if (animator != null && !string.IsNullOrEmpty(damageTrigger))
        {
            animator.SetTrigger(damageTrigger);
        }
    }

    private void CreateHealthBar()
    {
        if (healthBarRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("BossHealthBar");
        root.transform.SetParent(transform);
        root.transform.localPosition = healthBarOffset;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        healthBarRoot = root.transform;

        GameObject border = new GameObject("Border");
        border.transform.SetParent(healthBarRoot, false);
        SpriteRenderer borderRenderer = border.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = GetBarSprite();
        borderRenderer.color = healthBarBorderColor;
        borderRenderer.sortingOrder = healthBarSortingOrder;
        if (!string.IsNullOrEmpty(healthBarSortingLayer))
        {
            borderRenderer.sortingLayerName = healthBarSortingLayer;
        }
        border.transform.localScale = new Vector3(healthBarSize.x + healthBarBorderPadding * 2f,
            healthBarSize.y + healthBarBorderPadding * 2f, 1f);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(healthBarRoot, false);
        SpriteRenderer bgRenderer = bg.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = GetBarSprite();
        bgRenderer.color = healthBarBackgroundColor;
        bgRenderer.sortingOrder = healthBarSortingOrder + 1;
        if (!string.IsNullOrEmpty(healthBarSortingLayer))
        {
            bgRenderer.sortingLayerName = healthBarSortingLayer;
        }
        bg.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y, 1f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(healthBarRoot, false);
        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = GetBarSprite();
        fillRenderer.color = healthBarFillColor;
        fillRenderer.sortingOrder = healthBarSortingOrder + 2;
        if (!string.IsNullOrEmpty(healthBarSortingLayer))
        {
            fillRenderer.sortingLayerName = healthBarSortingLayer;
        }

        healthBarFill = fill.transform;

        GameObject nameObject = new GameObject("NameLabel");
        nameObject.transform.SetParent(healthBarRoot, false);
        TextMesh nameText = nameObject.AddComponent<TextMesh>();
        nameText.text = bossName;
        nameText.color = bossNameColor;
        nameText.anchor = TextAnchor.MiddleCenter;
        nameText.alignment = TextAlignment.Center;
        nameText.fontSize = bossNameFontSize;
        nameText.characterSize = bossNameCharacterSize;

        MeshRenderer nameRenderer = nameObject.GetComponent<MeshRenderer>();
        if (nameRenderer != null)
        {
            nameRenderer.sortingOrder = healthBarSortingOrder + 3;
            if (!string.IsNullOrEmpty(healthBarSortingLayer))
            {
                nameRenderer.sortingLayerName = healthBarSortingLayer;
            }
        }

        nameObject.transform.localPosition = new Vector3(bossNameOffset.x, bossNameOffset.y, -0.02f);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null)
        {
            return;
        }

        float percent = vidaMax <= 0f ? 0f : Mathf.Clamp01(vida / vidaMax);
        float width = healthBarSize.x * percent;
        healthBarFill.localScale = new Vector3(width, healthBarSize.y, 1f);

        float xOffset = -(healthBarSize.x - width) * 0.5f;
        healthBarFill.localPosition = new Vector3(xOffset, 0f, -0.01f);
    }

    private static Sprite GetBarSprite()
    {
        if (sharedBarSprite != null)
        {
            return sharedBarSprite;
        }

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        sharedBarSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sharedBarSprite.name = "BossHealthBarSprite";
        return sharedBarSprite;
    }

    public void Boss_Magic5_AnimationEnd()
    {
        SpawnFireBossAtAngle(fireBossAngle0);
        SpawnFireBossAtAngle(fireBossAngle1);
        SpawnFireBossAtAngle(fireBossAngle2);
        SpawnFireBossAtAngle(fireBossAngle3);
        SpawnFireBossAtAngle(fireBossAngle4);
    }

    private void SpawnFireBossAtAngle(float angleDeg)
    {
        if (fireBossPrefab == null)
        {
            Debug.LogWarning("Boss1: FireBoss prefab no asignado.", this);
            return;
        }

        Vector3 spawnPos = transform.position + (Vector3)fireBossSpawnOffset;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angleDeg);
        GameObject instance = Instantiate(fireBossPrefab, spawnPos, rotation);

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector2.right;
        }

        FireBossProjectile projectile = instance.GetComponent<FireBossProjectile>();
        if (projectile == null)
        {
            projectile = instance.AddComponent<FireBossProjectile>();
        }

        projectile.Initialize(dir, fireBossSpeed, fireBossLifeTime);
    }
}
