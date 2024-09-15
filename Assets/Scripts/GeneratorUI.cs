using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorUI : MonoBehaviour
{
    private MapGenerator mapGenerator;

    void Start()
    {
        mapGenerator = GameObject.Find("MapGenerator").GetComponent<MapGenerator>();    
    }

    public void CreateButton()
    {
        mapGenerator.GenerateNewMap();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
