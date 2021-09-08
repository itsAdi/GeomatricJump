#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(pathCreator))]
public class pathEditor : Editor
{
    pathCreator pC;
    path p;

    int selectedSegmentIndex;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();
        pC.curveName = EditorGUILayout.TextField("CurveName", pC.curveName);
        pC.isVisualizing = EditorGUILayout.Toggle("Test Curve", pC.isVisualizing);
        EditorGUI.BeginDisabledGroup(!pC.isVisualizing);
        pC.pointRadius = EditorGUILayout.FloatField("Radius", pC.pointRadius);
        EditorGUI.EndDisabledGroup();
        if (pC.isVisualizing)
        {
            pC.visualPoints.Clear();
            pC.visualPoints = Bezier.CalculateEvenlySpacedPoints(pC.curveSpacing, p.getPoints, pC.curveResolution);
        }
        EditorGUI.BeginDisabledGroup(pC.isVisualizing);
        if (GUILayout.Button("Create New Path"))
        {
            if(pC.curveSaved){
                createNewCurve();
            }else{
                if(EditorUtility.DisplayDialog("Curve not rendered", "Continue without rendering this curve ?", "Yes", "No")){
                    createNewCurve();
                }
            }
        }
        if (GUILayout.Button("Render Curve"))
        {
            if(!string.IsNullOrEmpty(pC.curveName)){
                if(AssetDatabase.FindAssets(WWW.EscapeURL(pC.curveName), null).Length == 0){
                    createCurveAsset();
                }else{
                    if(EditorUtility.DisplayDialog("Overwrite Curve Data", string.Format("Curve data with name {0} already in project \n Please make sure that it is not in Assets folder \n If it is then do you want to overwrite it ?", WWW.EscapeURL(pC.curveName)), "Yes", "No")){
                        createCurveAsset();
                    }
                }
            }else{
                EditorUtility.DisplayDialog("Empty Curve Name", "Curve name could not be empty", "Got It");
            }
        }
        bool closed = GUILayout.Toggle(p.closed, "Closed");
        if (closed != p.closed)
        {
            Undo.RecordObject(pC, "Toggle Close");
            p.closed = closed;
        }
        bool autoSet = GUILayout.Toggle(p.autoSet, "Auto Set Curve");
        if (autoSet != p.autoSet)
        {
            Undo.RecordObject(pC, "Auto Curve");
            p.autoSet = autoSet;
        }
        pC.curveSpacing = EditorGUILayout.FloatField("Spacing : ", pC.curveSpacing);
        pC.curveResolution = EditorGUILayout.FloatField("Resolution : ", pC.curveResolution);
        EditorGUI.EndDisabledGroup();
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup((pC.selectedControlIndex == -1 || pC.isVisualizing));
        pC.selectedControlPos = EditorGUILayout.Vector2Field("Control Point Position", pC.selectedControlPos);
        EditorGUI.EndDisabledGroup();
        if(EditorGUI.EndChangeCheck()){
            p.movePoint(pC.selectedControlIndex, pC.selectedControlPos);
        }
    }

    void OnSceneGUI()
    {
        drawPath();
        fetchInput();
    }

    void fetchInput()
    {
        Event guiEvent = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        if (guiEvent.shift)
        {
            if (guiEvent.type == EventType.MouseDown)
            {
                switch (guiEvent.button)
                {
                    case 0:
                        pC.curveSaved = false;
                        if (selectedSegmentIndex != -1)
                        {
                            Undo.RecordObject(pC, "Split segment");
                            p.splitSegment(mousePos, selectedSegmentIndex);
                            selectedSegmentIndex = -1; // So user just can't double click in same position
                        }
                        else if (!p.closed)
                        {
                            Undo.RecordObject(pC, "Add segment");
                            p.addAnchor(mousePos);
                        }
                        break;
                    case 1:
                        pC.curveSaved = false;
                        float minDist = pC.anchorDiametre * 0.5f;
                        int currentAnchorIndex = -1;
                        for (int i = 0; i < p.numPoints; i += 3)
                        {
                            float dist = Vector2.Distance(mousePos, p[i]);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                currentAnchorIndex = i;
                            }
                        }
                        if (currentAnchorIndex != -1)
                        {
                            Undo.RecordObject(pC, "Remove segment");
                            p.deleteSegment(currentAnchorIndex);
                        }
                        break;
                }
            }
            if (guiEvent.type == EventType.MouseMove)
            {
                float minDist = 0.1f;
                int currentSegmentIndex = -1;
                for (int i = 0; i < p.numSegments; i++)
                {
                    Vector2[] segPoints = p.getPointsInSegment(i);
                    float dist = HandleUtility.DistancePointBezier(mousePos, segPoints[0], segPoints[3], segPoints[1], segPoints[2]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        currentSegmentIndex = i;
                    }
                }
                if (currentSegmentIndex != selectedSegmentIndex)
                {
                    selectedSegmentIndex = currentSegmentIndex;
                    HandleUtility.Repaint();
                }
            }
        }


    }

    void drawPath()
    {
        for (int j = 0; j < p.numSegments; j++)
        {
            Vector2[] points = p.getPointsInSegment(j);
            Handles.color = Color.black;
            Handles.DrawLine(points[0], points[1]);
            Handles.DrawLine(points[3], points[2]);
            Color selectSegCol = (j == selectedSegmentIndex && Event.current.shift) ? Color.red : Color.green;
            Handles.DrawBezier(points[0], points[3], points[1], points[2], selectSegCol, null, 2f);
        }
        for (int i = 0; i < p.numPoints; i++)
        {
            if (i % 3 == 0)
            {
                Handles.color = Color.green;
            }
            else
            {
                Handles.color = Color.red;
            }
            Vector2 newPos = Handles.FreeMoveHandle(p[i], Quaternion.identity, pC.anchorDiametre, Vector2.zero, Handles.CylinderHandleCap);
            if (p[i] != newPos)
            {
                Undo.RecordObject(pC, "Move Path Point");
                if(pC.curveSaved){
                    pC.curveSaved = false;
                }
                p.movePoint(i, newPos);
                if(i % 3 == 0){
                    pC.selectedControlIndex = i;
                    pC.selectedControlPos = newPos;
                }else{
                    pC.selectedControlIndex = -1;
                }
            }
        }
        if(pC.isVisualizing){
            for (int i = 0; i < pC.visualPoints.Count; i ++)
            {
                Handles.DrawWireArc(pC.visualPoints[i], Vector3.forward, -Vector3.right, 360f, pC.pointRadius);
            }
            if(pC.TargetCurve != null)
            {
                for (int i = 0; i < pC.TargetCurve.data.Count; i++)
                {
                    Handles.DrawWireArc(pC.TargetCurve.data[i], Vector3.forward, -Vector3.right, 360f, pC.pointRadius);
                }
            }
        }
    }

    void createCurveAsset(){
        pC.curveSaved = true;
        curveData cd = ScriptableObject.CreateInstance<curveData>();
        AssetDatabase.CreateAsset(cd, string.Format("Assets/{0}.asset", WWW.EscapeURL(pC.curveName)));
        AssetDatabase.SaveAssets();
        pC.renderCurve(cd);
    }

    void createNewCurve(){
        Undo.RecordObject(pC, "Create New");
        pC.createPath();
        p = pC.pathInstance;
    }

    void OnEnable()
    {
        pC = (pathCreator)target;
        if (pC.pathInstance == null)
        {
            pC.createPath();
        }
        p = pC.pathInstance;
    }
}
#endif
