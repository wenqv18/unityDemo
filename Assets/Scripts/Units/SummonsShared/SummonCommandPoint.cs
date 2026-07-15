using UnityEngine;

/// <summary>
/// Shared non-combat command point for summons. Combat state remains owned by each summon AI.
/// </summary>
public static class SummonCommandPoint
{
    public const float DefaultMarkerRadius = 5f;

    public static bool HasMarker { get; private set; }
    public static Vector3 MarkerPosition { get; private set; }
    public static float MarkerRadius { get; private set; } = DefaultMarkerRadius;

    public static void SetMarker(Vector3 position)
    {
        SetMarker(position, DefaultMarkerRadius);
    }

    public static void SetMarker(Vector3 position, float radius)
    {
        MarkerPosition = position;
        MarkerRadius = Mathf.Max(0.1f, radius);
        HasMarker = true;
    }

    public static void Clear()
    {
        HasMarker = false;
        MarkerPosition = Vector3.zero;
        MarkerRadius = DefaultMarkerRadius;
    }
}
