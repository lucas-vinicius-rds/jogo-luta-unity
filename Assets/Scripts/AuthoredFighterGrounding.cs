using UnityEngine;

/// <summary>Physical flight clock and foot constraints; COM height is baked in clips.</summary>
[RequireComponent(typeof(Animator), typeof(FighterMovement))]
public sealed class AuthoredFighterGrounding : MonoBehaviour
{
    Animator animator;
    FighterMovement movement;
    FighterController fighter;
    Vector3 leftAnchor, rightAnchor, previousRoot;
    Quaternion leftRotation, rightRotation;
    bool planted, hasStandingFeet;
    float crouchWeight;
    static readonly int FlightRate = Animator.StringToHash("FlightRate");
    void Awake()
    {
        animator=GetComponent<Animator>(); movement=GetComponent<FighterMovement>();
        fighter=GetComponent<FighterController>(); previousRoot=transform.position;
    }
    bool UsesAuthoredController => animator.isHuman && animator.runtimeAnimatorController &&
        animator.runtimeAnimatorController.name=="AuthoredFighter";
    void Update()
    {
        if(!UsesAuthoredController) return;
        // Keep height/gravity dynamic; do not assume a particular jump tuning.
        animator.SetFloat(FlightRate,1f/Mathf.Max(.01f,movement.ExpectedAirTime));
    }
    void OnDisable() { planted=false; hasStandingFeet=false; crouchWeight=0f; }
    void OnAnimatorIK(int layer)
    {
        if(layer!=0 || !UsesAuthoredController) return;
        ClearWeights();
        bool neutral=fighter && fighter.CurrentState is NeutralState;
        bool grounded=movement.IsGrounded;
        // Reset/teleport and airborne/hit/KO must never reuse world-space anchors.
        if(!grounded || !neutral || (transform.position-previousRoot).sqrMagnitude>.0625f)
        {
            planted=false; hasStandingFeet=false; crouchWeight=0f;
        }
        previousRoot=transform.position;
        bool crouching=movement.IsCrouching && grounded && neutral;
        if(crouching && !planted)
        {
            // Normally supplied by the last standing IK pass. A direct crouch
            // spawn has no history: capture current foot bones instead of zero.
            if(!hasStandingFeet) CaptureSupport();
            planted=true;
        }
        crouchWeight=Mathf.MoveTowards(crouchWeight,crouching?1f:0f,Time.deltaTime/.28f);
        if(planted && crouchWeight>0f)
        {
            // No bodyPosition offset here: RootT already solves knee flexion.
            // Anchors originate from real bones; authored clips omit FootT/FootQ.
            Plant(AvatarIKGoal.LeftFoot,leftAnchor,leftRotation);
            Plant(AvatarIKGoal.RightFoot,rightAnchor,rightRotation);
        }
        if(!crouching && crouchWeight<=0f)
        {
            planted=false;
            if(grounded && neutral) CaptureSupport();
        }
    }
    void CaptureSupport()
    {
        leftAnchor=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        rightAnchor=animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        // IK goals use sole-forward/up axes, not the model's arbitrary bone axes.
        leftRotation=SupportRotation(HumanBodyBones.LeftToes,leftAnchor);
        rightRotation=SupportRotation(HumanBodyBones.RightToes,rightAnchor);
        hasStandingFeet=true;
    }
    Quaternion SupportRotation(HumanBodyBones toes,Vector3 ankle)
    {
        var toe=animator.GetBoneTransform(toes);
        Vector3 forward=Vector3.ProjectOnPlane(toe ? toe.position-ankle : transform.forward,Vector3.up);
        return Quaternion.LookRotation(forward.sqrMagnitude>.0001f ? forward : transform.forward,Vector3.up);
    }
    void ClearWeights()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot,0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot,0f);
    }
    void Plant(AvatarIKGoal goal,Vector3 position,Quaternion rotation)
    {
        animator.SetIKPositionWeight(goal,crouchWeight);
        animator.SetIKRotationWeight(goal,crouchWeight);
        animator.SetIKPosition(goal,position); animator.SetIKRotation(goal,rotation);
    }
}
