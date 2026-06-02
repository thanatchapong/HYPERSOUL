using System.Collections;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public Gun[] weapon;
    [SerializeField] int currentWeapon = 0;
    [SerializeField] Transform gunHolder;
    [SerializeField] GameObject holdingWeapon;

    [SerializeField] PlayableDirector parry;

    void Start()
    {
        if(weapon.Length != 0)
        {
            EquipWeapon();
        }
    }

    void Update()
    {
        if(holdingWeapon)
        {
            weapon[currentWeapon].cd += Time.deltaTime;

            if(Input.GetKeyDown(KeyCode.Mouse0) && weapon[currentWeapon].cd >= weapon[currentWeapon].cooldown)
            {
                weapon[currentWeapon].cd = 0;

                //Shoot Bullet
                if(weapon[currentWeapon].bullet)
                {   
                    Rigidbody shot = Instantiate(weapon[currentWeapon].bullet, Camera.main.transform.position, Camera.main.transform.rotation).GetComponent<Rigidbody>();
                    shot.AddForce(shot.transform.forward * weapon[currentWeapon].shotForce, ForceMode.Impulse);

                    holdingWeapon.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    Camera mainCamera = Camera.main;

                    if (mainCamera != null)
                    {
                        // Create a ray from the camera through the mouse position
                        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                        // Declare a RaycastHit to store information about the hit
                        RaycastHit hit;

                        holdingWeapon.transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<ParticleSystem>().Play();

                        LineRenderer shot;

                        // Perform the raycast
                        if (Physics.Raycast(ray, out hit))
                        {
                            // The ray hit something
                            Debug.Log("Hit object: " + hit.collider.gameObject.name);

                            // Do something with the hit information
                            // hit.collider.gameObject.GetComponent<MyScript>().MyFunction();

                            shot = Instantiate(weapon[currentWeapon].lineTrajectory, holdingWeapon.transform.GetChild(0).position, holdingWeapon.transform.GetChild(0).rotation).GetComponent<LineRenderer>();

                            shot.SetPosition(0, holdingWeapon.transform.GetChild(0).position);
                            shot.SetPosition(1, hit.point);

                            // BasicEnemy enemy = hit.collider.gameObject.GetComponent<BasicEnemy>();
                            // if(enemy)
                            // {
                            //     enemy.TakeDamage(weapon[currentWeapon].damage);
                            // }
                        }
                        else
                        {
                            shot = Instantiate(weapon[currentWeapon].lineTrajectory, holdingWeapon.transform.GetChild(0).position, holdingWeapon.transform.GetChild(0).rotation).GetComponent<LineRenderer>();

                            shot.SetPosition(0, holdingWeapon.transform.GetChild(0).position);
                            shot.SetPosition(1, holdingWeapon.transform.GetChild(0).position + holdingWeapon.transform.GetChild(0).transform.forward * 100);
                        }

                        Destroy(shot,0.1f);
                    }
                }

                //Shoot Animation
                holdingWeapon.GetComponent<PlayableDirector>().Play();
            }
        }

        if(Input.GetKeyDown(KeyCode.F) && parry)
        {
            parry.Play();
        }
    }

    void EquipWeapon()
    {
        holdingWeapon = Instantiate(weapon[currentWeapon].gunPrefabs, gunHolder);
    }
}
