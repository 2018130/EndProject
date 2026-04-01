using UnityEngine;
using UnityEngine.UI;

public class WaterUI : MonoBehaviour
{
    public static WaterUI Instance = null;

    [SerializeField] private Image[] waterTanks = new Image[4]; // 물통 4개

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetVisible(false); // 처음엔 물통 끄기
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateWater(float currentWater, float maxWater)
    {
        float tankSize = maxWater / waterTanks.Length;

        for (int i = 0; i < waterTanks.Length; i++)
        {
            int tankIndex = waterTanks.Length - 1 - i;

            float tankMin = tankSize * tankIndex;
            float fill = Mathf.Clamp01(
                (currentWater - tankMin) / tankSize
            );

            waterTanks[tankIndex].fillAmount = fill;
        }

    }

    public void SetVisible(bool visible)
    {
        foreach (var tank in waterTanks)
            tank.gameObject.SetActive(visible);
    }

}
