using System.Collections.Generic;
using TeamG.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TeamG.EditorTools
{
    public static class RodeoSplashDemoBuilder
    {
        [MenuItem("Tools/RODEO SPLASH/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateScene();
        }

        // Separate from the menu entry so automated verification never opens a dialog.
        public static string CreateScene()
        {
            const string parent = "Assets/_Project/Scenes/Dev";
            EnsureFolder(parent);
            string assetFolder = AssetDatabase.GenerateUniqueAssetPath(parent + "/rodeo_splash_assets");
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(assetFolder));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader shader = Shader.Find(GraphicsSettings.defaultRenderPipeline != null ?
                "Universal Render Pipeline/Unlit" : "Standard");
            if (shader == null) throw new System.InvalidOperationException("Use a Built-in or URP project for this demo.");
            Material shirtMaterial = new Material(shader) { name = "mat_shirt" };
            Material ballMaterial = new Material(shader) { name = "mat_ball" };
            SetColor(ballMaterial, new Color(0.05f, 0.55f, 1f));
            AssetDatabase.CreateAsset(shirtMaterial, assetFolder + "/mat_shirt.mat");
            AssetDatabase.CreateAsset(ballMaterial, assetFolder + "/mat_ball.mat");

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "PaintBall";
            ball.transform.localScale = Vector3.one * 0.14f;
            ball.GetComponent<MeshRenderer>().sharedMaterial = ballMaterial;
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.15f;
            rb.isKinematic = true;
            rb.useGravity = false;
            ball.AddComponent<PaintBall>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, assetFolder + "/paint_ball.prefab");
            Object.DestroyImmediate(ball);

            Mesh shirtMesh = CreateShirtMesh();
            AssetDatabase.CreateAsset(shirtMesh, assetFolder + "/shirt_mesh.asset");
            GameObject shirt = new GameObject("TShirt", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            shirt.transform.position = new Vector3(0f, 0.4f, 3f);
            shirt.GetComponent<MeshFilter>().sharedMesh = shirtMesh;
            shirt.GetComponent<MeshRenderer>().sharedMaterial = shirtMaterial;
            shirt.GetComponent<MeshCollider>().sharedMesh = shirtMesh;
            shirt.AddComponent<Paint>();

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1.25f, -3f);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            GameObject point = new GameObject("ThrowPoint");
            point.transform.SetParent(camera.transform, false);
            point.transform.localPosition = new Vector3(0.22f, -0.17f, 0.55f);
            player.AddComponent<PlayerThrow>().Configure(camera, point.transform, prefab.GetComponent<PaintBall>(), player.transform);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.2f, 16f);
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            string scenePath = AssetDatabase.GenerateUniqueAssetPath(parent + "/RodeoSplashDemo.unity");
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("RODEO SPLASH demo created: " + scenePath + ". Press Play, hold LMB, then release.");
            return scenePath;
        }

        private static void SetColor(Material material, Color color)
        {
            material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color", color);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static Mesh CreateShirtMesh()
        {
            // A flat T-shirt silhouette with a neck notch; front faces the camera along -Z.
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            AddRectangle(vertices, uvs, triangles, -0.5f, 0.5f, 0f, 1.05f);
            AddRectangle(vertices, uvs, triangles, -0.5f, -0.18f, 1.05f, 1.3f);
            AddRectangle(vertices, uvs, triangles, 0.18f, 0.5f, 1.05f, 1.3f);
            AddRectangle(vertices, uvs, triangles, -0.95f, -0.5f, 0.8f, 1.3f);
            AddRectangle(vertices, uvs, triangles, 0.5f, 0.95f, 0.8f, 1.3f);
            Mesh mesh = new Mesh { name = "shirt_mesh" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddRectangle(List<Vector3> vertices, List<Vector2> uvs,
            List<int> triangles, float x0, float x1, float y0, float y1)
        {
            int start = vertices.Count;
            Vector3[] points = { new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f),
                new Vector3(x1, y1, 0f), new Vector3(x0, y1, 0f) };
            foreach (Vector3 p in points)
            {
                vertices.Add(p);
                uvs.Add(new Vector2((p.x + 0.95f) / 1.9f, p.y / 1.3f));
            }
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }
    }
}
