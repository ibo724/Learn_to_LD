#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VillaFPS.Editor;
using Object = UnityEngine.Object;

/// <summary>Creates the FPS edition from the author's saved blockout without applying prefab changes.</summary>
public static class VillaSceneBuilder
{
    public const string SourceScene = "Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity";
    public const string GameScene = "Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity";
    private const string RootPath = "Assets/_LevelProject/FPS";
    private static Material plaster, stone, trim, copper, ivory, foliage, terracotta, glass, gold, dark, tracer;
    private static Transform decoration;
    private static Terrain terrain;
    private static string sourceHashBefore;

    [MenuItem("Tools/Villa FPS/Prepare game scene")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before preparing the scene.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (string folder in new[] { "Scenes", "Materials", "Prefabs", "Meshes", "Terrain", "Navigation", "Audio" }) EnsureFolder(RootPath + "/" + folder);
        BackupGeneratedScene();
        sourceHashBefore = SourceHash();
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        // Change the scene's destination before editing its contents.
        EditorSceneManager.SaveScene(scene, GameScene);
        ConfigureLayers();
        Materials();
        HidePracticeObjects();
        StyleArchitecture();
        terrain = Object.FindFirstObjectByType<Terrain>();
        StyleTerrain();
        decoration = new GameObject("FPS Environment").transform;
        Lighting();
        Garden();
        Interior();
        Pool();
        SetupDoors();

        GameObject rig = new GameObject("Villa Mission");
        VillaMission mission = rig.AddComponent<VillaMission>();
        mission.renderPipeline = VillaRendering.CreatePipeline();
        mission.sceneCamera = Camera.main;
        if (mission.sceneCamera == null)
        {
            mission.sceneCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mission.sceneCamera.tag = "MainCamera";
        }
        if (mission.sceneCamera.GetComponent<AudioListener>() == null) mission.sceneCamera.gameObject.AddComponent<AudioListener>();
        mission.sceneCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        mission.sceneCamera.nearClipPlane = .05f;
        mission.sceneCamera.farClipPlane = 180f;
        mission.sceneCamera.clearFlags = CameraClearFlags.Skybox;
        mission.sceneCamera.cullingMask = ~(1 << VillaQueries.WeaponLayer);
        mission.accent = VillaArt.Turquoise;
        mission.spawn = Marker("Player Spawn", new Vector3(138.05f, WalkableHeight(138.05f, -17.85f) + .04f, -17.85f), rig.transform);
        mission.spawn.rotation = Quaternion.LookRotation(new Vector3(.05f, 0, 1));
        mission.sceneCamera.transform.SetPositionAndRotation(mission.spawn.position + Vector3.up * 1.65f, mission.spawn.rotation);
        mission.objective = Objective(rig.transform);
        mission.extraction = Marker("Extraction", new Vector3(127.6f, Ground(127.6f, -16.4f) + .05f, -16.4f), rig.transform);
        Extraction(mission.extraction);
        mission.cues = VillaAudioFactory.CreateCues();
        mission.weaponPrefab = VillaModelFactory.Weapon(dark, copper, ivory, tracer);
        GameObject guardPrefab = VillaModelFactory.Guard(trim, terracotta, gold, dark);
        NavMeshSurface surface = BakeNavigation(rig);
        Vector3[] locations =
        {
            new Vector3(145f, Ground(145f,-13.8f), -13.8f),
            new Vector3(130f, Ground(130f,-3.2f), -3.2f),
            new Vector3(141f,.55f,-9.8f), new Vector3(134.3f,.55f,-2.7f),
            new Vector3(140.2f,.55f,-2.45f), new Vector3(141.2f,3.65f,-8.2f),
            new Vector3(134.2f,3.65f,-10.6f), new Vector3(128.9f,Ground(128.9f,-12.4f),-12.4f)
        };
        float[] facing = { 245, 180, 270, 180, 180, 270, 0, 180 };
        List<VillaGuard> guards = new List<VillaGuard>();
        for (int i = 0; i < locations.Length; i++)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(guardPrefab, scene);
            obj.name = "Guard " + (i+1).ToString("00"); obj.transform.SetParent(rig.transform);
            Vector3 point = OnNavigation(locations[i]);
            obj.transform.SetPositionAndRotation(point, Quaternion.Euler(0,facing[i],0));
            VillaGuard guard = obj.GetComponent<VillaGuard>();
            guard.shotClip = mission.cues[0]; guard.alertClip = mission.cues[7]; guard.tracerMaterial = tracer;
            guard.footstepClip = mission.cues[6];
            // Short routes stay within the assigned room or garden lane.
            Vector3 routeOffset = i == 0 ? new Vector3(0,0,2.1f) : i == 1 ? new Vector3(0,0,2f)
                : i == 5 ? new Vector3(0,0,1.2f) : new Vector3(.55f,0,0);
            Vector3 second = OnNavigation(point + routeOffset);
            guard.patrol = new[] { point, second };
            guards.Add(guard);
        }
        mission.guards = guards.ToArray();
        mission.reserveGuard = guards[7]; mission.reserveGuard.gameObject.SetActive(false);
        Pickup("Garden Health", new Vector3(129.5f, Ground(129.5f,-1.5f), -1.5f), true, rig.transform);
        Pickup("Hall Ammo", new Vector3(139.7f,.55f,-7.0f), false, rig.transform);
        Pickup("Upper Health", new Vector3(134f,3.65f,-10.0f), true, rig.transform);
        Pickup("Upper Ammo", new Vector3(138f,3.65f,-4.5f), false, rig.transform);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScene, true) };
        AssetDatabase.SaveAssets();
        WritePreparationReport(mission, surface);
        Debug.Log("VILLA_FPS_SCENE_READY " + GameScene);
    }

    [MenuItem("Tools/Villa FPS/Open game scene")]
    public static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(GameScene);
    }

    private static void ConfigureLayers()
    {
        SerializedObject settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = settings.FindProperty("layers");
        string[] names = { "Villa Player", "Villa Guard", "Villa Glass", "Villa Viewmodel" };
        for (int i=0;i<names.Length;i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(8+i);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != names[i])
                throw new InvalidOperationException("Layer " + (8+i) + " is occupied by " + layer.stringValue);
            layer.stringValue = names[i];
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Materials()
    {
        plaster = VillaArt.Mat("Plaster",VillaArt.Plaster);
        stone = VillaArt.Mat("Limestone",VillaArt.Stone);
        ivory = VillaArt.Mat("Ivory",VillaArt.Ivory);
        trim = VillaArt.Mat("Petroleum",VillaArt.Petroleum);
        copper = VillaArt.Mat("Copper",VillaArt.Copper);
        dark = VillaArt.Mat("Ink",VillaArt.Ink);
        foliage = VillaArt.Mat("Leaves",VillaArt.Olive);
        terracotta = VillaArt.Mat("Terracotta",VillaArt.Terracotta);
        glass = VillaArt.Glass("Architectural Glass",VillaArt.Turquoise,.16f);
        gold = VillaArt.Mat("Signal",VillaArt.Hex("D7B35A"));
        Material freshTracer = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        freshTracer.SetColor("_BaseColor",VillaArt.Hex("F2BB67"));
        tracer = VillaArt.SaveMaterial(freshTracer,"Shot Trace");
    }

    private static void HidePracticeObjects()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "LEVEL" || root.name == "ScaleRef" || root.name == "Taarruzcu"
                || root.name == "Tuzakçı" || root.name == "Ninja" || root.name == "Pusucu"
                || (root.name.StartsWith("Cube",StringComparison.Ordinal) && root.transform.position.x < 100)) root.SetActive(false);
        }
    }

    private static void StyleArchitecture()
    {
        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!(renderer is MeshRenderer)) continue;
            string path = HierarchyPath(renderer.transform);
            Material chosen = null;
            if (path.StartsWith("Villa/",StringComparison.Ordinal))
            {
                Bounds b = renderer.bounds;
                bool horizontal = b.size.y < .4f && b.size.x > .7f && b.size.z > .7f;
                chosen = horizontal || renderer.name.Contains("Stairs") ? stone : plaster;
                if (renderer.name == "Cube (28)") chosen = ivory;
            }
            else if (path.Contains("Window_Frame") || path.Contains("Door") || path.Contains("Indoor_Root"))
            {
                bool isGlass = renderer.sharedMaterials.Any(m => m != null && m.name.Contains("Glass"));
                chosen = isGlass ? glass : renderer.name == "Door_Leaf" ? trim : copper;
                if (isGlass)
                {
                    renderer.gameObject.layer = VillaQueries.GlassLayer;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            else if (path.Contains("Garden_Path")) chosen = stone;
            else if (path.Contains("Garden_Pool")) chosen = ivory;
            if (chosen == null) continue;
            renderer.sharedMaterials = Enumerable.Repeat(chosen, renderer.sharedMaterials.Length).ToArray();
        }
    }

    private static void StyleTerrain()
    {
        if (terrain == null) throw new InvalidOperationException("Garden terrain is missing.");
        TerrainData copy = Object.Instantiate(terrain.terrainData);
        copy.name = "Villa Garden";
        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(RootPath + "/Terrain/Villa Garden.asset");
        if (existing == null) { AssetDatabase.CreateAsset(copy, RootPath + "/Terrain/Villa Garden.asset"); existing = copy; }
        else { EditorUtility.CopySerialized(copy,existing); Object.DestroyImmediate(copy); }
        TerrainLayer[] original = existing.terrainLayers;
        TerrainLayer[] layers = new TerrainLayer[original.Length];
        for (int i=0;i<layers.Length;i++)
        {
            string path = RootPath + "/Terrain/Garden Layer " + i + ".terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer,path); }
            layer.diffuseTexture = Swatch(i == 0 ? "Grass" : "Soil",i == 0 ? VillaArt.Hex("89986B") : VillaArt.Hex("A28B68"));
            layer.normalMapTexture = null; layer.maskMapTexture = null;
            layer.tileSize = new Vector2(3,3); layer.metallic = 0; layer.smoothness = 0;
            layers[i] = layer;
        }
        existing.terrainLayers = layers; terrain.terrainData = existing;
        TerrainCollider tc = terrain.GetComponent<TerrainCollider>(); if (tc != null) tc.terrainData = existing;
        terrain.drawInstanced = true; terrain.basemapDistance = 100f;
        EditorUtility.SetDirty(existing);
    }

    private static Texture2D Swatch(string name,Color color)
    {
        string path = RootPath + "/Terrain/" + name + ".asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) { texture = new Texture2D(32,32,TextureFormat.RGB24,false) { name=name,wrapMode=TextureWrapMode.Repeat }; AssetDatabase.CreateAsset(texture,path); }
        Color[] pixels = new Color[1024];
        for (int y=0;y<32;y++) for (int x=0;x<32;x++)
        {
            float amount = ((x/8 + y/8*7)%3-1)*.025f;
            pixels[y*32+x] = new Color(color.r+amount,color.g+amount,color.b+amount);
        }
        texture.SetPixels(pixels); texture.Apply(); texture.filterMode = FilterMode.Bilinear;
        EditorUtility.SetDirty(texture); return texture;
    }

    private static void Lighting()
    {
        RenderSettings.skybox = VillaArt.Sky("Garden Sky");
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = VillaArt.Hex("C3D1C8");
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = VillaArt.Hex("DDDCC1"); RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 100;
        Light sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
        if (sun == null) sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = VillaArt.Hex("FFF0CD"); sun.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(48,-35,0); sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .7f; RenderSettings.sun = sun;
        Volume volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null) volume = new GameObject("FPS Grade").AddComponent<Volume>();
        string path = RootPath + "/Materials/Camera Grade.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,path); }
        foreach (VolumeComponent old in profile.components.ToArray()) Object.DestroyImmediate(old,true);
        profile.components.Clear();
        ColorAdjustments adjustments = profile.Add<ColorAdjustments>(true);
        adjustments.contrast.Override(6); adjustments.saturation.Override(-4);
        Tonemapping tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.Neutral);
        foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        volume.sharedProfile = profile; volume.isGlobal = true; volume.weight = 1f;
        EditorUtility.SetDirty(profile);
    }

    private static void Garden()
    {
        // Fences are visible collision boundaries, rather than invisible world limits.
        Vector3 origin = terrain.transform.position, size = terrain.terrainData.size;
        float minX = origin.x+.45f, maxX = origin.x+size.x-.45f, minZ = origin.z+.45f, maxZ = origin.z+size.z-.45f;
        for (int side=0;side<4;side++)
        {
            Vector3 a = side == 0 ? new Vector3(minX,0,minZ) : side == 1 ? new Vector3(maxX,0,minZ)
                : side == 2 ? new Vector3(minX,0,minZ) : new Vector3(minX,0,maxZ);
            Vector3 b = side < 2 ? new Vector3(a.x,0,maxZ) : new Vector3(maxX,0,a.z);
            int pieces = Mathf.CeilToInt(Vector3.Distance(a,b)/2.8f);
            for (int i=0;i<pieces;i++)
            {
                Vector3 c = Vector3.Lerp(a,b,(i+.5f)/pieces); c.y = Ground(c.x,c.z)+1.4f;
                Vector3 dimensions = side < 2 ? new Vector3(.14f,2.8f,(maxZ-minZ)/pieces+.02f) : new Vector3((maxX-minX)/pieces+.02f,2.8f,.14f);
                Box("Boundary wall",c,dimensions,plaster);
                Vector3 post = Vector3.Lerp(a,b,(float)i/pieces); post.y=Ground(post.x,post.z)+1.48f;
                Box("Boundary post",post,new Vector3(.25f,2.96f,.25f),copper);
            }
        }
        // Front cornice and a narrow coloured band give the blockout a deliberate façade.
        Box("Front cornice",new Vector3(138.12f,6.39f,-12.17f),new Vector3(10.5f,.16f,.32f),trim,false);
        Box("Rear cornice",new Vector3(138.12f,6.39f,-1.31f),new Vector3(10.5f,.16f,.32f),trim,false);
        Box("Left cornice",new Vector3(132.94f,6.39f,-6.74f),new Vector3(.32f,.16f,10.6f),trim,false);
        Box("Right cornice",new Vector3(143.27f,6.39f,-6.74f),new Vector3(.32f,.16f,10.6f),trim,false);
        Label("Villa sign","VİLLA  /  01",new Vector3(140.8f,3.9f,-12.22f),Quaternion.identity,.1f,trim);
        Cover(new Vector3(145.1f,Ground(145.1f,-15.9f),-15.9f),0);
        Cover(new Vector3(130.5f,Ground(130.5f,-5.8f),-5.8f),90);
        Cover(new Vector3(148f,Ground(148f,-6),-6),0);
        Tree(126.5f,7.5f,1.25f); Tree(151.2f,7.6f,1.05f); Tree(150.6f,-16.4f,.9f); Tree(126.3f,-11f,.85f);
        for (int i=0;i<7;i++) { Bush(126.3f+i*.8f,8.4f,.7f); Bush(151.5f,-1f+i*.85f,.6f); }
        Planter(134.8f,-16.8f); Planter(141.8f,-15.1f); Planter(132f,.6f);
        Bench(new Vector3(133f,Ground(133f,4.5f),4.5f),90);
        Bench(new Vector3(149.2f,Ground(149.2f,4.4f),4.4f),-90);
        for (int i=0;i<5;i++)
        {
            float z = -15.6f + i*1.8f;
            Box("Garden stepping stone",new Vector3(129.1f,Ground(129.1f,z)+.025f,z),new Vector3(1.2f,.05f,1.1f),stone);
        }
    }

    private static void Interior()
    {
        // Furnishings are sparse, with generous clearance around all doorways and stairs.
        Box("Kitchen island",new Vector3(135.1f,1.05f,-5.1f),new Vector3(1.55f,1f,.75f),trim);
        Box("Kitchen counter",new Vector3(135.1f,1.58f,-5.1f),new Vector3(1.67f,.12f,.85f),ivory);
        Box("Lounge low cabinet",new Vector3(141.45f,1.1f,-11.15f),new Vector3(2f,1.1f,.55f),trim);
        Box("Lounge cabinet top",new Vector3(141.45f,1.7f,-11.15f),new Vector3(2.1f,.1f,.65f),stone);
        Box("Upper archive",new Vector3(133.6f,4.56f,-11.4f),new Vector3(.65f,1.8f,.65f),trim);
        Box("Upper archive band",new Vector3(133.6f,4.65f,-11.74f),new Vector3(.5f,.12f,.025f),copper,false);
        for (int i=0;i<3;i++) Box("Archive shelf",new Vector3(133.6f,4.05f+i*.48f,-11.4f),new Vector3(.72f,.06f,.72f),stone,false);
        Box("Data desk",new Vector3(141.6f,4.54f,-10.85f),new Vector3(1.6f,.12f,.7f),ivory);
        Box("Data desk left support",new Vector3(140.98f,4.05f,-10.85f),new Vector3(.13f,.9f,.58f),trim);
        Box("Data desk right support",new Vector3(142.22f,4.05f,-10.85f),new Vector3(.13f,.9f,.58f),trim);
        Label("Archive label","ARŞİV / 02",new Vector3(137.35f,5.9f,-9.81f),Quaternion.identity,.07f,trim);
    }

    private static Transform Objective(Transform parent)
    {
        Transform root = Marker("Data Device",new Vector3(141.6f,4.84f,-10.85f),parent);
        Box("Device body",root.position,new Vector3(.48f,.35f,.37f),dark,true,root);
        Box("Device face",root.position+new Vector3(0,.025f,-.195f),new Vector3(.38f,.22f,.025f),gold,false,root);
        for (int i=0;i<3;i++) Box("Data indicator",root.position+new Vector3(-.1f+i*.1f,.025f,-.215f),new Vector3(.045f,.09f,.015f),ivory,false,root);
        Box("Device handle",root.position+new Vector3(0,.23f,0),new Vector3(.3f,.05f,.08f),copper,false,root);
        Label("Data instruction","VERİ / E",root.position+new Vector3(-.25f,.48f,0),Quaternion.identity,.04f,trim,root);
        return root;
    }

    private static void Extraction(Transform marker)
    {
        Box("Exit platform",marker.position+Vector3.up*.025f,new Vector3(2.8f,.05f,2.8f),trim);
        for (int i=0;i<4;i++)
        {
            float angle = i*90*Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*1.25f;
            Box("Exit corner",marker.position+offset+Vector3.up*.075f,new Vector3(.35f,.1f,.35f),gold,false);
        }
        Vector3 sign = marker.position + new Vector3(-1.5f,1.4f,.25f);
        Box("Exit beacon post",sign-Vector3.up*.5f,new Vector3(.12f,1.7f,.12f),trim);
        Box("Exit beacon",sign+Vector3.up*.2f,new Vector3(.65f,.45f,.15f),trim);
        Label("Exit label","ÇIKIŞ",sign+new Vector3(-.26f,.2f,-.09f),Quaternion.identity,.06f,ivory);
    }

    private static void Pickup(string name,Vector3 feet,bool health,Transform parent)
    {
        Transform root = Marker(name,feet+Vector3.up*.5f,parent);
        SphereCollider trigger = root.gameObject.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .65f;
        Rigidbody body = root.gameObject.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        VillaPickup item = root.gameObject.AddComponent<VillaPickup>(); item.health = health;
        Transform visual = new GameObject("Visual").transform; visual.SetParent(root,false); item.visual = visual;
        Box("Pickup case",root.position,new Vector3(.42f,.32f,.25f),health ? ivory : trim,false,visual);
        Material mark = health ? terracotta : gold;
        Box("Pickup marker",root.position+new Vector3(0,0,-.14f),new Vector3(.24f,.07f,.025f),mark,false,visual);
        if (health) Box("Health cross",root.position+new Vector3(0,0,-.155f),new Vector3(.07f,.24f,.025f),mark,false,visual);
        else for (int i=0;i<3;i++) Box("Ammo mark",root.position+new Vector3(-.08f+i*.08f,.07f,-.15f),new Vector3(.035f,.12f,.025f),gold,false,visual);
    }

    private static void Pool()
    {
        GameObject pool = GameObject.Find("Garden_Pool");
        if (pool == null) return;
        Renderer bottom = pool.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.name == "Pool_Bottom");
        if (bottom == null) return;
        Bounds bounds = bottom.bounds;
        float y = pool.transform.position.y;
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Pool Water"; water.transform.SetParent(decoration);
        water.transform.position = new Vector3(bounds.center.x,y-.08f,bounds.center.z);
        water.transform.localScale = new Vector3((bounds.size.x-.6f)/10f,1,(bounds.size.z-.6f)/10f);
        Object.DestroyImmediate(water.GetComponent<Collider>());
        Renderer renderer = water.GetComponent<Renderer>();
        renderer.sharedMaterial = VillaArt.Water("Pool Water",VillaArt.Turquoise,VillaArt.Hex("287B87"));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        for (int side=0;side<4;side++)
        {
            bool alongX = side < 2;
            Vector3 center = new Vector3(bounds.center.x,y+.12f,bounds.center.z);
            if (alongX) center.z = side == 0 ? bounds.min.z : bounds.max.z;
            else center.x = side == 2 ? bounds.min.x : bounds.max.x;
            Vector3 size = alongX ? new Vector3(bounds.size.x+.5f,.18f,.5f) : new Vector3(.5f,.18f,bounds.size.z-.5f);
            Box("Pool coping",center,size,stone);
            Vector3 fence = center+Vector3.up*.58f;
            if (alongX) fence.z += side == 0 ? -.35f : .35f; else fence.x += side == 2 ? -.35f : .35f;
            Box("Pool glass safety panel",fence,alongX ? new Vector3(bounds.size.x+.6f,1.1f,.045f) : new Vector3(.045f,1.1f,bounds.size.z+.6f),glass);
            Vector3 rail = fence+Vector3.up*.56f;
            Box("Pool safety rail",rail,alongX ? new Vector3(bounds.size.x+.6f,.045f,.08f) : new Vector3(.08f,.045f,bounds.size.z+.6f),copper);
            int posts = alongX ? 4 : 3;
            for (int i=0;i<=posts;i++)
            {
                Vector3 post=fence;
                if (alongX) post.x = bounds.min.x-.3f+(bounds.size.x+.6f)*i/posts;
                else post.z = bounds.min.z-.3f+(bounds.size.z+.6f)*i/posts;
                Box("Pool post",post,new Vector3(.07f,1.15f,.07f),trim);
            }
        }
        // Dark tile lanes remain visible through the transparent surface.
        for (int i=0;i<3;i++)
            Box("Pool lane",new Vector3(bounds.center.x,bounds.max.y+.01f,bounds.center.z-1.1f+i*1.1f),new Vector3(bounds.size.x-.8f,.01f,.1f),trim,false);
    }

    private static void SetupDoors()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if ((t.name == "DoorHinge" || t.name == "Door_Hinge") && t.Find("Door_Leaf") != null)
            { if (t.GetComponent<LevelDoor>() == null) t.gameObject.AddComponent<LevelDoor>(); }
            if (t.name != "Door_Leaf") continue;
            BoxCollider box=t.GetComponent<BoxCollider>();
            if (box == null) continue;
            NavMeshObstacle obstacle = t.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle=t.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape=NavMeshObstacleShape.Box; obstacle.center=box.center; obstacle.size=box.size;
            obstacle.carving=true; obstacle.carveOnlyStationary=false; obstacle.carvingMoveThreshold=.03f; obstacle.carvingTimeToStationary=.1f;
            if (t.gameObject.layer != VillaQueries.GlassLayer) DoorHardware(t,box);
        }
    }

    private static void DoorHardware(Transform leaf,BoxCollider shape)
    {
        if (leaf.Find("Door hardware") != null || leaf.parent == null) return;
        Vector3 center=shape.center, half=shape.size*.5f;
        float left=Vector3.SqrMagnitude(leaf.TransformPoint(center-Vector3.right*half.x)-leaf.parent.position);
        float right=Vector3.SqrMagnitude(leaf.TransformPoint(center+Vector3.right*half.x)-leaf.parent.position);
        float freeSide=left > right ? -1f : 1f;
        float handleX=center.x+freeSide*(half.x-.14f);
        float handleY=center.y-half.y+Mathf.Min(1.05f,shape.size.y*.45f);
        Transform hardware=new GameObject("Door hardware").transform;
        hardware.SetParent(leaf,false);
        for (int side=-1;side<=1;side+=2)
        {
            float z=center.z+side*(half.z+.012f);
            DoorPart("Handle plate",hardware,new Vector3(handleX,handleY,z),new Vector3(.075f,.2f,.018f),copper);
            DoorPart("Handle lever",hardware,new Vector3(handleX-freeSide*.055f,handleY,z+side*.035f),new Vector3(.2f,.035f,.04f),ivory);
            for (int i=0;i<2;i++)
                DoorPart("Hinge plate",hardware,new Vector3(center.x-freeSide*(half.x-.025f),center.y+(i==0 ? -.65f : .65f),z),
                    new Vector3(.055f,.17f,.015f),copper);
        }
    }

    private static void DoorPart(string name,Transform parent,Vector3 localPosition,Vector3 size,Material material)
    {
        GameObject part=Box(name,parent.TransformPoint(localPosition),size,material,false,parent);
        part.transform.localRotation=Quaternion.identity;
        Vector3 inherited=parent.lossyScale;
        part.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,Mathf.Abs(inherited.x)),
            size.y/Mathf.Max(.001f,Mathf.Abs(inherited.y)),size.z/Mathf.Max(.001f,Mathf.Abs(inherited.z)));
    }

    private static NavMeshSurface BakeNavigation(GameObject root)
    {
        Physics.SyncTransforms();
        NavMeshSurface surface=root.AddComponent<NavMeshSurface>();
        surface.collectObjects=CollectObjects.All; surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask=~((1<<8)|(1<<9)|(1<<11));
        Bounds area=new Bounds(new Vector3(138.8f,3,-4.4f),new Vector3(32,12,32));
        List<NavMeshBuildMarkup> markup=new List<NavMeshBuildMarkup>();
        foreach (NavMeshObstacle obstacle in Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None))
        {
            Transform movingRoot = obstacle.transform.name == "Door_Leaf" && obstacle.transform.parent != null
                ? obstacle.transform.parent : obstacle.transform;
            markup.Add(new NavMeshBuildMarkup {root=movingRoot,ignoreFromBuild=true});
        }
        List<NavMeshBuildSource> sources=new List<NavMeshBuildSource>();
        UnityEngine.AI.NavMeshBuilder.CollectSources(area,surface.layerMask,NavMeshCollectGeometry.PhysicsColliders,0,markup,sources);
        NavMeshBuildSettings settings=NavMesh.GetSettingsByID(0);
        settings.agentRadius=.26f; settings.agentHeight=1.8f; settings.agentClimb=.32f; settings.agentSlope=45f;
        settings.overrideVoxelSize=true; settings.voxelSize=.07f; settings.overrideTileSize=true; settings.tileSize=128;
        settings.minRegionArea=.25f;
        NavMeshData data=UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings,sources,area,Vector3.zero,Quaternion.identity);
        if (data == null) throw new InvalidOperationException("Could not prepare navigation data.");
        string path=RootPath+"/Navigation/VillaNav.asset";
        NavMeshData existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (existing == null) { AssetDatabase.CreateAsset(data,path); existing=data; }
        else { EditorUtility.CopySerialized(data,existing); Object.DestroyImmediate(data); }
        surface.RemoveData(); surface.navMeshData=existing; surface.AddData();
        EditorUtility.SetDirty(existing);
        return surface;
    }

    private static Vector3 OnNavigation(Vector3 desired)
    {
        if (NavMesh.SamplePosition(desired,out NavMeshHit hit,1.1f,NavMesh.AllAreas)) return hit.position;
        Debug.LogWarning("No navigation surface near " + desired + "; guard will use stationary defence.");
        return desired;
    }

    private static void Cover(Vector3 feet,float angle)
    {
        Transform root=Marker("Supply cover",feet,decoration); root.rotation=Quaternion.Euler(0,angle,0);
        BoxLocal("Tall crate",root,new Vector3(0,.8f,0),new Vector3(1.2f,1.6f,.9f),trim);
        BoxLocal("Crate top",root,new Vector3(0,1.62f,0),new Vector3(1.27f,.08f,.97f),stone);
        BoxLocal("Crate copper stripe",root,new Vector3(0,.8f,-.46f),new Vector3(1.1f,.12f,.02f),copper,false);
        BoxLocal("Low crate",root,new Vector3(1.0f,.45f,.2f),new Vector3(.7f,.9f,.75f),copper);
    }
    private static void Bench(Vector3 feet,float angle)
    {
        Transform root=Marker("Garden bench",feet,decoration); root.rotation=Quaternion.Euler(0,angle,0);
        BoxLocal("Bench seat",root,new Vector3(0,.52f,0),new Vector3(1.9f,.12f,.6f),copper);
        BoxLocal("Bench back",root,new Vector3(0,.9f,.28f),new Vector3(1.9f,.55f,.1f),copper);
        BoxLocal("Bench leg",root,new Vector3(-.68f,.25f,0),new Vector3(.15f,.5f,.5f),trim);
        BoxLocal("Bench leg",root,new Vector3(.68f,.25f,0),new Vector3(.15f,.5f,.5f),trim);
    }
    private static void Planter(float x,float z)
    {
        float y=Ground(x,z);
        Box("Garden planter",new Vector3(x,y+.35f,z),new Vector3(.9f,.7f,.9f),terracotta);
        Box("Planter rim",new Vector3(x,y+.72f,z),new Vector3(.98f,.08f,.98f),ivory);
        Bush(x,z,.65f,y+.8f);
    }
    private static void Bush(float x,float z,float scale,float forcedY=float.NaN)
    {
        Vector3 center=new Vector3(x,float.IsNaN(forcedY) ? Ground(x,z)+scale*.4f : forcedY+scale*.4f,z);
        Facet("Shrub",center,new Vector3(scale,scale*.7f,scale),foliage);
    }
    private static void Tree(float x,float z,float scale)
    {
        float y=Ground(x,z);
        Box("Tree trunk",new Vector3(x,y+1.0f*scale,z),new Vector3(.25f*scale,2f*scale,.25f*scale),copper);
        Facet("Tree crown",new Vector3(x,y+2.1f*scale,z),new Vector3(1.5f*scale,1.2f*scale,1.4f*scale),foliage);
        Facet("Tree crown",new Vector3(x+.55f*scale,y+2.6f*scale,z+.1f),new Vector3(1.05f*scale,1f*scale,1.05f*scale),foliage);
    }
    private static void Facet(string name,Vector3 position,Vector3 size,Material material)
    {
        const string path=RootPath+"/Meshes/Garden Octahedron.asset";
        Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh=new Mesh {name="Garden Octahedron"};
            Vector3[] points={ Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back };
            int[] order={0,2,4,0,4,3,0,3,5,0,5,2,1,4,2,1,3,4,1,5,3,1,2,5};
            Vector3[] vertices=order.Select(i=>points[i]).ToArray();
            mesh.vertices=vertices; mesh.triangles=Enumerable.Range(0,vertices.Length).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,path);
        }
        GameObject obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        obj.transform.SetParent(decoration); obj.transform.position=position; obj.transform.localScale=size;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh; obj.GetComponent<Renderer>().sharedMaterial=material;
    }
    private static float Ground(float x,float z) => terrain == null ? .53f : terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
    private static float WalkableHeight(float x,float z)
    {
        Physics.SyncTransforms();
        float y=Ground(x,z);
        if (Physics.Raycast(new Vector3(x,y+1f,z),Vector3.down,out RaycastHit hit,2f,
            ~((1<<8)|(1<<9)|(1<<11)),QueryTriggerInteraction.Ignore) && hit.normal.y > .5f) return hit.point.y;
        return y;
    }
    private static Transform Marker(string name,Vector3 position,Transform parent)
    { Transform t=new GameObject(name).transform; t.SetParent(parent); t.position=position; return t; }
    private static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool solid=true,Transform parent=null)
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name=name;
        obj.transform.SetParent(parent != null ? parent : decoration); obj.transform.position=position; obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;
        if (decoration != null && obj.transform.IsChildOf(decoration))
            GameObjectUtility.SetStaticEditorFlags(obj,StaticEditorFlags.BatchingStatic);
        if (!solid) Object.DestroyImmediate(obj.GetComponent<Collider>());
        if (material == glass) { obj.layer=VillaQueries.GlassLayer; obj.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off; }
        return obj;
    }
    private static GameObject BoxLocal(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool solid=true)
    {
        GameObject obj=Box(name,parent.TransformPoint(position),size,material,solid,parent);
        obj.transform.localRotation=Quaternion.identity; return obj;
    }
    private static void Label(string name,string text,Vector3 position,Quaternion rotation,float characterSize,Material color,Transform parent=null)
    {
        GameObject obj=new GameObject(name); obj.transform.SetParent(parent != null ? parent : decoration);
        obj.transform.SetPositionAndRotation(position,rotation);
        TextMesh label=obj.AddComponent<TextMesh>(); label.text=text; label.characterSize=characterSize; label.fontSize=48;
        label.anchor=TextAnchor.MiddleLeft; label.color=color.GetColor("_BaseColor");
        label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        obj.GetComponent<Renderer>().sharedMaterial=label.font.material;
    }
    private static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent)+"/"+t.name;
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    [Serializable] private class PreparationReport
    {
        public string scene, source, notes, sourceHashBefore, sourceHashAfter;
        public int guards, pickups, navTriangles;
        public Vector3 spawn, objective, extraction;
        public string[] shaderErrors, guardNavigation;
    }
    private static void WritePreparationReport(VillaMission mission,NavMeshSurface surface)
    {
        List<string> shaderErrors=new List<string>();
        foreach (string name in new[]{"Villa/Matte","Villa/Water","Villa/Sky"})
        {
            Shader shader=Shader.Find(name);
            if (shader == null) { shaderErrors.Add("Missing: "+name); continue; }
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) shaderErrors.Add(name+": "+message.message);
        }
        PreparationReport report=new PreparationReport
        {
            scene=GameScene,source=SourceScene,guards=mission.guards.Length,
            sourceHashBefore=sourceHashBefore,sourceHashAfter=SourceHash(),
            pickups=Object.FindObjectsByType<VillaPickup>(FindObjectsSortMode.None).Length,
            navTriangles=NavMesh.CalculateTriangulation().indices.Length/3,
            spawn=mission.spawn.position,objective=mission.objective.position,extraction=mission.extraction.position,
            shaderErrors=shaderErrors.ToArray(),
            guardNavigation=mission.guards.Select(g=>g.name+": "+NavMesh.SamplePosition(g.transform.position,out NavMeshHit hit,.4f,NavMesh.AllAreas)).ToArray(),
            notes="Editor asset preparation only. No Play mode, no user-input automation, no gameplay verification."
        };
        Directory.CreateDirectory("Logs/VillaFPS"); File.WriteAllText("Logs/VillaFPS/preparation.json",JsonUtility.ToJson(report,true));
        if (shaderErrors.Count > 0) throw new InvalidOperationException(string.Join("\n",shaderErrors));
        if (report.navTriangles == 0) throw new InvalidOperationException("Navigation has no triangles.");
        if (report.sourceHashBefore != report.sourceHashAfter) throw new InvalidOperationException("Source scene changed during preparation.");
    }
    private static string SourceHash()
    { using (SHA256 hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(SourceScene))).Replace("-",""); }

    private static void BackupGeneratedScene()
    {
        if (!File.Exists(GameScene)) return;
        string digest;
        using (SHA256 hash=SHA256.Create()) digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(GameScene))).Replace("-","").Substring(0,12);
        const string folder="Logs/VillaFPS/SceneBackups";
        Directory.CreateDirectory(folder);
        string backup=folder+"/Villa_Operation-"+digest+".unity";
        if (!File.Exists(backup)) File.Copy(GameScene,backup);
    }
}
#endif
