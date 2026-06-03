using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blob : MonoBehaviour
{
    Transform target;
    public float blobSpeed = 2f;
    Rigidbody rb;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        target = GameObject.FindWithTag("MainCamera").transform;
    }
    
    void Update()
    {
        transform.LookAt(target);
        rb.AddForce(transform.forward * blobSpeed, ForceMode.Force);
    }
}
