using System;
using UnityEditor;
using UnityEngine;

namespace Arawn.TextureStudio.Editor
{
    [CustomEditor(typeof(TextureImporter)), CanEditMultipleObjects]
    public class BaseTextureGeneratorInspector : UnityEditor.Editor
    {
        private UnityEditor.Editor _defaultInspector;
        private bool _showGenerator = true;
        private UniversalShaderConverter.TextureGenerationSettings _settings = UniversalShaderConverter.TextureGenerationSettings.CreateDefault();
        private UniversalShaderConverter.RenderPipeline _pipeline;
        private bool _pipelineInitialized;

        private void OnEnable()
        {
            CreateDefaultInspector();

            if (!_pipelineInitialized)
            {
                _settings = TextureStudioProjectSettings.GetDefaultGenerationSettings();
                _pipeline = TextureStudioProjectSettings.GetDefaultGenerationPipeline();
                _pipelineInitialized = true;
            }
        }

        private void OnDisable()
        {
            if (_defaultInspector != null)
            {
                DestroyImmediate(_defaultInspector);
                _defaultInspector = null;
            }
        }

        public override void OnInspectorGUI()
        {
            if (_defaultInspector != null)
            {
                _defaultInspector.OnInspectorGUI();
            }
            else
            {
                DrawDefaultInspector();
            }

            if (targets == null || targets.Length != 1)
                return;

            TextureImporter importer = target as TextureImporter;
            if (importer == null)
                return;

            Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(importer.assetPath);
            if (baseTexture == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showGenerator = EditorGUILayout.Foldout(_showGenerator, "Texture Generator", true);
            if (_showGenerator)
            {
                DrawGeneratorUI(baseTexture);
            }
            EditorGUILayout.EndVertical();
        }

        private void CreateDefaultInspector()
        {
            if (_defaultInspector != null)
                return;

            var inspectorType = Type.GetType("UnityEditor.TextureImporterInspector, UnityEditor");
            if (inspectorType != null)
            {
                _defaultInspector = CreateEditor(targets, inspectorType);
            }
        }

        private void DrawGeneratorUI(Texture2D baseTexture)
        {
            _pipeline = DrawPipelinePopup(_pipeline);

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Generated textures are saved next to the source texture.", MessageType.Info);

            EditorGUILayout.Space(4);
            _settings.GenerateNormalMap = EditorGUILayout.ToggleLeft("Normal Map", _settings.GenerateNormalMap);
            if (_settings.GenerateNormalMap)
            {
                _settings.NormalMapStrength = EditorGUILayout.IntSlider("Strength", _settings.NormalMapStrength, 1, 10);
            }

            if (_pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateHeightMap, "Height/Parallax Map (optional)");
            }
            else
            {
                _settings.GenerateHeightMap = EditorGUILayout.ToggleLeft("Height/Parallax Map", _settings.GenerateHeightMap);
            }

            if (_settings.GenerateHeightMap)
            {
                _settings.HeightScale = EditorGUILayout.Slider("Height Scale", _settings.HeightScale, 0.01f, 0.2f);
            }

            if (_pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateOcclusionMap, "Occlusion Map (optional)");
            }
            else
            {
                _settings.GenerateOcclusionMap = EditorGUILayout.ToggleLeft("Occlusion Map", _settings.GenerateOcclusionMap);
            }

            if (_settings.GenerateOcclusionMap)
            {
                _settings.OcclusionStrength = EditorGUILayout.Slider("AO Strength", _settings.OcclusionStrength, 0f, 1f);
            }

            string metallicLabel = _pipeline == UniversalShaderConverter.RenderPipeline.HDRP ? "Mask Map (Metallic/AO/Smoothness)" : "Metallic/Smoothness Map";
            _settings.GenerateMetallicMap = EditorGUILayout.ToggleLeft(metallicLabel, _settings.GenerateMetallicMap);
            if (_settings.GenerateMetallicMap)
            {
                _settings.MetallicValue = EditorGUILayout.Slider("Metallic", _settings.MetallicValue, 0f, 1f);
                _settings.SmoothnessValue = EditorGUILayout.Slider("Smoothness", _settings.SmoothnessValue, 0f, 1f);
            }

            if (_pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateBentNormalMap, "Bent Normal Map (optional)");
                if (_settings.GenerateBentNormalMap)
                {
                    _settings.BentNormalStrength = EditorGUILayout.IntSlider("Bent Normal Strength", _settings.BentNormalStrength, 1, 10);
                }

