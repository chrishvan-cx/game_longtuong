using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slot : MonoBehaviour
{
    public int slotIndex; // 0..6
    public int teamId; // 0 or 1
    public Transform spawnPoint; // child transform where unit will be instantiated
    public HeroData initialHero; // optional - set in inspector for quick tests
    [HideInInspector] public HeroUnit occupied;
}