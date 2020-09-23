using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{
    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject cubeGameObject;

    public string id;

    public CubeEntity(GameObject cubeGameObject, string id) {
        this.cubeGameObject = cubeGameObject;
        this.id = id;
    }

    public void Serialize(BitBuffer buffer) {
        var transform = cubeGameObject.transform;
        var position = transform.position;
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(transform.eulerAngles.x);
        buffer.PutFloat(transform.eulerAngles.y);
        buffer.PutFloat(transform.eulerAngles.z);
    }

    public void Deserialize(BitBuffer buffer) {
        position = new Vector3();
        eulerAngles = new Vector3();
        position.x = buffer.GetFloat();
        position.y = buffer.GetFloat();
        position.z = buffer.GetFloat();
        eulerAngles.x = buffer.GetFloat();
        eulerAngles.y = buffer.GetFloat();
        eulerAngles.z = buffer.GetFloat();
    }

    public static CubeEntity CreateInterpolated(CubeEntity previous, CubeEntity next, float t) {
        var cubeEntity = new CubeEntity(previous.cubeGameObject, previous.id);
        cubeEntity.position += Vector3.Lerp(previous.position, next.position, t);
        cubeEntity.eulerAngles += Vector3.Lerp(previous.eulerAngles, next.eulerAngles, t);
        return cubeEntity;
    }

    public void Apply() {
        cubeGameObject.GetComponent<Transform>().position = position;
        cubeGameObject.GetComponent<Transform>().eulerAngles = eulerAngles;
    }

    public void ApplyClientInput(Actions action) {
        // 0 = jumps
        // 1 = left
        // 2 = right
        if (action.jump) {
            cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.up * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (action.left) {
            cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.left * 5, Vector3.zero, ForceMode.Impulse);
        }
        if (action.right) {
            cubeGameObject.GetComponent<Rigidbody>().AddForceAtPosition(Vector3.right * 5, Vector3.zero, ForceMode.Impulse);
        }

    }
}
