using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float cameraDecel, cameraDefaultLenght, currentCameraLength;
    Vector3 velocity, currentPosition;
    public Transform cameraHolder;
    Quaternion currentRotation = Quaternion.LookRotation((Vector3.back+Vector3.up*0.15f).normalized);
    public PlayerAdventure player;
    Vector3 lastPlayerPos, playerVelocity;
    // Start is called before the first frame update
    void Start()
    {
        currentPosition = player.transform.position + Vector3.up * 0.8f;
        lastPlayerPos = player.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        currentCameraLength = cameraDefaultLenght;
        playerVelocity = (player.transform.position - lastPlayerPos)/Time.deltaTime;


        velocity = Vector3.Project(playerVelocity, Extensions.ProjectDirectionOnPlane(cameraHolder.forward, player.player.GetGroundNormal));
        velocity += Vector3.Project(playerVelocity, player.player.GetGroundNormal);
        ProcessVelocity(Time.deltaTime);
        lastPlayerPos = player.transform.position;

    }

    void ProcessVelocity(float deltaTime)
    {
        currentPosition += (velocity * deltaTime);
        cameraHolder.transform.position = currentPosition + (currentRotation * new Vector3(0, 0, currentCameraLength));
        cameraHolder.LookAt(player.transform);
    }
}
