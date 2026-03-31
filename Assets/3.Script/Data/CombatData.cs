using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Faction
{
    TeamA,
    TeamB,
    TeamC,
    None,
}

[Serializable]
public struct CombatData
{
    public float HP;
    public float Damage;
    public Faction Faction;
}
