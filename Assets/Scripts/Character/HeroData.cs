using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "HeroData", menuName = "TurnBased/Hero Data")]
public class HeroData : ScriptableObject
{
    public string heroName;
    public Sprite sprite;
    public RuntimeAnimatorController animatorController;
    [Header("Power Stats")]
    public int maxHP = 100;
    public int attack = 10;
    public int speed = 10;
    public int energy = 50;
    public Skill specialSkill;

    [Header("Grid Position")]

    public int column = 1;

    public int row = 1;
}