using UnityEditor;
using UnityEngine;

namespace Arawn.TextureStudio.Editor
{
    internal static class TextureStudioContextMenus
    {
        [MenuItem("Assets/Create PBR Maps", true)]
        private static bool ValidateCreatePbrMaps()
        {
            return Selection.GetFiltered<Texture2D>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Assets/Create PBR Maps")]
        private static void CreatePbrMaps()
        {
            Texture2D[] textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
            if (textures.Length == 0)
                return;

            var settings = TextureStudioProjectSettings.GetDefaultGenerationSettings();
            var pipeline = TextureStudioProjectSettings.GetDefaultGenerationPipeline();

            if (!HasAnyGenerationEnabled(settings, pipeline))
            {
                EditorUtility.DisplayDialog("Create PBR Maps", "No map types are enabled in Project Settings > Texture Studio.", "OK");
                return;
            }

            int generatedCount = 0;
            int skippedCount = 0;

            EditorUtility.DisplayProgressBar("Create PBR Maps", "Starting...", 0f);
            try
            {
                for (int i = 0; i < textures.Length; i++)
                {
                    Texture2D tex = textures[i];
                    if (tex == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Create PBR Maps",
                        $"Processing {tex.name} ({i + 1}/{textures.Length})",
                        (float)i / textures.Length);

                    bool generated = UniversalShaderConverter.GenerateTexturesFromBaseTexture(tex, settings, pipeline, true);
                    if (generated)
                        generatedCount++;
                    else
                        skippedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (generatedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Create PBR Maps",
                $"Processed: {generatedCount}\nSkipped: {skippedCount}",
                "OK");
        }

        private static bool HasAnyGenerationEnabled(UniversalShaderConverter.TextureGenerationSettings settings, UniversalShaderConverter.RenderPipeline pipeline)
        {
            bool any = settings.GenerateNormalMap ||
                       settings.GenerateHeightMap ||
                       settings.GenerateMetallicMap ||
                       settings.GenerateOcclusionMap ||
                       settings.GenerateEmissionMap;

            if (pipeline == UniversalShaderConverter.RenderPipeline.HDRP)
            {
                any |= settings.GenerateBentNormalMap ||
                       settings.GenerateDetailMap ||
                       settings.GenerateCoatMask;
            }

            return any;
        }
    }
}
