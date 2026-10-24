using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

/// <summary>Scene view switch for the level design measurements drawn by LDSceneDrawer.</summary>
[Overlay(typeof(SceneView), "ld-gorunumu", "LD Görünümü")]
public sealed class LDSceneOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var toggle = new Toggle("Ölçüleri göster") { value = LDSceneDrawer.Enabled };
        toggle.RegisterValueChangedCallback(e => LDSceneDrawer.Enabled = e.newValue);
        return toggle;
    }
}
