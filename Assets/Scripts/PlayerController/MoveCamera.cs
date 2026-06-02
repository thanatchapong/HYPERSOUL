using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MoveCamera : MonoBehaviour
{
    [SerializeField] Transform camPos;

    [SerializeField] bool rotate = false;

    [SerializeField] Transform lookAt;

    float x;
    float y;
    float z;

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, camPos.position, Time.deltaTime * 20);
        
        if(rotate == true)
        {
            // Vector3 targetRotation = lookAt.position - transform.position;
            // transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetRotation), 30f * Time.deltaTime);

            // transform.rotation = Quaternion.lerp(transform.rotation, camPos.rotation, Time.deltaTime * 20);
            // transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetRotation), Time.deltaTime * 20);

            transform.rotation = Quaternion.Euler(Mathf.SmoothDampAngle(transform.eulerAngles.x, camPos.eulerAngles.x, ref x, 0.1f), Mathf.SmoothDampAngle(transform.eulerAngles.y, camPos.eulerAngles.y, ref y, 0.1f), Mathf.SmoothDampAngle(transform.eulerAngles.z, camPos.eulerAngles.z, ref z, 0.1f));
        }
    }
}
