using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_Text dataText;

    [SerializeField] Transform goldCollect;

    public int goldValue = 0;
    public static int maxCollectible;

    public static int goldAmount = 0;

    static GameObject[] collectibles;
    static Transform gold;

    bool successfullyCollect = false;

    void Awake()
    {
        gold = goldCollect;
    }

    void Update()
    {
        // Debug.Log(collectibles.Length);
        if(successfullyCollect == true)
        {
            dataText.text = goldAmount.ToString() + " / " + maxCollectible.ToString();
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
        collectibles = GameObject.FindGameObjectsWithTag("GoldCollect");
    }
    
    public static void SpawnGold()
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
