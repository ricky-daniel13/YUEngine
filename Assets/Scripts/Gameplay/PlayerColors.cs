using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2;

public class PlayerColors : MonoBehaviour
{
    public enum MoveType { damizean, rotation, oneDir, forceDir }
    public MoveType moveMethod;
    public MoveType jumpMethod;

    public YUInput input;
    public YUCameraControl cameraInput;
    public YUController player;
    //CorrectiveSpline corrSpline;

    bool isJumping, isRolling, isBraking;
    [Header("Collision")]
    public float tryGroundDistance = 0.5f;
    public float tryGroundDistanceFast=0.25f;
    public LayerMask InteractionLayer;

    [Header("Movement")]
    public float acc = 13;
    public float dcc=40, frc=6, air=26, topSpeed=40, runSpeed=20, slopeFactor=20;
    public AnimationCurve accOverSpeed;
    [Header("Roll")]
    public float rollSlopeUpFactor = 16;
    public float rollSlopeDownFactor=50, rollFrc=4, rollDcl=20, rollStopSpeed=1, rollRotaSpeed=45;

    [Header("Input Modes")]
    public float inputBehindAngle = 110;
    public float inputBehindSlowAngle = 110, maxGroundSpeed = 30, tangentDrag = 20, jumpTangentDrag = 20, rotaModeSpeed=495, rotaModeSpeedFast=75, rotaModeSpeedMax=30, rotaModeSpeedMin=15, rotaModeSpdMul = 10;
    public bool accOnDesDir = true;

    [Header("Actions")]
    public float jumpForce = 18;
    public float lowJumpSpeed=4, quickForce = 30;

    [Header("Gravity")]
    public float gravityForce = 30;
    public float upGravityForce = 10;

    private float cosBrakeAngle, cosBrakeAngleSlow, splineRightOffset;
    private bool isOnCorrectiveSpline=false, isGettingCorrected=false;

    public bool fakeJump;

    private Vector3 splinePos, splineForw, oldSplineDir;

    public bool getJump
    {
        get { return isJumping; }
    }
    public bool getRoll
    {
        get { return isRolling; }
    }

