#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(POI))]
public class POIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Afficher l'inspecteur normal
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Travel Path Tools", EditorStyles.boldLabel);

        POI poi = (POI)target;

        // Bouton pour creer un point de depart
        if (GUILayout.Button("Create Travel Start Point"))
        {
            CreateTravelStartPoint(poi);
        }

        // Bouton pour supprimer le point de depart
        if (poi.GetTravelPathStartPosition() != poi.transform.position && GUILayout.Button("Remove Travel Start Point"))
        {
            RemoveTravelStartPoint(poi);
        }

        // Info sur le point actuel
        Vector3 startPos = poi.GetTravelPathStartPosition();
        if (startPos != poi.transform.position)
        {
            EditorGUILayout.HelpBox($"Custom start point at: ({startPos.x:F2}, {startPos.y:F2}, {startPos.z:F2})", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Using POI center as start point", MessageType.Info);
        }
    }

    private void CreateTravelStartPoint(POI poi)
    {
        // Creer un nouveau GameObject enfant
        GameObject startPoint = new GameObject("TravelStartPoint");
        startPoint.transform.SetParent(poi.transform);

        // Le positionner legerement decale du POI pour qu'il soit visible
        startPoint.transform.localPosition = new Vector3(0.5f, 0f, 0f);

        // Assigner le Transform au POI
        SerializedObject serializedPOI = new SerializedObject(poi);
        SerializedProperty startPointProperty = serializedPOI.FindProperty("travelPathStartPoint");
        startPointProperty.objectReferenceValue = startPoint.transform;
        serializedPOI.ApplyModifiedProperties();

        // Marquer la scene comme modifiee
        EditorUtility.SetDirty(poi);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poi.gameObject.scene);

        // Selectionner le nouveau point pour faciliter son positionnement
        Selection.activeGameObject = startPoint;

        Debug.Log($"Travel start point created for POI '{poi.LocationID}'");
    }

    private void RemoveTravelStartPoint(POI poi)
    {
        SerializedObject serializedPOI = new SerializedObject(poi);
        SerializedProperty startPointProperty = serializedPOI.FindProperty("travelPathStartPoint");

        Transform currentStartPoint = startPointProperty.objectReferenceValue as Transform;
        if (currentStartPoint != null)
        {
            // Supprimer le GameObject
            DestroyImmediate(currentStartPoint.gameObject);

            // Nettoyer la reference
            startPointProperty.objectReferenceValue = null;
            serializedPOI.ApplyModifiedProperties();

            // Marquer comme modifie
            EditorUtility.SetDirty(poi);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(poi.gameObject.scene);

            Debug.Log($"Travel start point removed for POI '{poi.LocationID}'");
        }
    }
}
#endif