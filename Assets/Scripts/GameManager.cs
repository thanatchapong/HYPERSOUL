using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_Text dataText;

    [SerializeField] Transform collectible;
    [SerializeField] Transform goldCollect;

    public int goldValue = 0;
    public static int maxCollectible;

    public static int goldAmount = 0;

    static GameObject[] collectibles;
    static Transform collect;
    static Transform gold;

    bool successfullyCollect = false;

    void Awake()
    {
        collect = collectible;
        gold = goldCollect;
    }

    void Update()
    {
        GetCollectibles();

        // Debug.Log(collectibles.Length);
        if(successfullyCollect == true)
        {
            dataText.text = goldAmount.ToString() + " / " + maxCollectible.ToString();
        }
        else
        {
            dataText.text = collectibles.Length.ToString() + " / " + maxCollectible.ToString();
        }

        if(collectibles.Length <= 0 && !successfullyCollect)
        {
            successfullyCollect = true;
            SpawnGold();
        }
        goldValue = goldAmount;
    }

    static void GetCollectiblesSpot()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Gold");
    }
    
    static void GetCollectibles()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Collectibles");
    }


    public static void SpawnCollectible()
    {
        GetCollectiblesSpot();
        maxCollectible = collectibles.Length;

        for(int i=0; i<collectibles.Length; i++)
        {
            Instantiate(collect, collectibles[i].transform.position, collectibles[i].transform.rotation);
        }
    }
    void SpawnGold()
    {
        GetCollectiblesSpot();

        for(int i=0; i<collectibles.Length; i++)
        {
            Instantiate(gold, collectibles[i].transform.position, collectibles[i].transform.rotation);
        }
    }
    public static void CollectGold(int value)
    {
        goldAmount += value;
    }
}
