using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTrigger : MonoBehaviour
{
    public enum PathType {TwoD, Loop, Corrective}
    public PathType pathType;
    public Path path;
    public bool IsExit;
}
