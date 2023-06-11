using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float radSpeed, azimSpeed, elevSpeed, elevSpeedAir, defRad, defAzim, defElev, defElevJump, lookSpeed, lookHorSpeed,fovSpeed,fovNormal,fovFast,maxAngle,angleSpeed, playerOffset, minRunSpeed, maxRunSpeed, upAdjustSpeed,elevPhysSpeed, timeToAuto, autoTransTime, followSpeed, followAmmount;
    float rad, azim, elev, radVel, azimVel, elevVel,fovVel,currFov,currentUpDot, lookSpeedVel, currAutoTime, autoAmmount,autoVel;
    public bool followOnSlopes,autoAdjustElev,physAdjustElev;
    public PlayerAdventure player;
    public Camera cam;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentUp=Vector3.up, currentDir = Vector3.up, upVel, lookVel, lookHorVel,lastPlayerPos, followVel,followOffset;

    private void Start()
    {
        lastPlayerPos = player.transform.position;
        currFov = fovNormal;
        currentUpDot = 1;
    }
    private void LateUpdate() {
        Vector3 localCamPos;
        
        float currElevSpeed=elevSpeed;
        float currRadSpeed=radSpeed;
        float currDefElev = defElev;
        //Calculamos la velocidad del jugador. Ya que es interpolada, nos dara un mejor resultado.
        Vector3 playerVelocity = (player.transform.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.transform.position;

        //Calculamos la velocidad acorde al eje del piso
        Vector3 groundVelocity = Vector3.ProjectOnPlane(playerVelocity, Vector3.up);
        float groundSpeed = groundVelocity.magnitude;

        currAutoTime -= Time.deltaTime;
        
        cam.transform.position += Vector3.Lerp(groundVelocity, Vector3.zero, autoAmmount)*Time.deltaTime;
        
        

        float upSpeedDot = Vector3.Dot(Vector3.up, playerVelocity);

        if(player.actionState.StateName == "Jump"){
            cam.transform.position += Vector3.up * (upSpeedDot * Time.deltaTime);
                //currRadSpeed = 0.05f;
            
            currDefElev = defElevJump;
            currElevSpeed = elevSpeedAir;
            
            
        }

        Debug.Log("y: " + groundVelocity.y + " elev: " + currDefElev);

        if(followOnSlopes&&player.player.GetIsGround){
            cam.transform.position += Vector3.up * (upSpeedDot * Time.deltaTime);
        }
        
        
        

        //Calculamos el FOV actual en base a la velocidad minima y maxima.
        currFov = Mathf.SmoothDamp(currFov, Mathf.Lerp(fovNormal, fovFast, Mathf.Max(0, groundSpeed - minRunSpeed) / (maxRunSpeed-minRunSpeed)), ref fovVel, fovSpeed);
        cam.fieldOfView = currFov;

        
        //currentUpDot = Mathf.MoveTowards(currentUpDot, globalUpDot, upAdjustSpeed*Time.deltaTime);

        //Suavizamos el cambio en el arriba de Sonic, y ademas lo limitamos para que solo cambie si supera los 10 grados
        currentUp = Vector3.SmoothDamp(currentUp, Vector3.Angle(Vector3.up, player.transform.up)  < maxAngle ? Vector3.up : player.transform.up, ref upVel, angleSpeed);
        float globalUpDot = Vector3.Dot(currentUp, Vector3.up);

        followOffset = Vector3.SmoothDamp(followOffset, groundVelocity*followAmmount, ref followVel, followSpeed);

        Vector3 focusCenter = player.transform.position + (Vector3.up*globalUpDot*playerOffset) + followOffset;
        //Definimos la matriz local para que sea el centro de Sonic, mas un offset solo en el eje vertical. Si Sonic esta de lado, el offset se queda en ceros, en lugar de moverse a la derecha, ya que eso marea un poco
        localMat.SetTRS(focusCenter, Quaternion.FromToRotation(Vector3.up, currentUp), Vector3.one);
        Debug.DrawRay(player.transform.position, focusCenter-player.transform.position, Color.magenta);

        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        
        //Obtenemos la posicion de la camara en coordenadas esfericas
        Vector3 sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;


        rad = Mathf.Lerp(rad, Mathf.SmoothDamp(rad, defRad, ref radVel, currRadSpeed),1);

        //Calculamos un angulo de elevacion que nos deje a la altura deseada. Tenemos que limitar la altura para que no supere al radio.
        //Si para obtener la altura de un punto a cierto radio y angulo se hace radio por sin del angulo
        //Para obtener el angulo de un punto a cierto radio y altura se hace arcsin de altura entre radio
        float targetElev = elev;
        if(localCamPos.y < currDefElev)
            targetElev = Mathf.Asin(Mathf.Max(Mathf.Min(autoAdjustElev? currDefElev : localCamPos.y, rad-Mathf.Epsilon),-(rad-Mathf.Epsilon))/rad);
        
        elev = Mathf.Lerp(elev, Mathf.Deg2Rad*Mathf.SmoothDampAngle(elev*Mathf.Rad2Deg, targetElev*Mathf.Rad2Deg, ref elevVel, currElevSpeed),autoAmmount);
        if(elev > Mathf.Deg2Rad*80)
            elev = Mathf.Deg2Rad*80;
        //Despues de calcular la nueva posicion, la devolvemos al plano cartesiano
        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);
        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        
        localMat.SetTRS(focusCenter, cam.transform.rotation, Vector3.one);
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        
        //Obtenemos la posicion de la camara en coordenadas esfericas
        sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;

        float xMove = Input.GetAxis("Mouse X");
        float yMove = Input.GetAxis("Mouse Y");
        azim += Mathf.Deg2Rad*(-xMove*azimSpeed);
        elev += Mathf.Deg2Rad*(-yMove*azimSpeed);

        if(xMove != 0 || yMove != 0){
            autoVel= 0;
            currAutoTime=timeToAuto;
            autoAmmount=0;
        }

        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);
        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        

        Vector3 toPlayerDir = (focusCenter - cam.transform.position).normalized;
        Vector3 cameraHorDir = cam.transform.forward;
        Vector3 cameraVerDir = Vector3.Project(cameraHorDir, Vector3.up);
        cameraHorDir -= cameraVerDir;

        Vector3 toPlayerVertDir = Vector3.Project(toPlayerDir, Vector3.up);
        toPlayerDir -= toPlayerVertDir;

        cameraHorDir = Vector3.SmoothDamp(cameraHorDir, toPlayerDir, ref lookHorVel, lookHorSpeed*autoAmmount);
        cameraVerDir = Vector3.SmoothDamp(cameraVerDir, toPlayerVertDir, ref lookVel, lookSpeed*autoAmmount);


        Vector3 cameraUp = cam.transform.up;
        //Cambiamos el eje vertical de la camara para evitar rotaciones aleatorias
        Vector3 upAngle = Mathf.Abs(Vector3.Dot(toPlayerDir, Vector3.up)) > 0.95f ? player.transform.up : Vector3.up;

        cam.transform.rotation = Quaternion.LookRotation((cameraHorDir+cameraVerDir), upAngle);

        if(currAutoTime <= 0)
            autoAmmount = Mathf.SmoothDamp(autoAmmount, 1, ref autoVel, autoTransTime);

    }

    
}
