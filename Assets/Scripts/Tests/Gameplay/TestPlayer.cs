using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    [System.Serializable]
    public struct InputReference
    {
        public Vector3 forw;
        public Vector3 rgt;
        public Vector3 up;
        public Vector3 desiredDir;
    }
    [System.Serializable]
    public struct PlayerInput
    {
        public Vector3 direction;
        public Vector2 directionRaw;
        public float magnitude;
    }

    public event EventHandler BeforePhys, AfterPhys, BeforeSpeed;

    public enum GroundNormalType {bySphere, byRay, basedOnSphere }
    public enum MoveType { damizean, rotation, oneDir, forceDir}
    public enum InputType { catGrav, cat, hedgephy}

    int colCount = 0, ceilCount = 0, wallCount = 0;
    public Rigidbody physBody;
    public CapsuleCollider physShape;
    Vector3 averageFloorNor = Vector3.zero;
    Vector3 averageFloorPos = Vector3.zero;
    Vector3 averageCeiNor = Vector3.zero;
    Vector3 averageCeiPos = Vector3.zero;
    Vector3 averageWallNor = Vector3.zero;
    Vector3 averageWallPos = Vector3.zero;
    Vector3 finalSpeed, oldNormal;
    public float gravityForce;
    public float maxGravityForce;
    public bool fakeInput = false, slopeSpeedAdjust, uploadSpeed;
    bool isGrounded = true, isJumping, isRolling, d_isUpSlope;
    bool skipNextCol;
    public GroundNormalType groundMethod;
    public MoveType moveMethod;
    public InputType controlMethod;
    PlayerInput joyInput;
    public InputReference refPoint;
    public float acc, dcc, frc, air, topSpeed, runSpeed, maxSpeed, quickForce, slopeFactor, inputBehindAngle, maxAngle, maxStandAngle, maxCeiAngle, maxSlipAngle, maxGroundSpeed, tangentDrag, jumpForce, slipSpeedLimit, airDragLimit, fakeX, fakeY, lowJumpSpeed, drag, rollSlopeUpFactor, rollSlopeDownFactor, rollFrc, rollDcl, rollStopSpeed, rollRotaSpeed, tryGroundDistance, tryGroundDistanceFast, rotaModeSpeed, rotaModeSpeedFast, rotaModeSpeedMax, rotaModeSpeedMin, rotaModeSpdMul=10;
    private float cosMaxAngle, cosMinAngle, cosMaxCei, cosSlipAngle, controlLockTimer, d_speed, d_dragCalc, slopeUpDot, cosBrakeAngle;
    Vector3 d_normalSpeed, d_tangentSpeed;

    public bool getJump {get { return isJumping; }
    }
    public bool getRoll{ get { return isRolling; }
    }

    public bool getGround{get { return isGrounded; }
        set { isGrounded = value; }
    }

    public Vector3 getGroundNormal{get { return averageFloorNor; 
        }
    }

    public Vector3 desiredDir{get { return joyInput.direction; }
    }

    public Vector3 internalSpeed {get { return finalSpeed; }
        set { finalSpeed=value; }
    }

    public float heldPower { get { return joyInput.magnitude; } }

    int keepRotation;

    private void Start()
    {
        cosMaxAngle = Extensions.DegCos(maxAngle);
        cosMaxCei = Extensions.DegCos(maxCeiAngle);
        cosMinAngle = Extensions.DegCos(maxStandAngle);
        cosSlipAngle = Extensions.DegCos(maxSlipAngle);
        cosBrakeAngle = Extensions.DegCos(inputBehindAngle);
        joyInput.direction = Vector3.forward;
        joyInput.directionRaw = Vector2.up;
        refPoint.forw = Vector3.forward;
        refPoint.rgt = Vector3.right;
    }

    private void OnGUI()
    {
        // Make a background box
        GUI.Box(new Rect(10, 10, 250, 250), "Sonic data");

        GUI.Label(new Rect(15, 25, 250, 100), "isGrounded =" + isGrounded);
        GUI.Label(new Rect(15, 25+15, 250, 100), "position =" + transform.position);
        GUI.Label(new Rect(15, 25+15+15, 250, 100), "speed magnitude =" + physBody.velocity.magnitude.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15+15, 250, 100), "fall speed =" + Vector3.Dot(finalSpeed, Physics.gravity.normalized).ToString("F2"));
        GUI.Label(new Rect(15, 25+15+15+15+15, 250, 100), "ground angle =" + Vector3.Angle(-Physics.gravity.normalized, averageFloorNor));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15+15+15, 250, 100), "control lock =" + controlLockTimer.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "input Speed =" + d_speed.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15+15+15, 250, 100), "input vel=" + d_normalSpeed);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15+15+15, 250, 100), "input tangent vel=" + d_tangentSpeed);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "air drag=" + d_dragCalc.ToString("F2"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "air drag calc =" + Vector3.Dot(finalSpeed, -Physics.gravity.normalized) + " res " + (Vector3.Dot(finalSpeed, -Physics.gravity.normalized) < airDragLimit));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "isRolling =" + isRolling + " isUpSlope =" + d_isUpSlope);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15+15+15, 250, 100), "rota speed calc =" +  (Mathf.Max(0,finalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)).ToString("F3"));
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15, 250, 100), "movement mode =" + moveMethod);
        GUI.Label(new Rect(15, 25 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15 + 15+15+15, 250, 100), "camera input mode =" + controlMethod);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = controlLockTimer < 0 ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position + (transform.rotation * joyInput.direction), 0.2f);   
    }


    private void OnCollisionStay(Collision col)
    {
        int cacheContacts = col.contactCount;

        for (int i = 0; i < cacheContacts; i++)
        {
            
            if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized) > cosMaxAngle)
            {
                if(colCount==0)
                {
                    averageFloorNor = Vector3.zero;
                    averageFloorPos = Vector3.zero;
                }

                averageFloorNor += col.GetContact(i).normal;
                averageFloorPos += col.GetContact(i).point;
                if (colCount > 0)
                {
                    averageFloorNor *= 0.5f;
                    averageFloorPos *= 0.5f;
                }

                colCount++;
                Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.blue, Time.fixedDeltaTime);
            }
            else if (Vector3.Dot(transform.up, col.GetContact(i).normal.normalized) < cosMaxCei)
            {
                if (ceilCount == 0)
                {
                    averageCeiNor = Vector3.zero;
                    averageCeiPos = Vector3.zero;
                }

                averageCeiNor += col.GetContact(i).normal;
                averageCeiPos += col.GetContact(i).point;
                if (ceilCount > 0)
                {
                    averageCeiNor *= 0.5f;
                    averageCeiPos *= 0.5f;
                }

                ceilCount++;
                //Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.magenta, Time.fixedDeltaTime);
            }
            else 
            {
                if (wallCount == 0)
                {
                    averageWallNor = Vector3.zero;
                    averageWallPos = Vector3.zero;
                }

                averageWallNor += col.GetContact(i).normal;
                averageWallPos += col.GetContact(i).point;
                if (wallCount > 0)
                {
                    averageWallNor *= 0.5f;
                    averageWallPos *= 0.5f;
                }

                float dotFromWall = Vector3.Dot(finalSpeed, -col.GetContact(i).normal);
                Debug.DrawRay(averageWallPos, col.GetContact(i).normal, Color.magenta, Time.fixedDeltaTime);
                if (dotFromWall > 0)
                    finalSpeed += col.GetContact(i).normal * dotFromWall;

                wallCount++;
                //Debug.DrawRay(col.GetContact(i).point, col.GetContact(i).normal, Color.magenta, Time.fixedDeltaTime);
            }
        }

    }

    private void Update()
    {
        Debug.DrawRay(transform.position, finalSpeed, Color.green, Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded && !isJumping)
        {
            finalSpeed += averageFloorNor * jumpForce;
            physBody.MovePosition(transform.position += (averageFloorNor) * 0.2f);
            physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up,-Physics.gravity.normalized));
            Debug.Log("Jump! + floornor" + averageFloorNor);
            isGrounded = false;
            isJumping = true;
            skipNextCol = true;
        }

        if (Input.GetButtonDown("QuickRight") && isGrounded)
        {

            finalSpeed += transform.InverseTransformDirection(Quaternion.LookRotation(Vector3.right) * joyInput.direction) * quickForce;
            physBody.MovePosition(transform.position += (averageFloorNor) * 0.2f);
            //physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized));
            //Debug.Log("Jump! + floornor" + averageFloorNor);
            //isGrounded = false;
            //isJumping = true;
            //skipNextCol = true;
        }
        if (Input.GetButtonDown("QuickLeft") && isGrounded)
        {

            finalSpeed += transform.InverseTransformDirection(Quaternion.LookRotation(Vector3.left) * joyInput.direction) * quickForce;
            physBody.MovePosition(transform.position += (averageFloorNor) * 0.2f);
            //physBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized));
            //Debug.Log("Jump! + floornor" + averageFloorNor);
            //isGrounded = false;
            //isJumping = true;
            //skipNextCol = true;
        }

        if (Input.GetButtonDown("Roll") && isGrounded && !isJumping)
        {
            isRolling = true;
        }

        if (isJumping && !Input.GetButton("Jump") && Vector3.Dot(finalSpeed, -Physics.gravity.normalized) > lowJumpSpeed && Vector3.Dot(finalSpeed, -Physics.gravity.normalized) > 0)
        {
            Vector3 speedUp = Vector3.Project(finalSpeed, -Physics.gravity.normalized);
            finalSpeed -= speedUp;
            finalSpeed += speedUp.normalized * lowJumpSpeed;
        }
    }

    private void FixedUpdate()
    {
        if (skipNextCol)
        {
            ResetCol();
        }
        ClearCol();
        NormalCollisionChecks();
        skipNextCol = false;
        NormalInputChecks();
        BeforePhys?.Invoke(this, new EventArgs());
        NormalPhysics();
        if (Input.GetButton("Fly"))
        {
            Vector3 speedUp = Vector3.Project(finalSpeed, -Physics.gravity.normalized);
            finalSpeed -= speedUp;
            finalSpeed += -Physics.gravity.normalized * jumpForce;
            controlLockTimer = -1f;
            transform.position += (averageFloorNor) * 0.1f;
            isGrounded = false;
            colCount = 0;
            isJumping = false;
        }

        AfterPhys?.Invoke(this, new EventArgs());
        BeforeSpeed?.Invoke(this, new EventArgs());
        if(uploadSpeed)
        physBody.velocity = finalSpeed;
        ResetCol();
        
    }

    void NormalInputChecks()
    {
        switch (controlMethod)
        {
            case InputType.catGrav:
                HandleInput();
                break;
            case InputType.hedgephy:
                HandleInputHP();
                break;
            case InputType.cat:
                HandleInputNoGrav();
                break;
        }
    }

    void NormalCollisionChecks()
    {
        if(!skipNextCol)
            DoColNormal();

        if (colCount > 0)
            isGrounded = true;
        else if (isGrounded)
            isGrounded = TryGround();

        Debug.DrawRay(averageFloorPos, averageFloorNor, Color.green, Time.fixedDeltaTime);
        //Caching slopeUpDot and doing groundrotation
        if (isGrounded)
        {
            Vector3 ySpeed = Vector3.zero, xSpeed = Vector3.zero;
            if (slopeSpeedAdjust)
                transform.BreakDownSpeed(finalSpeed, out ySpeed, out xSpeed);

            slopeUpDot = Vector3.Dot(-Physics.gravity.normalized, averageFloorNor);

            transform.rotation = Quaternion.FromToRotation(Vector3.up, averageFloorNor);
            oldNormal = averageFloorNor;
            keepRotation = 0;
            if (controlLockTimer >= 0 && !isRolling)
            {
                controlLockTimer -= Time.fixedDeltaTime;
            }
            if ((isRolling && finalSpeed.magnitude < rollStopSpeed && slopeUpDot > cosMinAngle) || isJumping)
                isRolling = false;
            isJumping = false;

            if (slopeSpeedAdjust)
            {
                finalSpeed = transform.TransformDirection(xSpeed + ySpeed);
            }


        }
        else
        {
            keepRotation += 1;
            if (keepRotation < 5)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, oldNormal);
            }
            else
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, averageFloorNor);
            }
        }
    }

    void NormalPhysics()
    {
        DoSlopes();

        if (isGrounded)
        {
            if(!isRolling)
                switch(moveMethod)
                {
                    case MoveType.damizean:
                        {
                            DoInputDamizean(acc, dcc, tangentDrag, topSpeed, controlLockTimer);
                            break;
                        }
                    case MoveType.rotation:
                        {
                            DoInputRota(acc, dcc, topSpeed, Mathf.Lerp(rotaModeSpeed, rotaModeSpeedFast, Mathf.Max(0, finalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)), controlLockTimer, rotaModeSpdMul);
                            break;
                        }
                    case MoveType.oneDir:
                        {
                            DoInput1Dir(acc, dcc, topSpeed, controlLockTimer, refPoint.desiredDir);
                            break;
                        }
                }
                
            else
            {
                switch (moveMethod)
                {
                    case MoveType.damizean:
                    case MoveType.rotation:
                        {
                            DoInputRota(0, rollDcl, Mathf.Infinity, rollRotaSpeed, -1);
                            break;
                        }
                    case MoveType.forceDir:
                        {
                            DoInput1Dir(0, rollDcl, Mathf.Infinity, -1, refPoint.desiredDir);
                            break;
                        }
                }

            }
        }
        else
            switch (moveMethod)
            {
                case MoveType.damizean:
                    {
                        DoInputDamizean(air, air, 0, Mathf.Infinity, controlLockTimer);
                        break;
                    }
                case MoveType.rotation:
                    {
                        DoInputRota(air, air, Mathf.Infinity, Mathf.Lerp(rotaModeSpeed, rotaModeSpeedFast, Mathf.Max(0, finalSpeed.magnitude - rotaModeSpeedMin) / (rotaModeSpeedMax - rotaModeSpeedMin)), controlLockTimer, rotaModeSpdMul);
                        break;
                    }
                case MoveType.forceDir:
                    {
                        DoInput1Dir(air, air, Mathf.Infinity, controlLockTimer, refPoint.desiredDir);
                        break;
                    }
            }
        

        if (isGrounded)
            DoFriction();
        else
            DoAirFriction();

        DoGravity();

        DoStopOnWall();
    }

    void DoColNormal()
    {
        switch(groundMethod)
        {
            case GroundNormalType.byRay:
                {
                    ColByRay(-transform.up);
                    break;
                }
            case GroundNormalType.basedOnSphere:
                {
                    ColByRay(-averageFloorNor);
                    break;
                }
        }

    }

    void ColByRay(Vector3 direction)
    {
        RaycastHit hit;
        Vector3 capsuleTop, capsuleBottom;
        physShape.ToWorldSpaceCapsule(out capsuleTop, out capsuleBottom, out _);

        if (Physics.Raycast(transform.position, direction, out hit, 0.1f))
        {
            if (Vector3.Dot(transform.up, hit.normal) > cosMaxAngle)
            {
                averageFloorPos = hit.point;
                averageFloorNor = hit.normal;
                colCount=1;
                transform.position = hit.point + (hit.normal * ((physShape.height / 2) - physShape.center.y));
            }
        }
        else
        {
            averageFloorNor = -Physics.gravity.normalized;
            averageFloorPos = Vector3.zero;
            colCount = 0;
        }
    }
    

    bool TryGround()
    {
        RaycastHit hit;
        Vector3 capsuleTop, capsuleBottom;
        physShape.ToWorldSpaceCapsule(out capsuleTop, out capsuleBottom, out _);


        if(Physics.Raycast(transform.position + (transform.up*0.05f), -transform.up, out hit, physBody.velocity.magnitude < maxGroundSpeed ? tryGroundDistance: tryGroundDistanceFast))
        {
            if (Vector3.Dot(transform.up, hit.normal) > cosMaxAngle)
            {
                colCount=1;
                averageFloorPos = hit.point;
                averageFloorNor = hit.normal;
                physBody.MovePosition(hit.point + (hit.normal * ((physShape.height / 2)-physShape.center.y)));
                return true;
            }
            else
                return false;
                
        }
        return false;
    }

    void HandleInputNoGrav()
    {
        Vector2 joystick;
        joystick.x = Input.GetAxis("Horizontal");
        joystick.y = Input.GetAxis("Vertical");

        if (fakeInput)
        {
            joystick.x = fakeX;
            joystick.y = fakeY;
        }

        joyInput.magnitude = joystick.magnitude;
        

        if (joyInput.magnitude > 0)
        {
            joyInput.directionRaw = joystick.normalized;
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Camera.main.transform.right;
            right.y = 0f;
            right.Normalize();
            joyInput.direction = forward * joyInput.directionRaw.y + right * joyInput.directionRaw.x;
        }
    }

    void HandleInput()
    {
        Vector2 joystick;
        joystick.x = Input.GetAxis("Horizontal");
        joystick.y = Input.GetAxis("Vertical");

        if (fakeInput)
        {
            joystick.x = fakeX;
            joystick.y = fakeY;
        }

        joyInput.magnitude = joystick.magnitude;

        if (joyInput.magnitude > 0)
        {
            joyInput.directionRaw = joystick.normalized;
            //From CatLikeCoding' ball tutorial.

            Vector3 forward = Extensions.ProjectDirectionOnPlane(refPoint.forw, refPoint.up);
            Vector3 right = Extensions.ProjectDirectionOnPlane(refPoint.rgt, refPoint.up);
            joyInput.direction = (forward * joyInput.directionRaw.y + right * joyInput.directionRaw.x).normalized;
            joyInput.direction = transform.InverseTransformDirection(Quaternion.FromToRotation(refPoint.up, averageFloorNor) * joyInput.direction);
        }
    }

    void HandleInputHP()
    {
        Vector2 joystick;
        joystick.x = Input.GetAxis("Horizontal");
        joystick.y = Input.GetAxis("Vertical");
        joyInput.magnitude = joystick.magnitude;

        joystick = joystick.normalized;

        Vector3 moveInp = new Vector3(joystick.x, 0, joystick.y);
        Transform cam = Camera.main.transform;

        if (joyInput.magnitude > 0)
        {
            Vector3 transformedInput = Quaternion.FromToRotation(cam.up, averageFloorNor) * (cam.rotation * moveInp);
            transformedInput = transform.InverseTransformDirection(transformedInput);
            transformedInput.y = 0.0f;
            joyInput.direction = transformedInput;
        }
    }

    void DoStopOnWall()
    {
        if (ceilCount > 0)
        {
            //Vector3 speedToWall = Vector3.Project(finalSpeed, averageWallNor);
            float dotFromCei = Vector3.Dot(finalSpeed, -averageCeiNor);
            if (dotFromCei > 0)
                finalSpeed += averageCeiNor * dotFromCei;
        }
    }

    void DoFriction()
    {
        var localVelocity = transform.InverseTransformDirection(finalSpeed);
        var lateralVelocity = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
        var verticalVelocity = new Vector3(0.0f, localVelocity.y, 0.0f);

        if (isRolling ||(joyInput.magnitude<float.Epsilon && lateralVelocity.magnitude > 0))
        {
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, (isRolling ? rollFrc : frc) * Time.fixedDeltaTime);

        }

        localVelocity = lateralVelocity + verticalVelocity;
        finalSpeed = transform.TransformDirection(localVelocity);
    }

    void DoAirFriction()
    {
        var localVelocity = transform.InverseTransformDirection(finalSpeed);
        var lateralVelocity = new Vector3(localVelocity.x, 0.0f, localVelocity.z);
        var verticalVelocity = new Vector3(0.0f, localVelocity.y, 0.0f);

        if (lateralVelocity.magnitude > 0 && Vector3.Dot(finalSpeed, -Physics.gravity.normalized) < airDragLimit)
        {
            //d_dragCalc = ((lateralVelocity.magnitude % 0.125f) / 256); drag calc from Sonic games. Had a hard time understanding. wont use it.

            d_dragCalc = Mathf.Pow(lateralVelocity.magnitude,2) * drag; // The variable 

            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, d_dragCalc*Time.fixedDeltaTime);
        }

        localVelocity = lateralVelocity + verticalVelocity;
        finalSpeed = transform.TransformDirection(localVelocity);
    }

    void DoInputDamizean(float inAcc, float inDcc, float inTangDrag, float inMaxSpeed, float inControlLock)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        float wrongDelta = Time.fixedDeltaTime;

        Vector3 inputDirection = joyInput.direction;

        // If there is some input...

        if (joyInput.magnitude != 0 && inControlLock < 0)
        {
            // Fetch velocity in the Player's local frame, decompose into lateral and vertical
            // motion, and decompose lateral motion further into normal and tangential components.


            transform.BreakDownSpeed(finalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

            float normalSpeed = Vector3.Dot(lateralVelocity, inputDirection);
            Vector3 normalVelocity = inputDirection * normalSpeed;
            Vector3 tangentVelocity = lateralVelocity - normalVelocity;

            // Note: normalSpeed is the magnitude of normalVelocity, with the added
            // bonus that it's signed. If positive, the speed goes towards the same
            // direction than the input :)
            


            if (normalSpeed < Mathf.Lerp(0,inMaxSpeed, joyInput.magnitude))
            {
                // Accelerate towards the input direction.
                    normalSpeed += inDcc * wrongDelta;

                normalSpeed = Mathf.Min(normalSpeed, inMaxSpeed);

                normalVelocity = inputDirection * normalSpeed;
            }

            // Additionally, we can apply some drag on the tangent directions for
            // tighter control.
            tangentVelocity = Vector3.MoveTowards(tangentVelocity, Vector3.zero, inTangDrag * wrongDelta * joyInput.magnitude);


            //Debug.DrawRay(averageFloorPos, transform.TransformDirection(tangentVelocity), Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(averageFloorPos, transform.TransformDirection(normalVelocity), Color.blue, Time.fixedDeltaTime);

            d_speed = normalSpeed;
            d_normalSpeed = normalVelocity;
            d_tangentSpeed = tangentVelocity;

            // Compose local velocity back and compute velocity back into the Global frame.
            // You probably want to delay doing this to the end of the physics processing,
            // as transformations can incur into numerical damping of the velocities.
            // The last step is included only for the sake of completeness.
            finalSpeed = transform.TransformDirection(normalVelocity + tangentVelocity + verticalVelocity);
        }
    }

    void DoInputRota(float inAcc, float inDcc, float inMaxSpeed, float rotaSpeed, float inControlLock, float rotaDecelFactor = 10)
    {
        //Original by Damizean

        // We assume input is already in the Player's local frame...

        //float wrongDelta = Time.fixedDeltaTime;

        //Vector3 inputDirection = joyInput.direction;

        // If there is some input...

        if (joyInput.magnitude != 0 && inControlLock < 0)
        {
            // Fetch velocity in the Player's local frame, decompose into lateral and vertical
            // motion, and decompose lateral motion further into normal and tangential components.


            transform.BreakDownSpeed(finalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

            float normalSpeed = Vector3.Dot(lateralVelocity, joyInput.direction);
            Quaternion currRota = Quaternion.LookRotation(lateralVelocity.magnitude > 0 ? lateralVelocity.normalized : joyInput.direction); //Get the current rotation
            Quaternion toRota = Quaternion.LookRotation(joyInput.direction);  //Get the target rotation
            float currSpeed = lateralVelocity.magnitude;
            Vector3 newDir = Quaternion.RotateTowards(currRota, normalSpeed > cosBrakeAngle ? toRota : currRota, rotaSpeed * Time.fixedDeltaTime) * Vector3.forward;

            Debug.Log("Resultado: " + Vector3.Dot(lateralVelocity, newDir).ToString("F2") + " currSPeed =" + currSpeed.ToString("F2"));

            currSpeed -= Mathf.Abs((currSpeed - Vector3.Dot(lateralVelocity, newDir)))*rotaDecelFactor;


            if (normalSpeed < cosBrakeAngle)
                currSpeed -= inDcc * Time.fixedDeltaTime;
            else if(currSpeed < inMaxSpeed)
                currSpeed += inAcc * Time.fixedDeltaTime;

            lateralVelocity = newDir * currSpeed;

            d_speed = normalSpeed;
            d_normalSpeed = lateralVelocity;
            d_tangentSpeed = Vector3.zero;

            //Debug.DrawRay(averageFloorPos, transform.TransformDirection(toRota*Vector3.forward), Color.red, Time.fixedDeltaTime);
            Debug.DrawRay(averageFloorPos, transform.TransformDirection(lateralVelocity), Color.blue, Time.fixedDeltaTime);


            finalSpeed = transform.TransformDirection(lateralVelocity + verticalVelocity);
        }
    }


    void DoInput1Dir(float inAcc, float inDcc, float inMaxSpeed, float inControlLock, Vector3 disDir)
    {
        transform.BreakDownSpeed(finalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

        float vAcc = Mathf.Min(disDir.magnitude,1);

        Vector3 rgt = refPoint.rgt;//Quaternion.LookRotation(Vector3.right) * disDir;
        disDir =Vector3.Cross(rgt, averageFloorNor).normalized;
        
        disDir = transform.InverseTransformDirection(disDir);
        rgt = Quaternion.LookRotation(Vector3.right) * disDir;






        lateralVelocity = Quaternion.LookRotation(disDir) * ((Vector3.Dot(disDir, lateralVelocity)>0?Vector3.forward:Vector3.back) * lateralVelocity.magnitude);

        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(disDir), Color.red, Time.fixedDeltaTime);


        if (joyInput.magnitude != 0 && inControlLock < 0)
        {
            Vector3 inDir = Vector3.Dot(disDir, joyInput.direction) > 0 ? disDir : -disDir;
            float normalSpeed = Vector3.Dot(lateralVelocity, inDir);

            //Debug.Log("Resultado: " + Vector3.Dot(disDir, lateralVelocity).ToString("F2") + " currSPeed =" + normalSpeed.ToString("F2"));



            if (normalSpeed < 0)
                normalSpeed += inDcc * Time.fixedDeltaTime;
            else if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, joyInput.magnitude))
                normalSpeed += inAcc * Time.fixedDeltaTime;

            lateralVelocity = inDir*normalSpeed;

            d_speed = normalSpeed;
            d_normalSpeed = lateralVelocity;
            d_tangentSpeed = Vector3.zero;

            //Debug.DrawRay(averageFloorPos, transform.TransformDirection(toRota * Vector3.forward), Color.red, Time.fixedDeltaTime);
        }
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(lateralVelocity), Color.blue, Time.fixedDeltaTime);

        finalSpeed = transform.TransformDirection(lateralVelocity*vAcc + verticalVelocity);

        //Vector3 rgt = Quaternion.LookRotation(Vector3.right) * disDir;
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(rgt), Color.red, Time.fixedDeltaTime);
        float dotFromRight = Vector3.Dot(finalSpeed, rgt);
        Debug.Log("dotfromright = " + dotFromRight.ToString("F3") + " vacc " + vAcc);
        finalSpeed -= rgt * dotFromRight;
    }

    void DoInputForceDir(float inAcc, float inDcc, float inMaxSpeed, float inControlLock, Vector3 disDir)
    {
        transform.BreakDownSpeed(finalSpeed, out Vector3 verticalVelocity, out Vector3 lateralVelocity);

        float vAcc = Mathf.Min(disDir.magnitude, 1);

        Vector3 rgt = refPoint.rgt;//Quaternion.LookRotation(Vector3.right) * disDir;
        disDir = Vector3.Cross(rgt, averageFloorNor).normalized;

        disDir = transform.InverseTransformDirection(disDir);
        rgt = Quaternion.LookRotation(Vector3.right) * disDir;






        lateralVelocity = Quaternion.LookRotation(disDir) * ((Vector3.Dot(disDir, lateralVelocity) > 0 ? Vector3.forward : Vector3.back) * lateralVelocity.magnitude);

        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(disDir), Color.red, Time.fixedDeltaTime);


        if (joyInput.magnitude != 0 && inControlLock < 0)
        {
            Vector3 inDir = Vector3.Dot(disDir, joyInput.direction) > 0 ? disDir : -disDir;
            float normalSpeed = Vector3.Dot(lateralVelocity, inDir);

            //Debug.Log("Resultado: " + Vector3.Dot(disDir, lateralVelocity).ToString("F2") + " currSPeed =" + normalSpeed.ToString("F2"));



            if (normalSpeed < 0)
                normalSpeed += inDcc * Time.fixedDeltaTime;
            else if (normalSpeed < Mathf.Lerp(0, inMaxSpeed, joyInput.magnitude))
                normalSpeed += inAcc * Time.fixedDeltaTime;

            lateralVelocity = inDir * normalSpeed;

            d_speed = normalSpeed;
            d_normalSpeed = lateralVelocity;
            d_tangentSpeed = Vector3.zero;

            //Debug.DrawRay(averageFloorPos, transform.TransformDirection(toRota * Vector3.forward), Color.red, Time.fixedDeltaTime);
        }
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(lateralVelocity), Color.blue, Time.fixedDeltaTime);

        finalSpeed = transform.TransformDirection(lateralVelocity * vAcc + verticalVelocity);

        //Vector3 rgt = Quaternion.LookRotation(Vector3.right) * disDir;
        //Debug.DrawRay(averageFloorPos, transform.TransformDirection(rgt), Color.red, Time.fixedDeltaTime);
        float dotFromRight = Vector3.Dot(finalSpeed, rgt);
        Debug.Log("dotfromright = " + dotFromRight.ToString("F3") + " vacc " + vAcc);
        finalSpeed -= rgt * dotFromRight;
    }

    void DoSlopes()
    {
        // based on-> https://stackoverflow.com/a/4372760 
        if (joyInput.magnitude!=0 || isRolling || slopeUpDot < cosMinAngle)
        {
            Vector3 slope = (Physics.gravity.normalized - Vector3.Dot(Physics.gravity.normalized, averageFloorNor) * averageFloorNor).normalized;

            float slp;

            d_isUpSlope = (Vector3.Dot(slope, finalSpeed.normalized) < 0);

            if (Vector3.Dot(slope, finalSpeed.normalized) < 0)
                slp = rollSlopeUpFactor;
            else
                slp = rollSlopeDownFactor;

            if (!isRolling)
                slp = slopeFactor;

            //Debug.DrawRay(averageFloorPos, slope, Color.blue, Time.fixedDeltaTime);

            finalSpeed += slope * (slp * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-Physics.gravity.normalized, averageFloorNor))))) * Time.fixedDeltaTime;
        }

        if (finalSpeed.magnitude < slipSpeedLimit && controlLockTimer < 0 && slopeUpDot < cosSlipAngle)
        {
            isGrounded = false;
            colCount = 0;
            controlLockTimer = 0.5f;
            if (slopeUpDot < cosMaxAngle)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized);
                oldNormal = -Physics.gravity.normalized;
                MoveAbovePoint(averageFloorPos, averageFloorNor);
            }
        }
    }

    void DoSlopesPhys()
    {
        if (joyInput.magnitude != 0 || isRolling || slopeUpDot < cosMinAngle)
        {
            // based on-> https://stackoverflow.com/a/4372760 
            Vector3 downGrav = Physics.gravity.normalized * gravityForce;

            Vector3 slope = (Physics.gravity.normalized  - Vector3.Dot(Physics.gravity.normalized, averageFloorNor) * averageFloorNor).normalized;

            float slp;

            d_isUpSlope = (Vector3.Dot(slope, finalSpeed.normalized) < 0);

            if (Vector3.Dot(slope, finalSpeed.normalized) < 0)
                slp = rollSlopeUpFactor;
            else
                slp = rollSlopeDownFactor;

            if (!isRolling)
                slp = slopeFactor;

            //Debug.DrawRay(averageFloorPos, slope, Color.blue, Time.fixedDeltaTime);

            finalSpeed += slope;// * (slp * (Mathf.Abs(Extensions.DegSin(Vector3.Angle(-Physics.gravity.normalized, averageFloorNor))))) * Time.fixedDeltaTime;
        }

        if (finalSpeed.magnitude < slipSpeedLimit && controlLockTimer < 0 && slopeUpDot < cosSlipAngle)
        {
            isGrounded = false;
            colCount = 0;
            controlLockTimer = 0.5f;
            if (slopeUpDot < cosMaxAngle)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, -Physics.gravity.normalized);
                oldNormal = -Physics.gravity.normalized;
                MoveAbovePoint(averageFloorPos, averageFloorNor);
            }
        }
    }

    void DoGravity()
    {
        if (isGrounded)
        {
            var verticalVelocity = new Vector3(0.0f, 0.0f, 0.0f);

            transform.BreakDownSpeed(finalSpeed, out _, out Vector3 lateralVelocity);

            finalSpeed = transform.TransformDirection(lateralVelocity + verticalVelocity);
        }
        else if (Vector3.Dot(finalSpeed, Physics.gravity.normalized) < (maxGravityForce))
        {
            finalSpeed += Physics.gravity.normalized * gravityForce * Time.fixedDeltaTime;
        }

        //Debug.Log(Vector3.Dot(finalSpeed, Physics.gravity.normalized) + " < " + (maxGravityForce) + " = " + (Vector3.Dot(finalSpeed, Physics.gravity.normalized) < (maxGravityForce)));
    }

    void MoveAbovePoint(Vector3 point, Vector3 normal)
    {

        float yFactor = Vector3.Angle(normal, transform.up);
        float xFactor = Vector3.Angle(normal, transform.right);
        float zFactor = Vector3.Angle(normal, transform.forward);

        yFactor = Mathf.Abs(1 - (yFactor >= 180 ? yFactor - 180 : yFactor) / 90);
        xFactor = Mathf.Abs(1 - (xFactor >= 180 ? xFactor - 180 : xFactor) / 90);
        zFactor = Mathf.Abs(1 - (zFactor >= 180 ? zFactor - 180 : zFactor) / 90);


        float fYPos = ((physShape.height / 2) * yFactor + physShape.radius * xFactor + physShape.radius * zFactor);

        physBody.MovePosition(transform.position + normal*fYPos + transform.rotation * -physShape.center);
    }

    void ClearCol()
    {
        if (colCount==0)
        {
            averageFloorNor = -Physics.gravity.normalized;
            averageFloorPos = Vector3.zero;
        }
        if (ceilCount == 0)
        {
            averageCeiNor = Physics.gravity.normalized;
            averageCeiPos = Vector3.zero;
        }

        if (wallCount == 0)
        {
            averageWallNor = -transform.forward;
            averageWallPos = Vector3.zero;
        }
    }

    void ResetCol()
    {
        wallCount = 0;
        ceilCount = 0;
        colCount = 0;
    }
}
