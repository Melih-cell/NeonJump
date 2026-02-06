using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Robot Animator Controller'ını programatik olarak yapılandırır.
/// Kullanım: Tools > Robot Animator Setup
/// </summary>
public class RobotAnimatorSetup : Editor
{
    [MenuItem("Tools/Robot Animator Setup")]
    public static void SetupRobotAnimator()
    {
        string controllerPath = "Assets/Robot dusman/robot ates/Robot.controller";

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("Robot.controller bulunamadı: " + controllerPath);
            return;
        }

        // Mevcut parametreleri temizle
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(i);
        }

        // Parametreleri ekle
        controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("LazerAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("MeleeAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DashAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsRage", AnimatorControllerParameterType.Bool);

        // Animasyon kliplerini yükle
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Robot dusman/robot ates/robot_yurume.anim");
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Robot dusman/robot ates/robot_ates.anim");
        AnimationClip lazerClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Robot dusman/robot ates/robot_lazerSaldırısı.anim");
        AnimationClip meleeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Robot dusman/robot ates/robot_s_saldırısı.anim");
        AnimationClip dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Robot dusman/robot ates/robot_olme.anim");

        // Base layer'ı al
        AnimatorControllerLayer baseLayer = controller.layers[0];
        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Mevcut state'leri temizle
        foreach (ChildAnimatorState state in stateMachine.states)
        {
            stateMachine.RemoveState(state.state);
        }

        // State'leri oluştur
        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300, 0, 0));
        AnimatorState walkState = stateMachine.AddState("Walk", new Vector3(300, 100, 0));
        AnimatorState attackState = stateMachine.AddState("Attack", new Vector3(600, -120, 0));
        AnimatorState lazerState = stateMachine.AddState("LazerAttack", new Vector3(600, -40, 0));
        AnimatorState meleeState = stateMachine.AddState("MeleeAttack", new Vector3(600, 40, 0));
        AnimatorState dashState = stateMachine.AddState("DashAttack", new Vector3(600, 120, 0));
        AnimatorState dieState = stateMachine.AddState("Die", new Vector3(450, 260, 0));

        // Rage sub-states (hizli versiyonlar)
        AnimatorState rageWalkState = stateMachine.AddState("RageWalk", new Vector3(100, 100, 0));
        AnimatorState rageIdleState = stateMachine.AddState("RageIdle", new Vector3(100, 0, 0));

        // Klipleri ata
        if (walkClip != null)
        {
            idleState.motion = walkClip; // Idle de yürüme sprite kullanır (ilk frame)
            walkState.motion = walkClip;
            rageWalkState.motion = walkClip;
            rageIdleState.motion = walkClip;
        }
        if (attackClip != null) attackState.motion = attackClip;
        if (lazerClip != null) lazerState.motion = lazerClip;
        if (meleeClip != null) meleeState.motion = meleeClip;
        if (meleeClip != null) dashState.motion = meleeClip; // Dash icin melee klip (hizli oynatilacak)
        if (dieClip != null) dieState.motion = dieClip;

        // Default state
        stateMachine.defaultState = idleState;

        // Speed ayarlari
        idleState.speed = 0f;
        walkState.speed = 1f;
        rageIdleState.speed = 0f;
        rageWalkState.speed = 1.5f; // Rage modda %50 daha hizli yurume animasyonu
        dashState.speed = 2f; // Dash animasyonu 2x hizli
        attackState.speed = 1f;
        lazerState.speed = 1f;
        meleeState.speed = 1f;

        // ===========================
        // --- TRANSITIONS ---
        // ===========================

        // === NORMAL MODE TRANSITIONS ===

        // Idle -> Walk (isWalking = true, IsRage = false)
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        idleToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRage");
        idleToWalk.duration = 0f;
        idleToWalk.hasExitTime = false;

        // Walk -> Idle (isWalking = false, IsRage = false)
        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRage");
        walkToIdle.duration = 0f;
        walkToIdle.hasExitTime = false;

        // === RAGE MODE TRANSITIONS ===

        // Idle -> RageIdle (IsRage = true, isWalking = false)
        AnimatorStateTransition idleToRageIdle = idleState.AddTransition(rageIdleState);
        idleToRageIdle.AddCondition(AnimatorConditionMode.If, 0, "IsRage");
        idleToRageIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        idleToRageIdle.duration = 0.05f;
        idleToRageIdle.hasExitTime = false;

        // Idle -> RageWalk (IsRage = true, isWalking = true)
        AnimatorStateTransition idleToRageWalk = idleState.AddTransition(rageWalkState);
        idleToRageWalk.AddCondition(AnimatorConditionMode.If, 0, "IsRage");
        idleToRageWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        idleToRageWalk.duration = 0.05f;
        idleToRageWalk.hasExitTime = false;

        // Walk -> RageWalk (IsRage = true)
        AnimatorStateTransition walkToRageWalk = walkState.AddTransition(rageWalkState);
        walkToRageWalk.AddCondition(AnimatorConditionMode.If, 0, "IsRage");
        walkToRageWalk.duration = 0.05f;
        walkToRageWalk.hasExitTime = false;

        // RageIdle -> RageWalk (isWalking = true)
        AnimatorStateTransition rageIdleToRageWalk = rageIdleState.AddTransition(rageWalkState);
        rageIdleToRageWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
        rageIdleToRageWalk.duration = 0f;
        rageIdleToRageWalk.hasExitTime = false;

        // RageWalk -> RageIdle (isWalking = false)
        AnimatorStateTransition rageWalkToRageIdle = rageWalkState.AddTransition(rageIdleState);
        rageWalkToRageIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
        rageWalkToRageIdle.duration = 0f;
        rageWalkToRageIdle.hasExitTime = false;

        // RageIdle -> Idle (IsRage = false) - robot iyilesti (heal vb.)
        AnimatorStateTransition rageIdleToIdle = rageIdleState.AddTransition(idleState);
        rageIdleToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRage");
        rageIdleToIdle.duration = 0.1f;
        rageIdleToIdle.hasExitTime = false;

        // RageWalk -> Walk (IsRage = false)
        AnimatorStateTransition rageWalkToWalk = rageWalkState.AddTransition(walkState);
        rageWalkToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRage");
        rageWalkToWalk.duration = 0.1f;
        rageWalkToWalk.hasExitTime = false;

        // === ANY STATE -> ATTACK TRANSITIONS ===

        // Any State -> Attack (mermi)
        AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.duration = 0f;
        anyToAttack.hasExitTime = false;
        anyToAttack.canTransitionToSelf = false;

        // Any State -> LazerAttack
        AnimatorStateTransition anyToLazer = stateMachine.AddAnyStateTransition(lazerState);
        anyToLazer.AddCondition(AnimatorConditionMode.If, 0, "LazerAttack");
        anyToLazer.duration = 0f;
        anyToLazer.hasExitTime = false;
        anyToLazer.canTransitionToSelf = false;

        // Any State -> MeleeAttack
        AnimatorStateTransition anyToMelee = stateMachine.AddAnyStateTransition(meleeState);
        anyToMelee.AddCondition(AnimatorConditionMode.If, 0, "MeleeAttack");
        anyToMelee.duration = 0f;
        anyToMelee.hasExitTime = false;
        anyToMelee.canTransitionToSelf = false;

        // Any State -> DashAttack
        AnimatorStateTransition anyToDash = stateMachine.AddAnyStateTransition(dashState);
        anyToDash.AddCondition(AnimatorConditionMode.If, 0, "DashAttack");
        anyToDash.duration = 0f;
        anyToDash.hasExitTime = false;
        anyToDash.canTransitionToSelf = false;

        // Any State -> Die (en yuksek oncelik)
        AnimatorStateTransition anyToDie = stateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.duration = 0f;
        anyToDie.hasExitTime = false;
        anyToDie.canTransitionToSelf = false;

        // === ATTACK -> IDLE DONUSLERI (exit time ile) ===

        // Attack -> Idle
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0f;
        attackToIdle.hasFixedDuration = true;

        // LazerAttack -> Idle
        AnimatorStateTransition lazerToIdle = lazerState.AddTransition(idleState);
        lazerToIdle.hasExitTime = true;
        lazerToIdle.exitTime = 1f;
        lazerToIdle.duration = 0f;
        lazerToIdle.hasFixedDuration = true;

        // MeleeAttack -> Idle
        AnimatorStateTransition meleeToIdle = meleeState.AddTransition(idleState);
        meleeToIdle.hasExitTime = true;
        meleeToIdle.exitTime = 1f;
        meleeToIdle.duration = 0f;
        meleeToIdle.hasFixedDuration = true;

        // DashAttack -> Idle
        AnimatorStateTransition dashToIdle = dashState.AddTransition(idleState);
        dashToIdle.hasExitTime = true;
        dashToIdle.exitTime = 1f;
        dashToIdle.duration = 0f;
        dashToIdle.hasFixedDuration = true;

        // Die state'inde kalır (loop yok)
        if (dieClip != null)
        {
            // Die animasyonu loop olmamalı
            AnimationClipSettings dieSettings = AnimationUtility.GetAnimationClipSettings(dieClip);
            dieSettings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(dieClip, dieSettings);
        }

        // Walk animasyonu loop olmalı
        if (walkClip != null)
        {
            AnimationClipSettings walkSettings = AnimationUtility.GetAnimationClipSettings(walkClip);
            walkSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(walkClip, walkSettings);
        }

        // Saldırı animasyonları loop olmamalı
        AnimationClip[] noLoopClips = { attackClip, lazerClip, meleeClip }; // dashState meleeClip kullaniyor, zaten listede
        foreach (AnimationClip clip in noLoopClips)
        {
            if (clip != null)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("Robot Animator Controller başarıyla yapılandırıldı! 5 state, 5 parametre eklendi.");
    }

    [MenuItem("Tools/Robot Setup Components")]
    public static void SetupRobotComponents()
    {
        GameObject robot = GameObject.Find("Robot_Dusman");
        if (robot == null)
        {
            Debug.LogError("Robot_Dusman sahnede bulunamadı!");
            return;
        }

        // Tag ayarla
        robot.tag = "Enemy";

        // Rigidbody2D
        Rigidbody2D rb = robot.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = robot.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // CapsuleCollider2D
        CapsuleCollider2D col = robot.GetComponent<CapsuleCollider2D>();
        if (col == null)
        {
            col = robot.AddComponent<CapsuleCollider2D>();
        }
        col.size = new Vector2(0.35f, 0.55f);
        col.offset = new Vector2(0f, -0.05f);
        col.direction = CapsuleDirection2D.Vertical;

        // EnemyHealth
        EnemyHealth health = robot.GetComponent<EnemyHealth>();
        if (health == null)
        {
            health = robot.AddComponent<EnemyHealth>();
        }
        health.maxHealth = 5;
        health.flashOnDamage = true;

        // RobotEnemy
        RobotEnemy robotEnemy = robot.GetComponent<RobotEnemy>();
        if (robotEnemy == null)
        {
            robotEnemy = robot.AddComponent<RobotEnemy>();
        }

        // RobotEnemy ayarları
        robotEnemy.patrolSpeed = 2f;
        robotEnemy.patrolLeftDistance = 5f;
        robotEnemy.patrolRightDistance = 5f;
        robotEnemy.detectionRange = 15f;
        robotEnemy.longRangeThreshold = 8f;
        robotEnemy.midRangeThreshold = 4f;
        robotEnemy.projectileSpeed = 10f;
        robotEnemy.projectileDamage = 1;
        robotEnemy.projectileCooldown = 2f;
        robotEnemy.laserDamage = 2;
        robotEnemy.laserCooldown = 3f;
        robotEnemy.laserDuration = 0.5f;
        robotEnemy.meleeDamage = 1;
        robotEnemy.meleeCooldown = 1.5f;
        robotEnemy.contactDamage = 1;
        robotEnemy.contactKnockback = 5f;
        robotEnemy.canBeKilledByJump = true;
        robotEnemy.bounceForce = 10f;
        robotEnemy.useSquashDeath = false;
        robotEnemy.deathFadeDuration = 0.5f;

        EditorUtility.SetDirty(robot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(robot.scene);

        Debug.Log("Robot_Dusman bileşenleri başarıyla yapılandırıldı! Tag: Enemy, HP: 5, 3 saldırı modu aktif.");
    }
}
