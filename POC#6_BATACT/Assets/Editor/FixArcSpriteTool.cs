using UnityEditor;
using UnityEngine;

public class FixArcSpriteTool {
    [MenuItem("Tools/BAT/Fix Arc Projectile Sprites")]
    public static void FixSprites() {
        string texPath = "Assets/Prefab/CircleTex.png";
        
        // Ensure texture is imported as Sprite properly
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
        if (importer != null) {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        
        // Fix Projectile
        GameObject proj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ArcProjectile.prefab");
        if (proj != null) {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(proj);
            SpriteRenderer[] srs = instance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs) sr.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefab/ArcProjectile.prefab");
            Object.DestroyImmediate(instance);
        }

        // Fix Warning Marker
        GameObject marker = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ArcWarningMarker.prefab");
        if (marker != null) {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(marker);
            SpriteRenderer[] srs = instance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs) sr.sprite = sprite;
            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefab/ArcWarningMarker.prefab");
            Object.DestroyImmediate(instance);
        }
        
        Debug.Log("Sprites fixed!");
    }
}
