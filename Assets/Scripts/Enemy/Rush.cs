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
    
    [Tooltip("The MINIMUM distance (radius) the target point must be from the spawn light.")]
    [SerializeField] private float searchRadius = 25f;

    [Tooltip("The MAXIMUM distance the target point can be from the spawn light.")]
    [SerializeField] private float maxSearchRadius = 50f;

    [Header("Flicker Settings")]
    [Tooltip("How close a light must be to the calculated path corridor to be triggered to flicker.")]
    [SerializeField] private float pathFlickerWidth = 12f;

    // CRITICAL FIX: Initialized the list to prevent NullReferenceExceptions!
    private List<LightSystem> lightFlickered = new List<LightSystem>();
    private GameObject[] lights;
    private bool isAttacking = false;

    void Start()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        // Find all lights in the scene initially
        GetLight();

        // Start the infinite attack loop
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
            // 1. Wait for the configured delay window between attacks
            yield return new WaitForSeconds(timeBetweenMove);

            // Refresh lights array in case the map generation added new rooms
            GetLight();

            if (lights == null || lights.Length == 0)
            {
                Debug.LogWarning("Rush: No objects with tag 'Light' found in the scene yet!");
                continue;
            }

            // 2. Pick a completely random starting light source
            int randomLightIndex = Random.Range(0, lights.Length);
            GameObject selectedLight = lights[randomLightIndex];

            if (selectedLight != null)
            {
                // 3. Teleport to that light source safely
                TeleportToPosition(selectedLight.transform.position);
                body.SetActive(false);

                // 4. Find a valid coordinate on the NavMesh strictly between searchRadius (min) and maxSearchRadius (max)
                Vector3 targetDestination;
                if (TryGetRandomRingNavMeshPoint(selectedLight.transform.position, searchRadius, maxSearchRadius, out targetDestination))
                {
                    // 5. Pre-calculate the exact path we are about to travel
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(targetDestination, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        // 6. Flicker only the lights that sit along our calculated path segments
                        FlickerLightsAlongPath(path);
                    }
                    else
                    {
                        // Fallback: If path calculation failed, just flicker the local start light
                        LightSystem startLightSystem = selectedLight.GetComponentInParent<LightSystem>();
                        if (startLightSystem != null)
                        {
                            // Trigger flicker and leave it off (afterLight = false)
                            startLightSystem.flickeringLight(timeBeforeMove, false);
                            
                            if (!lightFlickered.Contains(startLightSystem))
                            {
                                lightFlickered.Add(startLightSystem);
                            }
                        }
                    }

                    // 7. WAIT at the start position while the lights are flickering (The build-up phase!)
                    yield return new WaitForSeconds(timeBeforeMove);

                    body.SetActive(true);

                    // 8. Launch the physical attack
                    agent.SetDestination(targetDestination);
                    isAttacking = true;

                    // 9. Keep tracking until the agent reaches the destination point
                    while (isAttacking)
                    {
                        // Check if the agent has fully completed its path calculation and arrived
                        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        {
                            body.SetActive(false);
                            isAttacking = false;

                            // Loop through all gathered lights and run their restored flicker-on sequence
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
    /// Searches for active LightSystem scripts in the scene and triggers those within proximity to the travel path.
    /// </summary>
    void FlickerLightsAlongPath(NavMeshPath path)
    {
        if (path.corners.Length < 2) return;

        // Loop through every light object in our cache
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null) continue;

            LightSystem lightSystem = lights[i].GetComponentInParent<LightSystem>();
            if (lightSystem == null) continue;

            // Check if this light is physically close to any of our path corridors
            if (IsPointNearPath(lights[i].transform.position, path.corners, pathFlickerWidth))
            {
                // Trigger the flicker! Pass false to ensure it remains turned off after finishing
                lightSystem.flickeringLight(timeBeforeMove, false);

                // Add to list if not already tracked
                if (!lightFlickered.Contains(lightSystem))
                {
                    lightFlickered.Add(lightSystem);
                }
            }
        }
    }

    /// <summary>
    /// Smoothly restores a specific light back to life with a cinematic flicker transition
    /// </summary>
    IEnumerator flickAndOpenback(LightSystem lightSystem)
    {
        if (lightSystem == null) yield break;

        // Wait a small delay after Rush passes before recovering the environment's lights
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

    /// <summary>
    /// Checks if a 3D coordinate point is within a threshold distance of any calculated path segment.
    /// </summary>
    bool IsPointNearPath(Vector3 point, Vector3[] pathCorners, float threshold)
    {
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            float distanceToSegment = DistanceToSegment(point, pathCorners[i], pathCorners[i + 1]);
            if (distanceToSegment <= threshold)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Measures the mathematically shortest 3D distance between a point and a line segment (start to end).
    /// </summary>
    float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        Vector3 toPoint = point - start;
        float segmentLengthSq = segment.sqrMagnitude;

        if (segmentLengthSq < 0.0001f) return Vector3.Distance(point, start);
        
        // Project point onto segment and clamp to segment bounds
        float t = Vector3.Dot(toPoint, segment) / segmentLengthSq;
        t = Mathf.Clamp01(t);

        Vector3 projection = start + t * segment;
        return Vector3.Distance(point, projection);
    }

    /// <summary>
    /// Safely updates position by temporarily overriding active NavMesh Agent constraints.
    /// </summary>
    void TeleportToPosition(Vector3 newPosition)
    {
        agent.enabled = false; // Turn off completely so it doesn't snap back
        transform.position = newPosition;
        agent.enabled = true;  // Turn back on to sample the destination surface
    }

    /// <summary>
    /// Samples points within a hollow ring (donut shape) to find a NavMesh point that satisfies 
    /// the minimum and maximum distance constraints from the starting position.
    /// </summary>
    bool TryGetRandomRingNavMeshPoint(Vector3 center, float minRadius, float maxRadius, out Vector3 result)
    {
        // Try up to 30 times to find a valid coordinate on the NavMesh that fits the distance profile
        for (int i = 0; i < 30; i++)
        {
            Vector2 randomDir2D = Random.insideUnitCircle.normalized;
            Vector3 randomDirection = new Vector3(randomDir2D.x, 0, randomDir2D.y);

            float randomDistance = Random.Range(minRadius, maxRadius);
            Vector3 targetPoint = center + (randomDirection * randomDistance);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPoint, out hit, 5f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, center) >= minRadius)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(center + (Random.onUnitSphere * maxRadius), out fallbackHit, maxRadius, NavMesh.AllAreas))
        {
            result = fallbackHit.position;
            return true;
        }

        result = center;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, maxSearchRadius);
    }
}