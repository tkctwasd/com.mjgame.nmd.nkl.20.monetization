using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelWaiting : UIPanel
{
    void OnEnable()
    {
        StartCoroutine(WaitAndHideIE());
    }

    private IEnumerator WaitAndHideIE()
    {
        yield return new WaitForSeconds(2f);
        Hide();
    }
}
