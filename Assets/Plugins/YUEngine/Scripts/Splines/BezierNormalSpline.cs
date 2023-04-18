using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YU2.Splines
{
	public class BezierNormalSpline : Spline
	{
		[SerializeField]
		private BezierNormalCurve[] points;
		private bool loop;
		public bool Loop
		{
			get
			{
				return loop;
			}
			set
			{
				loop = value;
				if (value == true)
				{
					points[points.Length-1].mode = points[points.Length - 1].mode;
					SetControlPoint(0, BezierKind.Point,points[0].point);
				}
			}
		}

		public int CurveCount
		{
			get
			{
				return points.Length-1;
			}
		}

		public Vector3 GetControlPoint(int index, BezierKind kind)
		{
			if(kind==BezierKind.Point)
				return points[index].point;
			else if(kind == BezierKind.TangentIn)
				return points[index].inPoint;
			else
				return points[index].outPoint;
		}

		public BezierControlPointMode GetControlPointMode(int index)
		{
			return points[index].mode;
		}

		public void SetControlPointMode(int index, BezierControlPointMode mode)
		{
			points[index].mode = mode;
			if (loop)
			{
				if (index == 0)
					points[CurveCount].mode = mode;
				if (index == CurveCount)
					points[0].mode = mode;
				
			}


			EnforceMode(index, BezierKind.Point);
		}

		public void SetControlPoint(int index, BezierKind kind, Vector3 point)
		{
			if (kind == BezierKind.Point)
			{
				

				Vector3 delta = point - points[index].point;
				if (loop)
				{
					if(index == 0)
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
						points[CurveCount].point = point;
					}
					else if(index == CurveCount)
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
						points[0].point = point;
					}
					else
					{
						points[index].outPoint += delta;
						points[index].inPoint += delta;
					}
				}
				else
				{
					points[index].outPoint += delta;
					points[index].inPoint += delta;
					
				}

				points[index].point = point;
			}
			else if (kind == BezierKind.TangentIn)
				points[index].inPoint = point;
			else
				points[index].outPoint = point;

			EnforceMode(index, kind);
		}

		private void EnforceMode(int index, BezierKind kind)
		{
			var mode = points[index].mode;

			if (mode == BezierControlPointMode.Free)
			{
				return;
			}

			Vector3 middle = points[index].point;
			int fixedIdx, enforceIdx;
			BezierKind fixedKind, enforceKind;

			//Si seleccionamos tangent out o es el primer indice, ese es nuestro anchor, de lo contrario, tangent in es.
			fixedKind = (kind == BezierKind.Point || index == 0 ? BezierKind.TangentOut : kind);
			fixedIdx = index;

			if (loop && (index == 0 || index == CurveCount))
			{
				if (index == 0)
				{
					fixedKind = BezierKind.TangentOut;
					enforceIdx = CurveCount;
					enforceKind = BezierKind.TangentIn;
				}
				else
				{
					fixedKind = BezierKind.TangentIn;
					enforceIdx = 0;
					enforceKind = BezierKind.TangentOut;
				}
			}
			else
			{
				if (fixedKind == BezierKind.TangentOut)
				{
					enforceIdx = index;
					enforceKind = BezierKind.TangentIn;
				}
				else
				{
					enforceIdx = index;
					enforceKind = BezierKind.TangentOut;
				}
			}


			Vector3 enforcedTangent = middle - (fixedKind == BezierKind.TangentOut ? points[fixedIdx].outPoint : points[fixedIdx].inPoint); 
			
			if (mode == BezierControlPointMode.Aligned)
			{
				enforcedTangent = enforcedTangent.normalized * Vector3.Distance(middle, (enforceKind == BezierKind.TangentOut ? points[enforceIdx].outPoint : points[enforceIdx].inPoint));
			}

			if(enforceKind == BezierKind.TangentIn)
				points[enforceIdx].inPoint = middle + enforcedTangent;
			else
				points[enforceIdx].outPoint = middle + enforcedTangent;
		}

		public override Vector3 GetPoint(float t)
		{
			int i;
			float realT = t;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length-2;
			}
			else
			{
				t = Mathf.Clamp01(t) * (points.Length - 1);
				i = (int)t;
				t -= i;
			}

			return transform.TransformPoint(Bezier.GetPoint(points[i].point, points[i].outPoint, points[i+1].inPoint, points[i+1].point, t));
		}

		public override Vector3 GetVelocity(float t)
		{
			int i;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length-2;
			}
			else
			{
				t = Mathf.Clamp01(t) * (points.Length - 1);
				i = (int)t;
				t -= i;
			}

			return transform.TransformPoint(
				Bezier.GetFirstDerivative(points[i].point, points[i].outPoint, points[i + 1].inPoint, points[i + 1].point, t)) - transform.position;
		}

		public override Vector3 GetTangent(float t)
		{
			return GetVelocity(t).normalized;
		}

		public override Vector3 GetBinormal(float t)
		{
			int i;
			if (t >= 1f)
			{
				t = 1f;
				i = points.Length - 2;
			}
			else
			{
				t = Mathf.Clamp01(t) * (points.Length - 1);
				i = (int)t;
				t -= i;
			}
			Vector3 intNorm = Vector3.SlerpUnclamped(points[i].normal, points[i + 1].normal, t);

			return Vector3.Cross(intNorm, GetTangent(t));
		}

		public override Vector3 GetNormal(float t)
		{
			return Vector3.Cross(GetTangent(t), GetBinormal(t)).normalized;
		}

		public Vector3 SetNormal(float t)
		{
			return Vector3.Cross(GetTangent(t), GetBinormal(t)).normalized;
		}

		public void Reset()
		{
			points = new BezierNormalCurve[2];
			points[0].inPoint = new Vector3(-1f, 0f, 0f);
			points[0].point = new Vector3(0f, 0f, 0f);
			points[0].normal = Vector3.up;
			points[0].outPoint = new Vector3(1f, 0f, 0f);
			points[1].inPoint = new Vector3(2f, 0f, 0f);
			points[1].point = new Vector3(3f, 0f, 0f);
			points[0].normal = Vector3.up;
			points[1].outPoint = new Vector3(4f, 0f, 0f);
		}

		public void AddCurve()
		{
			Vector3 lastTang = GetTangent(1f);
			Array.Resize(ref points, points.Length + 1);
			points[points.Length - 1].inPoint.x += 1;
			points[points.Length - 1].point.x = points[points.Length - 1].inPoint.x + 1;
			points[points.Length - 1].outPoint.x = points[points.Length - 1].inPoint.x + 2;

			if (loop) {
				points[points.Length - 1] = points[0];
				EnforceMode(0, BezierKind.Point);
			}

			EnforceMode(points.Length-1, BezierKind.Point);
			Vector3 newTang = GetTangent(1f);
			Quaternion tangDif = Quaternion.FromToRotation(lastTang, newTang);
			points[points.Length - 1].normal = tangDif * points[points.Length - 2].normal;

		}
	}
}
