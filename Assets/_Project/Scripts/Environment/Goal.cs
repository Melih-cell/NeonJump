using UnityEngine;

public class Goal : MonoBehaviour
{
    public int bonusScore = 1000;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(bonusScore);
                GameManager.Instance.Win();
            }
        }
    }
}
