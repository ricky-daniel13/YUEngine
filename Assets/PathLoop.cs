using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YU2.Splines;

public class PathLoop : MonoBehaviour
{
    [SerializeField]
    private bool is2d, exitOnFall;

    [SerializeField]
    private BezierSpline left, right;

    [SerializeField]
    private Path path;

    [SerializeField]
    private Vector3 forward;

    [SerializeField]
    private Vector2 startGround = new Vector2(0f, 0.1f), endGround = new Vector2(0.9f, 1.0f);

    [SerializeField]
    private float switchPoint=0.5f;

    public Path getPath {get{ return path;}}
    // Update is called once per frame
    private void OnDrawGizmosSelected() {
        if(path == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(path.spl.GetPoint(startGround.x), 0.25f);
        Gizmos.DrawSphere(path.spl.GetPoint(startGround.y), 0.25f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(path.spl.GetPoint(endGround.x), 0.25f);
        Gizmos.DrawSphere(path.spl.GetPoint(endGround.y), 0.25f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(path.spl.GetPoint(switchPoint), 0.25f);
    }
}
