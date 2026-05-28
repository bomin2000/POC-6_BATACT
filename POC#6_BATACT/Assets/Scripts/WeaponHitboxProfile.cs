using UnityEngine;

[CreateAssetMenu(menuName = "POC/Dual Blade/Weapon Hitbox Profile")]
public sealed class WeaponHitboxProfile : ScriptableObject
{
    [Header("Identity")]
    public WeaponState state;

    [Header("Shape")]
    public HitboxShape shape = HitboxShape.Box;
    public Vector2 localOffset = new Vector2(1.2f, 0f);
    public Vector2 boxSize = new Vector2(1.8f, 0.45f);
    public float radius = 1.5f;

    [Header("Timing")]
    public float startupSeconds = 0.05f;
    public float activeSeconds = 0.08f;
    public float recoverySeconds = 0.18f;

    [Header("Combat")]
    public HitReactionData reaction;
}

public enum HitboxShape
{
    Box,
    Circle
}
