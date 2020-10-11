 using System;
 using UnityEngine;
 
 public class CameraFollow : MonoBehaviour
 {
     public Transform playerTransform;
     public int depth = -20;
     public float mouseSensitivity = 100f;
     private float xRotation = 0f;

 
     // Update is called once per frame
     private void Start()
     {
         Cursor.lockState = CursorLockMode.Locked;
     }

     void Update()
     {
         if(playerTransform != null)
         {
             float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
             float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
             
             xRotation -= mouseY;
             xRotation = Mathf.Clamp(xRotation, -90f, 90f);
             transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
             playerTransform.Rotate(Vector3.up * mouseX);
             transform.position = playerTransform.position  + new Vector3(0,2.1f,0); 
             transform.rotation = playerTransform.rotation;

         }
     }
 
     public void SetTarget(Transform target)
     {
         playerTransform = target;
     }
 }