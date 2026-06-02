using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectibles : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private float range = 5f;
    [SerializeField] private float pullSpeed = 10f;
    [SerializeField] private float collectionThreshold = 0.5f;
    [SerializeField] private LayerMask collectibleLayer;

    void Update()
    {
        InfluenceCollectibles();
    }

    void InfluenceCollectibles()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, range, collectibleLayer);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].CompareTag("Collectibles") || hitColliders[i].CompareTag("GoldCollect"))
            {
                Transform collectible = hitColliders[i].transform;
                float distance = Vector3.Distance(transform.position, collectible.position);

                if (distance <= collectionThreshold)
                {
                    CollectItem(collectible.gameObject);
                }
                else
                {
                    float ratio = Mathf.Clamp01(distance / range);
                    float dynamicSpeed = pullSpeed * (1.5f - ratio);
                    
                    collectible.position = Vector3.MoveTowards(
                        collectible.position, 
                        transform.position, 
                        dynamicSpeed * Time.deltaTime
                    );
                }
            }
        }
    }

    void CollectItem(GameObject collectibleObj)
    {   
        if(collectibleObj.tag == "GoldCollect") GameManager.CollectGold(1);

        Destroy(collectibleObj);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}