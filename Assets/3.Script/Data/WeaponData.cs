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
    public float Damage;

    [Header("물 소모량")]
    public float WaterPerShot;

    [Header("발사 메커니즘")]
    public float FireRate;
    public FireMode FireMode;
    public float BulletSpeed;
    public int MaxHitCountPerShot;
    [Tooltip("중력을 거스르는 상향 보정치 (저격총: 0.01 / 물총: 0.15)")]
    public float LiftForce;
    [Tooltip("공기 저항 (저격총: 0 / 물총: 1.5). 높을수록 사거리 끝에서 뚝 떨어짐")]
    public float AirResistance;

    [Header("정확도 및 반동")]
    public float BaseSpread;
    public Vector2 RecoilForce;


    [Header("로드된 리소스 (런타임 할당)")]
    public GameObject WeaponPrefab;
    public AudioClip FireSound;
    public GameObject MuzzleFlash;
}
