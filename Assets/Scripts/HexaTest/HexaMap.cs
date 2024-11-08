using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexaMap : MonoBehaviour
{
    int max = 100;
    public GameObject[] HexPrefab;
    public Material[] HexMaterials;

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        for (int column = 0; column < 10; column++)
        {
            for (int row = 0; row < 10; row++)
            {
                //Make HeaxTile

                //Tile's Position
                Hex h = new Hex(column, row);

                Instantiate(HexPrefab[Random.Range(0,HexPrefab.Length)],
                    /*new Vector3(column, 0, row)*/h.Position(),
                    Quaternion.identity,
                    this.transform);
            }
        }
    }
}
