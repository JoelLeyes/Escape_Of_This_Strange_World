using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public GameObject Target;
    private Vector3 TargetPos;
    public float haciaDelante;
    public float smoothing;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TargetPos = new Vector3(Target.transform.position.x, Target.transform.position.y, transform.position.z);
        if (Target.transform.localScale.x == 1) //derecha
        {
            TargetPos = new Vector3(TargetPos.x + haciaDelante, TargetPos.y, transform.position.z);
        }
        if (Target.transform.localScale.x == -1) //izquierda
        {
            TargetPos = new Vector3(TargetPos.x - haciaDelante, TargetPos.y, transform.position.z);
        }
        //Lerp(A, B, V) se mueve desde A hasta B a una velocidad V
        transform.position = Vector3.Lerp(transform.position, TargetPos, smoothing * Time.deltaTime);
    }
}
