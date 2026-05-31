using UnityEngine;
using UnityEditor;
using System.IO;

public class WeaponRoleSetup : MonoBehaviour
{
    [InitializeOnLoadMethod]
    private static void RunOnce()
    {
        if (SessionState.GetBool("WeaponRolesSetupRun", false)) return;
        SessionState.SetBool("WeaponRolesSetupRun", true);
        EditorApplication.delayCall += ApplyRoles;
    }

    [MenuItem("Tools/Setup Weapon Roles (Balancing)")]
    public static void ApplyRoles()
    {
        CreateSpecialProfile("Assets/ScriptableObjects/SpearChargeProfile.asset", WeaponState.Spear);
        CreateSpecialProfile("Assets/ScriptableObjects/ScissorsHoldProfile.asset", WeaponState.Scissors);
        CreateSpecialProfile("Assets/ScriptableObjects/BoomerangEmpoweredProfile.asset", WeaponState.Boomerang);

        // Allow time for AssetDatabase to refresh
        AssetDatabase.Refresh();

        WeaponHitboxProfile spearCharge = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>("Assets/ScriptableObjects/SpearChargeProfile.asset");
        WeaponHitboxProfile scissorsHold = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>("Assets/ScriptableObjects/ScissorsHoldProfile.asset");
        WeaponHitboxProfile boomerangEmpowered = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>("Assets/ScriptableObjects/BoomerangEmpoweredProfile.asset");

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
        if (playerPrefab != null)
        {
            DualBladeWeaponController controller = playerPrefab.GetComponent<DualBladeWeaponController>();
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                so.Update();
                so.FindProperty("spearChargeProfile").objectReferenceValue = spearCharge;
                so.FindProperty("scissorsHoldProfile").objectReferenceValue = scissorsHold;
                so.FindProperty("boomerangEmpoweredProfile").objectReferenceValue = boomerangEmpowered;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerPrefab);
                PrefabUtility.SavePrefabAsset(playerPrefab);
                Debug.Log("Successfully assigned secondary action profiles to Player prefab!");
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:WeaponHitboxProfile");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponHitboxProfile profile = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>(path);
            if (profile == null) continue;

            Undo.RecordObject(profile, "Setup Weapon Role");

            string lowerPath = path.ToLower();

            if (lowerPath.Contains("spearcharge"))
            {
                profile.state = WeaponState.Spear;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(2.5f, 0f);
                profile.boxSize = new Vector2(4.0f, 0.6f); 
                
                profile.startupSeconds = 0.2f;
                profile.activeSeconds = 0.2f;
                profile.recoverySeconds = 0.4f;

                profile.reaction.reactionType = HitReactionType.Knockback;
                profile.reaction.damage = 80f;
                profile.reaction.knockbackForce = 45f;
                profile.reaction.stunSeconds = 0.3f;
                profile.healOnHit = 15f; // Heal on hit for skill
            }
            else if (lowerPath.Contains("spear"))
            {
                profile.state = WeaponState.Spear;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(3.2f, 0f);
                profile.boxSize = new Vector2(2.5f, 0.5f); 
                
                profile.startupSeconds = 0.1f;
                profile.activeSeconds = 0.15f;
                profile.recoverySeconds = 0.3f; 

                profile.reaction.reactionType = HitReactionType.Knockback;
                profile.reaction.damage = 40f;
                profile.reaction.knockbackForce = 25f; 
                profile.reaction.stunSeconds = 0.15f;
                profile.healOnHit = 0f;
            }
            else if (lowerPath.Contains("scissorshold"))
            {
                profile.state = WeaponState.Scissors;
                profile.shape = HitboxShape.Circle;
                profile.localOffset = new Vector2(0f, 0f);
                profile.radius = 2.5f; 
                
                profile.startupSeconds = 0.1f;
                profile.activeSeconds = 0.3f;
                profile.recoverySeconds = 0.2f;

                profile.reaction.reactionType = HitReactionType.StunLock;
                profile.reaction.damage = 40f;
                profile.reaction.knockbackForce = 0f;
                profile.reaction.stunSeconds = 1.0f; 
                profile.healOnHit = 15f; // Heal on hit for skill
            }
            else if (lowerPath.Contains("scissor"))
            {
                profile.state = WeaponState.Scissors;
                profile.shape = HitboxShape.Circle;
                profile.localOffset = new Vector2(0.5f, 0f);
                profile.radius = 1.8f; 
                
                profile.startupSeconds = 0.05f;
                profile.activeSeconds = 0.1f;
                profile.recoverySeconds = 0.15f; 

                profile.reaction.reactionType = HitReactionType.StunLock;
                profile.reaction.damage = 25f;
                profile.reaction.knockbackForce = 5f; 
                profile.reaction.stunSeconds = 0.5f; 
                profile.healOnHit = 0f;
            }
            else if (lowerPath.Contains("boomerangempowered"))
            {
                profile.state = WeaponState.Boomerang;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(0.8f, 0f);
                profile.boxSize = new Vector2(2.5f, 2.5f);
                
                profile.startupSeconds = 0.1f;
                profile.activeSeconds = 0.3f;
                profile.recoverySeconds = 0.1f;

                profile.reaction.reactionType = HitReactionType.PullToPlayer;
                profile.reaction.damage = 50f;
                profile.reaction.knockbackForce = 15f;
                profile.reaction.stunSeconds = 0.2f;
                profile.healOnHit = 15f; // Heal on hit for skill
            }
            else if (lowerPath.Contains("boomerang"))
            {
                profile.state = WeaponState.Boomerang;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(0.8f, 0f);
                profile.boxSize = new Vector2(1.5f, 1.5f);
                
                profile.startupSeconds = 0.15f;
                profile.activeSeconds = 0.2f;
                profile.recoverySeconds = 0.1f;

                profile.reaction.reactionType = HitReactionType.PullToPlayer;
                profile.reaction.damage = 15f; 
                profile.reaction.knockbackForce = 8f; 
                profile.reaction.stunSeconds = 0.1f;
                profile.healOnHit = 0f;
            }

            EditorUtility.SetDirty(profile);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully balanced weapon profiles!");
    }

    private static void CreateSpecialProfile(string path, WeaponState state)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            WeaponHitboxProfile newProfile = ScriptableObject.CreateInstance<WeaponHitboxProfile>();
            newProfile.state = state;
            AssetDatabase.CreateAsset(newProfile, path);
        }
    }
}
