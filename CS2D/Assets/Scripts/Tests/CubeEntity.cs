using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeEntity
{
    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject cubeGameObject;

    public CubeEntity(GameObject cubeGameObject) {
        this.cubeGameObject = cubeGameObject;
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

    public static CubeEntity Deserialize(BitBuffer buffer) {
        CubeEntity newCube = new CubeEntity(null);
        newCube.position = new Vector3();
        newCube.eulerAngles = new Vector3();
        newCube.position.x = buffer.GetFloat();
        newCube.position.y = buffer.GetFloat();
        newCube.position.z = buffer.GetFloat();
        newCube.eulerAngles.x = buffer.GetFloat();
        newCube.eulerAngles.y = buffer.GetFloat();
        newCube.eulerAngles.z = buffer.GetFloat();
        return newCube;
    }

    public static CubeEntity CreateInterpolated(CubeEntity previous, CubeEntity next, float t) {
        var cubeEntity = new CubeEntity(previous.cubeGameObject);
        cubeEntity.position += Vector3.Lerp(previous.position, next.position, t);
        cubeEntity.eulerAngles += Vector3.Lerp(previous.eulerAngles, next.eulerAngles, t);
        return cubeEntity;
    }

    public void Apply() {
        cubeGameObject.GetComponent<Transform>().position = position;
        cubeGameObject.GetComponent<Transform>().eulerAngles = eulerAngles;
    }


}
