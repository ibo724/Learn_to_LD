#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VillaFPS.Editor
{
    /// <summary>Shared palette and persistent materials for the generated FPS scene.</summary>
    public static class VillaArt
    {
        public const string MaterialFolder = "Assets/_LevelProject/FPS/Materials";
        public static readonly Color Plaster = Hex("E8DFC9");
        public static readonly Color Ivory = Hex("F3EBDD");
        public static readonly Color Petroleum = Hex("294E58");
        public static readonly Color Copper = Hex("B9784B");
        public static readonly Color Terracotta = Hex("BC6952");
        public static readonly Color Olive = Hex("819361");
        public static readonly Color Stone = Hex("BDBBA5");
        public static readonly Color Turquoise = Hex("63C6BE");
        public static readonly Color Ink = Hex("263B42");

        public static Color Hex(string hex)
        {
            if (!ColorUtility.TryParseHtmlString(hex.StartsWith("#", StringComparison.Ordinal) ? hex : "#" + hex, out Color color))
                throw new ArgumentException("Invalid palette color: " + hex, nameof(hex));
            return color;
        }

        public static Material Mat(string name, Color color, Color? shade = null)
        {
            Material material = GetMaterial(name, "Villa/Matte");
            Color shadeColor = shade ?? new Color(color.r * 0.67f, color.g * 0.76f, color.b * 0.8f, 1);
            material.SetColor("_BaseColor", color);
            material.SetColor("_ShadeColor", shadeColor);
            material.SetFloat("_ShadeThreshold", 0.45f);
            material.SetFloat("_ShadeSoftness", 0.08f);
            material.SetFloat("_AmbientStrength", 0.7f);
            material.SetColor("_AccentColor", Turquoise);
            material.SetFloat("_AccentStrength", 0);
            material.renderQueue = -1;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Water(string name, Color shallow, Color deep)
        {
            Material material = GetMaterial(name, "Villa/Water");
            material.SetColor("_BaseColor", shallow);
            material.SetColor("_DeepColor", deep);
            material.SetColor("_EdgeColor", Hex("D1E7D6"));
            material.SetFloat("_Opacity", 0.72f);
            material.SetFloat("_DepthRange", 1.6f);
            material.SetFloat("_WaveScale", 0.8f);
            material.SetFloat("_WaveSpeed", 0.25f);
            material.SetFloat("_WaveStrength", 0.015f);
            material.SetFloat("_UseDepth", 1);
            material.DisableKeyword("_USEDEPTH_ON");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Sky(string name)
        {
            Material material = GetMaterial(name, "Villa/Sky");
            material.SetColor("_TopColor", Hex("508D9C"));
            material.SetColor("_HorizonColor", Hex("E8D9AE"));
            material.SetColor("_BottomColor", Hex("8FA189"));
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Glass(string name, Color color, float opacity = 0.2f)
        {
            Material material = GetMaterial(name, "Universal Render Pipeline/Lit");
            color.a = Mathf.Clamp01(opacity);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_AlphaClip", 0);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", 0.4f);
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_EnvironmentReflections", 0);
            material.SetFloat("_ReceiveShadows", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material SaveMaterial(Material material, string name)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            EnsureFolder();
            string path = MaterialPath(name);
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == material) return material;
            if (existing != null)
            {
                EditorUtility.CopySerialized(material, existing);
                existing.name = name;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader: " + shaderName);
            EnsureFolder();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, MaterialPath(name));
            }
            else material.shader = shader;
            return material;
        }

        private static string MaterialPath(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("A simple material name is required.", nameof(name));
            return MaterialFolder + "/" + name + ".mat";
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(MaterialFolder)) return;
            string[] folders = MaterialFolder.Split('/');
            string parent = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string path = parent + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folders[i]);
                parent = path;
            }
        }
    }
}
#endif
