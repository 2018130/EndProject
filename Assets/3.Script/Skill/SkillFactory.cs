using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SkillFactory
{
    public static BaseSkill Create(CardData card)
    {
        return card.CardType switch
        {
            CardType.MalrangBong => new MalrangBongSkill(card),
            CardType.PenguinCharge => new PenguinChargeSkill(card),
            CardType.CatGun => new CatGunSkill(card),
            CardType.BubbleGun => new BubbleGunSkill(card),
            CardType.WaterBalloon => new WaterBalloonSkill(card),
            CardType.DuckTube => new DuckTubeSkill(card),
            CardType.SharkTube => new SharkTubeSkill(card),
            CardType.GoatDisinfectant => new GoatDisinfectantSkill(card),
            _ => null
        };
    }
}
