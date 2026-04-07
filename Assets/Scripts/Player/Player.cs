using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float JumpForce;
    public float Speed;

    private Rigidbody2D Rigidbody2D;  //defino una variable global(puedo acceder de cualquier parte del script)
    private Animator Animator;
    private float Horizontal;
    private bool Grounded;

    private bool canMove = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody2D = GetComponent<Rigidbody2D>(); //esta funcion mete el componente Rigidbody dentro del script
        Animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //RAYOS
        if (Physics2D.Raycast(transform.position, Vector3.down, 0.28f) ||                           //rayo central
            Physics2D.Raycast(transform.position + Vector3.right * 0.15f, Vector3.down, 0.28f) ||   //rayo derecha
            Physics2D.Raycast(transform.position + Vector3.left * 0.15f, Vector3.down, 0.28f))      //rayo izquierda
        {
            Grounded = true;
            //Animator.SetBool("Jumping", false);
        }
        else {
            Grounded = false;
            //Animator.SetBool("Jumping", true);
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
            //if (Grounded)
                //Animator.SetBool("Running", Horizontal != 0.0f);
        }

        //SALTO
        if (IsJumpPressed() && Grounded) {
            Jump();
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
        Debug.DrawRay(transform.position + Vector3.right * 0.15f, Vector3.down * 0.28f, Color.red);
        Debug.DrawRay(transform.position + Vector3.left * 0.15f, Vector3.down * 0.28f, Color.red);
        Debug.DrawRay(transform.position, Vector3.down * 0.28f, Color.red);
    }
}
