using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Actions 
{
    public string id;
    public int inputIndex;
    public bool jump;
    public bool left;
    public bool right;

    public bool connected;

    // 0 = jumps
    // 1 = left
    // 2 = right

    public Actions(string id, int index, bool jump, bool left, bool right, bool connected) {
        this.id         = id;
        this.inputIndex = index;
        this.jump       = jump;
        this.left       = left;
        this.right      = right;
        this.connected  = connected;
    }

    public Actions() {

    }
    public void SerializeInput(BitBuffer buffer) {
        buffer.PutString(id);
        buffer.PutInt(inputIndex);
        buffer.PutBit(jump);
        buffer.PutBit(left);
        buffer.PutBit(right);
        buffer.PutBit(connected);
    }

    public void DeserializeInput(BitBuffer buffer) {
        id          = buffer.GetString();
        inputIndex  = buffer.GetInt();
        jump        = buffer.GetBit();
        left        = buffer.GetBit();
        right       = buffer.GetBit();
        connected   = buffer.GetBit();
    }
    
}