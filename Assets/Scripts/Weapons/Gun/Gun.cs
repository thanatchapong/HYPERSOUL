using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gun", menuName = "AmazingStuff/Gun", order = 1)]
public class Gun : ScriptableObject
{
    public GameObject gunPrefabs;
    public Rigidbody bullet;
    public LineRenderer lineTrajectory;

    public int shotForce = 1500;
    public int damage = 1;
    public float cooldown = 0.25f;
    public float cd;
}
