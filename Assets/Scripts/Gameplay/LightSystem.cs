using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSystem : MonoBehaviour
{
    [SerializeField] GameObject lightOn;
    [SerializeField] GameObject lightOff;

    public bool lighting = true;

    void Start()
    {
        lighting = true;
        updateLight();
    }

    public void flickeringLight(float flickTime)
    {
        StartCoroutine(FlickLight(flickTime));
    }
    public void flickeringLight(float flickTime, bool afterLight)
    {
        StartCoroutine(FlickLight(flickTime, afterLight));
    }

    void updateLight()
    {
        if(lighting)
        {
            lightOn.SetActive(true);
            lightOff.SetActive(false);
        }
        else
        {
            lightOn.SetActive(false);
            lightOff.SetActive(true);
        }
    }
    public void toggleLight()
    {
        lighting = !lighting;

        updateLight();
    }
    public void toggleLight(bool value)
    {
        lighting = value;

        updateLight();
    }

    IEnumerator FlickLight(float flickTime)
    {
        for(int i=0; i<flickTime * 10; i++)
        {
            yield return new WaitForSeconds(0.1f);
            toggleLight();
        }
    }
    IEnumerator FlickLight(float flickTime, bool afterLight)
    {
        for(int i=0; i<flickTime * 10; i++)
        {
            yield return new WaitForSeconds(0.1f);
            toggleLight();
        }
        toggleLight(afterLight);
    }
}
