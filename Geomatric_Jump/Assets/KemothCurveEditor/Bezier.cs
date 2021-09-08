using UnityEngine;
using System.Collections.Generic;

public static class Bezier {
	static Vector2 evaluateQuadraticcurve(Vector2 a, Vector2 b, Vector2 c, float t){
		Vector2 p0 = Vector2.Lerp(a, b, t);
		Vector2 p1 = Vector2.Lerp(b, c, t);
		return Vector2.Lerp(p0, p1, t);
	}

	public static int LoopIndex(int index,int maxIteration){ // When path is closed we want to re start the indexing from 0 if index excedes points.count
		return (index + maxIteration) % maxIteration;
	}

	public static Vector2 evaluateCubicCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t){
		Vector2 p0 = evaluateQuadraticcurve(a, b, c, t);
		Vector2 p1 = evaluateQuadraticcurve(b, c, d, t);
		return Vector2.Lerp(p0, p1, t);
	}

	public static List<Vector2> CalculateEvenlySpacedPoints(float spacing, List<Vector2> recData, float resolution = 1f){
        float res = Mathf.Abs(resolution); // Because a negative value was freezing unity editor
		List<Vector2> evenlySpacedPoints = new List<Vector2>();
		evenlySpacedPoints.Add(recData[0]);
		Vector2 lastPoint = recData[0];
		float distSinceLastEvenlySpacedPoint = 0f;
		for (int i = 0; i < (recData.Count / 3); i++)
		{
			Vector2[] p = new Vector2[]{recData[i * 3], recData[i * 3 + 1], recData[i * 3 + 2], recData[LoopIndex(i * 3 + 3, recData.Count)]};
			float t = 0f;
			float controlNetLenght = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
			float bezierLength = Vector2.Distance(p[0], p[3]) + (controlNetLenght / 2f);
			float divisions = Mathf.Ceil(bezierLength * res * 10f); // The 10 here is just some constant value, experiment with it for varied results
			while(t <= 1f){
				t += 0.1f / divisions;
				Vector2 pointOnCurve = Bezier.evaluateCubicCurve(p[0], p[1], p[2], p[3], t);
				distSinceLastEvenlySpacedPoint += Vector2.Distance(lastPoint, pointOnCurve);
				while(distSinceLastEvenlySpacedPoint >= spacing){
					float overShoot = distSinceLastEvenlySpacedPoint - spacing;
					Vector2 newEvenlySpacedPoint = pointOnCurve + (lastPoint - pointOnCurve).normalized * overShoot;
					evenlySpacedPoints.Add(newEvenlySpacedPoint);
					distSinceLastEvenlySpacedPoint = overShoot;
					lastPoint = newEvenlySpacedPoint;
				}
				lastPoint = pointOnCurve;
			}
		}
		return evenlySpacedPoints;
	}
}
