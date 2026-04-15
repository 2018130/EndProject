using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Ranged, Melee }
public enum FireMode { Single, Auto, Burst }

[System.Serializable]
public struct WeaponData
{
    [Header("기본 정보")]
    public string ID;
    public WeaponType WeaponType;

    [Header("물 소모량")]
    public float WaterPerShot;

    [Header("발사 메커니즘")]
    public float FireRate;
    public FireMode FireMode;
    public float BulletSpeed;
    public int MaxHitCountPerShot;

    [Header("정확도 및 반동")]
    public float BaseSpread;
    public Vector2 RecoilForce;

    [Header("로드된 리소스 (런타임 할당)")]
    public GameObject WeaponPrefab;
    public AudioClip FireSound;
    public GameObject MuzzleFlash;
}
