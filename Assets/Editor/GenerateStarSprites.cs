using UnityEngine;
using UnityEditor;
using System.IO;

namespace RuralEarthquake.Editor
{
    /// <summary>
    /// One-shot editor utility that generates StarFilled.png and StarEmpty.png
    /// placeholder sprites under Assets/UI/Badges/. Runs automatically on first compile
    /// if the files don't already exist. Safe to delete after sprites are generated.
    /// </summary>
    [InitializeOnLoad]
    public static class GenerateStarSprites
    {
        private const string OutputFolder = "Assets/UI/Badges";
        private const int TextureSize = 128;

        static GenerateStarSprites()
        {
            // Guard: only generate if files don't already exist
            if (!File.Exists(OutputFolder + "/StarFilled.png") ||
                !File.Exists(OutputFolder + "/StarEmpty.png"))
            {
                EditorApplication.delayCall += Generate;
            }
        }

        [MenuItem("Tools/RuralEarthquake/Generate Star Sprites")]
        public static void Generate()
        {
            Directory.CreateDirectory(Application.dataPath + "/../" + OutputFolder);

            CreateStarTexture(OutputFolder + "/StarFilled.png", filled: true);
            CreateStarTexture(OutputFolder + "/StarEmpty.png", filled: false);

            AssetDatabase.Refresh();

            SetSpriteImportMode(OutputFolder + "/StarFilled.png");
            SetSpriteImportMode(OutputFolder + "/StarEmpty.png");

            AssetDatabase.SaveAssets();
            Debug.Log("[GenerateStarSprites] StarFilled.png and StarEmpty.png created in " + OutputFolder);
        }

        private static void CreateStarTexture(string relativePath, bool filled)
        {
            int size = TextureSize;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color fillColor   = filled ? new Color(1f, 0.85f, 0.1f, 1f)  : new Color(0.4f, 0.4f, 0.4f, 1f);
            Color borderColor = filled ? new Color(0.8f, 0.6f, 0f, 1f)   : new Color(0.6f, 0.6f, 0.6f, 1f);
            Color clear       = new Color(0, 0, 0, 0);

            float cx = size / 2f;
            float cy = size / 2f;

            // Draw a 5-pointed star using point-in-polygon test
            Vector2[] starPoints = BuildStarPolygon(cx, cy, size * 0.46f, size * 0.20f, 5);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (PointInPolygon(p, starPoints))
                    {
                        // Border: within 3px of edge
                        bool nearEdge = IsNearEdge(p, starPoints, 3f);
                        tex.SetPixel(x, y, nearEdge ? borderColor : fillColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string absolutePath = Application.dataPath + "/../" + relativePath;
            File.WriteAllBytes(absolutePath, bytes);
        }

        private static Vector2[] BuildStarPolygon(float cx, float cy, float outerR, float innerR, int points)
        {
            int total = points * 2;
            Vector2[] verts = new Vector2[total];
            for (int i = 0; i < total; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts[i] = new Vector2(cx + r * Mathf.Cos(angle), cy + r * Mathf.Sin(angle));
            }
            return verts;
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = polygon[i].x, yi = polygon[i].y;
                float xj = polygon[j].x, yj = polygon[j].y;
                bool intersect = ((yi > p.y) != (yj > p.y)) &&
                                 (p.x < (xj - xi) * (p.y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        private static bool IsNearEdge(Vector2 p, Vector2[] polygon, float threshold)
        {
            int n = polygon.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float dist = DistanceToSegment(p, polygon[j], polygon[i]);
                if (dist < threshold) return true;
            }
            return false;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + t * ab)).magnitude;
        }

        private static void SetSpriteImportMode(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
