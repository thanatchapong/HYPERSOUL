using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class You : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The object this enemy will retrace. Usually tagged 'Player' or 'MainCamera'.")]
    [SerializeField] private string targetTag = "Player";
    private Transform target;

    [Header("Delay Settings")]
    [Tooltip("How many seconds behind the target this enemy will trace.")]
    [SerializeField] private float timeStart = 5.0f;
    [SerializeField] private float timeDelay = 2.0f;
    float countDown = 0;

    [Header("Physics (Optional)")]
    [Tooltip("If checked, moves via Rigidbody.MovePosition. Recommended if you want physical collisions.")]
    [SerializeField] private bool usePhysicsMovement = true;

    // Struct to store exact historical frame state
    private struct PathPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timeStamp;

        public PathPoint(Vector3 pos, Quaternion rot, float time)
        {
            position = pos;
            rotation = rot;
            timeStamp = time;
        }
    }

    private List<PathPoint> pathHistory = new List<PathPoint>();
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.useGravity = false; 
        }

        GameObject targetObj = GameObject.FindWithTag(targetTag);
        if (targetObj != null)
        {
            target = targetObj.transform;
            
            pathHistory.Add(new PathPoint(transform.position, target.rotation, Time.time));
            // pathHistory.Add(new PathPoint(target.position, target.rotation, Time.time));
        }
        else
        {
            Debug.LogWarning($"Delayed Path Follower: Could not find target with tag '{targetTag}'!");
        }
    }

    void Update()
    {
        if (target == null) return;

        countDown += Time.deltaTime;
        if(countDown >= timeDelay)
        {
            pathHistory.Add(new PathPoint(target.position, target.rotation, Time.time));
        }
    }

    void FixedUpdate()
    {
        if (target == null || pathHistory.Count == 0) return;

        float playbackTime = Time.time - timeDelay;

        while (pathHistory.Count > 2 && pathHistory[1].timeStamp < playbackTime)
        {
            pathHistory.RemoveAt(0);
        }

        if (pathHistory.Count >= 2)
        {
            PathPoint pointBefore = pathHistory[0];
            PathPoint pointAfter = pathHistory[1];

            float timeDiff = pointAfter.timeStamp - pointBefore.timeStamp;
            float t = 0f;
            
            if (timeDiff > 0.0001f)
            {
                t = (playbackTime - pointBefore.timeStamp) / timeDiff;
            }

            Vector3 targetPosition = Vector3.Lerp(pointBefore.position, pointAfter.position, t);
            Quaternion targetRotation = Quaternion.Slerp(pointBefore.rotation, pointAfter.rotation, t);

            if (usePhysicsMovement && rb != null)
            {
                rb.MovePosition(targetPosition);
                // transform.LookAt(targetPosition);
                rb.MoveRotation(targetRotation);
            }
            else
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }
        else if (pathHistory.Count == 1)
        {
            if (usePhysicsMovement && rb != null)
            {
                rb.MovePosition(pathHistory[0].position);
                rb.MoveRotation(pathHistory[0].rotation);
                // transform.LookAt(pathHistory[0].position);
            }
            else
            {
                transform.position = pathHistory[0].position;
                transform.rotation = pathHistory[0].rotation;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (pathHistory == null || pathHistory.Count < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < pathHistory.Count - 1; i++)
        {
            Gizmos.DrawLine(pathHistory[i].position, pathHistory[i + 1].position);
        }
    }
}