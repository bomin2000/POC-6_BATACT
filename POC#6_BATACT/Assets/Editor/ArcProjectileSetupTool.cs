using UnityEditor;
using UnityEngine;
using System.IO;

public class ArcProjectileSetupTool
{
    [MenuItem("Tools/BAT/Setup Arc Projectile System")]
    public static void SetupSystem()
    {
        string markerPath = "Assets/Prefab/ArcWarningMarker.prefab";
        string projPath = "Assets/Prefab/ArcProjectile.prefab";
        string enemyPath = "Assets/Prefab/EnemyRanged.prefab";

        // 1. Create Warning Marker Prefab
        if (!File.Exists(markerPath))
        {
            GameObject markerGo = new GameObject("ArcWarningMarker");
            var sr = markerGo.AddComponent<SpriteRenderer>();
            
            // Create a simple circle texture
            Texture2D tex = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++) {
                for (int x = 0; x < 32; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    Color c = dist < 16 ? Color.white : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            string texPath = "Assets/Prefab/CircleTex.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.textureType = TextureImporterType.Sprite;
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
            sr.color = new Color(1, 0, 0, 0); // Start transparent

            markerGo.AddComponent<ProjectileWarningMarker>();
            
            PrefabUtility.SaveAsPrefabAsset(markerGo, markerPath);
            Object.DestroyImmediate(markerGo);
        }

        // 2. Create Arc Projectile Prefab
        if (!File.Exists(projPath))
        {
            GameObject projGo = new GameObject("ArcProjectile");
            var rb = projGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            
            var col = projGo.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(projGo.transform);
            var sr = visualRoot.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefab/CircleTex.png");
            sr.color = Color.yellow;
            visualRoot.transform.localScale = Vector3.one * 0.5f;

            var arcProj = projGo.AddComponent<EnemyArcProjectile2D>();
            var serializedArc = new SerializedObject(arcProj);
            serializedArc.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            serializedArc.FindProperty("damage").floatValue = 10f;
            serializedArc.FindProperty("knockbackForce").floatValue = 15f;
            serializedArc.FindProperty("lifetime").floatValue = 5f;
            serializedArc.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(projGo, projPath);
            Object.DestroyImmediate(projGo);
        }

        // 3. Update EnemyRanged Prefab
        GameObject enemyRanged = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPath);
        if (enemyRanged != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(enemyRanged);
            var shooter = instance.GetComponent<EnemyRangedShooter>();
            if (shooter != null)
            {
                var serializedShooter = new SerializedObject(shooter);
                serializedShooter.FindProperty("arcProjectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
                serializedShooter.FindProperty("warningMarkerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(markerPath);
                serializedShooter.FindProperty("chargeDuration").floatValue = 1.0f;
                serializedShooter.FindProperty("timeOfFlight").floatValue = 1.2f;
                serializedShooter.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(instance, enemyPath);
            Object.DestroyImmediate(instance);
        }
        else
        {
            Debug.LogError("EnemyRanged.prefab not found at " + enemyPath);
        }

        Debug.Log("Arc Projectile System setup complete!");
    }
}
