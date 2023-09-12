using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CHM.ChocoWater.Samples.Dynamics
{
    public class KillArea : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other) 
        {
            Destroy(other.gameObject);
        }
    }
}
