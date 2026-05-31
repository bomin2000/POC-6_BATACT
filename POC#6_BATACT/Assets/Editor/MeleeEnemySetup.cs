using UnityEngine;
using UnityEditor;

public class MeleeEnemySetup : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Setup Melee Enemy Refactor")]
    public static void SetupMeleeEnemies()
    {
        string[] prefabs = new string[] { "Assets/Prefab/Enemy.prefab" };

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

            // 2. Configure EnemyContactDamage (Disable it completely for melee)
            EnemyContactDamage contactDamage = instance.GetComponent<EnemyContactDamage>();
            if (contactDamage != null)
            {
                contactDamage.enabled = false;
            }

            // 3. Add / Configure EnemyMeleeAttack
            EnemyMeleeAttack meleeAttack = instance.GetComponent<EnemyMeleeAttack>();
            if (meleeAttack == null)
            {
                meleeAttack = instance.AddComponent<EnemyMeleeAttack>();
            }

            SerializedObject soMelee = new SerializedObject(meleeAttack);
            soMelee.FindProperty("attackRange").floatValue = 1.2f;
            soMelee.FindProperty("attackWindup").floatValue = 0.5f;
            soMelee.FindProperty("attackCooldown").floatValue = 1.5f;
            soMelee.FindProperty("damage").floatValue = 15f;
            soMelee.FindProperty("attackBoxSize").vector2Value = new Vector2(1.2f, 1f);
            soMelee.FindProperty("attackBoxOffset").vector2Value = new Vector2(0.6f, 0f);
            
            Transform visualRootTransform = null;
            if (chaser != null)
            {
                SerializedObject soChaser = new SerializedObject(chaser);
                visualRootTransform = (Transform)soChaser.FindProperty("visualRoot").objectReferenceValue;
                
                if (visualRootTransform == null)
                {
                    visualRootTransform = instance.transform.Find("body");
                    if (visualRootTransform != null)
                    {
                        soChaser.FindProperty("visualRoot").objectReferenceValue = visualRootTransform;
                    }
                }

                soChaser.FindProperty("meleeAttack").objectReferenceValue = meleeAttack;
                soChaser.ApplyModifiedProperties();
            }

            if (visualRootTransform != null)
            {
                soMelee.FindProperty("visualRoot").objectReferenceValue = visualRootTransform;

                // 4. Create AttackArm
                Transform attackArm = visualRootTransform.Find("AttackArm");
                if (attackArm != null)
                {
                    DestroyImmediate(attackArm.gameObject);
                }

                GameObject armObj = new GameObject("AttackArm");
                attackArm = armObj.transform;
                attackArm.SetParent(visualRootTransform, false);
                attackArm.localPosition = new Vector3(0.3f, 0.2f, 0f);
                attackArm.localScale = new Vector3(0.6f, 0.2f, 1f);

                SpriteRenderer armRenderer = armObj.AddComponent<SpriteRenderer>();
                
                SpriteRenderer rootRenderer = visualRootTransform.GetComponent<SpriteRenderer>();
                if (rootRenderer != null)
                {
                    armRenderer.sprite = rootRenderer.sprite;
                }

                armRenderer.color = new Color(0.8f, 0.2f, 0.2f); // Reddish arm
                armRenderer.sortingOrder = 5;

                soMelee.FindProperty("attackVisual").objectReferenceValue = attackArm;
            }
            soMelee.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            DestroyImmediate(instance);
        }

        Debug.Log("Successfully updated Enemy and EnemyFast prefabs for Melee Attack Refactor!");
    }
}
