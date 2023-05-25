using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public float radSpeed, azimSpeed, elevSpeed, defRad, defAzim, defElev, lookSpeed,fovSpeed,fovNormal,fovFast,maxAngle,angleSpeed, playerOffset, minRunSpeed, maxRunSpeed;
    float rad, azim, elev, radVel, azimVel, elevVel,fovVel,currFov;
    public PlayerAdventure player;
    public Camera cam;
    Matrix4x4 localMat = Matrix4x4.identity;
    Vector3 currentUp=Vector3.up, currentDir = Vector3.up, upVel, lookVel,lastPlayerPos;

    private void Start()
    {
        lastPlayerPos = player.transform.position;
        currFov = fovNormal;
    }
    private void LateUpdate() {
        Vector3 localCamPos;

        //Calculamos la velocidad del jugador. Ya que es interpolada, nos dara un mejor resultado.
        Vector3 playerVelocity = (player.transform.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.transform.position;

        //Calculamos la velocidad acorde al eje del piso
        Vector3 groundVelocity = Vector3.ProjectOnPlane(playerVelocity, currentUp);
        float groundSpeed = groundVelocity.magnitude;

        //Calculamos el FOV actual en base a la velocidad minima y maxima.
        currFov = Mathf.SmoothDamp(currFov, Mathf.Lerp(fovNormal, fovFast, Mathf.Max(0, groundSpeed - minRunSpeed) / (maxRunSpeed-minRunSpeed)), ref fovVel, fovSpeed);
        cam.fieldOfView = currFov;

        float globalUpDot = Vector3.Dot(player.transform.up, Vector3.up);

        //Suavizamos el cambio en el arriba de Sonic, y ademas lo limitamos para que solo cambie si supera los 10 grados
        currentUp = Vector3.SmoothDamp(currentUp, Vector3.Angle(Vector3.up, player.transform.up)  < maxAngle ? Vector3.up : player.transform.up, ref upVel, angleSpeed);
        
        //Definimos la matriz local para que sea el centro de Sonic, mas un offset solo en el eje vertical. Si Sonic esta de lado, el offset se queda en ceros, en lugar de moverse a la derecha, ya que eso marea un poco
        localMat.SetTRS(player.transform.position + (Vector3.up*globalUpDot*playerOffset), Quaternion.FromToRotation(Vector3.up, currentUp), Vector3.one);
        
        localCamPos = localMat.inverse.MultiplyPoint3x4(cam.transform.position);
        
        //Obtenemos la posicion de la camara en coordenadas esfericas
        Vector3 sphereCoord = Extensions.CartesianToSpherical(localCamPos);
        rad = sphereCoord.x;
        azim = sphereCoord.y;
        elev = sphereCoord.z;

        rad = Mathf.SmoothDamp(rad, defRad, ref radVel, radSpeed);

        //Calculamos un angulo de elevacion que nos deje a la altura deseada. Tenemos que limitar la altura para que no supere al radio.
        //Si para obtener la altura de un punto a cierto radio y angulo se hace radio por sin del angulo
        //Para obtener el angulo de un punto a cierto radio y altura se hace arcsin de altura entre radio
        float targetElev = Mathf.Asin(Mathf.Max(Mathf.Min(defElev, rad-Mathf.Epsilon),-(rad-Mathf.Epsilon))/rad);
        elev = Mathf.Deg2Rad*Mathf.SmoothDampAngle(elev*Mathf.Rad2Deg, targetElev*Mathf.Rad2Deg, ref elevVel, elevSpeed);
        
        //Despues de calcular la nueva posicion, la devolvemos al plano cartesiano
        localCamPos = Extensions.SphericalToCartesian(rad, azim, elev);

        cam.transform.position = localMat.MultiplyPoint3x4(localCamPos);
        

        currentDir = cam.transform.forward;
        Vector3 toPlayerDir = ((player.transform.position + (Vector3.up * globalUpDot * playerOffset)) - cam.transform.position).normalized;
        //Cambiamos el eje vertical de la camara para evitar rotaciones aleatorias
        Vector3 upAngle = Mathf.Abs(Vector3.Dot(toPlayerDir, Vector3.up)) > 0.95f ? player.transform.up : Vector3.up;

        cam.transform.rotation = Extensions.SmoothDampQuaternion(cam.transform.rotation, Quaternion.LookRotation(toPlayerDir, upAngle), ref lookVel, lookSpeed);
    }

    
}
