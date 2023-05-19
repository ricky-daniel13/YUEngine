using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float cameraAccel, cameraDefaultLenght, currentCameraLength;
    Vector3 velocity, currentPosition;
    public Transform cameraHolder;
    Quaternion currentRotation = Quaternion.LookRotation((Vector3.back+Vector3.up*0.15f).normalized);
    public PlayerAdventure player;
    Vector3 lastPlayerPos, playerVelocity;

    public Vector3 vecDefaultCameraRota;
    Quaternion defaultCameraRota;
    // Start is called before the first frame update
    void Start()
    {
        currentPosition = (player.transform.position + Vector3.up * 0.8f) + (currentRotation * new Vector3(0, 0, cameraDefaultLenght));
        lastPlayerPos = player.transform.position;
        defaultCameraRota = Quaternion.Euler(vecDefaultCameraRota);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        playerVelocity = (player.transform.position - lastPlayerPos)/Time.deltaTime;
        Vector3 correctedPlayerPos =  player.transform.position + player.player.GetGroundNormal * 0.8f;
        Vector3 localPlayerPos = player.transform.position - currentPosition;
        Vector3 localCameraPos = currentPosition-player.transform.position;
        velocity = Vector3.Project(playerVelocity, Extensions.ProjectDirectionOnPlane(cameraHolder.forward, -player.player.gravityDir));
        velocity += Vector3.Project(playerVelocity, -player.player.gravityDir);
        Vector3 rgtAxis = Vector3.Cross(Extensions.ProjectDirectionOnPlane(cameraHolder.forward, -player.player.gravityDir), -player.player.gravityDir);

        Vector3 cameraDirection = currentPosition - player.transform.position;
        //Debug.Log("Camera distance= " + cameraDirection.magnitude);
        if(cameraDirection.sqrMagnitude > cameraDefaultLenght * cameraDefaultLenght){
            velocity += -(cameraDirection.normalized) * (10f + Time.deltaTime);
        }

        //float cameraHeightToSonic = Vector3.Dot(localCameraPos, player.player.GetGroundNormal);
        //if((cameraHeightToSonic-0.8f) > Mathf.Epsilon)
            //velocity -= player.player.GetGroundNormal * (Mathf.MoveTowards(cameraHeightToSonic, 0, 30));

        //Debug.Log("Camera height difference = " + cameraHeightToSonic);

        Quaternion idToCurr = Quaternion.LookRotation(Extensions.ProjectDirectionOnPlane(cameraHolder.forward, -player.player.gravityDir), -player.player.gravityDir);
        Vector3 desiredPosition = correctedPlayerPos  + Vector3.up * 0.8f + (idToCurr * defaultCameraRota * (Vector3.back * cameraDefaultLenght));
        Debug.DrawRay(desiredPosition, Vector3.Cross(currentPosition - correctedPlayerPos , rgtAxis).normalized, Color.magenta);
        Vector3 localDesired = desiredPosition - currentPosition;
        float dirMult=1;
        
        if(Vector3.Dot(-player.player.gravityDir, localDesired)<0)
            dirMult=-1;

        if(Mathf.Abs(Vector3.Dot(-player.player.gravityDir, localDesired)) > 1f)
            velocity += Vector3.Cross(desiredPosition - correctedPlayerPos, rgtAxis).normalized * (Mathf.Clamp01(localDesired.magnitude)*dirMult*cameraAccel);

        

        velocity += Vector3.Cross(currentPosition - player.transform.position, -player.player.gravityDir).normalized * (-Input.GetAxis("Mouse X")*cameraAccel);
        //velocity = Vector3.Cross(transform.position - centerOfRotation.position, centerOfRotation.up).normalized * orbitSpeed;

        ProcessVelocity(Time.deltaTime);
        lastPlayerPos = player.transform.position;

    }

    void ProcessVelocity(float deltaTime)
    {
        currentPosition += (velocity * deltaTime);
        cameraHolder.transform.position = currentPosition;
        cameraHolder.LookAt(player.transform);
    }
}
