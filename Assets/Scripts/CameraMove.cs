using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public GameObject Target;
    private Vector3 TargetPos;
    public float haciaDelante = 0.8f;
    public float smoothing = 5f;
    [SerializeField] private string targetTag = "Player";

    private void EnsureTarget()
    {
        if (Target != null)
        {
            return;
        }

        if (Player.Instance != null)
        {
            Target = Player.Instance.gameObject;
            return;
        }

        Target = GameObject.FindWithTag(targetTag);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureTarget();

        if (smoothing <= 0f)
        {
            smoothing = 5f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        EnsureTarget();

        if (Target == null)
        {
            return;
        }

        TargetPos = new Vector3(Target.transform.position.x, Target.transform.position.y, transform.position.z);

        float yRotation = Mathf.Repeat(Target.transform.eulerAngles.y, 360f);
        bool lookingLeft = yRotation > 90f && yRotation < 270f;

        if (!lookingLeft) //derecha
        {
            TargetPos = new Vector3(TargetPos.x + haciaDelante, TargetPos.y, transform.position.z);
        }
        else //izquierda
        {
            TargetPos = new Vector3(TargetPos.x - haciaDelante, TargetPos.y, transform.position.z);
        }
        //Lerp(A, B, V) se mueve desde A hasta B a una velocidad V
        transform.position = Vector3.Lerp(transform.position, TargetPos, smoothing * Time.deltaTime);
    }
}
