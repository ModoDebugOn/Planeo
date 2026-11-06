using System.IO;
using Gliding.Infrastructure;
using Gliding.Infrastructure.Profiles;
using Gliding.Presentation;
using Gliding.Presentation.Cameras;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Gliding.Editor
{
    /// <summary>
    /// Generates the demo's own art out of Unity primitives, so nothing on screen comes from a real
    /// game and everything can be regenerated after tweaking a number here:
    /// - Assets: ground grid texture, materials and the valley (the ledge everybody jumps from, the
    ///   distance markers, the river and the scenery).
    /// - Scene: 00_Valle, the playground every method starts from.
    ///
    /// The flight lane runs along +Z from the edge of the ledge (z = 0), so the distance flown is
    /// just the Z of the landing spot. The side camera looks at the lane from +X: keep +X clear.
    /// Regenerating keeps the asset GUIDs, so scenes that already use the prefabs stay linked.
    /// </summary>
    static class ValleyBuilder
    {
        const string Root = "Assets/_Project";
        const string EnvMaterials = Root + "/Materials/Environment";
        const string EnvPrefabs = Root + "/Prefabs/Environment";
        const string DebugMaterials = Root + "/Materials/Debug";
        const string Profiles = Root + "/ScriptableObjects/Methods";
        const string ScenePath = Root + "/Scenes/00_Valle.unity";

        // The leads, left to right on the ledge, one per method (plus Link for the Zelda extra).
        static readonly string[] Gliders = { "Spyro", "MarioTanuki", "Batman", "Link" };

        // The ledge: everybody jumps from its edge at z = 0, at this height (metres) plus the turf.
        const float LedgeHeight = 40f;
        const float TurfThickness = 0.3f;
        const float LedgeWidth = 24f;
        const float LedgeDepth = 24f;

        // A river crossing the lane: a clear "fell short" mark half way.
        const float RiverZ = 90f;
        const float RiverWidth = 8f;

        // The Zelda extra's updraft over Link's lane: centre along the lane, length and lift (m/s).
        const float UpdraftZ = 35f;
        const float UpdraftLength = 20f;
        const float UpdraftLift = 6f;

        // Distance markers: a thin line every 5 m and a sign every 25 m, up to this distance.
        const float LaneHalfWidth = 8f;
        const float MarkersUpTo = 250f;

        // Side of the ground square, centred on the lane (5 m checker cells).
        const float GroundSize = 700f;
        const float GroundCentreZ = 150f;

        // ------------------------------------------------------------------ menu

        [MenuItem("Tools/Gliding/Build Valley Assets")]
        static void BuildAssets()
        {
            foreach (var folder in new[] { EnvMaterials, EnvPrefabs, DebugMaterials, Profiles })
                EnsureFolder(folder);

            SavePrefab(BuildValley(new Palette()), EnvPrefabs + "/Valley.prefab");
            BuildDebugMaterials();
            BuildProfiles();
            AssetDatabase.SaveAssets();
            Debug.Log("[ValleyBuilder] Assets generated.");
        }

        [MenuItem("Tools/Gliding/Build Valley Scene")]
        static void BuildScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Build Valley Scene", ScenePath + " already exists. Overwrite it?", "Overwrite", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            BuildAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = Object.FindAnyObjectByType<Light>();
            // Sun from the side camera's side, so the faces it sees are lit.
            light.transform.rotation = Quaternion.Euler(45f, -60f, 0f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            // Distance haze: the mountains fade into the sky, the lane stays sharp.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.74f, 0.84f, 0.93f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 520f;

            // Side camera. Placed here: the overview, every flight from the ledge to ~245 m in one
            // shot. In Play it follows the character from close by (key C goes through the shots).
            var camera = Camera.main;
            camera.name = "SideCamera";
            camera.transform.position = new Vector3(165f, 22f, 112f);
            camera.transform.LookAt(new Vector3(0f, 14f, 112f));
            camera.fieldOfView = 50f;
            camera.farClipPlane = 1000f;
            var side = camera.gameObject.AddComponent<SideFollowCamera>();

            // Behind the gliders, looking down the lane: where the chase camera starts. Off until
            // its shot comes; tagged MainCamera too, so what faces the camera faces it when it is on.
            var behind = new GameObject("ChaseCamera").AddComponent<Camera>();
            behind.tag = "MainCamera";
            behind.transform.position = new Vector3(0f, LedgeHeight + 8f, -20f);
            behind.transform.LookAt(new Vector3(0f, LedgeHeight - 15f, 60f));
            behind.fieldOfView = 60f;
            behind.farClipPlane = 1000f;
            var chase = behind.gameObject.AddComponent<ChaseCamera>();
            behind.gameObject.SetActive(false);

            var shots = new GameObject("Cameras").AddComponent<CameraShots>();
            var shotsSo = new SerializedObject(shots);
            shotsSo.FindProperty("side").objectReferenceValue = side;
            shotsSo.FindProperty("chase").objectReferenceValue = chase;
            shotsSo.ApplyModifiedPropertiesWithoutUndo();

            var valley = Place(EnvPrefabs + "/Valley.prefab", Vector3.zero, 0f);
            var characters = new GameObject[Gliders.Length];
            for (int i = 0; i < Gliders.Length; i++)
            {
                // In a row, a few metres back from the edge, facing the lane (pivot 1 m above the feet).
                // The first one on the +X end, nearest the side camera, so the others never hide it.
                float x = ((Gliders.Length - 1) / 2f - i) * 3f;
                characters[i] = Place($"{Root}/Prefabs/Characters/{Gliders[i]}.prefab", new Vector3(x, LedgeHeight + TurfThickness + 1f, -4f), 0f);
            }
            BuildUpdraft(characters[System.Array.IndexOf(Gliders, "Link")].transform.position.x);
            BuildFlight(valley, characters);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[ValleyBuilder] Scene saved at " + ScenePath);
        }

        // ------------------------------------------------------------------ demo

        /// <summary>One profile per method, created once: after that they are tuned by hand (and
        /// regenerating never overwrites the numbers).</summary>
        static void BuildProfiles()
        {
            Profile<FixedDescentProfile>("Spyro_DescensoFijo", "Descenso fijo", new Color(0.62f, 0.36f, 0.95f));   // Spyro's purple
            // Tanuki orange-brown: Mario's red is taken by the vertical arrow.
            Profile<ReducedGravityProfile>("Mario_CaidaFrenada", "Caída frenada", new Color(0.95f, 0.52f, 0.15f));
            // Night blue, almost black: Batman, and it still stands out on the green.
            Profile<DiveGlideProfile>("Batman_Picado", "Física de picado", new Color(0.12f, 0.16f, 0.32f));
            // The champion's tunic blue: lighter than Batman's, so the two never get mixed up.
            Profile<ParagliderProfile>("Link_Paravela", "Paravela", new Color(0.25f, 0.55f, 1f), tires: true);
        }

        /// <summary>The Zelda extra's updraft: a column of rising air over Link's lane only (the
        /// others never cross it), from the valley floor to above the ledge.</summary>
        static void BuildUpdraft(float laneX)
        {
            var zone = new GameObject("Updraft").AddComponent<UpdraftZone>();
            zone.transform.position = new Vector3(laneX, 0f, UpdraftZ);
            var so = new SerializedObject(zone);
            so.FindProperty("size").vector3Value = new Vector3(2.5f, LedgeHeight + 20f, UpdraftLength);
            so.FindProperty("liftSpeed").floatValue = UpdraftLift;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Profile<T>(string file, string displayName, Color color, bool tires = false) where T : GlideProfile
        {
            string path = $"{Profiles}/{file}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

            var profile = ScriptableObject.CreateInstance<T>();
            var so = new SerializedObject(profile);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("color").colorValue = color;
            so.FindProperty("tires").boolValue = tires;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, path);
        }

        /// <summary>Unlit, so the debug drawing keeps its exact colour in any light.</summary>
        static void BuildDebugMaterials()
        {
            string dot = DebugMaterials + "/TrailDot.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(dot) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")), dot);

            // Lines take their colour from the LineRenderer (vertex colours): the sprite shader does that.
            string line = DebugMaterials + "/Arrow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(line) == null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")), line);
        }

        /// <summary>The composition root, wired: the valley as ground and the characters that
        /// already have a method.</summary>
        static void BuildFlight(GameObject valley, GameObject[] characters)
        {
            var flight = new GameObject("Flight").AddComponent<FlightBootstrap>();
            var so = new SerializedObject(flight);
            so.FindProperty("solids").objectReferenceValue = valley.transform;
            so.FindProperty("cameras").objectReferenceValue = Object.FindAnyObjectByType<CameraShots>();

            // The drawing comes with it (RequireComponent): it only needs its materials.
            var drawing = new SerializedObject(flight.GetComponent<FlightDrawing>());
            drawing.FindProperty("dotMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DebugMaterials + "/TrailDot.mat");
            drawing.FindProperty("lineMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(DebugMaterials + "/Arrow.mat");
            drawing.ApplyModifiedPropertiesWithoutUndo();

            var slots = so.FindProperty("gliders");
            slots.arraySize = characters.Length;
            for (int i = 0; i < characters.Length; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("character").objectReferenceValue = characters[i] != null ? characters[i].transform : null;
                slot.FindPropertyRelative("profile").objectReferenceValue = ProfileFor(Gliders[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GlideProfile ProfileFor(string character) => character switch
        {
            "Spyro" => AssetDatabase.LoadAssetAtPath<GlideProfile>(Profiles + "/Spyro_DescensoFijo.asset"),
            "MarioTanuki" => AssetDatabase.LoadAssetAtPath<GlideProfile>(Profiles + "/Mario_CaidaFrenada.asset"),
            "Batman" => AssetDatabase.LoadAssetAtPath<GlideProfile>(Profiles + "/Batman_Picado.asset"),
            "Link" => AssetDatabase.LoadAssetAtPath<GlideProfile>(Profiles + "/Link_Paravela.asset"),
            _ => null,
        };

        // ------------------------------------------------------------------ art

        /// <summary>All the colours of the demo in one place, picked to read well on video.</summary>
        sealed class Palette
        {
            public readonly Material RockLight = Mat(EnvMaterials, "RockLight", new Color(0.66f, 0.56f, 0.46f), 0.1f);
            public readonly Material RockDark = Mat(EnvMaterials, "RockDark", new Color(0.55f, 0.45f, 0.37f), 0.1f);
            public readonly Material Turf = Mat(EnvMaterials, "Turf", new Color(0.42f, 0.70f, 0.32f), 0.05f);
            public readonly Material Grass = GrassMaterial();
            public readonly Material Marker = Mat(EnvMaterials, "Marker", new Color(0.97f, 0.97f, 0.95f), 0.1f);
            public readonly Material SignWood = Mat(EnvMaterials, "SignWood", new Color(0.45f, 0.29f, 0.16f), 0.15f);
            public readonly Material Water = Mat(EnvMaterials, "Water", new Color(0.25f, 0.55f, 0.85f), 0.85f);
            public readonly Material Trunk = Mat(EnvMaterials, "Trunk", new Color(0.40f, 0.25f, 0.13f), 0.15f);
            public readonly Material Leaves = Mat(EnvMaterials, "Leaves", new Color(0.22f, 0.55f, 0.25f), 0.1f);
            public readonly Material Mountain = Mat(EnvMaterials, "Mountain", new Color(0.42f, 0.52f, 0.50f), 0.05f);
        }

        /// <summary>The whole set. Pivot at the foot of the ledge's edge, lane along +Z.</summary>
        static GameObject BuildValley(Palette p)
        {
            var valley = new GameObject("Valley");
            Box("Ground", valley.transform, new Vector3(0f, -0.1f, GroundCentreZ), new Vector3(GroundSize, 0.2f, GroundSize), p.Grass, keepCollider: true);

            BuildLedge(valley.transform, p);
            BuildMarkers(valley.transform, p);
            BuildRiver(valley.transform, p);
            BuildScenery(valley.transform, p);
            return valley;
        }

        /// <summary>The ledge: a block of rock in layers (they read as height on video) with a turf
        /// top and a white take-off line at the very edge.</summary>
        static void BuildLedge(Transform parent, Palette p)
        {
            var ledge = new GameObject("Ledge").transform;
            ledge.SetParent(parent, false);

            // How far each layer's front sticks out (+) or in (-). The top one is exactly at z = 0,
            // and none sticks out more than a metre, so no glider can clip the cliff on the way out.
            float[] front = { 0.6f, -0.8f, 0.9f, -0.4f, 0.5f, -0.9f, 0.3f, 0f };
            float layer = LedgeHeight / front.Length;
            for (int i = 0; i < front.Length; i++)
            {
                float depth = LedgeDepth + front[i];
                float width = LedgeWidth + (i % 2 == 0 ? 1.5f : 0f);
                Box("Layer" + i, ledge, new Vector3(0f, layer * (i + 0.5f), front[i] - depth / 2f),
                    new Vector3(width, layer, depth), i % 2 == 0 ? p.RockLight : p.RockDark, keepCollider: true);
            }
            Box("Turf", ledge, new Vector3(0f, LedgeHeight + 0.15f, -LedgeDepth / 2f), new Vector3(LedgeWidth, 0.3f, LedgeDepth), p.Turf, keepCollider: true);
            Box("TakeOffLine", ledge, new Vector3(0f, LedgeHeight + 0.31f, -0.3f), new Vector3(LedgeWidth, 0.02f, 0.4f), p.Marker);
        }

        /// <summary>A line across the lane every 5 m (wider every 25 m) and, every 25 m, a sign
        /// facing the side camera with the distance from the edge of the ledge.</summary>
        static void BuildMarkers(Transform parent, Palette p)
        {
            var markers = new GameObject("DistanceMarkers").transform;
            markers.SetParent(parent, false);

            for (float z = 5f; z <= MarkersUpTo; z += 5f)
            {
                if (Mathf.Abs(z - RiverZ) < RiverWidth / 2f + 1f) continue;   // no lines over the water
                bool major = z % 25f == 0f;
                Box("Line" + z, markers, new Vector3(0f, 0.01f, z), new Vector3(LaneHalfWidth * 2f, 0.02f, major ? 0.5f : 0.15f), p.Marker);
                if (major) Sign(markers, z, p);
            }
        }

        static void Sign(Transform parent, float z, Palette p)
        {
            var sign = new GameObject("Sign" + z).transform;
            sign.SetParent(parent, false);
            // On the far side of the lane (-X): seen behind the characters, never in front of the camera.
            sign.localPosition = new Vector3(-(LaneHalfWidth + 2f), 0f, z);

            Cylinder("Post", sign, new Vector3(0f, 1.5f, 0f), 0.15f, 3f, p.SignWood, upright: true);
            Box("Board", sign, new Vector3(0f, 3.8f, 0f), new Vector3(0.2f, 2.6f, 6f), p.SignWood);

            // Text on the +X face, facing the side camera, reading towards +Z (left to right on screen).
            var text = new GameObject("Label").AddComponent<TextMesh>();
            text.transform.SetParent(sign, false);
            text.transform.localPosition = new Vector3(0.11f, 3.8f, 0f);
            text.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            text.text = $"{z:0} m";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 96;
            text.characterSize = 0.18f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(0.98f, 0.96f, 0.9f);
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        static void BuildRiver(Transform parent, Palette p)
        {
            var river = new GameObject("River").transform;
            river.SetParent(parent, false);
            Box("Water", river, new Vector3(0f, 0.02f, RiverZ), new Vector3(GroundSize, 0.04f, RiverWidth), p.Water);
            foreach (float side in new[] { -1f, 1f })
                Box("Bank", river, new Vector3(0f, 0.03f, RiverZ + side * (RiverWidth / 2f + 0.4f)), new Vector3(GroundSize, 0.06f, 0.8f), p.RockLight);
        }

        /// <summary>Trees, rocks and mountains. Tall things only on the -X side (behind the lane
        /// as the side camera sees it) and far away; on +X just low trees near the ground.</summary>
        static void BuildScenery(Transform parent, Palette p)
        {
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(parent, false);

            var random = new System.Random(4);   // fixed seed: the same valley every time
            for (int i = 0; i < 70; i++)
            {
                float z = Range(random, -40f, 300f);
                float x = Range(random, 14f, 90f) * (random.NextDouble() < 0.6 ? -1f : 1f);
                if (Mathf.Abs(z - RiverZ) < RiverWidth) continue;
                if (z < 4f && Mathf.Abs(x) < LedgeWidth) continue;   // not inside the ledge
                Tree(scenery, new Vector3(x, 0f, z), Range(random, 0.8f, 1.4f), p);
            }

            // Rock pillars behind the lane, like the ledge's neighbours.
            RockPillar(scenery, new Vector3(-40f, 0f, 40f), 30f, 12f, p);
            RockPillar(scenery, new Vector3(-55f, 0f, 115f), 22f, 14f, p);
            RockPillar(scenery, new Vector3(-38f, 0f, 190f), 26f, 10f, p);

            // Mountains: big flattened spheres half buried, far enough for the haze to soften them.
            for (int i = 0; i < 9; i++)
            {
                float x = -320f + i * 80f + Range(random, -20f, 20f);
                float size = Range(random, 120f, 200f);
                Sphere("Mountain", scenery, new Vector3(x, 0f, Range(random, 420f, 480f)), new Vector3(size, size * 0.6f, size), p.Mountain);
            }
            for (int i = 0; i < 5; i++)
            {
                float size = Range(random, 120f, 180f);
                Sphere("Mountain", scenery, new Vector3(Range(random, -260f, -220f), 0f, -40f + i * 90f), new Vector3(size, size * 0.55f, size), p.Mountain);
            }
        }

        static void RockPillar(Transform parent, Vector3 foot, float height, float width, Palette p)
        {
            var pillar = new GameObject("RockPillar").transform;
            pillar.SetParent(parent, false);
            pillar.localPosition = foot;
            int layers = Mathf.Max(2, Mathf.RoundToInt(height / 5f));
            float layer = height / layers;
            for (int i = 0; i < layers; i++)
                Box("Layer" + i, pillar, new Vector3(0f, layer * (i + 0.5f), 0f),
                    new Vector3(width + (i % 2 == 0 ? 1f : 0f), layer, width + (i % 2 == 0 ? 0f : 1f)), i % 2 == 0 ? p.RockLight : p.RockDark);
            Box("Turf", pillar, new Vector3(0f, height + 0.15f, 0f), new Vector3(width, 0.3f, width), p.Turf);
        }

        static void Tree(Transform parent, Vector3 position, float scale, Palette p)
        {
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.localPosition = position;
            tree.localScale = Vector3.one * scale;
            Cylinder("Trunk", tree, new Vector3(0f, 1.25f, 0f), 0.3f, 2.5f, p.Trunk, upright: true);
            Sphere("Leaves", tree, new Vector3(0f, 3.4f, 0f), Vector3.one * 3f, p.Leaves);
            Sphere("Leaves", tree, new Vector3(0.6f, 4.5f, 0.2f), Vector3.one * 2f, p.Leaves);
        }

        // ------------------------------------------------------------------ helpers

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool keepCollider = false)
        {
            var box = Primitive(PrimitiveType.Cube, name, parent, material, keepCollider);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            return box;
        }

        /// <summary>Unity's cylinder is 2 units tall along Y; lying down it points along +Z.</summary>
        static void Cylinder(string name, Transform parent, Vector3 position, float radius, float length,
                             Material material, bool upright = false)
        {
            var cylinder = Primitive(PrimitiveType.Cylinder, name, parent, material, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localRotation = upright ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);
            cylinder.transform.localScale = new Vector3(radius * 2f, length / 2f, radius * 2f);
        }

        static void Sphere(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var sphere = Primitive(PrimitiveType.Sphere, name, parent, material, false);
            sphere.transform.localPosition = position;
            sphere.transform.localScale = size;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Place(string prefabPath, Vector3 position, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogError($"[ValleyBuilder] Missing {prefabPath}: bake it with the character kit."); return null; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return instance;
        }

        static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Material Mat(string folder, string name, Color color, float smoothness, Texture texture = null)
        {
            string path = $"{folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Two greens in a 5 m checkerboard with thin darker lines: seen from 40 m up the
        /// grid still reads, and it gives speed and distance a scale.</summary>
        static Material GrassMaterial()
        {
            const int size = 128, cell = size / 2, line = 2;
            const float tileMetres = 10f;   // two 5 m cells per texture tile
            string path = EnvMaterials + "/GrassGrid.png";
            var a = new Color32(108, 186, 84, 255);
            var b = new Color32(98, 174, 76, 255);
            var edge = new Color32(84, 152, 66, 255);

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool onLine = x % cell < line || y % cell < line;
                bool even = (x / cell + y / cell) % 2 == 0;
                pixels[y * size + x] = onLine ? edge : even ? a : b;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();

            var material = Mat(EnvMaterials, "Grass", Color.white, 0.05f, AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetTextureScale("_BaseMap", Vector2.one * GroundSize / tileMetres);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
