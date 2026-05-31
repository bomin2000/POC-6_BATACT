#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class TestEnvironmentSetup : Editor
{
    [MenuItem("Tools/Legacy Setups/Create Debug Test Environment")]
    public static void CreateTestEnvironment()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1)
        {
            Debug.LogError("The 'Ground' layer does not exist! Please run 'Tools > Cleanup Layers & Prefabs' first.");
            return;
        }

        // 1. Create Ground Floor
        GameObject floor = GameObject.Find("Test_GroundFloor");
        if (floor == null)
        {
            floor = new GameObject("Test_GroundFloor");
            floor.layer = groundLayer;
            
            BoxCollider2D floorCol = floor.AddComponent<BoxCollider2D>();
            // Make it wide enough to walk on, but limited so there are empty spaces at the edges
            floorCol.size = new Vector2(15f, 2f);
            
            // Position it below the typical player spawn
            floor.transform.position = new Vector3(0f, -4f, 0f);

            // Add a simple sprite renderer to make it visible
            SpriteRenderer sr = floor.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0.5f, 0.3f, 1f); // Dark green
            sr.sprite = CreateSquareSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = floorCol.size;
        }

        // 2. Create Ground Wall
        GameObject wall = GameObject.Find("Test_GroundWall");
        if (wall == null)
        {
            wall = new GameObject("Test_GroundWall");
            wall.layer = groundLayer;

            BoxCollider2D wallCol = wall.AddComponent<BoxCollider2D>();
            // Tall and thin
            wallCol.size = new Vector2(2f, 10f);

            // Position it to the right of the player
            wall.transform.position = new Vector3(8f, 0f, 0f);

            SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.3f, 0.3f, 1f); // Dark red
            sr.sprite = CreateSquareSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = wallCol.size;
        }

        // 3. Deploy Test Enemy
        GameObject enemyObj = GameObject.Find("Test_Enemy");
        if (enemyObj == null)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy.prefab");
            if (enemyPrefab != null)
            {
                enemyObj = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemyObj.name = "Test_Enemy";
                // Position to the left of the player
                enemyObj.transform.position = new Vector3(-6f, -2.5f, 0f);
            }
            else
            {
                Debug.LogWarning("Could not find Assets/Prefab/Enemy.prefab to deploy test enemy.");
            }
        }

        Debug.Log("<color=cyan><b>Debug Test Environment Created!</b></color> Play Mode is now ready for Probe/Hitbox tuning.");
    }

    private static Sprite CreateSquareSprite()
    {
        // Try to find the default Unity UI background sprite or a plain square to use
        Sprite builtIn = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        return builtIn;
    }
}
#endif
