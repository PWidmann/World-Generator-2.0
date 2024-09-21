using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class FpsCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    private string fpsString = " FPS";
    private int currentFPS;
    private List<int> intList = new List<int>();
    private int avg = 0;

    void Update()
    {
        intList.Add((int)(1 / Time.deltaTime));
        text.text = GetAvgFPS().ToString() + fpsString;
    }

    private int GetAvgFPS()
    {
        if (intList.Count > 10) { intList.RemoveAt(0); }
        return intList.Sum() / intList.Count;
    }
}
