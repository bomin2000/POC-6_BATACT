using UnityEngine;

public sealed class StatusTagController : MonoBehaviour
{
    private float pulledExpiration = -1f;
    private float exposedExpiration = -1f;
    private float guardBrokenExpiration = -1f;

    public bool HasTag(StatusTag tag)
    {
        if (tag == StatusTag.None) return false;
        
        bool has = false;
        if ((tag & StatusTag.Pulled) != 0) has |= (Time.time < pulledExpiration);
        if ((tag & StatusTag.Exposed) != 0) has |= (Time.time < exposedExpiration);
        if ((tag & StatusTag.GuardBroken) != 0) has |= (Time.time < guardBrokenExpiration);
        return has;
    }

    public bool HasAnyTag()
    {
        return (Time.time < pulledExpiration) || 
               (Time.time < exposedExpiration) || 
               (Time.time < guardBrokenExpiration);
    }

    public void AddTag(StatusTag tag, float duration)
    {
        if (tag == StatusTag.None || duration <= 0f) return;

        if ((tag & StatusTag.Pulled) != 0) pulledExpiration = Time.time + duration;
        if ((tag & StatusTag.Exposed) != 0) exposedExpiration = Time.time + duration;
        if ((tag & StatusTag.GuardBroken) != 0) guardBrokenExpiration = Time.time + duration;
    }
}
