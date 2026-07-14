/// <summary>
/// Exposes lightweight movement/combat state to CharacterAnimationDriver without coupling it
/// to a specific summon or enemy AI implementation.
/// </summary>
public interface ICharacterAnimationStateProvider
{
    string AnimationStateName { get; }
    bool HasActiveCombatTarget { get; }
    bool WantsRunAnimation { get; }
    bool WantsWalkAnimation { get; }
}
