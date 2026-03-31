using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>
{
    public SceneContext SceneContext { get; set; } = null;
}
