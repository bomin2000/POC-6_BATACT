#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class TutorialSetupEditor : Editor
{
    [MenuItem("Tools/Setup Tutorial Triggers")]
    public static void CreateTutorialTriggers()
    {
        GameObject root = new GameObject("Tutorial_Triggers");

        CreateTrigger(root, "Start Zone Hint", "목표 지점까지 도달하세요. 적을 모두 잡을 필요는 없습니다.", new Vector3(0, 0, 0));
        CreateTrigger(root, "Spear Vault Hint", "창을 바닥에 꽂고 Space로 높게 도약할 수 있습니다.", new Vector3(5, 0, 0));
        CreateTrigger(root, "Boomerang Rope Hint", "검은색 앵커 지점에 부메랑을 맞힌 뒤 우클릭으로 접근합니다.", new Vector3(10, 0, 0));

        Selection.activeGameObject = root;
        Debug.Log("Tutorial Triggers created! Please move them to the correct locations in your scene and adjust their BoxCollider2D sizes.");
    }

    private static void CreateTrigger(GameObject parent, string name, string text, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(5f, 5f); // Default size

        TutorialHintTrigger2D trigger = go.AddComponent<TutorialHintTrigger2D>();
        trigger.hintText = text;
        trigger.duration = 4f;
        trigger.isOneShot = true;
        trigger.targetTag = "Player";
    }
}
#endif
