using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml;

namespace YU2.Splines
{
    public class XMLSpline : MonoBehaviour
    {
        /*public TextAsset from;
        public string splineName;
        public List<BezierKnot> points;
        public float HEscale = 0.1f;
        public SplineContainer cont;


        // Start is called before the first frame update
        [ContextMenu("ReadSplineFile")]
        public void ReadSpline()
        {
            points = new List<BezierKnot>();
            XmlDocument set = new XmlDocument();
            set.LoadXml(from.text);
            Debug.Log("Starting");
            foreach (XmlElement node in set.SelectNodes("SonicPath/library/geometry"))
            {
                //Debug.Log("id nodo:" + node.Attributes["id"].InnerText + " is equal? " + (node.Attributes["id"].InnerText == splineName).ToString());
                Matrix4x4 mat = Matrix4x4.identity;
                if (node.Attributes["id"].InnerText == splineName)
                {

                    foreach (XmlElement scene in set.SelectNodes("SonicPath/scene/node"))
                    {
                        //Debug.Log("id scena:" + scene.Attributes["id"].InnerText + " is equal to" + splineName.Substring(0, splineName.Length - 9)  + "? " + (scene.Attributes["id"].InnerText == splineName).ToString());
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
                        float3 pos = new float3(new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale));

                        values = BNSpline.ChildNodes[i]["point"].InnerText.Split(' ');
                        float3 BNPos = new float3(new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale));


                        values = knot["invec"].InnerText.Split(' ');
                        float3 invec = new float3(new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale)) - pos;
                        values = knot["outvec"].InnerText.Split(' ');
                        float3 outvec = new float3(new Vector3(-float.Parse(values[0]) * HEscale, float.Parse(values[1]) * HEscale, float.Parse(values[2]) * HEscale)) - pos;

                        BezierKnot uniKnot = new BezierKnot(pos, invec, outvec, quaternion.identity);
                        points.Add(uniKnot);
                    }
                }
            }

            cont.Spline = new Spline(points, false);
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
