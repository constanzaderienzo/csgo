using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    public PlayerWeapon weapon;
    public Transform playerTransform;
    public LayerMask mask;

    [SerializeField] private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        if (cam == null)
        {
            Debug.LogError("PlayerShoot: No camera referenced");
            this.enabled = false;
        }

        playerTransform = GetComponent<Transform>();
    }

    public int Shoot()
    {
        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 1 << 8;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        RaycastHit hit;
        Vector3 positionWithOffset = transform.position + new Vector3(0f,2.1f,0f);
        //Vector3 directionWithOffset = Vector3.forward - new Vector3(0f, 0.05f, 0f);
        Vector3 directionWithOffset = cam.transform.forward - new Vector3(0f, 0.05f, 0f);
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(cam.transform.position, directionWithOffset, out hit, weapon.range, layerMask))
        {
            Debug.DrawRay(cam.transform.position, directionWithOffset * hit.distance, Color.yellow);
            //Debug.DrawRay(positionWithOffset, transform.TransformDirection(directionWithOffset) * hit.distance, Color.yellow);
            Debug.Log("Did Hit " + hit.collider.gameObject.name );
            int number;
            if (Int32.TryParse(hit.collider.gameObject.name, out number))
                return number;
            return -1;
        }
        return -1;
    }
    
    
}
