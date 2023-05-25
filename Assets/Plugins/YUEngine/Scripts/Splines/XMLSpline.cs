using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml;

namespace YU2.Splines
{
    public class XMLSpline : MonoBehaviour
    {
        public TextAsset from;
        public string splineName;
        public float HEscale = 0.1f;


        // Start is called before the first frame update
        [ContextMenu("ReadSplineFile")]
        public void ReadSpline()
        {
            List<BezierCurve>  points = new List<BezierCurve>();
            List<BezierCurve>  bnsPoints = new List<BezierCurve>();
            XmlDocument set = new XmlDocument();
            set.LoadXml(from.text);
            Debug.Log("Starting");
            Matrix4x4 mat = Matrix4x4.identity;
            foreach (XmlElement node in set.SelectNodes("SonicPath/library/geometry"))
            {
                Debug.Log("id nodo:" + node.Attributes["id"].InnerText + " is equal? " + (node.Attributes["id"].InnerText == splineName).ToString());
                
                if (node.Attributes["id"].InnerText == splineName)
                {

                    foreach (XmlElement scene in set.SelectNodes("SonicPath/scene/node"))
                    {
                        Debug.Log("id scena:" + scene.Attributes["id"].InnerText + " is equal to" + splineName.Substring(0, splineName.Length - 9)  + "? " + (scene.Attributes["id"].InnerText == splineName).ToString());
                        if (scene.Attributes["id"].InnerText == splineName.Substring(0, splineName.Length - 9))
                        {
                            Vector3 pos, sca, rot = Vector3.zero;
                            string[] values = scene["translate"].InnerText.Split(' ');
                            Debug.Log("translate mtx:" + scene["translate"].InnerText);
                            pos = new Vector3(-float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
                            mat = Matrix4x4.Translate(pos);
                        }
                    }



                    int splineKnots = int.Parse(node["spline"].ChildNodes[0].Attributes["count"].Value);

                    Debug.Log("There are " + splineKnots + " in this spline");
                    XmlNode spline3D = node["spline"].ChildNodes[0];
                    XmlNode BNSpline = node["spline"].ChildNodes[1];
                    for (int i = 0; i < splineKnots; i++)
                    {
                        XmlNode knot = spline3D.ChildNodes[i];
                        string[] values = knot["point"].InnerText.Split(' ');
                        Vector3 pos = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);

                        values = knot["invec"].InnerText.Split(' ');
                        Vector3 invec = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);
                        values = knot["outvec"].InnerText.Split(' ');
                        Vector3 outvec = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);

                        BezierCurve uniKnot = new BezierCurve();
                        // = new BezierCurve(pos, invec, outvec, quaternion.identity)
                        uniKnot.inPoint = invec;
                        uniKnot.outPoint = outvec;
                        uniKnot.point = pos;
                        uniKnot.mode = BezierControlPointMode.Free;
                        points.Add(uniKnot);


                        knot = BNSpline.ChildNodes[i];
                        values = knot["point"].InnerText.Split(' ');

                        Vector3 BnPos = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);

                        values = knot["invec"].InnerText.Split(' ');
                        Vector3 BnInvec = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);
                        values = knot["outvec"].InnerText.Split(' ');
                        Vector3 BnOutvec = new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale);

                        BezierCurve bnKnot = new BezierCurve();
                        // = new BezierCurve(pos, invec, outvec, quaternion.identity)
                        bnKnot.inPoint = BnInvec;
                        bnKnot.outPoint = BnOutvec;
                        bnKnot.point = BnPos;
                        uniKnot.mode = BezierControlPointMode.Free;
                        bnsPoints.Add(bnKnot);
                    }
                }
            }

            BezierSpline newSpline = new GameObject(splineName + "_Left").AddComponent<BezierSpline>();
            newSpline.SetPointArray(points.ToArray(), false);
            newSpline.transform.position = mat.GetPosition();

            newSpline = new GameObject(splineName + "_Right").AddComponent<BezierSpline>();
            newSpline.SetPointArray(bnsPoints.ToArray(), false);
            newSpline.transform.position = mat.GetPosition();
        }

        // Update is called once per frame
        /*void Update()
        {
            for(int i=0; i < points.Count; i++)
            {
                Debug.DrawRay(points[i], Vector3.up, Color.red, Time.deltaTime);
            }

        }*/
        
    }

}
