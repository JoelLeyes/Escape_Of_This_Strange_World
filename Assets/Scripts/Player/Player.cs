using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float JumpForce;
    public float Speed;
    public float JumpCooldown = 0.1f;
    [SerializeField] private Fire firePrefab;

    private Rigidbody2D Rigidbody2D;  //defino una variable global(puedo acceder de cualquier parte del script)
    private Collider2D PlayerCollider;
    private Animator Animator;
    private float Horizontal;
    private bool Grounded;
    private float nextJumpTime;
    private float nextAttackTime;

    private bool canMove = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>(); //esta funcion mete el componente Rigidbody dentro del script
        PlayerCollider = GetComponent<Collider2D>();
        Animator = GetComponent<Animator>();

        if (Animator == null)
        {
            Debug.LogWarning("No Animator found on Player. Animation states will be skipped.", this);
        }
    }

    // Update is called once per frame
    void Update()
    {
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

        //SALTO
        if (IsJumpPressed() && Grounded && Time.time >= nextJumpTime) {
            Jump();
            nextJumpTime = Time.time + JumpCooldown;
        }

        MagicAttack();
        // Controlar el movimiento según la animación mágica
        if (Animator != null)
        {
            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Magic_Attack"))
            {
                canMove = false;
            }
            else if (!canMove)
            {
                canMove = true;
            }
        }
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

    private void MagicAttack()
    {
        if (Keyboard.current == null
            || !Keyboard.current.qKey.wasPressedThisFrame
            || !Grounded
            || Animator == null
            || !Cooldown(0.35f))
        {
            return;
        }

        canMove = false;
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

    private void FixedUpdate()
    /*el fixed update se actualiza mucho mas rapido que el update y esto es necesario para las fisicas
    es una funcion incorporada en Unity que se llama automaticamente en intervalos de tiempo fijos
    para manejar operaciones relacionadas con la fisica del juego.*/
    {
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
