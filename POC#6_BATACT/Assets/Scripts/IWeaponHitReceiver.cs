using UnityEngine;

public interface IWeaponHitReceiver
{
    void ReceiveWeaponHit(HitReactionData reaction, Vector2 impulse, GameObject source);
}
