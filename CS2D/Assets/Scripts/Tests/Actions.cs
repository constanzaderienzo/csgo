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

    // 0 = jumps
    // 1 = left
    // 2 = right

    public Actions(int id, int index, bool jump, bool left, bool right, bool up, bool down) {
        this.id         = id;
        inputIndex = index;
        this.jump       = jump;
        this.left       = left;
        this.right      = right;
        this.up = up;
        this.down = down;
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
    }

    public void DeserializeInput(BitBuffer buffer) {
        id          = buffer.GetInt();
        inputIndex  = buffer.GetInt();
        jump        = buffer.GetBit();
        left        = buffer.GetBit();
        right       = buffer.GetBit();
        up = buffer.GetBit();
        down = buffer.GetBit();
    }
    
}