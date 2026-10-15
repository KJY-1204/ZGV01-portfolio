// 참가자가 서버에 보고한 장착 상태와 생존 수치를 저장하기 전에 걸러 내는 규칙이다. 깨진 값(NaN, 음수, 너무 큰 값)과 지나치게 긴 목록/문자열이 세이브 파일을 망가뜨리지 않게 한다. 유니티 컴포넌트에 의존하지 않는다.
using System.Collections.Generic;

public static class ParticipantStateRule
{
    public const int MaxIdLength = 64;
    public const int MaxMagazines = 32;
    public const int MaxArmor = 16;
    public const float MaxMeter = 10000f;
    public const int MaxRounds = 10000;

    // 값을 안전한 범위로 고친다. 보고가 상태를 담고 있지 않으면(hasState 거짓) false를 돌려준다.
    public static bool Sanitize(ParticipantSaveData data)
    {
        if (data == null || !data.hasState) return false;

        data.equippedWeaponId = CleanId(data.equippedWeaponId);
        data.lastWeaponId = CleanId(data.lastWeaponId);

        data.magazines = data.magazines ?? new List<MagazineSaveData>();
        var magazines = new List<MagazineSaveData>();
        foreach (MagazineSaveData magazine in data.magazines)
        {
            if (magazine == null || magazines.Count >= MaxMagazines) continue;
            string id = CleanId(magazine.weaponId);
            if (id.Length == 0) continue;
            magazines.Add(new MagazineSaveData { weaponId = id, rounds = ClampRounds(magazine.rounds) });
        }
        data.magazines = magazines;

        data.equippedArmorIds = data.equippedArmorIds ?? new List<string>();
        var armor = new List<string>();
        foreach (string armorId in data.equippedArmorIds)
        {
            string id = CleanId(armorId);
            if (id.Length > 0 && armor.Count < MaxArmor && !armor.Contains(id)) armor.Add(id);
        }
        data.equippedArmorIds = armor;

        data.hunger = ClampMeter(data.hunger);
        data.thirst = ClampMeter(data.thirst);
        data.health = ClampMeter(data.health);
        return true;
    }

    private static string CleanId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        id = id.Trim();
        return id.Length > MaxIdLength ? "" : id;
    }

    private static int ClampRounds(int rounds) => rounds < 0 ? 0 : rounds > MaxRounds ? MaxRounds : rounds;

    private static float ClampMeter(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) return 0f;
        return value > MaxMeter ? MaxMeter : value;
    }
}
