using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnergyBar : MonoBehaviour
{
    private Transform fill;
    private TextMeshProUGUI currentManaText;

    private HeroUnit heroUnit;

    // Start is called before the first frame update
    void Start()
    {
        fill = transform.Find("Fill");
        currentManaText = transform.Find("Canvas/currentMP").GetComponent<TextMeshProUGUI>();


        // Find the HeroUnit component in parent hierarchy
        heroUnit = GetComponentInParent<HeroUnit>();

        if (heroUnit != null)
        {
            // Subscribe to energy change events
            heroUnit.OnEnergyChanged.AddListener(OnEnergyChanged);

            // Initialize with current energy
            SetEnergy(heroUnit.CurrentEnergy);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (heroUnit != null)
        {
            heroUnit.OnEnergyChanged.RemoveListener(OnEnergyChanged);
        }
    }

    // Called when hero's energy changes
    private void OnEnergyChanged(int newEnergy)
    {
        // Convert energy (0-100) to normalized value (0-1)
        // float normalizedEnergy = newEnergy / 100f;
        SetEnergy(newEnergy);
    }

    // Set energy bar fill amount (0-1 range or 0-100 range)
    public void SetEnergy(float energy)
    {
        if (fill == null)
        {
            fill = transform.Find("Fill");
        }
        
        if (currentManaText == null)
        {
            currentManaText = transform.Find("Canvas/currentMP")?.GetComponent<TextMeshProUGUI>();
        }

        if (fill != null)
        {
            // If energy is greater than 1, assume it's in 0-100 range and normalize it
            float normalizedEnergy = energy > 1f ? energy / 100f : energy;

            // Clamp between 0 and 1
            normalizedEnergy = Mathf.Clamp01(normalizedEnergy);

            fill.localScale = new Vector3(normalizedEnergy, 1, 1);
        }
        
        if (currentManaText != null)
        {
            currentManaText.text = energy.ToString() + "%";
        }
    }

    // Get current energy bar fill amount (0-1 range)
    public float GetEnergy()
    {
        if (fill == null)
        {
            fill = transform.Find("Fill");
        }

        return fill != null ? fill.localScale.x : 0f;
    }
}
