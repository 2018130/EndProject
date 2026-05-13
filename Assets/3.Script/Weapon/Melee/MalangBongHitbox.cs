using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MalangBongHitbox : MonoBehaviour
{
    private MalangBong _owner;

    public void Setup(MalangBong owner)
    {
        _owner = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        _owner?.OnHitboxTriggerEnter(other);
    }
}
