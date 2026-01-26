using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [Header("Boss Settings")]
    public Vector3 bossSpawnPosition;
    public float arenaMinX = 0f;
    public float arenaMaxX = 30f;

    private bool bossSpawned = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (bossSpawned) return;

        if (other.CompareTag("Player"))
        {
            SpawnBoss();
        }
    }

    void SpawnBoss()
    {
        bossSpawned = true;

        // Neon Guardian Boss olustur
        GameObject bossObj = new GameObject("NeonGuardian");
        bossObj.transform.position = bossSpawnPosition;
        bossObj.transform.localScale = new Vector3(2f, 2f, 1f);

        NeonGuardianBoss boss = bossObj.AddComponent<NeonGuardianBoss>();
        boss.SetArenaBounds(arenaMinX, arenaMaxX);

        // Boss muzigini baslat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossMusic();
        }

        // Trigger'i yok et (artik gerek yok)
        Destroy(gameObject);
    }
}
