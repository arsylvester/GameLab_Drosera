using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class Scurrier : EnemyBase {

#pragma warning disable 0649 // Disable "Field is never assigned" warning for SerializeField

    [Header("Scurrier Specific")]
    [SerializeField] private float aggressiveRange; // Range at which Scurrier turns aggressive
    [SerializeField] private float swatRange; // Range at which Scurrier will swat instead of gore (charge)
    [SerializeField] private Vector2 goreRange; // Min-max range for gore attack
    [SerializeField] private float cooldownGore; // Cooldown timer for attempting another gore (charge) attack
    [SerializeField] private float goreSpeedMultiplier; // Speed multiplier for gore
    [SerializeField] private float goreSkidDistance; // Distance scurrier will travel during gore skid
    private float cooldownTimerGore; // Timer for gore (charge) attack cooldowns

    [Header("Hitboxes")]
    [SerializeField] private Hitbox swatHitbox;
    [SerializeField] private Hitbox goreHitbox;

    private ScurrierCrashDetector crashDetector;

    [System.Serializable]
    public class FX {
        [Header("VFX")]
        public GameObject shockwave; // TODO
        public GameObject goreTrail, goreImpact;

        [Header("SFX")]
        public UnityEvent OnSwatHit;
        public UnityEvent OnGoreTarget, OnGoreHit;
    }
    [Header("Scurrier VFX & SFX")] [SerializeField] private FX _scurrierFX;

#pragma warning disable 0649

    // -------------------------------------------------------------------------------------------

    protected override void Awake() {
        base.Awake();
        crashDetector = GetComponentInChildren<ScurrierCrashDetector>(true);
        crashDetector.gameObject.SetActive(false);
    }

    protected override void Start() {
        base.Start();
        goreHitbox.OnHit.AddListener(() => {
            _scurrierFX.OnGoreHit.Invoke();
            SpawnGoreHitVFX();
        });
    }

    // -------------------------------------------------------------------------------------------
    // Behavior Coroutines - Main

    protected override IEnumerator Idle(bool regen = false) {
        _agent.stoppingDistance = 0f;
        _agent.SetDestination(transform.position);

        if(regen) {
            isHealing = true;
            StartCoroutine(Regenerate());
        }
        currentState = EnemyState.Passive;

        GoreReset();

        Vector3 forward;
        while(true) {
            // Get target position
            targetPosition = spawnPosition + (new Vector3(Random.Range(-idleWanderRange, idleWanderRange), 0, Random.Range(-idleWanderRange, idleWanderRange)));
            forward = targetPosition - transform.position;
            forward.y = 0;

            // Turn to look towards position over 1 sec
            for(float i = 0; i < 1; i += Time.deltaTime) {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(forward, Vector3.up), Time.deltaTime * 180f);
                yield return null;
                CheckAggression();
            }

            // Wait 0.5 sec
            for(float i = 0; i < 0.5; i += Time.deltaTime) {
                yield return null;
                CheckAggression();
            }

            // Move towards position
            _agent.SetDestination(targetPosition);
            while(_agent.remainingDistance > 1f)
                yield return null;

            // Wait at position for 2 sec
            for(float i = 0; i < 2; i += Time.deltaTime) {
                yield return null;
                CheckAggression();
            }
        }
    }

    protected override void CheckAggression() {
        // TODO - if doing multiplayer, get spherecast working for checking players
        /*if(Physics.SphereCast(transform.position - new Vector3(0, -2, 0), aggressiveRange, Vector3.up, out RaycastHit hit, aggressiveRange, LayerMask.GetMask("Player"))) {
            targetPlayer = hit.collider.GetComponentInParent<EntityBase>().gameObject;
            TurnAggressive.Invoke();
        }*/
        if(Vector3.Distance((PlayerBase.instance != null ? PlayerBase.instance.transform.position : Vector3.zero), transform.position) < aggressiveRange) {
            TurnAggressive.Invoke();
        }
    }

    // ------

    protected override IEnumerator AggressiveMove() {
        _agent.stoppingDistance = stoppingDistance;
        attackDone = false;

        GoreReset();

        while(true) {
            yield return null;
            FindTarget();

            // No target player available - idle instead
            if(targetPlayer == null) {
                ForceIdle();
                yield break;
            }

            // Move towards player
            _agent.SetDestination(targetPlayer.transform.position);

            // Turn if standing still
            if(_agent.velocity.magnitude < 0.5f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(VectorToPlayer(), Vector3.up), Time.deltaTime * _agent.angularSpeed);

            // Swat (Melee) Attack
            if(cooldownTimer == 0) { // Check cooldown
                if(Vector3.Distance(transform.position, targetPlayer.transform.position) <= swatRange && // Check melee range
                    Vector3.Angle(transform.forward, VectorToPlayer()) < 15f) { // Check player in front of scurrier (total 30° cone)
                    currentBehavior = StartCoroutine(AttackSwat());
                    yield break;
                }
            } else { // On cooldown
                cooldownTimer -= Time.deltaTime;
                if(cooldownTimer <= 0)
                    cooldownTimer = 0;
            }

            // Gore (Charge) Attack
            if(cooldownTimerGore == 0) { // check cooldown
                if(!Physics.Raycast(transform.position, VectorToPlayer(), // Raycast (contains max distance)
                    Mathf.Clamp(VectorToPlayer().magnitude, 0, goreRange.y), LayerMask.GetMask("Terrain")) &&
                    Vector3.Distance(transform.position, _agent.destination) > goreRange.x) { // Check min distance

                    currentBehavior = StartCoroutine(AttackGore());
                    yield break;
                }
            } else { // On cooldown
                cooldownTimerGore -= Time.deltaTime;
                if(cooldownTimerGore <= 0)
                    cooldownTimerGore = 0;
            }
        }
    }
    
    // -------------------------------------------------------------------------------------------
    // Attacks

    /// <summary>
    /// Irrelevant for Scurrier - use AttackGore or AttackSwat instead
    /// </summary>
    protected override IEnumerator Attack() {
        Debug.LogWarning(gameObject.name + " called irrelevant Coroutine \"Attack\" - use \"AttackGore\" or \"AttackSwat\" instead.");
        currentBehavior = StartCoroutine(AttackSwat());
        yield return null;
    }

    // -----

    /// <summary>
    /// Gore (charge) attack
    /// </summary>
    private IEnumerator AttackGore() {
        currentState = EnemyState.Attacking;
        _agent.stoppingDistance = 0;
        _agent.autoBraking = false;
        _agent.isStopped = true;

        // TODO - windup animation

        // Turn to look towards position over 0.5 sec
        Vector3 forward = Vector3.zero;
        Vector3 initialTargetPos = targetPlayer.transform.position;
        forward.y = 0;

        for(float i = 0; i < 0.5; i += Time.deltaTime) {
            // Check if player left line of sight or left max range - exit
            if(Physics.Raycast(transform.position, VectorToPlayer(), goreRange.y, LayerMask.GetMask("Terrain")) || // Raycast
                Vector3.Distance(transform.position, initialTargetPos) >= goreRange.y) { // Check distance

                currentBehavior = StartCoroutine(AggressiveMove());
                yield break;
            }

            initialTargetPos = targetPlayer.transform.position;
            forward = (initialTargetPos - transform.position).normalized;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(forward, Vector3.up), Time.deltaTime * 360f);
            yield return null;
        }

        // Set vars
        _agent.speed *= goreSpeedMultiplier;
        _agent.SetDestination(initialTargetPos);
        while(_agent.pathPending)
            yield return null;
        //Debug.DrawRay(transform.position, _agent.destination + (forward * goreSkidDistance) - transform.position, Color.red, 2);
        //Debug.DrawRay(transform.position, _agent.destination - transform.position, Color.green, 2);

        // TODO - begin charge animation

        // Charge
        _scurrierFX.OnGoreTarget.Invoke();
        crashDetector.gameObject.SetActive(true);
        _scurrierFX.goreTrail.SetActive(true);
        _animator.SetTrigger("Gore");
        _agent.isStopped = false;
        float timeout = 0; // Failsafe to prevent infinite gore
        while(_agent.remainingDistance > 1) {
            // Check wall collision
            if(crashDetector.crash) {
                currentBehavior = StartCoroutine(GoreCrash());
                yield break;
            }

            yield return null;

            // Check timeout
            timeout += Time.deltaTime;
            if(timeout > 5f) {
                Debug.LogError(gameObject.name + "'s gore attack timed out.");
                break;
            }
        }

        // Did not crash - begin skidding
        currentBehavior = StartCoroutine(GoreSkid());
    }

    /// <summary>
    /// Function for if gore finishes successfully, and Scurrier starts skidding
    /// </summary>
    private IEnumerator GoreSkid() {
        goreHitbox.damage /= 2;

        // TODO - start skid animation

        // Δx = (v + v_o)t/2 => t = 2(Δx)/(v + v_o) => v = 0, so t = 2(Δx)/v_0
        Vector3 baseVelocity = _agent.velocity;
        float skidTime = 2 * goreSkidDistance / baseVelocity.magnitude;

        _agent.updatePosition = false;
        _agent.ResetPath();

        // Skid
        for(float i = 0; i < skidTime; i += Time.deltaTime) {
            // Check wall collision
            if(crashDetector.crash) {
                currentBehavior = StartCoroutine(GoreCrash());
                yield break;
            }

            // Slow velocity
            _agent.velocity = Vector3.Lerp(baseVelocity, Vector3.zero, i / skidTime);
            transform.position = _agent.nextPosition;

            yield return null;
        }
        _agent.velocity = Vector3.zero;
        _animator.SetTrigger("Gore Done");
        yield return new WaitForSeconds(0.25f);

        // Set cooldown & return to movement
        cooldownTimer = _cooldown;
        cooldownTimerGore = cooldownGore;
        currentBehavior = StartCoroutine(AggressiveMove());
    }

    /// <summary>
    /// Function for if Scurrier crashes into a wall during gore
    /// </summary>
    private IEnumerator GoreCrash() {
        Debug.Log("Gore crashed into wall");
        _animator.SetTrigger("Gore Done");
        _agent.isStopped = true;

        SpawnGoreHitVFX();

        yield return new WaitForSeconds(1f);

        // Set cooldown & return to movement
        cooldownTimer = _cooldown;
        cooldownTimerGore = cooldownGore;
        currentBehavior = StartCoroutine(AggressiveMove());
    }

    /// <summary>
    /// Reset scurrier stats after gore in case of interruption
    /// </summary>
    private void GoreReset() {
        crashDetector.gameObject.SetActive(false);
        _scurrierFX.goreTrail.SetActive(false);

        _agent.isStopped = false;
        _agent.autoBraking = true;
        _agent.speed = _moveSpeed;
        _agent.updatePosition = true;
    }

    // -----

    /// <summary>
    /// Swat attack
    /// </summary>
    private IEnumerator AttackSwat() {
        currentState = EnemyState.Attacking;
        _animator.SetTrigger("Swat");

        while(!attackDone) {
            yield return null;
        }

        cooldownTimer = _cooldown;
        currentBehavior = StartCoroutine(AggressiveMove());
    }

    // -------------------------------------------------------------------------------------------

    public override void ResetEnemy() {
        GoreReset();
        base.ResetEnemy();
    }

    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Plays swat SFX - called in the animator
    /// </summary>
    public void PlaySwatSound() {
        _scurrierFX.OnSwatHit.Invoke();
    }

    /// <summary>
    /// Spawns the gore hit VFX in the correct position
    /// </summary>
    private void SpawnGoreHitVFX() {
        VFXSpawner.vfx.SpawnVFX(_scurrierFX.goreImpact, 1, transform.position + Vector3.up + (transform.forward * 0.5f), transform.rotation);
    }

}
