// 세이브 파일에 그대로 기록되는 순수 데이터 구조다. 런타임 컴포넌트, ScriptableObject와는 분리한다.
using System;
using System.Collections.Generic;

[Serializable]
public class ItemStackSaveData
{
    public string definitionId;
    public int quantity;
}

[Serializable]
public class MagazineSaveData
{
    public string weaponId;
    public int rounds;
}

[Serializable]
public class PlayerSaveData
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
    public float currentHealth;
    public List<ItemStackSaveData> inventory = new List<ItemStackSaveData>();

    // 장착 상태. 필드가 없는 옛 세이브 파일은 빈 값(맨손, 탄창 없음)으로 읽힌다.
    public string equippedWeaponId;
    public string lastWeaponId;
    public List<MagazineSaveData> magazines = new List<MagazineSaveData>();

    // 휴대 랜턴이 켜져 있었는지다. 필드가 없는 옛 세이브는 꺼진 상태로 읽힌다.
    public bool lanternOn;

    // 부활 위치로 지정한 침대의 월드 id다. 비어 있으면(옛 세이브 포함) 시작 위치에서 부활한다.
    public string respawnBedId;

    // 장착한 방어구의 stableId 목록이다. 필드가 없는 옛 세이브는 아무것도 장착하지 않은 상태로 읽힌다.
    public List<string> equippedArmorIds = new List<string>();

    // 생존 수치. hasSurvival이 false인 옛 세이브는 허기/갈증을 가득 찬 상태로 둔다.
    public bool hasSurvival;
    public float hunger;
    public float thirst;
}

[Serializable]
public class BuildableSaveData
{
    public string worldId;
    public string buildableId;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;

    // 문 상태. 필드가 없는 옛 세이브는 닫힌 상태로 읽힌다.
    public bool doorOpen;
    public float doorSide = 1f;

    // 남은 내구도다. 음수(필드 없는 옛 세이브)면 가득 찬 상태로 읽는다.
    public float health = -1f;

    // 벽에 붙은 조각이 붙은 건축물의 식별자다.
    public string supportId;

    // 수명이 있는 건축물(시체 가방)의 남은 초다. 음수(필드 없는 옛 세이브, 수명 없는 건축물)면 가득 찬 수명으로 읽는다.
    public float lifetimeRemaining = -1f;

    // 보관함에 든 아이템이다. 필드가 없는 옛 세이브(그리고 보관함이 아닌 건축물)는 빈 목록으로 읽힌다.
    public List<ItemStackSaveData> items = new List<ItemStackSaveData>();
}

// 살아 있는 동물이나 맹수 한 마리다. 행동 상태(추격 중 등)는 저장하지 않고 불러오면 대기 상태로 시작한다.
[Serializable]
public class AnimalSaveData
{
    // AnimalDefinition의 stableId다(AnimalDatabase로 프리팹을 찾는다).
    public string definitionId;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
    public float health;
}

// 멀티플레이 참가자 한 명의 저장 데이터다. 접속마다 바뀌는 clientId가 아니라 참가자 컴퓨터가 만든 고유 id로 구분한다. 위치, 체력, 생존 수치, 장착 상태는 저장하지 않는다.
[Serializable]
public class ParticipantSaveData
{
    public string playerId;
    public List<ItemStackSaveData> inventory = new List<ItemStackSaveData>();
    // 부활 위치로 지정한 침대의 월드 id다. 비어 있으면 지정하지 않았다.
    public string respawnBedId;

    // 참가자가 서버에 보고한 장착 상태와 생존 수치다. hasState가 거짓이면(옛 기록, 아직 보고 전) 아래 값은 없는 것으로 본다.
    public bool hasState;
    public string equippedWeaponId;
    public string lastWeaponId;
    public List<MagazineSaveData> magazines = new List<MagazineSaveData>();
    public List<string> equippedArmorIds = new List<string>();
    public bool hasSurvival;
    public float hunger;
    public float thirst;

    // 참가자가 보고한 현재 체력이다. hasHealth가 거짓이면(옛 기록, 죽어 있던 참가자) 가득 찬 체력으로 시작한다.
    public bool hasHealth;
    public float health;

    // 서버가 아바타에서 읽은 마지막 위치와 y축 회전이다. hasPosition이 거짓이면(옛 기록, 죽어 있던 참가자) 시작 칸에서 시작한다.
    public bool hasPosition;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY;
}

[Serializable]
public class GameSaveData
{
    public int schemaVersion = 1;
    public PlayerSaveData player = new PlayerSaveData();
    // 저장 당시 게임 시각(0~24시)이다. 음수(필드 없는 옛 세이브)면 시각을 바꾸지 않는다.
    public float timeOfDay = -1f;
    // 게임 시작부터 흐른 게임 시간(시)이다(좀비 정찰 유예 10일 계산). 음수(필드 없는 옛 세이브)면 지금 값을 그대로 둔다.
    public float elapsedGameHours = -1f;
    // 저장한 실제 시각 문구다(슬롯 목록에 보여 준다). 비어 있으면 옛 세이브다.
    public string savedAt = "";
    public List<BuildableSaveData> buildables = new List<BuildableSaveData>();

    // 동물 목록이 저장된 세이브인지다. false인 옛 세이브는 지금 있는 동물을 그대로 둔다(true면 목록대로 다시 만든다).
    public bool hasAnimals;
    public List<AnimalSaveData> animals = new List<AnimalSaveData>();

    // 멀티플레이 참가자들의 저장 데이터다. 필드가 없는 옛 세이브는 빈 목록으로 읽힌다.
    public List<ParticipantSaveData> participants = new List<ParticipantSaveData>();
}
