using UnityEngine;

public class LookAtCam : MonoBehaviour
{
    [SerializeField] float lookSpeed = 5f;
    [SerializeField] Transform target;

    Transform cam;

    void Start()
    {
        if (cam == null) 
            cam = GameObject.FindWithTag("MainCamera").transform;
    }

    void Update()
    {
        Vector3 direction = cam.position - target.position;
        direction.y = 0; 

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            target.rotation = Quaternion.Slerp(target.rotation, targetRotation, Time.deltaTime * lookSpeed);
        }
    }
}
