// 게임 안의 시각(0~24시)을 흘려보내고, 그에 맞춰 태양 방향과 밝기를 바꾸는 컴포넌트다. 스폰 밀도 같은 시간대 규칙이 이 시각을 읽는다.
using UnityEngine;

public class GameClock : MonoBehaviour
{
    // 현실 몇 초가 게임 하루(24시간)인지다.
    [SerializeField] private float dayLengthSeconds = 1200f;
    [SerializeField, Range(0f, 24f)] private float startHour = 9f;
    [SerializeField] private Light sun;
    // 시각(0~24)에 따른 태양 밝기 배율과 환경광 배율이다. 밤에는 아주 어둡게 하되 완전한 암흑은 피한다.
    [SerializeField] private AnimationCurve sunIntensity = new AnimationCurve(
        new Keyframe(0f, 0.08f), new Keyframe(5f, 0.1f), new Keyframe(7f, 0.8f), new Keyframe(12f, 1.1f),
        new Keyframe(17f, 0.9f), new Keyframe(19.5f, 0.2f), new Keyframe(21f, 0.08f), new Keyframe(24f, 0.08f));
    [SerializeField] private AnimationCurve ambientIntensity = new AnimationCurve(
        new Keyframe(0f, 0.3f), new Keyframe(5f, 0.32f), new Keyframe(7f, 0.8f), new Keyframe(12f, 1f),
        new Keyframe(17f, 0.85f), new Keyframe(19.5f, 0.4f), new Keyframe(21f, 0.3f), new Keyframe(24f, 0.3f));
    // 밤으로 보는 시각 범위다(날이 어두워지는 저녁부터 새벽 전까지).
    [SerializeField] private float nightStartHour = 20f;
    [SerializeField] private float nightEndHour = 6f;

    private static GameClock current;
    // 씬에 시계가 없으면 null이다(시간대 규칙은 낮으로 취급한다).
    public static GameClock Current => current;

    public float Hour { get; private set; }
    public bool IsNight => Hour >= nightStartHour || Hour < nightEndHour;
    public float DayLengthSeconds => dayLengthSeconds;

    private void Awake()
    {
        current = this;
        Hour = startHour;
        if (sun == null) sun = RenderSettings.sun;
        ApplyLighting();
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void Update()
    {
        if (dayLengthSeconds <= 0f) return;
        SetHour(Hour + Time.deltaTime * 24f / dayLengthSeconds);
    }

    // 시각을 바꾼다(저장 불러오기, 시험). 24를 넘으면 0시로 돌아간다.
    public void SetHour(float hour)
    {
        Hour = Mathf.Repeat(hour, 24f);
        ApplyLighting();
    }

    // 해는 6시에 뜨고 12시에 가장 높으며 18시에 진다. 밤에는 지평선 아래에서 반대로 돈다.
    private void ApplyLighting()
    {
        if (sun != null)
        {
            float pitch = (Hour / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(pitch, 170f, 0f);
            sun.intensity = Mathf.Max(0f, sunIntensity.Evaluate(Hour));
        }
        RenderSettings.ambientIntensity = Mathf.Max(0f, ambientIntensity.Evaluate(Hour));
    }

    // "09:30" 같은 표시용 문자열이다.
    public static string Format(float hour)
    {
        int h = Mathf.FloorToInt(hour) % 24;
        int m = Mathf.FloorToInt((hour - Mathf.Floor(hour)) * 60f);
        return $"{h:00}:{m:00}";
    }
}
