using UnityEngine;

/// <summary>
/// Bridges gameplay state to the character Animator. Action priority is owned by CharacterActionController;
/// this component only plays requested action states and low-priority locomotion loops.
/// </summary>
public sealed class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterRuntimeStats stats;
    [SerializeField] private CombatUnitAI combatAI;
    [SerializeField] private PlayerSummonController summonController;
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private string walkStateName = "Walk";
    [SerializeField] private string runStateName = "Run";
    [SerializeField] private string standStateName = "Summon_0Stand";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string attackSecondStateName;
    [SerializeField] private string hitStateName = "Hit";
    [SerializeField] private string deathStateName = "Death";
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private float crossFadeDuration = 0.08f;
    [SerializeField] private float fallbackActionLockDuration = 0.35f;
    [SerializeField] private float attackLockDuration = 1.667f;
    [SerializeField] private float hitLockDuration = 0.7f;
    [SerializeField] private float deathDuration = 3.9f;
    [SerializeField, Range(0.01f, 0.99f)] private float attackSecondStateStartNormalizedTime = 0.5f;


    private Coroutine attackSequenceRoutine;
    private Vector3 previousPosition;
    private float actionLockedUntil;
    private string currentLoopState;
    private bool deathPlayed;
    private int walkStateHash;
    private int walkFullPathHash;
    private int runStateHash;
    private int runFullPathHash;
    private int standStateHash;
    private int standFullPathHash;
    private int attackStateHash;
    private int attackFullPathHash;
    private int attackSecondStateHash;
    private int attackSecondFullPathHash;
    private int hitStateHash;
    private int hitFullPathHash;
    private int deathStateHash;
    private int deathFullPathHash;

    public float AttackDuration => Mathf.Max(attackLockDuration, fallbackActionLockDuration);
    public float HitDuration => Mathf.Max(hitLockDuration, fallbackActionLockDuration);

    private void Awake()
    {
        CacheStateHashes();
        ResolveReferences();
        previousPosition = transform.position;
    }

    private void OnValidate()
    {
        CacheStateHashes();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (stats != null)
        {
            stats.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (animator == null || deathPlayed)
        {
            previousPosition = transform.position;
            return;
        }

        if (Time.time < actionLockedUntil)
        {
            previousPosition = transform.position;
            return;
        }

        CrossFadeLoop(GetLoopStateName());
        previousPosition = transform.position;
    }

    /// <summary>
    /// Plays the configured attack action and prevents locomotion from overriding it until duration ends.
    /// </summary>
    public void PlayAttack()
    {
        PlayAttack(AttackDuration);
    }

    public void PlayAttack(float duration)
    {
        float safeDuration = Mathf.Max(duration, fallbackActionLockDuration);
        if (PlaySplitAttack(safeDuration))
        {
            return;
        }

        PlayAction(attackStateName, safeDuration);
    }

    public void PlayHit()
    {
        PlayHit(HitDuration);
    }

    public void PlayHit(float duration)
    {
        if (deathPlayed)
        {
            return;
        }

        StopAttackSequence();
        PlayAction(hitStateName, Mathf.Max(duration, fallbackActionLockDuration));
    }

    public void PlayDeath()
    {
        if (deathPlayed)
        {
            return;
        }

        deathPlayed = true;
        StopAttackSequence();
        actionLockedUntil = float.PositiveInfinity;
        currentLoopState = null;
        if (animator != null && HasState(deathStateName))
        {
            animator.CrossFadeInFixedTime(deathStateName, crossFadeDuration);
        }
    }

    public float GetDeathDuration(float fallbackDuration)
    {
        return Mathf.Max(deathDuration, fallbackDuration);
    }

    private void ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }

        if (combatAI == null)
        {
            combatAI = GetComponent<CombatUnitAI>();
        }

        if (summonController == null)
        {
            summonController = GetComponent<PlayerSummonController>();
        }

        if (actionController == null)
        {
            actionController = GetComponent<CharacterActionController>();
        }
    }

    private void CacheStateHashes()
    {
        walkStateHash = Animator.StringToHash(walkStateName);
        walkFullPathHash = Animator.StringToHash("Base Layer." + walkStateName);
        runStateHash = Animator.StringToHash(runStateName);
        runFullPathHash = Animator.StringToHash("Base Layer." + runStateName);
        standStateHash = Animator.StringToHash(standStateName);
        standFullPathHash = Animator.StringToHash("Base Layer." + standStateName);
        attackStateHash = Animator.StringToHash(attackStateName);
        attackFullPathHash = Animator.StringToHash("Base Layer." + attackStateName);
        attackSecondStateHash = Animator.StringToHash(attackSecondStateName);
        attackSecondFullPathHash = Animator.StringToHash("Base Layer." + attackSecondStateName);
        hitStateHash = Animator.StringToHash(hitStateName);
        hitFullPathHash = Animator.StringToHash("Base Layer." + hitStateName);
        deathStateHash = Animator.StringToHash(deathStateName);
        deathFullPathHash = Animator.StringToHash("Base Layer." + deathStateName);
    }

    private string GetLoopStateName()
    {
        if (stats != null && stats.IsDead)
        {
            return deathStateName;
        }

        if (combatAI != null && combatAI.CurrentStateName == "Attack" && HasState(standStateName))
        {
            return standStateName;
        }

        if (ShouldUseRunAnimation())
        {
            return runStateName;
        }

        if (ShouldUseWalkAnimation())
        {
            return walkStateName;
        }

        return HasState(standStateName) ? standStateName : walkStateName;
    }

    private bool ShouldUseRunAnimation()
    {
        if (summonController != null && summonController.IsReturningToPlayer)
        {
            return true;
        }

        if (combatAI != null)
        {
            string state = combatAI.CurrentStateName;
            if (state == "Chase" || state == "HoldDistance")
            {
                return true;
            }
        }

        Vector3 delta = transform.position - previousPosition;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        return speed > movementThreshold && combatAI != null && combatAI.HasActiveTarget;
    }

    private bool ShouldUseWalkAnimation()
    {
        Vector3 delta = transform.position - previousPosition;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        return speed > movementThreshold;
    }

    private void HandleDied(CharacterRuntimeStats deadStats)
    {
        PlayDeath();
    }

    private void PlayAction(string stateName, float lockDuration)
    {
        if (animator == null || string.IsNullOrEmpty(stateName) || !HasState(stateName))
        {
            return;
        }

        StopAttackSequence();
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration);
        actionLockedUntil = Time.time + Mathf.Max(0f, lockDuration);
        currentLoopState = null;
    }

    private bool PlaySplitAttack(float lockDuration)
    {
        if (animator == null || string.IsNullOrEmpty(attackSecondStateName) || !HasState(attackStateName) || !HasState(attackSecondStateName))
        {
            return false;
        }

        StopAttackSequence();
        animator.CrossFadeInFixedTime(attackStateName, crossFadeDuration);
        actionLockedUntil = Time.time + Mathf.Max(0f, lockDuration);
        currentLoopState = null;
        attackSequenceRoutine = StartCoroutine(PlaySplitAttackRoutine(lockDuration));
        return true;
    }

    private System.Collections.IEnumerator PlaySplitAttackRoutine(float lockDuration)
    {
        float switchDelay = Mathf.Max(0f, lockDuration * Mathf.Clamp01(attackSecondStateStartNormalizedTime));
        yield return new WaitForSeconds(switchDelay);

        if (!deathPlayed && animator != null && HasState(attackSecondStateName))
        {
            animator.CrossFadeInFixedTime(attackSecondStateName, crossFadeDuration);
        }

        attackSequenceRoutine = null;
    }

    private void StopAttackSequence()
    {
        if (attackSequenceRoutine != null)
        {
            StopCoroutine(attackSequenceRoutine);
            attackSequenceRoutine = null;
        }
    }

    private void CrossFadeLoop(string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || currentLoopState == stateName || !HasState(stateName))
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateName, crossFadeDuration);
        currentLoopState = stateName;
    }

    private bool HasState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        if (stateName == walkStateName)
        {
            return animator.HasState(0, walkStateHash) || animator.HasState(0, walkFullPathHash);
        }

        if (stateName == runStateName)
        {
            return animator.HasState(0, runStateHash) || animator.HasState(0, runFullPathHash);
        }

        if (stateName == standStateName)
        {
            return animator.HasState(0, standStateHash) || animator.HasState(0, standFullPathHash);
        }

        if (stateName == attackStateName)
        {
            return animator.HasState(0, attackStateHash) || animator.HasState(0, attackFullPathHash);
        }

        if (stateName == attackSecondStateName)
        {
            return animator.HasState(0, attackSecondStateHash) || animator.HasState(0, attackSecondFullPathHash);
        }

        if (stateName == hitStateName)
        {
            return animator.HasState(0, hitStateHash) || animator.HasState(0, hitFullPathHash);
        }

        if (stateName == deathStateName)
        {
            return animator.HasState(0, deathStateHash) || animator.HasState(0, deathFullPathHash);
        }

        return animator.HasState(0, Animator.StringToHash(stateName));
    }
}
