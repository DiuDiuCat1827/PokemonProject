using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialObjectSpawn : MonoBehaviour
{
    [SerializeField] GameObject essentialObjectPrefab;

    private void Awake()
    {
      var existingObjects =   FindObjectsOfType<EssentialObjects>();
        if(existingObjects.Length == 0)
        {
            var spawPos = new Vector3(0,0,0);

            var grid = FindObjectOfType<Grid>();

            if (grid != null)
            {
                spawPos = grid.transform.position;
            }
            Instantiate(essentialObjectPrefab, spawPos, Quaternion.identity);
        }
    }
}
