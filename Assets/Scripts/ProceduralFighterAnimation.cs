using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Substitui em runtime os controllers e clips legados por poses procedurais baseadas
/// no Avatar humanoide. Os FBX originais permanecem no projeto como backup, mas nao
/// participam mais do movimento, ataque, salto ou agachamento dos lutadores.
/// </summary>
[DisallowMultipleComponent]
public sealed class ProceduralFighterAnimation : MonoBehaviour
{
    private readonly Dictionary<Transform, Quaternion> baseRotations = new Dictionary<Transform, Quaternion>();
    private Animator animator;
    private FighterController fighter;
    private FighterMovement movement;
    private Transform hips, spine, chest, head;
    private Transform leftUpperArm, rightUpperArm, leftLowerArm, rightLowerArm;
    private Transform leftUpperLeg, rightUpperLeg, leftLowerLeg, rightLowerLeg;
    private IFighterState lastState;
    private float stateTime;
    private bool wasAirborne;
    private float airborneTime;

    private void Awake()
    {
        fighter = GetComponent<FighterController>();
        movement = GetComponent<FighterMovement>();
        animator = GetComponent<Animator>();
        if (animator == null || !animator.isHuman) { enabled = false; return; }

        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        head = animator.GetBoneTransform(HumanBodyBones.Head);
        leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        Cache(hips, spine, chest, head, leftUpperArm, rightUpperArm, leftLowerArm, rightLowerArm, leftUpperLeg, rightUpperLeg, leftLowerLeg, rightLowerLeg);

        // A única base reaproveitada é Bouncing Fight Idle: uma postura de luta
        // coerente, com pés plantados. Demais ações são sobrepostas proceduralmente.
        RuntimeAnimatorController foundation = Resources.Load<RuntimeAnimatorController>("Animations/FighterFoundation");
        if (foundation != null) animator.runtimeAnimatorController = foundation;
        animator.applyRootMotion = false;
        animator.enabled = true;
        animator.Play("Idle", 0, Random.value);
    }

    private void LateUpdate()
    {
        if (fighter == null || movement == null) return;
        if (animator == null || !animator.enabled)
            foreach (KeyValuePair<Transform, Quaternion> item in baseRotations)
                if (item.Key != null) item.Key.localRotation = item.Value;

        if (fighter.CurrentState != lastState) { lastState = fighter.CurrentState; stateTime = 0f; }
        else stateTime += Time.deltaTime;

        bool airborne = !movement.IsGrounded;
        airborneTime = airborne ? (wasAirborne ? airborneTime + Time.deltaTime : 0f) : 0f;
        wasAirborne = airborne;

        if (fighter.HealthSystem != null && fighter.HealthSystem.IsDead) { KnockoutPose(); return; }
        if (fighter.CurrentState is AttackState) { AttackPose(); return; }
        if (fighter.CurrentState is HitStunState) { HitPose(); return; }
        if (airborne) { JumpPose(); return; }
        if (movement.IsCrouching) { CrouchPose(); return; }
        if (movement.CurrentInput.sqrMagnitude > .01f) LocomotionPose();
    }

    private void LocomotionPose()
    {
        float speed = Mathf.Clamp01(movement.CurrentInput.magnitude);
        float cycle = Time.time * Mathf.Lerp(1.7f, 7.1f, speed);
        float stride = Mathf.Sin(cycle) * 26f * speed;
        float counter = Mathf.Sin(cycle + Mathf.PI) * 20f * speed;
        Rotate(leftUpperLeg, stride, 0f, 0f); Rotate(rightUpperLeg, counter, 0f, 0f);
        Rotate(leftLowerLeg, Mathf.Max(0f, -stride) * .62f, 0f, 0f); Rotate(rightLowerLeg, Mathf.Max(0f, -counter) * .62f, 0f, 0f);
        Rotate(leftUpperArm, counter * .42f, 0f, -6f); Rotate(rightUpperArm, stride * .42f, 0f, 6f);
        float breathe = Mathf.Sin(Time.time * 2.1f) * 2.3f;
        Rotate(chest, breathe, 0f, 0f); Rotate(head, -breathe * .42f, 0f, 0f);
        Rotate(leftLowerArm, 0f, 0f, -25f); Rotate(rightLowerArm, 0f, 0f, 25f);
    }