                DrawOptionalToggle(ref _settings.GenerateCoatMask, "Coat Mask (optional)");
                if (_settings.GenerateCoatMask)
                {
                    _settings.CoatCoverage = EditorGUILayout.Slider("Coat Coverage", _settings.CoatCoverage, 0.05f, 0.6f);
                    _settings.CoatFeather = EditorGUILayout.IntSlider("Edge Feather (px)", _settings.CoatFeather, 0, 8);
                }

                DrawOptionalToggle(ref _settings.GenerateDetailMap, "Detail Map (optional)");
                if (_settings.GenerateDetailMap)
                {
                    _settings.DetailStrength = EditorGUILayout.Slider("Detail Strength", _settings.DetailStrength, 0.2f, 2f);
                }

                DrawOptionalToggle(ref _settings.GenerateEmissionMap, "Emission Map (optional)");
            }
            else
            {
                _settings.GenerateEmissionMap = EditorGUILayout.ToggleLeft("Emission Map", _settings.GenerateEmissionMap);
            }

            if (_settings.GenerateEmissionMap)
            {
                _settings.EmissionColor = EditorGUILayout.ColorField("Emission Color", _settings.EmissionColor);
                _settings.EmissionAlgorithm = (UniversalShaderConverter.EmissionAlgorithm)EditorGUILayout.EnumPopup("Algorithm", _settings.EmissionAlgorithm);
                _settings.EmissionCoverage = EditorGUILayout.Slider("Emission Coverage", _settings.EmissionCoverage, 0.02f, 0.5f);
                EditorGUILayout.HelpBox("Higher coverage marks more colorful pixels as emissive.", MessageType.None);
                _settings.EmissionMaskOnly = EditorGUILayout.Toggle("Mask Only (Grayscale)", _settings.EmissionMaskOnly);
                _settings.EmissionBinaryMask = EditorGUILayout.Toggle("Binary Mask (Black/White)", _settings.EmissionBinaryMask);
                _settings.EmissionFeather = EditorGUILayout.IntSlider("Edge Feather (px)", _settings.EmissionFeather, 0, 8);
            }

            bool generationRequested =
                _settings.GenerateNormalMap ||
                _settings.GenerateHeightMap ||
                _settings.GenerateMetallicMap ||
                _settings.GenerateOcclusionMap ||
                _settings.GenerateEmissionMap ||
                (_pipeline == UniversalShaderConverter.RenderPipeline.HDRP && (_settings.GenerateBentNormalMap || _settings.GenerateDetailMap || _settings.GenerateCoatMask));

            using (new EditorGUI.DisabledScope(!generationRequested))
            {
                if (GUILayout.Button("Generate Maps", GUILayout.Height(26)))
                {
                    bool generated = UniversalShaderConverter.GenerateTexturesFromBaseTexture(baseTexture, _settings, _pipeline, true);
                    if (generated)
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        private static UniversalShaderConverter.RenderPipeline DrawPipelinePopup(UniversalShaderConverter.RenderPipeline current)
        {
            string[] labels = { "Built-in", "URP", "HDRP" };
            int index = current == UniversalShaderConverter.RenderPipeline.URP ? 1 : current == UniversalShaderConverter.RenderPipeline.HDRP ? 2 : 0;
            int next = EditorGUILayout.Popup("Target Pipeline", index, labels);
            switch (next)
            {
                case 1:
                    return UniversalShaderConverter.RenderPipeline.URP;
                case 2:
                    return UniversalShaderConverter.RenderPipeline.HDRP;
                default:
                    return UniversalShaderConverter.RenderPipeline.BuiltIn;
            }
        }

        private static void DrawOptionalToggle(ref bool toggleValue, string label)
        {
            toggleValue = EditorGUILayout.ToggleLeft(label, toggleValue);
        }

        private static UniversalShaderConverter.RenderPipeline DetectActivePipeline()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (rp != null)
            {
                string typeName = rp.GetType().Name;
                if (typeName.Contains("HDRenderPipelineAsset") || typeName.Contains("HDRP"))
                    return UniversalShaderConverter.RenderPipeline.HDRP;
                if (typeName.Contains("UniversalRenderPipelineAsset") || typeName.Contains("URP"))
                    return UniversalShaderConverter.RenderPipeline.URP;
            }
            return UniversalShaderConverter.RenderPipeline.BuiltIn;
        }
    }
}
