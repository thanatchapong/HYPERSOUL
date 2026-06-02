using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    [System.Serializable]
    public struct RoomPrefabs
    {
        public Transform room;
        [Range(0, 100)] public int percentage;
    }
    
    [Header("References")]
    [SerializeField] Transform wall;
    [SerializeField] List<RoomPrefabs> roomPrefabs;
    
    [SerializeField] Transform collect;
    [SerializeField] Transform gold;
    GameObject[] tiles;
    GameObject[] waypoints;
    GameObject[] collectibles;

    [Header("Settings")]
    [SerializeField] int maxRoom = 5;
    int roomCount;

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

        for(int i=0; i<tiles.Length; i++)
        {
            if(Vector3.Distance(waypoint.position, tiles[i].transform.position) < 0.1f) return true;
        }

        return false;
    }
    bool IsTileCollided()
    {
        GetTiles();

        for(int i=0; i<tiles.Length; i++)
        {
            for(int o=0; o<tiles.Length; o++)
            {
                if(o == i) continue;
                if (Vector3.Distance(tiles[o].transform.position, tiles[i].transform.position) < 0.1f) return true;
            }
        }

        return false;
    }

    void SpawnCollectibles()
    {
        GetCollectibles();

        for(int i=0; i<collectibles.Length; i++)
        {
            Instantiate(collect, collectibles[i].transform.position, collectibles[i].transform.rotation);
        }
    }

    void ApplyWall()
    {
        GetWaypoints();

        if(waypoints.Length <= 0) 
        {
            SpawnCollectibles();
            return;
        }

        int randomWaypoint = Random.Range(0, waypoints.Length);

        if(!IsWaypointCollided(waypoints[randomWaypoint].transform))
        {
            Instantiate(wall, waypoints[randomWaypoint].transform.position, waypoints[randomWaypoint].transform.rotation);
        }
            
        SafeDestroy(waypoints[randomWaypoint]);

        ApplyWall();
    }

    private Transform GetWeightedRandomRoom()
    {
        if (roomPrefabs == null || roomPrefabs.Count == 0) return null;

        int totalWeight = 0;
        foreach (var p in roomPrefabs)
        {
            totalWeight += p.percentage;
        }

        if (totalWeight <= 0)
        {
            return roomPrefabs[Random.Range(0, roomPrefabs.Count)].room;
        }

        int roll = Random.Range(0, totalWeight);
        int weightCounter = 0;

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
        if(roomCount >= maxRoom) 
        {
            ApplyWall();
            return;
        }

        GetWaypoints();

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
