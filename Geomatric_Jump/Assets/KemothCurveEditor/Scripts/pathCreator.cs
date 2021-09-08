#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

public class pathCreator : MonoBehaviour {
	[HideInInspector]
	public path pathInstance;
	[HideInInspector]
	public float anchorDiametre = 0.1f, curveSpacing = 1.0f, curveResolution = 1.0f, pointRadius = 0.3f;
	[HideInInspector]
	public string curveName = "Curve";
	[HideInInspector]
	public bool curveSaved, isVisualizing;
	[HideInInspector]
	public int selectedControlIndex;
	[HideInInspector]
	public Vector2 selectedControlPos;
	[HideInInspector]
	public List<Vector2> visualPoints;
    [Header("Compare current curve with this curve")]
    public curveData TargetCurve;

	public void createPath(){
		pathInstance = new path(transform.position);
		curveSaved = true;
		selectedControlIndex = -1;
		selectedControlPos = pathInstance.getPoints[0];
		visualPoints = new List<Vector2>(pathInstance.getPoints);
	}
	public void renderCurve(curveData cdInstance){
		float dist = Vector2.Distance(Vector2.zero, transform.position);
		Vector2 dir = (Vector3.zero - transform.position).normalized;
		List<Vector2> tempData = new List<Vector2>(pathInstance.getPoints);
		for (int i = 0; i < pathInstance.numPoints; i++)
		{
			tempData[i] = tempData[i] + dir * dist;			
		}
		cdInstance.writeData(tempData, curveSpacing, curveResolution);
		UnityEditor.EditorUtility.SetDirty(cdInstance);
		UnityEditor.AssetDatabase.SaveAssets();
	}
}
#endif
