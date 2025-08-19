using UnityEngine;
using Helloop.Data;

public enum MeleeAnimationType
{
    Slash,
    Overhead,
    Thrust,
    Swing
}

[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "Helloop/Weapons/MeleeWeapon")]
public class MeleeWeaponData : WeaponData
{
    [Header("Melee Stats")]
    [Tooltip("Base durability before scaling")]
    public int durability = 100;

    [Tooltip("Base melee range in meters for hit detection")]
    public float range = 2.0f;

    [Tooltip("Light attack total time (windup+recovery) in seconds")]
    public float swingTime = 0.36f;

    [Tooltip("Heavy attack total time (windup+recovery) in seconds")]
    public float swipeTime = 0.58f;

    [Header("Angles (degrees)")]
    [Tooltip("Light attack arc width")]
    public float swingAngleDegrees = 75f;

    [Tooltip("Heavy attack arc width")]
    public float swipeAngleDegrees = 150f;

    [Tooltip("Dash attack arc width (narrow)")]
    public float dashAngleDegrees = 25f;

    [Header("Animation")]
    public MeleeAnimationType animationType = MeleeAnimationType.Slash;

    [Header("Slash Settings (Only for Slash type)")]
    [Tooltip("Use wider arc visuals for katanas/longswords, narrower for knives")]
    public bool useWideSlash = false;

    [Header("Hit Detection")]
    [Tooltip("Layers to ignore when detecting hits (like floors, walls, environment)")]
    public LayerMask ignoreLayers = 0;

    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip breakSound;
}
