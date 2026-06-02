using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MapGeneration : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform wall;
    [SerializeField] Transform center;
    [SerializeField] List<Transform> roomPrefabs;
    [SerializeField] List<Transform> extraPrefabs;
    
    [SerializeField] Transform collect;
    [SerializeField] Transform gold;
    GameObject[] tiles;
    GameObject[] waypoints;
    GameObject[] collectibles;

    [Header("Settings")]
    [SerializeField] int maxRoom = 5;
    [SerializeField] int centerPercent = 5;
    [SerializeField] int extraPercent = 15;
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
            
        DestroyImmediate(waypoints[randomWaypoint]);

        ApplyWall();
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

        if(!IsWaypointCollided(waypoints[randomWaypoint].transform))
        {
            int randomRoom;
            Transform LastRoom;

            int percentage = Random.Range(0, 100);
            if(roomCount < 4) percentage = 100; // make first 4
            if(percentage <= centerPercent) // Center
            {
                LastRoom = Instantiate(center, waypoints[randomWaypoint].transform.position, waypoints[randomWaypoint].transform.rotation);
            }
            else if(percentage <= extraPercent) // Extra Room
            {
                randomRoom = Random.Range(0, extraPrefabs.Count);
                LastRoom = Instantiate(extraPrefabs[randomRoom], waypoints[randomWaypoint].transform.position, waypoints[randomWaypoint].transform.rotation);
            }
            else
            {
                randomRoom = Random.Range(0, roomPrefabs.Count);
                LastRoom = Instantiate(roomPrefabs[randomRoom], waypoints[randomWaypoint].transform.position, waypoints[randomWaypoint].transform.rotation);
            }
            
            roomCount ++;

            if(IsTileCollided())
            {
                DestroyImmediate(LastRoom.gameObject);
                roomCount--;

                GenerateRoom();
                return;
            }
        }
            
        DestroyImmediate(waypoints[randomWaypoint]);

        GenerateRoom();
    }
}
