#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Authoring aids; these controls and scene guides are not included in the player.</summary>
[CustomEditor(typeof(VillaMission))]
public sealed class VillaMissionEditor : Editor
{
    private static bool guardGuides;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        VillaMission mission=(VillaMission)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sahne tasarımı",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Denge değerleri ayrı varlıkta saklanır. Prepare komutu oyun sahnesini yeniden üretir; bu sahneye yaptığın elle eklemeleri önce kaydet.",MessageType.Info);
        if (GUILayout.Button("Denge ayarlarını seç") && mission.balance!=null) Selection.activeObject=mission.balance;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Başlangıç")) Focus(mission.spawn);
            if (GUILayout.Button("Veri cihazı")) Focus(mission.objective);
            if (GUILayout.Button("Çıkış")) Focus(mission.extraction);
        }
        bool next=EditorGUILayout.Toggle("Muhafız görüş yayları",guardGuides);
        if (next!=guardGuides) { guardGuides=next; SceneView.RepaintAll(); }
        if (guardGuides) EditorGUILayout.HelpBox("Yaylar, mesafe ve açı için tasarım kılavuzudur. Gerçek görüş, duvar ve cam sorgularıyla oyun sırasında belirlenir.",MessageType.None);
        if (mission.sceneCamera==null || mission.weaponPrefab==null || mission.spawn==null || mission.objective==null || mission.extraction==null)
            EditorGUILayout.HelpBox("Görevin zorunlu sahne referanslarından biri eksik.",MessageType.Error);
    }

    private static void Focus(Transform marker)
    {
        if (marker==null) return;
        Selection.activeGameObject=marker.gameObject;
        if (SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.Frame(new Bounds(marker.position,Vector3.one*3f),false);
    }

    private void OnSceneGUI()
    {
        VillaMission mission=(VillaMission)target;
        if (mission.spawn!=null)
        {
            Handles.color=new Color(.45f,.8f,.65f);
            Handles.DrawWireDisc(mission.spawn.position,Vector3.up,.3f);
            Handles.DrawLine(mission.spawn.position,mission.spawn.position+Vector3.up*1.65f);
            Handles.ArrowHandleCap(0,mission.spawn.position+Vector3.up*.1f,mission.spawn.rotation,.8f,EventType.Repaint);
            Handles.Label(mission.spawn.position+Vector3.up*1.9f,"BAŞLANGIÇ");
        }
        if (mission.objective!=null)
        {
            Handles.color=new Color(.9f,.7f,.3f);
            Handles.DrawWireCube(mission.objective.position,new Vector3(.6f,.5f,.5f));
            Handles.DrawWireDisc(mission.objective.position,Vector3.up,2.5f);
            Handles.Label(mission.objective.position+Vector3.up*.6f,"VERİ / E / 2,5 m");
        }
        if (mission.extraction!=null)
        {
            Handles.color=new Color(.4f,.8f,.75f);
            Handles.DrawWireDisc(mission.extraction.position,Vector3.up,1.8f);
            Handles.Label(mission.extraction.position+Vector3.up*.3f,"ÇIKIŞ / 1,8 m");
        }
        if (!guardGuides || mission.guards==null) return;
        VillaBalance balance=mission.Balance;
        foreach (VillaGuard guard in mission.guards)
        {
            if (guard==null) continue;
            Vector3 eye=guard.transform.position+Vector3.up*1.5f;
            Handles.color=guard.gameObject.activeSelf ? new Color(.85f,.43f,.28f,.6f) : new Color(.5f,.6f,.65f,.4f);
            Vector3 edge=Quaternion.AngleAxis(-balance.GuardVisionAngle*.5f,Vector3.up)*guard.transform.forward;
            Handles.DrawWireArc(eye,Vector3.up,edge,balance.GuardVisionAngle,balance.GuardViewDistance);
            Handles.DrawLine(eye,eye+edge*balance.GuardViewDistance);
            Handles.DrawLine(eye,eye+Quaternion.AngleAxis(balance.GuardVisionAngle,Vector3.up)*edge*balance.GuardViewDistance);
        }
    }

    [MenuItem("Tools/Villa FPS/Open balance settings")]
    private static void OpenBalance()
    {
        VillaBalance balance=AssetDatabase.LoadAssetAtPath<VillaBalance>("Assets/_LevelProject/FPS/Settings/Villa Balance.asset");
        if (balance!=null) { Selection.activeObject=balance; EditorGUIUtility.PingObject(balance); }
        else Debug.LogWarning("Prepare game scene ile denge varlığını oluştur.");
    }
}

[CustomEditor(typeof(VillaBalance))]
public sealed class VillaBalanceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("GDD başlangıç değerleri bu profilin varsayılanıdır. Oynanış kontrolünde tek seferde bir değeri değiştir. Prepare mevcut profil değerlerini korur.",MessageType.Info);
        serializedObject.Update();
        SerializedProperty property=serializedObject.GetIterator();
        bool enterChildren=true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren=false;
            using (new EditorGUI.DisabledScope(property.name=="m_Script"))
                EditorGUILayout.PropertyField(property,new GUIContent(Label(property.name,property.displayName)),true);
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        if (!GUILayout.Button("GDD başlangıç değerlerine dön (Undo ile geri alınabilir)")) return;
        VillaBalance defaults=ScriptableObject.CreateInstance<VillaBalance>();
        string existingName=target.name;
        Undo.RecordObject(target,"Restore Villa balance defaults");
        EditorUtility.CopySerialized(defaults,target); target.name=existingName;
        DestroyImmediate(defaults); EditorUtility.SetDirty(target);
        serializedObject.Update();
    }

    private static string Label(string name,string fallback)=>name switch
    {
        "playerHealth"=>"Oyuncu canı",
        "walkSpeed"=>"Yürüme hızı (m/s)",
        "sprintSpeed"=>"Koşma hızı (m/s)",
        "jumpHeight"=>"Zıplama yüksekliği (m)",
        "gravity"=>"Yerçekimi (m/s²)",
        "magazineSize"=>"Şarjör kapasitesi",
        "startingReserve"=>"Başlangıç yedek mühimmatı",
        "maxReserve"=>"Yedek mühimmat sınırı",
        "shotDamage"=>"Mermi başına hasar",
        "shotsPerSecond"=>"Saniyedeki atış",
        "reloadSeconds"=>"Doldurma süresi (s)",
        "shotRange"=>"Mermi menzili (m)",
        "hipRecoil"=>"Normal geri tepme (°)",
        "aimRecoil"=>"Nişan alırken geri tepme (°)",
        "guardHealth"=>"Muhafız canı",
        "guardSpeed"=>"Muhafız hızı (m/s)",
        "guardViewDistance"=>"Görüş mesafesi (m)",
        "guardAttackDistance"=>"Ateş mesafesi (m)",
        "guardVisionAngle"=>"Görüş açısı (°)",
        "guardReactionSeconds"=>"İlk tepki süresi (s)",
        "guardSearchSeconds"=>"Arama süresi (s)",
        "guardShotInterval"=>"Atış aralığı (s)",
        "guardDamage"=>"Muhafız mermi hasarı",
        "healing"=>"Can paketi miktarı",
        "ammoPerPack"=>"Mühimmat paketi miktarı",
        _=>fallback
    };
}
#endif
