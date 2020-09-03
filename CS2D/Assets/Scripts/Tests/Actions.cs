using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Actions 
{
    public int inputIndex;
    public bool jump;
    public bool left;
    public bool right;

    public bool connected;

    // 0 = jumps
    // 1 = left
    // 2 = right

    public Actions(int index, bool jump, bool left, bool right, bool connected) {
        this.inputIndex = index;
        this.jump       = jump;
        this.left       = left;
        this.right      = right;
        this.connected  = connected;
    }

    public Actions() {

    }
    public void SerializeInput(BitBuffer buffer) {
        buffer.PutInt(inputIndex);
        buffer.PutBit(jump);
        buffer.PutBit(left);
        buffer.PutBit(right);
        buffer.PutBit(connected);
    }

    public void DeserializeInput(BitBuffer buffer) {
        inputIndex  = buffer.GetInt();
        jump        = buffer.GetBit();
        left        = buffer.GetBit();
        right       = buffer.GetBit();
        connected   = buffer.GetBit();
    }
    
}