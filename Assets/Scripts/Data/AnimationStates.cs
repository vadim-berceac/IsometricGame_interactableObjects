using UnityEngine;

public class AnimationStates
{
    public readonly int MotionXHash =  Animator.StringToHash("MotionX");
    public readonly int MotionYHash =  Animator.StringToHash("MotionY");
    public readonly int MotionSpeedHash =  Animator.StringToHash("MotionSpeed");
    public readonly int InteractHash =  Animator.StringToHash("Interact");
    public readonly int TurnHash =  Animator.StringToHash("Turn");
    public readonly int LadderDirectionHash =  Animator.StringToHash("LadderDirection");
    public readonly int Ladder = Animator.StringToHash("Ladder");
    
    public readonly int HashActivePara = Animator.StringToHash ("Active");
}
