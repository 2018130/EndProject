using System;
using System.Collections;
using System.Collections.Generic;
using Unity.NetCode;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class MyNetworkBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
}
