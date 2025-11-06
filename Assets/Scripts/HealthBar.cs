using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    private Transform fill;
    // Start is called before the first frame update
    void Start()
    {
        fill = transform.Find("Fill");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetHealth(float health)
    {
        fill.localScale = new Vector3(health, 1, 1);
    }

    public float GetHealth()
    {
        return fill.localScale.x;
    }
}
