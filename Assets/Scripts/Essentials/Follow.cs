using UnityEngine;

public class Follow : MonoBehaviour
{
    [SerializeField] float followSpeed = 15f;
    [SerializeField] float lookSpeed = 5f;
    [SerializeField] Transform target;
    [SerializeField] bool workWhenPause;

    void Update()
    {
        float dt = workWhenPause ? Time.unscaledDeltaTime : Time.deltaTime;

        if(followSpeed > 0) if(transform.position != target.position) transform.position = Vector3.Lerp(transform.position, target.position, followSpeed * dt);
        if(lookSpeed > 0) if(transform.rotation != target.rotation) transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lookSpeed * dt);
    }
}
