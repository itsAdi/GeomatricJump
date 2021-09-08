using System.Collections.Generic;
using UnityEngine;

public class curveData : ScriptableObject {
	public List<Vector2> data;
	public Vector2 highestPoint;

	public void writeData(List<Vector2> withPoints, float withSpacing, float withResolution){
		data = new List<Vector2>(Bezier.CalculateEvenlySpacedPoints(withSpacing, withPoints, withResolution));
		float temp = data[0].y;
		foreach (Vector2 curr in data)
		{
			if(curr.y > temp){
				temp = curr.y;
			}
		}
		highestPoint = new Vector2(0f, temp);
	}
}
