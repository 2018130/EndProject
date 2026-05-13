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
    // Inspector 연결 불필요 — OnNetworkSpawn에서 자동으로 찾음
    private Transform _handBone;
    public Transform HandBone => _handBone;

    [Header("Rig Layers")]
    [SerializeField] private Rig headAimRig;
    [SerializeField] private Rig spineAimRig;
    [SerializeField] private Rig armIKRig;
    //[SerializeField] private Rig handAimRig;

    [Header("Constraint References")]
    [SerializeField] private TwoBoneIKConstraint armIKConstraint;

    [Header("Weight Settings")]
    [SerializeField] private float aimingHeadWeight = 0.5f;
    [SerializeField] private float aimingSpineWeight = 0.25f;
    [SerializeField] private float aimingArmWeight = 1.0f;
    //[SerializeField] private float aimingHandWeight = 1.0f;
    [SerializeField] private float idleHeadWeight = 0.15f;
    [SerializeField] private float idleArmWeight = 0f;
    [SerializeField] private float idleSpineWeight = 0f;
    [SerializeField] private float weightLerpSpeed = 6f;
    [SerializeField] private float targetLerpSpeed = 12f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int upperBodyLayerIndex = 1; // Upper Body Layer 인덱스
    [SerializeField] private float animLayerLerpSpeed = 6f;

    private GunAlignToHand _currentGunAlign;
    private BaseWeapon _lastWeapon;
    private RigBuilder _rigBuilder;

    public override void OnNetworkSpawn()
    {
        _rigBuilder = GetComponent<RigBuilder>();

        // Animator에서 RightHand 본 자동으로 찾기
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            _handBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (_handBone == null)
                Debug.LogWarning("[AimRigController] RightHand 본을 찾지 못했습니다. Avatar 매핑을 확인하세요.");
        }

        // WeaponController의 무기 등록 이벤트 구독
        // 총이 나중에 생성되므로 WeaponController에 콜백을 연결
        if (weaponController != null)
            weaponController.OnWeaponRegistered += OnWeaponRegistered;
    }

    public override void OnNetworkDespawn()
    {
        if (weaponController != null)
            weaponController.OnWeaponRegistered -= OnWeaponRegistered;
    }

    private void OnWeaponRegistered(BaseWeapon weapon)
    {
        var align = weapon.GetComponent<GunAlignToHand>();
        if (align != null && _handBone != null)
        {
            align.SetHandBone(_handBone);
            // ★ AimTarget과 AimController도 함께 주입
            align.SetAimInfo(aimTarget, aimController);
        }

        var gripAim = weapon.GetComponentInChildren<GripTargetAim>();
        if (gripAim != null && aimTarget != null)
            gripAim.Initialize(aimTarget, aimController);
    }

    private void LateUpdate()
    {
        UpdateAimTarget();
        UpdateRigWeights();
        SyncCurrentGun();
    }

    private void UpdateAimTarget()
    {
        if (aimTarget == null || aimController == null) return;

        Vector3 targetPos = IsOwner
            ? aimController.AimWorldPoint
            : transform.position + aimController.NetAimDirection.Value * 10f;

        aimTarget.position = Vector3.Lerp(
            aimTarget.position, targetPos, targetLerpSpeed * Time.deltaTime);
    }

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
                isAiming ? aimingSpineWeight : idleSpineWeight, t);

        if (armIKRig != null)
            armIKRig.weight = Mathf.Lerp(armIKRig.weight,
                (isAiming && hasGun) ? aimingArmWeight : idleArmWeight, t);

        if (animator != null)
        {
            float currentLayerWeight = animator.GetLayerWeight(upperBodyLayerIndex);
            float targetLayerWeight = isAiming ? 1f : 0f;
            animator.SetLayerWeight(upperBodyLayerIndex,
                Mathf.Lerp(currentLayerWeight, targetLayerWeight, animLayerLerpSpeed * t));
        }
        //if (handAimRig != null)
        //    handAimRig.weight = Mathf.Lerp(handAimRig.weight,
        //        (isAiming && hasGun) ? aimingHandWeight : idleArmWeight, t);
    }

    private BaseWeapon _lastSyncedWeapon = null;

    private void SyncCurrentGun()
{
    if (weaponController == null) return;

    BaseWeapon current = weaponController.CurrentWeapon;
    if (current == _lastSyncedWeapon) return;

    if (_currentGunAlign != null)
        _currentGunAlign.StopAlign();

    _lastSyncedWeapon = current;

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
        if (gripTarget != null)
        {
            armIKConstraint.data.target = gripTarget;

            // ★ 진행 중인 코루틴 모두 중단 후 재시작
            StopAllCoroutines();
            StartCoroutine(RebuildSequence());
        }
        else
        {
            Debug.LogWarning($"[AimRigController] {current.name}에 GripTarget이 없습니다.");
        }
    }

}


private IEnumerator RebuildSequence()
{
    // 즉시 한 번
    _rigBuilder?.Build();
    
    // 다음 프레임에 한 번 더 (Animator 초기화 완료 후)
    yield return null;
    _rigBuilder?.Build();
    
    Debug.Log($"[AimRigController] RebuildSequence 완료 - target: {armIKConstraint?.data.target?.name}");
}
}