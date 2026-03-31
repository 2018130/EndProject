using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CombatArgument
{
    public Combat AttackTarget;
    public Combat DamagedTarget;
}

public class Combat : MonoBehaviour
{
    [SerializeField]
    private CombatData combatData;
    public CombatData CombatData => combatData;

    public event Action<CombatArgument> OnKilledTarget;

    public void TakeDamage(Combat attackTarget, float damage)
    {
        if ((attackTarget.CombatData.Faction == combatData.Faction) &&
            combatData.Faction != Faction.None)
            return;

        combatData.HP = Mathf.Clamp(combatData.HP - damage, 0, combatData.HP);

        if(combatData.HP <= 0)
        {
            OnKilledTarget?.Invoke(new CombatArgument() { AttackTarget = attackTarget, DamagedTarget = this });
            Death();
        }
    }

    private void Death()
    {
        Destroy(gameObject);
    }
}
