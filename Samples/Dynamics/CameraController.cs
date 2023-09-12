using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CHM.ChocoWater.Samples.Dynamics
{
    [RequireComponent(typeof(PixelPerfectCamera))]
    public class CameraController : MonoBehaviour
    {
         private PixelPerfectCamera pixelPerfectCamera;
         private Vector3 currentVelocity;
         void Awake() 
         {
            TryGetComponent(out pixelPerfectCamera);
         }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.pKey.wasPressedThisFrame)
#else
            if(Input.GetKeyDown(KeyCode.P))
#endif
            {
                pixelPerfectCamera.enabled = !pixelPerfectCamera.enabled;
            }
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.upArrowKey.IsPressed())
#else
            if(Input.GetKey(KeyCode.UpArrow))
#endif
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, 
                    new Vector3(transform.position.x, 1f, transform.position.z), 
                    ref currentVelocity, 
                    0.25f);
            }
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.downArrowKey.IsPressed())
#else
            if(Input.GetKey(KeyCode.DownArrow))
#endif
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, 
                    new Vector3(transform.position.x, -1f, transform.position.z), 
                    ref currentVelocity, 
                    0.25f);
            }
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.rightArrowKey.IsPressed())
#else
            if(Input.GetKey(KeyCode.RightArrow))
#endif
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, 
                    new Vector3(1.5f, transform.position.y, transform.position.z), 
                    ref currentVelocity, 
                    0.25f);
            }
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.leftArrowKey.IsPressed())
#else
            if(Input.GetKey(KeyCode.LeftArrow))
#endif
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position, 
                    new Vector3(-1.5f, transform.position.y, transform.position.z), 
                    ref currentVelocity, 
                    0.25f);
            }
        }
    }
}
