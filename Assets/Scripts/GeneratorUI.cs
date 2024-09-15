using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorUI : MonoBehaviour
{
    [SerializeField] MapGenerator mapGenerator;

    void Start()
    {
        mapGenerator = GameObject.Find("MapGenerator").GetComponent<MapGenerator>();    
    }

    public void CreateButton()
    {
        mapGenerator.GenerateNewMap();
    }
}
