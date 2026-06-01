using UnityEngine;
using UnityEditor;

public class WeaponRoleSetup
{
    // Make sure we have a menu item so the user can run it, or we can run it from command line if possible.
    // Actually, I can just write an editor script that executes on compilation to force the update, but a menu item is safer.
    [MenuItem("Tools/BAT/Apply Weapon Roles")]
    public static void ApplyRoles()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponHitboxProfile");
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponHitboxProfile profile = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>(path);
            if (profile == null) continue;

            SerializedObject so = new SerializedObject(profile);
            SerializedProperty reactionProp = so.FindProperty("reaction");
            if (reactionProp == null) continue;

            string lowerName = profile.name.ToLower();

            if (lowerName.Contains("spear"))
            {
                // Spear: Low damage, high knockback/control
                reactionProp.FindPropertyRelative("damage").floatValue = 15f;
                reactionProp.FindPropertyRelative("knockbackForce").floatValue = 30f;
                reactionProp.FindPropertyRelative("stunSeconds").floatValue = 0.4f;
                reactionProp.FindPropertyRelative("taggedDamageMultiplier").floatValue = 1.0f;
                reactionProp.FindPropertyRelative("appliedTag").intValue = 0; // None
            }
            else if (lowerName.Contains("boomerang"))
            {
                // Boomerang: Pull tag, medium damage
                reactionProp.FindPropertyRelative("damage").floatValue = 25f;
                reactionProp.FindPropertyRelative("taggedDamageMultiplier").floatValue = 1.0f;
                reactionProp.FindPropertyRelative("appliedTag").intValue = 1; // Pulled (1 << 0)
                reactionProp.FindPropertyRelative("tagDuration").floatValue = 3.0f;
            }
            else if (lowerName.Contains("scissors"))
            {
                // Scissors: Execution damage against tagged
                reactionProp.FindPropertyRelative("damage").floatValue = 35f;
                reactionProp.FindPropertyRelative("taggedDamageMultiplier").floatValue = 2.5f;
                reactionProp.FindPropertyRelative("appliedTag").intValue = 0; // None
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
            Debug.Log($"[WeaponRoleSetup] Updated profile: {profile.name}");
            count++;
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"[WeaponRoleSetup] Weapon roles applied successfully to {count} profiles.");
    }
}
