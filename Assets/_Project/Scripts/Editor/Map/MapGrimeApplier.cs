// 구역 소품의 외벽 재질을 오염/이끼 셰이더(ZGV01/GrimeSimpleLit, ZGV01/GrimeLit) 복사본으로 바꾸는 에디터 도구다. 원본 에셋 재질은 건드리지 않고 프로젝트 쪽에 복사본을 만든다.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapGrimeApplier
{
    private const string MaterialDir = "Assets/_Project/Materials/Map/Grime/";

    private struct Look
    {
        public string tag;
        public float moss, grime, streak;
    }

    // 버려진 학교와 야영지는 이끼가 많고, 산업 구역은 먼지와 빗물 줄무늬가 많다.
    private static readonly Look Mossy = new Look { tag = "Mossy", moss = 0.75f, grime = 0.55f, streak = 0.5f };
    private static readonly Look Dusty = new Look { tag = "Dusty", moss = 0.3f, grime = 0.7f, streak = 0.55f };

    // 이 경로 조각이 들어 있는 원본 프리팹만 바꾼다(건물 외벽, 컨테이너, 탱크, 울타리 등). 실내 가구와 식물, 가로등은 건드리지 않는다.
    private static readonly string[] IncludePaths = { "RPG_FPS_game_assets_industrial", "SchoolScene/Models/Exterior" };
    private static readonly string[] ExcludePaths = { "/Plants/", "/Lamps/", "/Benches/" };

    [MenuItem("ZGV01/Map/Apply Grime Shader")]
    public static void ApplyToScene()
    {
        GameObject props = GameObject.Find("MapProps");
        if (props == null) throw new System.InvalidOperationException("MapProps가 없다. 먼저 맵 소품을 배치한다.");
        Apply(props.transform);
        EditorSceneManager.MarkSceneDirty(props.scene);
        EditorSceneManager.SaveScene(props.scene);
    }

    // MapProps 아래 구역별 그룹에 오염/이끼를 입힌다. 맵 배치 마지막에도 부른다.
    public static void Apply(Transform props)
    {
        Shader simple = Shader.Find("ZGV01/GrimeSimpleLit");
        Shader lit = Shader.Find("ZGV01/GrimeLit");
        if (simple == null || lit == null) throw new System.InvalidOperationException("오염 셰이더를 찾지 못했다.");
        Directory.CreateDirectory(MaterialDir);

        int changed = 0;
        foreach (MapLayout.Zone zone in MapLayout.Zones)
        {
            Transform group = props.Find(zone.name);
            if (group == null) continue;
            Look look = zone.kind == MapLayout.ZoneKind.School || zone.kind == MapLayout.ZoneKind.Camp ? Mossy : Dusty;
            foreach (MeshRenderer renderer in group.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!IsExterior(renderer)) continue;
                Material[] materials = renderer.sharedMaterials;
                bool any = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material variant = Variant(materials[i], look, simple, lit);
                    if (variant == materials[i]) continue;
                    materials[i] = variant;
                    any = true;
                }
                if (!any) continue;
                renderer.sharedMaterials = materials;
                changed++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("오염/이끼 셰이더 적용: 렌더러 " + changed + "개");
    }

    private static bool IsExterior(MeshRenderer renderer)
    {
        if (renderer.GetComponentInParent<NightLight>() != null) return false;
        GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer.gameObject);
        if (source == null) return false;
        string path = AssetDatabase.GetAssetPath(source);
        foreach (string exclude in ExcludePaths) if (path.Contains(exclude)) return false;
        foreach (string include in IncludePaths) if (path.Contains(include)) return true;
        return false;
    }

    // Lit/Simple Lit 재질이면 같은 값을 가진 오염 셰이더 복사본을 돌려준다. 이미 복사본이거나 다른 셰이더면 그대로 둔다.
    private static Material Variant(Material source, Look look, Shader simple, Shader lit)
    {
        if (source == null) return null;
        Shader target = source.shader.name == "Universal Render Pipeline/Simple Lit" ? simple
            : source.shader.name == "Universal Render Pipeline/Lit" ? lit : null;
        if (target == null) return source;

        string path = MaterialDir + source.name + "_" + look.tag + ".mat";
        Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (copy == null)
        {
            copy = new Material(source) { name = source.name + "_" + look.tag };
            copy.shader = target;
            AssetDatabase.CreateAsset(copy, path);
        }
        copy.SetFloat("_MossAmount", look.moss);
        copy.SetFloat("_GrimeAmount", look.grime);
        copy.SetFloat("_StreakAmount", look.streak);
        EditorUtility.SetDirty(copy);
        return copy;
    }
}
