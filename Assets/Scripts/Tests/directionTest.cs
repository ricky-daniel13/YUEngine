using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputTest { 
    public class directionTest : MonoBehaviour
    {
        public Vector2 joystick;
        public Transform refPoint;
        public Transform gravity;
        public Vector3 finalDir;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (joystick.magnitude > 0)
            {
                //From CatLikeCoding' ball tutorial.

                Vector3 forward = ProjectDirectionOnPlane(refPoint.transform.forward, -gravity.forward.normalized);
                Vector3 right = ProjectDirectionOnPlane(refPoint.transform.right, -gravity.forward.normalized);
                finalDir = (forward * joystick.y + right * joystick.x).normalized;
                finalDir = transform.InverseTransformDirection(Quaternion.FromToRotation(-Physics.gravity.normalized, transform.up) * finalDir);
            }
        }

        private void OnDrawGizmos()
        {
            if (refPoint == null || gravity == null)
                return;
            Debug.DrawRay(transform.position, transform.rotation * finalDir, Color.green);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position + (transform.rotation * finalDir), 0.2f);
        }

        Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
        {
            return (direction - normal * Vector3.Dot(direction, normal)).normalized;
        }
    }

}
