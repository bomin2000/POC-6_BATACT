using UnityEditor;
using UnityEngine;

public class CheckPrefabNames {
    public static void Do() {
        string destPath = "Assets/Prefab/EnemyShieldTank.prefab";
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(destPath);
        if (go != null) {
            Debug.Log("Root: "+go.name);
            foreach(Transform t in go.transform) {
                Debug.Log("Child: "+t.name);
            }
        }
    }
}
