using UnityEditor;
using UnityEngine;
public class PatchProfiles {
    public static void DoPatch() {
        string[] guids = AssetDatabase.FindAssets("t:WeaponHitboxProfile");
        foreach(var guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponHitboxProfile p = AssetDatabase.LoadAssetAtPath<WeaponHitboxProfile>(path);
            if (p != null && (p.name.ToLower().Contains("scissors"))) {
                p.reaction.partDamageMultiplier = 3.0f;
                EditorUtility.SetDirty(p);
                Debug.Log("Updated "+p.name+" with partDamageMultiplier=3.0");
            }
        }
        AssetDatabase.SaveAssets();
    }
}
