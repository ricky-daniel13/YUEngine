using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2;

public class YUMovement
{
    public YUController player;
    public Transform transform;

    public float rotaDecelFactor = 10;
    public bool accOnDesiredDir = true, isBraking;
    public float brakeAngle;
    public float runSpeed;

    /// <summary>
    /// Controls the player like a car wheel. Speed gets applied in 
    /// the desired direction, and friction is applied perpendicularly to that.
    /// </summary>
    /// <param name="inAcc">Acceleration Speed</param>
    /// <param name="inDcc">Braking Speed</param>
    /// <param name="inTangDrag">Friction to be applied to the tangent</param>
    /// <param name="inMaxSpeed">Speed where to stop accelerating</param>
    /// <param name="input">Struct with data of the player inputs</param>
    public void DoInputDamizean(float inAcc, float inDcc, float inTangDrag, float inMaxSpeed, PlayerInput input)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        float wrongDelta = Time.fixedDeltaTime;

        Vector3 inputDirection = transform.InverseTransformDirection(input.playerDir);

        // If there is some input...

        if (input.mag != 0)
        {
            // Fetch velocity in the Player's local frame, decompose into lateral and vertical
            // motion, and decompose lateral motion further into normal and tangential components.


            transform.BreakDownSpeed(player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);

            float normalSpeed = Vector3.Dot(hSpeed, inputDirection);
            Vector3 normalVelocity = inputDirection * normalSpeed;
            Vector3 tangentVelocity = hSpeed - normalVelocity;

            // Note: normalSpeed is the magnitude of normalVelocity, with the added
            // bonus that it's signed. If positive, the speed goes towards the same
            // direction than the input :)



            if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, input.mag))
            {
                // Accelerate towards the input direction.

                if (normalSpeed > 0)
                    normalSpeed += inAcc * wrongDelta;
                else
                    normalSpeed += inDcc * wrongDelta;

                normalSpeed = Mathf.Min(normalSpeed, inMaxSpeed);
                normalVelocity = inputDirection * normalSpeed;
            }

            // Additionally, we can apply some drag on the tangent directions for
            // tighter control.
            tangentVelocity = Vector3.MoveTowards(tangentVelocity, Vector3.zero, inTangDrag * wrongDelta * input.mag);
            Debug.DrawRay(transform.position + transform.up, transform.TransformDirection(tangentVelocity), Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(transform.position + transform.up, transform.TransformDirection(input.playerDir), Color.blue, Time.fixedDeltaTime);

            player.InternalSpeed = transform.TransformDirection(normalVelocity + tangentVelocity + vSpeed);
            // Compose local velocity back and compute velocity back into the Global frame.
            // You probably want to delay doing this to the end of the physics processing,
            // as transformations can incur into numerical damping of the velocities.
            // The last step is included only for the sake of completeness.
        }
    }

    public void DoInputRota(float inAcc, float inDcc, float inMaxSpeed, float rotaSpeed, PlayerInput input)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        //float wrongDelta = Time.fixedDeltaTime;

        //Vector3 inputDirection = joyInput.direction;

        // If there is some input...

        Vector3 inputDir = transform.InverseTransformDirection(input.playerDir);
        transform.BreakDownSpeed(player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);

        if (input.mag != 0)
        {
            // Fetch velocity in the Player's local frame, decompose into lateral and vertical
            // motion, and decompose lateral motion further into normal and tangential components.
            Debug.DrawRay(transform.position, transform.TransformDirection(inputDir), Color.black, Time.fixedDeltaTime);

            float normalSpeed = Vector3.Dot(hSpeed, inputDir);
            float dirDif = Vector3.Dot(hSpeed.normalized, inputDir);
            Quaternion currRota = Quaternion.LookRotation(hSpeed.magnitude > 0 ? hSpeed.normalized : inputDir); //Get the current rotation
            Quaternion toRota = Quaternion.LookRotation(inputDir);  //Get the target rotation
            float currSpeed = hSpeed.magnitude;

            bool canTurn = (dirDif > (currSpeed < runSpeed ? -2 : brakeAngle));

            Vector3 newDir = Quaternion.RotateTowards(currRota, (canTurn && !isBraking) ? toRota : currRota, rotaSpeed * Time.fixedDeltaTime) * Vector3.forward;

            currSpeed -= Mathf.Abs((currSpeed - Vector3.Dot(newDir, hSpeed))) * rotaDecelFactor;

            if (normalSpeed < 0 && ((!(canTurn))||isBraking) )
            {
                //Debug.Log("Is braking? " + isBraking + ",is breakspeed " + (!isBraking && currSpeed > runSpeed));
                if (!isBraking && currSpeed > runSpeed)
                    isBraking = true;
            }
            else
                isBraking = false;

            if (accOnDesiredDir)
            {
                hSpeed = (newDir * currSpeed) + inputDir * ((isBraking ? inDcc : (currSpeed < inMaxSpeed ? inAcc : 0)) * Time.fixedDeltaTime);
                Debug.DrawRay(transform.position, transform.TransformDirection(inputDir * ((isBraking ? inDcc : (currSpeed < inMaxSpeed ? inAcc : 0)) * Time.fixedDeltaTime)), Color.magenta, Time.fixedDeltaTime);
            }
            else
            {
                if (isBraking)
                {
                    currSpeed -= inDcc * Time.fixedDeltaTime;
                }
                else if (currSpeed < inMaxSpeed)
                {
                    currSpeed += inAcc * Time.fixedDeltaTime;
                }
                hSpeed = (newDir * currSpeed);
            }

            //Debug.DrawRay(transform.position, transform.TransformDirection(newDir), Color.green, Time.fixedDeltaTime);
            //Debug.DrawRay(transform.position, transform.TransformDirection(toRota * Vector3.forward), (canTurn) ? Color.blue : Color.red, Time.fixedDeltaTime);
            //Debug.DrawRay(transform.position, transform.TransformDirection(localSlope * speedInSlope), Color.black, Time.fixedDeltaTime);
            //Debug.DrawRay(transform.position, transform.TransformDirection(hSpeed), Color.green, Time.fixedDeltaTime);


            player.InternalSpeed = transform.TransformDirection(hSpeed + vSpeed);

            //Debug.DrawRay(transform.position, player.InternalSpeed, Color.red, Time.fixedDeltaTime);
        }

        float newSpeedInUp = Vector3.Dot(player.InternalSpeed, Vector3.up);
        //Debug.Log("Added speed: " + (newSpeedInUp - speedInUp));
    }


    public void DoInput1Dir(float inAcc, float inDcc, float inMaxSpeed, PlayerInput input, InputRef refFrame)
    {
        float vAcc = Mathf.Min(input.mag, 1);

        Vector3 rgt = refFrame.rgt;//Quaternion.LookRotation(Vector3.right) * disDir;
        input.playerDir = Vector3.Cross(rgt, player.GetGroundNormal).normalized;

        input.playerDir = transform.InverseTransformDirection(input.playerDir);
        rgt = Quaternion.LookRotation(Vector3.right) * input.playerDir;



        transform.BreakDownSpeed(player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);


        hSpeed = Quaternion.LookRotation(input.playerDir) * ((Vector3.Dot(input.playerDir, hSpeed) > 0 ? Vector3.forward : Vector3.back) * hSpeed.magnitude);

        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(disDir), Color.red, Time.fixedDeltaTime);


        if (input.mag != 0)
        {
            Vector3 inDir = Vector3.Dot(input.playerDir, input.playerDir) > 0 ? input.playerDir : -input.playerDir;
            float normalSpeed = Vector3.Dot(hSpeed, inDir);

            //Debug.Log("Resultado: " + Vector3.Dot(disDir, lateralVelocity).ToString("F2") + " currSPeed =" + normalSpeed.ToString("F2"));



            if (normalSpeed < 0)
                normalSpeed += inDcc * Time.fixedDeltaTime;
            else if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, input.mag))
                normalSpeed += inAcc * Time.fixedDeltaTime;

            hSpeed = inDir * normalSpeed;

            //d_normalSpeed = lateralVelocity;
            //d_tangentSpeed = Vector3.zero;

            //Debug.DrawRay(averageFloorPos, transform.TransformDirection(toRota * Vector3.forward), Color.red, Time.fixedDeltaTime);
        }
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(lateralVelocity), Color.blue, Time.fixedDeltaTime);

        player.InternalSpeed = transform.TransformDirection(hSpeed * vAcc + vSpeed);

        //finalSpeed = transform.TransformDirection(lateralVelocity * vAcc + verticalVelocity);

        //Vector3 rgt = Quaternion.LookRotation(Vector3.right) * disDir;
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(rgt), Color.red, Time.fixedDeltaTime);
        float dotFromRight = Vector3.Dot(player.InternalSpeed, rgt);
        //Debug.Log("dotfromright = " + dotFromRight.ToString("F3") + " vacc " + vAcc);
        player.InternalSpeed -= rgt * dotFromRight;
    }


}
