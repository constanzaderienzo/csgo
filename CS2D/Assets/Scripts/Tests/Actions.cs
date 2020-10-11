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
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public int hitPlayerId;

    public Actions(int id, int index, bool jump, bool left, bool right, bool up, bool down, Vector3 eulerAngles, int hitPlayerId) {
        this.id = id;
        inputIndex = index;
        this.jump = jump;
        this.left = left;
        this.right = right;
        this.up = up;
        this.down = down;
        rotationX = eulerAngles.x;
        rotationY = eulerAngles.y;
        rotationZ = eulerAngles.z;
        this.hitPlayerId = hitPlayerId;
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
        buffer.PutFloat(rotationX);
        buffer.PutFloat(rotationY);
        buffer.PutFloat(rotationZ);
        buffer.PutInt(hitPlayerId);
    }

    public void DeserializeInput(BitBuffer buffer) {
        id = buffer.GetInt();
        inputIndex = buffer.GetInt();
        jump = buffer.GetBit();
        left = buffer.GetBit();
        right = buffer.GetBit();
        up = buffer.GetBit();
        down = buffer.GetBit();
        rotationX = buffer.GetFloat();
        rotationY = buffer.GetFloat();
        rotationZ = buffer.GetFloat();
        hitPlayerId = buffer.GetInt();
    }
    
}