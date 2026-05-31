using UnityEngine;
using UnityEditor;

public class WeaponRoleSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Weapon Roles (Balancing)")]
    public static void ApplyRoles()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponHitboxProfile");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponHitboxProfile profile = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>(path);
            if (profile == null) continue;

            Undo.RecordObject(profile, "Setup Weapon Role");

            string lowerPath = path.ToLower();

            if (lowerPath.Contains("spear"))
            {
                // Spear: Long range, narrow width, strong forward knockback
                profile.state = WeaponState.Spear;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(1.5f, 0f);
                profile.boxSize = new Vector2(2.5f, 0.5f); // Long, narrow
                
                profile.startupSeconds = 0.1f;
                profile.activeSeconds = 0.15f;
                profile.recoverySeconds = 0.3f; // Slightly longer recovery

                profile.reaction.reactionType = HitReactionType.Knockback;
                profile.reaction.damage = 40f;
                profile.reaction.knockbackForce = 25f; // Strong knockback
                profile.reaction.stunSeconds = 0.15f;
            }
            else if (lowerPath.Contains("scissor"))
            {
                // Scissors: Short range, wide width, strong hitstun
                profile.state = WeaponState.Scissors;
                profile.shape = HitboxShape.Circle;
                profile.localOffset = new Vector2(0.5f, 0f);
                profile.radius = 1.8f; // Wide, close range
                
                profile.startupSeconds = 0.05f;
                profile.activeSeconds = 0.1f;
                profile.recoverySeconds = 0.15f; // Fast strikes

                profile.reaction.reactionType = HitReactionType.StunLock;
                profile.reaction.damage = 25f;
                profile.reaction.knockbackForce = 5f; // Low knockback
                profile.reaction.stunSeconds = 0.5f; // Strong hitstun
            }
            else if (lowerPath.Contains("boomerang"))
            {
                // Boomerang: Medium/long range, multi-hit, low damage, pulls towards player
                profile.state = WeaponState.Boomerang;
                profile.shape = HitboxShape.Box;
                profile.localOffset = new Vector2(0.8f, 0f);
                profile.boxSize = new Vector2(1.5f, 1.5f);
                
                profile.startupSeconds = 0.15f;
                profile.activeSeconds = 0.2f;
                profile.recoverySeconds = 0.1f;

                profile.reaction.reactionType = HitReactionType.PullToPlayer;
                profile.reaction.damage = 15f; // Low damage (multi-hit compensates)
                profile.reaction.knockbackForce = 8f; // Pull force
                profile.reaction.stunSeconds = 0.1f;
            }

            EditorUtility.SetDirty(profile);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully balanced weapon profiles!");
    }
}
