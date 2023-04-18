using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Blitz
{
    [System.Serializable]
    public struct PlayerMotion
    {
        public Vector3 speed, align;
        public float direction;
        public bool ground;
        public Vector3 colDirection;
        public float colDistance;
    }

    [System.Serializable]
    public struct PlayerCollision
    {
        public float dotResul, ceilTest, groundTest, frontTest, frontFactor;
        public bool align, shouldAlign, colResult;

        public Vector3 tempColNormal, tempGroundNormal, tempCeilNormal, tempSpeedNormal;
    }

    [System.Serializable]
    public struct PlayerInput
    {
        public Vector2 dirinput;
        public float dirInputAngle, dirInputPress;

    }

    [System.Serializable]
    public struct PlayerFlags
    {
        public bool allowCommonInput, allowXZMove,
            allowYMove, allowSkid, isHurt,
            isShield, isInvin, isSneakers,
            isSkid;
    }

    [System.Serializable]
    public enum PlayerAction { Common, Jump, Crouch, Spindash, Roll, Custom };

    [System.Serializable]
    public enum PlayerControlMode { Normal, sideView, hardSpline, correctSpline, loopSpline };


    public class PlayerBase : MonoBehaviour
    {
        //Common values
        [Header("Common Values")]
        public float valMulti = 1;
        public float u_baseXZAccel = 0.024f;
        public float u_baseXZDeccel = 0.012f,
            u_skidFactor = 0.005f,
            u_xZTopSpeed = 2.6f,
            u_xZMaxSpeed = 7.5f,
            u_baseYAccel = 0.04f,
            u_yTopSpeed = -3.0f;

        public float baseXZAccel
        {
            get { return u_baseXZAccel * valMulti; }
            set { u_baseXZAccel = value; }
        }

        public float baseXZDeccel
        {
            get { return u_baseXZDeccel * valMulti; }
            set { u_baseXZDeccel = value; }
        }

        public float skidFactor
        {
            get { return u_skidFactor * valMulti; }
            set { u_skidFactor = value; }
        }

        public float xZTopSpeed
        {
            get { return u_xZTopSpeed * valMulti; }
            set { u_xZTopSpeed = value; }
        }

        public float xZMaxSpeed
        {
            get { return u_xZMaxSpeed * valMulti; }
            set { u_xZMaxSpeed = value; }
        }

        public float baseYAccel
        {
            get { return u_baseYAccel * valMulti; }
            set { u_baseYAccel = value; }
        }

        public float yTopSpeed
        {
            get { return u_yTopSpeed * valMulti; }
            set { u_yTopSpeed = value; }
        }


        public LayerMask groundLayer;

        float xZAccel, xZDeccel, yAccel;

        [Header("Motion Values")]
        const float dotGround = 0.65f,
        dotCeil = -0.65f,
        dotCeilStop = -0.79f,
        dotWallUp = -0.7f,
        dotWallDown = 0.2f,
        dotWallDir = 0.3f;

        public PlayerMotion playerMotion;
        public PlayerFlags playerFlags;
        public PlayerInput playerInput;
        public PlayerCollision collision;

        public PlayerControlMode playerControl = PlayerControlMode.Normal;

        public PlayerAction playerAction = PlayerAction.Common;

        //Jump values
        [Header("Jump Values")]
        public float u_jumpStrenght = 1.4f;

        public float jumpStrenght
        {
            get { return u_jumpStrenght * valMulti; }
            set { u_jumpStrenght = value; }
        }

        public float jumpStrenghtFactor = 0.7f;

        [Header("References")]
        public CapsuleCollider col;
        public GameObject mesh;

        float debugGroundDot, debugWallDot, debugCeilDot;
        int debugColQuan;
        string debugCollisionObject;
        Vector3 debugGroundNorm;


        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 200, 120), "Debug Data");
            GUI.Label(new Rect(15, 20, 200, 30), "Collisions: " + debugColQuan);
            GUI.Label(new Rect(15, 40, 200, 30), "Ground Dot: " + debugGroundDot);
            GUI.Label(new Rect(15, 60, 200, 30), "Ground Vector: " + debugGroundNorm);
            GUI.Label(new Rect(15, 80, 200, 30), "Wall Dot: " + debugWallDot);
            GUI.Label(new Rect(15, 100, 200, 30), "Col name: " + debugCollisionObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            PlayerCreate();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            PlayerInput();

            PlayerMotion();

            PlayerHandle();

            switch (playerAction)
            {
                case PlayerAction.Common:
                    {
                        PlayerAction_Common();
                        break;
                    }
                case PlayerAction.Jump:
                    {
                        PlayerAction_Jump();
                        break;
                    }
                default:
                    {
                        TestAction();
                        break;
                    }
            }

            ResetColData();

            Debug.DrawRay(transform.position + col.center, transform.rotation * (playerMotion.speed.normalized * (playerMotion.speed.magnitude + 1)), Color.blue);
        }

        /*private void LateFixedUpdate()
        {

        }*/

        void PlayerInput()
        {
            playerInput.dirinput = new Vector2(Input.GetAxis("Vertical"), -Input.GetAxis("Horizontal"));
            playerInput.dirInputPress = playerInput.dirinput.magnitude;
            if (playerInput.dirInputPress > 0)
                playerInput.dirInputAngle = playerInput.dirinput.InputAngle();

        }

        void PlayerCreate()
        {
            playerMotion.speed = Vector3.zero;
            playerMotion.align = Vector3.up;
            playerMotion.ground = false;
            playerMotion.direction = Quaternion.identity.eulerAngles.y;
            playerMotion.colDirection = Vector3.down;
            playerMotion.colDistance = float.Epsilon;

            playerFlags.allowCommonInput = true;
            playerFlags.allowXZMove = true;
            playerFlags.allowYMove = true;

            playerInput.dirInputAngle = 0;

        }

        //Player Motion
        void PlayerMotion()
        {
            bool Grounded = TestCollision();

            if (Grounded)
            {
                PlayerAlign();
                if (!playerMotion.ground)
                {
                    PlayerAirToGround();
                    playerMotion.ground = true;
                }
            }
            else
            {
                if (playerMotion.ground)
                {
                    PlayerGroundToAir();
                    playerMotion.ground = false;
                }
                PlayerAlign();
            }


            Vector3 ogPos = transform.position;

            if (playerMotion.ground)
                transform.Translate(playerMotion.speed.x, playerMotion.speed.y * (0.015f + (playerMotion.speed.magnitude * 0.33f)), playerMotion.speed.z);
            else
                transform.Translate(playerMotion.speed);

            playerMotion.colDirection = (transform.position - ogPos);
            playerMotion.colDistance = Vector3.Distance(ogPos, transform.position);
        }

        private void OnCollisionStay(Collision hits)
        {
            //Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.magenta);

            foreach (var hit in hits.contacts)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.red, Time.deltaTime);

                bool shouldAlign = true;
                Vector3 tempColNormal = hit.normal.normalized;
                collision.dotResul = Vector3.Dot(hit.normal.normalized, playerMotion.align);
                //Test ground
                if (collision.dotResul > collision.groundTest && (playerMotion.speed.y <= 0.0f || playerMotion.ground))
                {
                    if (shouldAlign)
                    {
                        collision.align = true;
                        collision.tempGroundNormal += tempColNormal;
                    }
                    collision.groundTest = collision.dotResul;
                    debugGroundDot = collision.groundTest;
                }

                //Test ceiling
                if (collision.dotResul < collision.ceilTest && playerMotion.speed.y > 0.0f)
                {

                    collision.tempCeilNormal += tempColNormal;
                    collision.ceilTest = collision.dotResul;
                    debugCeilDot = collision.dotResul;
                }

                //Test Wall
                if (collision.dotResul >= dotWallUp && collision.dotResul <= dotWallDown)
                {
                    collision.dotResul = (1 - Mathf.Abs(Vector3.Dot(Vector3.Cross(collision.tempGroundNormal, playerMotion.align), collision.tempSpeedNormal)));
                    if (collision.dotResul > collision.frontTest)
                        collision.frontTest = collision.dotResul;
                    debugWallDot = collision.dotResul;
                }

                transform.position += hit.normal * hit.separation;
            }

        }

        bool OldTestCollision()
        {
            //Collider[] collisions = Physics.OverlapCapsule(transform.position, transform.position + (Vector3.up * col.height), col.radius, groundLayer);

            //RaycastHit [] collisions =  Physics.CapsuleCastAll(transform.position, transform.position + (Vector3.up * col.height), col.radius, playerMotion.speed.normalized, (playerMotion.speed * Time.deltaTime).magnitude, groundLayer);
            float distanceToPoints = col.height / 2 - col.radius;
            Vector3 point1 = transform.position + (col.center + transform.up * distanceToPoints);
            Vector3 point2 = transform.position + (col.center - transform.up * distanceToPoints);


            RaycastHit[] collisions = Physics.CapsuleCastAll(point1, point2, col.radius, Vector3.down, float.Epsilon, groundLayer);

            /*RaycastHit hitTest;
            if (Physics.Raycast(transform.position + col.center, Vector3.down, out hitTest, col.height / 2, groundLayer))
            {
                Debug.DrawRay(hitTest.point, hitTest.normal.normalized * 2f, Color.magenta);
            }*/

            float dotResul, ceilTest = -dotCeil, groundTest = dotGround, frontTest = dotWallDir, frontFactor = 0;
            bool align = false, shouldAlign = false, colResult = false;

            Vector3 tempColNormal = Vector3.zero;
            Vector3 tempGroundNormal = Vector3.zero;
            Vector3 tempCeilNormal = Vector3.zero;
            Vector3 tempSpeedNormal = playerMotion.speed.normalized;

            debugColQuan = collisions.Length;

            foreach (var hit in collisions)
            {
                //hit.ClosestPoint()

                //Debug.DrawRay(hit.point, hit.normal.normalized * 2f, Color.magenta);
                shouldAlign = true;

                tempColNormal = hit.normal.normalized;
                dotResul = Vector3.Dot(hit.normal.normalized, playerMotion.align);
                //Debug.Log("dotResul = " + dotResul);
                //Test ground
                if (dotResul > groundTest && (playerMotion.speed.y <= 0.0f || playerMotion.ground))
                {
                    if (shouldAlign)
                    {
                        align = true;
                        tempGroundNormal += tempColNormal;
                    }
                    groundTest = dotResul;
                    debugGroundDot = groundTest;
                    debugCollisionObject = hit.collider.gameObject.name;
                }

                //Test ceiling
                if (dotResul < ceilTest && playerMotion.speed.y > 0.0f)
                {

                    tempCeilNormal += tempColNormal;
                    ceilTest = dotResul;
                    debugCeilDot = dotResul;
                }

                //Test Wall
                if (dotResul >= dotWallUp && dotResul <= dotWallDown)
                {
                    dotResul = (1 - Mathf.Abs(Vector3.Dot(Vector3.Cross(tempGroundNormal, playerMotion.align), tempSpeedNormal)));
                    if (dotResul > frontTest)
                        frontTest = dotResul;
                    debugWallDot = dotResul;
                }
                Vector3 disDir;
                float disDis;

                if (Physics.ComputePenetration(col, transform.position, transform.rotation, hit.collider, hit.collider.transform.position, hit.collider.transform.rotation, out disDir, out disDis))
                {
                    transform.position += disDir.normalized * disDis;
                }
            }
            //Debug.DrawRay(transform.position, Vector3.up, Color.blue);

            //Debug.DrawRay(transform.position+col.center, tempGroundNormal*1, Color.magenta);
            debugGroundNorm = tempGroundNormal;

            if (frontTest > dotWallDir)
            {
                Debug.Log("Wall Collision");
                frontFactor = 1 - Mathf.Min(((frontTest - dotWallDir) / (1 - dotWallDir)) * Time.deltaTime * 1.2f, 1.0f);
                playerMotion.speed = new Vector3(playerMotion.speed.x * frontFactor, playerMotion.speed.y, playerMotion.speed.z * frontFactor);
            }

            if (groundTest > dotGround)
            {
                if (align)
                    playerMotion.align = tempGroundNormal.normalized;
                else
                    playerMotion.align = -Physics.gravity.normalized;
                colResult = true;
            }
            else if (ceilTest < dotCeil)
            {
                if (ceilTest < dotCeilStop)
                {
                    playerMotion.speed = new Vector3(playerMotion.speed.x, 0.0f, playerMotion.speed.z);
                    colResult = false;
                }
                else
                {
                    playerMotion.align = tempCeilNormal.normalized;
                }
            }
            else
            {
                playerMotion.align = -Physics.gravity.normalized;
                colResult = false;
            }


            return colResult;
        }

        void ResetColData()
        {
            collision.ceilTest = -dotCeil;
            collision.groundTest = dotGround;
            collision.frontTest = dotWallDir;
            collision.frontFactor = 0;
            collision.align = false;
            collision.shouldAlign = false;

            collision.tempGroundNormal = Vector3.zero;
            collision.tempCeilNormal = Vector3.zero;
            collision.tempSpeedNormal = playerMotion.speed.normalized;
        }

        bool TestCollision()
        {
            bool colResult = false;
            debugGroundNorm = collision.tempGroundNormal;

            if (collision.frontTest > dotWallDir)
            {
                Debug.Log("Wall Collision");
                collision.frontFactor = 1 - Mathf.Min(((collision.frontTest - dotWallDir) / (1 - dotWallDir)) * 1.2f, 1.0f);
                playerMotion.speed = new Vector3(playerMotion.speed.x * collision.frontFactor, playerMotion.speed.y, playerMotion.speed.z * collision.frontFactor);
            }

            if (collision.groundTest > dotGround)
            {
                if (collision.align)
                    playerMotion.align = collision.tempGroundNormal.normalized;
                else
                    playerMotion.align = -Physics.gravity.normalized;
                colResult = true;
            }
            else if (collision.ceilTest < dotCeil)
            {
                if (collision.ceilTest < dotCeilStop)
                {
                    playerMotion.speed = new Vector3(playerMotion.speed.x, 0.0f, playerMotion.speed.z);
                    colResult = false;
                }
                else
                {
                    playerMotion.align = collision.tempCeilNormal.normalized;
                }
            }
            else
            {
                playerMotion.align = -Physics.gravity.normalized;
                colResult = false;
            }


            return colResult;
        }

        void PlayerAlign()
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, playerMotion.align);
        }

        void PlayerAirToGround()
        {
            Debug.Log("Player Landed");
            Vector3 tempSpeed = Matrix4x4.Rotate(transform.rotation).inverse.MultiplyVector(playerMotion.speed);
            playerMotion.speed = new Vector3(tempSpeed.x, 0, tempSpeed.z);
        }

        void PlayerGroundToAir()
        {
            Debug.Log("Player Tookoff");
            Vector3 tempSpeed = Matrix4x4.Rotate(transform.rotation).MultiplyVector(playerMotion.speed);

            playerMotion.speed = tempSpeed;
        }

        //Player Management

        void PlayerHandle()
        {
            float motionDirection = 0;
            float motionPressure = 0;

            switch (playerControl)
            {
                default:
                    {
                        motionDirection = Camera.main.transform.rotation.eulerAngles.y - playerInput.dirInputAngle;
                        motionPressure = playerInput.dirInputPress;
                        break;
                    }
            }

            Vector3 acc = new Vector3(Extensions.DegCos(motionDirection) * motionPressure, 0f, Extensions.DegSin(motionDirection) * motionPressure);
            Vector3 speed = new Vector3(playerMotion.speed.x, 0f, playerMotion.speed.z);
            Vector3 speedComp = Vector3.zero;
            float speedLenght = speed.magnitude;

            playerFlags.isSkid = false;

            //If there is acceleration, handle it and change direction
            if (acc.magnitude > 0)
            {
                //Calculate delta cos and sin
                float deltaCos = Extensions.DegCos(playerMotion.direction - 90);
                float deltaSin = Extensions.DegSin(playerMotion.direction - 90);

                //Change player direction
                if (speedLenght < 0.1)
                    playerMotion.direction = (Mathf.Atan2(((acc.x + deltaCos * 3) / 4) * 1.0001f, -(acc.z + deltaSin * 3) / 4)) * Mathf.Rad2Deg;
                else
                    playerMotion.direction = (Mathf.Atan2(((acc.x + deltaCos * 5) / 6) * 1.0001f, -(acc.z + deltaSin * 5) / 6)) * Mathf.Rad2Deg;

                //Dot of new direction and current direction
                float dirDot = Vector3.Dot(acc.normalized, speed.normalized);

                if (dirDot < 0.0f)
                {
                    //If the change if opposite of current diection
                    if (playerMotion.ground)
                        acc *= 1.2f;
                    if (speedLenght > 0.4)
                    {
                        //skid
                    }

                    Vector3.MoveTowards(speed, Vector3.zero, 0.06f * Time.fixedDeltaTime);
                }
                else if (dirDot < 0.4f)
                {
                    //If theres a harsh change in direction, decrease greatly current direction, increase acceleration on the new
                    if (playerMotion.ground)
                    {
                        speedComp = new Vector3((speed.x * 33 + deltaCos * speedLenght) / 34 * 0.96f, 0f, (speed.z * 33 + deltaSin * speedLenght) / 34 * 0.96f);
                        speed = Vector3.Lerp(speed, speedComp, Time.fixedDeltaTime);
                    }
                    else
                        Vector3.MoveTowards(speed, Vector3.zero, 0.02f);

                    acc *= 1.2f;
                }
                else if (dirDot < 0.95)
                {
                    //If theres a mild change in direction, decrese motion slightly.
                    if (playerMotion.ground)
                        speedComp = new Vector3((speed.x * 19 + deltaCos * speedLenght) / 20, 0f, (speed.z * 19 + deltaSin * speedLenght) / 20);
                    else
                        speedComp = new Vector3((speed.x * 21 + deltaCos * speedLenght) / 22 * 0.98f, 0f, (speed.z * 21 + deltaSin * speedLenght) / 22 * 0.98f);

                    speed = Vector3.Lerp(speed, speedComp, Time.fixedDeltaTime);
                }

                if (speedLenght <= xZTopSpeed)
                    speed += (acc * xZAccel);
            }

            playerMotion.speed = new Vector3(speed.x, playerMotion.speed.y, speed.z);

            if (playerMotion.ground)
                PlayerAngleAcceleration();

            if (speedLenght > 0.0f)
            {
                float x = playerMotion.speed.x;
                if (x > 0.0f)
                    x = Mathf.Max(x - (x / speedLenght) * xZDeccel, 0);
                else
                    x = Mathf.Min(x - (x / speedLenght) * xZDeccel, 0);

                float z = playerMotion.speed.z;
                if (z > 0.0f)
                    z = Mathf.Max(z - (z / speedLenght) * xZDeccel, 0);
                else
                    z = Mathf.Min(z - (z / speedLenght) * xZDeccel, 0);

                playerMotion.speed = new Vector3(x, playerMotion.speed.y, z);
            }

            if (!playerMotion.ground)
                playerMotion.speed = new Vector3(playerMotion.speed.x, Mathf.Max(playerMotion.speed.y - (yAccel), yTopSpeed), playerMotion.speed.z);
            else
                playerMotion.speed = new Vector3(playerMotion.speed.x, 0f, playerMotion.speed.z);



        }

        void PlayerAngleAcceleration()
        {
            if (Mathf.Abs((playerMotion.align.y)) <= 0.7f)
                playerMotion.speed = new Vector3(Mathf.Pow(playerMotion.speed.x + playerMotion.align.x, 2) * 0.04f * Mathf.Sign(playerMotion.align.x) * Time.fixedDeltaTime, playerMotion.speed.y, Mathf.Pow(playerMotion.speed.z + playerMotion.align.z, 2) * 0.04f * Mathf.Sign(playerMotion.align.z));

            if (playerMotion.align.y <= 0.1 && (playerMotion.speed.magnitude * 10 < (2.0f * valMulti)))
            {
                PlayerGroundToAir();
                playerMotion.align = -Physics.gravity.normalized;
            }
        }

        // Player Actions

        void TestAction()
        {
            xZAccel = baseXZAccel;
            xZDeccel = baseXZDeccel;
            yAccel = baseYAccel;
        }

        void PlayerAction_Common()
        {
            xZAccel = baseXZAccel;
            xZDeccel = baseXZDeccel;
            yAccel = baseYAccel;

            if (Input.GetKey(KeyCode.Space) && playerMotion.ground)
            {
                playerAction = PlayerAction.Jump;
                playerMotion.speed = new Vector3(playerMotion.speed.x * 0.7f, jumpStrenght, playerMotion.speed.z * 0.7f);
                PlayerGroundToAir();
                playerMotion.ground = false;

                playerMotion.align = Vector3.up;
            }
        }

        void PlayerAction_Jump()
        {
            if (playerMotion.ground)
                playerAction = PlayerAction.Common;
        }
    }
}