// 저장 슬롯 하나의 요약(비어 있는지, 몇 일째 몇 시, 언제 저장했는지)이다. 슬롯 목록 화면이 보여 주는 문구를 만든다.
public readonly struct SaveSlotInfo
{
    public readonly bool exists;
    // 게임 시작부터 센 날짜(1일째부터)다. 알 수 없으면 0이다.
    public readonly int day;
    // 저장 당시 게임 시각(0~24시)이다. 알 수 없으면 음수다.
    public readonly float hour;
    public readonly string savedAt;

    public SaveSlotInfo(bool exists, int day, float hour, string savedAt)
    {
        this.exists = exists;
        this.day = day;
        this.hour = hour;
        this.savedAt = savedAt ?? "";
    }

    public static SaveSlotInfo Empty => new SaveSlotInfo(false, 0, -1f, "");

    public static SaveSlotInfo FromData(GameSaveData data)
    {
        if (data == null) return Empty;
        int day = data.elapsedGameHours >= 0f ? (int)(data.elapsedGameHours / 24f) + 1 : 0;
        return new SaveSlotInfo(true, day, data.timeOfDay, data.savedAt);
    }

    // "3일째 14:30  ·  2026-10-10 20:12" 형태다. 모르는 항목은 빼고, 비어 있으면 "비어 있음"이다.
    public string Describe()
    {
        if (!exists) return "비어 있음";

        string text = "";
        if (day > 0) text += day + "일째";
        if (hour >= 0f)
        {
            int minutes = (int)(hour * 60f) % (24 * 60);
            text += (text.Length > 0 ? " " : "") + (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
        }
        if (savedAt.Length > 0) text += (text.Length > 0 ? "  ·  " : "") + savedAt;
        return text.Length > 0 ? text : "저장됨";
    }
}
