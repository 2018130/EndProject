using UnityEngine;

public class SpawnAreaManager : MonoBehaviour
{
    public static SpawnAreaManager Instance { get; private set; }

    [SerializeField] private SpawnArea[] spawnAreas;

    private void Awake()
    {
        Instance = this;
    }

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