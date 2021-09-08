#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class path {
	[SerializeField, HideInInspector]
	List<Vector2> points;
	[SerializeField, HideInInspector]
	bool isClosed;
	[SerializeField, HideInInspector]
	bool autoSetControlPoints;

	public path(Vector2 center){
		points = new List<Vector2>{ // Creating first segment
			center + Vector2.left, // First anchor with index 0
			center + (Vector2.left + Vector2.up) * 0.5f, // First Control point between two anchors
			center + (Vector2.right + Vector2.down) * 0.5f, // Second control point between two anchors
			center + Vector2.right // Second anchor with index 3
		}; // Each segment will hold two anchors and two control points between them
	}

	public Vector2 this[int index]{ // Just a public indexer for easy access to points
		get{
			return points[index];
		}
	}

	public bool autoSet{
		get{
			return autoSetControlPoints;
		}

		set{
			if(autoSetControlPoints != value){
				autoSetControlPoints = value;
				if(autoSetControlPoints){
					autoSetAllControlPoints();
				}
			}
		}
	}

	public bool closed{
		get{
			return isClosed;
		}
		set{
			if(isClosed != value){
				isClosed = value;
				if(isClosed){
					points.Add(points[points.Count - 1] * 2f - points[points.Count - 2]);
					points.Add(points[0] * 2f - points[1]);
					if(autoSetControlPoints){
						autoSetAnchorControlPoints(0);
						autoSetAnchorControlPoints(numPoints - 3);
					}
				}
				else{
					points.RemoveRange(points.Count - 2, 2);
					if(autoSetControlPoints){
						autoSetStartEndControlPoints();
					}
				}
			}
		}
	}

	public int numPoints{ // How many points this path has, includes both anchor and controls
		get{
			return points.Count;
		}
	}

	public int numSegments{ // how many segments this path has, a segment is a collection of two anchors and two control points between them
		get{
			return numPoints / 3;
		}
	}

	public Vector2[] getPointsInSegment(int index){
		return new Vector2[]{points[index * 3], points[index * 3 + 1], points[index * 3 + 2], points[LoopIndex(index * 3 + 3)]};
	}

	public List<Vector2> getPoints{
		get{
			return points;
		}
	}

	public void addAnchor(Vector2 anchorPos){ // Adding new segment and taking last anchor of previous segment as first for this segment
		points.Add(points[points.Count - 1] * 2f - points[points.Count - 2]); // First control point having same direction and distance as the second contol point of previous segment
		points.Add((points[points.Count - 1] + anchorPos) * 0.5f); // Second control point exactly in between first control point and this new anchor
		points.Add(anchorPos); // Second anchor of new segment
		if(autoSetControlPoints){
			autoSetAffectedControlPoints(numPoints - 1);
		}
	}

	public void splitSegment(Vector2 anchorPos, int segmentIndex){
		points.InsertRange(segmentIndex * 3 + 2, new Vector2[]{Vector2.zero, anchorPos, Vector2.zero});
		if(autoSetControlPoints){
			autoSetAffectedControlPoints(segmentIndex * 3 + 3);
		}else{
			autoSetAnchorControlPoints(segmentIndex * 3 + 3);
		}
	}

	public void deleteSegment(int anchorIndex){
		if(numSegments > 2 || !isClosed && numSegments > 1){
			if(anchorIndex == 0){
				if(isClosed){
					points[numPoints - 1] = points[2];
				}
				points.RemoveRange(0, 3);
			}else if(anchorIndex == numPoints - 1 && !closed){
				points.RemoveRange(anchorIndex - 2, 3);
			}else{
				points.RemoveRange(anchorIndex - 1, 3);
			}
		}
	}

	public void movePoint(int index, Vector2 toPos){
		Vector2 delta = toPos - points[index];
		if(index % 3 == 0 || !autoSetControlPoints){
			points[index] = toPos;
			if(autoSetControlPoints){
				autoSetAffectedControlPoints(index);
			}else{
				if(index % 3 == 0){ // Means we are trying to move anchor itself, so move control points along with anchor
					if(index + 1 < numPoints || isClosed){ // Check if this anchor have a control point after it
						points[LoopIndex(index + 1)] += delta;
					}
					if(index - 1 > 0 || isClosed){ // Check if this anchor have a control point before it
						points[LoopIndex(index - 1)] += delta;
					}
				}else{ // Means we are trying to move a control point
					bool nextPointIsAnchor = (index + 1) % 3 == 0;
					int correspoindingControlIndex = (nextPointIsAnchor) ? index + 2 : index - 2;
					int anchorIndex = (nextPointIsAnchor) ? index + 1 : index - 1;
					if(correspoindingControlIndex >= 0 && correspoindingControlIndex < numPoints || isClosed){
						float dist = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspoindingControlIndex)]).magnitude;
						Vector2 dir = (points[LoopIndex(anchorIndex)] - toPos).normalized;
						points[LoopIndex(correspoindingControlIndex)] = points[LoopIndex(anchorIndex)] + dist * dir;
					}
				}
			}
		}
	}

	void autoSetAffectedControlPoints(int anchorIndex){
		for (int i = anchorIndex-3; i <= anchorIndex+3; i+=3)
		{
			if(i >= 0 && i < numPoints || isClosed){
				autoSetAnchorControlPoints(LoopIndex(i));
			}
		}
		autoSetStartEndControlPoints();
	}

	void autoSetAllControlPoints(){
		for (int i = 0; i < numPoints; i+=3)
		{
			autoSetAnchorControlPoints(i);
		}
		autoSetStartEndControlPoints();
	}

	void autoSetAnchorControlPoints(int anchorIndex){
		Vector2 anchorPos = points[anchorIndex];
		Vector2 dir = Vector2.zero;
		float[] neighbourDist = new float[2];

		if(anchorIndex - 3 >= 0 || isClosed){
			Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
			dir += offset.normalized;
			neighbourDist[0] = offset.magnitude;
		}
		if(anchorIndex + 3 >= 0 || isClosed){
			Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
			dir -= offset.normalized;
			neighbourDist[1] = -offset.magnitude;
		}
		dir.Normalize();
		for (int i = 0; i < 2; i++)
		{
			int controlPointIndex = anchorIndex + i * 2 - 1;
			if(controlPointIndex >= 0 && controlPointIndex < numPoints || isClosed){
				points[LoopIndex(controlPointIndex)] = anchorPos + dir * neighbourDist[i] * 0.5f;
			}
		}
	}

	void autoSetStartEndControlPoints(){
		if(!isClosed){
			points[1] = (points[0] + points[2]) * 0.5f;
			points[numPoints - 2] = (points[numPoints - 1] + points[numPoints - 3]) * 0.5f;
		}
	}

	int LoopIndex(int index){ // When path is closed we want to re start the indexing from 0 if index excedes numPoints
		return Bezier.LoopIndex(index, numPoints);
	}
}
#endif
