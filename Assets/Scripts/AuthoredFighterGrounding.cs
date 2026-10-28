using UnityEngine;

/// <summary>Only authored controller: physical flight clock and planted crouch feet.</summary>
[RequireComponent(typeof(Animator), typeof(FighterMovement))]
public sealed class AuthoredFighterGrounding : MonoBehaviour
{
    Animator animator;
    FighterMovement movement;
    FighterController fighter;
    Vector3 leftAnchor, rightAnchor;
    Quaternion leftRotation, rightRotation;
    bool planted;
    bool hasStandingFeet;
    float crouchWeight;
    static readonly int FlightRate = Animator.StringToHash("FlightRate");
    void Awake()
    {
        animator=GetComponent<Animator>(); movement=GetComponent<FighterMovement>();
        fighter=GetComponent<FighterController>();
    }
    void Update()
    {
        if(!animator.isHuman || !animator.runtimeAnimatorController || animator.runtimeAnimatorController.name!="AuthoredFighter") return;
        // The builder sets gravity=-20 and height=1.45: flight time = 2 sqrt(2h/g).
        animator.SetFloat(FlightRate, 1f / movement.ExpectedAirTime);
    }
    void OnAnimatorIK(int layer)
    {
        if(!animator.isHuman || !animator.runtimeAnimatorController || animator.runtimeAnimatorController.name!="AuthoredFighter") return;
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,0f);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot,0f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot,0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot,0f);
        bool crouching=movement.IsCrouching && movement.IsGrounded && fighter.CurrentState is NeutralState;
        if(crouching && !planted && hasStandingFeet)
        {
            planted=true;
        }
        crouchWeight=Mathf.MoveTowards(crouchWeight,crouching?1f:0f,Time.deltaTime/ .28f);
        if(crouchWeight>0f && planted)
        {
            animator.bodyPosition += Vector3.down * (.28f * animator.humanScale * crouchWeight);
            Plant(AvatarIKGoal.LeftFoot,leftAnchor,leftRotation);
            Plant(AvatarIKGoal.RightFoot,rightAnchor,rightRotation);
        }
        if(!crouching && crouchWeight<=0f)
        {
            planted=false;
            leftAnchor=animator.GetIKPosition(AvatarIKGoal.LeftFoot);
            rightAnchor=animator.GetIKPosition(AvatarIKGoal.RightFoot);
            leftRotation=animator.GetIKRotation(AvatarIKGoal.LeftFoot);
            rightRotation=animator.GetIKRotation(AvatarIKGoal.RightFoot);
            hasStandingFeet=true;
        }
    }
    void Plant(AvatarIKGoal goal,Vector3 position,Quaternion rotation)
    {
        animator.SetIKPositionWeight(goal,crouchWeight);
        animator.SetIKRotationWeight(goal,crouchWeight);
        animator.SetIKPosition(goal,position); animator.SetIKRotation(goal,rotation);
    }
}
