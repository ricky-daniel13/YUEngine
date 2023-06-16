using UnityEngine;
using YU2.Splines;

public class PathLoop : MonoBehaviour
{
    [SerializeField]
    private bool is2d, exitOnFall;

    [SerializeField]
    private BezierSpline right;

    [SerializeField]
    private Path path;

    [SerializeField]
    private Vector3 forward;

    [SerializeField]
    private Vector2 startGround = new Vector2(0f, 0.1f), endGround = new Vector2(0.9f, 1.0f);

    [SerializeField]
    private float switchPoint=0.5f;

    public Path getPath {get{ return path;}}
    public Vector2 getStart { get { return startGround; } }

    public Vector2 getEnd { get { return endGround; } }

    public float getSwitch { get { return switchPoint; } }

    public bool Is2d { get => is2d;}
    public bool ExitOnFall { get => exitOnFall;}

    public float pathSpeed, pathTest = 0;
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

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(path.spl.GetPoint(pathTest), 0.25f);
        Gizmos.DrawRay(path.spl.GetPoint(pathTest), path.spl.GetTangent(pathTest) *2);
    }

    private void Update()
    {
        pathTest += pathSpeed * Time.deltaTime;
    }

    public Vector3 PutOnPath(Vector3 position, Vector3 normal, out BezierKnot bezierKnot, out float closestTimeOnSpline, out float pathBound, float startSearch = 0, float endSearch = 1)
    {
        Vector3 pos = path.PutOnPath(position, normal, out BezierKnot thisKnot, out float time, startSearch, endSearch);

        bezierKnot = thisKnot;
        closestTimeOnSpline = time;
        pathBound = 0;

        if(right != null)
        {
            float myRgt = Vector3.Dot(pos, bezierKnot.binormal);
            float boundRgt = Vector3.Dot(right.GetPoint(time), bezierKnot.binormal);
            pathBound = boundRgt - myRgt;
        }

        return pos;

    }
}
