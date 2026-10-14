#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace VillaFPS.Editor
{
    /// <summary>Small, deliberately faceted models made from reusable geometric pieces.</summary>
    public static class VillaModelFactory
    {
        private const string PrefabFolder = "Assets/_LevelProject/FPS/Prefabs";
        private const string MeshFolder = "Assets/_LevelProject/FPS/Meshes";
        private const int WeaponLayer = 11;

        public static GameObject Weapon(Material dark, Material copper, Material light, Material tracer)
        {
            EnsureFolder(PrefabFolder);
            GameObject root = new GameObject("Carbine") { layer = WeaponLayer };
            try
            {
                Transform model = Empty("Moving_Model", root.transform, new Vector3(.22f, -.25f, .4f));
                model.gameObject.layer = WeaponLayer;
                Transform muzzle = BuildRifle(model, dark, copper, light, true);
                BuildHands(model, dark, copper);

                Light flash = Empty("Muzzle_Flash", muzzle, Vector3.zero).gameObject.AddComponent<Light>();
                flash.type = LightType.Point;
                flash.color = VillaArt.Hex("FFD59A");
                flash.range = .45f;
                flash.intensity = 2.2f;
                flash.shadows = LightShadows.None;
                flash.cullingMask = 1 << WeaponLayer;
                flash.enabled = false;

                VillaWeapon weapon = root.AddComponent<VillaWeapon>();
                weapon.movingModel = model;
                weapon.muzzle = muzzle;
                weapon.muzzleLight = flash;
                weapon.tracerMaterial = tracer;
                SetWeaponRendering(root);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Carbine.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static GameObject Guard(Material uniform, Material armor, Material visor, Material dark)
        {
            EnsureFolder(PrefabFolder);
            GameObject root = new GameObject("Guard") { layer = VillaQueries.GuardLayer };
            try
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, .9f, 0);
                capsule.height = 1.8f;
                capsule.radius = .28f;
                capsule.direction = 1;

                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.agentTypeID = 0;
                agent.radius = .26f;
                agent.height = 1.8f;
                agent.baseOffset = 0;
                agent.speed = 2.2f;
                agent.acceleration = 12;
                agent.angularSpeed = 240;
                agent.stoppingDistance = .2f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                VillaHealth health = root.AddComponent<VillaHealth>();
                health.maximum = 100;
                AudioSource speaker = root.AddComponent<AudioSource>();
                speaker.playOnAwake = false;
                speaker.spatialBlend = 1;
                speaker.rolloffMode = AudioRolloffMode.Linear;
                speaker.minDistance = 1.5f;
                speaker.maxDistance = 20;
                speaker.dopplerLevel = 0;
                speaker.volume = .8f;

                Transform model = Empty("Model", root.transform, Vector3.zero);
                Mesh bevel = GetMesh("ChamferBox", ChamferBox);
                Mesh helmet = GetMesh("EightFacetHelmet", Helmet);
                Mesh torso = GetMesh("TaperedTorso", Torso);

                // Feet touch y=0; large blocks stay distinct at the villa's short combat distances.
                Part("Boot_Left", model, bevel, new Vector3(-.13f, .1f, .045f), new Vector3(.19f, .2f, .32f), dark);
                Part("Boot_Right", model, bevel, new Vector3(.13f, .1f, .045f), new Vector3(.19f, .2f, .32f), dark);
                Segment("Shin_Left", model, new Vector3(-.13f, .19f, 0), new Vector3(-.13f, .52f, 0), .155f, .19f, uniform);
                Segment("Shin_Right", model, new Vector3(.13f, .19f, 0), new Vector3(.13f, .52f, 0), .155f, .19f, uniform);
                Segment("Thigh_Left", model, new Vector3(-.13f, .52f, 0), new Vector3(-.12f, .87f, 0), .18f, .22f, uniform);
                Segment("Thigh_Right", model, new Vector3(.13f, .52f, 0), new Vector3(.12f, .87f, 0), .18f, .22f, uniform);
                Part("Knee_Left", model, bevel, new Vector3(-.13f, .52f, .105f), new Vector3(.15f, .15f, .055f), armor);
                Part("Knee_Right", model, bevel, new Vector3(.13f, .52f, .105f), new Vector3(.15f, .15f, .055f), armor);
                Part("Waist", model, bevel, new Vector3(0, .87f, 0), new Vector3(.38f, .17f, .27f), uniform);
                Part("Belt", model, bevel, new Vector3(0, .91f, 0), new Vector3(.43f, .085f, .3f), dark);
                Part("Belt_Buckle", model, bevel, new Vector3(0, .91f, .16f), new Vector3(.075f, .06f, .025f), armor);
                Part("Torso", model, torso, new Vector3(0, 1.18f, 0), new Vector3(.51f, .48f, .31f), uniform);
                Part("Chest_Armor", model, torso, new Vector3(0, 1.205f, .18f), new Vector3(.39f, .35f, .075f), armor);
                Part("Back_Armor", model, bevel, new Vector3(0, 1.22f, -.18f), new Vector3(.34f, .34f, .09f), dark);
                Part("Chest_Insignia", model, bevel, new Vector3(-.105f, 1.29f, .224f), new Vector3(.07f, .035f, .009f), visor);
                Part("Magazine_Pouch", model, bevel, new Vector3(-.135f, 1.015f, .2f), new Vector3(.09f, .14f, .08f), dark);
                Part("Radio_Pouch", model, bevel, new Vector3(.135f, 1.04f, .19f), new Vector3(.08f, .1f, .075f), uniform);
                Part("Neck", model, bevel, new Vector3(0, 1.455f, 0), new Vector3(.15f, .12f, .16f), dark);
                Part("Helmet", model, helmet, new Vector3(0, 1.57f, 0), new Vector3(.365f, .35f, .335f), armor);
                Part("Face_Shield", model, bevel, new Vector3(0, 1.585f, .153f), new Vector3(.265f, .12f, .035f), visor);
                Part("Lower_Face", model, bevel, new Vector3(0, 1.49f, .145f), new Vector3(.19f, .08f, .055f), dark);
                Part("Helmet_Ear_Left", model, bevel, new Vector3(-.178f, 1.57f, 0), new Vector3(.045f, .14f, .15f), dark);
                Part("Helmet_Ear_Right", model, bevel, new Vector3(.178f, 1.57f, 0), new Vector3(.045f, .14f, .15f), dark);

                Part("Shoulder_Left", model, bevel, new Vector3(-.285f, 1.355f, 0), new Vector3(.15f, .16f, .27f), armor);
                Part("Shoulder_Right", model, bevel, new Vector3(.285f, 1.355f, 0), new Vector3(.15f, .16f, .27f), armor);
                Segment("Upper_Arm_Left", model, new Vector3(-.285f, 1.31f, 0), new Vector3(-.29f, 1.06f, .15f), .13f, .16f, uniform);
                Segment("Forearm_Left", model, new Vector3(-.29f, 1.06f, .15f), new Vector3(-.055f, 1.115f, .49f), .12f, .14f, uniform);
                Segment("Upper_Arm_Right", model, new Vector3(.285f, 1.31f, 0), new Vector3(.3f, 1.035f, .09f), .13f, .16f, uniform);
                Segment("Forearm_Right", model, new Vector3(.3f, 1.035f, .09f), new Vector3(.11f, 1.085f, .285f), .12f, .14f, uniform);
                Part("Glove_Left", model, bevel, new Vector3(-.055f, 1.115f, .49f), new Vector3(.1f, .1f, .13f), dark);
                Part("Glove_Right", model, bevel, new Vector3(.11f, 1.085f, .285f), new Vector3(.1f, .13f, .1f), dark);

                Transform rifle = Empty("Rifle", model, new Vector3(.055f, 1.165f, .2f));
                rifle.localScale = Vector3.one * .76f;
                Transform muzzle = BuildRifle(rifle, dark, armor, uniform, false);
                VillaGuard guard = root.AddComponent<VillaGuard>();
                guard.visual = model;
                guard.muzzle = muzzle;
                guard.patrol = Array.Empty<Vector3>();
                VillaGuardAnimation walk = root.AddComponent<VillaGuardAnimation>();
                walk.leftHip = RigLeg(model, "Left", -.12f, out Transform leftKnee);
                walk.rightHip = RigLeg(model, "Right", .12f, out Transform rightKnee);
                walk.leftKnee = leftKnee;
                walk.rightKnee = rightKnee;
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = VillaQueries.GuardLayer;
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Guard.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Transform RigLeg(Transform model, string side, float hipX, out Transform knee)
        {
            // Preserve the manufactured bind pose; only the new joint transforms are animated.
            Transform hip = Empty("Hip_" + side, model, new Vector3(hipX, .87f, 0));
            knee = Empty("Knee_Pivot", hip, new Vector3(Mathf.Sign(hipX) * .01f, -.35f, 0));
            model.Find("Thigh_" + side).SetParent(hip, true);
            foreach (string part in new[] { "Shin_", "Boot_", "Knee_" })
                model.Find(part + side).SetParent(knee, true);
            return hip;
        }

        private static Transform BuildRifle(Transform parent, Material dark, Material copper, Material light, bool detail)
        {
            Mesh bevel = GetMesh("ChamferBox", ChamferBox);
            Mesh tube = GetMesh("OctagonalCylinder", OctagonalCylinder);
            Part("Receiver", parent, bevel, new Vector3(0, .01f, .18f), new Vector3(.115f, .125f, .32f), dark);
            Part("Upper_Rail", parent, bevel, new Vector3(0, .084f, .24f), new Vector3(.08f, .03f, .4f), copper);
            Part("Stock", parent, bevel, new Vector3(0, -.025f, -.055f), new Vector3(.095f, .115f, .14f), dark);
            Part("Stock_Pad", parent, bevel, new Vector3(0, -.025f, -.135f), new Vector3(.105f, .125f, .025f), copper);
            Part("Handguard", parent, bevel, new Vector3(0, .01f, .405f), new Vector3(.105f, .115f, .2f), copper);
            Transform barrel = Part("Barrel", parent, tube, new Vector3(0, .016f, .55f), new Vector3(.038f, .14f, .038f), dark);
            barrel.localRotation = Quaternion.Euler(90, 0, 0);
            Transform muzzleBrake = Part("Muzzle_Brake", parent, tube, new Vector3(0, .016f, .63f), new Vector3(.052f, .04f, .052f), copper);
            muzzleBrake.localRotation = Quaternion.Euler(90, 0, 0);
            Transform grip = Part("Grip", parent, bevel, new Vector3(0, -.105f, .105f), new Vector3(.075f, .145f, .08f), dark);
            grip.localRotation = Quaternion.Euler(-15, 0, 0);
            Transform magazine = Part("Magazine", parent, bevel, new Vector3(0, -.12f, .255f), new Vector3(.075f, .18f, .1f), dark);
            magazine.localRotation = Quaternion.Euler(-10, 0, 0);
            Part("Magazine_Base", magazine, bevel, new Vector3(0, -.49f, 0), new Vector3(1.12f, .1f, 1.12f), copper);
            Part("Rear_Sight_Left", parent, bevel, new Vector3(-.026f, .12f, .065f), new Vector3(.018f, .05f, .025f), dark);
            Part("Rear_Sight_Right", parent, bevel, new Vector3(.026f, .12f, .065f), new Vector3(.018f, .05f, .025f), dark);
            Part("Front_Sight", parent, bevel, new Vector3(0, .121f, .49f), new Vector3(.014f, .055f, .025f), dark);
            Part("Sight_Marker", parent, bevel, new Vector3(0, .145f, .488f), new Vector3(.009f, .008f, .028f), light);
            if (detail)
            {
                Part("Side_Plate", parent, bevel, new Vector3(.06f, .015f, .17f), new Vector3(.009f, .068f, .16f), copper);
                Part("Charging_Handle", parent, bevel, new Vector3(-.075f, .04f, .16f), new Vector3(.05f, .025f, .035f), dark);
                for (int i = 0; i < 3; i++)
                    Part("Receiver_Mark_" + i, parent, bevel, new Vector3(.066f, .018f, .145f + i * .018f), new Vector3(.003f, .035f, .006f), light);
                for (int i = 0; i < 3; i++)
                    Part("Handguard_Slot_" + i, parent, bevel, new Vector3(.054f, .015f, .355f + i * .045f), new Vector3(.006f, .04f, .022f), dark);
            }
            return Empty("Muzzle", parent, new Vector3(0, .016f, .655f));
        }

        private static void BuildHands(Transform model, Material glove, Material sleeve)
        {
            Mesh bevel = GetMesh("ChamferBox", ChamferBox);
            Segment("Sleeve_Right", model, new Vector3(.15f, -.31f, -.13f), new Vector3(.055f, -.13f, .095f), .115f, .13f, sleeve);
            Part("Glove_Right", model, bevel, new Vector3(.045f, -.11f, .105f), new Vector3(.085f, .12f, .1f), glove);
            Segment("Sleeve_Left", model, new Vector3(-.15f, -.27f, .17f), new Vector3(-.055f, -.065f, .39f), .11f, .12f, sleeve);
            Part("Glove_Left", model, bevel, new Vector3(-.045f, -.065f, .39f), new Vector3(.09f, .075f, .12f), glove);
        }

        private static Transform Empty(string name, Transform parent, Vector3 position)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent, false);
            result.localPosition = position;
            return result;
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Vector3 position, Vector3 size, Material material)
        {
            Transform result = Empty(name, parent, position);
            result.localScale = size;
            result.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = result.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return result;
        }

        private static Transform Segment(string name, Transform parent, Vector3 from, Vector3 to, float width, float depth, Material material)
        {
            Transform result = Part(name, parent, GetMesh("ChamferBox", ChamferBox),
                (from + to) * .5f, new Vector3(width, Vector3.Distance(from, to), depth), material);
            result.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            return result;
        }

        private static void SetWeaponRendering(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = WeaponLayer;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private static Mesh GetMesh(string name, Func<Mesh> create)
        {
            EnsureFolder(MeshFolder);
            string path = MeshFolder + "/" + name + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = create();
            mesh.name = name;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh ChamferBox()
        {
            Vector2[] ring =
            {
                new Vector2(-.42f, -.5f), new Vector2(.42f, -.5f),
                new Vector2(.5f, -.42f), new Vector2(.5f, .42f),
                new Vector2(.42f, .5f), new Vector2(-.42f, .5f),
                new Vector2(-.5f, .42f), new Vector2(-.5f, -.42f)
            };
            var builder = new FacetedBuilder();
            Vector3[] front = new Vector3[ring.Length];
            Vector3[] back = new Vector3[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                front[i] = new Vector3(ring[i].x, ring[i].y, .5f);
                back[ring.Length - 1 - i] = new Vector3(ring[i].x, ring[i].y, -.5f);
                int next = (i + 1) % ring.Length;
                builder.Face(new Vector3(ring[i].x, ring[i].y, -.5f), new Vector3(ring[next].x, ring[next].y, -.5f),
                    new Vector3(ring[next].x, ring[next].y, .5f), new Vector3(ring[i].x, ring[i].y, .5f));
            }
            builder.Face(front);
            builder.Face(back);
            return builder.Build();
        }

        private static Mesh Torso()
        {
            Mesh mesh = ChamferBox();
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i].x *= Mathf.Lerp(.77f, 1, vertices[i].y + .5f);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh OctagonalCylinder() => RadialMesh(new[] { -.5f, .5f }, new[] { .5f, .5f });
        private static Mesh Helmet() => RadialMesh(new[] { -.5f, .25f, .5f }, new[] { .42f, .5f, .32f });

        private static Mesh RadialMesh(float[] heights, float[] radii)
        {
            const int sides = 8;
            var builder = new FacetedBuilder();
            Vector3 Point(int ring, int side)
            {
                float angle = side * Mathf.PI * 2 / sides + Mathf.PI / 8;
                return new Vector3(Mathf.Cos(angle) * radii[ring], heights[ring], Mathf.Sin(angle) * radii[ring]);
            }
            for (int ring = 0; ring < heights.Length - 1; ring++)
                for (int side = 0; side < sides; side++)
                    builder.Face(Point(ring, side + 1), Point(ring, side), Point(ring + 1, side), Point(ring + 1, side + 1));
            var bottom = new Vector3[sides];
            var top = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                bottom[side] = Point(0, side);
                top[sides - 1 - side] = Point(heights.Length - 1, side);
            }
            builder.Face(bottom);
            builder.Face(top);
            return builder.Build();
        }

        private sealed class FacetedBuilder
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            public void Face(params Vector3[] points)
            {
                int start = vertices.Count;
                Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
                for (int i = 0; i < points.Length; i++)
                {
                    vertices.Add(points[i]);
                    normals.Add(normal);
                    uv.Add(new Vector2(i == 0 || i == points.Length - 1 ? 0 : 1, i < 2 ? 0 : 1));
                }
                for (int i = 1; i < points.Length - 1; i++)
                {
                    triangles.Add(start);
                    triangles.Add(start + i);
                    triangles.Add(start + i + 1);
                }
            }

            public Mesh Build()
            {
                Mesh mesh = new Mesh();
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string path = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = path;
            }
        }
    }
}
#endif
