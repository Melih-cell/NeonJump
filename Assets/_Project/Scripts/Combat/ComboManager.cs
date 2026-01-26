using UnityEngine;

/// <summary>
/// Combo sistemi - AdvancedHUD ile entegrasyon icin
/// </summary>
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Settings")]
    public float comboTimeout = 2f;
    public int maxMultiplier = 10;

    // Public properties for HUD
    public int currentCombo { get; private set; }
    public int multiplier { get; private set; }
    public float comboTimer { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // GameManager'dan combo bilgilerini al
        if (GameManager.Instance != null)
        {
            currentCombo = GameManager.Instance.GetCombo();
            multiplier = GameManager.Instance.GetComboMultiplier();

            // Timer tahmini (GameManager'da direkt erisim yok)
            if (currentCombo > 0)
            {
                comboTimer = Mathf.Max(0, comboTimer - Time.deltaTime);
                if (comboTimer <= 0)
                {
                    comboTimer = comboTimeout; // Combo aktifse timer'i resetle
                }
            }
            else
            {
                comboTimer = 0;
            }
        }
    }

    // GameManager tarafindan cagrilabilir
    public void OnComboHit()
    {
        comboTimer = comboTimeout;
    }

    public void ResetCombo()
    {
        currentCombo = 0;
        multiplier = 1;
        comboTimer = 0;
    }
}
