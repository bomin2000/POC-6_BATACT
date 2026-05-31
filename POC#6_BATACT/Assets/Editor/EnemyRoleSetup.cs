using UnityEngine;
using UnityEditor;

public class EnemyRoleSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Enemy Roles (Differentiate)")]
    public static void ApplyRoles()
    {
        string[] paths = {
            "Assets/Prefab/Enemy.prefab",
            "Assets/Prefab/EnemyFast.prefab",
            "Assets/Prefab/EnemyRanged.prefab"
        };

        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = editingScope.prefabContentsRoot;

                // Hit receiver (Health)
                var hp = root.GetComponent<PrototypeEnemyHitReceiver>();
                
                // Visual root
                Transform visualRoot = root.transform.Find("visualRoot");
                if (visualRoot == null) visualRoot = root.transform.Find("body");

                SpriteRenderer sr = visualRoot != null ? visualRoot.GetComponent<SpriteRenderer>() : null;

                if (path.Contains("EnemyFast"))
                {
                    // Fast: Low HP, fast, doesn't hop, orange color
                    if (hp != null) hp.GetType().GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hp, 40f);
                    
                    var chaser = root.GetComponent<EnemySideViewChaser>();
                    if (chaser != null)
                    {
                        var t = chaser.GetType();
                        t.GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(chaser, 4.8f);
                        t.GetField("hopWhenBlocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(chaser, false);
                    }

                    var sd = root.GetComponent<EnemySelfDestruct>();
                    if (sd != null)
                    {
                        sd.GetType().GetField("windupSeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(sd, 1.2f);
                    }

                    if (sr != null)
                    {
                        sr.color = new Color(1f, 0.5f, 0f); // Orange
                    }
                    if (visualRoot != null)
                    {
                        visualRoot.localScale = new Vector3(0.8f, 0.8f, 1f); // Smaller
                    }
                }
                else if (path.Contains("EnemyRanged"))
                {
                    // Ranged: Medium HP, keeps distance, blue color
                    if (hp != null) hp.GetType().GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hp, 60f);

                    var shooter = root.GetComponent<EnemyRangedShooter>();
                    if (shooter != null)
                    {
                        var t = shooter.GetType();
                        t.GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shooter, 2.5f);
                        t.GetField("stopDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shooter, 6.0f);
                        t.GetField("retreatDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shooter, 3.5f);
                    }

                    if (sr != null)
                    {
                        sr.color = new Color(0.2f, 0.6f, 1f); // Blue
                    }
                    if (visualRoot != null)
                    {
                        visualRoot.localScale = new Vector3(0.9f, 0.9f, 1f);
                    }
                }
                else
                {
                    // Basic: High HP, slow, grey/reddish, large
                    if (hp != null) hp.GetType().GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(hp, 120f);

                    var chaser = root.GetComponent<EnemySideViewChaser>();
                    if (chaser != null)
                    {
                        var t = chaser.GetType();
                        t.GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(chaser, 1.8f);
                        t.GetField("stopDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(chaser, 0.9f);
                    }

                    if (sr != null)
                    {
                        sr.color = new Color(0.8f, 0.3f, 0.3f); // Dark Reddish
                    }
                    if (visualRoot != null)
                    {
                        visualRoot.localScale = new Vector3(1.2f, 1.2f, 1f); // Larger
                    }
                }
            }
        }

        Debug.Log("Successfully differentiated Enemy roles in their prefabs!");
    }
}
