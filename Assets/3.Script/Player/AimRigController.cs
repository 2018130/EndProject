using UnityEngine;
using UnityEngine.Animations.Rigging;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class AimRigController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private AimController aimController;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Transform aimTarget;

    [Header("Hand Bone")]
    // PlayerRoot의 Animator 기준 RightHand 본
    // Awake에서 Animator.GetBoneTransform으로 자동 찾음
    [SerializeField] private Transform handBone;

    public Transform HandBone => handBone;

    [Header("Rig Layers")]
    [SerializeField] private Rig headAimRig;
    [SerializeField] private Rig spineAimRig;
    [SerializeField] private Rig armIKRig;

    [Header("Constraint References")]
    [SerializeField] private TwoBoneIKConstraint armIKConstraint;

    [Header("Weight Settings")]
    [SerializeField] private float aimingHeadWeight = 0.5f;
    [SerializeField] private float aimingSpineWeight = 0.25f;
    [SerializeField] private float aimingArmWeight = 1.0f;
    [SerializeField] private float idleHeadWeight = 0.15f;
    [SerializeField] private float idleArmWeight = 0.0f;
    [SerializeField] private float weightLerpSpeed = 6f;
    [SerializeField] private float targetLerpSpeed = 12f;

    private GunAlignToHand _currentGunAlign;
    private RigBuilder _rigBuilder;

    public override void OnNetworkSpawn()
    {
        _rigBuilder = GetComponent<RigBuilder>();

        // handBone이 Inspector에서 안 연결됐으면 자동으로 찾음
        if (handBone == null)
        {
            Animator animator = GetComponent<Animator>();
            if (animator != null)
                handBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
    }

    private void LateUpdate()
    {
        UpdateAimTarget();
        UpdateRigWeights();
        SyncCurrentGun();
    }

    // ── AimTarget 위치 갱신 ──────────────────────────────
    private void UpdateAimTarget()
    {
        if (aimTarget == null || aimController == null) return;

        Vector3 targetPos = IsOwner
            ? aimController.AimWorldPoint
            : transform.position + aimController.NetAimDirection.Value * 10f;

        aimTarget.position = Vector3.Lerp(
            aimTarget.position, targetPos, targetLerpSpeed * Time.deltaTime);
    }

    // ── Rig 가중치 갱신 ──────────────────────────────────
    private void UpdateRigWeights()
    {
        if (aimController == null) return;

        bool isAiming = aimController.GetIsAiming();
        bool hasGun = weaponController != null && weaponController.CurrentWeapon != null;
        float t = weightLerpSpeed * Time.deltaTime;

        if (headAimRig != null)
            headAimRig.weight = Mathf.Lerp(headAimRig.weight,
                isAiming ? aimingHeadWeight : idleHeadWeight, t);

        if (spineAimRig != null)
            spineAimRig.weight = Mathf.Lerp(spineAimRig.weight,
                isAiming ? aimingSpineWeight : 0f, t);

        // 총이 없거나 비활성이면 ArmIK도 끔
        if (armIKRig != null)
            armIKRig.weight = Mathf.Lerp(armIKRig.weight,
                (isAiming && hasGun) ? aimingArmWeight : idleArmWeight, t);
    }

    // ── 현재 활성 총의 GripTarget을 IK에 연결 ────────────
    // WeaponController가 SetActive로 총을 교체하므로
    // 매 프레임 CurrentWeapon을 체크해서 변경 시에만 갱신
    private BaseWeapon _lastWeapon = null;

    private void SyncCurrentGun()
    {
        if (weaponController == null) return;

        BaseWeapon current = weaponController.CurrentWeapon;
        if (current == _lastWeapon) return;

        if (_currentGunAlign != null)
            _currentGunAlign.StopAlign();

        _lastWeapon = current;

        if (current == null)
        {
            _currentGunAlign = null;
            return;
        }

        _currentGunAlign = current.GetComponent<GunAlignToHand>();
        if (_currentGunAlign != null)
            _currentGunAlign.StartAlign();

        if (armIKConstraint != null)
        {
            Transform gripTarget = current.transform.Find("GripTarget");
            armIKConstraint.data.target = gripTarget;

            // ★ 즉시 Build 대신 한 프레임 뒤에 실행
            StartCoroutine(RebuildNextFrame());
        }
    }

    private IEnumerator RebuildNextFrame()
    {
        yield return null; // 한 프레임 대기
        _rigBuilder?.Build();
    }
}