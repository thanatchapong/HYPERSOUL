using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Esroh : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How many seconds the entity waits before planning and taking its next multi-tile journey.")]
    [SerializeField] private float timePerMove = 10f;
    [Tooltip("How fast the entity slides from tile to tile.")]
    [SerializeField] private float speed = 5f;
    [Tooltip("How fast the entity rotates to face the next step.")]
    [SerializeField] private float rotationSpeed = 12f;
    [Tooltip("The vertical height offset of the entity above the tiles so it doesn't clip into the floor.")]
    [SerializeField] private float heightOffset = 1.0f;

    private GameObject[] tiles;
    private List<Transform> currentPath = new List<Transform>();
    private bool isMoving = false;
    private float moveTimer = 0f;
    private float gridSpacing = 5.0f; 

    void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForSeconds(0.2f); 
        
        GetTiles();
        
        if (tiles != null && tiles.Length > 0)
        {
            gridSpacing = CalculateGridSpacing();
            SnapToNearestTile();
        }
        else
        {
            Debug.LogWarning("Esroh: No objects with tag 'Tile' were found in the scene!");
        }
    }

    void Update()
    {
        if (tiles == null || tiles.Length == 0 || isMoving) return;

        moveTimer += Time.deltaTime;
        if (moveTimer >= timePerMove)
        {
            moveTimer = 0f;
            TriggerNewPathfindingJourney();
        }
    }

    void GetTiles()
    {
        tiles = GameObject.FindGameObjectsWithTag("Tile");
    }

    float CalculateGridSpacing()
    {
        float minDistance = float.MaxValue;
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = i + 1; j < tiles.Length; j++)
            {
                float d = Vector3.Distance(tiles[i].transform.position, tiles[j].transform.position);
                if (d > 0.1f && d < minDistance)
                {
                    minDistance = d;
                }
            }
        }
        return minDistance == float.MaxValue ? 2.0f : minDistance;
    }

    void SnapToNearestTile()
    {
        Transform nearest = GetNearestTile(transform.position);
        if (nearest != null)
        {
            Vector3 targetPos = nearest.position;
            targetPos.y += heightOffset;
            transform.position = targetPos;
        }
    }

    Transform GetNearestTile(Vector3 position)
    {
        if (tiles == null || tiles.Length == 0) return null;

        Transform nearest = null;
        float minDistance = float.MaxValue;

        foreach (GameObject tile in tiles)
        {
            if (tile == null) continue;
            float dist = Vector3.Distance(position, tile.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = tile.transform;
            }
        }
        return nearest;
    }

    void TriggerNewPathfindingJourney()
    {
        Transform startTile = GetNearestTile(transform.position);
        
        int randIndex = Random.Range(0, tiles.Length);
        Transform destinationTile = tiles[randIndex].transform;

        if (startTile != null && destinationTile != null)
        {
            List<Transform> path = FindPathBFS(startTile, destinationTile);
            if (path != null && path.Count > 1)
            {
                StartCoroutine(MoveAlongPath(path));
            }
        }
    }

    List<Transform> FindPathBFS(Transform start, Transform target)
    {
        Queue<Transform> queue = new Queue<Transform>();
        Dictionary<Transform, Transform> cameFrom = new Dictionary<Transform, Transform>();

        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            if (current == target)
            {
                List<Transform> path = new List<Transform>();
                Transform temp = target;
                while (temp != null)
                {
                    path.Add(temp);
                    temp = cameFrom[temp];
                }
                path.Reverse();
                return path;
            }

            foreach (Transform neighbor in GetNeighbors(current))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return null; 
    }

    List<Transform> GetNeighbors(Transform tile)
    {
        List<Transform> neighbors = new List<Transform>();
        float threshold = gridSpacing * 1.2f;

        foreach (GameObject t in tiles)
        {
            if (t == null || t.transform == tile) continue;

            float horizontalDist = Vector3.Distance(
                new Vector3(tile.position.x, 0, tile.position.z),
                new Vector3(t.transform.position.x, 0, t.transform.position.z)
            );

            float verticalDist = Mathf.Abs(tile.position.y - t.transform.position.y);

            if (horizontalDist > 0.1f && horizontalDist <= threshold && verticalDist < 0.5f)
            {
                neighbors.Add(t.transform);
            }
        }
        return neighbors;
    }

    private IEnumerator MoveAlongPath(List<Transform> path)
    {
        isMoving = true;
        currentPath = path;

        for (int i = 1; i < path.Count; i++)
        {
            Transform targetNode = path[i];
            Vector3 targetPosition = targetNode.position;
            targetPosition.y += heightOffset;

            Vector3 lookDirection = targetPosition - transform.position;
            lookDirection.y = 0;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                while (Quaternion.Angle(transform.rotation, targetRotation) > 1.0f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.rotation = targetRotation; 
            }

            while (Vector3.Distance(transform.position, targetPosition) > 0.02f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPosition; 
        }

        currentPath.Clear();
        isMoving = false;
    }

    private void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.Count < 2) return;

        Gizmos.color = Color.magenta;
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            if (currentPath[i] != null && currentPath[i + 1] != null)
            {
                Vector3 start = currentPath[i].position + Vector3.up * (heightOffset + 0.1f);
                Vector3 end = currentPath[i + 1].position + Vector3.up * (heightOffset + 0.1f);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(end, 0.2f);
            }
        }
    }
}