    private void CrouchPose()
    {
        float moving = Mathf.Abs(movement.CurrentInput.x);
        float cycle = Mathf.Sin(Time.time * 7f) * 10f * moving;
        Rotate(hips, 23f, 0f, 0f); Rotate(spine, -16f, 0f, 0f); Rotate(chest, -9f, 0f, 0f);
        Rotate(leftUpperLeg, -42f + cycle, 0f, 0f); Rotate(rightUpperLeg, -42f - cycle, 0f, 0f);
        Rotate(leftLowerLeg, 65f, 0f, 0f); Rotate(rightLowerLeg, 65f, 0f, 0f);
        Rotate(leftUpperArm, 18f, 0f, -28f); Rotate(rightUpperArm, 18f, 0f, 28f);
    }

    private void JumpPose()
    {
        float phase = Mathf.Clamp01(airborneTime / .55f);
        float tuck = Mathf.Sin(phase * Mathf.PI) * 47f;
        Rotate(hips, 8f, 0f, 0f); Rotate(leftUpperLeg, -tuck, 0f, 0f); Rotate(rightUpperLeg, -tuck, 0f, 0f);
        Rotate(leftLowerLeg, tuck * .92f, 0f, 0f); Rotate(rightLowerLeg, tuck * .92f, 0f, 0f);
        Rotate(leftUpperArm, 20f, 0f, -18f); Rotate(rightUpperArm, 20f, 0f, 18f);
    }

    private void AttackPose()
    {
        float duration = fighter.CurrentAttackDuration;
        float t = Mathf.Clamp01(stateTime / Mathf.Max(.05f, duration));
        if (fighter.ActiveAttackType == FighterAttackType.Attack2) KickPose(t);
        else PunchPose(t);
    }

    private void PunchPose(float t)
    {
        float extend = ImpactCurve(t, .18f, .43f, .76f);
        Rotate(hips, 0f, -18f * extend, 0f); Rotate(chest, 0f, -31f * extend, 0f);
        Rotate(rightUpperArm, -42f * extend, 0f, 30f * extend);
        Rotate(rightLowerArm, 0f, 0f, -48f * extend);
        Rotate(leftUpperArm, 12f * extend, 0f, -28f * extend);
    }

    private void KickPose(float t)
    {
        float chamber = Mathf.Clamp01(t / .25f);
        float extend = ImpactCurve(t, .24f, .51f, .82f);
        Rotate(hips, 5f, 25f * extend, 0f); Rotate(chest, -7f, 32f * extend, 0f);
        Rotate(leftUpperLeg, -42f * chamber, 0f, 0f); Rotate(leftLowerLeg, 58f * chamber, 0f, 0f);
        Rotate(rightUpperLeg, -74f * extend + 18f * chamber * (1f - extend), 0f, 0f);
        Rotate(rightLowerLeg, 78f * chamber * (1f - extend) + 8f * extend, 0f, 0f);
        Rotate(leftUpperArm, 28f, 0f, -36f); Rotate(rightUpperArm, 22f, 0f, 32f);
    }

    private void HitPose()
    {
        float recoil = Mathf.Clamp01(1f - stateTime / .42f);
        Rotate(hips, -8f * recoil, 0f, 0f); Rotate(spine, -18f * recoil, 0f, 0f);
        Rotate(head, 14f * recoil, 0f, 0f); Rotate(leftUpperArm, 26f * recoil, 0f, -20f); Rotate(rightUpperArm, 26f * recoil, 0f, 20f);
    }

    private void KnockoutPose()
    {
        Rotate(hips, 0f, 0f, 72f); Rotate(spine, 0f, 0f, -35f); Rotate(leftUpperLeg, 30f, 0f, 0f); Rotate(rightUpperLeg, -20f, 0f, 0f);
        Rotate(leftUpperArm, 0f, 0f, -72f); Rotate(rightUpperArm, 0f, 0f, 72f);
    }

    private static float ImpactCurve(float t, float windupEnd, float impact, float recoveryEnd)
    {
        if (t < windupEnd) return Mathf.SmoothStep(0f, .28f, t / windupEnd);
        if (t < impact) return Mathf.SmoothStep(.28f, 1f, (t - windupEnd) / (impact - windupEnd));
        if (t < recoveryEnd) return Mathf.SmoothStep(1f, 0f, (t - impact) / (recoveryEnd - impact));
        return 0f;
    }

    private void Cache(params Transform[] bones)
    {
        foreach (Transform bone in bones)
            if (bone != null && !baseRotations.ContainsKey(bone)) baseRotations.Add(bone, bone.localRotation);
    }

    private void Rotate(Transform bone, float x, float y, float z)
    {
        if (bone != null) bone.localRotation *= Quaternion.Euler(x, y, z);
    }
}
