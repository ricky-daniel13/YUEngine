using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleMover : MonoBehaviour
{
    public CapsuleCollider cap;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        // Make a background box
        GUI.Box(new Rect(10, 300, 250, 140), "capsule test");
        if (GUI.Button(new Rect(100, 325, 60, 15), "moveCapsule"))
            MoveCapsule();


    }

    void MoveCapsule()
    {
        float yFactor = Vector3.Angle(transform.up, cap.transform.up);
        float xFactor = Vector3.Angle(transform.up, cap.transform.right);
        float zFactor = Vector3.Angle(transform.up, cap.transform.forward);

        yFactor = Mathf.Abs(1 - (yFactor >= 180 ? yFactor - 180 : yFactor) / 90);
        xFactor = Mathf.Abs(1 - (xFactor >= 180 ? xFactor - 180 : xFactor) / 90);
        zFactor = Mathf.Abs(1 - (zFactor >= 180 ? zFactor - 180 : zFactor) / 90);


        float fYPos = ((cap.height/2) * yFactor + cap.radius * xFactor + cap.radius * zFactor);

        Debug.Log("Angulo con axis up: " + Vector3.Angle(transform.up, cap.transform.up) + " Angulo con axis right: " + Vector3.Angle(transform.up, cap.transform.right) + " Angulo con axis forwards: " + Vector3.Angle(transform.up, cap.transform.forward));
        Debug.Log("Angulo con axis up: " + yFactor + " Angulo con axis right: " + xFactor + " Angulo con axis forwards: " + zFactor);

        cap.transform.position = transform.position + transform.rotation*(new Vector3(0, fYPos, 0)) + cap.transform.rotation * -cap.center;
    }
}
