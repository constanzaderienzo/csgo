using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Actions 
{
    public int id;
    public int inputIndex;
    public bool jump;
    public bool left;
    public bool right;
    public bool up;
    public bool down;
    public bool shift;
    public bool ctrl;
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public AnimationState animationState;

    public Actions(int id, int index, bool jump, bool left, bool right, bool up, bool down, bool shift, bool ctrl, Vector3 eulerAngles, AnimationState animationState) {
        this.id = id;
        inputIndex = index;
        this.jump = jump;
        this.left = left;
        this.right = right;
        this.up = up;
        this.down = down;
        this.shift = shift;
        this.ctrl = ctrl;
        rotationX = eulerAngles.x;
        rotationY = eulerAngles.y;
        rotationZ = eulerAngles.z;
        this.animationState = animationState;
    }

    public Actions() {

    }
    public void SerializeInput(BitBuffer buffer) {
        buffer.PutInt(id);
        buffer.PutInt(inputIndex);
        buffer.PutBit(jump);
        buffer.PutBit(left);
        buffer.PutBit(right);
        buffer.PutBit(up);
        buffer.PutBit(down);
        buffer.PutBit(shift);
        buffer.PutBit(ctrl);
        buffer.PutFloat(rotationX);
        buffer.PutFloat(rotationY);
        buffer.PutFloat(rotationZ);
        animationState.Serialize(buffer);
    }

    public void DeserializeInput(BitBuffer buffer) {
        id = buffer.GetInt();
        inputIndex = buffer.GetInt();
        jump = buffer.GetBit();
        left = buffer.GetBit();
        right = buffer.GetBit();
        up = buffer.GetBit();
        down = buffer.GetBit();
        shift = buffer.GetBit();
        ctrl = buffer.GetBit();
        rotationX = buffer.GetFloat();
        rotationY = buffer.GetFloat();
        rotationZ = buffer.GetFloat();
        animationState = AnimationState.Deserialize(buffer);
    }
    
}