using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    [System.Serializable]
    public struct RoomPrefabs
    {
        public Transform room;
        [Range(0, 100)] public float percentage;
    }

    [System.Serializable]
    public struct PropPrefabs
    {
        public Transform prop;
        [Range(0, 100)] public float percentage;
    }
    
    [Header("References")]
    [SerializeField] private Transform wall;
    [SerializeField] private List<RoomPrefabs> roomPrefabs;
    [SerializeField] private List<PropPrefabs> propPrefabs;
    
    private GameObject[] tiles;
    private GameObject[] waypoints;
    private GameObject[] collectibles;

    [Header("Room Settings")]
    [SerializeField] private int maxRoom = 5;
    private int roomCount;

    [Header("Prop Spawn Settings")]
    [Tooltip("The percentage chance (0 to 100) that a prop will spawn at any given collectible marker spot.")]
    [Range(0f, 100f)] [SerializeField] private float propSpawnRate = 75f;

    void Start()
    {
        GenerateRoom();
    }

    void GetTiles()
    {
        tiles = GameObject.FindGameObjectsWithTag("Tile");
    }
    
    void GetWaypoints()
    {
        waypoints = GameObject.FindGameObjectsWithTag("WayPoint");
    }
    
    void GetCollectibles()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Gold");
    }

    bool IsWaypointCollided(Transform waypoint)
    {
        GetTiles();

        for(int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;
            if (Vector3.Distance(waypoint.position, tiles[i].transform.position) < 2.5f) return true;
        }

        return false;
    }

    bool IsTileCollided()
    {
        GetTiles();

        for(int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;
            for(int o = 0; o < tiles.Length; o++)
            {
                if (tiles[o] == null || o == i) continue;
                if (Vector3.Distance(tiles[o].transform.position, tiles[i].transform.position) < 2.5f) return true;
            }
        }

        return false;
    }

    void ApplyProp()
    {
        GetCollectibles();

        if (collectibles == null || collectibles.Length == 0) return;

        foreach (GameObject spot in collectibles)
        {
            if (spot == null) continue;

            float roll = Random.Range(0f, 100f);
            if (roll <= propSpawnRate)
            {
                Transform chosenPropPrefab = GetWeightedRandomProp();

                if (chosenPropPrefab != null)
                {
                    Transform spawnedProp = Instantiate(chosenPropPrefab, spot.transform.position, spot.transform.rotation);

                    Vector3 originalScale = spawnedProp.localScale;
                    if (Random.value > 0.5f)
                    {
                        originalScale.x *= -1f;
                    }
                    spawnedProp.localScale = originalScale;
                }
            }

            // SafeDestroy(spot);
        }

        GameManager.SpawnGold();
    }

    void ApplyWall()
    {
        GetWaypoints();

        if (waypoints == null || waypoints.Length == 0) 
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
            ApplyProp();
            return;
        }

        List<GameObject> shuffledWaypoints = new List<GameObject>(waypoints);
        for (int i = 0; i < shuffledWaypoints.Count; i++)
        {
            GameObject temp = shuffledWaypoints[i];
            int randomIndex = Random.Range(i, shuffledWaypoints.Count);
            shuffledWaypoints[i] = shuffledWaypoints[randomIndex];
            shuffledWaypoints[randomIndex] = temp;
        }

        foreach (GameObject waypointObj in shuffledWaypoints)
        {
            if (waypointObj == null) continue;

            if (!IsWaypointCollided(waypointObj.transform))
            {
                Instantiate(wall, waypointObj.transform.position, waypointObj.transform.rotation);
            }
            
            SafeDestroy(waypointObj);
        }

        GetComponent<NavMeshSurface>().BuildNavMesh();

        ApplyProp();
    }

    private Transform GetWeightedRandomRoom()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0) return null;

        float totalWeight = 0;
        foreach (var p in roomPrefabs)
        {
            totalWeight += p.percentage;
        }

        if (totalWeight <= 0)
        {
            return roomPrefabs[Random.Range(0, roomPrefabs.Count)].room;
        }

        float roll = Random.Range(0, totalWeight);
        float weightCounter = 0;

        for (int i = 0; i < roomPrefabs.Count; i++)
        {
            weightCounter += roomPrefabs[i].percentage;
            if (roll < weightCounter)
            {
                return roomPrefabs[i].room;
            }
        }

        return roomPrefabs[0].room;
    }

    private Transform GetWeightedRandomProp()
    {
        if (propPrefabs == null || propPrefabs.Count == 0) return null;

        float totalWeight = 0;
        foreach (var p in propPrefabs)
        {
            totalWeight += p.percentage;
        }

        if (totalWeight <= 0)
        {
            return propPrefabs[Random.Range(0, propPrefabs.Count)].prop;
        }

        float roll = Random.Range(0, totalWeight);
        float weightCounter = 0;

        for (int i = 0; i < propPrefabs.Count; i++)
        {
            weightCounter += propPrefabs[i].percentage;
            if (roll < weightCounter)
            {
                return propPrefabs[i].prop;
            }
        }

        return propPrefabs[0].prop;
    }

    private void SafeDestroy(GameObject obj)
    {
        if (obj == null) return;

        if (Application.isPlaying)
        {
            obj.tag = "Untagged"; 
            obj.SetActive(false);
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    void GenerateRoom()
    {
        if (roomCount >= maxRoom) 
        {
            ApplyWall();
            return;
        }

        GetWaypoints();

        if (waypoints == null || waypoints.Length == 0)
        {
            ApplyWall();
            return;
        }

        int randomWaypoint = Random.Range(0, waypoints.Length);
        GameObject selectedWaypoint = waypoints[randomWaypoint];

        if (selectedWaypoint != null)
        {
            if (!IsWaypointCollided(selectedWaypoint.transform))
            {
                Transform chosenRoomPrefab = GetWeightedRandomRoom();

                if (chosenRoomPrefab != null)
                {
                    Transform LastRoom = Instantiate(chosenRoomPrefab, selectedWaypoint.transform.position, selectedWaypoint.transform.rotation);
                    roomCount++;

                    if (IsTileCollided())
                    {
                        SafeDestroy(LastRoom.gameObject);
                        roomCount--;

                        GenerateRoom();
                        return;
                    }
                }
            }
                
            SafeDestroy(selectedWaypoint);
        }

        GenerateRoom();
    }
}