using UnityEngine;
using UnityEditor;

public class MeleeEnemySetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Melee Enemy Refactor")]
    public static void SetupMeleeEnemies()
    {
        string[] prefabs = new string[] { "Assets/Prefab/Enemy.prefab", "Assets/Prefab/EnemyFast.prefab" };

        foreach (string path in prefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"Could not find {path}");
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            // 1. Configure EnemySideViewChaser stopDistance
            EnemySideViewChaser chaser = instance.GetComponent<EnemySideViewChaser>();
            if (chaser != null)
            {
                SerializedObject soChaser = new SerializedObject(chaser);
                soChaser.FindProperty("stopDistance").floatValue = path.Contains("Fast") ? 0.8f : 1.0f;
                soChaser.ApplyModifiedProperties();
            }

            // 2. Configure EnemyContactDamage (Nerf it)
            EnemyContactDamage contactDamage = instance.GetComponent<EnemyContactDamage>();
            if (contactDamage != null)
            {
                SerializedObject soContact = new SerializedObject(contactDamage);
                soContact.FindProperty("damage").floatValue = path.Contains("Fast") ? 3f : 5f;
                soContact.FindProperty("knockbackForce").floatValue = 4f;
                soContact.ApplyModifiedProperties();
            }

            // 3. Add / Configure EnemyMeleeAttack
            EnemyMeleeAttack meleeAttack = instance.GetComponent<EnemyMeleeAttack>();
            if (meleeAttack == null)
            {
                meleeAttack = instance.AddComponent<EnemyMeleeAttack>();
            }

            SerializedObject soMelee = new SerializedObject(meleeAttack);
            if (path.Contains("Fast"))
            {
                soMelee.FindProperty("attackRange").floatValue = 1.0f;
                soMelee.FindProperty("attackWindup").floatValue = 0.2f; // Fast!
                soMelee.FindProperty("attackCooldown").floatValue = 1.0f;
                soMelee.FindProperty("damage").floatValue = 10f;
                soMelee.FindProperty("attackBoxSize").vector2Value = new Vector2(1f, 1f);
                soMelee.FindProperty("attackBoxOffset").vector2Value = new Vector2(0.5f, 0f);
            }
            else
            {
                soMelee.FindProperty("attackRange").floatValue = 1.2f;
                soMelee.FindProperty("attackWindup").floatValue = 0.5f;
                soMelee.FindProperty("attackCooldown").floatValue = 1.5f;
                soMelee.FindProperty("damage").floatValue = 15f;
                soMelee.FindProperty("attackBoxSize").vector2Value = new Vector2(1.2f, 1f);
                soMelee.FindProperty("attackBoxOffset").vector2Value = new Vector2(0.6f, 0f);
            }
            
            Transform visualRootTransform = null;
            if (chaser != null)
            {
                SerializedObject soChaser = new SerializedObject(chaser);
                visualRootTransform = (Transform)soChaser.FindProperty("visualRoot").objectReferenceValue;
            }

            if (visualRootTransform != null)
            {
                soMelee.FindProperty("visualRoot").objectReferenceValue = visualRootTransform;
            }
            soMelee.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
        }

        Debug.Log("Successfully updated Enemy and EnemyFast prefabs for Melee Attack Refactor!");
    }
}