    private void OnGUI()
    {
        // Make a background box
        GUI.Box(new Rect(10, 10, 400, 250), "Sonic data");

        GUI.Label(new Rect(15, 25, 250, 100), "isGrounded =" + player.GetIsGround);
        GUI.Label(new Rect(15, 25 + 15, 250, 100), "position =" + transform.position);
        GUI.Label(new Rect(15, 25 + 15 + 15, 250, 100), "speed magnitude =" + player.InternalSpeed.magnitude.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15, 250, 100), "fall speed =" + Vector3.Dot(player.InternalSpeed, player.gravityDir).ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15, 400, 100), "ground angle =" + Vector3.Angle(-player.gravityDir, player.GetGroundNormal).ToString("F2") + ", Slope factor: " + Mathf.Abs(Extensions.DegSin(Vector3.Angle(-player.gravityDir, player.GetGroundNormal))).ToString("F2") + ", dot factor: " + Mathf.Abs(1-Vector3.Dot(-player.gravityDir, player.GetGroundNormal)).ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15, 250, 100), "control lock =" + player.controlLockTimer.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "isRolling =" + isRolling);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "rota speed calc =" + (Mathf.Max(0, player.InternalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)).ToString("F3"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "rota speed =" + (Mathf.Lerp(rotaModeSpeed, rotaModeSpeedFast, Mathf.Max(0, player.InternalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin))).ToString("F3"));
        //GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "movement mode =" + moveMethod);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "Input raw angle =" + Vector2.Angle(Vector2.up, input.joyInput.directionRaw));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "Is on corrective spline =" + isOnCorrectiveSpline);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15+15, 250, 100), "Is getting corrected =" + isGettingCorrected);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "splineRightOffset =" + splineRightOffset);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15, 250, 100), "Acc on desired dir?: " + accOnDesDir);
    }

    // Start is called before the first frame update
    void Start()
    {
        cosBrakeAngle = Extensions.DegCos(inputBehindAngle);
        cosBrakeAngleSlow = Extensions.DegCos(inputBehindSlowAngle);
    }

    void OnEnable()
    {
        player.BeforeCol += PlayerCollision;
        player.BeforePhys += PlayerControl;
        player.AfterPhys += PlayerControlAfter;
    }

    private void OnDisable()
    {
        player.BeforeCol -= PlayerCollision;
        player.BeforePhys -= PlayerControl;
        player.AfterPhys -= PlayerControlAfter;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("QuickLeft"))
            accOnDesDir = !accOnDesDir;


        if ((Input.GetButtonDown("Jump")|| fakeJump) && player.GetIsGround && !isJumping)
        {
            player.InternalSpeed += player.GetGroundNormal * jumpForce;
            player.physBody.MovePosition(transform.position += (player.GetGroundNormal) * player.tryGroundDistance);
            player.physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, -player.gravityDir));
            //Debug.Log("Jump! + floornor" + player.GetGroundNormal);
            player.GetIsGround = false;
            isJumping = true;
            player.skipNextCol = true;
        }

        if (Input.GetButtonDown("QuickRight") && player.GetIsGround)
        {

            //player.InternalSpeed += transform.InverseTransformDirection(Quaternion.LookRotation(Vector3.right) * input.desiredDir) * quickForce;
            //physBody.MovePosition(transform.position += (averageFloorNor) * 0.2f);
            //physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized));
            //Debug.Log("Jump! + floornor" + averageFloorNor);
            //isGrounded = false;
            //isJumping = true;
            //skipNextCol = true;
        }
        /*if (Input.GetButtonDown("QuickLeft") && player.GetGround)
        {

            player.InternalSpeed += transform.InverseTransformDirection(Quaternion.LookRotation(Vector3.left) * input.desiredDir) * quickForce;
            //physBody.MovePosition(transform.position += (averageFloorNor) * 0.2f);
            //physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized));
            //Debug.Log("Jump! + floornor" + averageFloorNor);
            //isGrounded = false;
            //isJumping = true;
            //skipNextCol = true;
        }*/

        if (Input.GetButtonDown("Roll") && player.GetIsGround && !isJumping)
        {
            isRolling = true;
        }

        if (isJumping && !Input.GetButton("Jump") && Vector3.Dot(player.InternalSpeed, -player.gravityDir) > lowJumpSpeed && Vector3.Dot(player.InternalSpeed, -player.gravityDir) > 0)
        {
            fakeJump = false;
            Vector3 speedUp = Vector3.Project(player.InternalSpeed, -player.gravityDir);
            player.InternalSpeed -= speedUp;
            player.InternalSpeed += speedUp.normalized * lowJumpSpeed;
            player.GetIsGround = false;
            player.physBody.MovePosition(transform.position + (player.GetGroundNormal * 0.1f));
        }

        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(player.InternalSpeed, -player.gravityDir);
            player.InternalSpeed -= speedUp;
            player.InternalSpeed += -player.gravityDir * jumpForce;
            player.controlLockTimer = -1f;
            player.physBody.MovePosition(transform.position + (player.GetGroundNormal * 0.1f));
            player.GetIsGround = false;
            player.skipNextCol = true;
            isJumping = false;
        }

        if(isOnCorrectiveSpline)
        {

            /*
            if (player.InternalSpeed.magnitude > corrSpline.MinSpeed && Vector2.Angle(Vector2.up, input.joyInput.directionRaw) < corrSpline.MinAngle)
            {
                if (!isGettingCorrected)
                {
                    isGettingCorrected = true;
                    Vector3 pos = corrSpline.spline.PutOnPath(player.transform.position, corrSpline.pathMode, out float tim, out splineForw, out Vector3 rgt, out _);
                    splineRightOffset = Vector3.Dot(player.transform.position - pos, rgt);
                    cameraInput.enabled = false;
                }
            }
            else if (isGettingCorrected) {
                isGettingCorrected = false;
                cameraInput.enabled = true;
            }
                

            if (isGettingCorrected)
            {
                Corrective_Update();
            }

            */
        }
    }

    void PlayerCollision()
    {
        player.tryGroundDistance = player.physBody.velocity.magnitude < runSpeed ? tryGroundDistance : tryGroundDistanceFast;
    }

    void Corrective_Update()
    {/*
        corrSpline.spline.PutOnPath(player.transform.position, corrSpline.pathMode, out float tim, out splineForw, out Vector3 rgt, out _);
        //Vector3 acc = path.GetDirection(tim);
        /*input.refPoint.forw = splineForw;
        input.refPoint.rgt = rgt;
        input.refPoint.up = corrSpline.spline.GetNormal(tim);*/
        /*
        input.refPoint.desiredDir = splineForw;*/
    }

    void Corrective_FixedUpdate()
    {
        //Debug.Log("Correcting");
        /*
        splinePos = corrSpline.spline.PutOnPath(player.transform.position, corrSpline.pathMode, out float time, out Vector3 nForw, out Vector3 rgt, out Vector3 mv);

        Debug.DrawRay(splinePos, Vector3.up, Color.blue);

        player.transform.BreakDownSpeed(player.InternalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

        Vector3 disDir = Vector3.Cross(rgt, player.GetGroundNormal).normalized;
        Vector3 pastDir = Vector3.Cross(oldSplineDir, player.GetGroundNormal).normalized;


        disDir = player.transform.InverseTransformDirection(disDir);
        pastDir = player.transform.InverseTransformDirection(pastDir);

        /*Vector3 nor = Vector3.up;
        rgt = (rgt - nor * Vector3.Dot(rgt, nor)).normalized;*/

        /*
        Vector3 nRgt = -Vector3.Cross(nForw, player.GetGroundNormal).normalized;
        nRgt = player.transform.InverseTransformDirection(nRgt);

        //Vector3 nRgt = Quaternion.LookRotation(Vector3.right) * disDir;
        float speedInDir = lateralVelocity.magnitude;


        Vector3 normalVelocity = pastDir * speedInDir;
        Vector3 tangentVelocity = lateralVelocity - normalVelocity;

        float speedToRight = Vector3.Dot(nRgt, tangentVelocity);

        //splineRightOffset += speedToRight * Time.fixedDeltaTime;

        normalVelocity = disDir * speedInDir;

        player.InternalSpeed = player.transform.TransformDirection(normalVelocity + verticalVelocity);

        splineRightOffset = Mathf.MoveTowards(splineRightOffset, 0, corrSpline.centerAtractionForce * Time.fixedDeltaTime);

        player.physBody.MovePosition(splinePos + (rgt * splineRightOffset));
        Debug.DrawRay(splinePos + (rgt * splineRightOffset), Vector3.up, Color.red);
        oldSplineDir = disDir;
        */
    }

    void PlayerControl()
    {
        float evAcc = acc * accOverSpeed.Evaluate(Mathf.Max(0, player.InternalSpeed.magnitude) / (topSpeed));


        if (player.GetIsGround)
        {
            float slopeUpDot = Vector3.Dot(transform.up, player.GetGroundNormal);
            if ((isRolling && player.InternalSpeed.magnitude < rollStopSpeed && slopeUpDot > Extensions.DegCos(player.maxStandAngle)) || isJumping)
                isRolling = false;
            isJumping = false;


            //player.doFriction = !(input.heldPower > 0) || isRolling;

            if (Vector3.Dot(player.GetSlopeVector, player.InternalSpeed.normalized) < 0)
                player.slopeFactor = rollSlopeUpFactor;
            else
                player.slopeFactor = rollSlopeDownFactor;

            if (!isRolling)
                player.slopeFactor = slopeFactor;

            if (!isRolling)
            {
                player.frc = frc;


                switch (moveMethod)
                {
                    case MoveType.damizean:
                        {
                            DoInputDamizean(evAcc, dcc, tangentDrag, topSpeed, player.GetIsControlLock);
                            break;
                        }
                    case MoveType.rotation:
                        {
                            DoInputRota(evAcc, dcc, topSpeed, Mathf.Lerp(rotaModeSpeed, rotaModeSpeedFast, Mathf.Max(0, player.InternalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)), player.GetIsControlLock, rotaModeSpdMul, accOnDesDir);
                            break;
                        }
                    case MoveType.oneDir:
                        {
                            DoInput1Dir(evAcc, dcc, topSpeed, player.GetIsControlLock, Vector3.forward);
                            break;
                        }
                }
            }
            else
            {
                player.frc = rollFrc;
                switch (moveMethod)
                {
                    case MoveType.damizean:
                    case MoveType.rotation:
                        {
                            DoInputRota(0, rollDcl, Mathf.Infinity, rollRotaSpeed, false);
                            break;
                        }
                    case MoveType.forceDir:
                        {
                            DoInput1Dir(0, rollDcl, Mathf.Infinity, false, Vector3.forward);
                            break;
                        }
                }

            }
        }
        else
        {
            switch (jumpMethod)
            {
                case MoveType.damizean:
                    {
                        DoInputDamizean(air, air, jumpTangentDrag, Mathf.Infinity, player.GetIsControlLock);
                        break;
                    }
                case MoveType.rotation:
                    {
                        DoInputRota(air, air, Mathf.Infinity, Mathf.Lerp(rotaModeSpeed, rotaModeSpeedFast, Mathf.Max(0, player.InternalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)), player.GetIsControlLock, rotaModeSpdMul);
                        break;
                    }
                case MoveType.forceDir:
                    {
                        DoInput1Dir(air, air, Mathf.Infinity, player.GetIsControlLock, Vector3.forward);
                        break;
                    }
            }
        }

        if (isJumping && player.GetUpSpeed > 0 && Input.GetButton("Jump"))
            player.gravityForce = upGravityForce;
        else
            player.gravityForce = gravityForce;
    }

    void PlayerControlAfter()
    {
        if (isGettingCorrected)
        {
            Debug.Log("Correcting");
            Corrective_FixedUpdate();
        }
    }

    void DoInputDamizean(float inAcc, float inDcc, float inTangDrag, float inMaxSpeed, bool isControlLock)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        float wrongDelta = Time.fixedDeltaTime;

        Vector3 inputDirection = Vector3.forward;

        // If there is some input...

        if (!isControlLock)
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



            if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, 1))
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
            tangentVelocity = Vector3.MoveTowards(tangentVelocity, Vector3.zero, inTangDrag * wrongDelta * 10);

            Debug.DrawRay(transform.position, transform.TransformDirection(tangentVelocity), Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(transform.position, transform.TransformDirection(normalVelocity), Color.blue, Time.fixedDeltaTime);

            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward), Color.green, Time.fixedDeltaTime);

            player.InternalSpeed = transform.TransformDirection(normalVelocity + tangentVelocity + vSpeed);
            // Compose local velocity back and compute velocity back into the Global frame.
            // You probably want to delay doing this to the end of the physics processing,
            // as transformations can incur into numerical damping of the velocities.
            // The last step is included only for the sake of completeness.
        }
    }

    void DoInputRota(float inAcc, float inDcc, float inMaxSpeed, float rotaSpeed, bool isControlLock, float rotaDecelFactor = 10, bool accOnDesiredDir = false)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        //float wrongDelta = Time.fixedDeltaTime;

        //Vector3 inputDirection = joyInput.direction;

        // If there is some input...


        if (1 != 0 && !isControlLock)
        {
            // Fetch velocity in the Player's local frame, decompose into lateral and vertical
            // motion, and decompose lateral motion further into normal and tangential components.


            transform.BreakDownSpeed(player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);

            

            float normalSpeed = Vector3.Dot(hSpeed, Vector3.forward);
            float dirDif = Vector3.Dot(hSpeed.normalized, Vector3.forward);
            Quaternion currRota = Quaternion.LookRotation(hSpeed.magnitude > 0 ? hSpeed.normalized : Vector3.forward); //Get the current rotation
            Quaternion toRota = Quaternion.LookRotation(Vector3.forward);  //Get the target rotation
            float currSpeed = hSpeed.magnitude;

            bool canTurn = (dirDif > (currSpeed < runSpeed ? -2 : cosBrakeAngle))&&!isBraking;

            Vector3 newDir = Quaternion.RotateTowards(currRota, (canTurn) ? toRota : currRota, rotaSpeed * Time.fixedDeltaTime) * Vector3.forward;

            currSpeed -= Mathf.Abs((currSpeed - Vector3.Dot(newDir, hSpeed))) * rotaDecelFactor;

            Debug.DrawRay(transform.position, transform.TransformDirection(newDir*currSpeed), Color.green, Time.fixedDeltaTime);

            if (normalSpeed < 0)
            {
                isBraking = true;
            }
            else
                isBraking = false;

            if (accOnDesiredDir)
            {
                hSpeed = (newDir * currSpeed) + Vector3.forward * ((isBraking ? inDcc : (currSpeed < inMaxSpeed ? inAcc : 0)) * Time.fixedDeltaTime);
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

            Debug.DrawRay(transform.position, transform.TransformDirection(newDir), Color.green, Time.fixedDeltaTime);
            Debug.DrawRay(transform.position, transform.TransformDirection(toRota * Vector3.forward), (canTurn) ? Color.blue : Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(transform.position, transform.TransformDirection(hSpeed*Time.fixedDeltaTime), Color.black, Time.fixedDeltaTime);



            player.InternalSpeed = transform.TransformDirection(hSpeed + vSpeed);
        }
    }


    void DoInput1Dir(float inAcc, float inDcc, float inMaxSpeed, bool isControlLock, Vector3 disDir)
    {
        float vAcc = Mathf.Min(disDir.magnitude, 1);

        Vector3 rgt = Vector3.right;//Quaternion.LookRotation(Vector3.right) * disDir;
        disDir = Vector3.Cross(rgt, player.GetGroundNormal).normalized;

        disDir = transform.InverseTransformDirection(disDir);
        rgt = Quaternion.LookRotation(Vector3.right) * disDir;



        transform.BreakDownSpeed(player.InternalSpeed, out Vector3 vSpeed, out Vector3 hSpeed);


        hSpeed = Quaternion.LookRotation(disDir) * ((Vector3.Dot(disDir, hSpeed) > 0 ? Vector3.forward : Vector3.back) * hSpeed.magnitude);

        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(disDir), Color.red, Time.fixedDeltaTime);


        if (1 != 0 && !isControlLock)
        {
            Vector3 inDir = Vector3.Dot(disDir, Vector3.forward) > 0 ? disDir : -disDir;
            float normalSpeed = Vector3.Dot(hSpeed, inDir);

            //Debug.Log("Resultado: " + Vector3.Dot(disDir, lateralVelocity).ToString("F2") + " currSPeed =" + normalSpeed.ToString("F2"));



            if (normalSpeed < 0)
                normalSpeed += inDcc * Time.fixedDeltaTime;
            else if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, 1))
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CorrectiveSpline"))
        {
            //corrSpline = other.GetComponent<CorrectiveSpline>();
            isOnCorrectiveSpline = true;
            splineRightOffset = 0;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CorrectiveSpline"))
        {
            /*if (corrSpline == other.GetComponent<CorrectiveSpline>())
            {
                corrSpline = null;
                isOnCorrectiveSpline = false;
                isGettingCorrected = false;
                cameraInput.enabled = true;
            }*/
        }
    }
}
