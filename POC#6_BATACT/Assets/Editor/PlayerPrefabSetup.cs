#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ProjectLayerCleanup : Editor
{
    private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";
    private const string EnemyPrefabPath = "Assets/Prefab/Enemy.prefab";
    private const string BoomerangPrefabPath = "Assets/Prefab/DualBladeBoomerangProjectile.prefab";

    [MenuItem("Tools/Legacy Setups/Cleanup Layers & Prefabs")]
    public static void ExecuteCleanup()
    {
        FixTagManagerLayers();

        SetupPlayerPrefab();
        SetupEnemyPrefab();
        SetupBoomerangPrefab();

        Debug.Log("<color=green><b>Project Layer Cleanup Completed Successfully!</b></color>");
    }

    private static void FixTagManagerLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Rename the typo layer
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == "PlayerAttacHitBox")
            {
                layerProp.stringValue = "PlayerAttackHitbox";
            }
        }

        // Ensure Projectile layer exists
        string[] requiredLayers = { "PlayerAttackHitbox", "WeaponTerrainProbe", "PlayerWeapon", "Ground", "Enemy", "Projectile" };
        foreach (string layer in requiredLayers)
        {
            bool found = false;
            for (int i = 8; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layer)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layer;
                        break;
                    }
                }
            }
        }

        tagManager.ApplyModifiedProperties();
    }

    private static void SetupPlayerPrefab()
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"Could not find Player prefab at {PlayerPrefabPath}");
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(PlayerPrefabPath))
        {
            GameObject playerRoot = editingScope.prefabContentsRoot;
            DualBladeWeaponController weaponController = playerRoot.GetComponent<DualBladeWeaponController>();
            
            // 1. Setup MeleeHitbox
            Transform meleeHitboxTr = playerRoot.transform.Find("MeleeHitbox");
            GameObject meleeHitbox = meleeHitboxTr != null ? meleeHitboxTr.gameObject : new GameObject("MeleeHitbox");
            if (meleeHitboxTr == null) meleeHitbox.transform.SetParent(playerRoot.transform, false);
            meleeHitbox.layer = LayerMask.NameToLayer("PlayerAttackHitbox");

            WeaponHitbox2D rootHitbox = playerRoot.GetComponent<WeaponHitbox2D>();
            WeaponHitbox2D newHitbox = meleeHitbox.GetComponent<WeaponHitbox2D>();
            
            if (rootHitbox != null)
            {
                if (newHitbox == null)
                {
                    newHitbox = meleeHitbox.AddComponent<WeaponHitbox2D>();
                    EditorUtility.CopySerialized(rootHitbox, newHitbox);
                }
                DestroyImmediate(rootHitbox, true);
            }

            if (newHitbox != null)
            {
                SerializedObject soHitbox = new SerializedObject(newHitbox);
                soHitbox.FindProperty("targetLayers").intValue = 1 << LayerMask.NameToLayer("Enemy");
                soHitbox.ApplyModifiedProperties();
            }

            // 2. Setup WeaponTerrainProbe
            Transform probeTr = playerRoot.transform.Find("WeaponTerrainProbe");
            GameObject probeObj = probeTr != null ? probeTr.gameObject : new GameObject("WeaponTerrainProbe");
            if (probeTr == null) probeObj.transform.SetParent(playerRoot.transform, false);
            probeObj.layer = LayerMask.NameToLayer("WeaponTerrainProbe");

            WeaponTerrainProbe2D probeComp = probeObj.GetComponent<WeaponTerrainProbe2D>();
            if (probeComp == null)
            {
                probeComp = probeObj.AddComponent<WeaponTerrainProbe2D>();
            }

            if (weaponController != null)
            {
                SerializedObject soProbe = new SerializedObject(probeComp);
                soProbe.FindProperty("weaponController").objectReferenceValue = weaponController;
                soProbe.FindProperty("terrainLayers").intValue = 1 << LayerMask.NameToLayer("Ground");
                
                SerializedObject soController = new SerializedObject(weaponController);
                Object weaponRootObj = soController.FindProperty("weaponRoot").objectReferenceValue;
                if (weaponRootObj != null)
                {
                    soProbe.FindProperty("probeOrigin").objectReferenceValue = weaponRootObj;
                }
                
                if (newHitbox != null)
                {
                    soController.FindProperty("meleeHitbox").objectReferenceValue = newHitbox;
                }
                soController.ApplyModifiedProperties();
                soProbe.ApplyModifiedProperties();
            }

            // 3. Fix Player Movement Ground Layers
            PlayerTopDownMovement movement = playerRoot.GetComponent<PlayerTopDownMovement>();
            if (movement != null)
            {
                SerializedObject soMovement = new SerializedObject(movement);
                soMovement.FindProperty("groundLayers").intValue = 1 << LayerMask.NameToLayer("Ground");
                soMovement.ApplyModifiedProperties();
            }

            // 4. Set Weapon Visual Layers
            int weaponLayer = LayerMask.NameToLayer("PlayerWeapon");
            string[] weaponParts = { "WeaponRoot", "Pivot", "UpperBlade", "LowerBlade", "ThrowSpawnPoint" };
            foreach (string part in weaponParts)
            {
                Transform partTr = FindChildRecursive(playerRoot.transform, part);
                if (partTr != null) partTr.gameObject.layer = weaponLayer;
            }
            
            playerRoot.layer = LayerMask.NameToLayer("Player");
        }
    }

    private static void SetupEnemyPrefab()
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"Could not find Enemy prefab at {EnemyPrefabPath}");
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(EnemyPrefabPath))
        {
            GameObject enemyRoot = editingScope.prefabContentsRoot;
            
            // Assuming the script is called EnemySideViewChaser
            MonoBehaviour chaser = enemyRoot.GetComponent("EnemySideViewChaser") as MonoBehaviour;
            if (chaser != null)
            {
                SerializedObject soChaser = new SerializedObject(chaser);
                
                SerializedProperty enemyLayersProp = soChaser.FindProperty("enemyLayers");
                if (enemyLayersProp != null) enemyLayersProp.intValue = 1 << LayerMask.NameToLayer("Enemy");

                SerializedProperty groundLayersProp = soChaser.FindProperty("groundLayers");
                if (groundLayersProp != null) groundLayersProp.intValue = 1 << LayerMask.NameToLayer("Ground");

                soChaser.ApplyModifiedProperties();
            }
            
            enemyRoot.layer = LayerMask.NameToLayer("Enemy");
        }
    }

    private static void SetupBoomerangPrefab()
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(BoomerangPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"Could not find Boomerang prefab at {BoomerangPrefabPath}");
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(BoomerangPrefabPath))
        {
            GameObject boomerangRoot = editingScope.prefabContentsRoot;
            boomerangRoot.layer = LayerMask.NameToLayer("Projectile");

            DualBladeBoomerangProjectile proj = boomerangRoot.GetComponent<DualBladeBoomerangProjectile>();
            if (proj != null)
            {
                SerializedObject soProj = new SerializedObject(proj);
                soProj.FindProperty("targetLayers").intValue = 1 << LayerMask.NameToLayer("Enemy");
                soProj.ApplyModifiedProperties();
            }
        }
    }

    private static Transform FindChildRecursive(Transform parent, string exactName)
    {
        if (parent.name == exactName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, exactName);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
