using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "TurnBased/Skill")]
public class Skill : ScriptableObject
{
    [Header("Skill Properties")]
    public bool isAOE;
    public float damageMultiplier = 2.0f;
    public GameObject effectPrefab; // the meteor or animation

    [Header("Animation Timing")]
    public float castWindup = 0.5f; // Time before meteors start falling
    public float meteorSpawnHeight = 7.0f; // Height above target where meteor spawns
    public float meteorFallDuration = 0.8f; // Time for meteor to fall
    public float impactPause = 0.1f; // Pause after impact before continuing

    [Header("Audio (Optional)")]
    public AudioClip sfxCast;
    public AudioClip sfxImpact;
}
