using UnityEngine;
using UnityEditor;

public class KamikazeEnemySetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Kamikaze Fast Enemy")]
    public static void SetupKamikazeEnemy()
    {
        string path = "Assets/Prefab/EnemyFast.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            Debug.LogError($"Could not find {path}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // 1. Remove Melee Attack
        EnemyMeleeAttack meleeAttack = instance.GetComponent<EnemyMeleeAttack>();
        if (meleeAttack != null) DestroyImmediate(meleeAttack);

        // 2. Remove Contact Damage
        EnemyContactDamage contactDamage = instance.GetComponent<EnemyContactDamage>();
        if (contactDamage != null) DestroyImmediate(contactDamage);

        // 3. Configure Chaser Stop Distance (let it get closer before stopping)
        EnemySideViewChaser chaser = instance.GetComponent<EnemySideViewChaser>();
        if (chaser != null)
        {
            SerializedObject soChaser = new SerializedObject(chaser);
            soChaser.FindProperty("stopDistance").floatValue = 0.5f; // Gets very close
            soChaser.ApplyModifiedProperties();
        }

        // 4. Add Self Destruct
        EnemySelfDestruct kamikaze = instance.GetComponent<EnemySelfDestruct>();
        if (kamikaze == null) kamikaze = instance.AddComponent<EnemySelfDestruct>();

        SerializedObject soKamikaze = new SerializedObject(kamikaze);
        soKamikaze.FindProperty("triggerRange").floatValue = 1.5f;
        soKamikaze.FindProperty("windupSeconds").floatValue = 0.8f;
        soKamikaze.FindProperty("stopDuringWindup").boolValue = true;
        soKamikaze.FindProperty("explodeOnDeath").boolValue = false; // Defuse-able by default
        soKamikaze.FindProperty("explosionRadius").floatValue = 2.5f;
        soKamikaze.FindProperty("explosionDamage").floatValue = 30f;
        soKamikaze.FindProperty("explosionKnockback").floatValue = 15f;
        
        if (chaser != null)
        {
            SerializedObject soChaser = new SerializedObject(chaser);
            Transform visualRootTransform = (Transform)soChaser.FindProperty("visualRoot").objectReferenceValue;
            
            if (visualRootTransform == null)
            {
                visualRootTransform = instance.transform.Find("body");
                if (visualRootTransform != null)
                {
                    soChaser.FindProperty("visualRoot").objectReferenceValue = visualRootTransform;
                    soChaser.ApplyModifiedProperties();
                }
            }

            if (visualRootTransform != null)
            {
                soKamikaze.FindProperty("visualRoot").objectReferenceValue = visualRootTransform;
            }
        }

        soKamikaze.ApplyModifiedProperties();

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log("Successfully converted Fast Enemy into Kamikaze (Self-Destruct) enemy!");
    }
}
