using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "TurnBased/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;

    [Header("Skill Type")]
    public SkillType skillType = SkillType.Meteor;

    [Header("Skill Properties")]
    public bool isAOE;
    public float damageMultiplier = 2.0f;
    public GameObject effectPrefab; // the meteor or projectile effect

    [Header("Animation Timing")]
    public float castWindup = 0.5f; // Time before skill activates

    [Header("Meteor Settings (for Meteor type)")]
    public float meteorSpawnHeight = 7.0f; // Height above target where meteor spawns
    public float meteorFallDuration = 0.8f; // Time for meteor to fall

    [Header("Projectile Settings (for Projectile type)")]
    public float projectileSpeed = 7f; // Speed of projectile
    public float projectileSpawnOffsetX = 0.5f; // Offset from caster position
    public float projectileSpawnOffsetY = 1.0f; // Height offset from caster

    [Header("Impact Settings")]
    public float impactPause = 0.3f; // Fade out duration after impact

    [Header("Audio (Optional)")]
    public AudioClip sfxCast;
    public AudioClip sfxImpact;
}
