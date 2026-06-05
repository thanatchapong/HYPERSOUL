using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Rush : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private GameObject body;

    [Header("Movement Settings")]
    [Tooltip("How many seconds Rush waits at its spawn light while flickering the path's lights before starting to move.")]
    [SerializeField] private float timeBeforeMove = 3f;
    [Tooltip("How many seconds between attacks/movements.")]
    [SerializeField] private float timeBetweenMove = 5f;

    [Header("Detection Settings")]
    [Tooltip("How close (horizontally) the path must be to a room tile or light to consider that room traversed.")]
    [SerializeField] private float detectionRadius = 8f;

    [Header("Debugging Options")]
    [Tooltip("Draws real-time room bounds and light connections in the Scene View while playing.")]
    [SerializeField] private bool showDebugGizmos = true;
    [Tooltip("Prints detailed search results in the Unity Console.")]
    [SerializeField] private bool enableConsoleLogging = true;

    private List<LightSystem> lightFlickered = new List<LightSystem>();
    private GameObject[] lights;
    private bool isAttacking = false;

    // --- Visual Debug Cache Fields ---
    private HashSet<Transform> debugTraversedRooms = new HashSet<Transform>();
    private List<Vector3> debugPathCorners = new List<Vector3>();

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        GetLight();
        StartCoroutine(RushAttackLoop());
    }

    void GetLight()
    {
        lights = GameObject.FindGameObjectsWithTag("Light");
    }

    private IEnumerator RushAttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenMove);

            GetLight();

            if (lights == null || lights.Length == 0)
            {
                if (enableConsoleLogging) Debug.LogWarning("Rush: No objects with tag 'Light' found in the scene yet!");
                continue;
            }

            // 1. Pick a completely random starting light source
            int randomLightIndex = Random.Range(0, lights.Length);
            GameObject selectedLight = lights[randomLightIndex];

            if (selectedLight != null)
            {
                // Project the ceiling light's position down to the ground NavMesh first
                Vector3 groundSpawnPos = selectedLight.transform.position;
                if (NavMesh.SamplePosition(selectedLight.transform.position, out NavMeshHit startHit, 30f, NavMesh.AllAreas))
                {
                    groundSpawnPos = startHit.position;
                }

                // 2. Move Rush physically to the ground location (snapping to the ground NavMesh)
                TeleportToPosition(groundSpawnPos);
                body.SetActive(false);

                // 3. Find the absolute longest complete path from this starting point
                if (TryGetLongestPathTargetPoint(groundSpawnPos, selectedLight, out Vector3 targetDestination, out NavMeshPath path))
                {
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        debugPathCorners = path.corners.ToList();
                        FlickerLightsAlongPath(path);
                    }
                    else
                    {
                        if (enableConsoleLogging) Debug.LogWarning("Rush: Longest calculated path was incomplete! Reverting to local spawn flicker fallback.");
                        debugPathCorners.Clear();
                        debugTraversedRooms.Clear();
                        
                        // Fallback backup optimization
                        LightSystem startLightSystem = selectedLight.GetComponentInParent<LightSystem>();
                        if (startLightSystem != null)
                        {
                            startLightSystem.flickeringLight(timeBeforeMove, false);
                            
                            if (!lightFlickered.Contains(startLightSystem))
                            {
                                lightFlickered.Add(startLightSystem);
                            }
                        }
                    }

                    // 4. Pause while the corridor lights complete their active cycle
                    yield return new WaitForSeconds(timeBeforeMove);

                    body.SetActive(true);

                    // 5. Initiate tracking run
                    agent.SetDestination(targetDestination);
                    isAttacking = true;

                    while (isAttacking)
                    {
                        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        {
                            body.SetActive(false);
                            isAttacking = false;

                            for (int i = 0; i < lightFlickered.Count; i++)
                            {
                                if (lightFlickered[i] != null)
                                {
                                    StartCoroutine(flickAndOpenback(lightFlickered[i]));
                                }
                            }
                            lightFlickered.Clear();
                        }
                        yield return null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks what room instances the continuous NavMesh path cuts through and flickers all lights in those rooms.
    /// </summary>
    void FlickerLightsAlongPath(NavMeshPath path)
    {
        if (path.corners.Length < 2) return;

        HashSet<Transform> traversedRooms = new HashSet<Transform>();

        // Find all active tiles and lights in the scene
        GameObject[] allTiles = GameObject.FindGameObjectsWithTag("Tile");
        LightSystem[] allLights = FindObjectsOfType<LightSystem>();

        // Sample points along the path segments to find nearby rooms
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];

            Vector3 direction = end - start;
            float distance = direction.magnitude;
            Vector3 normalizedDir = direction.normalized;

            // Sample every 3 units to ensure no rooms are skipped along long corridors
            float stepSize = 3.0f;
            for (float d = 0f; d <= distance; d += stepSize)
            {
                Vector3 samplePoint = start + normalizedDir * d;
                Vector3 flatSample = new Vector3(samplePoint.x, 0, samplePoint.z);

                // A. Check closest tile
                Transform closestTile = FindClosestTile(flatSample, allTiles, out float tileDistance);
                if (closestTile != null && tileDistance <= detectionRadius)
                {
                    Transform roomRoot = GetRoomRoot(closestTile);
                    if (roomRoot != null) traversedRooms.Add(roomRoot);
                }

                // B. Check closest light (as a backup)
                Transform closestLight = FindClosestLight(flatSample, allLights, out float lightDistance);
                if (closestLight != null && lightDistance <= detectionRadius)
                {
                    Transform roomRoot = GetRoomRoot(closestLight);
                    if (roomRoot != null) traversedRooms.Add(roomRoot);
                }
            }

            // Also check the end corner of this segment
            Vector3 flatEnd = new Vector3(end.x, 0, end.z);
            Transform endTile = FindClosestTile(flatEnd, allTiles, out float endTileDist);
            if (endTile != null && endTileDist <= detectionRadius)
            {
                Transform roomRoot = GetRoomRoot(endTile);
                if (roomRoot != null) traversedRooms.Add(roomRoot);
            }

            Transform endLight = FindClosestLight(flatEnd, allLights, out float endLightDist);
            if (endLight != null && endLightDist <= detectionRadius)
            {
                Transform roomRoot = GetRoomRoot(endLight);
                if (roomRoot != null) traversedRooms.Add(roomRoot);
            }
        }

        debugTraversedRooms = traversedRooms; // Cache for Scene Gizmos

        int triggeredCount = 0;

        // Flicker all lights inside the traversed rooms
        foreach (Transform room in traversedRooms)
        {
            LightSystem[] roomLights = room.GetComponentsInChildren<LightSystem>();
            foreach (LightSystem lightSystem in roomLights)
            {
                lightSystem.flickeringLight(timeBeforeMove, false);

                if (!lightFlickered.Contains(lightSystem))
                {
                    lightFlickered.Add(lightSystem);
                    triggeredCount++;
                }
            }
        }

        if (enableConsoleLogging)
        {
            Debug.Log($"[RUSH DEBUG] Detected {traversedRooms.Count} traversed room instances. Triggered {triggeredCount} total light systems.");
        }
    }

    Transform FindClosestTile(Vector3 position, GameObject[] allTiles, out float minDistance)
    {
        Transform closest = null;
        minDistance = float.MaxValue;
        Vector3 flatPos = new Vector3(position.x, 0, position.z);

        for (int i = 0; i < allTiles.Length; i++)
        {
            if (allTiles[i] == null) continue;
            
            Vector3 tilePos = allTiles[i].transform.position;
            Vector3 flatTilePos = new Vector3(tilePos.x, 0, tilePos.z);
            float dist = Vector3.Distance(flatPos, flatTilePos);
            
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = allTiles[i].transform;
            }
        }
        return closest;
    }

    Transform FindClosestLight(Vector3 position, LightSystem[] allLights, out float minDistance)
    {
        Transform closest = null;
        minDistance = float.MaxValue;
        Vector3 flatPos = new Vector3(position.x, 0, position.z);

        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i] == null) continue;
            
            Vector3 lightPos = allLights[i].transform.position;
            Vector3 flatLightPos = new Vector3(lightPos.x, 0, lightPos.z);
            float dist = Vector3.Distance(flatPos, flatLightPos);
            
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = allLights[i].transform;
            }
        }
        return closest;
    }

    Transform GetRoomRoot(Transform current)
    {
        if (current == null) return null;
        
        Transform root = current;
        while (root.parent != null)
        {
            // Climb hierarchy until parent is the MapGenerator container or the MapGeneration script
            if (root.parent.name.Contains("MapGenerator") || root.parent.GetComponent<MapGeneration>() != null)
            {
                return root;
            }
            root = root.parent;
        }
        return root;
    }

    Bounds GetRoomBounds(Transform roomRoot)
    {
        Renderer[] renderers = roomRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(roomRoot.position, Vector3.one * 5f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    IEnumerator flickAndOpenback(LightSystem lightSystem)
    {
        if (lightSystem == null) yield break;

        yield return new WaitForSeconds(1f);
        
        if (lightSystem == null) yield break;
        lightSystem.toggleLight(true);
        
        yield return new WaitForSeconds(0.2f);
        if (lightSystem == null) yield break;
        lightSystem.toggleLight(false);
        
        yield return new WaitForSeconds(0.2f);
        if (lightSystem == null) yield break;
        lightSystem.toggleLight(true);
    }

    void TeleportToPosition(Vector3 newPosition)
    {
        agent.enabled = false;
        transform.position = newPosition;
        agent.enabled = true;
    }

    /// <summary>
    /// Scans candidate rooms in the scene and maps the absolute longest path currently available.
    /// Uses the lights list to dynamically evaluate target coordinates.
    /// </summary>
    bool TryGetLongestPathTargetPoint(Vector3 center, GameObject startingLight, out Vector3 result, out NavMeshPath bestPath)
    {
        bestPath = new NavMeshPath();
        result = center;
        float longestDistance = -1f;
        bool foundValidPath = false;

        if (lights == null || lights.Length <= 1)
        {
            // Direct safety fallback in case no other rooms have generated yet
            NavMeshHit fallbackHit;
            if (NavMesh.SamplePosition(center + (Random.onUnitSphere * 50f), out fallbackHit, 50f, NavMesh.AllAreas))
            {
                result = fallbackHit.position;
                NavMesh.CalculatePath(center, result, NavMesh.AllAreas, bestPath);
                return true;
            }
            return false;
        }

        // Gather all lights in the scene except the start room to avoid self-targeting
        List<GameObject> candidates = new List<GameObject>(lights);
        candidates.Remove(startingLight);

        // Cap evaluation candidates to 40 for clean execution times
        if (candidates.Count > 40)
        {
            candidates = candidates.OrderBy(x => Random.value).Take(40).ToList();
        }

        foreach (GameObject targetLight in candidates)
        {
            if (targetLight == null) continue;

            // Project candidate light to ground level
            Vector3 targetGroundPos = targetLight.transform.position;
            if (NavMesh.SamplePosition(targetLight.transform.position, out NavMeshHit hit, 30f, NavMesh.AllAreas))
            {
                targetGroundPos = hit.position;
            }
            else
            {
                continue; // Cannot reach ground beneath this light, skip
            }

            NavMeshPath testPath = new NavMeshPath();
            if (NavMesh.CalculatePath(center, targetGroundPos, NavMesh.AllAreas, testPath))
            {
                // Verify path is fully reachable and complete
                if (testPath.status == NavMeshPathStatus.PathComplete)
                {
                    float pathLength = CalculatePathLength(testPath);
                    if (pathLength > longestDistance)
                    {
                        longestDistance = pathLength;
                        bestPath = testPath;
                        result = targetGroundPos;
                        foundValidPath = true;
                    }
                }
            }
        }

        if (enableConsoleLogging && foundValidPath)
        {
            Debug.Log($"[RUSH DEBUG] Selected longest complete path route spanning {longestDistance:F1} units!");
        }

        return foundValidPath;
    }

    /// <summary>
    /// Calculates the continuous length of a NavMesh path across all its corners.
    /// </summary>
    private float CalculatePathLength(NavMeshPath path)
    {
        if (path.corners.Length < 2) return 0f;
        float length = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // 1. Draw the continuous NavMesh Path in solid Cyan
        if (debugPathCorners != null && debugPathCorners.Count >= 2)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < debugPathCorners.Count - 1; i++)
            {
                Gizmos.DrawLine(debugPathCorners[i] + Vector3.up * 0.2f, debugPathCorners[i + 1] + Vector3.up * 0.2f);
                Gizmos.DrawSphere(debugPathCorners[i] + Vector3.up * 0.2f, 0.4f);
            }
            Gizmos.DrawSphere(debugPathCorners[debugPathCorners.Count - 1] + Vector3.up * 0.2f, 0.4f);
        }

        // 2. Draw the transparent bounding box of each traversed Room parent in soft Green
        if (debugTraversedRooms != null)
        {
            foreach (Transform room in debugTraversedRooms)
            {
                if (room == null) continue;
                Bounds rBounds = GetRoomBounds(room);
                
                Gizmos.color = new Color(0f, 1f, 0f, 0.15f); // Soft green fill
                Gizmos.DrawCube(rBounds.center, rBounds.size);
                
                Gizmos.color = Color.green; // Wireframe outline
                Gizmos.DrawWireCube(rBounds.center, rBounds.size);
            }
        }

        // 3. Draw connection lines between lights and their Room Roots
        if (lights != null)
        {
            foreach (GameObject lightObj in lights)
            {
                if (lightObj == null) continue;

                LightSystem lightSystem = lightObj.GetComponentInParent<LightSystem>();
                if (lightSystem == null) continue;

                Transform roomRoot = GetRoomRoot(lightObj.transform);
                bool isTriggered = debugTraversedRooms != null && debugTraversedRooms.Contains(roomRoot);

                if (isTriggered)
                {
                    // Triggered successfully: draw a solid Green line to its room root position
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(lightObj.transform.position, roomRoot.position);
                    Gizmos.DrawWireSphere(lightObj.transform.position, 1.5f);
                }
                else
                {
                    // Ignored: draw a thin Red line to its room root position
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(lightObj.transform.position, roomRoot.position);
                    Gizmos.DrawWireSphere(lightObj.transform.position, 0.8f);
                }
            }
        }
    }
}