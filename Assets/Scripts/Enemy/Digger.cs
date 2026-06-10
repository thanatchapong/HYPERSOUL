using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Digger : MonoBehaviour
{
    [SerializeField] PlayableDirector anim;
    [SerializeField] float timeDelay = 2f;
    float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if(timer >= timeDelay)
        {
            timer = 0;
            
            GetCollectibles();

            transform.position = collectibles[Random.Range(0, collectibles.Length)].transform.position;
            anim.Play();
        }
    }

    private GameObject[] collectibles;
    void GetCollectibles()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Gold");
    }
}
