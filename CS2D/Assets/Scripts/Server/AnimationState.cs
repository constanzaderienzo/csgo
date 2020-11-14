using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationState
{
    public int weaponType;
    public float speed;
    public bool shoot, reload, death, jump, grounded, crouch, fullAuto;

    public AnimationState()
    {
        weaponType = 1;
        speed = 0f;
        shoot = false;
        reload = false;
        death = false;
        jump = false;
        grounded = false;
        crouch = false;
        fullAuto = false;
    }

    public void Serialize(BitBuffer buffer)
    {
        buffer.PutInt(weaponType);
        buffer.PutFloat(speed);
        buffer.PutBit(shoot);
        buffer.PutBit(reload);
        buffer.PutBit(death);
        buffer.PutBit(jump);
        buffer.PutBit(grounded);
        buffer.PutBit(crouch);
        buffer.PutBit(fullAuto);
    }

    public static AnimationState Deserialize(BitBuffer buffer)
    {
        AnimationState animationState = new AnimationState();
        animationState.weaponType = buffer.GetInt();
        animationState.speed = buffer.GetFloat();
        animationState.shoot = buffer.GetBit();
        animationState.reload = buffer.GetBit();
        animationState.death = buffer.GetBit();
        animationState.jump = buffer.GetBit();
        animationState.grounded = buffer.GetBit();
        animationState.crouch = buffer.GetBit();
        animationState.fullAuto = buffer.GetBit();

        return animationState;
    }

    public static AnimationState GetFromAnimator(Animator animator)
    {
        AnimationState animationState = new AnimationState();
        animationState.weaponType = animator.GetInteger("WeaponType_int");
        animationState.speed = animator.GetFloat("Speed_f");
        animationState.shoot = animator.GetBool("Shoot_b");
        animationState.reload = animator.GetBool("Reload_b");
        animationState.death = animator.GetBool("Death_b");
        animationState.jump = animator.GetBool("Jump_b");
        animationState.grounded = animator.GetBool("Grounded_b");
        animationState.crouch = animator.GetBool("Crouch_b");
        animationState.fullAuto = animator.GetBool("FullAuto_b");

        return animationState;
    }

    public void SetToAnimator(Animator animator)
    {
        animator.SetInteger("WeaponType_int", weaponType);
        animator.SetFloat("Speed_f", speed);
        animator.SetBool("Shoot_b", shoot);
        animator.SetBool("Reload_b", reload);
        animator.SetBool("Death_b", death);
        animator.SetBool("Jump_b", jump);
        animator.SetBool("Grounded_b", grounded);
        animator.SetBool("Crouch_b", crouch);
        animator.SetBool("FullAuto_b", fullAuto);
    }
}
