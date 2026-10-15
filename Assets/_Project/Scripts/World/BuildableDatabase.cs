// buildableId로 BuildableDefinition을 찾을 수 있게 하는 실행 시점 조회용 데이터 에셋이다. 저장 파일은 프리팹 참조 대신 이 id를 저장한다.
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildableDatabase", menuName = "ZGV01/World/Buildable Database")]
public class BuildableDatabase : ScriptableObject
{
    [SerializeField] private List<BuildableDefinition> buildables = new List<BuildableDefinition>();

    private static BuildableDatabase instance;

    public static BuildableDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<BuildableDatabase>("BuildableDatabase");
            }
            return instance;
        }
    }

    public BuildableDefinition GetById(string buildableId)
    {
        foreach (BuildableDefinition def in buildables)
        {
            if (def != null && def.BuildableId == buildableId) return def;
        }
        return null;
    }
}
