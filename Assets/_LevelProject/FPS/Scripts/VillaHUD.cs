using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class VillaHUD : MonoBehaviour
{
    public VillaMission mission;

    private const float ReferenceWidth = 1440f;
    private const float ReferenceHeight = 900f;
    private static readonly Color Ink = new Color(.055f, .12f, .15f, .97f);
    private static readonly Color Panel = new Color(.10f, .19f, .22f, .98f);
    private static readonly Color Cream = new Color(.95f, .92f, .84f);
    private static readonly Color Muted = new Color(.65f, .73f, .72f);
    private static readonly Color Copper = new Color(.82f, .49f, .29f);
    private static readonly Color Sage = new Color(.61f, .74f, .57f);
    private static readonly Color Danger = new Color(.94f, .40f, .29f);

    private GUIStyle small, normal, heading, title, number, buttonLabel, rightSmall, centered;
    private GUISkin styledSkin;
    private bool settings, clearFocus;
    private int selected;
    private float width, height, scale;
    private VillaMission.Phase previousPhase;
    private Vector2 pointer = new Vector2(-10000f, -10000f);
    private int pressedButton = -1, draggingSlider = -1, adjustDirection;
    private float nextAdjust;
    private int lastScreenWidth, lastScreenHeight;

    private int ControlCount => settings || mission.State != VillaMission.Phase.Briefing ? 4 : 3;

    // Mission calls this before its pause/resume shortcut so Esc first leaves settings.
    public bool TryHandleEscape()
    {
        if (!settings) return false;
        if (mission != null) mission.SaveSettings();
        settings = false;
        selected = 1;
        clearFocus = true;
        ResetPointerCapture();
        return true;
    }

    private void Update()
    {
        if (mission == null || mission.Player == null || mission.Health == null || mission.Weapon == null) return;
        UpdateCanvas();
        SyncPhase();
        if (mission.Playing || !Application.isFocused)
        {
            ResetPointerCapture();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        // Mission owns Esc. Do not let another menu action share the pause/resume frame.
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) return;
        if (HandleMenuKeyboard(keyboard)) return;
        HandleMenuPointer(Mouse.current);
    }

    private void UpdateCanvas()
    {
        if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
        {
            ResetPointerCapture();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
        scale = Mathf.Max(.01f, Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight));
        width = Screen.width / scale;
        height = Screen.height / scale;
    }

    private void SyncPhase()
    {
        if (mission.State == previousPhase) return;
        settings = false;
        selected = 0;
        clearFocus = true;
        previousPhase = mission.State;
        ResetPointerCapture();
    }

    private void ResetPointerCapture()
    {
        pressedButton = -1;
        draggingSlider = -1;
        adjustDirection = 0;
        pointer = new Vector2(-10000f, -10000f);
    }

    // One geometry source is shared by rendering and Input System hit tests.
    private Rect ControlRect(int index)
    {
        float left = (width - 1280) * .5f;
        float top = (height - ReferenceHeight) * .5f + 60;
        float x = left + 28;
        if (settings)
        {
            float y = top + 286;
            if (index == 0) return new Rect(x, y + 117, 720, 32);
            if (index == 1) return new Rect(x, y + 222, 720, 32);
            if (index == 2) return new Rect(x, y + 286, 720, 54);
            return new Rect(x, y + 430, 350, 56);
        }
        float buttonsY = top + 555;
        if (index == 0) return new Rect(x, buttonsY, 720, 60);
        if (index == 1) return new Rect(x, buttonsY + 76, 350, 56);
        if (index == 2 && mission.State != VillaMission.Phase.Briefing)
            return new Rect(left + 398, buttonsY + 76, 350, 56);
        return new Rect(x, buttonsY + 148, 350, 56);
    }

    private void HandleMenuPointer(Mouse mouse)
    {
        if (mouse == null) { ResetPointerCapture(); return; }
        Vector2 physical = mouse.position.ReadValue();
        pointer = new Vector2(physical.x / scale, (Screen.height - physical.y) / scale);
        if (mouse.leftButton.wasPressedThisFrame)
        {
            ResetPointerCapture();
            pointer = new Vector2(physical.x / scale, (Screen.height - physical.y) / scale);
            for (int index = 0; index < ControlCount; index++)
            {
                if (!ControlRect(index).Contains(pointer)) continue;
                selected = index;
                if (settings && index < 2) draggingSlider = index;
                else pressedButton = index;
                break;
            }
        }
        if (draggingSlider >= 0 && (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame))
        {
            Rect rect = ControlRect(draggingSlider);
            float value = Mathf.Clamp01((pointer.x - rect.x - 7f) / (rect.width - 14f));
            if (draggingSlider == 0) mission.Sensitivity = Mathf.Lerp(.03f, .3f, value);
            else { mission.Volume = value; AudioListener.volume = value; }
        }
        if (!mouse.leftButton.wasReleasedThisFrame) return;
        int released = pressedButton;
        bool clicked = released >= 0 && ControlRect(released).Contains(pointer);
        ResetPointerCapture();
        if (clicked) ActivateControl(released);
    }

    private GUIStyle Style(int size, Color color, FontStyle weight = FontStyle.Normal)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = weight,
            wordWrap = true,
            richText = false,
            padding = new RectOffset(),
            margin = new RectOffset()
        };
        style.normal.textColor = color;
        return style;
    }

    private void InitStyles()
    {
        styledSkin = GUI.skin;
        small = Style(19, Muted);
        normal = Style(24, Cream);
        heading = Style(31, Cream, FontStyle.Bold);
        title = Style(72, Cream, FontStyle.Bold);
        number = Style(46, Cream, FontStyle.Bold);
        buttonLabel = Style(23, Cream, FontStyle.Bold);
        buttonLabel.alignment = TextAnchor.MiddleLeft;
        centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
        rightSmall = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
    }

    private static void Fill(Rect rect, Color color)
    {
        Color before = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = before;
    }

    private static void Stroke(Rect rect, Color color, float thickness = 1f)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void Text(float x, float y, float w, float h, string text, GUIStyle style)
    {
        GUI.Label(new Rect(x, y, w, h), text, style);
    }

    private void OnGUI()
    {
        if (mission == null || mission.Player == null || mission.Health == null || mission.Weapon == null) return;
        if (normal == null || styledSkin != GUI.skin) InitStyles();

        Matrix4x4 beforeMatrix = GUI.matrix;
        Color beforeColor = GUI.color;
        UpdateCanvas();
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.color = Color.white;
        try
        {
            SyncPhase();
            if (clearFocus)
            {
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                clearFocus = false;
            }
            if (mission.Playing) DrawGame();
            else DrawMenu();
        }
        finally
        {
            GUI.matrix = beforeMatrix;
            GUI.color = beforeColor;
        }
    }

    private void DrawGame()
    {
        float centerX = width * .5f;
        float centerY = height * .5f;
        bool lowHealth = mission.Health.Current / Mathf.Max(1f, mission.Health.maximum) <= .35f;
        Color healthColor = lowHealth ? Danger : Sage;

        Fill(new Rect(28, 28, 425, 112), Ink);
        Fill(new Rect(28, 28, 4, 112), Copper);
        Text(49, 41, 385, 24, "SAHA 01  /  VİLLA OPERASYONU", small);
        Text(49, 72, 385, 34, mission.HasData ? "ÇIKIŞA ULAŞ" : "VERİ CİHAZINI BUL", normal);
        Fill(new Rect(49, 119, 22, 4), Sage);
        Fill(new Rect(77, 119, 22, 4), mission.HasData ? Sage : new Color(.29f, .35f, .35f));
        Text(111, 108, 320, 26, mission.HasData ? "Veri sende · Bahçeye dön" : "Hedef · Üst kat", small);

        Fill(new Rect(width - 278, 28, 250, mission.HasData ? 112 : 80), Ink);
        Text(width - 257, 40, 208, 30, FormatTime(mission.Elapsed), rightSmall);
        Text(width - 257, 71, 208, 30, "MUHAFIZ  " + GuardProgress(), rightSmall);
        if (mission.HasData && mission.extraction != null)
        {
            float distance = Vector3.Distance(mission.Player.transform.position, mission.extraction.position);
            Text(width - 257, 102, 208, 30, "ÇIKIŞ  " + Mathf.CeilToInt(distance) + " m", rightSmall);
            DrawExtractionMarker();
        }

        float bottom = height - 148;
        Fill(new Rect(28, bottom, 250, 120), Ink);
        Text(49, bottom + 13, 215, 25, lowHealth ? "DURUM / KRİTİK" : "DURUM / CAN", small);
        Text(49, bottom + 37, 140, 57, Mathf.CeilToInt(mission.Health.Current).ToString("000"), number);
        Text(186, bottom + 62, 70, 25, "/ " + Mathf.RoundToInt(mission.Health.maximum), small);
        Fill(new Rect(49, bottom + 102, 207, 5), new Color(.24f, .32f, .33f));
        Fill(new Rect(49, bottom + 102, 207 * Mathf.Clamp01(mission.Health.Current / Mathf.Max(1f, mission.Health.maximum)), 5), healthColor);
        if (lowHealth) Fill(new Rect(28, bottom, 4, 120), Danger);

        float ammoX = width - 300;
        Fill(new Rect(ammoX, bottom, 272, 120), Ink);
        string ammoState = mission.Weapon.Reloading ? "ŞARJÖR DOLDURULUYOR" :
            mission.Weapon.Magazine == 0 && mission.Weapon.Reserve == 0 ? "MÜHİMMAT TÜKENDİ" : "KARABİNA / 06";
        Text(ammoX + 21, bottom + 13, 235, 25, ammoState, small);
        Color old = number.normal.textColor;
        number.normal.textColor = mission.Weapon.Magazine == 0 ? Danger : Cream;
        Text(ammoX + 21, bottom + 37, 95, 57, mission.Weapon.Magazine.ToString("00"), number);
        number.normal.textColor = old;
        Text(ammoX + 121, bottom + 60, 130, 30, "/ " + mission.Weapon.Reserve.ToString("000"), normal);
        Fill(new Rect(ammoX + 21, bottom + 102, 230, 5), new Color(.24f, .32f, .33f));
        if (mission.Weapon.Reloading)
            Fill(new Rect(ammoX + 21, bottom + 102, 230 * Mathf.Clamp01(mission.Weapon.ReloadProgress), 5), Copper);

        DrawReticle(centerX, centerY);
        if (!string.IsNullOrEmpty(mission.Interaction))
        {
            Fill(new Rect(centerX - 250, centerY + 54, 500, 48), Ink);
            Text(centerX - 230, centerY + 65, 460, 34, mission.Interaction, normal);
        }
        else if (!mission.Weapon.Reloading && mission.Weapon.Magazine <= 5 && mission.Weapon.Reserve > 0)
            Text(centerX - 200, centerY + 56, 400, 30, "R  /  ŞARJÖRÜ DOLDUR", centered);

        if (Time.unscaledTime < mission.NoticeUntil && !string.IsNullOrEmpty(mission.Notice))
        {
            Fill(new Rect(centerX - 355, 166, 710, 58), Ink);
            Fill(new Rect(centerX - 355, 166, 4, 58), Copper);
            Text(centerX - 331, 181, 663, 36, mission.Notice, normal);
        }
        DrawDamageDirection(centerX, centerY);
        Text(centerX - 330, height - 56, 660, 28, "E  ETKİLEŞİM     R  ŞARJÖR     ESC  DURAKLAT", centered);
    }

    private void DrawReticle(float x, float y)
    {
        bool hit = Time.unscaledTime < mission.HitUntil;
        Color color = hit ? Copper : Cream;
        Fill(new Rect(x - 1, y - 1, 3, 3), color);
        if (!mission.Weapon.Aiming)
        {
            const float gap = 8f;
            Fill(new Rect(x - gap - 6, y, 6, 2), color);
            Fill(new Rect(x + gap, y, 6, 2), color);
            Fill(new Rect(x, y - gap - 6, 2, 6), color);
            Fill(new Rect(x, y + gap, 2, 6), color);
        }
        if (hit)
        {
            Line(new Vector2(x - 13, y - 13), new Vector2(x - 7, y - 7), color, 2);
            Line(new Vector2(x + 7, y + 7), new Vector2(x + 13, y + 13), color, 2);
            Line(new Vector2(x + 7, y - 7), new Vector2(x + 13, y - 13), color, 2);
            Line(new Vector2(x - 13, y + 13), new Vector2(x - 7, y + 7), color, 2);
        }
    }

    private void DrawDamageDirection(float x, float y)
    {
        if (Time.unscaledTime >= mission.HurtUntil) return;
        float alpha = Mathf.Clamp01((mission.HurtUntil - Time.unscaledTime) / .3f);
        Color color = new Color(Danger.r, Danger.g, Danger.b, alpha * .85f);
        Fill(new Rect(0, 0, width, 5), color);
        Fill(new Rect(0, height - 5, width, 5), color);
        Vector3 local = mission.Player.transform.InverseTransformPoint(mission.HurtSource);
        if (Mathf.Abs(local.x) > Mathf.Abs(local.z))
            Fill(new Rect(x + (local.x < 0 ? -92 : 87), y - 18, 5, 36), color);
        else Fill(new Rect(x - 18, y + (local.z > 0 ? -92 : 87), 36, 5), color);
    }

    private void DrawExtractionMarker()
    {
        if (mission.sceneCamera == null) return;
        Vector3 point = mission.sceneCamera.WorldToScreenPoint(mission.extraction.position + Vector3.up * 1.5f);
        float x = point.x / scale;
        float y = (Screen.height - point.y) / scale;
        if (point.z <= 0 || x < 90 || x > width - 90 || y < 250 || y > height - 180) return;
        Stroke(new Rect(x - 12, y - 12, 24, 24), Sage, 2);
        Fill(new Rect(x - 46, y + 24, 92, 28), Ink);
        Text(x - 43, y + 26, 86, 24, "ÇIKIŞ", centered);
    }

    private static void Line(Vector2 from, Vector2 to, Color color, float thickness)
    {
        Matrix4x4 before = GUI.matrix;
        Vector2 delta = to - from;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
        Fill(new Rect(from.x, from.y - thickness * .5f, delta.magnitude, thickness), color);
        GUI.matrix = before;
    }

    private void DrawMenu()
    {
        Fill(new Rect(0, 0, width, height), new Color(.035f, .08f, .10f, .93f));
        float left = (width - 1280) * .5f;
        float top = (height - ReferenceHeight) * .5f + 60;
        Fill(new Rect(left, top, 5, 774), Copper);
        Text(left + 28, top + 8, 740, 28, "SAHA DOSYASI 01  /  AKDENİZ", small);
        Text(left + 24, top + 60, 820, 174, "VİLLA\nOPERASYONU", title);
        DrawDossier(left + 826, top + 8);

        if (settings)
        {
            DrawSettings(left + 28, top + 286);
            return;
        }

        bool won = mission.State == VillaMission.Phase.Won;
        bool lost = mission.State == VillaMission.Phase.Lost;
        bool paused = mission.State == VillaMission.Phase.Paused;
        string status = won ? "GÖREV TAMAMLANDI" : lost ? "OPERASYON BAŞARISIZ" :
            paused ? "DURAKLATILDI" : "GİR. VERİYİ AL. GERİ DÖN.";
        Fill(new Rect(left + 28, top + 278, 44, 4), won ? Sage : lost ? Danger : Copper);
        Text(left + 28, top + 299, 760, 48, status, heading);

        if (won || lost)
        {
            DrawResults(left + 28, top + 371);
            Text(left + 28, top + 484, 730, 60, won ?
                "Veri güvenli bölgede. Operasyon kapatıldı." :
                "Korunakları kullan. Doldururken açıkta kalma; tekrar dene.", normal);
        }
        else
        {
            Text(left + 28, top + 371, 720, 95, paused ?
                (mission.HasData ? "Veri cihazı sende. Bahçedeki işaretli çıkışa ulaş." :
                "Üst kattaki veri cihazını bul. Cihazı aldıktan sonra bahçedeki çıkışa dön.") :
                "Villaya gir, üst kattaki veri cihazını al ve bahçedeki işaretli çıkışa ulaş.", normal);
            Text(left + 28, top + 477, 720, 52,
                "Köşeleri ve duvarları korunak olarak kullan. Tüm muhafızları etkisizleştirmek zorunda değilsin.", small);
        }

        Button(0, paused ? "DEVAM ET" : won || lost ? "YENİDEN DENE" : "OPERASYONU BAŞLAT", true);
        Button(1, "AYARLAR");
        bool briefing = mission.State == VillaMission.Phase.Briefing;
        if (!briefing) Button(2, "BAŞLANGICA DÖN");
        int quitIndex = briefing ? 2 : 3;
        Button(quitIndex, "OYUNDAN ÇIK");
        Text(left + 28, top + 765, 720, 30, "↑ ↓  SEÇ     ENTER  ONAYLA", small);
    }

    private void DrawDossier(float x, float y)
    {
        Fill(new Rect(x, y, 426, 742), Ink);
        Fill(new Rect(x, y, 426, 4), Copper);
        Text(x + 26, y + 28, 370, 34, "OPERASYON ROTASI", heading);
        string[] stages = { "01   VİLLAYA GİR", "02   VERİ CİHAZINI AL", "03   ÇIKIŞA ULAŞ" };
        for (int i = 0; i < stages.Length; i++)
        {
            float row = y + 91 + i * 48;
            Fill(new Rect(x + 26, row + 8, 8, 8), i == 1 && mission.HasData ? Sage : Copper);
            Text(x + 50, row, 350, 34, stages[i], small);
        }
        Fill(new Rect(x + 26, y + 254, 374, 1), new Color(.23f, .32f, .33f));
        Text(x + 26, y + 280, 370, 34, "KONTROLLER", heading);
        string[] keys = { "W A S D", "FARE", "SOL TIK", "SAĞ TIK", "SHIFT", "SPACE", "R", "E", "ESC" };
        string[] actions = { "Hareket", "Bakış", "Ateş", "Nişan", "Koşu", "Zıplama", "Şarjör doldur", "Etkileşim", "Duraklat / geri" };
        for (int i = 0; i < keys.Length; i++)
        {
            float row = y + 337 + i * 36;
            Text(x + 26, row, 127, 28, keys[i], small);
            Text(x + 165, row - 2, 235, 30, actions[i], normal);
        }
        Text(x + 26, y + 694, 370, 34, "VERİYİ AL. GERİ DÖN.", small);
    }

    private void DrawResults(float x, float y)
    {
        int accuracy = mission.Shots <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(100f * mission.Hits / mission.Shots), 0, 100);
        string[] labels = { "SÜRE", "MUHAFIZ", "İSABET" };
        string[] values = { FormatTime(mission.Elapsed), GuardProgress(), "%" + accuracy };
        for (int i = 0; i < labels.Length; i++)
        {
            float column = x + i * 245;
            Fill(new Rect(column, y, 230, 90), Panel);
            Text(column + 15, y + 11, 205, 25, labels[i], small);
            Text(column + 15, y + 37, 205, 45, values[i], heading);
        }
    }

    private void DrawSettings(float x, float y)
    {
        Text(x, y, 720, 43, "AYARLAR", heading);
        float percent = Mathf.InverseLerp(.03f, .3f, mission.Sensitivity);
        Text(x, y + 69, 540, 34, "Fare hassasiyeti", normal);
        Text(x + 560, y + 73, 160, 30, Mathf.RoundToInt(percent * 100) + " / 100", rightSmall);
        Slider(0, mission.Sensitivity, .03f, .3f);

        Text(x, y + 174, 540, 34, "Ana ses", normal);
        Text(x + 560, y + 178, 160, 30, "%" + Mathf.RoundToInt(mission.Volume * 100), rightSmall);
        Slider(1, mission.Volume, 0f, 1f);

        Button(2, "KAMERA TEPKİSİ");
        Text(x + 410, y + 303, 180, 26, mission.CameraShake ? "AÇIK" : "KAPALI", rightSmall);
        Fill(new Rect(x + 613, y + 301, 79, 24), mission.CameraShake ? Sage : new Color(.25f, .33f, .34f));
        Fill(new Rect(x + (mission.CameraShake ? 669 : 617), y + 305, 19, 16), Cream);
        Text(x, y + 359, 720, 60,
            "Kamera tepkisi, ateş ederken oluşan bakış hareketini açar. Kapattığında isabet ve hasar göstergeleri görünmeye devam eder.", small);
        Button(3, "GERİ");
        Text(x + 370, y + 443, 350, 35, "← →  AYARLA     ESC  GERİ", small);
    }

    private void Slider(int index, float value, float min, float max)
    {
        Rect rect = ControlRect(index);
        bool hover = rect.Contains(pointer);
        float normalized = Mathf.InverseLerp(min, max, value);
        Fill(new Rect(rect.x, rect.y + 11, rect.width, 6), new Color(.24f, .32f, .33f));
        Fill(new Rect(rect.x, rect.y + 11, rect.width * normalized, 6), Copper);
        if (selected == index) Fill(new Rect(rect.x - 12, rect.y + 6, 3, 18), Cream);
        Fill(new Rect(rect.x + (rect.width - 14f) * normalized, rect.y, 14, 28),
            selected == index || hover || draggingSlider == index ? Cream : Copper);
    }

    private void Button(int index, string label, bool primary = false)
    {
        Rect rect = ControlRect(index);
        bool hover = rect.Contains(pointer);
        bool focused = selected == index;
        Color background = primary ? Copper : hover || focused ? new Color(.16f, .28f, .30f) : Panel;
        if (pressedButton == index && hover) background = Color.Lerp(background, Ink, .18f);
        Fill(rect, background);
        if (!primary) Fill(new Rect(rect.x, rect.y, 3, rect.height), focused || hover ? Copper : Muted);
        if (focused) Stroke(rect, primary ? Cream : Copper, 1);
        Color before = buttonLabel.normal.textColor;
        buttonLabel.normal.textColor = primary ? Ink : Cream;
        Text(rect.x + 22, rect.y, rect.width - 38, rect.height, label, buttonLabel);
        buttonLabel.normal.textColor = before;
    }

    private bool HandleMenuKeyboard(Keyboard keyboard)
    {
        if (keyboard == null) return false;
        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame && !shift)
            selected = (selected + 1) % ControlCount;
        else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame && shift)
            selected = (selected + ControlCount - 1) % ControlCount;
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            ActivateControl(selected);
            return true;
        }

        int direction = settings ? (keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.leftArrowKey.isPressed ? 1 : 0) : 0;
        if (direction == 0) adjustDirection = 0;
        else if (direction != adjustDirection || Time.unscaledTime >= nextAdjust)
        {
            bool fresh = direction != adjustDirection;
            adjustDirection = direction;
            nextAdjust = Time.unscaledTime + (fresh ? .35f : .07f);
            if (selected == 0) mission.Sensitivity = Mathf.Clamp(mission.Sensitivity + direction * .01f, .03f, .3f);
            else if (selected == 1)
            {
                mission.Volume = Mathf.Clamp01(mission.Volume + direction * .05f);
                AudioListener.volume = mission.Volume;
            }
            else if (selected == 2) mission.CameraShake = direction > 0;
        }
        return false;
    }

    private void ActivateControl(int index)
    {
        if (index < 0 || index >= ControlCount) return;
        if (!settings) { Activate(index); return; }
        clearFocus = true;
        ResetPointerCapture();
        if (index == 2) mission.CameraShake = !mission.CameraShake;
        else if (index == 3) TryHandleEscape();
    }

    private void Activate(int index)
    {
        clearFocus = true;
        if (index == 0)
        {
            if (mission.State == VillaMission.Phase.Briefing) mission.Begin();
            else if (mission.State == VillaMission.Phase.Paused) mission.Resume();
            else mission.Restart();
        }
        else if (index == 1) { settings = true; selected = 0; ResetPointerCapture(); }
        else if (mission.State == VillaMission.Phase.Briefing || index == 3) mission.Quit();
        else mission.Briefing();
    }

    private string GuardProgress()
    {
        int total = 0;
        if (mission.guards != null)
            foreach (VillaGuard guard in mission.guards) if (guard != null) total++;
        return total > 0 ? mission.Kills + "/" + total : mission.Kills.ToString();
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return total >= 3600 ? $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
    }
}
