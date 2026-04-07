using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("唯一出生点ID（必须唯一）")]
    public string spawnID;

    void OnEnable()
    {
        RuntimeRefs.RegisterSpawnPoint(this);
    }

    void OnDisable()
    {
        RuntimeRefs.UnregisterSpawnPoint(this);
    }
}
