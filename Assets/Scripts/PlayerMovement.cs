using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 25f;
    public float staminaRegenDelay = 1.5f;
    public float lightAttackCost = 15f;
    public float heavyAttackCost = 35f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool startsArmed = true;
    [SerializeField] private float animationFadeTime = 0.15f;
    [SerializeField] private float jumpLockTime = 0.75f;
    [SerializeField] private float lightAttackLockTime = 0.55f;
    [SerializeField] private float heavyAttackLockTime = 1.1f;
    [SerializeField] private float comboWindowDuration = 0.65f;
    [SerializeField] private float equipLockTime = 1f;
    [SerializeField] private float tauntLockTime = 1.4f;
    [SerializeField] private float hitLockTime = 0.8f;
    [SerializeField] private float idleVariantDelay = 4f;
    [SerializeField] private float idleVariantLockTime = 2f;

    [Header("Distance Matching")]
    [SerializeField] private bool distanceMatchLocomotion = true;
    [SerializeField] private float runCycleDistance = 2.4f;
    [SerializeField] private float walkCycleDistance = 1.4f;

    [Header("Standing States")]
    [SerializeField] private string standingIdleStateName = "standing idle";
    [SerializeField] private string standingRunStateName = "standing run forward";
    [SerializeField] private string standingWalkStateName = "standing walk forward";
    [SerializeField] private string standingJumpStateName = "standing jump";
    [SerializeField] private string crouchStateName = "crouch idle";
    [SerializeField] private string crouchToStandStateName = "crouch to standing idle";

    [Header("Unarmed States")]
    [SerializeField] private string unarmedIdleStateName = "unarmed idle";
    [SerializeField] private string unarmedRunStateName = "unarmed run forward";
    [SerializeField] private string unarmedWalkStateName = "unarmed walk forward";
    [SerializeField] private string unarmedJumpStateName = "unarmed jump";

    [Header("Action States")]
    [SerializeField] private string[] standingIdleVariantStateNames =
    {
        "standing idle looking ver. 1",
        "standing idle looking ver. 2"
    };
    [SerializeField] private string[] unarmedIdleVariantStateNames =
    {
        "unarmed idle looking ver. 1",
        "unarmed idle looking ver. 2"
    };
    // 3-hit light combo played in order
    [SerializeField] private string[] lightComboStateNames =
    {
        "standing melee attack horizontal",
        "standing melee attack downward",
        "standing melee attack backhand"
    };
    // Heavy attack — cycles between variants
    [SerializeField] private string[] heavyAttackStateNames =
    {
        "standing melee attack 360 high",
        "standing melee attack 360 low"
    };
    [SerializeField] private string[] tauntStateNames =
    {
        "standing taunt battlecry",
        "standing taunt chest thump"
    };
    [SerializeField] private string[] hitStateNames =
    {
        "standing react large gut",
        "standing react large from left",
        "standing react large from right",
        "standing block react large"
    };
    [SerializeField] private string[] equipStateNames =
    {
        "unarmed equip over shoulder",
        "unarmed equip underarm"
    };
    [SerializeField] private string[] disarmStateNames =
    {
        "standing disarm over shoulder",
        "standing disarm underarm"
    };

    // Exposed for UI
    public float CurrentStamina => currentStamina;
    public float MaxStamina     => maxStamina;
    public int   ComboStep      => comboStep;
    public int   ComboMaxSteps  => lightComboStateNames.Length;
    public float ComboWindowEnd => comboWindowEnd;

    private CharacterController controller;
    private PlayerControls controls;
    private Transform movementRoot;
    private Vector2 moveInput;
    private bool isArmed;
    private bool wasCrouching;
    private float animationLockedUntil;
    private int nextHeavyAttackIndex;
    private int nextTauntIndex;
    private int nextHitIndex;
    private int nextEquipIndex;
    private int nextDisarmIndex;
    private int nextStandingIdleVariantIndex;
    private int nextUnarmedIdleVariantIndex;
    private float nextIdleVariantTime;
    private float locomotionDistance;
    private int currentDistanceMatchedStateHash;

    // Stamina
    private float currentStamina;
    private float lastStaminaDrainTime;

    // Combo
    private int comboStep;          // 0-2, which hit comes next
    private float comboWindowEnd;   // deadline for next queued hit to fire
    private bool comboPending;      // LMB pressed during lock — fire when lock ends

    private AnimationState standingIdleState;
    private AnimationState standingRunState;
    private AnimationState standingWalkState;
    private AnimationState standingJumpState;
    private AnimationState crouchState;
    private AnimationState crouchToStandState;
    private AnimationState unarmedIdleState;
    private AnimationState unarmedRunState;
    private AnimationState unarmedWalkState;
    private AnimationState unarmedJumpState;
    private AnimationState[] standingIdleVariantStates;
    private AnimationState[] unarmedIdleVariantStates;
    private AnimationState[] lightComboStates;
    private AnimationState[] heavyAttackStates;
    private AnimationState[] tauntStates;
    private AnimationState[] hitStates;
    private AnimationState[] equipStates;
    private AnimationState[] disarmStates;

    private struct AnimationState
    {
        public int Hash;
        public bool Exists;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>() ?? GetComponentInParent<CharacterController>();
        movementRoot = controller != null ? controller.transform : transform;
        animator = animator != null
            ? animator
            : movementRoot.GetComponentInChildren<Animator>() ?? GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
        isArmed = startsArmed;
        currentStamina = maxStamina;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            TryAssignAnimatorControllerInEditor();
        }

        controls = new PlayerControls();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        CacheAnimatorStates();
    }

    private void Start()
    {
        PlayState(GetIdleState(), true);
        ResetIdleVariantTimer();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        bool isWalking = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float speed = isWalking ? walkSpeed : runSpeed;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        Quaternion isoRotation = Quaternion.Euler(0f, 45f, 0f);
        move = isoRotation * move;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        if (move != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            movementRoot.rotation = Quaternion.Slerp(
                movementRoot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        Vector3 positionBeforeMove = movementRoot.position;

        if (controller != null)
            controller.Move(move * speed * Time.deltaTime);
        else
            movementRoot.position += move * speed * Time.deltaTime;

        float distanceMoved = Vector3.ProjectOnPlane(
            movementRoot.position - positionBeforeMove,
            Vector3.up
        ).magnitude;

        RegenStamina();
        UpdateAnimation(move.sqrMagnitude > 0.0001f, distanceMoved, isWalking);
    }

    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;

        if (animator != null)
            animator.applyRootMotion = false;

        CacheAnimatorStates();
    }

    public void PlayHitReaction()
    {
        PlayNextLockedState(hitStates, ref nextHitIndex, hitLockTime);
    }

    // -------------------------------------------------------
    // Stamina
    // -------------------------------------------------------

    private void RegenStamina()
    {
        if (Time.time - lastStaminaDrainTime >= staminaRegenDelay)
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    private void DrainStamina(float amount)
    {
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        lastStaminaDrainTime = Time.time;
    }

    // -------------------------------------------------------
    // Attack helpers
    // -------------------------------------------------------

    private bool TryLightAttack()
    {
        if (currentStamina < lightAttackCost)
            return false;

        if (lightComboStates == null || lightComboStates.Length == 0)
            return false;

        AnimationState state = lightComboStates[comboStep % lightComboStates.Length];
        if (!state.Exists)
            return false;

        DrainStamina(lightAttackCost);
        PlayLockedState(state, lightAttackLockTime);
        // Next hit can be queued until the lock ends + window
        comboWindowEnd = animationLockedUntil + comboWindowDuration;
        comboStep = (comboStep + 1) % lightComboStates.Length;
        return true;
    }

    private bool TryHeavyAttack()
    {
        if (currentStamina < heavyAttackCost)
            return false;

        bool played = PlayNextLockedState(heavyAttackStates, ref nextHeavyAttackIndex, heavyAttackLockTime);
        if (!played)
            return false;

        DrainStamina(heavyAttackCost);
        // Heavy attack resets the light combo
        comboStep = 0;
        comboWindowEnd = 0f;
        comboPending = false;
        return true;
    }

    // -------------------------------------------------------
    // Animation
    // -------------------------------------------------------

    private void CacheAnimatorStates()
    {
        standingIdleState = MakeState(standingIdleStateName);
        standingRunState = MakeState(standingRunStateName);
        standingWalkState = MakeState(standingWalkStateName);
        standingJumpState = MakeState(standingJumpStateName);
        crouchState = MakeState(crouchStateName);
        crouchToStandState = MakeState(crouchToStandStateName);
        unarmedIdleState = MakeState(unarmedIdleStateName);
        unarmedRunState = MakeState(unarmedRunStateName);
        unarmedWalkState = MakeState(unarmedWalkStateName);
        unarmedJumpState = MakeState(unarmedJumpStateName);
        standingIdleVariantStates = MakeStates(standingIdleVariantStateNames);
        unarmedIdleVariantStates = MakeStates(unarmedIdleVariantStateNames);
        lightComboStates = MakeStates(lightComboStateNames);
        heavyAttackStates = MakeStates(heavyAttackStateNames);
        tauntStates = MakeStates(tauntStateNames);
        hitStates = MakeStates(hitStateNames);
        equipStates = MakeStates(equipStateNames);
        disarmStates = MakeStates(disarmStateNames);
    }

    private AnimationState MakeState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return default;

        int hash = Animator.StringToHash($"Base Layer.{SanitizeStateName(stateName)}");
        return new AnimationState { Hash = hash, Exists = animator.HasState(0, hash) };
    }

    private AnimationState[] MakeStates(string[] stateNames)
    {
        AnimationState[] states = new AnimationState[stateNames.Length];
        for (int i = 0; i < stateNames.Length; i++)
            states[i] = MakeState(stateNames[i]);
        return states;
    }

    private void UpdateAnimation(bool isMoving, float distanceMoved, bool isWalking)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        bool crouchHeld = Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed);

        // Reset combo if the window expired and no input was queued
        if (comboStep > 0 && !comboPending && Time.time > comboWindowEnd)
            comboStep = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame && ToggleWeaponState())
            {
                ResetIdleVariantTimer();
                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame && PlayLockedState(GetJumpState(), jumpLockTime))
            {
                ResetIdleVariantTimer();
                return;
            }

            if (Keyboard.current.tKey.wasPressedThisFrame &&
                PlayNextLockedState(tauntStates, ref nextTauntIndex, tauntLockTime))
            {
                ResetIdleVariantTimer();
                return;
            }
        }

        if (Mouse.current != null)
        {
            // PPM — heavy attack (not during lock)
            if (Mouse.current.rightButton.wasPressedThisFrame && Time.time >= animationLockedUntil)
            {
                if (TryHeavyAttack())
                {
                    ResetIdleVariantTimer();
                    return;
                }
            }

            // LPM — light combo
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Time.time < animationLockedUntil)
                {
                    // Accept input during lock as long as we are inside the combo window
                    if (Time.time <= comboWindowEnd)
                        comboPending = true;
                }
                else
                {
                    if (TryLightAttack())
                    {
                        ResetIdleVariantTimer();
                        return;
                    }
                }
            }
        }

        // Fire the queued combo step as soon as the lock lifts
        if (comboPending && Time.time >= animationLockedUntil)
        {
            comboPending = false;
            if (Time.time <= comboWindowEnd && TryLightAttack())
            {
                ResetIdleVariantTimer();
                return;
            }
            // Missed the window — reset combo
            comboStep = 0;
        }

        if (Time.time < animationLockedUntil)
            return;

        if (crouchHeld && PlayState(crouchState))
        {
            wasCrouching = true;
            ResetIdleVariantTimer();
            return;
        }

        if (wasCrouching)
        {
            wasCrouching = false;
            if (PlayLockedState(crouchToStandState, jumpLockTime))
                return;
        }

        if (isMoving)
        {
            bool playedLocomotion = PlayLocomotionState(
                isWalking ? GetWalkState() : GetRunState(),
                distanceMoved,
                isWalking ? walkCycleDistance : runCycleDistance
            );

            if (!playedLocomotion)
                PlayState(GetIdleState());

            ResetIdleVariantTimer();
            return;
        }

        if (Time.time >= nextIdleVariantTime && PlayNextIdleVariant())
        {
            nextIdleVariantTime = Time.time + idleVariantDelay + idleVariantLockTime;
            return;
        }

        PlayState(GetIdleState());
    }

    private bool ToggleWeaponState()
    {
        bool wasArmed = isArmed;
        isArmed = !isArmed;

        if (wasArmed)
            return PlayNextLockedState(disarmStates, ref nextDisarmIndex, equipLockTime);

        return PlayNextLockedState(equipStates, ref nextEquipIndex, equipLockTime);
    }

    private AnimationState GetIdleState()
    {
        if (!isArmed && unarmedIdleState.Exists) return unarmedIdleState;
        return standingIdleState;
    }

    private AnimationState GetRunState()
    {
        if (!isArmed && unarmedRunState.Exists) return unarmedRunState;
        return standingRunState;
    }

    private AnimationState GetWalkState()
    {
        if (!isArmed && unarmedWalkState.Exists) return unarmedWalkState;
        return standingWalkState;
    }

    private AnimationState GetJumpState()
    {
        if (!isArmed && unarmedJumpState.Exists) return unarmedJumpState;
        return standingJumpState;
    }

    private bool PlayNextIdleVariant()
    {
        if (!isArmed && PlayNextLockedState(unarmedIdleVariantStates, ref nextUnarmedIdleVariantIndex, idleVariantLockTime))
            return true;

        return PlayNextLockedState(standingIdleVariantStates, ref nextStandingIdleVariantIndex, idleVariantLockTime);
    }

    private void ResetIdleVariantTimer()
    {
        nextIdleVariantTime = Time.time + idleVariantDelay;
    }

    private bool PlayNextLockedState(AnimationState[] states, ref int nextIndex, float lockTime)
    {
        for (int i = 0; i < states.Length; i++)
        {
            int index = (nextIndex + i) % states.Length;

            if (!states[index].Exists)
                continue;

            nextIndex = (index + 1) % states.Length;
            return PlayLockedState(states[index], lockTime);
        }

        return false;
    }

    private bool PlayLockedState(AnimationState state, float lockTime)
    {
        if (!PlayState(state, true))
            return false;

        animationLockedUntil = Time.time + lockTime;
        return true;
    }

    private bool PlayState(AnimationState state, bool forceRestart = false)
    {
        if (!state.Exists)
        {
            animator.speed = 1f;
            currentDistanceMatchedStateHash = 0;
            return false;
        }

        animator.speed = 1f;
        currentDistanceMatchedStateHash = 0;

        if (forceRestart || animator.GetCurrentAnimatorStateInfo(0).fullPathHash != state.Hash)
            animator.CrossFade(state.Hash, animationFadeTime, 0);

        return true;
    }

    private bool PlayLocomotionState(AnimationState state, float distanceMoved, float cycleDistance)
    {
        if (!state.Exists)
            return false;

        if (!distanceMatchLocomotion)
        {
            animator.speed = 1f;
            return PlayState(state);
        }

        if (currentDistanceMatchedStateHash != state.Hash)
            currentDistanceMatchedStateHash = state.Hash;

        locomotionDistance += distanceMoved;

        float normalizedTime = cycleDistance > 0.001f
            ? Mathf.Repeat(locomotionDistance / cycleDistance, 1f)
            : 0f;

        animator.speed = 0f;
        animator.Play(state.Hash, 0, normalizedTime);
        return true;
    }

    private void TryAssignAnimatorControllerInEditor()
    {
#if UNITY_EDITOR
        if (animator.runtimeAnimatorController != null)
            return;

        RuntimeAnimatorController ctrl =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/PlayerAuto.controller");

        if (ctrl == null)
            return;

        animator.runtimeAnimatorController = ctrl;
        EditorUtility.SetDirty(animator);
#endif
    }

    private static string SanitizeStateName(string stateName)
    {
        return stateName.Replace('.', '_');
    }
}
