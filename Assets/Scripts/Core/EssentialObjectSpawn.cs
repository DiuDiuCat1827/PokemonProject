using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialObjectSpawn : MonoBehaviour
{
    [SerializeField] GameObject seentialObjectPrefab;

    private void Awake()
    {
      var existingObjects =   FindObjectsOfType<EssentialObjects>();
        if(existingObjects.Length == 0)
        {
            Instantiate(seentialObjectPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        }
    }
}
