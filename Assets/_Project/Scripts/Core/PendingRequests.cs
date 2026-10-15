// 서버 응답을 기다리는 요청의 중복 전송을 막는 규칙이다. 같은 키의 요청은 응답이 와서 지우거나 제한 시간이 지나기 전에는 한 번만 보낸다. 유니티 컴포넌트에 의존하지 않는다.
using System.Collections.Generic;

public class PendingRequests
{
    private readonly Dictionary<string, float> until = new Dictionary<string, float>();

    // 아직 요청 중이 아니면 요청을 시작한 것으로 기록하고 true를 돌려준다. 이미 요청 중이면(제한 시간 안) false다.
    public bool TryBegin(string key, float now, float timeoutSeconds)
    {
        if (key == null || IsPending(key, now)) return false;
        until[key] = now + timeoutSeconds;
        return true;
    }

    public bool IsPending(string key, float now)
    {
        return key != null && until.TryGetValue(key, out float deadline) && now < deadline;
    }

    // 응답이 왔거나 화면을 닫을 때 모든 요청 중 표시를 지운다.
    public void Clear() => until.Clear();
}
