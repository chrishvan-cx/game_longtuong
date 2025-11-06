using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetGameAssets : MonoBehaviour
{
    private static GetGameAssets instance;
    public static GetGameAssets Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Instantiate(Resources.Load<GetGameAssets>("GetGameAssets"));
            }
            return instance;

        }
    }
    [Header("Common Sprites")]
    public Sprite WhitePixel;
}
