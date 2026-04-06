using UnityEngine;
using UnityEngine.UI;

public class WaterUI : MonoBehaviour
{
    [SerializeField] private Image[] waterTanks = new Image[4]; // π∞≈Î 4∞≥


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

            if (waterTanks[tankIndex] != null)
                waterTanks[tankIndex].fillAmount = fill;
        }

    }

    public void SetVisible(bool visible)
    {
        foreach (var tank in waterTanks)
        {
            if (tank != null)
                tank.gameObject.SetActive(visible);
        }
    }

}
