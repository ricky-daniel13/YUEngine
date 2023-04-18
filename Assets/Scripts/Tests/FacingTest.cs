using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputTest
{
    public class FacingTest : MonoBehaviour
    {
        // Start is called before the first frame update
        public Color displayColor;
        private void OnDrawGizmos()
        {
            Debug.DrawRay(transform.position, transform.forward, displayColor);
        }
    }
}
