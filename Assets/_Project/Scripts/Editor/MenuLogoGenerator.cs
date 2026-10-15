// 메인 메뉴 로고(육각 테두리 안의 Z와 세 줄 할큄 자국)를 코드로 그려 PNG로 저장하는 에디터 도구다.
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MenuLogoGenerator
{
    public const string OutputPath = "Assets/_Project/UI/Icons/Logo_ZGV.png";
    private const int Size = 512;

    private static readonly Color Cyan = new Color(0.31f, 0.92f, 1f);
    private static readonly Color Ink = new Color(0.77f, 0.96f, 1f);
    private static readonly Color Blood = new Color(1f, 0.28f, 0.34f);

    [MenuItem("ZGV01/Menu/Generate Logo")]
    public static void Generate()
    {
        Color[] pixels = new Color[Size * Size];
        Vector2[] hexagon = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i + 30f);
            hexagon[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        Vector2 zTopLeft = new Vector2(-0.42f, 0.42f), zTopRight = new Vector2(0.42f, 0.42f);
        Vector2 zBottomLeft = new Vector2(-0.42f, -0.42f), zBottomRight = new Vector2(0.42f, -0.42f);

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // 화면 좌표를 -1~1 범위로 바꾼다(위가 +y).
                Vector2 p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                Color result = Color.clear;

                // 육각 테두리(굵은 바깥선과 가는 안쪽선)
                float outer = PolygonRing(p, hexagon, 0.94f);
                float inner = PolygonRing(p, hexagon, 0.84f);
                result = Over(result, Cyan, Coverage(outer, 0.035f));
                result = Over(result, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f), Coverage(inner, 0.008f));

                // Z
                float z = Mathf.Min(SegmentDistance(p, zTopLeft, zTopRight), SegmentDistance(p, zTopRight, zBottomLeft), SegmentDistance(p, zBottomLeft, zBottomRight));
                result = Over(result, Ink, Coverage(z, 0.075f));

                // 세 줄 할큄(오른쪽 위에서 왼쪽 아래로, 양 끝이 가늘어진다)
                for (int k = -1; k <= 1; k++)
                {
                    Vector2 a = new Vector2(0.5f + k * 0.18f, 0.62f - k * 0.09f);
                    Vector2 b = new Vector2(-0.46f + k * 0.18f, -0.52f - k * 0.09f);
                    result = Over(result, Blood, Coverage(TaperedDistance(p, a, b, 0.05f), 0f));
                }

                pixels[y * Size + x] = result;
            }
        }

        Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(OutputPath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    // 거리와 선 반두께로 가장자리가 부드러운 덮임 정도(0~1)를 구한다.
    private static float Coverage(float distance, float halfWidth)
    {
        float edge = 1.5f / Size * 2f;
        return Mathf.Clamp01((halfWidth - distance) / edge + 0.5f);
    }

    private static Color Over(Color under, Color over, float coverage)
    {
        float alpha = over.a * coverage;
        float outAlpha = alpha + under.a * (1f - alpha);
        if (outAlpha <= 0f) return Color.clear;
        Color rgb = (new Color(over.r, over.g, over.b) * alpha + new Color(under.r, under.g, under.b) * (under.a * (1f - alpha))) / outAlpha;
        return new Color(rgb.r, rgb.g, rgb.b, outAlpha);
    }

    // 다각형 변까지의 거리(scale 배한 다각형 기준)다.
    private static float PolygonRing(Vector2 p, Vector2[] polygon, float scale)
    {
        float best = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
            best = Mathf.Min(best, SegmentDistance(p, polygon[i] * scale, polygon[(i + 1) % polygon.Length] * scale));
        return best;
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // 가운데가 굵고 양 끝이 가는 선까지의 거리에서 두께를 뺀 값(0 이하면 선 안쪽)을 덮임 계산용으로 돌려준다.
    private static float TaperedDistance(Vector2 p, Vector2 a, Vector2 b, float maxHalfWidth)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        float width = maxHalfWidth * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.7f);
        return Vector2.Distance(p, a + ab * t) - width;
    }
}
