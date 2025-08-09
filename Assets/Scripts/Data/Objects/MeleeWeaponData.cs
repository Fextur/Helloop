using UnityEngine;
using Helloop.Data;

public enum MeleeAnimationType
{
    Slash,
    Overhead,
    Thrust,
    Punch,
    Swing
}

[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "Helloop/Weapons/MeleeWeapon")]
public class MeleeWeaponData : WeaponData
{
    [Header("Melee Stats")]
    public int durability;
    public float range;
    public float swingTime;

    [Header("Animation")]
    public MeleeAnimationType animationType = MeleeAnimationType.Slash;

    [Header("Slash Settings (Only for Slash type)")]
    [Tooltip("Use wider arc for katanas/longswords, narrower for knives")]
    public bool useWideSlash = false;

    [Header("Hit Detection")]
    [Tooltip("Layers to ignore when detecting hits (like floors, walls, environment)")]
    public LayerMask ignoreLayers = 0;

    [Header("Audio")]
    public AudioClip swingSound;
    public AudioClip breakSound;

}