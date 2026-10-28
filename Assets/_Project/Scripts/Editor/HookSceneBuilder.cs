using System.Collections.Generic;
using BrineBattle.Hunting;
using BrineBattle.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrineBattle.Editor
{
    /// <summary>
    /// Builds the Phase 2 placeholder scene (water, raft, drift spawner, hook) from code, so the
    /// scene file is never hand-written. Menu: BrineBattle > Build Hook Scene.
    /// Headless: -executeMethod BrineBattle.Editor.HookSceneBuilder.Build
    /// </summary>
    public static class HookSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Hook.unity";
        const string MatFolder = "Assets/_Project/Art/Materials";

        [MenuItem("BrineBattle/Build Hook Scene")]
        public static void Build()
        {
            SampleDataBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.SetPositionAndRotation(new Vector3(0f, 22f, -14f), Quaternion.Euler(58f, 0f, 0f));
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.55f, 0.75f, 0.9f);
            c.fieldOfView = 45f;

            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Ocean";
            water.transform.localScale = new Vector3(12f, 1f, 12f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<Renderer>().sharedMaterial = Mat("Ocean", new Color(0.1f, 0.35f, 0.55f));

            var raftGo = new GameObject("Raft");
            var raft = raftGo.AddComponent<Raft>();
            var plank = Mat("RaftBone", new Color(0.85f, 0.8f, 0.68f));
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Part {x},{z}";
                    tile.transform.SetParent(raftGo.transform, false);
                    tile.transform.localPosition = new Vector3(x * 1.05f, 0.15f, z * 1.05f);
                    tile.transform.localScale = new Vector3(1f, 0.3f, 1f);
                    tile.GetComponent<Renderer>().sharedMaterial = plank;
                    Object.DestroyImmediate(tile.GetComponent<Collider>());
                }
            var hunter = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hunter.name = "Hunter";
            hunter.transform.SetParent(raftGo.transform, false);
            hunter.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            hunter.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            hunter.GetComponent<Renderer>().sharedMaterial = Mat("Hunter", new Color(0.95f, 0.45f, 0.35f));
            Object.DestroyImmediate(hunter.GetComponent<Collider>());

            var spawnerGo = new GameObject("Drift Spawner");
            var spawner = spawnerGo.AddComponent<DriftSpawner>();
            spawner.raft = raft;
            spawner.monsters = LoadMonsters();

            var hookGo = new GameObject("Hook");
            hookGo.AddComponent<LineRenderer>().sharedMaterial = Mat("Line", new Color(0.95f, 0.95f, 0.9f));
            hookGo.AddComponent<HookController>().raft = raft;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log($"[HookSceneBuilder] saved {ScenePath} with {spawner.monsters.Count} monsters");
        }

        static List<MonsterDefinition> LoadMonsters()
        {
            var list = new List<MonsterDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:MonsterDefinition", new[] { "Assets/_Project/Data" }))
                list.Add(AssetDatabase.LoadAssetAtPath<MonsterDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            return list;
        }

        static Material Mat(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MatFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art", "Materials");
            string path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
