using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemSettings : MonoBehaviour
{
    private bool vSync = false;
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            vSync = !vSync;
            QualitySettings.vSyncCount = vSync ? 1 : 0;
        }
    }
}
