using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2.Splines;

namespace YU2
{
    public struct InputRef
    {
        public Vector3 forw;
        public Vector3 rgt;
        public Vector3 up;
    }
    [System.Serializable]
    public struct PlayerInput
    {
        public Vector3 playerDir;
        public Vector2 directionRaw;
        public float mag;
    }

    [RequireComponent(typeof(YUController))]
    public class YUInput : MonoBehaviour
    {
        public YUController player;
        
        public bool doFakeInput;
        public Vector2 fakeInput;

        public InputType controlMethod;
        public PlayerInput joyInput;

        public bool simpleInput;

        public enum InputType { catGrav, cat, hedgephy }

        private void Start()
        {
            joyInput.playerDir = Vector3.forward;
            joyInput.directionRaw = Vector2.up;
            
        }

        private void Update()
        {
            DoInput();
        }

        //public Vector3 desiredDir { get { return joyInput.playerDir; } }
        //public float heldPower { get { return joyInput.mag; } }

        public void DoInput()
        {
            HandleInput();
        }

        public PlayerInput TransformInput(InputRef frameRef)
        {
            PlayerInput builtInp;
            Vector3 globalUp = player.GetGroundNormal;
            Vector3 forward = frameRef.forw;
            Vector3 bestPln = Vector3.up;

            float dotFront, dotBack, dotLeft, dotRight, dotUp, dotDown, maxDot = float.NegativeInfinity;
            dotBack = -(dotFront = Vector3.Dot(globalUp, Vector3.forward));
            dotLeft = -(dotRight = Vector3.Dot(globalUp, Vector3.right));
            dotDown = -(dotUp = Vector3.Dot(globalUp, Vector3.up));

            if (dotUp > maxDot)
            {
                maxDot = dotUp;
            }

            if (dotDown > maxDot)
            {
                maxDot = dotDown;
                bestPln = Vector3.down;
            }

            if (dotLeft > maxDot)
            {
                maxDot = dotLeft;
                bestPln = Vector3.left;
                if (Vector3.Dot(frameRef.forw, Vector3.right) > 0.5)
                {

                    forward = frameRef.up;
                }
                else
                    forward = frameRef.forw;
            }

            if (dotRight > maxDot)
            {
                maxDot = dotRight;
                bestPln = Vector3.right;
                if (Vector3.Dot(frameRef.forw, Vector3.left) > 0.5)
                {

                    forward = frameRef.up;
                }
                else
                    forward = frameRef.forw;
            }

            if (dotFront > maxDot)
            {
                maxDot = dotFront;
                bestPln = Vector3.forward;
                if (Vector3.Dot(frameRef.forw, Vector3.back) > 0.5)
                {

                    forward = frameRef.up;
                }
                else
                    forward = frameRef.forw;
            }

            if (dotBack > maxDot)
            {
                maxDot = dotBack;
                bestPln = Vector3.back;
                if (Vector3.Dot(frameRef.forw, Vector3.forward) > 0.5)
                {

                    forward = frameRef.up;
                }
                else
                    forward = frameRef.forw;
            }

            forward = Extensions.ProjectDirectionOnPlane(forward, bestPln).normalized;

            forward = Extensions.ProjectDirectionOnPlane(forward, globalUp).normalized;


            Vector3 right = Vector3.Cross(globalUp, forward);

            Debug.DrawRay(player.transform.position + Vector3.up, forward, Color.blue);
            Debug.DrawRay(player.transform.position + Vector3.up, right, Color.red);
            Debug.DrawRay(player.transform.position + Vector3.up, globalUp, Color.green);

            Vector3 tempDirection = (forward * joyInput.directionRaw.y + right * joyInput.directionRaw.x).normalized;
            //builtInp.playerDir = transform.InverseTransformDirection(tempDirection);
            builtInp.playerDir = tempDirection;
            builtInp.mag = joyInput.mag;
            builtInp.directionRaw = joyInput.directionRaw;

            return builtInp;
        }

        public PlayerInput TransformInputSimple(InputRef frameRef)
        {
            PlayerInput builtInp;
            Vector3 globalUp = player.GetGroundNormal;
            Vector3 forward = Extensions.ProjectDirectionOnPlane(frameRef.forw, globalUp);
            if(forward == Vector3.zero)
            {
                forward = Extensions.ProjectDirectionOnPlane(frameRef.up, globalUp);
            }

            Vector3 right = Vector3.Cross(globalUp, forward);

            builtInp.playerDir = (forward * joyInput.directionRaw.y + right * joyInput.directionRaw.x).normalized;
            builtInp.mag = joyInput.mag;
            builtInp.directionRaw = joyInput.directionRaw;

            return builtInp;
        }

        void HandleInput()
        {
            Vector2 joystick;
            joystick.x = Input.GetAxis("Horizontal");
            joystick.y = Input.GetAxis("Vertical");

            if (doFakeInput)
            {
                joystick = fakeInput;
            }

            joyInput.mag = joystick.magnitude;

            if (joyInput.mag > 0)
            {
                joyInput.directionRaw = joystick.normalized;
            }
        }

        public static InputRef GetRefFrame(Transform trans)
        {
            InputRef frameRef;
            frameRef.forw = trans.forward;
            frameRef.rgt = trans.right;
            frameRef.up = trans.up;

            return frameRef;

        }

        public static InputRef GetRefFrame(BezierKnot knot)
        {
            InputRef frameRef;
            frameRef.forw = knot.tangent;
            frameRef.rgt = knot.binormal;
            frameRef.up = knot.normal;

            return frameRef;

        }

    }


}
