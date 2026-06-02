using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    [SerializeField] Transform targetPoint;
    [SerializeField] GameObject effect;
    
    public void playClip(AudioClip clip)
    {
        AudioSource effectSound = Instantiate(effect, targetPoint.position, targetPoint.rotation).GetComponent<AudioSource>();
        effectSound.clip = clip;
        effectSound.pitch = effect.GetComponent<AudioSource>().pitch + Random.Range(-0.1f, 0.1f);
    }
}
