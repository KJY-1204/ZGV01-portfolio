// 이 컴퓨터의 멀티플레이 참가자 고유 id를 만들고 파일에 보관하는 정적 도우미다. 접속마다 바뀌는 clientId와 달리 같은 사람이 다시 접속했을 때 호스트가 저장해 둔 인벤토리와 침대를 찾는 데 쓴다. 인증이 아니라 식별이다.
using System;
using System.IO;
using UnityEngine;

public static class LocalPlayerIdentity
{
    private const string FileName = "player_id.txt";
    private static string cached;

    public static string Id
    {
        get
        {
            if (!string.IsNullOrEmpty(cached)) return cached;
            cached = ReadOverride() ?? LoadOrCreate();
            return cached;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 테스트용: 같은 컴퓨터에서 참가자 여러 명을 흉내 낼 때 -mp-player-id=이름으로 id를 덮어쓴다.
    private static string ReadOverride()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
            if (arg.StartsWith("-mp-player-id=") && arg.Length > "-mp-player-id=".Length) return arg.Substring("-mp-player-id=".Length);
        return null;
    }
#else
    private static string ReadOverride() => null;
#endif

    private static string LoadOrCreate()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path).Trim();
                if (existing.Length > 0) return existing;
            }

            string created = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, created);
            return created;
        }
        catch (Exception e)
        {
            // 파일을 쓸 수 없으면 이번 실행에서만 쓰는 임시 id로 접속한다(저장은 이어지지 않는다).
            Debug.LogWarning("참가자 id 파일을 읽거나 쓰지 못했다: " + e.Message);
            return Guid.NewGuid().ToString("N");
        }
    }
}
