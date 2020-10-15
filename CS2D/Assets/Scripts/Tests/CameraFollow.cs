 using System;
 using UnityEngine;
 
 public class CameraFollow : MonoBehaviour
 {
     public Transform playerTransform;
     public int depth = -20;
     public float mouseSensitivity = 3f;
     private float xRotation = 0f;
     [SerializeField]
     private Camera camera;
 
     // Update is called once per frame
     private void Start()
     {
         Cursor.lockState = CursorLockMode.Locked;
         playerTransform = GetComponent<Transform>();
     }

     void FixedUpdate()
     {
        MoveView();
     }

     void Extra()
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
     void MoveView()
     {
         float _yRot = Input.GetAxis("Mouse X");
         
         Vector3 _rotation = new Vector3(0f, _yRot, 0f) * (mouseSensitivity * Time.fixedDeltaTime);

         playerTransform.Rotate(_rotation);

         float _xRot = Input.GetAxis("Mouse Y");
         _xRot = -_xRot;
         _xRot = Mathf.Clamp(_xRot, -90f, 90f);
         
         Vector3 _cameraRotation = new Vector3(_xRot, 0f, 0f) * (mouseSensitivity * Time.fixedDeltaTime);
         if (camera != null)
         {
             camera.transform.Rotate(_cameraRotation);
         }
     }
 
     public void SetTarget(Transform target)
     {
         playerTransform = target;
     }
 }