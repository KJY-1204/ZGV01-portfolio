// 맵 씬이 쓰는 외부 에셋(저장소가 기본으로 무시하는 에셋스토어 폴더 안의 파일)을 모아 목록 파일로 쓰는 에디터 도구다. 목록의 파일은 git add -f로 추적한다.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MapAssetManifest
{
    public const string ManifestPath = "Assets/_Project/Data/Map/MapAssets.txt";

    // 저장소가 이미 추적하는 최상위 폴더다(그 밖의 Assets 하위가 외부 에셋이다).
    private static readonly string[] OwnRoots =
    {
        "Assets/_Project/", "Assets/Survivalist/", "Assets/Resources/", "Assets/Settings/", "Assets/Scenes/",
    };

    [MenuItem("ZGV01/Map/Write Asset Manifest")]
    public static void Write()
    {
        var files = new SortedSet<string>();
        foreach (string dependency in AssetDatabase.GetDependencies(MapLayout.ScenePath, true))
        {
            if (!dependency.StartsWith("Assets/")) continue;
            bool own = false;
            foreach (string root in OwnRoots) if (dependency.StartsWith(root)) { own = true; break; }
            if (own || dependency.EndsWith(".cs")) continue;
            files.Add(dependency);
            if (File.Exists(dependency + ".meta")) files.Add(dependency + ".meta");
        }

        long total = 0;
        StringBuilder text = new StringBuilder();
        text.AppendLine("# 맵 씬(Main_Map)이 쓰는 외부 에셋 목록이다. `git add -f --pathspec-from-file=<이 파일>`로 추적한다. ZGV01/Map/Write Asset Manifest로 다시 만든다.");
        foreach (string file in files)
        {
            text.AppendLine(file);
            if (File.Exists(file)) total += new FileInfo(file).Length;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath));
        File.WriteAllText(ManifestPath, text.ToString(), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ManifestPath);
        Debug.Log($"맵 외부 에셋 목록: 파일 {files.Count}개, 약 {total / 1024 / 1024}MB");
    }
}
