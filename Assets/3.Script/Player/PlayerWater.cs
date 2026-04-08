using Unity.Netcode;
using UnityEngine;

public class PlayerWater : NetworkBehaviour
{
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float jetpackDrain = 20f;
    [SerializeField] private float rechargeRate = 10f;  // 초당 충전량
    [SerializeField] private float rechargeDelay = 1f;  // 충전 시작 대기 시간

    public bool HasEnoughWater(float amount) => Water.Value >= amount;
    public bool HasWater() => Water.Value > 0;

    private float lastWaterUseTime = -999f;

    private WaterUI waterUI;

    public NetworkVariable<float> Water = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        Water.OnValueChanged += OnWaterChanged;

        // 내 캐릭터일 때만 UI 초기화
        if (IsOwner && IsLocalPlayer)
        {
            waterUI = FindAnyObjectByType<WaterUI>();

            Debug.Log($"WaterUI 찾음: {waterUI != null}");

            waterUI?.SetVisible(true);
            waterUI?.UpdateWater(Water.Value, maxWater);
        }
        else
        {
            Debug.Log($"[{OwnerClientId}] IsOwner:False - UI 숨김");
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        RechargeWater();
    }

    private void RechargeWater()
    {
        if (Water.Value >= maxWater) return;

        // 마지막 사용 후 1초 지났으면 충전
        if (Time.time - lastWaterUseTime >= rechargeDelay)
        {
            Water.Value = Mathf.Min(Water.Value + rechargeRate * Time.fixedDeltaTime, maxWater);
        }
    }

    private void OnWaterChanged(float oldVal, float newVal)
    {
        // 내 캐릭터일 때만 UI 업데이트
        if (!IsOwner) return;
        waterUI?.UpdateWater(newVal, maxWater);
    }

    public bool UseWaterForShot(float amount)
    {
        if (Water.Value < amount) return false;
        Water.Value -= amount;
        lastWaterUseTime = Time.time;
        return true;
    }


    public void UseWaterForJetpack()
    {
        if (!IsServer) return;
        if (Water.Value <= 0) return;

        Water.Value -= jetpackDrain * Time.fixedDeltaTime;
        Water.Value = Mathf.Max(0, Water.Value);
        lastWaterUseTime = Time.time;
    }


}
