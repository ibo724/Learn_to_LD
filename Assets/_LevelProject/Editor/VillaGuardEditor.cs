#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(VillaGuard))]
public sealed class VillaGuardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VillaGuard guard=(VillaGuard)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Can, hız ve görüş değerleri görevdeki denge profilinden alınır. Devriye noktaları dünya koordinatındadır; sahnede seçiliyken noktaları sürükleyebilirsin.",MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(guard)))
        {
            if (GUILayout.Button("İki kısa devriye noktası oluştur"))
            {
                Undo.RecordObject(guard,"Create guard patrol");
                guard.patrol=new[]{guard.transform.position,guard.transform.position+guard.transform.forward*.75f};
                Record(guard);
            }
            if (GUILayout.Button("Devriye noktalarını yakındaki NavMesh'e oturt"))
            {
                Undo.RecordObject(guard,"Snap guard patrol");
                if (guard.patrol!=null)
                    for (int i=0;i<guard.patrol.Length;i++)
                        if (NavMesh.SamplePosition(guard.patrol[i],out NavMeshHit hit,.6f,NavMesh.AllAreas)) guard.patrol[i]=hit.position;
                Record(guard);
            }
        }
        if (GUILayout.Button("Denge profilini seç"))
        {
            VillaMission mission=Object.FindFirstObjectByType<VillaMission>();
            if (mission!=null && mission.balance!=null) Selection.activeObject=mission.balance;
        }
    }

    private void OnSceneGUI()
    {
        VillaGuard guard=(VillaGuard)target;
        if (guard.patrol==null || EditorUtility.IsPersistent(guard)) return;
        Handles.color=new Color(.88f,.61f,.34f);
        for (int i=0;i<guard.patrol.Length;i++)
        {
            Vector3 point=guard.patrol[i];
            Handles.DrawWireDisc(point,Vector3.up,.22f);
            Handles.Label(point+Vector3.up*.25f,"DEVRİYE "+(i+1));
            if (guard.patrol.Length>1) Handles.DrawDottedLine(point,guard.patrol[(i+1)%guard.patrol.Length],5f);
            if (Application.isPlaying) continue;
            EditorGUI.BeginChangeCheck();
            Vector3 moved=Handles.PositionHandle(point,Quaternion.identity);
            if (!EditorGUI.EndChangeCheck()) continue;
            Undo.RecordObject(guard,"Move guard patrol point");
            guard.patrol[i]=moved; Record(guard);
        }
    }

    private static void Record(VillaGuard guard)
    {
        EditorUtility.SetDirty(guard);
        if (PrefabUtility.IsPartOfPrefabInstance(guard)) PrefabUtility.RecordPrefabInstancePropertyModifications(guard);
        if (guard.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(guard.gameObject.scene);
    }
}
#endif
