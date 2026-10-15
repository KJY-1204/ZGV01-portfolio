// 지형 풀(디테일)에 쓸 풀 다발을 코드로 만들어 프로젝트 에셋으로 저장하는 에디터 도구다(텍스처 PNG, 교차 판 메시, 컷아웃 재질, 프리팹). 풀 에셋이 프로젝트에 없어서 직접 만든다. 이미 있으면 그대로 쓴다.
using UnityEditor;
using UnityEngine;

public static class GrassTextureBuilder
{
    public const string Dir = "Assets/_Project/Textures/Map/";
    private const string PrefabDir = "Assets/_Project/Prefabs/Map/";
    private const string MaterialDir = "Assets/_Project/Materials/Map/";
    private const int Width = 256, Height = 256;

    // 풀 다발 두 종류(긴 풀, 짧고 촘촘한 풀)다.
    public static Texture2D[] EnsureTextures()
    {
        System.IO.Directory.CreateDirectory(Dir);
        return new[]
        {
            EnsureTexture("Grass_Tuft_Tall.png", seed: 11, blades: 26, minHeight: 0.55f, maxHeight: 1.0f, lean: 0.22f),
            EnsureTexture("Grass_Tuft_Short.png", seed: 29, blades: 34, minHeight: 0.3f, maxHeight: 0.6f, lean: 0.14f),
        };
    }

    // 풀 다발 프리팹 두 종류다. 지형 디테일(메시 인스턴싱)이 이 프리팹을 심는다. URP에서는 기본 풀 빌보드가 그려지지 않아 메시를 쓴다.
    public static GameObject[] EnsurePrefabs()
    {
        Texture2D[] textures = EnsureTextures();
        System.IO.Directory.CreateDirectory(PrefabDir);
        System.IO.Directory.CreateDirectory(MaterialDir);
        return new[]
        {
            EnsurePrefab("GrassTuft_Tall", textures[0]),
            EnsurePrefab("GrassTuft_Short", textures[1]),
        };
    }

    private static GameObject EnsurePrefab(string name, Texture2D texture)
    {
        string path = PrefabDir + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        string materialPath = MaterialDir + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", texture);
        // 지형 색과 어울리게 채도를 낮춘다(짧은 풀은 조금 더 마른 색).
        material.SetColor("_BaseColor", name.EndsWith("Tall") ? new Color(0.5f, 0.56f, 0.34f) : new Color(0.46f, 0.52f, 0.32f));
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetFloat("_Cull", 0f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);

        string meshPath = PrefabDir + name + "_Mesh.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = BuildCrossMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        GameObject root = new GameObject(name);
        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    // 바닥을 기준(피벗)으로 서로 교차한 판 세 장(60도 간격)이다. 가로 1m, 높이 1m.
    private static Mesh BuildCrossMesh()
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 3; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, i * 60f, 0f);
            int start = vertices.Count;
            vertices.Add(rotation * new Vector3(-0.5f, 0f, 0f));
            vertices.Add(rotation * new Vector3(0.5f, 0f, 0f));
            vertices.Add(rotation * new Vector3(-0.5f, 1f, 0f));
            vertices.Add(rotation * new Vector3(0.5f, 1f, 0f));
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f));
            triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
        }
        Mesh mesh = new Mesh { name = "GrassCross" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        // 풀은 위를 향해 조명을 받게(바닥 풀처럼 보이게) 법선을 위로 모은다.
        var normals = new Vector3[vertices.Count];
        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Texture2D EnsureTexture(string fileName, int seed, int blades, float minHeight, float maxHeight, float lean)
    {
        string path = Dir + fileName;
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;

        Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[Width * Height];
        System.Random rng = new System.Random(seed);
        for (int i = 0; i < blades; i++) DrawBlade(pixels, rng, minHeight, maxHeight, lean);
        texture.SetPixels32(pixels);
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipMapsPreserveCoverage = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // 아래쪽 한 점에서 위로 휘어 올라가는 잎 하나를 그린다(끝으로 갈수록 가늘어지고 밝아진다).
    private static void DrawBlade(Color32[] pixels, System.Random rng, float minHeight, float maxHeight, float lean)
    {
        float baseX = Width * (0.5f + ((float)rng.NextDouble() - 0.5f) * 0.7f);
        float height = Height * (minHeight + (float)rng.NextDouble() * (maxHeight - minHeight));
        float bend = ((float)rng.NextDouble() - 0.5f) * 2f * lean * Width;
        float baseWidth = 3.5f + (float)rng.NextDouble() * 3f;
        float tint = 0.8f + (float)rng.NextDouble() * 0.35f;

        int steps = (int)(height * 1.5f);
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            // 위로 갈수록 바깥으로 휘는 곡선이다.
            float x = baseX + bend * t * t;
            float y = height * t;
            float half = Mathf.Max(0.45f, baseWidth * (1f - t) * 0.5f);
            // 뿌리는 어둡고 끝은 연한 연두다.
            float r = Mathf.Lerp(0.16f, 0.52f, t) * tint;
            float g = Mathf.Lerp(0.30f, 0.78f, t) * tint;
            float b = Mathf.Lerp(0.08f, 0.22f, t) * tint;
            Color32 color = new Color32((byte)(Mathf.Clamp01(r) * 255f), (byte)(Mathf.Clamp01(g) * 255f), (byte)(Mathf.Clamp01(b) * 255f), 255);

            for (int dx = -(int)Mathf.Ceil(half); dx <= (int)Mathf.Ceil(half); dx++)
            {
                int px = Mathf.RoundToInt(x) + dx;
                int py = Mathf.RoundToInt(y);
                if (px < 0 || px >= Width || py < 0 || py >= Height) continue;
                pixels[py * Width + px] = color;
            }
        }
    }
}
