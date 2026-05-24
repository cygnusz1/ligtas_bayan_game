using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arawn.TextureStudio.Editor
{
    /// <summary>
    /// Universal shader converter for Unity materials.
    /// Converts between Built-in, URP, and HDRP render pipelines.
    /// Handles texture property mapping where possible, skips incompatible properties.
    /// </summary>
    public class UniversalShaderConverter : EditorWindow
    {
        #region Shader Definitions

        private static class ShaderPaths
        {
            // Built-in RP
            public const string BuiltIn_Standard = "Standard";
            public const string BuiltIn_StandardSpecular = "Standard (Specular setup)";
            public const string BuiltIn_Unlit = "Unlit/Texture";
            public const string BuiltIn_UnlitColor = "Unlit/Color";
            public const string BuiltIn_UnlitTransparent = "Unlit/Transparent";
            
            // URP
            public const string URP_Lit = "Universal Render Pipeline/Lit";
            public const string URP_SimpleLit = "Universal Render Pipeline/Simple Lit";
            public const string URP_Unlit = "Universal Render Pipeline/Unlit";
            public const string URP_BakedLit = "Universal Render Pipeline/Baked Lit";
            public const string URP_Terrain = "Universal Render Pipeline/Terrain/Lit";
            
            // HDRP
            public const string HDRP_Lit = "HDRP/Lit";
            public const string HDRP_LitTessellation = "HDRP/LitTessellation";
            public const string HDRP_Unlit = "HDRP/Unlit";
            public const string HDRP_TerrainLit = "HDRP/TerrainLit";
        }

        /// <summary>
        /// Property mapping between different render pipelines.
        /// Maps source property name to target property name.
        /// </summary>
        private static class PropertyMaps
        {
            // Built-in to URP
            public static readonly Dictionary<string, string> BuiltInToURP = new Dictionary<string, string>
            {
                // Textures
                { "_MainTex", "_BaseMap" },
                { "_BumpMap", "_BumpMap" },
                { "_EmissionMap", "_EmissionMap" },
                { "_MetallicGlossMap", "_MetallicGlossMap" },
                { "_OcclusionMap", "_OcclusionMap" },
                { "_ParallaxMap", "_ParallaxMap" },
                { "_DetailAlbedoMap", "_DetailAlbedoMap" },
                { "_DetailNormalMap", "_DetailNormalMap" },
                
                // Colors
                { "_Color", "_BaseColor" },
                { "_EmissionColor", "_EmissionColor" },
                
                // Properties
                { "_Glossiness", "_Smoothness" },
                { "_GlossMapScale", "_Smoothness" },
                { "_Metallic", "_Metallic" },
                { "_BumpScale", "_BumpScale" },
                { "_Parallax", "_Parallax" },
                { "_OcclusionStrength", "_OcclusionStrength" },
                { "_DetailNormalMapScale", "_DetailNormalMapScale" },
                
                // Modes
                { "_Mode", "_Surface" }, // 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
                { "_Cutoff", "_Cutoff" },
            };

            // URP to Built-in
            public static readonly Dictionary<string, string> URPToBuiltIn = new Dictionary<string, string>
            {
                { "_BaseMap", "_MainTex" },
                { "_BaseColor", "_Color" },
                { "_BumpMap", "_BumpMap" },
                { "_EmissionMap", "_EmissionMap" },
                { "_EmissionColor", "_EmissionColor" },
                { "_MetallicGlossMap", "_MetallicGlossMap" },
                { "_Smoothness", "_Glossiness" },
                { "_Metallic", "_Metallic" },
                { "_BumpScale", "_BumpScale" },
                { "_OcclusionMap", "_OcclusionMap" },
                { "_OcclusionStrength", "_OcclusionStrength" },
                { "_Surface", "_Mode" },
                { "_Cutoff", "_Cutoff" },
            };

            // Built-in to HDRP
            public static readonly Dictionary<string, string> BuiltInToHDRP = new Dictionary<string, string>
            {
                // Textures
                { "_MainTex", "_BaseColorMap" },
                { "_BumpMap", "_NormalMap" },
                { "_EmissionMap", "_EmissiveColorMap" },
                { "_MetallicGlossMap", "_MaskMap" }, // R=Metallic, A=Smoothness
                { "_OcclusionMap", "_MaskMap" }, // G=AO
                { "_DetailAlbedoMap", "_DetailMap" },
                { "_DetailNormalMap", "_DetailNormalMap" },
                
                // Colors
                { "_Color", "_BaseColor" },
                { "_EmissionColor", "_EmissiveColor" },
                
                // Properties
                { "_Glossiness", "_Smoothness" },
                { "_Metallic", "_Metallic" },
                { "_BumpScale", "_NormalScale" },
                { "_OcclusionStrength", "_AORemapMax" },
                { "_DetailNormalMapScale", "_DetailNormalScale" },
                
                // Modes
                { "_Mode", "_SurfaceType" },
                { "_Cutoff", "_AlphaCutoff" },
            };

            // HDRP to Built-in
            public static readonly Dictionary<string, string> HDRPToBuiltIn = new Dictionary<string, string>
            {
                { "_BaseColorMap", "_MainTex" },
                { "_BaseColor", "_Color" },
                { "_NormalMap", "_BumpMap" },
                { "_NormalScale", "_BumpScale" },
                { "_EmissiveColorMap", "_EmissionMap" },
                { "_EmissiveColor", "_EmissionColor" },
                { "_MaskMap", "_MetallicGlossMap" },
                { "_Smoothness", "_Glossiness" },
                { "_Metallic", "_Metallic" },
                { "_SurfaceType", "_Mode" },
                { "_AlphaCutoff", "_Cutoff" },
            };

            // URP to HDRP
            public static readonly Dictionary<string, string> URPToHDRP = new Dictionary<string, string>
            {
                { "_BaseMap", "_BaseColorMap" },
                { "_BaseColor", "_BaseColor" },
                { "_BumpMap", "_NormalMap" },
                { "_BumpScale", "_NormalScale" },
                { "_EmissionMap", "_EmissiveColorMap" },
                { "_EmissionColor", "_EmissiveColor" },
                { "_MetallicGlossMap", "_MaskMap" },
                { "_Smoothness", "_Smoothness" },
                { "_Metallic", "_Metallic" },
                { "_OcclusionMap", "_MaskMap" },
                { "_Surface", "_SurfaceType" },
                { "_Cutoff", "_AlphaCutoff" },
            };

            // HDRP to URP
            public static readonly Dictionary<string, string> HDRPToURP = new Dictionary<string, string>
            {
                { "_BaseColorMap", "_BaseMap" },
                { "_BaseColor", "_BaseColor" },
                { "_NormalMap", "_BumpMap" },
                { "_NormalScale", "_BumpScale" },
                { "_EmissiveColorMap", "_EmissionMap" },
                { "_EmissiveColor", "_EmissionColor" },
                { "_MaskMap", "_MetallicGlossMap" },
                { "_Smoothness", "_Smoothness" },
                { "_Metallic", "_Metallic" },
                { "_SurfaceType", "_Surface" },
                { "_AlphaCutoff", "_Cutoff" },
            };
        }

        #endregion

        #region UI State

        internal enum RenderPipeline
        {
            BuiltIn,
            URP,
            HDRP,
            Custom
        }

        private enum ConversionMode
        {
            SelectedMaterials,
            MaterialsInFolder,
            AllProjectMaterials
        }

        public enum EmissionAlgorithm
        {
            HighChroma,
            BackgroundDeviation
        }

        [SerializeField] private RenderPipeline _sourceRP = RenderPipeline.BuiltIn;
        [SerializeField] private RenderPipeline _targetRP = RenderPipeline.URP;
        [SerializeField] private bool _autoDetectSourceRP = true;
        [SerializeField] private int _sourceCustomMappingIndex = -1;
        [SerializeField] private int _targetCustomMappingIndex = -1;
        [SerializeField] private ConversionMode _conversionMode = ConversionMode.SelectedMaterials;
        
        [SerializeField] private DefaultAsset _targetFolder;
        [SerializeField] private List<Material> _selectedMaterials = new List<Material>();
        private Vector2 _scrollPosition;
        private Vector2 _materialsScrollPosition;
        
        [SerializeField] private bool _showAdvancedOptions = false;
        [SerializeField] private bool _createBackup = true;
        [SerializeField] private bool _preserveRenderQueue = true;
        [SerializeField] private bool _logConversionDetails = true;
        
        // Texture generation options
        [SerializeField] private bool _showTextureGenerationOptions = false;
        [SerializeField] private bool _autoGenerateMissingTextures = false;
        [SerializeField] private bool _generateNormalMap = true;
        [SerializeField] private bool _generateBentNormalMap = false; // HDRP only
        [SerializeField] private bool _generateMetallicMap = true;
        [SerializeField] private bool _generateHeightMap = false;
        [SerializeField] private bool _generateOcclusionMap = true;
        [SerializeField] private bool _generateEmissionMap = false;
        [SerializeField] private bool _generateCoatMask = false; // HDRP only
        
        // Texture generation settings
        [SerializeField] private int _normalMapStrength = 5;
        [SerializeField] private float _metallicValue = 0.0f;
        [SerializeField] private float _smoothnessValue = 0.5f;
        [SerializeField] private float _heightScale = 0.05f;
        [SerializeField] private float _occlusionStrength = 1.0f;
        [SerializeField] private Color _emissionColor = Color.black;
        [SerializeField] private float _emissionCoverage = 0.15f;
        [SerializeField] private bool _emissionMaskOnly = false;
        [SerializeField] private bool _emissionBinaryMask = false;
        [SerializeField] private EmissionAlgorithm _emissionAlgorithm = EmissionAlgorithm.BackgroundDeviation;
        [SerializeField] private int _emissionFeather = 2;
        [SerializeField] private int _bentNormalStrength = 5;
        [SerializeField] private bool _generateDetailMap = false; // HDRP only
        [SerializeField] private float _detailStrength = 1.0f;
        [SerializeField] private float _coatCoverage = 0.25f;
        [SerializeField] private int _coatFeather = 2;
        [SerializeField] private bool _preserveExistingCoat = true;

        [Serializable]
        internal struct TextureGenerationSettings
        {
            public bool GenerateNormalMap;
            public bool GenerateBentNormalMap;
            public bool GenerateMetallicMap;
            public bool GenerateHeightMap;
            public bool GenerateOcclusionMap;
            public bool GenerateEmissionMap;
            public int NormalMapStrength;
            public int BentNormalStrength;
            public float MetallicValue;
            public float SmoothnessValue;
            public float HeightScale;
            public float OcclusionStrength;
            public Color EmissionColor;
            public float EmissionCoverage;
            public bool EmissionMaskOnly;
            public bool EmissionBinaryMask;
            public EmissionAlgorithm EmissionAlgorithm;
            public int EmissionFeather;
            public bool GenerateDetailMap;
            public float DetailStrength;
            public bool GenerateCoatMask;
            public float CoatCoverage;
            public int CoatFeather;
            public bool PreserveExistingCoat;

            public static TextureGenerationSettings CreateDefault()
            {
                return new TextureGenerationSettings
                {
                    GenerateNormalMap = true,
                    GenerateBentNormalMap = false,
                    GenerateMetallicMap = true,
                    GenerateHeightMap = false,
                    GenerateOcclusionMap = true,
                    GenerateEmissionMap = false,
                    NormalMapStrength = 5,
                    BentNormalStrength = 5,
                    MetallicValue = 0.0f,
                    SmoothnessValue = 0.5f,
                    HeightScale = 0.05f,
                    OcclusionStrength = 1.0f,
                    EmissionColor = Color.black,
                    EmissionCoverage = 0.15f,
                    EmissionMaskOnly = false,
                    EmissionBinaryMask = false,
                    EmissionAlgorithm = EmissionAlgorithm.BackgroundDeviation,
                    EmissionFeather = 2,
                    GenerateDetailMap = false,
                    DetailStrength = 1.0f,
                    GenerateCoatMask = false,
                    CoatCoverage = 0.25f,
                    CoatFeather = 2,
                    PreserveExistingCoat = true
                };
            }
        }

        #endregion

        #region Window Management

        [MenuItem("Tools/Texture Studio/Material Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<UniversalShaderConverter>("Material Converter");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        #endregion

        #region GUI

        private struct PipelineOption
        {
            public RenderPipeline Pipeline;
            public int MappingIndex;
            public string Label;
        }

        private List<PipelineOption> GetPipelineOptions()
        {
            var options = new List<PipelineOption>
            {
                new PipelineOption { Pipeline = RenderPipeline.BuiltIn, MappingIndex = -1, Label = "Built-in" },
                new PipelineOption { Pipeline = RenderPipeline.URP, MappingIndex = -1, Label = "URP" },
                new PipelineOption { Pipeline = RenderPipeline.HDRP, MappingIndex = -1, Label = "HDRP" }
            };

            var mappings = ProjectSettings?.ShaderMappings;
            if (mappings != null)
            {
                for (int i = 0; i < mappings.Count; i++)
                {
                    var mapping = mappings[i];
                    string name = mapping != null ? mapping.shaderNameContains : string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                        name = $"Mapping {i + 1}";
                    options.Add(new PipelineOption
                    {
                        Pipeline = RenderPipeline.Custom,
                        MappingIndex = i,
                        Label = $"Custom: {name}"
                    });
                }
            }

            return options;
        }

        private int GetPipelineOptionIndex(List<PipelineOption> options, RenderPipeline pipeline, int mappingIndex)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Pipeline == pipeline && options[i].MappingIndex == mappingIndex)
                    return i;
            }
            // Fallback to first entry
            return 0;
        }

        private bool ArePipelinesEquivalent(RenderPipeline source, int sourceMappingIndex, RenderPipeline target, int targetMappingIndex)
        {
            if (source != target)
                return false;
            if (source != RenderPipeline.Custom)
                return true;
            return sourceMappingIndex == targetMappingIndex;
        }

        private const string PrefsPrefix = "TextureStudio_MaterialConverter_";
        private bool _settingsLoaded = false;

        private void OnEnable()
        {
            LoadSettings();
            RefreshSelectedMaterials();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            EditorPrefs.SetInt(PrefsPrefix + "sourceRP", (int)_sourceRP);
            EditorPrefs.SetInt(PrefsPrefix + "targetRP", (int)_targetRP);
            EditorPrefs.SetBool(PrefsPrefix + "autoDetectSourceRP", _autoDetectSourceRP);
            EditorPrefs.SetInt(PrefsPrefix + "sourceCustomMappingIndex", _sourceCustomMappingIndex);
            EditorPrefs.SetInt(PrefsPrefix + "targetCustomMappingIndex", _targetCustomMappingIndex);
            EditorPrefs.SetInt(PrefsPrefix + "conversionMode", (int)_conversionMode);
            EditorPrefs.SetBool(PrefsPrefix + "showAdvancedOptions", _showAdvancedOptions);
            EditorPrefs.SetBool(PrefsPrefix + "createBackup", _createBackup);
            EditorPrefs.SetBool(PrefsPrefix + "preserveRenderQueue", _preserveRenderQueue);
            EditorPrefs.SetBool(PrefsPrefix + "logConversionDetails", _logConversionDetails);
            EditorPrefs.SetBool(PrefsPrefix + "showTextureGenerationOptions", _showTextureGenerationOptions);
            EditorPrefs.SetBool(PrefsPrefix + "autoGenerateMissingTextures", _autoGenerateMissingTextures);
            EditorPrefs.SetBool(PrefsPrefix + "generateNormalMap", _generateNormalMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateBentNormalMap", _generateBentNormalMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateMetallicMap", _generateMetallicMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateHeightMap", _generateHeightMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateOcclusionMap", _generateOcclusionMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateEmissionMap", _generateEmissionMap);
            EditorPrefs.SetBool(PrefsPrefix + "generateCoatMask", _generateCoatMask);
            EditorPrefs.SetInt(PrefsPrefix + "normalMapStrength", _normalMapStrength);
            EditorPrefs.SetFloat(PrefsPrefix + "metallicValue", _metallicValue);
            EditorPrefs.SetFloat(PrefsPrefix + "smoothnessValue", _smoothnessValue);
            EditorPrefs.SetFloat(PrefsPrefix + "heightScale", _heightScale);
            EditorPrefs.SetFloat(PrefsPrefix + "occlusionStrength", _occlusionStrength);
            EditorPrefs.SetFloat(PrefsPrefix + "emissionColor_r", _emissionColor.r);
            EditorPrefs.SetFloat(PrefsPrefix + "emissionColor_g", _emissionColor.g);
            EditorPrefs.SetFloat(PrefsPrefix + "emissionColor_b", _emissionColor.b);
            EditorPrefs.SetFloat(PrefsPrefix + "emissionColor_a", _emissionColor.a);
            EditorPrefs.SetFloat(PrefsPrefix + "emissionCoverage", _emissionCoverage);
            EditorPrefs.SetBool(PrefsPrefix + "emissionMaskOnly", _emissionMaskOnly);
            EditorPrefs.SetBool(PrefsPrefix + "emissionBinaryMask", _emissionBinaryMask);
            EditorPrefs.SetInt(PrefsPrefix + "emissionAlgorithm", (int)_emissionAlgorithm);
            EditorPrefs.SetInt(PrefsPrefix + "emissionFeather", _emissionFeather);
            EditorPrefs.SetInt(PrefsPrefix + "bentNormalStrength", _bentNormalStrength);
            EditorPrefs.SetBool(PrefsPrefix + "generateDetailMap", _generateDetailMap);
            EditorPrefs.SetFloat(PrefsPrefix + "detailStrength", _detailStrength);
            EditorPrefs.SetFloat(PrefsPrefix + "coatCoverage", _coatCoverage);
            EditorPrefs.SetInt(PrefsPrefix + "coatFeather", _coatFeather);
            EditorPrefs.SetBool(PrefsPrefix + "preserveExistingCoat", _preserveExistingCoat);
        }

        private void LoadSettings()
        {
            if (!EditorPrefs.HasKey(PrefsPrefix + "targetRP"))
            {
                // First time: use auto-detected pipeline as default
                _targetRP = DetectActivePipeline();
                _targetCustomMappingIndex = -1;
                _settingsLoaded = true;
                return;
            }

            _sourceRP = (RenderPipeline)EditorPrefs.GetInt(PrefsPrefix + "sourceRP", (int)RenderPipeline.BuiltIn);
            _targetRP = (RenderPipeline)EditorPrefs.GetInt(PrefsPrefix + "targetRP", (int)RenderPipeline.URP);
            _autoDetectSourceRP = EditorPrefs.GetBool(PrefsPrefix + "autoDetectSourceRP", true);
            _sourceCustomMappingIndex = EditorPrefs.GetInt(PrefsPrefix + "sourceCustomMappingIndex", -1);
            _targetCustomMappingIndex = EditorPrefs.GetInt(PrefsPrefix + "targetCustomMappingIndex", -1);
            _conversionMode = (ConversionMode)EditorPrefs.GetInt(PrefsPrefix + "conversionMode", (int)ConversionMode.SelectedMaterials);
            _showAdvancedOptions = EditorPrefs.GetBool(PrefsPrefix + "showAdvancedOptions", false);
            _createBackup = EditorPrefs.GetBool(PrefsPrefix + "createBackup", true);
            _preserveRenderQueue = EditorPrefs.GetBool(PrefsPrefix + "preserveRenderQueue", true);
            _logConversionDetails = EditorPrefs.GetBool(PrefsPrefix + "logConversionDetails", true);
            _showTextureGenerationOptions = EditorPrefs.GetBool(PrefsPrefix + "showTextureGenerationOptions", false);
            _autoGenerateMissingTextures = EditorPrefs.GetBool(PrefsPrefix + "autoGenerateMissingTextures", false);
            _generateNormalMap = EditorPrefs.GetBool(PrefsPrefix + "generateNormalMap", true);
            _generateBentNormalMap = EditorPrefs.GetBool(PrefsPrefix + "generateBentNormalMap", false);
            _generateMetallicMap = EditorPrefs.GetBool(PrefsPrefix + "generateMetallicMap", true);
            _generateHeightMap = EditorPrefs.GetBool(PrefsPrefix + "generateHeightMap", false);
            _generateOcclusionMap = EditorPrefs.GetBool(PrefsPrefix + "generateOcclusionMap", true);
            _generateEmissionMap = EditorPrefs.GetBool(PrefsPrefix + "generateEmissionMap", false);
            _generateCoatMask = EditorPrefs.GetBool(PrefsPrefix + "generateCoatMask", false);
            _normalMapStrength = EditorPrefs.GetInt(PrefsPrefix + "normalMapStrength", 5);
            _metallicValue = EditorPrefs.GetFloat(PrefsPrefix + "metallicValue", 0.0f);
            _smoothnessValue = EditorPrefs.GetFloat(PrefsPrefix + "smoothnessValue", 0.5f);
            _heightScale = EditorPrefs.GetFloat(PrefsPrefix + "heightScale", 0.05f);
            _occlusionStrength = EditorPrefs.GetFloat(PrefsPrefix + "occlusionStrength", 1.0f);
            _emissionColor = new Color(
                EditorPrefs.GetFloat(PrefsPrefix + "emissionColor_r", 0f),
                EditorPrefs.GetFloat(PrefsPrefix + "emissionColor_g", 0f),
                EditorPrefs.GetFloat(PrefsPrefix + "emissionColor_b", 0f),
                EditorPrefs.GetFloat(PrefsPrefix + "emissionColor_a", 0f));
            _emissionCoverage = EditorPrefs.GetFloat(PrefsPrefix + "emissionCoverage", 0.15f);
            _emissionMaskOnly = EditorPrefs.GetBool(PrefsPrefix + "emissionMaskOnly", false);
            _emissionBinaryMask = EditorPrefs.GetBool(PrefsPrefix + "emissionBinaryMask", false);
            _emissionAlgorithm = (EmissionAlgorithm)EditorPrefs.GetInt(PrefsPrefix + "emissionAlgorithm", (int)EmissionAlgorithm.BackgroundDeviation);
            _emissionFeather = EditorPrefs.GetInt(PrefsPrefix + "emissionFeather", 2);
            _bentNormalStrength = EditorPrefs.GetInt(PrefsPrefix + "bentNormalStrength", 5);
            _generateDetailMap = EditorPrefs.GetBool(PrefsPrefix + "generateDetailMap", false);
            _detailStrength = EditorPrefs.GetFloat(PrefsPrefix + "detailStrength", 1.0f);
            _coatCoverage = EditorPrefs.GetFloat(PrefsPrefix + "coatCoverage", 0.25f);
            _coatFeather = EditorPrefs.GetInt(PrefsPrefix + "coatFeather", 2);
            _preserveExistingCoat = EditorPrefs.GetBool(PrefsPrefix + "preserveExistingCoat", true);
            _settingsLoaded = true;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space(10);
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawConversionSettings();
            EditorGUILayout.Space(10);
            
            DrawModeSelection();
            EditorGUILayout.Space(10);
            
            DrawAdvancedOptions();
            EditorGUILayout.Space(10);
            
            DrawTextureGenerationOptions();
            EditorGUILayout.Space(10);
            
            DrawConversionButton();
            EditorGUILayout.Space(10);
            
            DrawHelp();

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Universal Material Converter", titleStyle);
            EditorGUILayout.LabelField("Built-in ⇄ URP ⇄ HDRP", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawConversionSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Conversion Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source Pipeline:", GUILayout.Width(120));
            var pipelineOptions = GetPipelineOptions();
            string[] optionLabels = pipelineOptions.Select(o => o.Label).ToArray();
            EditorGUI.BeginDisabledGroup(_autoDetectSourceRP);
            int currentSourceIndex = GetPipelineOptionIndex(pipelineOptions, _sourceRP, _sourceCustomMappingIndex);
            int selectedSourceIndex = EditorGUILayout.Popup(currentSourceIndex, optionLabels);
            if (selectedSourceIndex != currentSourceIndex)
            {
                _sourceRP = pipelineOptions[selectedSourceIndex].Pipeline;
                _sourceCustomMappingIndex = pipelineOptions[selectedSourceIndex].MappingIndex;
            }
            EditorGUI.EndDisabledGroup();
            bool newAutoDetect = EditorGUILayout.ToggleLeft(new GUIContent("Auto-Detect", "Disable this to manually select Source Pipeline if using custom shaders."), _autoDetectSourceRP, GUILayout.Width(100));
            if (newAutoDetect != _autoDetectSourceRP)
            {
                _autoDetectSourceRP = newAutoDetect;
                if (_autoDetectSourceRP && _selectedMaterials.Count > 0)
                {
                    DetectSourcePipelineFromMaterials(_selectedMaterials);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(120);
            GUIStyle arrowStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("↓", arrowStyle, GUILayout.Height(25));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Pipeline:", GUILayout.Width(120));
            int currentTargetIndex = GetPipelineOptionIndex(pipelineOptions, _targetRP, _targetCustomMappingIndex);
            int selectedTargetIndex = EditorGUILayout.Popup(currentTargetIndex, optionLabels);
            if (selectedTargetIndex != currentTargetIndex)
            {
                _targetRP = pipelineOptions[selectedTargetIndex].Pipeline;
                _targetCustomMappingIndex = pipelineOptions[selectedTargetIndex].MappingIndex;
            }
            if (GUILayout.Button("Set to Active", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                _targetRP = DetectActivePipeline();
                _targetCustomMappingIndex = -1;
            }
            EditorGUILayout.EndHorizontal();
            
            if (ArePipelinesEquivalent(_sourceRP, _sourceCustomMappingIndex, _targetRP, _targetCustomMappingIndex))
            {
                EditorGUILayout.HelpBox("Source and Target pipelines are the same. No conversion needed.", MessageType.Warning);
            }

            if (!IsPipelineInstalled(_targetRP))
            {
                EditorGUILayout.HelpBox($"The {_targetRP} pipeline does not appear to be installed in this project. Necessary shaders are missing.", MessageType.Error);
            }
            
            EditorGUILayout.EndVertical();
        }

        private RenderPipeline DetectActivePipeline()
        {
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
            {
                string typeName = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().Name;
                if (typeName.Contains("HDRenderPipelineAsset") || typeName.Contains("HDRP"))
                    return RenderPipeline.HDRP;
                if (typeName.Contains("UniversalRenderPipelineAsset") || typeName.Contains("URP"))
                    return RenderPipeline.URP;
            }
            return RenderPipeline.BuiltIn;
        }

        private bool IsPipelineInstalled(RenderPipeline rp)
        {
            switch (rp)
            {
                case RenderPipeline.BuiltIn:
                    return Shader.Find(ShaderPaths.BuiltIn_Standard) != null;
                case RenderPipeline.URP:
                    return Shader.Find(ShaderPaths.URP_Lit) != null;
                case RenderPipeline.HDRP:
                    return Shader.Find(ShaderPaths.HDRP_Lit) != null;
                case RenderPipeline.Custom:
                    return FindShaderForMapping(_targetCustomMappingIndex) != null;
                default:
                    return false;
            }
        }

        private void DrawModeSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Conversion Mode", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            _conversionMode = (ConversionMode)EditorGUILayout.EnumPopup("Mode:", _conversionMode);
            
            EditorGUILayout.Space(5);
            
            switch (_conversionMode)
            {
                case ConversionMode.SelectedMaterials:
                    DrawSelectedMaterialsMode();
                    break;
                    
                case ConversionMode.MaterialsInFolder:
                    DrawFolderMode();
                    break;
                    
                case ConversionMode.AllProjectMaterials:
                    DrawAllMaterialsMode();
                    break;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedMaterialsMode()
        {
            EditorGUILayout.HelpBox("Convert currently selected materials in the Project window.", MessageType.Info);
            
            if (GUILayout.Button("Refresh Selection"))
            {
                RefreshSelectedMaterials();
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Selected Materials: {_selectedMaterials.Count}", EditorStyles.boldLabel);
            
            if (_selectedMaterials.Count > 0)
            {
                _materialsScrollPosition = EditorGUILayout.BeginScrollView(_materialsScrollPosition, GUILayout.MaxHeight(150));
                foreach (var mat in _selectedMaterials)
                {
                    if (mat != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(mat, typeof(Material), false);
                        EditorGUILayout.LabelField(mat.shader.name, EditorStyles.miniLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No materials selected. Select materials in Project window.", MessageType.Warning);
            }
        }

        private void DrawFolderMode()
        {
            EditorGUILayout.HelpBox("Convert all materials in a specific folder (including subfolders).", MessageType.Info);
            
            _targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Target Folder:",
                _targetFolder,
                typeof(DefaultAsset),
                false
            );
            
            if (_targetFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
                if (System.IO.Directory.Exists(folderPath))
                {
                    string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
                    EditorGUILayout.LabelField($"Materials found: {materialGuids.Length}", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox("Selected asset is not a folder.", MessageType.Error);
                }
            }
        }

        private void DrawAllMaterialsMode()
        {
            EditorGUILayout.HelpBox("Convert ALL materials in the entire project. Use with caution!", MessageType.Warning);
            
            string[] allMaterialGuids = AssetDatabase.FindAssets("t:Material");
            EditorGUILayout.LabelField($"Total Project Materials: {allMaterialGuids.Length}", EditorStyles.boldLabel);
        }

        private void DrawAdvancedOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true);
            
            if (_showAdvancedOptions)
            {
                EditorGUILayout.Space(5);
                _createBackup = EditorGUILayout.Toggle("Create Backup", _createBackup);
                _preserveRenderQueue = EditorGUILayout.Toggle("Preserve Render Queue", _preserveRenderQueue);
                _logConversionDetails = EditorGUILayout.Toggle("Log Conversion Details", _logConversionDetails);
                
                EditorGUILayout.Space(5);
                if (_createBackup)
                {
                    EditorGUILayout.HelpBox("Backup copies will be created with '.backup' suffix before conversion.", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawTextureGenerationOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showTextureGenerationOptions = EditorGUILayout.Foldout(_showTextureGenerationOptions, "Texture Generation Options", true);
            
            if (_showTextureGenerationOptions)
            {
                EditorGUILayout.Space(5);
                
                _autoGenerateMissingTextures = EditorGUILayout.Toggle("Auto-Generate Missing Textures", _autoGenerateMissingTextures);
                
                if (_autoGenerateMissingTextures)
                {
                    EditorGUILayout.HelpBox("Automatically generates missing texture maps during conversion. Generated textures are saved in the same folder as the material.", MessageType.Info);
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Select Maps to Generate:", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel++;
                    
                    // Normal Map
                    _generateNormalMap = EditorGUILayout.Toggle("Normal Map", _generateNormalMap);
                    if (_generateNormalMap)
                    {
                        EditorGUI.indentLevel++;
                        _normalMapStrength = EditorGUILayout.IntSlider("Strength", _normalMapStrength, 1, 10);
                        EditorGUILayout.LabelField("Generated from height/base texture", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    
                    // Metallic Map
                    _generateMetallicMap = EditorGUILayout.Toggle("Metallic/Smoothness Map", _generateMetallicMap);
                    if (_generateMetallicMap)
                    {
                        EditorGUI.indentLevel++;
                        _metallicValue = EditorGUILayout.Slider("Metallic Value", _metallicValue, 0f, 1f);
                        _smoothnessValue = EditorGUILayout.Slider("Smoothness Value", _smoothnessValue, 0f, 1f);
                        EditorGUILayout.LabelField("Metallic stored in R, Smoothness in A", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    
                    // Height Map
                    _generateHeightMap = EditorGUILayout.Toggle("Height Map", _generateHeightMap);
                    if (_generateHeightMap)
                    {
                        EditorGUI.indentLevel++;
                        _heightScale = EditorGUILayout.Slider("Height Scale", _heightScale, 0.01f, 0.2f);
                        EditorGUILayout.LabelField("Generated from base texture luminance", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    
                    // Occlusion Map
                    _generateOcclusionMap = EditorGUILayout.Toggle("Occlusion Map", _generateOcclusionMap);
                    if (_generateOcclusionMap)
                    {
                        EditorGUI.indentLevel++;
                        _occlusionStrength = EditorGUILayout.Slider("AO Strength", _occlusionStrength, 0f, 1f);
                        EditorGUILayout.LabelField("Generated from base texture darkening", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    
                    // Emission Map
                    _generateEmissionMap = EditorGUILayout.Toggle("Emission Map", _generateEmissionMap);
                    if (_generateEmissionMap)
                    {
                        EditorGUI.indentLevel++;
                        _emissionColor = EditorGUILayout.ColorField("Emission Color", _emissionColor);
                        _emissionAlgorithm = (EmissionAlgorithm)EditorGUILayout.EnumPopup("Algorithm", _emissionAlgorithm);
                        _emissionCoverage = EditorGUILayout.Slider("Emission Coverage", _emissionCoverage, 0.02f, 0.5f);
                        EditorGUILayout.LabelField("Higher coverage tags more colorful spots as emissive", EditorStyles.miniLabel);
                            _emissionMaskOnly = EditorGUILayout.Toggle("Mask Only (Grayscale)", _emissionMaskOnly);
                            _emissionBinaryMask = EditorGUILayout.Toggle("Binary Mask (Black/White)", _emissionBinaryMask);
                            _emissionFeather = EditorGUILayout.IntSlider("Edge Feather (px)", _emissionFeather, 0, 8);
                        EditorGUI.indentLevel--;
                    }

                    // Coat Mask (HDRP only)
                    _generateCoatMask = EditorGUILayout.Toggle("Coat Mask (HDRP)", _generateCoatMask);
                    if (_generateCoatMask)
                    {
                        EditorGUI.indentLevel++;
                        _coatCoverage = EditorGUILayout.Slider("Coat Coverage", _coatCoverage, 0.05f, 0.6f);
                        _coatFeather = EditorGUILayout.IntSlider("Edge Feather (px)", _coatFeather, 0, 8);
                        _preserveExistingCoat = EditorGUILayout.Toggle("Preserve Existing Coat B", _preserveExistingCoat);
                        EditorGUILayout.LabelField("Writes to Mask Map B (HDRP).", EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.Space(5);
                    
                    // Standalone texture generation button
                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("Generate Textures for Existing Materials", GUILayout.Height(30)))
                    {
                        GenerateTexturesForExistingMaterials();
                        GUIUtility.ExitGUI();
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.HelpBox("Use this button to generate missing textures for already-converted materials without re-converting them.", MessageType.Info);
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawConversionButton()
        {
            EditorGUI.BeginDisabledGroup((!_autoDetectSourceRP && ArePipelinesEquivalent(_sourceRP, _sourceCustomMappingIndex, _targetRP, _targetCustomMappingIndex)) || !CanConvert());
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Convert Shaders", GUILayout.Height(40)))
            {
                PerformConversion();
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUI.EndDisabledGroup();
        }

        private void DrawHelp()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Help", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Select source and target render pipelines", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• Choose materials to convert", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• Properties without equivalents will be skipped", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• Enable backup to preserve originals", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Conversion Logic

        private bool CanConvert()
        {
            switch (_conversionMode)
            {
                case ConversionMode.SelectedMaterials:
                    return _selectedMaterials.Count > 0;
                    
                case ConversionMode.MaterialsInFolder:
                    return _targetFolder != null;
                    
                case ConversionMode.AllProjectMaterials:
                    return true;
                    
                default:
                    return false;
            }
        }

        private TextureGenerationSettings GetCurrentGenerationSettings()
        {
            return new TextureGenerationSettings
            {
                GenerateNormalMap = _generateNormalMap,
                GenerateBentNormalMap = _generateBentNormalMap,
                GenerateMetallicMap = _generateMetallicMap,
                GenerateHeightMap = _generateHeightMap,
                GenerateOcclusionMap = _generateOcclusionMap,
                GenerateEmissionMap = _generateEmissionMap,
                NormalMapStrength = _normalMapStrength,
                BentNormalStrength = _bentNormalStrength,
                MetallicValue = _metallicValue,
                SmoothnessValue = _smoothnessValue,
                HeightScale = _heightScale,
                OcclusionStrength = _occlusionStrength,
                EmissionColor = _emissionColor,
                EmissionCoverage = _emissionCoverage,
                EmissionMaskOnly = _emissionMaskOnly,
                EmissionBinaryMask = _emissionBinaryMask,
                EmissionAlgorithm = _emissionAlgorithm,
                EmissionFeather = _emissionFeather,
                GenerateDetailMap = _generateDetailMap,
                DetailStrength = _detailStrength,
                GenerateCoatMask = _generateCoatMask,
                CoatCoverage = _coatCoverage,
                CoatFeather = _coatFeather,
                PreserveExistingCoat = _preserveExistingCoat
            };
        }

        private void RefreshSelectedMaterials()
        {
            _selectedMaterials.Clear();
            
            foreach (var obj in Selection.objects)
            {
                if (obj is Material mat)
                {
                    _selectedMaterials.Add(mat);
                }
            }

            if (_autoDetectSourceRP && _selectedMaterials.Count > 0)
            {
                DetectSourcePipelineFromMaterials(_selectedMaterials);
            }
            
            Repaint();
        }

        private void DetectSourcePipelineFromMaterials(List<Material> materials)
        {
            int builtInCount = 0;
            int urpCount = 0;
            int hdrpCount = 0;
            Dictionary<int, int> customCounts = new Dictionary<int, int>();

            foreach (var mat in materials)
            {
                if (mat == null || mat.shader == null) continue;
                string shaderName = mat.shader.name;

                // Check custom mappings first
                var mappings = ProjectSettings?.ShaderMappings;
                if (mappings != null)
                {
                    for (int i = 0; i < mappings.Count; i++)
                    {
                        var mapping = mappings[i];
                        if (mapping == null || string.IsNullOrWhiteSpace(mapping.shaderNameContains))
                            continue;
                        if (shaderName.IndexOf(mapping.shaderNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            customCounts[i] = customCounts.ContainsKey(i) ? customCounts[i] + 1 : 1;
                            goto NextMaterial;
                        }
                    }
                }

                if (shaderName.Contains("Universal Render Pipeline") || shaderName.Contains("URP"))
                {
                    urpCount++;
                }
                else if (shaderName.Contains("HDRP") || shaderName.Contains("High Definition Render Pipeline"))
                {
                    hdrpCount++;
                }
                else
                {
                    // Assume built-in for Standard, Unlit, Legacy, etc.
                    builtInCount++;
                }

                NextMaterial: ;
            }

            int bestCustomIndex = -1;
            int bestCustomCount = 0;
            foreach (var kvp in customCounts)
            {
                if (kvp.Value > bestCustomCount)
                {
                    bestCustomCount = kvp.Value;
                    bestCustomIndex = kvp.Key;
                }
            }

            if (bestCustomCount > urpCount && bestCustomCount > hdrpCount && bestCustomCount > builtInCount)
            {
                _sourceRP = RenderPipeline.Custom;
                _sourceCustomMappingIndex = bestCustomIndex;
            }
            else if (urpCount > builtInCount && urpCount > hdrpCount)
            {
                _sourceRP = RenderPipeline.URP;
                _sourceCustomMappingIndex = -1;
            }
            else if (hdrpCount > builtInCount && hdrpCount > urpCount)
            {
                _sourceRP = RenderPipeline.HDRP;
                _sourceCustomMappingIndex = -1;
            }
            else
            {
                _sourceRP = RenderPipeline.BuiltIn;
                _sourceCustomMappingIndex = -1;
            }
        }

        private void PerformConversion()
        {
            List<Material> materialsToConvert = GetMaterialsToConvert();
            
            if (materialsToConvert.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials", "No materials found to convert.", "OK");
                return;
            }

            if (_autoDetectSourceRP)
            {
                DetectSourcePipelineFromMaterials(materialsToConvert);
            }
            
            if (!EditorUtility.DisplayDialog(
                "Confirm Conversion",
                $"Convert {materialsToConvert.Count} material(s) from {_sourceRP} to {_targetRP}?\n\n" +
                (_createBackup ? "Backups will be created." : "No backups will be created."),
                "Convert",
                "Cancel"))
            {
                return;
            }
            
            int successCount = 0;
            int skippedCount = 0;
            
            EditorUtility.DisplayProgressBar("Converting Shaders", "Starting conversion...", 0f);
            AssetDatabase.StartAssetEditing(); // Batch imports to prevent constant re-compilation
            
            try
            {
                for (int i = 0; i < materialsToConvert.Count; i++)
                {
                    Material mat = materialsToConvert[i];
                    
                    // Safety check - material might have been destroyed or be null
                    if (mat == null)
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    // Get name safely for progress bar
                    string matName;
                    try
                    {
                        matName = mat.name;
                    }
                    catch (MissingReferenceException)
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    EditorUtility.DisplayProgressBar(
                        "Converting Shaders",
                        $"Converting {matName} ({i + 1}/{materialsToConvert.Count})",
                        (float)i / materialsToConvert.Count
                    );
                    
                    if (ConvertMaterial(mat))
                    {
                        successCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing(); // Resume imports
                EditorUtility.ClearProgressBar();
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"Conversion Complete!\n\n" +
                           $"Successfully converted: {successCount}\n" +
                           $"Skipped: {skippedCount}";
            
            EditorUtility.DisplayDialog("Conversion Complete", message, "OK");
            
            Debug.Log($"[Shader Converter] {message}");
        }

        private List<Material> GetMaterialsToConvert()
        {
            switch (_conversionMode)
            {
                case ConversionMode.SelectedMaterials:
                    RefreshSelectedMaterials();
                    return _selectedMaterials;
                    
                case ConversionMode.MaterialsInFolder:
                    return GetMaterialsInFolder();
                    
                case ConversionMode.AllProjectMaterials:
                    return GetAllProjectMaterials();
                    
                default:
                    return new List<Material>();
            }
        }

        private List<Material> GetMaterialsInFolder()
        {
            List<Material> materials = new List<Material>();
            
            if (_targetFolder == null)
                return materials;
            
            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
            
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    materials.Add(mat);
                }
            }
            
            return materials;
        }

        private List<Material> GetAllProjectMaterials()
        {
            List<Material> materials = new List<Material>();
            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    materials.Add(mat);
                }
            }
            
            return materials;
        }

        private bool ConvertMaterial(Material material)
        {
            // Comprehensive null/destroyed check
            if (material == null || !material)
                return false;
            
            // Wrap entire conversion in try-catch to handle any destroyed object exceptions
            try
            {
                // Safety check - material might have been destroyed during iteration
                // Access multiple properties to ensure the object is truly valid
                var sourceShader = material.shader;
                if (sourceShader == null)
                {
                    Debug.LogWarning($"[Shader Converter] Material has null shader. Skipping.");
                    return false;
                }
                
                string materialName = material.name;
                string currentShaderName = sourceShader.name;
                
                // Skip materials that already use a target pipeline shader
                if (IsAlreadyTargetPipelineShader(currentShaderName))
                {
                    if (_logConversionDetails)
                    {
                        Debug.Log($"[Shader Converter] Material '{materialName}' already uses target pipeline shader '{currentShaderName}'. Skipping.");
                    }
                    return false;
                }
                
                // Create backup if enabled
                if (_createBackup)
                {
                    CreateBackup(material);
                }
                
                // Re-check material validity after backup (AssetDatabase operations can invalidate references)
                if (material == null || !material)
                {
                    Debug.LogWarning($"[Shader Converter] Material '{materialName}' became invalid after backup. Skipping.");
                    return false;
                }
                
                // Get target shader
                Shader targetShader = GetTargetShader(material.shader);
            
            if (targetShader == null)
            {
                if (_logConversionDetails)
                {
                    Debug.LogWarning($"[Shader Converter] No equivalent shader found for '{material.shader.name}' on material '{material.name}'. Skipping.");
                }
                return false;
            }
            
            // Store original properties
            int originalRenderQueue = material.renderQueue;
            
            // Get property mapping
            Dictionary<string, string> propertyMap = BuildPropertyMap(material.shader, targetShader);
            
            // Store properties to transfer
            Dictionary<string, object> propertiesToTransfer = new Dictionary<string, object>();
            
            foreach (var kvp in propertyMap)
            {
                string sourceProperty = kvp.Key;
                string targetProperty = kvp.Value;
                
                // Try to get the property value from source material
                if (material.HasProperty(sourceProperty))
                {
                    // Check property type and store value
                    var shader = material.shader;
                    int propertyIndex = shader.FindPropertyIndex(sourceProperty);
                    
                    if (propertyIndex >= 0)
                    {
                        var propertyType = shader.GetPropertyType(propertyIndex);
                        
                        switch (propertyType)
                        {
                            case UnityEngine.Rendering.ShaderPropertyType.Color:
                                propertiesToTransfer[targetProperty] = material.GetColor(sourceProperty);
                                break;
                                
                            case UnityEngine.Rendering.ShaderPropertyType.Vector:
                                propertiesToTransfer[targetProperty] = material.GetVector(sourceProperty);
                                break;
                                
                            case UnityEngine.Rendering.ShaderPropertyType.Float:
                            case UnityEngine.Rendering.ShaderPropertyType.Range:
                                propertiesToTransfer[targetProperty] = material.GetFloat(sourceProperty);
                                break;
                                
                            case UnityEngine.Rendering.ShaderPropertyType.Texture:
                                var tex = material.GetTexture(sourceProperty);
                                if (tex != null)
                                {
                                    propertiesToTransfer[targetProperty] = tex;
                                    // Also store texture scale/offset
                                    propertiesToTransfer[targetProperty + "_ST"] = material.GetTextureScale(sourceProperty);
                                    propertiesToTransfer[targetProperty + "_Offset"] = material.GetTextureOffset(sourceProperty);
                                }
                                break;
                        }
                    }
                }
            }
            
            // Assign new shader
            material.shader = targetShader;
            
            // Apply transferred properties
            foreach (var kvp in propertiesToTransfer)
            {
                string targetProperty = kvp.Key;
                object value = kvp.Value;
                
                if (!material.HasProperty(targetProperty))
                {
                    if (_logConversionDetails)
                    {
                        Debug.Log($"[Shader Converter] Target shader doesn't have property '{targetProperty}' for material '{material.name}'. Skipping.");
                    }
                    continue;
                }
                
                try
                {
                    if (value is Color color)
                    {
                        material.SetColor(targetProperty, color);
                    }
                    else if (value is Vector4 vector)
                    {
                        if (targetProperty.EndsWith("_ST"))
                        {
                            // This is a texture scale
                            string texProp = targetProperty.Replace("_ST", "");
                            material.SetTextureScale(texProp, new Vector2(vector.x, vector.y));
                        }
                        else if (targetProperty.EndsWith("_Offset"))
                        {
                            // This is a texture offset
                            string texProp = targetProperty.Replace("_Offset", "");
                            material.SetTextureOffset(texProp, new Vector2(vector.x, vector.y));
                        }
                        else
                        {
                            material.SetVector(targetProperty, vector);
                        }
                    }
                    else if (value is float floatValue)
                    {
                        material.SetFloat(targetProperty, floatValue);
                    }
                    else if (value is Texture texture)
                    {
                        material.SetTexture(targetProperty, texture);
                    }
                }
                catch (System.Exception e)
                {
                    if (_logConversionDetails)
                    {
                        Debug.LogWarning($"[Shader Converter] Failed to set property '{targetProperty}' on material '{material.name}': {e.Message}");
                    }
                }
            }
            
            // Restore render queue if enabled
            if (_preserveRenderQueue)
            {
                material.renderQueue = originalRenderQueue;
            }
            
            // Generate missing textures if enabled
            if (_autoGenerateMissingTextures)
            {
                GenerateMissingTexturesForMaterial(material, GetCurrentGenerationSettings(), _logConversionDetails);
            }
            
            EditorUtility.SetDirty(material);
            
            if (_logConversionDetails)
            {
                Debug.Log($"[Shader Converter] Successfully converted '{material.name}' from '{material.shader.name}' to '{targetShader.name}'");
            }
            
            return true;
            }
            catch (MissingReferenceException)
            {
                Debug.LogWarning($"[Shader Converter] Material was destroyed during conversion. Skipping.");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Shader Converter] Unexpected error during material conversion: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a shader is already part of the target render pipeline.
        /// This prevents unnecessary conversions and avoids "no equivalent found" warnings
        /// for materials that don't need conversion.
        /// </summary>
        private bool IsAlreadyTargetPipelineShader(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName))
                return false;
            
            switch (_targetRP)
            {
                case RenderPipeline.URP:
                    return shaderName.StartsWith("Universal Render Pipeline/") ||
                           shaderName.StartsWith("Shader Graphs/") ||
                           shaderName.Contains("URP");
                    
                case RenderPipeline.HDRP:
                    return shaderName.StartsWith("HDRP/") ||
                           shaderName.StartsWith("Shader Graphs/") ||
                           shaderName.Contains("HDRP");
                    
                case RenderPipeline.BuiltIn:
                    // Built-in shaders don't have a prefix, but URP/HDRP do
                    // So if it's not URP or HDRP, assume it's built-in compatible
                    return !shaderName.StartsWith("Universal Render Pipeline/") &&
                           !shaderName.StartsWith("HDRP/") &&
                           !shaderName.Contains("URP") &&
                           !shaderName.Contains("HDRP");

                case RenderPipeline.Custom:
                    var mapping = ProjectSettings.GetMapping(_targetCustomMappingIndex);
                    if (mapping != null && !string.IsNullOrWhiteSpace(mapping.shaderNameContains))
                    {
                        return shaderName.IndexOf(mapping.shaderNameContains, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    return false;
                    
                default:
                    return false;
            }
        }

        private void CreateBackup(Material material)
        {
            string path = AssetDatabase.GetAssetPath(material);
            string backupPath = path.Replace(".mat", ".backup.mat");
            
            if (!AssetDatabase.CopyAsset(path, backupPath))
            {
                Debug.LogWarning($"[Shader Converter] Failed to create backup for '{material.name}'");
            }
        }

        private Shader GetTargetShader(Shader sourceShader)
        {
            string sourceName = sourceShader.name;
            string targetName = GetTargetShaderName(sourceName);
            
            if (_targetRP == RenderPipeline.Custom)
            {
                Shader customShader = FindShaderForMapping(_targetCustomMappingIndex);
                if (customShader == null && _logConversionDetails)
                {
                    Debug.LogWarning("[Shader Converter] Target custom shader not found. Check your Texture Studio mappings and ensure the shader is in the project.");
                }
                return customShader;
            }

            if (string.IsNullOrEmpty(targetName))
                return null;

            Shader targetShader = Shader.Find(targetName);

            if (targetShader == null && _logConversionDetails)
            {
                Debug.LogWarning($"[Shader Converter] Target shader '{targetName}' not found in project. Make sure the target render pipeline is installed.");
            }

            return targetShader;
        }

        private Shader FindShaderForMapping(int mappingIndex)
        {
            var mapping = ProjectSettings.GetMapping(mappingIndex);
            if (mapping == null || string.IsNullOrWhiteSpace(mapping.shaderNameContains))
                return null;

            string needle = mapping.shaderNameContains;
            var shaders = Resources.FindObjectsOfTypeAll<Shader>();
            return shaders.FirstOrDefault(s => s != null && s.name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private string GetTargetShaderName(string sourceShaderName)
        {
            // Normalize shader name
            sourceShaderName = sourceShaderName.Trim();
            
            // Built-in to URP
            if (_sourceRP == RenderPipeline.BuiltIn && _targetRP == RenderPipeline.URP)
            {
                if (sourceShaderName.Contains("Standard"))
                    return ShaderPaths.URP_Lit;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.URP_Unlit;
            }
            
            // Built-in to HDRP
            if (_sourceRP == RenderPipeline.BuiltIn && _targetRP == RenderPipeline.HDRP)
            {
                if (sourceShaderName.Contains("Standard"))
                    return ShaderPaths.HDRP_Lit;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.HDRP_Unlit;
            }
            
            // URP to Built-in
            if (_sourceRP == RenderPipeline.URP && _targetRP == RenderPipeline.BuiltIn)
            {
                if (sourceShaderName.Contains("Lit") && !sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.BuiltIn_Standard;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.BuiltIn_Unlit;
            }
            
            // URP to HDRP
            if (_sourceRP == RenderPipeline.URP && _targetRP == RenderPipeline.HDRP)
            {
                if (sourceShaderName.Contains("Lit") && !sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.HDRP_Lit;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.HDRP_Unlit;
                if (sourceShaderName.Contains("Terrain"))
                    return ShaderPaths.HDRP_TerrainLit;
            }
            
            // HDRP to Built-in
            if (_sourceRP == RenderPipeline.HDRP && _targetRP == RenderPipeline.BuiltIn)
            {
                if (sourceShaderName.Contains("Lit") && !sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.BuiltIn_Standard;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.BuiltIn_Unlit;
            }
            
            // HDRP to URP
            if (_sourceRP == RenderPipeline.HDRP && _targetRP == RenderPipeline.URP)
            {
                if (sourceShaderName.Contains("Lit") && !sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.URP_Lit;
                if (sourceShaderName.Contains("Unlit"))
                    return ShaderPaths.URP_Unlit;
                if (sourceShaderName.Contains("Terrain"))
                    return ShaderPaths.URP_Terrain;
            }

            // Custom to standard pipelines: default to primary lit shaders
            if (_sourceRP == RenderPipeline.Custom)
            {
                if (_targetRP == RenderPipeline.BuiltIn)
                    return ShaderPaths.BuiltIn_Standard;
                if (_targetRP == RenderPipeline.URP)
                    return ShaderPaths.URP_Lit;
                if (_targetRP == RenderPipeline.HDRP)
                    return ShaderPaths.HDRP_Lit;
            }
            
            return null;
        }

        private Dictionary<string, string> GetPropertyMap(RenderPipeline source, RenderPipeline target)
        {
            if (source == RenderPipeline.BuiltIn && target == RenderPipeline.URP)
                return PropertyMaps.BuiltInToURP;
            
            if (source == RenderPipeline.URP && target == RenderPipeline.BuiltIn)
                return PropertyMaps.URPToBuiltIn;
            
            if (source == RenderPipeline.BuiltIn && target == RenderPipeline.HDRP)
                return PropertyMaps.BuiltInToHDRP;
            
            if (source == RenderPipeline.HDRP && target == RenderPipeline.BuiltIn)
                return PropertyMaps.HDRPToBuiltIn;
            
            if (source == RenderPipeline.URP && target == RenderPipeline.HDRP)
                return PropertyMaps.URPToHDRP;
            
            if (source == RenderPipeline.HDRP && target == RenderPipeline.URP)
                return PropertyMaps.HDRPToURP;
            
            return new Dictionary<string, string>();
        }

        private static readonly TextureSlot[] SlotsToMap = new[]
        {
            TextureSlot.Base,
            TextureSlot.Normal,
            TextureSlot.Height,
            TextureSlot.Occlusion,
            TextureSlot.Emission,
            TextureSlot.Detail,
            TextureSlot.BentNormal,
            TextureSlot.CoatMask
        };

        private static readonly Dictionary<TextureSlot, string[]> BuiltInSlotFallbacks = new Dictionary<TextureSlot, string[]>
        {
            { TextureSlot.Base, new[] { "_MainTex" } },
            { TextureSlot.Normal, new[] { "_BumpMap" } },
            { TextureSlot.Height, new[] { "_ParallaxMap" } },
            { TextureSlot.MetallicMask, new[] { "_MetallicGlossMap" } },
            { TextureSlot.Occlusion, new[] { "_OcclusionMap" } },
            { TextureSlot.Emission, new[] { "_EmissionMap" } },
            { TextureSlot.Detail, new[] { "_DetailAlbedoMap" } },
            { TextureSlot.BentNormal, Array.Empty<string>() },
            { TextureSlot.CoatMask, Array.Empty<string>() }
        };

        private static readonly Dictionary<TextureSlot, string[]> URPSlotFallbacks = new Dictionary<TextureSlot, string[]>
        {
            { TextureSlot.Base, new[] { "_BaseMap" } },
            { TextureSlot.Normal, new[] { "_BumpMap" } },
            { TextureSlot.Height, new[] { "_ParallaxMap" } },
            { TextureSlot.MetallicMask, new[] { "_MetallicGlossMap" } },
            { TextureSlot.Occlusion, new[] { "_OcclusionMap" } },
            { TextureSlot.Emission, new[] { "_EmissionMap" } },
            { TextureSlot.Detail, new[] { "_DetailAlbedoMap" } },
            { TextureSlot.BentNormal, Array.Empty<string>() },
            { TextureSlot.CoatMask, new[] { "_CoatMask" } }
        };

        private static readonly Dictionary<TextureSlot, string[]> HDRPSlotFallbacks = new Dictionary<TextureSlot, string[]>
        {
            { TextureSlot.Base, new[] { "_BaseColorMap" } },
            { TextureSlot.Normal, new[] { "_NormalMap" } },
            { TextureSlot.Height, new[] { "_HeightMap", "_ParallaxMap" } },
            { TextureSlot.MetallicMask, new[] { "_MaskMap" } },
            { TextureSlot.Occlusion, new[] { "_MaskMap" } },
            { TextureSlot.Emission, new[] { "_EmissiveColorMap" } },
            { TextureSlot.Detail, new[] { "_DetailMap" } },
            { TextureSlot.BentNormal, new[] { "_BentNormalMap" } },
            { TextureSlot.CoatMask, new[] { "_CoatMaskMap" } }
        };

        private string[] GetFallbacksForPipeline(RenderPipeline pipeline, TextureSlot slot)
        {
            switch (pipeline)
            {
                case RenderPipeline.BuiltIn:
                    return BuiltInSlotFallbacks.TryGetValue(slot, out var bi) ? bi : Array.Empty<string>();
                case RenderPipeline.URP:
                    return URPSlotFallbacks.TryGetValue(slot, out var urp) ? urp : Array.Empty<string>();
                case RenderPipeline.HDRP:
                    return HDRPSlotFallbacks.TryGetValue(slot, out var hdrp) ? hdrp : Array.Empty<string>();
                default:
                    return Array.Empty<string>();
            }
        }

        private static IEnumerable<string> GetMappingSlotProperties(ShaderTexturePropertyMap mapping, TextureSlot slot)
        {
            if (mapping == null) return Array.Empty<string>();
            switch (slot)
            {
                case TextureSlot.Base: return mapping.baseMapProperties ?? Array.Empty<string>();
                case TextureSlot.Normal: return mapping.normalMapProperties ?? Array.Empty<string>();
                case TextureSlot.Height: return mapping.heightMapProperties ?? Array.Empty<string>();
                case TextureSlot.MetallicMask: return mapping.metallicMaskProperties ?? Array.Empty<string>();
                case TextureSlot.Occlusion: return mapping.occlusionMapProperties ?? Array.Empty<string>();
                case TextureSlot.Emission: return mapping.emissionMapProperties ?? Array.Empty<string>();
                case TextureSlot.BentNormal: return mapping.bentNormalMapProperties ?? Array.Empty<string>();
                case TextureSlot.Detail: return mapping.detailMapProperties ?? Array.Empty<string>();
                case TextureSlot.CoatMask: return mapping.coatMaskMapProperties ?? Array.Empty<string>();
                default: return Array.Empty<string>();
            }
        }

        private string ResolvePropertyForSlot(Shader shader, RenderPipeline pipeline, int mappingIndex, TextureSlot slot)
        {
            IEnumerable<string> candidates = Array.Empty<string>();

            if (pipeline == RenderPipeline.Custom)
            {
                var mapping = ProjectSettings.GetMapping(mappingIndex);
                if (mapping != null)
                {
                    candidates = GetMappingSlotProperties(mapping, slot);
                }

                // Also allow configured global mappings + fallbacks for whatever pipeline the shader belongs to
                candidates = candidates.Concat(GetPropertyNamesForSlot(shader, slot, Array.Empty<string>()));
            }
            else
            {
                candidates = GetPropertyNamesForSlot(shader, slot, GetFallbacksForPipeline(pipeline, slot));
            }

            foreach (var name in candidates)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                if (shader != null && shader.FindPropertyIndex(name) >= 0)
                    return name;
            }

            return string.Empty;
        }

        private Dictionary<string, string> BuildPropertyMap(Shader sourceShader, Shader targetShader)
        {
            var map = new Dictionary<string, string>();

            foreach (var slot in SlotsToMap)
            {
                string sourceProp = ResolvePropertyForSlot(sourceShader, _sourceRP, _sourceCustomMappingIndex, slot);
                string targetProp = ResolvePropertyForSlot(targetShader, _targetRP, _targetCustomMappingIndex, slot);

                if (string.IsNullOrEmpty(sourceProp) || string.IsNullOrEmpty(targetProp))
                    continue;

                if (!map.ContainsKey(sourceProp))
                    map[sourceProp] = targetProp;
            }

            // Fallback to existing static maps if nothing was found
            if (map.Count == 0)
            {
                map = GetPropertyMap(_sourceRP, _targetRP);
            }

            return map;
        }

        #endregion

        #region Texture Generation

        private static TextureStudioProjectSettings ProjectSettings => TextureStudioProjectSettings.GetOrCreateSettings();

        internal static string[] GetPropertyNamesForSlot(Shader shader, TextureSlot slot, params string[] fallback)
        {
            IEnumerable<string> custom = ProjectSettings.GetPropertyNames(shader, slot);
            IEnumerable<string> combined = custom ?? Enumerable.Empty<string>();

            if (fallback != null && fallback.Length > 0)
                combined = combined.Concat(fallback);

            return combined.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray();
        }

        /// <summary>
        /// Generates missing textures for existing materials without converting the shader.
        /// Useful for materials that have already been converted but need missing texture maps.
        /// </summary>
        private void GenerateTexturesForExistingMaterials()
        {
            if (!_autoGenerateMissingTextures)
            {
                EditorUtility.DisplayDialog("Feature Disabled", "Auto-generate missing textures is not enabled.", "OK");
                return;
            }

            List<Material> materialsToProcess = GetMaterialsToConvert();
            
            if (materialsToProcess.Count == 0)
            {
                EditorUtility.DisplayDialog("No Materials", "No materials found to process.", "OK");
                return;
            }
            
            if (!EditorUtility.DisplayDialog(
                "Generate Textures",
                $"Generate missing textures for {materialsToProcess.Count} material(s)?\n\n" +
                $"This will create texture files in the same folders as the materials.",
                "Generate",
                "Cancel"))
            {
                return;
            }
            
            int successCount = 0;
            int skippedCount = 0;
            
            EditorUtility.DisplayProgressBar("Generating Textures", "Starting...", 0f);
            AssetDatabase.StartAssetEditing();
            
            try
            {
                for (int i = 0; i < materialsToProcess.Count; i++)
                {
                    Material mat = materialsToProcess[i];
                    
                    EditorUtility.DisplayProgressBar(
                        "Generating Textures",
                        $"Processing {mat.name} ({i + 1}/{materialsToProcess.Count})",
                        (float)i / materialsToProcess.Count
                    );
                    
                    if (GenerateMissingTexturesForMaterial(mat, GetCurrentGenerationSettings(), _logConversionDetails))
                    {
                        successCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"Texture Generation Complete!\n\n" +
                           $"Materials processed: {successCount}\n" +
                           $"Skipped: {skippedCount}";
            
            EditorUtility.DisplayDialog("Generation Complete", message, "OK");
            
            Debug.Log($"[Shader Converter] {message}");
        }

        /// <summary>
        /// Generates missing textures for a single material during or after conversion.
        /// Reused by the material inspector helper and the converter window.
        /// </summary>
        internal static bool GenerateMissingTexturesForMaterial(Material material, TextureGenerationSettings settings, bool logMessages)
        {
            if (material == null)
                return false;

            bool anyGenerated = false;
            string materialPath = AssetDatabase.GetAssetPath(material);
            string materialFolder = System.IO.Path.GetDirectoryName(materialPath);
            string materialName = material.name;

            Texture2D baseTexture = GetBaseTexture(material);
            if (baseTexture == null)
            {
                if (logMessages)
                {
                    Debug.LogWarning($"[Shader Converter] No base texture found for material '{materialName}'. Cannot generate textures.");
                }
                return false;
            }

            if (!MakeTextureReadable(baseTexture))
            {
                Debug.LogWarning($"[Shader Converter] Could not make base texture readable for '{materialName}'. Skipping texture generation.");
                return false;
            }

            RenderPipeline detectedPipeline = DetectPipelineFromShader(material.shader);

            var normalProps = GetPropertyNamesForSlot(material.shader, TextureSlot.Normal, "_BumpMap", "_NormalMap");
            var heightProps = GetPropertyNamesForSlot(material.shader, TextureSlot.Height, "_ParallaxMap", "_HeightMap");
            var metallicProps = GetPropertyNamesForSlot(material.shader, TextureSlot.MetallicMask, "_MetallicGlossMap", "_MaskMap");
            var occlusionProps = GetPropertyNamesForSlot(material.shader, TextureSlot.Occlusion, "_OcclusionMap");
            var emissionProps = GetPropertyNamesForSlot(material.shader, TextureSlot.Emission, "_EmissionMap", "_EmissiveColorMap");
            var bentNormalProps = GetPropertyNamesForSlot(material.shader, TextureSlot.BentNormal, "_BentNormalMap");
            var detailProps = GetPropertyNamesForSlot(material.shader, TextureSlot.Detail, "_DetailMap");
            var coatProps = GetPropertyNamesForSlot(material.shader, TextureSlot.CoatMask, "_CoatMaskMap");

            // Normal Map
            if (settings.GenerateNormalMap && !HasTexture(material, normalProps))
            {
                Texture2D normalMap = GenerateNormalMapFromTexture(baseTexture, settings.NormalMapStrength);
                if (normalMap != null)
                {
                    string normalPath = SaveGeneratedTexture(normalMap, materialFolder, materialName, "Normal");
                    if (!string.IsNullOrEmpty(normalPath))
                    {
                        AssignTextureToMaterial(material, normalPath, normalProps);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Normal Map for '{materialName}'");
                    }
                }
            }

            // Height Map
            if (settings.GenerateHeightMap && !HasTexture(material, heightProps))
            {
                Texture2D heightMap = GenerateHeightMapFromTexture(baseTexture);
                if (heightMap != null)
                {
                    string heightPath = SaveGeneratedTexture(heightMap, materialFolder, materialName, "Height");
                    if (!string.IsNullOrEmpty(heightPath))
                    {
                        AssignTextureToMaterial(material, heightPath, heightProps);
                        if (material.HasProperty("_Parallax"))
                            material.SetFloat("_Parallax", settings.HeightScale);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Height Map for '{materialName}'");
                    }
                }
            }

            Texture2D occlusionSource = TryGetTexture(material, occlusionProps);
            Texture2D generatedOcclusion = null;
            Texture2D existingMaskMap = TryGetTexture(material, metallicProps);

            // Occlusion Map
            if (settings.GenerateOcclusionMap && !HasTexture(material, occlusionProps))
            {
                generatedOcclusion = GenerateOcclusionMapFromTexture(baseTexture, settings.OcclusionStrength);
                if (generatedOcclusion != null)
                {
                    string occlusionPath = SaveGeneratedTexture(generatedOcclusion, materialFolder, materialName, "Occlusion");
                    if (!string.IsNullOrEmpty(occlusionPath))
                    {
                        AssignTextureToMaterial(material, occlusionPath, occlusionProps);
                        if (material.HasProperty("_OcclusionStrength"))
                            material.SetFloat("_OcclusionStrength", settings.OcclusionStrength);
                        anyGenerated = true;
                        occlusionSource = generatedOcclusion;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Occlusion Map for '{materialName}'");
                    }
                }
            }

            // Bent Normal Map (HDRP)
            if (detectedPipeline == RenderPipeline.HDRP && settings.GenerateBentNormalMap && !HasTexture(material, bentNormalProps))
            {
                Texture2D bentNormal = GenerateBentNormalMap(baseTexture, occlusionSource ?? generatedOcclusion, settings.BentNormalStrength);
                if (bentNormal != null)
                {
                    string bentPath = SaveGeneratedTexture(bentNormal, materialFolder, materialName, "BentNormal");
                    if (!string.IsNullOrEmpty(bentPath))
                    {
                        AssignTextureToMaterial(material, bentPath, bentNormalProps);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Bent Normal Map for '{materialName}'");
                    }
                }
            }

            // Detail Map (HDRP)
            if (detectedPipeline == RenderPipeline.HDRP && settings.GenerateDetailMap && !HasTexture(material, detailProps))
            {
                Texture2D detailMap = GenerateDetailMap(baseTexture, settings.DetailStrength);
                if (detailMap != null)
                {
                    string detailPath = SaveGeneratedTexture(detailMap, materialFolder, materialName, "Detail");
                    if (!string.IsNullOrEmpty(detailPath))
                    {
                        AssignTextureToMaterial(material, detailPath, detailProps);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Detail Map for '{materialName}'");
                    }
                }
            }

            // Metallic / Mask Map
            bool coatAlreadyGenerated = false;
            if (settings.GenerateMetallicMap && !HasTexture(material, metallicProps))
            {
                if (occlusionSource != null && !MakeTextureReadable(occlusionSource))
                {
                    occlusionSource = null;
                }

                Texture2D coatMask = null;
                if (detectedPipeline == RenderPipeline.HDRP && settings.GenerateCoatMask)
                {
                    coatMask = GenerateCoatMask(baseTexture, settings.CoatCoverage, settings.CoatFeather);
                    coatAlreadyGenerated = coatMask != null;
                }

                Texture2D metallicMap = GenerateMetallicSmoothnessMap(
                    baseTexture,
                    settings.MetallicValue,
                    settings.SmoothnessValue,
                    detectedPipeline,
                    occlusionSource,
                    coatMask);

                if (metallicMap != null)
                {
                    string metallicPath = SaveGeneratedTexture(metallicMap, materialFolder, materialName, detectedPipeline == RenderPipeline.HDRP ? "Mask" : "Metallic");
                    if (!string.IsNullOrEmpty(metallicPath))
                    {
                        AssignTextureToMaterial(material, metallicPath, metallicProps);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Metallic/Smoothness Map for '{materialName}' (Format: {detectedPipeline})");
                    }
                }

                // Save coat mask as separate texture for _CoatMaskMap
                if (detectedPipeline == RenderPipeline.HDRP && coatMask != null)
                {
                    string coatPath = SaveGeneratedTexture(coatMask, materialFolder, materialName, "CoatMask");
                    if (!string.IsNullOrEmpty(coatPath))
                    {
                        AssignTextureToMaterial(material, coatPath, coatProps);
                        if (material.HasProperty("_CoatMask"))
                            material.SetFloat("_CoatMask", 1f);
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Coat Mask for '{materialName}'");
                    }
                }
            }

            // Emission Map
            if (settings.GenerateEmissionMap && !HasTexture(material, emissionProps))
            {
                Texture2D emissionMap = GenerateEmissionMap(baseTexture, settings.EmissionColor, settings.EmissionCoverage, settings.EmissionMaskOnly, settings.EmissionBinaryMask, settings.EmissionAlgorithm, settings.EmissionFeather);
                if (emissionMap != null)
                {
                    string emissionPath = SaveGeneratedTexture(emissionMap, materialFolder, materialName, "Emission");
                    if (!string.IsNullOrEmpty(emissionPath))
                    {
                        AssignTextureToMaterial(material, emissionPath, emissionProps);
                        if (material.HasProperty("_EmissionColor"))
                            material.SetColor("_EmissionColor", settings.EmissionColor);
                        if (material.HasProperty("_EmissiveColor"))
                            material.SetColor("_EmissiveColor", settings.EmissionColor);
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Shader Converter] Generated Emission Map for '{materialName}'");
                    }
                }
            }

            if (anyGenerated)
            {
                EditorUtility.SetDirty(material);
            }

            // HDRP-only: generate coat mask as separate texture
            if (detectedPipeline == RenderPipeline.HDRP && settings.GenerateCoatMask && !coatAlreadyGenerated)
            {
                // Check if coat mask already assigned
                if (!HasTexture(material, coatProps))
                {
                    Texture2D coatMask = GenerateCoatMask(baseTexture, settings.CoatCoverage, settings.CoatFeather);
                    if (coatMask != null)
                    {
                        string coatPath = SaveGeneratedTexture(coatMask, materialFolder, materialName, "CoatMask");
                        if (!string.IsNullOrEmpty(coatPath))
                        {
                            AssignTextureToMaterial(material, coatPath, coatProps);
                            if (material.HasProperty("_CoatMask"))
                                material.SetFloat("_CoatMask", 1f);
                            anyGenerated = true;
                            if (logMessages)
                                Debug.Log($"[Shader Converter] Generated Coat Mask for '{materialName}'");
                        }

                        // Also update the Mask Map B channel if it exists
                        Texture2D currentMask = TryGetTexture(material, metallicProps);
                        if (currentMask != null)
                        {
                            TryAddCoatToExistingMask(material, materialFolder, materialName, currentMask, baseTexture, settings, logMessages, ref anyGenerated);
                        }
                    }
                }
            }

            return anyGenerated;
        }

        internal static bool GenerateTexturesFromBaseTexture(Texture2D baseTexture, TextureGenerationSettings settings, RenderPipeline targetPipeline, bool logMessages)
        {
            if (baseTexture == null)
                return false;

            string basePath = AssetDatabase.GetAssetPath(baseTexture);
            if (string.IsNullOrEmpty(basePath))
            {
                if (logMessages)
                {
                    Debug.LogWarning("[Texture Studio] Selected texture is not an asset. Skipping generation.");
                }
                return false;
            }

            string folder = System.IO.Path.GetDirectoryName(basePath);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(basePath);

            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(baseName))
                return false;

            if (!MakeTextureReadable(baseTexture))
            {
                Debug.LogWarning($"[Texture Studio] Could not make base texture readable for '{baseName}'. Skipping texture generation.");
                return false;
            }

            bool anyGenerated = false;
            Texture2D generatedOcclusion = null;

            if (settings.GenerateNormalMap)
            {
                Texture2D normalMap = GenerateNormalMapFromTexture(baseTexture, settings.NormalMapStrength);
                if (normalMap != null)
                {
                    string normalPath = SaveGeneratedTexture(normalMap, folder, baseName, "Normal");
                    if (!string.IsNullOrEmpty(normalPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Normal Map for '{baseName}'");
                    }
                }
            }

            if (settings.GenerateHeightMap)
            {
                Texture2D heightMap = GenerateHeightMapFromTexture(baseTexture);
                if (heightMap != null)
                {
                    string heightPath = SaveGeneratedTexture(heightMap, folder, baseName, "Height");
                    if (!string.IsNullOrEmpty(heightPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Height Map for '{baseName}'");
                    }
                }
            }

            if (settings.GenerateOcclusionMap)
            {
                generatedOcclusion = GenerateOcclusionMapFromTexture(baseTexture, settings.OcclusionStrength);
                if (generatedOcclusion != null)
                {
                    string occlusionPath = SaveGeneratedTexture(generatedOcclusion, folder, baseName, "Occlusion");
                    if (!string.IsNullOrEmpty(occlusionPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Occlusion Map for '{baseName}'");
                    }
                }
            }

            if (targetPipeline == RenderPipeline.HDRP && settings.GenerateBentNormalMap)
            {
                Texture2D bentNormal = GenerateBentNormalMap(baseTexture, generatedOcclusion, settings.BentNormalStrength);
                if (bentNormal != null)
                {
                    string bentPath = SaveGeneratedTexture(bentNormal, folder, baseName, "BentNormal");
                    if (!string.IsNullOrEmpty(bentPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Bent Normal Map for '{baseName}'");
                    }
                }
            }

            if (targetPipeline == RenderPipeline.HDRP && settings.GenerateDetailMap)
            {
                Texture2D detailMap = GenerateDetailMap(baseTexture, settings.DetailStrength);
                if (detailMap != null)
                {
                    string detailPath = SaveGeneratedTexture(detailMap, folder, baseName, "Detail");
                    if (!string.IsNullOrEmpty(detailPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Detail Map for '{baseName}'");
                    }
                }
            }

            Texture2D coatMask = null;
            bool coatSaved = false;
            if (targetPipeline == RenderPipeline.HDRP && settings.GenerateCoatMask && settings.GenerateMetallicMap)
            {
                coatMask = GenerateCoatMask(baseTexture, settings.CoatCoverage, settings.CoatFeather);
            }

            if (settings.GenerateMetallicMap)
            {
                Texture2D metallicMap = GenerateMetallicSmoothnessMap(
                    baseTexture,
                    settings.MetallicValue,
                    settings.SmoothnessValue,
                    targetPipeline,
                    generatedOcclusion,
                    coatMask);

                if (metallicMap != null)
                {
                    string suffix = targetPipeline == RenderPipeline.HDRP ? "Mask" : "Metallic";
                    string metallicPath = SaveGeneratedTexture(metallicMap, folder, baseName, suffix);
                    if (!string.IsNullOrEmpty(metallicPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated {suffix} Map for '{baseName}'");
                    }
                }

                if (targetPipeline == RenderPipeline.HDRP && coatMask != null)
                {
                    string coatPath = SaveGeneratedTexture(coatMask, folder, baseName, "CoatMask");
                    if (!string.IsNullOrEmpty(coatPath))
                    {
                        coatSaved = true;
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Coat Mask for '{baseName}'");
                    }
                }
            }

            if (targetPipeline == RenderPipeline.HDRP && settings.GenerateCoatMask && !coatSaved && !settings.GenerateMetallicMap)
            {
                if (coatMask == null)
                {
                    coatMask = GenerateCoatMask(baseTexture, settings.CoatCoverage, settings.CoatFeather);
                }

                if (coatMask != null)
                {
                    string coatPath = SaveGeneratedTexture(coatMask, folder, baseName, "CoatMask");
                    if (!string.IsNullOrEmpty(coatPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Coat Mask for '{baseName}'");
                    }
                }
            }

            if (settings.GenerateEmissionMap)
            {
                Texture2D emissionMap = GenerateEmissionMap(baseTexture, settings.EmissionColor, settings.EmissionCoverage, settings.EmissionMaskOnly, settings.EmissionBinaryMask, settings.EmissionAlgorithm, settings.EmissionFeather);
                if (emissionMap != null)
                {
                    string emissionPath = SaveGeneratedTexture(emissionMap, folder, baseName, "Emission");
                    if (!string.IsNullOrEmpty(emissionPath))
                    {
                        anyGenerated = true;
                        if (logMessages)
                            Debug.Log($"[Texture Studio] Generated Emission Map for '{baseName}'");
                    }
                }
            }

            return anyGenerated;
        }

        internal static Texture2D GetBaseTexture(Material material)
        {
            var baseTextureProperties = GetPropertyNamesForSlot(material.shader, TextureSlot.Base, "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap");

            foreach (string propName in baseTextureProperties)
            {
                if (!material.HasProperty(propName))
                    continue;

                Texture tex = material.GetTexture(propName);
                if (tex is Texture2D tex2D)
                {
                    return tex2D;
                }
            }

            // Fallback to material main texture if none of the mapped names matched
            if (material.mainTexture is Texture2D mainTex2D)
                return mainTex2D;
            
            return null;
        }

        internal static bool SetBaseTexture(Material material, Texture2D texture)
        {
            if (material == null)
                return false;

            var baseTextureProperties = GetPropertyNamesForSlot(material.shader, TextureSlot.Base, "_BaseMap", "_MainTex", "_BaseColorMap", "_BaseColor_Map", "_AlbedoMap");
            bool assigned = false;
            foreach (string propName in baseTextureProperties)
            {
                if (material.HasProperty(propName))
                {
                    material.SetTexture(propName, texture);
                    assigned = true;
                    break; // Stop after first successful assignment
                }
            }

            return assigned;
        }

        internal static Texture2D TryGetTexture(Material material, params string[] propertyNames)
        {
            return TryGetTexture(material, (IEnumerable<string>)propertyNames);
        }

        internal static Texture2D TryGetTexture(Material material, IEnumerable<string> propertyNames)
        {
            if (propertyNames == null)
                return null;

            foreach (string propName in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(propName))
                    continue;
                if (material.HasProperty(propName))
                {
                    Texture tex = material.GetTexture(propName);
                    if (tex is Texture2D tex2D)
                        return tex2D;
                }
            }
            return null;
        }

        internal static bool HasTexture(Material material, params string[] propertyNames)
        {
            return HasTexture(material, (IEnumerable<string>)propertyNames);
        }

        internal static bool HasTexture(Material material, IEnumerable<string> propertyNames)
        {
            if (propertyNames == null)
                return false;

            foreach (string propName in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(propName))
                    continue;
                if (material.HasProperty(propName))
                {
                    Texture tex = material.GetTexture(propName);
                    if (tex != null)
                        return true;
                }
            }
            return false;
        }

        internal static bool MakeTextureReadable(Texture2D texture)
        {
            if (texture == null)
                return false;

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                return true;
            }
            
            return importer != null;
        }

        private static Texture2D EnsureReadable(Texture2D texture)
        {
            if (texture == null)
                return null;

            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
                return texture;

            if (!MakeTextureReadable(texture))
                return texture;

            Texture2D reloaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return reloaded != null ? reloaded : texture;
        }

        private static Texture2D GenerateNormalMapFromTexture(Texture2D source, int strength)
        {
            int width = source.width;
            int height = source.height;
            
            Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGBA32, true);
            
            float strengthMultiplier = strength / 10f;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sample neighboring pixels for height
                    float left = source.GetPixel((x - 1 + width) % width, y).grayscale;
                    float right = source.GetPixel((x + 1) % width, y).grayscale;
                    float up = source.GetPixel(x, (y + 1) % height).grayscale;
                    float down = source.GetPixel(x, (y - 1 + height) % height).grayscale;
                    
                    // Calculate gradients
                    float dx = (right - left) * strengthMultiplier;
                    float dy = (up - down) * strengthMultiplier;
                    
                    // Normal from gradients
                    Vector3 normal = new Vector3(-dx, -dy, 1.0f).normalized;
                    
                    // Convert to 0-1 range and store in texture
                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1.0f
                    );
                    
                    normalMap.SetPixel(x, y, normalColor);
                }
            }
            
            normalMap.Apply();
            return normalMap;
        }

        private static Texture2D GenerateBentNormalMap(Texture2D source, Texture2D occlusion, int strength)
        {
            source = EnsureReadable(source);
            if (occlusion != null) occlusion = EnsureReadable(occlusion);

            int width = source.width;
            int height = source.height;

            Texture2D bentNormal = new Texture2D(width, height, TextureFormat.RGBA32, true);
            float strengthMultiplier = strength / 10f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float left = source.GetPixel((x - 1 + width) % width, y).grayscale;
                    float right = source.GetPixel((x + 1) % width, y).grayscale;
                    float up = source.GetPixel(x, (y + 1) % height).grayscale;
                    float down = source.GetPixel(x, (y - 1 + height) % height).grayscale;

                    float dx = (right - left) * strengthMultiplier;
                    float dy = (up - down) * strengthMultiplier;

                    Vector3 normal = new Vector3(-dx, -dy, 1.0f).normalized;

                    float occl = occlusion != null ? occlusion.GetPixel(x, y).grayscale : 1.0f;
                    float bendAmount = (1.0f - occl) * 0.6f;
                    Vector3 bent = Vector3.Lerp(normal, Vector3.forward, bendAmount).normalized;

                    Color bentColor = new Color(
                        bent.x * 0.5f + 0.5f,
                        bent.y * 0.5f + 0.5f,
                        bent.z * 0.5f + 0.5f,
                        1.0f
                    );

                    bentNormal.SetPixel(x, y, bentColor);
                }
            }

            bentNormal.Apply();
            return bentNormal;
        }

        private static Texture2D GenerateMetallicSmoothnessMap(Texture2D source, float metallicValue, float smoothnessValue, RenderPipeline targetPipeline, Texture2D occlusionSource = null, Texture2D coatMask = null)
        {
            source = EnsureReadable(source);
            if (occlusionSource != null) occlusionSource = EnsureReadable(occlusionSource);
            if (coatMask != null) coatMask = EnsureReadable(coatMask);

            int width = source.width;
            int height = source.height;
            
            Texture2D metallicMap = new Texture2D(width, height, TextureFormat.RGBA32, true);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float occlusion = occlusionSource != null ? occlusionSource.GetPixel(x, y).grayscale : source.GetPixel(x, y).grayscale;
                    float coat = coatMask != null ? coatMask.GetPixel(x, y).grayscale : 0.0f;

                    if (targetPipeline == RenderPipeline.HDRP)
                    {
                        // HDRP Mask Map format: R=Metallic, G=AO, B=Detail, A=Smoothness
                        metallicMap.SetPixel(x, y, new Color(metallicValue, occlusion, coat, smoothnessValue));
                    }
                    else
                    {
                        // URP/Built-in format: Grayscale Metallic in RGB, Smoothness in Alpha
                        metallicMap.SetPixel(x, y, new Color(metallicValue, metallicValue, metallicValue, smoothnessValue));
                    }
                }
            }
            
            metallicMap.Apply();
            return metallicMap;
        }
        
        /// <summary>
        /// Detects which render pipeline a shader belongs to based on its name.
        /// </summary>
        internal static RenderPipeline DetectPipelineFromShader(Shader shader)
        {
            if (shader == null)
                return RenderPipeline.BuiltIn;
            
            string shaderName = shader.name.ToLower();
            
            if (shaderName.Contains("hdrp") || shaderName.Contains("high definition"))
                return RenderPipeline.HDRP;
            
            if (shaderName.Contains("universal") || shaderName.Contains("urp"))
                return RenderPipeline.URP;
            
            return RenderPipeline.BuiltIn;
        }

        private static Texture2D GenerateHeightMapFromTexture(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            
            Texture2D heightMap = new Texture2D(width, height, TextureFormat.R8, true);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float luminance = source.GetPixel(x, y).grayscale;
                    heightMap.SetPixel(x, y, new Color(luminance, luminance, luminance, 1.0f));
                }
            }
            
            heightMap.Apply();
            return heightMap;
        }

        private static Texture2D GenerateOcclusionMapFromTexture(Texture2D source, float strength)
        {
            int width = source.width;
            int height = source.height;
            
            Texture2D occlusionMap = new Texture2D(width, height, TextureFormat.R8, true);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float luminance = source.GetPixel(x, y).grayscale;
                    // Darken based on original luminance and strength
                    float occlusion = Mathf.Lerp(1.0f, luminance, strength);
                    occlusionMap.SetPixel(x, y, new Color(occlusion, occlusion, occlusion, 1.0f));
                }
            }
            
            occlusionMap.Apply();
            return occlusionMap;
        }

        private static Texture2D GenerateDetailMap(Texture2D source, float strength)
        {
            source = EnsureReadable(source);
            int width = source.width;
            int height = source.height;

            // Use a Difference-of-Gaussians style detector to isolate high-frequency detail
            // while suppressing broad gradients. This avoids over-detailing flat areas.
            int smallRadius = 1;
            int largeRadius = 3;
            float intensity = Mathf.Max(0.05f, strength); // user strength scales contrast

            Texture2D detailMap = new Texture2D(width, height, TextureFormat.RGBA32, true);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float meanSmall = SampleMeanLuminance(source, x, y, smallRadius);
                    float meanLarge = SampleMeanLuminance(source, x, y, largeRadius);

                    // High-frequency signal
                    float dog = meanSmall - meanLarge;
                    float detail = Mathf.Abs(dog);

                    // Suppress tiny noise and boost meaningful edges
                    detail = Mathf.Max(0f, detail - 0.01f); // noise gate
                    detail = Mathf.Pow(Mathf.Clamp01(detail * 6f * intensity), 0.8f);

                    detailMap.SetPixel(x, y, new Color(detail, detail, detail, detail));
                }
            }

            detailMap.Apply();
            return detailMap;
        }

        private static float SampleMeanLuminance(Texture2D tex, int x, int y, int radius)
        {
            int w = tex.width;
            int h = tex.height;
            float sum = 0f;
            int count = 0;

            for (int ky = -radius; ky <= radius; ky++)
            {
                int sy = (y + ky + h) % h;
                for (int kx = -radius; kx <= radius; kx++)
                {
                    int sx = (x + kx + w) % w;
                    sum += tex.GetPixel(sx, sy).grayscale;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        private static Texture2D GenerateEmissionMap(Texture2D source, Color userColor, float coverage, bool maskOnly, bool binaryMask, EmissionAlgorithm algorithm, int featherRadius)
        {
            source = EnsureReadable(source);
            int width = source.width;
            int height = source.height;
            coverage = Mathf.Clamp01(coverage);
            Texture2D emissionMap = new Texture2D(width, height, TextureFormat.RGBA32, true);

            float[] mask = new float[width * height];
            Color[] pixels = source.GetPixels();
            int totalPixels = pixels.Length;

            if (algorithm == EmissionAlgorithm.HighChroma)
            {
                // --- Old High Chroma / Luminance Algorithm ---
                const int bins = 256;
                int[] chromaHistogram = new int[bins];
                int[] luminanceHistogram = new int[bins];

                for (int i = 0; i < totalPixels; i++)
                {
                    Color c = pixels[i];
                    float chroma = GetChroma(c);
                    float luminance = c.grayscale;
                    int chromaBin = Mathf.Clamp(Mathf.RoundToInt(chroma * (bins - 1)), 0, bins - 1);
                    int lumBin = Mathf.Clamp(Mathf.RoundToInt(luminance * (bins - 1)), 0, bins - 1);
                    chromaHistogram[chromaBin]++;
                    luminanceHistogram[lumBin]++;
                }

                float chromaCutoff = FindChromaCutoff(chromaHistogram, coverage);
                float luminanceCutoff = FindLuminanceCutoff(luminanceHistogram, coverage);

                for (int i = 0; i < totalPixels; i++)
                {
                    Color c = pixels[i];
                    float chroma = GetChroma(c);
                    float luminance = c.grayscale;
                    if (chroma >= chromaCutoff || luminance >= luminanceCutoff)
                    {
                        mask[i] = luminance;
                    }
                    else
                    {
                        mask[i] = 0f;
                    }
                }
            }
            else
            {
                // --- New Background Deviation Algorithm ---
                // 1. Calculate Global Mean (Dominant Background Color)
                Color globalMean = Color.black;
                foreach (Color c in pixels)
                {
                    globalMean += c;
                }
                if (totalPixels > 0) globalMean /= totalPixels;

                // 2. Build Histogram of "Emission Score" (Deviation from Mean * Luminance)
                // This identifies pixels that are both distinct from the background AND bright.
                const int bins = 256;
                int[] scoreHistogram = new int[bins];
                float[] scores = new float[totalPixels];
                float maxScore = 0f;

                for (int i = 0; i < totalPixels; i++)
                {
                    float d = ColorDistance(pixels[i], globalMean);
                    float lum = pixels[i].grayscale;
                    // Score combines distinctness from background with raw brightness.
                    float score = d * lum; 
                    
                    scores[i] = score;
                    if (score > maxScore) maxScore = score;
                }

                // Normalize scores to 0-1 for histogram
                if (maxScore < 0.0001f) maxScore = 1f;

                for (int i = 0; i < totalPixels; i++)
                {
                    int bin = Mathf.Clamp(Mathf.RoundToInt((scores[i] / maxScore) * (bins - 1)), 0, bins - 1);
                    scoreHistogram[bin]++;
                }

                // 3. Find Cutoff based on coverage
                float cutoffNormalized = FindLuminanceCutoff(scoreHistogram, coverage);
                float scoreCutoff = cutoffNormalized * maxScore;

                // 4. Build Mask
                for (int i = 0; i < totalPixels; i++)
                {
                    if (scores[i] >= scoreCutoff)
                    {
                        mask[i] = pixels[i].grayscale;
                    }
                    else
                    {
                        mask[i] = 0f;
                    }
                }
            }

            // Feather edges via simple box blur.
            int radius = Mathf.Clamp(featherRadius, 0, 16);
            if (radius > 0)
            {
                float[] blurred = new float[mask.Length];
                int kernel = radius * 2 + 1;
                float invCount = 1f / (kernel * kernel);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0f;
                        for (int ky = -radius; ky <= radius; ky++)
                        {
                            int sy = Mathf.Clamp(y + ky, 0, height - 1);
                            int row = sy * width;
                            for (int kx = -radius; kx <= radius; kx++)
                            {
                                int sx = Mathf.Clamp(x + kx, 0, width - 1);
                                sum += mask[row + sx];
                            }
                        }
                        blurred[y * width + x] = sum * invCount;
                    }
                }
                mask = blurred;
            }

            if (binaryMask)
            {
                for (int i = 0; i < mask.Length; i++)
                {
                    mask[i] = mask[i] > 0.1f ? 1f : 0f;
                }
            }

            // Determine Tint Color
            bool userProvided = userColor.maxColorComponent > 0.001f;
            Color tint = userProvided ? userColor : Color.white;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float m = mask[y * width + x];
                    if (m > 0f)
                    {
                        if (maskOnly)
                        {
                            Color maskColor = new Color(m, m, m, 1f);
                            emissionMap.SetPixel(x, y, maskColor);
                        }
                        else
                        {
                            if (userProvided)
                            {
                                emissionMap.SetPixel(x, y, tint * m);
                            }
                            else
                            {
                                emissionMap.SetPixel(x, y, pixels[y * width + x] * m);
                            }
                        }
                    }
                    else
                    {
                        emissionMap.SetPixel(x, y, Color.black);
                    }
                }
            }

            emissionMap.Apply();
            return emissionMap;
        }

        private static float GetChroma(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max - min;
        }

        private static float FindChromaCutoff(int[] histogram, float coverage)
        {
            int total = 0;
            foreach (int count in histogram)
                total += count;

            float target = Mathf.Max(coverage, 0.001f) * total;
            int accumulated = 0;

            for (int i = histogram.Length - 1; i >= 0; i--)
            {
                accumulated += histogram[i];
                if (accumulated >= target)
                {
                    return i / (float)(histogram.Length - 1);
                }
            }

            return 0.0f;
        }

        private static float FindLuminanceCutoff(int[] histogram, float coverage)
        {
            int total = 0;
            foreach (int count in histogram)
                total += count;

            float target = Mathf.Max(coverage, 0.001f) * total;
            int accumulated = 0;

            for (int i = histogram.Length - 1; i >= 0; i--)
            {
                accumulated += histogram[i];
                if (accumulated >= target)
                {
                    return i / (float)(histogram.Length - 1);
                }
            }

            return 0.0f;
        }

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        private static Texture2D GenerateCoatMask(Texture2D baseTexture, float coverage, int featherRadius)
        {
            int width = baseTexture.width;
            int height = baseTexture.height;
            coverage = Mathf.Clamp01(coverage);

            // Use luminance histogram to pick brightest/most reflective areas
            const int bins = 256;
            int[] luminanceHistogram = new int[bins];
            Color[] pixels = baseTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                int bin = Mathf.Clamp(Mathf.RoundToInt(pixels[i].grayscale * (bins - 1)), 0, bins - 1);
                luminanceHistogram[bin]++;
            }

            float lumCutoff = FindLuminanceCutoff(luminanceHistogram, coverage);

            Texture2D mask = new Texture2D(width, height, TextureFormat.R8, true);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float lum = baseTexture.GetPixel(x, y).grayscale;
                    float v = lum >= lumCutoff ? lum : 0f;
                    mask.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            mask.Apply();

            if (featherRadius > 0)
            {
                mask = FeatherMask(mask, featherRadius);
            }

            return mask;
        }

        private static Texture2D FeatherMask(Texture2D source, int radius)
        {
            int width = source.width;
            int height = source.height;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] src = source.GetPixels();
            Color[] dst = new Color[src.Length];

            int k = radius * 2 + 1;
            float inv = 1f / (k * k);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0f;
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        int row = sy * width;
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int sx = Mathf.Clamp(x + kx, 0, width - 1);
                            sum += src[row + sx].r;
                        }
                    }
                    float v = sum * inv;
                    dst[y * width + x] = new Color(v, v, v, 1f);
                }
            }

            result.SetPixels(dst);
            result.Apply();
            return result;
        }

        private static bool HasCoatInMask(Texture2D mask)
        {
            if (mask == null)
                return false;

            int width = mask.width;
            int height = mask.height;
            int stepX = Mathf.Max(1, width / 16);
            int stepY = Mathf.Max(1, height / 16);

            for (int y = 0; y < height; y += stepY)
            {
                for (int x = 0; x < width; x += stepX)
                {
                    if (mask.GetPixel(x, y).b > 0.02f)
                        return true;
                }
            }

            return false;
        }

        private static void TryAddCoatToExistingMask(Material material, string materialFolder, string materialName, Texture2D existingMaskMap, Texture2D baseTexture, TextureGenerationSettings settings, bool logMessages, ref bool anyGenerated)
        {
            if (existingMaskMap == null)
                return;

            existingMaskMap = EnsureReadable(existingMaskMap);
            if (existingMaskMap == null)
                return;

            if (settings.PreserveExistingCoat && HasCoatInMask(existingMaskMap))
                return;

            Texture2D coatMask = GenerateCoatMask(baseTexture, settings.CoatCoverage, settings.CoatFeather);
            if (coatMask == null)
                return;

            int width = existingMaskMap.width;
            int height = existingMaskMap.height;
            Texture2D updated = new Texture2D(width, height, TextureFormat.RGBA32, true);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color src = existingMaskMap.GetPixel(x, y);
                    float coat = coatMask.GetPixel(x, y).grayscale;
                    updated.SetPixel(x, y, new Color(src.r, src.g, coat, src.a));
                }
            }
            updated.Apply();

            string maskPath = SaveGeneratedTexture(updated, materialFolder, materialName, "Mask_Coat");
            if (!string.IsNullOrEmpty(maskPath))
            {
                AssignTextureToMaterial(material, maskPath, "_MaskMap", "_MetallicGlossMap");
                anyGenerated = true;
                if (logMessages)
                    Debug.Log($"[Shader Converter] Updated Mask Map with coat channel for '{materialName}'");
            }
        }

        private static string SaveGeneratedTexture(Texture2D texture, string folder, string materialName, string suffix)
        {
            if (texture == null || string.IsNullOrEmpty(folder))
                return null;

            // Generate unique filename
            string fileName = $"{materialName}_{suffix}_Generated.png";
            string fullPath = System.IO.Path.Combine(folder, fileName);
            if (TextureStudioProjectSettings.CreateTextureBackupsEnabled)
            {
                int counter = 1;
                while (System.IO.File.Exists(fullPath))
                {
                    fileName = $"{materialName}_{suffix}_Generated_{counter}.png";
                    fullPath = System.IO.Path.Combine(folder, fileName);
                    counter++;
                }
            }
            
            // Convert to relative Unity path
            string assetPath = fullPath.Replace("\\", "/");
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            
            // Encode and save
            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(fullPath, bytes);
            
            // We do NOT call AssetDatabase.ImportAsset here synchronously if we can avoid it.
            // However, we need to configure the importer immediately to return a valid path for assignment.
            // To prevent freezing, we use ImportAssetOptions.ForceSynchronousImport only when necessary.
            
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            
            // Configure texture import settings based on type
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (suffix == "Normal")
                {
                    if (importer.textureType != TextureImporterType.NormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        changed = true;
                    }
                }
                else
                {
                    if (importer.textureType != TextureImporterType.Default)
                    {
                        importer.textureType = TextureImporterType.Default;
                        changed = true;
                    }
                    bool isSRGB = (suffix == "Emission" || suffix == "Base" || suffix == "Albedo");
                    if (importer.sRGBTexture != isSRGB)
                    {
                        importer.sRGBTexture = isSRGB;
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
            
            return assetPath;
        }

        private static void AssignTextureToMaterial(Material material, string texturePath, params string[] propertyNames)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                return;

            foreach (string propName in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(propName))
                    continue;
                if (material.HasProperty(propName))
                {
                    material.SetTexture(propName, texture);
                    // Continue to assign to all matching properties
                }
            }
        }

        #endregion
    }
}
