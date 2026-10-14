#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VillaFPS.Editor
{
    /// <summary>Builds an isolated Forward pipeline for the FPS's base and viewmodel cameras.</summary>
    public static class VillaRendering
    {
        public const string PipelinePath = "Assets/_LevelProject/FPS/Rendering/VillaForwardPipeline.asset";
        public const string RendererPath = "Assets/_LevelProject/FPS/Rendering/VillaForwardRenderer.asset";
        private const string SourcePipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private const string SourceRendererPath = "Assets/Settings/PC_Renderer.asset";

        public static UniversalRenderPipelineAsset CreatePipeline()
        {
            UniversalRenderPipelineAsset sourcePipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SourcePipelinePath);
            UniversalRendererData sourceRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SourceRendererPath);
            if (sourcePipeline == null || sourceRenderer == null)
                throw new InvalidOperationException("The source PC render pipeline or renderer is missing.");
            EnsureFolder("Assets/_LevelProject/FPS/Rendering");

            UniversalRendererData rendererCopy = UnityEngine.Object.Instantiate(sourceRenderer);
            rendererCopy.name = System.IO.Path.GetFileNameWithoutExtension(RendererPath);
            rendererCopy.renderingMode = RenderingMode.Forward;
            rendererCopy.useNativeRenderPass = false;
            // Feature instances are subassets of the source renderer. Do not share or mutate them.
            rendererCopy.rendererFeatures.Clear();
            SerializedObject rendererSerialized = new SerializedObject(rendererCopy);
            SerializedProperty featureMap = rendererSerialized.FindProperty("m_RendererFeatureMap");
            if (featureMap != null) featureMap.ClearArray();
            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            UniversalRendererData renderer = Persist(rendererCopy, RendererPath);
            renderer.SetDirty();

            UniversalRenderPipelineAsset pipelineCopy = UnityEngine.Object.Instantiate(sourcePipeline);
            pipelineCopy.name = System.IO.Path.GetFileNameWithoutExtension(PipelinePath);
            pipelineCopy.msaaSampleCount = 4;
            pipelineCopy.supportsCameraDepthTexture = true;
            pipelineCopy.supportsCameraOpaqueTexture = true;
            SerializedObject pipelineSerialized = new SerializedObject(pipelineCopy);
            SerializedProperty renderers = pipelineSerialized.FindProperty("m_RendererDataList");
            SerializedProperty defaultRenderer = pipelineSerialized.FindProperty("m_DefaultRendererIndex");
            if (renderers == null || defaultRenderer == null)
            {
                UnityEngine.Object.DestroyImmediate(pipelineCopy);
                throw new InvalidOperationException("URP renderer serialization fields are unavailable.");
            }
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            defaultRenderer.intValue = 0;
            pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();
            UniversalRenderPipelineAsset pipeline = Persist(pipelineCopy, PipelinePath);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            return pipeline;
        }

        private static T Persist<T>(T copy, string path) where T : UnityEngine.Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(copy, path);
                return copy;
            }
            EditorUtility.CopySerialized(copy, existing);
            UnityEngine.Object.DestroyImmediate(copy);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string path = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = path;
            }
        }
    }
}
#endif
