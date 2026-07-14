using UnityEngine;

/// <summary>
/// Feeds simple player locomotion state into CharacterAnimationDriver.
/// The player only needs Run while moving and Stand while idle for now.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerAnimationStateProvider : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float movingSpeedThreshold = 0.05f;

    public string AnimationStateName => IsMoving ? "Run" : "Stand";
    public bool HasActiveCombatTarget => false;
    public bool WantsRunAnimation => IsMoving;
    public bool WantsWalkAnimation => false;

    private bool IsMoving
    {
        get
        {
            if (rb == null)
            {
                return false;
            }

            Vector3 flatVelocity = rb.velocity;
            flatVelocity.y = 0f;
            return flatVelocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold;
        }
    }

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }
}
