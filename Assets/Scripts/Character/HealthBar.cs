using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HealthBar : MonoBehaviour
{
    private Transform fill;
    private TextMeshProUGUI currentHPText;
    
    void Start()
    {
        InitializeReferences();
    }
    
    private void InitializeReferences()
    {
        if (fill == null)
        {
            fill = transform.Find("Fill");
        }
        
        if (currentHPText == null)
        {
            currentHPText = transform.Find("Canvas/currentHP")?.GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetHealth(int health, int maxHP)
    {
        // Make sure references are initialized
        InitializeReferences();
        
        if (fill != null)
        {
            fill.localScale = new Vector3((float)health / maxHP, 1, 1);
        }
        
        if (currentHPText != null)
        {
            currentHPText.text = health.ToString() + "/" + maxHP.ToString();
        }
    }
    
    public float GetHealth()
    {
        InitializeReferences();
        return fill != null ? fill.localScale.x : 0f;
    }
}
