using UnityEngine;

public class SpawnAreaManager : MonoBehaviour
{

    [SerializeField] private SpawnArea[] spawnAreas;

    public Vector3 GetSpawnPosition(Faction faction)
    {
        foreach (var area in spawnAreas)
        {
            if (area.OwnerFaction == faction)
                return area.GetRandomSpawnPoint();
        }

        Debug.LogWarning($"[SpawnAreaManager] {faction} 팩션의 스폰 에리어 없음");
        return Vector3.zero;
    }
}