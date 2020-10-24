using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientEntity
{
    public Vector3 position;
    public Vector3 eulerAngles;
    public GameObject playerGameObject;

    public ClientEntity(GameObject playerGameObject) {
        this.playerGameObject = playerGameObject;
        this.position = playerGameObject.transform.position;
        this.eulerAngles = playerGameObject.transform.eulerAngles;
    }
    public ClientEntity(GameObject playerGameObject, Vector3 position, Vector3 eulerAngles)
    {
        this.playerGameObject = playerGameObject;
        this.position = position;
        this.eulerAngles = eulerAngles;
    }

    public ClientEntity()
    {
    }

    public void Serialize(BitBuffer buffer) {
        var transform = playerGameObject.transform;
        var position = playerGameObject.transform.position;
        buffer.PutFloat(position.x);
        buffer.PutFloat(position.y);
        buffer.PutFloat(position.z);
        buffer.PutFloat(transform.eulerAngles.x);
        buffer.PutFloat(transform.eulerAngles.y);
        buffer.PutFloat(transform.eulerAngles.z);
    }

    public static ClientEntity DeserializeInfo(BitBuffer buffer) {
        ClientEntity newPlayer = new ClientEntity();
        newPlayer.position = new Vector3();
        newPlayer.eulerAngles = new Vector3();
        newPlayer.position.x = buffer.GetFloat();
        newPlayer.position.y = buffer.GetFloat();
        newPlayer.position.z = buffer.GetFloat();
        newPlayer.eulerAngles.x = buffer.GetFloat();
        newPlayer.eulerAngles.y = buffer.GetFloat();
        newPlayer.eulerAngles.z = buffer.GetFloat();
        return newPlayer;
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

    public static void CreateInterpolatedAndApply(ClientEntity previous, ClientEntity next, GameObject gameObject, float t) {
        Debug.Log("Interpolating " + previous.position + next.position);

        var cubeEntity = new ClientEntity(gameObject);
        cubeEntity.position = Vector3.Lerp(previous.position, next.position, t);
        cubeEntity.eulerAngles = Vector3.Lerp(previous.eulerAngles, next.eulerAngles, t);
        cubeEntity.Apply();
    }

    public void Apply() {
        playerGameObject.GetComponent<Transform>().position = position;
        playerGameObject.GetComponent<Transform>().eulerAngles = eulerAngles;
    }


}
