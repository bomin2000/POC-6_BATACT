using UnityEngine;
using UnityEditor;

public class AttachSlideAssist
{
    [MenuItem("Tools/Setups/Attach Wall Slide Assist to Player")]
    public static void Attach()
    {
        string path = "Assets/Prefab/Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            if (prefab.GetComponent<PlayerWallSlideAssist>() == null)
            {
                prefab.AddComponent<PlayerWallSlideAssist>();
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log("Attached PlayerWallSlideAssist to Player prefab.");
            }
            else
            {
                Debug.Log("PlayerWallSlideAssist already attached.");
            }
        }
    }
}
