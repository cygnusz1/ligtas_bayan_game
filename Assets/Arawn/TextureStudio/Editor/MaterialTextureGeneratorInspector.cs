using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arawn.TextureStudio.Editor
{
    /// <summary>
    /// Adds a lightweight helper to the Material inspector that can generate missing maps
    /// (normal, height, metallic/mask, occlusion, emission) in-place using the base texture.
    /// </summary>
    [CustomEditor(typeof(Material)), CanEditMultipleObjects]
    public class MaterialTextureGeneratorInspector : MaterialEditor
    {
        private bool _showGenerator = true;
        private UniversalShaderConverter.TextureGenerationSettings _settings = UniversalShaderConverter.TextureGenerationSettings.CreateDefault();

        // Hue tool state
        private bool _showHueTool = true;
        private float _hueShift = 0f;
        private Texture2D _huePreview;
        private Texture2D _hueOriginal;
        private bool _hueDirty;
        private int _hueFeatherPreview = 0;
        private bool _hueMaskOnly;
        private float _lastHueShift;
        private bool _lastHueMaskOnly;
        private int _lastHueFeather;
        private bool _canRestoreOriginal;
        private Material _trackedMaterial;
        private bool _occlusionHDRPInitDone;
        private bool _heightHDRPInitDone;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Keep this helper simple when multiple materials are selected to avoid confusing outcomes.
            if (targets == null || targets.Length != 1)
                return;

            var material = target as Material;
            if (material == null)
                return;

            if (_trackedMaterial != material)
            {
                ResetHueState(material);
                _trackedMaterial = material;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showGenerator = EditorGUILayout.Foldout(_showGenerator, "Texture Generator", true);

            if (_showGenerator)
            {
                DrawGeneratorUI(material);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                _showHueTool = EditorGUILayout.Foldout(_showHueTool, "Hue Adjust (Base Map)", true);
                if (_showHueTool)
                {
                    DrawHueTool(material);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (_hueDirty && _hueOriginal != null && target is Material mat)
            {
                UniversalShaderConverter.SetBaseTexture(mat, _hueOriginal);
            }
            _canRestoreOriginal = false;
        }

        private void ResetHueState(Material material)
        {
            _hueOriginal = UniversalShaderConverter.GetBaseTexture(material);
            _huePreview = null;
            _hueDirty = false;
            _canRestoreOriginal = false;
            _hueShift = 0f;
            _hueMaskOnly = false;
            _hueFeatherPreview = 0;
            _lastHueShift = 0f;
            _lastHueMaskOnly = _hueMaskOnly;
            _lastHueFeather = _hueFeatherPreview;
        }

        private void DrawGeneratorUI(Material material)
        {
            Texture2D baseTexture = UniversalShaderConverter.GetBaseTexture(material);
            if (baseTexture == null)
            {
                EditorGUILayout.HelpBox("No base/albedo texture found. Assign a base map to enable one-click generation.", MessageType.Info);
                return;
            }

            var pipeline = UniversalShaderConverter.DetectPipelineFromShader(material.shader);
            bool isHDRP = pipeline == UniversalShaderConverter.RenderPipeline.HDRP;
            EditorGUILayout.LabelField("Detected Pipeline", pipeline.ToString());

            var normalProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.Normal, "_BumpMap", "_NormalMap");
            var heightProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.Height, "_ParallaxMap", "_HeightMap");
            var metallicProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.MetallicMask, "_MetallicGlossMap", "_MaskMap");
            var occlusionProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.Occlusion, "_OcclusionMap");
            var emissionProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.Emission, "_EmissionMap", "_EmissiveColorMap");

            bool missingNormal = !UniversalShaderConverter.HasTexture(material, normalProps);
            bool missingHeight = !UniversalShaderConverter.HasTexture(material, heightProps);
            bool missingMetallicOrMask = !UniversalShaderConverter.HasTexture(material, metallicProps);
            bool missingOcclusion = !UniversalShaderConverter.HasTexture(material, occlusionProps);
            bool missingEmission = !UniversalShaderConverter.HasTexture(material, emissionProps);

            var bentNormalProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.BentNormal, "_BentNormalMap");
            var detailProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.Detail, "_DetailMap");
            var coatProps = UniversalShaderConverter.GetPropertyNamesForSlot(material.shader, TextureSlot.CoatMask, "_CoatMaskMap");

            bool missingBentNormal = isHDRP && !UniversalShaderConverter.HasTexture(material, bentNormalProps);
            bool missingDetail = isHDRP && !UniversalShaderConverter.HasTexture(material, detailProps);
            bool missingCoat = isHDRP && !UniversalShaderConverter.HasTexture(material, coatProps);

            bool hasAnyMissing = missingNormal || missingHeight || missingMetallicOrMask || missingOcclusion || missingEmission;
            if (isHDRP)
                hasAnyMissing |= missingBentNormal || missingDetail || missingCoat;

            if (!hasAnyMissing && !isHDRP)
            {
                EditorGUILayout.HelpBox("All supported maps are already assigned.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox("Generate and assign missing maps, or add HDRP coat to an existing mask.", MessageType.Info);

            DrawToggleWithState(ref _settings.GenerateNormalMap, "Normal Map", missingNormal);
            if (_settings.GenerateNormalMap && missingNormal)
            {
                _settings.NormalMapStrength = EditorGUILayout.IntSlider("Strength", _settings.NormalMapStrength, 1, 10);
            }

            if (isHDRP && missingHeight && !_heightHDRPInitDone)
            {
                _settings.GenerateHeightMap = false; // default off for HDRP when missing
                _heightHDRPInitDone = true;
            }
            if (isHDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateHeightMap, "Height/Parallax Map (optional)");
            }
            else
            {
                DrawToggleWithState(ref _settings.GenerateHeightMap, "Height/Parallax Map", missingHeight);
            }

            if (_settings.GenerateHeightMap && missingHeight)
            {
                _settings.HeightScale = EditorGUILayout.Slider("Height Scale", _settings.HeightScale, 0.01f, 0.2f);
            }

            if (isHDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateOcclusionMap, "Occlusion Map (optional)");
            }
            else
            {
                DrawToggleWithState(ref _settings.GenerateOcclusionMap, "Occlusion Map", missingOcclusion);
            }

            if (_settings.GenerateOcclusionMap && missingOcclusion)
            {
                _settings.OcclusionStrength = EditorGUILayout.Slider("AO Strength", _settings.OcclusionStrength, 0f, 1f);
            }

            string metallicLabel = pipeline == UniversalShaderConverter.RenderPipeline.HDRP ? "Mask Map (Metallic/AO/Smoothness)" : "Metallic/Smoothness Map";
            DrawToggleWithState(ref _settings.GenerateMetallicMap, metallicLabel, missingMetallicOrMask);
            if (_settings.GenerateMetallicMap && missingMetallicOrMask)
            {
                _settings.MetallicValue = EditorGUILayout.Slider("Metallic", _settings.MetallicValue, 0f, 1f);
                _settings.SmoothnessValue = EditorGUILayout.Slider("Smoothness", _settings.SmoothnessValue, 0f, 1f);
            }

            if (pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
            {
                if (missingOcclusion && !_occlusionHDRPInitDone)
                {
                    _settings.GenerateOcclusionMap = false; // default off for HDRP when missing
                    _occlusionHDRPInitDone = true;
                }

                DrawOptionalToggle(ref _settings.GenerateBentNormalMap, "Bent Normal Map (optional)");
                if (_settings.GenerateBentNormalMap && missingBentNormal)
                {
                    _settings.BentNormalStrength = EditorGUILayout.IntSlider("Bent Normal Strength", _settings.BentNormalStrength, 1, 10);
                }

                DrawOptionalToggle(ref _settings.GenerateCoatMask, "Coat Mask (optional)");
                if (_settings.GenerateCoatMask && missingCoat)
                {
                    _settings.CoatCoverage = EditorGUILayout.Slider("Coat Coverage", _settings.CoatCoverage, 0.05f, 0.6f);
                    _settings.CoatFeather = EditorGUILayout.IntSlider("Edge Feather (px)", _settings.CoatFeather, 0, 8);
                    _settings.PreserveExistingCoat = EditorGUILayout.Toggle("Preserve Existing Coat B", _settings.PreserveExistingCoat);
                }

                DrawOptionalToggle(ref _settings.GenerateDetailMap, "Detail Map (optional)");
                if (_settings.GenerateDetailMap && missingDetail)
                {
                    _settings.DetailStrength = EditorGUILayout.Slider("Detail Strength", _settings.DetailStrength, 0.2f, 2f);
                }
            }

            if (isHDRP)
            {
                DrawOptionalToggle(ref _settings.GenerateEmissionMap, "Emission Map (optional)");
            }
            else
            {
                DrawToggleWithState(ref _settings.GenerateEmissionMap, "Emission Map", missingEmission);
            }

            if (_settings.GenerateEmissionMap && missingEmission)
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
                (_settings.GenerateNormalMap && missingNormal) ||
                (_settings.GenerateHeightMap && missingHeight) ||
                (_settings.GenerateMetallicMap && missingMetallicOrMask) ||
                (_settings.GenerateOcclusionMap && missingOcclusion) ||
                (_settings.GenerateEmissionMap && missingEmission) ||
                (isHDRP && _settings.GenerateBentNormalMap && missingBentNormal) ||
                (isHDRP && _settings.GenerateDetailMap && missingDetail) ||
                (isHDRP && _settings.GenerateCoatMask && missingCoat);

            using (new EditorGUI.DisabledScope(!generationRequested))
            {
                if (GUILayout.Button("Generate Missing Maps", GUILayout.Height(26)))
                {
                    bool generated = UniversalShaderConverter.GenerateMissingTexturesForMaterial(material, _settings, true);
                    if (generated)
                    {
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        private static void DrawToggleWithState(ref bool toggleValue, string label, bool isMissing)
        {
            using (new EditorGUI.DisabledScope(!isMissing))
            {
                if (isMissing)
                {
                    toggleValue = EditorGUILayout.ToggleLeft(label + " (missing)", toggleValue);
                }
                else
                {
                    EditorGUILayout.ToggleLeft(label + " (assigned)", false);
                }
            }
        }

        private static void DrawOptionalToggle(ref bool toggleValue, string label)
        {
            toggleValue = EditorGUILayout.ToggleLeft(label, toggleValue);
        }

        private void DrawHueTool(Material material)
        {
            if (_hueOriginal == null)
            {
                _hueOriginal = UniversalShaderConverter.GetBaseTexture(material);
            }

            Texture2D baseTex = _hueOriginal;

            if (baseTex == null)
            {
                EditorGUILayout.HelpBox("No Base/Albedo texture found. Assign one to enable hue adjust.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Live preview updates as you slide; Apply writes a new texture.", EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            _hueShift = EditorGUILayout.Slider("Hue Shift", _hueShift, -180f, 180f);
            _hueMaskOnly = EditorGUILayout.Toggle("Mask Only (Grayscale)", _hueMaskOnly);
            if (_hueMaskOnly)
            {
                _hueFeatherPreview = EditorGUILayout.IntSlider("Edge Feather (px)", _hueFeatherPreview, 0, 8);
            }
            bool changed = EditorGUI.EndChangeCheck();

            if (changed && (_hueShift != _lastHueShift || _hueMaskOnly != _lastHueMaskOnly || _hueFeatherPreview != _lastHueFeather))
            {
                SafePreviewHue(material, baseTex);
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_hueDirty))
                {
                    if (GUILayout.Button("Apply", GUILayout.Height(22)))
                    {
                        SafeApplyHue(material, baseTex);
                    }
                }

                if (TextureStudioProjectSettings.CreateTextureBackupsEnabled)
                {
                    bool canRevert = _hueDirty || _canRestoreOriginal;
                    string revertLabel = _canRestoreOriginal ? "Restore Original" : "Revert";
                    using (new EditorGUI.DisabledScope(!canRevert))
                    {
                        if (GUILayout.Button(revertLabel, GUILayout.Height(22)))
                        {
                            SafeRevertHue(material);
                        }
                    }
                }
            }

            if (_huePreview != null)
            {
                Rect r = GUILayoutUtility.GetAspectRect(1f, GUILayout.Height(96));
                EditorGUI.DrawPreviewTexture(r, _huePreview, null, ScaleMode.ScaleToFit);
            }
        }

        private void PreviewHue(Material material, Texture2D baseTex)
        {
            CacheOriginalBase(material, baseTex);

            Texture2D source = _hueOriginal != null ? _hueOriginal : baseTex;

            if (!UniversalShaderConverter.MakeTextureReadable(source))
            {
                EditorGUILayout.HelpBox("Base texture is not readable; cannot preview hue.", MessageType.Warning);
                return;
            }

            if (_huePreview != null && Mathf.Approximately(_hueShift, _lastHueShift) && _hueMaskOnly == _lastHueMaskOnly && _hueFeatherPreview == _lastHueFeather)
                return;

            _huePreview = GenerateHueTexture(source, _hueShift, _hueMaskOnly, _hueFeatherPreview);
            UniversalShaderConverter.SetBaseTexture(material, _huePreview);
            _hueDirty = true;
            _lastHueShift = _hueShift;
            _lastHueMaskOnly = _hueMaskOnly;
            _lastHueFeather = _hueFeatherPreview;
            _canRestoreOriginal = true; // once previewed, original is available to restore
            Repaint();
        }

        private void SafePreviewHue(Material material, Texture2D baseTex)
        {
            try
            {
                PreviewHue(material, baseTex);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Texture Studio] Hue preview failed: {ex.Message}");
                _hueDirty = false;
            }
        }

        private void ApplyHue(Material material, Texture2D baseTex)
        {
            if (_huePreview == null)
                SafePreviewHue(material, baseTex);

            if (_huePreview == null)
                return;

            string basePath = AssetDatabase.GetAssetPath(baseTex);
            if (string.IsNullOrEmpty(basePath))
                return;

            string folder = Path.GetDirectoryName(basePath);
            string name = Path.GetFileNameWithoutExtension(basePath) + "_Hue";
            string targetPath = Path.Combine(folder, name + ".png");
            targetPath = targetPath.Replace("\\", "/");

            if (TextureStudioProjectSettings.CreateTextureBackupsEnabled)
            {
                int counter = 1;
                while (File.Exists(targetPath))
                {
                    targetPath = Path.Combine(folder, name + "_" + counter + ".png").Replace("\\", "/");
                    counter++;
                }
            }

            byte[] png = _huePreview.EncodeToPNG();
            File.WriteAllBytes(targetPath, png);
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            }

            Texture2D newAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
            if (newAsset != null)
            {
                bool success = UniversalShaderConverter.SetBaseTexture(material, newAsset);
                if (!success)
                {
                    Debug.LogWarning($"[Texture Studio] Failed to assign hue texture to material '{material.name}'. Check shader property mappings.");
                }
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
                _hueDirty = false;
                _huePreview = null;
                _lastHueShift = _hueShift;
                _lastHueMaskOnly = _hueMaskOnly;
                _lastHueFeather = _hueFeatherPreview;
                _canRestoreOriginal = true;
                GUIUtility.ExitGUI(); // Safely exit GUI frame after asset operations
            }
        }

        private void SafeApplyHue(Material material, Texture2D baseTex)
        {
            try
            {
                ApplyHue(material, baseTex);
            }
            catch (ExitGUIException)
            {
                throw; // Re-throw ExitGUIException - it's intentional
            }
            catch (System.Exception)
            {
                _hueDirty = false;
            }
        }

        private void RevertHue(Material material)
        {
            if (_hueOriginal != null)
            {
                UniversalShaderConverter.SetBaseTexture(material, _hueOriginal);
            }
            _hueShift = 0f;
            _hueDirty = false;
            _huePreview = null;
            _lastHueShift = 0f;
            _lastHueMaskOnly = _hueMaskOnly;
            _lastHueFeather = _hueFeatherPreview;
            _canRestoreOriginal = false;
            Repaint();
        }

        private void SafeRevertHue(Material material)
        {
            try
            {
                RevertHue(material);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Texture Studio] Hue revert failed: {ex.Message}");
                _hueDirty = false;
            }
        }

        private void CacheOriginalBase(Material material, Texture2D baseTex)
        {
            if (_hueOriginal == null)
            {
                _hueOriginal = baseTex;
            }
        }

        private static Texture2D GenerateHueTexture(Texture2D source, float hueShiftDegrees, bool maskOnly, int feather)
        {
            int width = source.width;
            int height = source.height;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, true);

            float shift = hueShiftDegrees / 360f;

            Color[] pixels = source.GetPixels();
            Color[] outPixels = new Color[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                Color.RGBToHSV(c, out float h, out float s, out float v);
                h = Mathf.Repeat(h + shift, 1f);
                Color shifted = Color.HSVToRGB(h, s, v);
                shifted.a = c.a;

                if (maskOnly)
                {
                    float lum = shifted.grayscale;
                    outPixels[i] = new Color(lum, lum, lum, 1f);
                }
                else
                {
                    outPixels[i] = shifted;
                }
            }

            result.SetPixels(outPixels);
            result.Apply();

            if (maskOnly && feather > 0)
            {
                result = FeatherMask(result, feather);
            }

            return result;
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
    }
}
