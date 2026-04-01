using Unity.Netcode;
using UnityEngine;

public class PlayerWater : NetworkBehaviour
{
    [SerializeField] private float maxWater = 100f;
    [SerializeField] private float jetpackDrain = 20f;

    public NetworkVariable<float> Water = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        Water.OnValueChanged += OnWaterChanged;

        // 내 캐릭터일 때만 UI 초기화
        if (IsOwner)
        {
            WaterUI.Instance?.SetVisible(true);
            WaterUI.Instance?.UpdateWater(Water.Value, maxWater);
        }
    }

    private void OnWaterChanged(float oldVal, float newVal)
    {
        // 내 캐릭터일 때만 UI 업데이트
        if (!IsOwner) return;
        WaterUI.Instance?.UpdateWater(newVal, maxWater);
    }

    public bool UseWaterForShot(float amount)
    {
        if (Water.Value < amount) return false;
        Water.Value -= amount;
        return true;
    }


    public void UseWaterForJetpack()
    {
        if (!IsServer) return;
        if (Water.Value <= 0) return;

        Water.Value -= jetpackDrain * Time.fixedDeltaTime;
        Water.Value = Mathf.Max(0, Water.Value);
    }

    public bool HasWater() => Water.Value > 0;
}
