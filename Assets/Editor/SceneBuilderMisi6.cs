using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneBuilderMisi6
{
    [MenuItem("ProjectZ/Build HalamanSekolah Scene")]
    public static string BuildScene()
    {
        // 1. Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Main Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();

        // 3. Global Light 2D
        var lightGo = new GameObject("Global Light 2D");
        var lightType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (lightType != null)
        {
            var l2d = lightGo.AddComponent(lightType);
            var colorProp = lightType.GetProperty("color");
            var intensityProp = lightType.GetProperty("intensity");
            if (colorProp != null) colorProp.SetValue(l2d, new Color(0.48f, 0.55f, 0.72f, 1f));
            if (intensityProp != null) intensityProp.SetValue(l2d, 0.9f);
        }

        // 4. Background Sprite
        var bgGo = new GameObject("Bg");
        var bgSr = bgGo.AddComponent<SpriteRenderer>();
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/halaman_sekolah.png");
        bgSr.sprite = bgSprite;
        bgSr.sortingOrder = -10;
        bgGo.transform.position = Vector3.zero;
        bgGo.transform.localScale = new Vector3(2.12f, 2.12f, 1f);

        // 5. Load Player from RuangCCTV
        var cctvScene = EditorSceneManager.OpenScene("Assets/Scenes/RuangCCTV.unity", OpenSceneMode.Additive);
        var srcPlayer = GameObject.Find("Player");
        if (srcPlayer != null)
        {
            var player = Object.Instantiate(srcPlayer);
            player.name = "Player";
            SceneManager.MoveGameObjectToScene(player, scene);
            player.transform.position = new Vector3(0f, 3.2f, 0f);
        }
        EditorSceneManager.CloseScene(cctvScene, true);

        // 6. Borrow Zombie from Lorong.unity
        var lorongScene = EditorSceneManager.OpenScene("Assets/Scenes/Lorong.unity", OpenSceneMode.Additive);
        var srcZombie = GameObject.Find("Zombie_1");
        if (srcZombie != null)
        {
            var z1 = Object.Instantiate(srcZombie);
            z1.name = "Zombie_Lap1";
            SceneManager.MoveGameObjectToScene(z1, scene);
            z1.transform.position = new Vector3(-3.5f, 0.5f, 0f);
            SetPatrol(z1, new Vector2[] { new Vector2(-4.5f, 1.2f), new Vector2(-2.0f, -0.8f) });

            var z2 = Object.Instantiate(srcZombie);
            z2.name = "Zombie_Lap2";
            SceneManager.MoveGameObjectToScene(z2, scene);
            z2.transform.position = new Vector3(-1.0f, -1.8f, 0f);
            SetPatrol(z2, new Vector2[] { new Vector2(-2.2f, -1.8f), new Vector2(1.2f, -1.8f) });

            var z3 = Object.Instantiate(srcZombie);
            z3.name = "Zombie_Parkir";
            SceneManager.MoveGameObjectToScene(z3, scene);
            z3.transform.position = new Vector3(3.6f, 0.8f, 0f);
            SetPatrol(z3, new Vector2[] { new Vector2(3.0f, 1.5f), new Vector2(4.5f, 0.2f) });
        }
        EditorSceneManager.CloseScene(lorongScene, true);

        // 7. Environment Colliders
        var collParent = new GameObject("collison");
        AddBoxCol("WallTopLeft", new Vector2(-5.0f, 4.3f), new Vector2(8.2f, 1.4f), collParent.transform);
        AddBoxCol("WallTopRight", new Vector2(5.0f, 4.3f), new Vector2(8.2f, 1.4f), collParent.transform);
        AddBoxCol("WallTopDoorBack", new Vector2(0f, 4.6f), new Vector2(2.0f, 0.8f), collParent.transform);
        AddBoxCol("WallLeft", new Vector2(-9.2f, 0f), new Vector2(0.8f, 10f), collParent.transform);
        AddBoxCol("WallRight", new Vector2(9.2f, 0f), new Vector2(0.8f, 10f), collParent.transform);
        AddBoxCol("FenceBottomLeft", new Vector2(-5.2f, -3.75f), new Vector2(7.8f, 0.7f), collParent.transform);
        AddBoxCol("FenceBottomRight", new Vector2(6.2f, -3.75f), new Vector2(5.8f, 0.7f), collParent.transform);
        AddBoxCol("StreetBoundary", new Vector2(0f, -5.2f), new Vector2(20f, 0.8f), collParent.transform);
        AddBoxCol("PosSatpam", new Vector2(2.2f, -3.15f), new Vector2(1.5f, 1.5f), collParent.transform);
        AddBoxCol("GoalLeft", new Vector2(-7.1f, -0.1f), new Vector2(0.5f, 1.8f), collParent.transform);
        AddBoxCol("GoalRight", new Vector2(2.0f, -0.1f), new Vector2(0.5f, 1.8f), collParent.transform);
        AddBoxCol("CarsTopRow", new Vector2(5.5f, 2.0f), new Vector2(5.5f, 1.8f), collParent.transform);
        AddBoxCol("CarsBottomRow", new Vector2(7.0f, -1.7f), new Vector2(3.0f, 1.8f), collParent.transform);

        // 8. Triggers
        // Step 0: Courtyard entry trigger
        var step0Go = new GameObject("CourtyardStartTrigger");
        step0Go.transform.position = new Vector2(0f, 2.5f);
        var step0Col = step0Go.AddComponent<BoxCollider2D>();
        step0Col.size = new Vector2(18f, 0.8f);
        step0Col.isTrigger = true;
        step0Go.AddComponent<CourtyardZombieTrigger>();

        // Step 1: Patrol car climax trigger
        var patrolGo = new GameObject("PatrolCarClimax");
        patrolGo.transform.position = new Vector2(4.7f, -1.7f);
        var patrolCol = patrolGo.AddComponent<BoxCollider2D>();
        patrolCol.size = new Vector2(3.5f, 2.8f);
        patrolCol.isTrigger = true;
        patrolGo.AddComponent<PatrolCarClimaxTrigger>();

        // Step 2: School gate escape interactable
        var gateGo = new GameObject("SchoolGateEscape");
        gateGo.transform.position = new Vector2(0.2f, -3.8f);
        var gateCol = gateGo.AddComponent<BoxCollider2D>();
        gateCol.size = new Vector2(2.8f, 1.6f);
        gateCol.isTrigger = true;
        gateGo.AddComponent<SchoolGateEscapeInteractable>();

        // 9. Save Scene
        string scenePath = "Assets/Scenes/HalamanSekolah.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // 10. Add to Build Settings if not already present
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) { found = true; break; }
        }
        if (!found)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        return $"HalamanSekolah scene successfully created and saved at {scenePath}!";
    }

    private static void SetPatrol(GameObject z, Vector2[] pts)
    {
        if (z == null) return;
        var zc = z.GetComponent<ZombieController>();
        if (zc == null) return;
        var so = new SerializedObject(zc);
        var prop = so.FindProperty("patrolPoints");
        if (prop != null)
        {
            prop.arraySize = pts.Length;
            for (int i = 0; i < pts.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).vector2Value = pts[i];
            }
        }
        var persistProp = so.FindProperty("persistAcrossScenes");
        if (persistProp != null) persistProp.boolValue = false;
        so.ApplyModifiedProperties();
    }

    private static void AddBoxCol(string name, Vector2 pos, Vector2 size, Transform parent, bool isTrigger = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = isTrigger;
    }
}
