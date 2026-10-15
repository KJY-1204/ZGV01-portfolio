// 동물 종류 id로 프리팹을 찾을 수 있게 하는 실행 시점 조회용 데이터 에셋이다. 저장 파일은 프리팹 참조 대신 이 id를 저장한다.
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalDatabase", menuName = "ZGV01/Animals/Animal Database")]
public class AnimalDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        // AnimalDefinition의 stableId와 같다.
        public string id;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> animals = new List<Entry>();

    private static AnimalDatabase instance;

    public static AnimalDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<AnimalDatabase>("AnimalDatabase");
            return instance;
        }
    }

    public GameObject GetPrefab(string id)
    {
        foreach (Entry entry in animals)
        {
            if (entry != null && entry.id == id) return entry.prefab;
        }
        return null;
    }
}
