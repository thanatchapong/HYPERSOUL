using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Rush : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Movement Settings")]
    [Tooltip("How many seconds between attacks/movements.")]
    [SerializeField] private float timeBetweenMove = 5f;
    [Tooltip("The maximum distance from the selected light the agent can pick a random destination point.")]
    [SerializeField] private float searchRadius = 25f;

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
            // 1. Wait for the configured delay window
            yield return new WaitForSeconds(timeBetweenMove);

            // Refresh lights array in case the map generation added new rooms
            GetLight();

            if (lights == null || lights.Length == 0)
            {
                Debug.LogWarning("Rush: No objects with tag 'Light' found in the scene yet!");
                continue;
            }

            // 2. Pick a completely random light source
            int randomLightIndex = Random.Range(0, lights.Length);
            GameObject selectedLight = lights[randomLightIndex];

            if (selectedLight != null)
            {
                // 3. Teleport to that light source safely
                TeleportToPosition(selectedLight.transform.position);

                // 4. Find a valid coordinate on the NavMesh surface to rush towards
                Vector3 targetDestination;
                if (TryGetRandomNavMeshPoint(selectedLight.transform.position, searchRadius, out targetDestination))
                {
                    // 5. Send the AI agent to the target location
                    agent.SetDestination(targetDestination);
                    isAttacking = true;

                    // 6. Keep tracking until the agent reaches the destination point
                    while (isAttacking)
                    {
                        // Check if the agent has fully completed its path calculation and arrived
                        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        {
                            isAttacking = false;
                        }
                        yield return null;
                    }
                }
            }
        }
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
    /// Samples the local geographic area around a point to calculate a guaranteed valid NavMesh point layout.
    /// </summary>
    bool TryGetRandomNavMeshPoint(Vector3 center, float radius, out Vector3 result)
    {
        // Generate a random vector direction within a sphere volume bounds
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;

        NavMeshHit hit;
        // Sample the coordinate space to find the closest solid mesh walkway surface
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        // Fallback backup path point if sample fails
        result = center;
        return false;
    }

    // Visualizes the search radius envelope directly in the editor viewport window
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
}