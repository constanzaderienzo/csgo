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
     private Quaternion _cameraRotation;
 
     // Update is called once per frame
     private void Start()
     {
         Cursor.lockState = CursorLockMode.Locked;
         playerTransform = GetComponent<Transform>();
         if (camera != null) 
             _cameraRotation = camera.transform.rotation;
     }

     void Update()
     {
        MoveView();
     }
     
     void MoveView()
     {
         float _yRot = Input.GetAxis("Mouse X");
         
         Vector3 _rotation = new Vector3(0f, _yRot, 0f) * (mouseSensitivity * Time.fixedDeltaTime);

         playerTransform.Rotate(_rotation);

         float _xRot = Input.GetAxis("Mouse Y");
         _xRot = -_xRot;
         _xRot = Mathf.Clamp(_xRot, -90f, 90f);
         
         _cameraRotation *= Quaternion.Euler(_xRot, 0f, 0f);
         _cameraRotation = LockCameraMovement(_cameraRotation);

         if (camera != null)
         {
             camera.transform.localRotation = _cameraRotation;
             //camera.transform.Rotate(_cameraRotation);
         }
     }
     
     private Quaternion LockCameraMovement(Quaternion q)
     {
         q.x /= q.w;
         q.y /= q.w;
         q.z /= q.w;
         q.w = 1.0f;
 
         var angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
 
         angleX = Mathf.Clamp(angleX, -90f, 90f);
 
         q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);
 
         return q;
     }
     
 }