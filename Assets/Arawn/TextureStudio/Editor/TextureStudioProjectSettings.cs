using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arawn.TextureStudio.Editor
{
    internal enum TextureSlot
    {
        Base,
        Normal,
        Height,
        MetallicMask,
        Occlusion,
        Emission,
        BentNormal,
        Detail,
        CoatMask
    }

    [Serializable]
    internal class ShaderTexturePropertyMap
    {
        [Tooltip("Shader name substring match (case-insensitive). First match wins.")]
        public string shaderNameContains = string.Empty;

        [Tooltip("Base/albedo property names for this shader.")]
        public string[] baseMapProperties = Array.Empty<string>();

        [Tooltip("Normal map property names for this shader.")]
        public string[] normalMapProperties = Array.Empty<string>();

        [Tooltip("Height/parallax property names for this shader.")]
        public string[] heightMapProperties = Array.Empty<string>();

        [Tooltip("Metallic/Mask map property names for this shader.")]
        public string[] metallicMaskProperties = Array.Empty<string>();

        [Tooltip("Occlusion map property names for this shader.")]
        public string[] occlusionMapProperties = Array.Empty<string>();

        [Tooltip("Emission map property names for this shader.")]
        public string[] emissionMapProperties = Array.Empty<string>();

        [Tooltip("Bent normal map property names for this shader.")]
        public string[] bentNormalMapProperties = Array.Empty<string>();

        [Tooltip("Detail map property names for this shader.")]
        public string[] detailMapProperties = Array.Empty<string>();

        [Tooltip("Coat mask property names for this shader.")]
        public string[] coatMaskMapProperties = Array.Empty<string>();
    }

    internal class TextureStudioProjectSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/TextureStudioSettings.asset"; // Preferred location
        private const string LegacySettingsPath = "ProjectSettings/TextureStudioSettings.asset"; // Legacy, read-only; we copy from here if found
        [SerializeField] private List<ShaderTexturePropertyMap> shaderMappings = new List<ShaderTexturePropertyMap>();
        [SerializeField] private bool createTextureBackups = true;
        [SerializeField] private UniversalShaderConverter.RenderPipeline defaultGenerationPipeline = UniversalShaderConverter.RenderPipeline.BuiltIn;
        [SerializeField] private UniversalShaderConverter.TextureGenerationSettings defaultGenerationSettings = UniversalShaderConverter.TextureGenerationSettings.CreateDefault();
        [SerializeField] private bool generationDefaultsInitialized = false;

        internal IReadOnlyList<ShaderTexturePropertyMap> ShaderMappings => shaderMappings;
        internal bool CreateTextureBackups => createTextureBackups;
        internal UniversalShaderConverter.RenderPipeline DefaultGenerationPipeline => defaultGenerationPipeline;
        internal UniversalShaderConverter.TextureGenerationSettings DefaultGenerationSettings => defaultGenerationSettings;
        internal static bool CreateTextureBackupsEnabled
        {
            get
            {
                var settings = GetOrCreateSettings();
                return settings == null || settings.createTextureBackups;
            }
        }
        internal static UniversalShaderConverter.RenderPipeline GetDefaultGenerationPipeline()
        {
            var settings = GetOrCreateSettings();
            return settings != null ? settings.defaultGenerationPipeline : UniversalShaderConverter.RenderPipeline.BuiltIn;
        }

        internal static UniversalShaderConverter.TextureGenerationSettings GetDefaultGenerationSettings()
        {
            var settings = GetOrCreateSettings();
            return settings != null ? settings.defaultGenerationSettings : UniversalShaderConverter.TextureGenerationSettings.CreateDefault();
        }
        internal ShaderTexturePropertyMap GetMapping(int index)
        {
            if (index < 0 || shaderMappings == null || index >= shaderMappings.Count)
                return null;
            return shaderMappings[index];
        }

        private static TextureStudioProjectSettings _cached;

        internal static TextureStudioProjectSettings GetOrCreateSettings()
        {
            if (_cached != null)
                return _cached;

            var settings = AssetDatabase.LoadAssetAtPath<TextureStudioProjectSettings>(SettingsPath);

            // If not found, try to migrate legacy location by copying/moving.
            if (settings == null)
            {
                var legacy = AssetDatabase.LoadAssetAtPath<TextureStudioProjectSettings>(LegacySettingsPath);
                if (legacy != null)
                {
                    EnsureFolder(SettingsPath);
                    string moveResult = AssetDatabase.MoveAsset(LegacySettingsPath, SettingsPath);
                    if (!string.IsNullOrEmpty(moveResult))
                    {
                        // Move failed, try copy instead
                        AssetDatabase.CopyAsset(LegacySettingsPath, SettingsPath);
                    }
                    settings = AssetDatabase.LoadAssetAtPath<TextureStudioProjectSettings>(SettingsPath);
                }
            }

            if (settings == null)
            {
                EnsureFolder(SettingsPath);
                settings = CreateInstance<TextureStudioProjectSettings>();
                settings.SetDefaults();
                try
                {
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                    AssetDatabase.SaveAssets();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Texture Studio] Failed to create settings at '{SettingsPath}'. {ex.Message}");
                    settings = null;
                }
            }

            // Fallback to in-memory settings so UI keeps working even if asset creation fails.
            if (settings == null)
            {
                settings = CreateInstance<TextureStudioProjectSettings>();
                Debug.LogWarning("[Texture Studio] Using in-memory settings; asset could not be created. Please ensure 'Assets' is writable and try again.");
            }

            if (settings != null)
            {
                settings.EnsureGenerationDefaults();
            }

            _cached = settings;
            return _cached;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        internal IEnumerable<string> GetPropertyNames(Shader shader, TextureSlot slot)
        {
            if (shader == null || shaderMappings == null)
                yield break;

            string shaderName = shader.name ?? string.Empty;
            foreach (var mapping in shaderMappings)
            {
                if (mapping == null)
                    continue;

                if (string.IsNullOrWhiteSpace(mapping.shaderNameContains))
                    continue;

                if (shaderName.IndexOf(mapping.shaderNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                foreach (var name in GetSlotProperties(mapping, slot))
                {
                    if (!string.IsNullOrWhiteSpace(name))
                        yield return name;
                }

                // first matching mapping wins
                yield break;
            }
        }

        private static IEnumerable<string> GetSlotProperties(ShaderTexturePropertyMap mapping, TextureSlot slot)
        {
            switch (slot)
            {
                case TextureSlot.Base:
                    return mapping.baseMapProperties ?? Array.Empty<string>();
                case TextureSlot.Normal:
                    return mapping.normalMapProperties ?? Array.Empty<string>();
                case TextureSlot.Height:
                    return mapping.heightMapProperties ?? Array.Empty<string>();
                case TextureSlot.MetallicMask:
                    return mapping.metallicMaskProperties ?? Array.Empty<string>();
                case TextureSlot.Occlusion:
                    return mapping.occlusionMapProperties ?? Array.Empty<string>();
                case TextureSlot.Emission:
                    return mapping.emissionMapProperties ?? Array.Empty<string>();
                case TextureSlot.BentNormal:
                    return mapping.bentNormalMapProperties ?? Array.Empty<string>();
                case TextureSlot.Detail:
                    return mapping.detailMapProperties ?? Array.Empty<string>();
                case TextureSlot.CoatMask:
                    return mapping.coatMaskMapProperties ?? Array.Empty<string>();
                default:
                    return Array.Empty<string>();
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
                return;

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
        }

        private void SetDefaults()
        {
            createTextureBackups = true;
            defaultGenerationSettings = UniversalShaderConverter.TextureGenerationSettings.CreateDefault();
            defaultGenerationPipeline = DetectActivePipeline();
            generationDefaultsInitialized = true;
        }

        private void EnsureGenerationDefaults()
        {
            if (generationDefaultsInitialized)
                return;

            defaultGenerationSettings = UniversalShaderConverter.TextureGenerationSettings.CreateDefault();
            defaultGenerationPipeline = DetectActivePipeline();
            generationDefaultsInitialized = true;
            EditorUtility.SetDirty(this);
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

        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/Texture Studio", SettingsScope.Project)
            {
                label = "Texture Studio",
                guiHandler = _ =>
                {
                    var settings = GetSerializedSettings();
                    settings.Update();

                    DrawHeader();
                    EditorGUILayout.Space(6);

                    DrawQuickStart();
                    EditorGUILayout.Space(10);

                    DrawOutputSettings(settings);
                    EditorGUILayout.Space(10);

                    DrawDefaultGenerationSettings(settings);
                    EditorGUILayout.Space(10);

                    DrawPresets(settings);
                    EditorGUILayout.Space(10);

                    DrawMappings(settings);

                    settings.ApplyModifiedProperties();
                },
                keywords = new HashSet<string>(new[] { "Texture", "Studio", "Material", "Converter", "Mapping", "Backup", "PBR", "Generation" })
            };
            return provider;
        }

        private static void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
                EditorGUILayout.LabelField("Auto-Assignment Mappings", titleStyle);
                EditorGUILayout.LabelField(
                    "Tell Texture Studio how to match your custom shaders and which texture property names to use for generated maps.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawQuickStart()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1) Add a mapping → enter a shader name substring (case-insensitive).", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("2) Fill the property names your shader uses (comma-separated).", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("3) Generate textures → assignments will use these names first.", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static void DrawOutputSettings(SerializedObject settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                var backupProp = settings.FindProperty("createTextureBackups");
                if (backupProp != null)
                {
                    var label = new GUIContent(
                        "Preserve Existing Textures (Backups)",
                        "When enabled, Texture Studio will avoid overwriting existing output files by creating a new copy with a suffix.");
                    backupProp.boolValue = EditorGUILayout.Toggle(label, backupProp.boolValue);
                    string note = backupProp.boolValue
                        ? "Existing output textures are kept; new files get an incremented suffix."
                        : "Existing output textures are overwritten when generating/adjusting.";
                    EditorGUILayout.LabelField(note, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private static void DrawDefaultGenerationSettings(SerializedObject settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Default Map Generation", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Used by the Create PBR Maps context action.", EditorStyles.wordWrappedMiniLabel);

                var pipelineProp = settings.FindProperty("defaultGenerationPipeline");
                var generationProp = settings.FindProperty("defaultGenerationSettings");

                if (pipelineProp == null || generationProp == null)
                {
                    EditorGUILayout.HelpBox("Generation defaults are unavailable. Reimport Texture Studio scripts.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Target Pipeline");
                    string[] labels = { "Built-in", "URP", "HDRP" };
                    int currentIndex = Mathf.Clamp(pipelineProp.enumValueIndex, 0, 2);
                    int nextIndex = EditorGUILayout.Popup(currentIndex, labels);
                    pipelineProp.enumValueIndex = nextIndex;
                    if (GUILayout.Button("Set to Active", EditorStyles.miniButton, GUILayout.Width(96)))
                    {
                        pipelineProp.enumValueIndex = (int)DetectActivePipeline();
                    }
                }

                var pipeline = (UniversalShaderConverter.RenderPipeline)pipelineProp.enumValueIndex;

                EditorGUILayout.Space(4);
                var normalProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateNormalMap));
                var normalStrengthProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.NormalMapStrength));
                var heightProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateHeightMap));
                var heightScaleProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.HeightScale));
                var occlusionProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateOcclusionMap));
                var occlusionStrengthProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.OcclusionStrength));
                var metallicProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateMetallicMap));
                var metallicValueProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.MetallicValue));
                var smoothnessProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.SmoothnessValue));
                var emissionProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateEmissionMap));
                var emissionColorProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionColor));
                var emissionAlgorithmProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionAlgorithm));
                var emissionCoverageProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionCoverage));
                var emissionMaskOnlyProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionMaskOnly));
                var emissionBinaryProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionBinaryMask));
                var emissionFeatherProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.EmissionFeather));
                var bentNormalProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateBentNormalMap));
                var bentNormalStrengthProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.BentNormalStrength));
                var detailProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateDetailMap));
                var detailStrengthProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.DetailStrength));
                var coatProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.GenerateCoatMask));
                var coatCoverageProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.CoatCoverage));
                var coatFeatherProp = generationProp.FindPropertyRelative(nameof(UniversalShaderConverter.TextureGenerationSettings.CoatFeather));

                if (normalProp != null)
                {
                    normalProp.boolValue = EditorGUILayout.ToggleLeft("Normal Map", normalProp.boolValue);
                    if (normalProp.boolValue && normalStrengthProp != null)
                    {
                        normalStrengthProp.intValue = EditorGUILayout.IntSlider("Strength", normalStrengthProp.intValue, 1, 10);
                    }
                }

                if (heightProp != null)
                {
                    heightProp.boolValue = EditorGUILayout.ToggleLeft("Height/Parallax Map", heightProp.boolValue);
                    if (heightProp.boolValue && heightScaleProp != null)
                    {
                        heightScaleProp.floatValue = EditorGUILayout.Slider("Height Scale", heightScaleProp.floatValue, 0.01f, 0.2f);
                    }
                }

                if (occlusionProp != null)
                {
                    occlusionProp.boolValue = EditorGUILayout.ToggleLeft("Occlusion Map", occlusionProp.boolValue);
                    if (occlusionProp.boolValue && occlusionStrengthProp != null)
                    {
                        occlusionStrengthProp.floatValue = EditorGUILayout.Slider("AO Strength", occlusionStrengthProp.floatValue, 0f, 1f);
                    }
                }

                if (metallicProp != null)
                {
                    string metallicLabel = pipeline == UniversalShaderConverter.RenderPipeline.HDRP ? "Mask Map (Metallic/AO/Smoothness)" : "Metallic/Smoothness Map";
                    metallicProp.boolValue = EditorGUILayout.ToggleLeft(metallicLabel, metallicProp.boolValue);
                    if (metallicProp.boolValue && metallicValueProp != null && smoothnessProp != null)
                    {
                        metallicValueProp.floatValue = EditorGUILayout.Slider("Metallic", metallicValueProp.floatValue, 0f, 1f);
                        smoothnessProp.floatValue = EditorGUILayout.Slider("Smoothness", smoothnessProp.floatValue, 0f, 1f);
                    }
                }

                if (pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
                {
                    if (bentNormalProp != null)
                    {
                        bentNormalProp.boolValue = EditorGUILayout.ToggleLeft("Bent Normal Map (HDRP)", bentNormalProp.boolValue);
                        if (bentNormalProp.boolValue && bentNormalStrengthProp != null)
                        {
                            bentNormalStrengthProp.intValue = EditorGUILayout.IntSlider("Bent Normal Strength", bentNormalStrengthProp.intValue, 1, 10);
                        }
                    }

                    if (detailProp != null)
                    {
                        detailProp.boolValue = EditorGUILayout.ToggleLeft("Detail Map (HDRP)", detailProp.boolValue);
                        if (detailProp.boolValue && detailStrengthProp != null)
                        {
                            detailStrengthProp.floatValue = EditorGUILayout.Slider("Detail Strength", detailStrengthProp.floatValue, 0.2f, 2f);
                        }
                    }

                    if (coatProp != null)
                    {
                        coatProp.boolValue = EditorGUILayout.ToggleLeft("Coat Mask (HDRP)", coatProp.boolValue);
                        if (coatProp.boolValue && coatCoverageProp != null && coatFeatherProp != null)
                        {
                            coatCoverageProp.floatValue = EditorGUILayout.Slider("Coat Coverage", coatCoverageProp.floatValue, 0.05f, 0.6f);
                            coatFeatherProp.intValue = EditorGUILayout.IntSlider("Edge Feather (px)", coatFeatherProp.intValue, 0, 8);
                        }
                    }
                }

                if (emissionProp != null)
                {
                    emissionProp.boolValue = EditorGUILayout.ToggleLeft("Emission Map", emissionProp.boolValue);
                    if (emissionProp.boolValue)
                    {
                        if (emissionColorProp != null)
                            emissionColorProp.colorValue = EditorGUILayout.ColorField("Emission Color", emissionColorProp.colorValue);
                        if (emissionAlgorithmProp != null)
                            emissionAlgorithmProp.enumValueIndex = (int)(UniversalShaderConverter.EmissionAlgorithm)EditorGUILayout.EnumPopup("Algorithm", (UniversalShaderConverter.EmissionAlgorithm)emissionAlgorithmProp.enumValueIndex);
                        if (emissionCoverageProp != null)
                            emissionCoverageProp.floatValue = EditorGUILayout.Slider("Emission Coverage", emissionCoverageProp.floatValue, 0.02f, 0.5f);
                        if (emissionMaskOnlyProp != null)
                            emissionMaskOnlyProp.boolValue = EditorGUILayout.Toggle("Mask Only (Grayscale)", emissionMaskOnlyProp.boolValue);
                        if (emissionBinaryProp != null)
                            emissionBinaryProp.boolValue = EditorGUILayout.Toggle("Binary Mask (Black/White)", emissionBinaryProp.boolValue);
                        if (emissionFeatherProp != null)
                            emissionFeatherProp.intValue = EditorGUILayout.IntSlider("Edge Feather (px)", emissionFeatherProp.intValue, 0, 8);
                    }
                }
            }
        }

        private static void DrawPresets(SerializedObject settings)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Drop in common mappings as a starting point.", EditorStyles.wordWrappedMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add URP Lit Preset"))
                    {
                        AddPreset(settings, "Universal Render Pipeline/Lit", new[] { "_BaseMap" }, new[] { "_BumpMap" }, null,
                            new[] { "_MetallicGlossMap" }, new[] { "_OcclusionMap" }, new[] { "_EmissionMap" }, null, null, null);
                    }
                    if (GUILayout.Button("Add HDRP Lit Preset"))
                    {
                        AddPreset(settings, "HDRP/Lit", new[] { "_BaseColorMap" }, new[] { "_NormalMap" }, null,
                            new[] { "_MaskMap" }, new[] { "_MaskMap" }, new[] { "_EmissiveColorMap" }, new[] { "_BentNormalMap" }, new[] { "_DetailMap" }, new[] { "_CoatMaskMap" });
                    }
                }
            }
        }

        private static void AddPreset(SerializedObject settings, string shaderSubstring,
            string[] baseNames, string[] normalNames, string[] heightNames, string[] metallicNames, string[] occlusionNames,
            string[] emissionNames, string[] bentNames, string[] detailNames, string[] coatNames)
        {
            var list = settings.FindProperty("shaderMappings");
            list.arraySize++;
            var element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("shaderNameContains").stringValue = shaderSubstring;
            SetArray(element.FindPropertyRelative("baseMapProperties"), baseNames);
            SetArray(element.FindPropertyRelative("normalMapProperties"), normalNames);
            SetArray(element.FindPropertyRelative("heightMapProperties"), heightNames);
            SetArray(element.FindPropertyRelative("metallicMaskProperties"), metallicNames);
            SetArray(element.FindPropertyRelative("occlusionMapProperties"), occlusionNames);
            SetArray(element.FindPropertyRelative("emissionMapProperties"), emissionNames);
            SetArray(element.FindPropertyRelative("bentNormalMapProperties"), bentNames);
            SetArray(element.FindPropertyRelative("detailMapProperties"), detailNames);
            SetArray(element.FindPropertyRelative("coatMaskMapProperties"), coatNames);
            settings.ApplyModifiedProperties();
        }

        private static void SetArray(SerializedProperty arrayProp, string[] values)
        {
            if (arrayProp == null || values == null)
                return;
            arrayProp.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }

        private static void DrawMappings(SerializedObject settings)
        {
            var list = settings.FindProperty("shaderMappings");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Shader Mappings", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add Mapping", GUILayout.Width(110)))
                    {
                        list.arraySize++;
                        var el = list.GetArrayElementAtIndex(list.arraySize - 1);
                        el.FindPropertyRelative("shaderNameContains").stringValue = string.Empty;
                        SetArray(el.FindPropertyRelative("baseMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("normalMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("heightMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("metallicMaskProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("occlusionMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("emissionMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("bentNormalMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("detailMapProperties"), Array.Empty<string>());
                        SetArray(el.FindPropertyRelative("coatMaskMapProperties"), Array.Empty<string>());
                    }
                }

                EditorGUILayout.Space(6);

                for (int i = 0; i < list.arraySize; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    DrawMappingCard(list, element, i);
                    EditorGUILayout.Space(6);
                }

                if (list.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No mappings yet. Add one to define custom shader names and property aliases.", MessageType.Info);
                }
            }
        }

        private static void DrawMappingCard(SerializedProperty list, SerializedProperty element, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Mapping {index + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(70)))
                    {
                        list.DeleteArrayElementAtIndex(index);
                        return;
                    }
                }

                EditorGUILayout.Space(2);
                var shaderProp = element.FindPropertyRelative("shaderNameContains");
                shaderProp.stringValue = EditorGUILayout.TextField(new GUIContent("Shader name contains",
                    "Substring to match shader names (case-insensitive). First match wins. Examples: 'MyToon', 'Stylized', 'Custom/Lit'."), shaderProp.stringValue);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Property names (comma-separated)", EditorStyles.miniBoldLabel);
                DrawCsvField(element, "baseMapProperties", "Base / Albedo", "_BaseMap, _MainTex, _BaseColorMap, _AlbedoMap");
                DrawCsvField(element, "normalMapProperties", "Normal Map", "_BumpMap, _NormalMap");
                DrawCsvField(element, "heightMapProperties", "Height / Parallax", "_ParallaxMap, _HeightMap");
                DrawCsvField(element, "metallicMaskProperties", "Metallic / Mask", "_MetallicGlossMap, _MaskMap");
                DrawCsvField(element, "occlusionMapProperties", "Occlusion", "_OcclusionMap");
                DrawCsvField(element, "emissionMapProperties", "Emission", "_EmissionMap, _EmissiveColorMap");
                DrawCsvField(element, "bentNormalMapProperties", "Bent Normal (HDRP)", "_BentNormalMap");
                DrawCsvField(element, "detailMapProperties", "Detail (HDRP)", "_DetailMap");
                DrawCsvField(element, "coatMaskMapProperties", "Coat Mask (HDRP)", "_CoatMaskMap");
            }
        }

        private static void DrawCsvField(SerializedProperty element, string relativeName, string label, string placeholder)
        {
            var prop = element.FindPropertyRelative(relativeName);
            string current = PropertyArrayToCsv(prop);
            EditorGUI.BeginChangeCheck();
            string next = EditorGUILayout.TextField(new GUIContent(label, placeholder), current);
            if (EditorGUI.EndChangeCheck())
            {
                CsvToPropertyArray(prop, next);
            }
        }

        private static string PropertyArrayToCsv(SerializedProperty arrayProp)
        {
            if (arrayProp == null || !arrayProp.isArray)
                return string.Empty;
            List<string> values = new List<string>();
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                values.Add(arrayProp.GetArrayElementAtIndex(i).stringValue);
            }
            return string.Join(", ", values.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private static void CsvToPropertyArray(SerializedProperty arrayProp, string csv)
        {
            if (arrayProp == null)
                return;
            var parts = (csv ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            arrayProp.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).stringValue = parts[i];
            }
        }
    }

    [CustomPropertyDrawer(typeof(ShaderTexturePropertyMap))]
    internal class ShaderTexturePropertyMapDrawer : PropertyDrawer
    {
        private static readonly GUIContent ShaderNameLabel = new GUIContent(
            "Shader Name Contains",
            "Substring match against the shader name (case-insensitive). First match wins. Example: 'MyCustom/Toon' or 'Lit'.");

        private static readonly GUIContent BaseLabel = new GUIContent(
            "Base Map Properties",
            "Albedo/base map property names. Examples: _BaseMap, _MainTex, _BaseColorMap, _AlbedoMap.");

        private static readonly GUIContent NormalLabel = new GUIContent(
            "Normal Map Properties",
            "Normal map property names. Examples: _BumpMap, _NormalMap.");

        private static readonly GUIContent HeightLabel = new GUIContent(
            "Height Map Properties",
            "Height/parallax property names. Examples: _ParallaxMap, _HeightMap.");

        private static readonly GUIContent MetallicLabel = new GUIContent(
            "Metallic/Mask Properties",
            "Metallic or mask property names. Examples: _MetallicGlossMap (Built-in/URP), _MaskMap (HDRP). Add custom if your shader differs.");

        private static readonly GUIContent OcclusionLabel = new GUIContent(
            "Occlusion Map Properties",
            "Occlusion/AO property names. Example: _OcclusionMap.");

        private static readonly GUIContent EmissionLabel = new GUIContent(
            "Emission Map Properties",
            "Emission property names. Examples: _EmissionMap, _EmissiveColorMap.");

        private static readonly GUIContent BentNormalLabel = new GUIContent(
            "Bent Normal Map Properties",
            "HDRP-only bent normal property names. Example: _BentNormalMap.");

        private static readonly GUIContent DetailLabel = new GUIContent(
            "Detail Map Properties",
            "HDRP detail map property names. Example: _DetailMap.");

        private static readonly GUIContent CoatLabel = new GUIContent(
            "Coat Mask Map Properties",
            "HDRP coat mask property names. Example: _CoatMaskMap.");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Sum heights of each child plus spacing
            float height = 0f;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // shader name

            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("baseMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("normalMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("heightMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("metallicMaskProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("occlusionMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("emissionMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("bentNormalMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("detailMapProperties"), true) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("coatMaskMapProperties"), true);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            float line = EditorGUIUtility.singleLineHeight;
            float svs = EditorGUIUtility.standardVerticalSpacing;
            float fullWidth = position.width;

            // Shader name substring
            var shaderProp = property.FindPropertyRelative("shaderNameContains");
            EditorGUI.PropertyField(new Rect(position.x, y, fullWidth, line), shaderProp, ShaderNameLabel);
            y += line + svs;

            // Arrays
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("baseMapProperties"), BaseLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("normalMapProperties"), NormalLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("heightMapProperties"), HeightLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("metallicMaskProperties"), MetallicLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("occlusionMapProperties"), OcclusionLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("emissionMapProperties"), EmissionLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("bentNormalMapProperties"), BentNormalLabel);
            y = DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("detailMapProperties"), DetailLabel);
            DrawArray(position, ref y, fullWidth, property.FindPropertyRelative("coatMaskMapProperties"), CoatLabel);

            EditorGUI.EndProperty();
        }

        private static float DrawArray(Rect position, ref float y, float width, SerializedProperty arrayProp, GUIContent label)
        {
            float h = EditorGUI.GetPropertyHeight(arrayProp, true);
            EditorGUI.PropertyField(new Rect(position.x, y, width, h), arrayProp, label, true);
            y += h + EditorGUIUtility.standardVerticalSpacing;
            return y;
        }
    }
}
