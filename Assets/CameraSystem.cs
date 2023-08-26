using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float radSpeed, radSpeedClose, azimSpeed, azimStickSpeed, elevSpeed, elevSpeedAir, defRad, closeRad, tooCloseRad, defAzim, defElev, defElevJump, lookSpeed, lookHorSpeed, fovSpeed, fovNormal, fovFast, maxAngle, topAngle, angleSpeed, playerOffset, minRunSpeed, maxRunSpeed, upAdjustSpeed, elevPhysSpeed, timeToAuto, autoTransTime, followSpeed, followAmmount, cameraRad, heightAllowTime, manualAdjustTime, cameraFlattenAmmount, cameraLocalAmmount;
    float rad, azim, elev, radVel, azimVel, elevVel, fovVel, currFov, currentSonicUpDot, currAutoTime, autoAmmount, autoVel, frozenRad, heightAllow, heightAllowVel;
    public bool followOnSlopes, autoAdjustElev, physAdjustElev;
    bool isJumpingState;
    public PlayerAdventure player;
    public Camera cam;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentSonicUp = Vector3.up, currentDir = Vector3.up, upVel, lookVel, lookHorVel, lastPlayerPos, followVel, followOffset;
    RaycastHit cameraCol;
    public LayerMask cameraLayers;


    private float userFov = 65.0f, userLocal = 0.85f;
    bool userDinFov = false;
    bool userLookAhead = false;
    bool userLookAheadCam = false;
    public float rightMargin, cameraWait;
    public Rect AlignRight(Rect rect)
    {
        rect.x = Screen.width - rect.width - rect.x;
        return rect;
    }

    string cameraState;

    void OnGUI()
    {
        GUI.Box(AlignRight(new Rect(0, 20, 205, 250)), "");
        GUI.Label(AlignRight(new Rect(45, 25, 100, 25)), "Angulo de vision");
        userFov = GUI.HorizontalSlider(AlignRight(new Rect(45, 50, 100, 25)), userFov, 40.0f, 70.0f);
        GUI.Label(AlignRight(new Rect(5, 45, 35, 25)), userFov.ToString("F02"));
        //GUI.Label(AlignRight(new Rect (50, 75, 100, 25)), "Angulo dinamico: ");
        userDinFov = GUI.Toggle(AlignRight(new Rect(5, 75, 145, 25)), userDinFov, "Angulo dinamico?");
        userLookAhead = GUI.Toggle(AlignRight(new Rect(10, 100, 180, 25)), userLookAhead, "Apuntar adelante de Sonic?");
        userLookAheadCam = GUI.Toggle(AlignRight(new Rect(10, 125, 180, 25)), userLookAheadCam, "Centrar delante de Sonic?");
        GUI.Label(AlignRight(new Rect(1.5f, 150, 200, 25)), "Estabilizacion de giro de camara");
        userLocal = GUI.HorizontalSlider(AlignRight(new Rect(45, 175, 100, 25)), userLocal, 0.5f, 0.95f);
        GUI.Label(AlignRight(new Rect(5, 170, 35, 25)), userLocal.ToString("F02"));
        GUI.Label(AlignRight(new Rect(1.5f, 200, 200, 25)), "Retardo de apuntado de camara");
        lookHorSpeed = GUI.HorizontalSlider(AlignRight(new Rect(45, 225, 100, 25)), lookHorSpeed, 0f, 0.5f);
        GUI.Label(AlignRight(new Rect(5, 225, 35, 25)), lookHorSpeed.ToString("F02"));

        if(GUI.Button(AlignRight(new Rect(75, 250, 65, 25)), "Estilo 1"))
        {
            userLookAhead = false;
            userLookAheadCam = false;
            userLocal = 0.90f;
            lookHorSpeed = 0.15f;
        }
        if (GUI.Button(AlignRight(new Rect(5, 250, 65, 25)), "Estilo 2"))
        {
            userLookAhead = true;
            userLookAheadCam = false;
            userLocal = 0.5f;
            lookHorSpeed = 0.35f;
        }
    }
    private void Start()
    {
        lastPlayerPos = player.transform.position;
        currFov = fovNormal;
        currentSonicUpDot = 1;
        autoAmmount = 1;
    }
    private void LateUpdate()
    {
        Vector3 localCamPos;
        bool fixHighCamera = false;
        float currElevSpeed = elevSpeed;
        float currRadSpeed = radSpeed;
        float currRadTarget = defRad;
        float currDefElev = defElev;

        Vector3 camPrevPos = cam.transform.position;


        //Calculamos la velocidad del jugador. Ya que es interpolada, nos dara un mejor resultado.
        Vector3 playerVelocity = (player.transform.position - lastPlayerPos - player.physPly.ConnDiff) / Time.deltaTime;
        lastPlayerPos = player.transform.position;

        //Calculamos la velocidad acorde al eje del piso
        Vector3 groundVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        float groundSpeed = groundVelocity.magnitude;

        //Suavizamos el cambio en el arriba de Sonic, y ademas lo limitamos para que solo cambie si supera los 10 grados
        Vector3 upTarget = Vector3.Angle(Vector3.up, player.transform.up) < maxAngle ? Vector3.up : player.transform.up;
        currentSonicUp = Vector3.SmoothDamp(currentSonicUp, upTarget, ref upVel, angleSpeed);
        if ((currentSonicUp - upTarget).sqrMagnitude < 0.01)
        {
            currentSonicUp = upTarget;
            upVel = Vector3.zero;
        }


        float currentUpDot = Vector3.Dot(currentSonicUp, Vector3.up);
        float dotVelFacing = Vector3.Dot(groundVelocity, cam.transform.forward);
        float upSpeedDot = Vector3.Dot(Vector3.up, playerVelocity);
        float realUpDot = Vector3.Dot(player.transform.up, Vector3.up);

        //Definimos la matriz local para que sea el centro de Sonic, mas un offset solo en el eje vertical. Si Sonic esta de lado, el offset se queda en ceros, en lugar de moverse a la derecha, ya que eso marea un poco
        Vector3 followTarget = (dotVelFacing > 0 ? groundVelocity : Vector3.Lerp(groundVelocity, Vector3.ProjectOnPlane(groundVelocity, cam.transform.forward), cameraFlattenAmmount)) * (player.actionState.StateName == "Jump" || !userLookAhead ? 0 : followAmmount);
        if (userLookAheadCam)
            followTarget = Vector3.Project(followTarget, cam.transform.forward);

        followOffset = Vector3.SmoothDamp(followOffset, followTarget, ref followVel, followSpeed);

        //Debug.Log("follow test: " + (followOffset - followTarget).sqrMagnitude);
        if ((followOffset - followTarget).sqrMagnitude < 0.01)
        {
            followOffset = followTarget;
            followVel = Vector3.zero;
        }

        Vector3 focusCenter = player.transform.position + (Vector3.up * currentUpDot * playerOffset) + followOffset;

        Vector3 planarFocusCenter = Vector3.ProjectOnPlane(focusCenter, player.transform.up);
        Vector3 planarCamera = Vector3.ProjectOnPlane(cam.transform.position, player.transform.up);
        float planCamDistance = player.physPly.GetIsGround ? (planarFocusCenter - planarCamera).magnitude : (focusCenter - cam.transform.position).magnitude;


        Debug.DrawRay(player.transform.position + followOffset, (Vector3.up * currentUpDot * playerOffset), Color.magenta);
        Debug.DrawRay(player.transform.position, followOffset, Color.magenta);


        currAutoTime -= Time.deltaTime;
        if (planCamDistance < tooCloseRad && dotVelFacing < 0 && currAutoTime < 0.1f)
        {
            //currAutoTime = 0.1f;
            autoAmmount = 0f;
        }

        float stopLocalFollow = autoAmmount;
        if (player.actionState.StateName == "Jump")
            stopLocalFollow = 0;
        if (planCamDistance < closeRad && dotVelFacing > 0)
        {
            stopLocalFollow = 1;
        }
        cam.transform.position += Vector3.Lerp(groundVelocity, groundVelocity * userLocal, stopLocalFollow) * Time.deltaTime;

        cam.transform.position += player.physPly.ConnDiff;




        if (player.actionState.StateName == "Jump" && autoAmmount > 0f)
        {
            cameraState = "Jump";
            if (!isJumpingState)
            {
                frozenRad = rad;
                isJumpingState = true;
            }
            currRadTarget = frozenRad;
            currRadSpeed = 0;
            cam.transform.position += Vector3.up * (upSpeedDot * Time.deltaTime);
            if(groundSpeed > 10f){
                currDefElev = defElevJump;
                currElevSpeed = elevSpeedAir;
            }
        }
        else isJumpingState = false;

        //Debug.Log("updote: " + realUpDot + "/" + Extensions.DegCos(topAngle) + ", Is valid slope: " + (followOnSlopes && player.player.GetIsGround && realUpDot > Extensions.DegCos(topAngle)) + ", is too close" + (planCamDistance < tooCloseRad && dotVelFacing < 0) + ", is auto: " + (autoAmmount < 0.5f && !isJumpingState));
        if ((followOnSlopes && player.physPly.GetIsGround && realUpDot > Extensions.DegCos(topAngle)) || (planCamDistance < tooCloseRad && dotVelFacing < 0) || (autoAmmount < 1f && !isJumpingState))
        {
            cam.transform.position += Vector3.up * (upSpeedDot * Time.deltaTime);
        }

        if (realUpDot < Extensions.DegCos(topAngle))
        {
            fixHighCamera = true;
        }

        //Calculamos el FOV actual en base a la velocidad minima y maxima.
        currFov = Mathf.SmoothDamp(currFov, Mathf.Lerp(userFov, userDinFov ? userFov + 15 : userFov, Mathf.Max(0, groundSpeed - minRunSpeed) / (maxRunSpeed - minRunSpeed)), ref fovVel, fovSpeed);
        cam.fieldOfView = currFov;

        //////////////////////////////////////////////// Camera user rotation
        localMat.SetTRS(focusCenter, cam.transform.rotation, Vector3.one);
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);

        //Obtenemos la posicion de la camara en coordenadas esfericas
        Vector3 sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;

        if (Cursor.lockState != CursorLockMode.None)
        {
            float xMove = Input.GetAxis("Mouse X");
            float yMove = Input.GetAxis("Mouse Y");
            azim += Mathf.Deg2Rad * (-xMove * azimSpeed);
            elev += Mathf.Deg2Rad * (-yMove * azimSpeed);

            bool moved = xMove != 0 || yMove != 0;

            xMove = Input.GetAxis("CamStickX");
            yMove = Input.GetAxis("CamStickY");
            azim += Mathf.Deg2Rad * (-xMove * azimStickSpeed * Time.deltaTime);
            elev += Mathf.Deg2Rad * (yMove * azimStickSpeed * Time.deltaTime);

            moved |= xMove != 0 || yMove != 0;


            if (moved)
            {
                if (autoAmmount > 0)
                    frozenRad = rad;
                autoVel = 0;
                currAutoTime = timeToAuto;
                autoAmmount = 0;
                isJumpingState = false;

                cameraState += "Manual Moving";
            }

            if (autoAmmount < 0.9f && rad > defRad)
                rad = defRad;

        }

        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);
        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);




        if (autoAmmount > 0f)
        {
            ////////////////////////////////////////////////////CHASE CAMERA
            localMat.SetTRS(focusCenter, Quaternion.FromToRotation(Vector3.up, currentSonicUp), Vector3.one);

            localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);

            //Obtenemos la posicion de la camara en coordenadas esfericas
            sphereCoord = Extensions.CartesianToSpherical(localCamPos);
            rad = sphereCoord.x;
            azim = sphereCoord.y;
            elev = sphereCoord.z;

            if (!isJumpingState)
            {
                cameraState = "Far Mode";
                if (rad < defRad)
                {
                    currRadTarget = rad;
                    cameraState = "Normal Mode";
                }
                if (rad < closeRad)
                {
                    fixHighCamera = true;
                    currRadTarget = closeRad;
                    currRadSpeed = radSpeedClose;
                    cameraState = "Close Mode";
                }
            }

            rad = Mathf.SmoothDamp(rad, currRadTarget, ref radVel, currRadSpeed);
            
            if (Mathf.Abs(currRadTarget - rad) < 0.0005f)
                rad = currRadTarget;
            //Calculamos un angulo de elevacion que nos deje a la altura deseada. Tenemos que limitar la altura para que no supere al radio.
            //Si para obtener la altura de un punto a cierto radio y angulo se hace radio por sin del angulo
            //Para obtener el angulo de un punto a cierto radio y altura se hace arcsin de altura entre radio
            float targetElev = elev;
            if (player.actionState.currentState != "Fall")
                targetElev = Mathf.Asin(Mathf.Max(Mathf.Min(autoAdjustElev ? currDefElev : localCamPos.y, rad - Mathf.Epsilon), -(rad - Mathf.Epsilon)) / rad);

            heightAllow = Mathf.SmoothDamp(heightAllow, (localCamPos.y < currDefElev || fixHighCamera) ? 1 : 0, ref heightAllowVel, heightAllowTime);
            if (Mathf.Abs(((localCamPos.y < currDefElev || fixHighCamera) ? 1 : 0) - heightAllow) < 0.0005f)
                heightAllow = (localCamPos.y < currDefElev || fixHighCamera) ? 1 : 0;

            //Debug.Log("Camera Height: " + localCamPos.y + "/" + currDefElev + ", heightAllow: " + heightAllow);


            elev = Mathf.Lerp(elev, Mathf.Deg2Rad * Mathf.SmoothDampAngle(elev * Mathf.Rad2Deg, targetElev * Mathf.Rad2Deg, ref elevVel, currElevSpeed), autoAmmount * heightAllow);

            if (Mathf.Abs(targetElev - elev) < 0.0005f)
                elev = targetElev;

            //Despues de calcular la nueva posicion, la devolvemos al plano cartesiano
            localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);
            cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        }
        else
        {
            cameraState = "Manual";
        }

        /////////////////////////////////////////////////////////////////////////////////////Camera Collision

        Vector3 camDisp = (cam.transform.position - camPrevPos);
        if (Physics.SphereCast(camPrevPos, cameraRad + 0.0001f, camDisp.normalized, out cameraCol, camDisp.magnitude, cameraLayers))
        {
            //Debug.Log("Collided! " + cameraCol.point + ", campos: " + cam.transform.position);
            camDisp = Vector3.ProjectOnPlane(camDisp, cameraCol.normal);
            cam.transform.position = camPrevPos + camDisp;
            //cam.transform.position =  cameraCol.point + cameraCol.normal * cameraRad;
        }


        /////////////////////////////////////////////////////////////////////////////////////CAMERA LOOKAT
        Vector3 toPlayerDir = (focusCenter - cam.transform.position).normalized;
        Vector3 cameraHorDir = cam.transform.forward;
        Vector3 cameraVerDir = Vector3.Project(cameraHorDir, Vector3.up);
        cameraHorDir -= cameraVerDir;

        Vector3 toPlayerVertDir = Vector3.Project(toPlayerDir, Vector3.up);
        Vector3 toPlayerHorDir = toPlayerDir - toPlayerVertDir;

        cameraHorDir = Vector3.SmoothDamp(cameraHorDir, toPlayerHorDir, ref lookHorVel, lookHorSpeed * autoAmmount);
        cameraVerDir = Vector3.SmoothDamp(cameraVerDir, toPlayerVertDir, ref lookVel, lookSpeed * autoAmmount);

        //Cambiamos el eje vertical de la camara para evitar rotaciones aleatorias

        Vector3 upAngle = Vector3.Lerp(Vector3.up, player.transform.up, (Mathf.Clamp01(Mathf.Abs(Vector3.Dot(toPlayerDir, Vector3.up)) - 0.8f) * 5f));
        cam.transform.rotation = Quaternion.LookRotation((cameraHorDir + cameraVerDir), upAngle);

        if (currAutoTime <= 0)
            autoAmmount = Mathf.SmoothDamp(autoAmmount, 1, ref autoVel, autoTransTime);
        if (1 - autoAmmount < 0.01f)
            autoAmmount = 1;

    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        GUIStyle STYLE = new GUIStyle();
        STYLE.fontSize = 20;

        Handles.Label(player.transform.position + cam.transform.right, cameraState, STYLE);
    }
#endif
}