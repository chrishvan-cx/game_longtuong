using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeHP : MonoBehaviour
{
    private Transform fillHP;
    private Transform fillSpriteHP;
    public static CodeHP Create(Vector3 position, Vector3 size)
    {
        // Main HP
        GameObject HP = new GameObject("HP");
        HP.transform.position = position;
        CodeHP codeHP = HP.AddComponent<CodeHP>();

        float convertScale = size.x / 100f;

        // Background HP
        GameObject bgHP = new GameObject("bgHP", typeof(SpriteRenderer));
        bgHP.transform.SetParent(HP.transform);
        bgHP.transform.position = position;
        bgHP.transform.localPosition = Vector3.zero;
        bgHP.transform.localScale = size;
        bgHP.GetComponent<SpriteRenderer>().sprite = GetGameAssets.Instance.WhitePixel;
        bgHP.GetComponent<SpriteRenderer>().color = Color.gray;

        // Fill HP
        GameObject fillHP = new GameObject("fillHP", typeof(SpriteRenderer));
        fillHP.transform.SetParent(HP.transform);
        fillHP.transform.localPosition = new Vector3(-convertScale / 2f, 0, 0);

        // Fill Sprite HP
        GameObject fillSpriteHP = new GameObject("fillSpriteHP", typeof(SpriteRenderer));
        fillSpriteHP.transform.SetParent(fillHP.transform);
        fillSpriteHP.transform.position = position;
        fillSpriteHP.transform.localPosition = new Vector3(convertScale / 2f, 0, 0);
        fillSpriteHP.transform.localScale = size;
        fillSpriteHP.GetComponent<SpriteRenderer>().sprite = GetGameAssets.Instance.WhitePixel;
        fillSpriteHP.GetComponent<SpriteRenderer>().color = Color.red;

        // ✅ Save reference
        codeHP.fillHP = fillHP.transform;

        return codeHP;
    }

    public void SetHealth(float health)
    {
        fillHP.localScale = new Vector3(health, 1, 1);
    }

    public float GetHealth()
    {
        return fillHP.localScale.x;
    }

}
