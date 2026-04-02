using System;
using UnityEngine;

public enum CardType
{
    CatGun,        // 고양이 머신건
    BubbleGun,     // 버블건
    PenguinCharge, // 펭타우로스의 돌진
    WaterBalloon,   // 물풍선
    DuckTube,       // 오리 튜브
    SharkTube,      // 상어 튜브
    GoatDisinfectant, // 염소 소독제
    MalrangBong     // 밍키의 말랑봉
}
public enum CardSkillType
{
    Active,  // 액티브
    Passive  // 패시브
}

[System.Serializable]
public struct CardData
{
    [Header("기본 정보")]
    public string ID;
    public string CardName;
    public string Description;
    public CardType CardType;
    public CardSkillType SkillType;
    public Sprite CardIcon;

    [Header("스킬 설정")]
    public float Cooldown;
    public float Duration;    // 지속 시간 (고양이머신건 2초 등)
    public float Damage;      // 데미지 (펭타우로스 HP 10% 등)
    public float Range;       // 범위 (물풍선 등)
    public float Speed;     // 속도 (오리/상어 튜브)
}