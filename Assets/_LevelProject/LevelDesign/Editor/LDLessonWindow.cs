using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Teaches a lesson step by step and shows what the scene scan found.</summary>
public sealed class LDLessonWindow : EditorWindow
{
    private static readonly string[] Tabs = { "Ders", "Denetim" };
    private int tab;
    private Vector2 scroll;

    [MenuItem("Tools/Level Design/Ders Penceresi")]
    private static void OpenLesson() => Open(0);

    [MenuItem("Tools/Level Design/Sahneyi Denetle")]
    private static void OpenAudit() => Open(1);

    private static void Open(int tab)
    {
        var window = GetWindow<LDLessonWindow>();
        window.titleContent = new GUIContent("Level Design");
        window.tab = tab;
        window.Show();
    }

    private void OnEnable()
    {
        LDScanService.Changed += Repaint;
        LDScanService.RequestScan();
    }

    private void OnDisable() => LDScanService.Changed -= Repaint;

    private void OnSelectionChange() => Repaint();

    private void OnGUI()
    {
        LDScanResult scan = LDScanService.Last;
        tab = GUILayout.Toolbar(tab, Tabs);
        if (scan.Pieces.Count == 0)
            EditorGUILayout.HelpBox("Sahnede rolü olan parça yok. Önce parçalara rol ver (aşağıdaki 'Rol ata' bölümü).", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tab == 0) DrawLesson(scan);
        else DrawFindings(scan.Findings.OrderBy(f => f.Severity).ToList());
        EditorGUILayout.EndScrollView();

        DrawRoleButtons();
        EditorGUILayout.Space();
        if (GUILayout.Button("Kontrol et", GUILayout.Height(26))) LDScanService.ScanNow();
        EditorGUILayout.LabelField("Not: eğik açıyla döndürülmüş parçalar yaklaşık ölçülür.", EditorStyles.miniLabel);
    }

    private void DrawLesson(LDScanResult scan)
    {
        var lessons = LDLessons.All;
        int lessonIndex = Mathf.Max(0, lessons.ToList().FindIndex(l => l.Id == LDProgress.LessonId));
        int chosen = EditorGUILayout.Popup("Ders", lessonIndex, lessons.Select(l => l.Title).ToArray());
        LDLesson lesson = lessons[chosen];
        if (LDProgress.LessonId != lesson.Id) LDProgress.LessonId = lesson.Id;

        int current = Mathf.Clamp(LDProgress.GetStep(lesson.Id), 0, lesson.Steps.Length - 1);
        EditorGUILayout.Space();
        for (int i = 0; i < lesson.Steps.Length; i++)
        {
            int stepIndex = i;
            LDStep step = lesson.Steps[i];
            bool autoOk = step.Check == null || step.Check(scan).Passed;
            string mark = step.IsPassed(scan, m => LDProgress.GetManual(lesson.Id, stepIndex, m)) ? "✓" : autoOk ? "•" : "✗";
            var style = new GUIStyle(EditorStyles.label) { fontStyle = i == current ? FontStyle.Bold : FontStyle.Normal };
            if (GUILayout.Button($"{mark}  {step.Title}", style))
            {
                current = i;
                LDProgress.SetStep(lesson.Id, i);
            }
        }

        DrawStep(lesson, current, scan);
    }

    private void DrawStep(LDLesson lesson, int index, LDScanResult scan)
    {
        LDStep step = lesson.Steps[index];
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(step.Title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Ne yapıyoruz", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(step.What, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Neden", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(step.Why, EditorStyles.wordWrappedLabel);

        if (step.Table.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ölçü tablosu (Position | Scale)", EditorStyles.miniBoldLabel);
            foreach (LDTableRow row in step.Table)
                EditorGUILayout.SelectableLabel($"{row.Name}   ({LDRules.RoleLabel(row.Role)})    {V(row.Position)}   |   {V(row.Scale)}", GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        if (step.Terms.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Terimler", EditorStyles.miniBoldLabel);
            foreach (var (term, meaning) in step.Terms)
                EditorGUILayout.LabelField($"<b>{term}:</b> {meaning}", new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true });
        }

        if (step.Action != null)
        {
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button(step.ActionLabel, GUILayout.Height(24))) EditorApplication.delayCall += () => step.Action();
        }

        for (int i = 0; i < step.ManualChecks.Length; i++)
        {
            bool ticked = LDProgress.GetManual(lesson.Id, index, i);
            bool now = EditorGUILayout.ToggleLeft(step.ManualChecks[i], ticked);
            if (now != ticked) LDProgress.SetManual(lesson.Id, index, i, now);
        }

        if (step.Check != null)
            foreach (string problem in step.Check(scan).Problems)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);

        DrawFindings(scan.ForRules(step.Rules).OrderBy(f => f.Severity).ToList());
    }

    private static void DrawFindings(System.Collections.Generic.List<LDFinding> findings)
    {
        if (findings.Count == 0) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bulgular", EditorStyles.miniBoldLabel);
        foreach (LDFinding f in findings)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Icon(f.Severity), GUILayout.Width(18), GUILayout.Height(18));
                GUILayout.Label($"Kural {f.Rule}: {f.Message}", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Göster", GUILayout.Width(60))) Show(f);
            }
        }
    }

    private static void Show(LDFinding f)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (f.Target != null)
        {
            Selection.activeObject = f.Target;
            view?.FrameSelected();
        }
        else view?.LookAt(f.Position);
    }

    private static void DrawRoleButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rol ata", EditorStyles.boldLabel);
        GameObject[] selected = Selection.gameObjects;
        using (new EditorGUI.DisabledScope(Application.isPlaying || selected.Length == 0))
        {
            var roles = (LDRoleType[])System.Enum.GetValues(typeof(LDRoleType));
            for (int i = 0; i < roles.Length; i += 2)
            using (new EditorGUILayout.HorizontalScope())
            for (int j = i; j < Mathf.Min(i + 2, roles.Length); j++)
                if (GUILayout.Button($"Seçilenler → {LDRules.RoleLabel(roles[j])}"))
                {
                    LDRoleAssigner.Assign(selected, roles[j]);
                    LDScanService.RequestScan();
                }
        }
        if (selected.Length == 0) EditorGUILayout.LabelField("Rol vermek için sahnede parça seç.", EditorStyles.miniLabel);
    }

    private static GUIContent Icon(LDSeverity s) => EditorGUIUtility.IconContent(
        s == LDSeverity.Hata ? "console.erroricon.sml" : s == LDSeverity.Uyari ? "console.warnicon.sml" : "console.infoicon.sml");

    private static string V(Vector3 v) => $"{LDRules.M(v.x)}; {LDRules.M(v.y)}; {LDRules.M(v.z)}";
}
