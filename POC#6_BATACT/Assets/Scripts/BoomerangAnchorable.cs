using UnityEngine;

/// <summary>
/// Attach this to any object that the boomerang can latch onto for Rope Moves.
/// </summary>
public class BoomerangAnchorable : MonoBehaviour
{
    [SerializeField] private Transform hookPoint;

    /// <summary>
    /// Returns the exact world position where the boomerang should visually lock on.
    /// If hookPoint is not assigned, returns the transform's center position.
    /// </summary>
    public Vector3 GetAnchorPosition()
    {
        return hookPoint != null ? hookPoint.position : transform.position;
    }
}
