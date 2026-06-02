using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] float power = 50f;

    void OnTriggerStay(Collider col)
    {
        if(col.gameObject.tag == "Player")
        {
            col.transform.parent.gameObject.GetComponent<Rigidbody>().AddForce(transform.forward * power, ForceMode.Impulse);
        }
        else
        {
            col.gameObject.GetComponent<Rigidbody>().AddForce(transform.forward * power, ForceMode.Impulse);
        }
    }
}
