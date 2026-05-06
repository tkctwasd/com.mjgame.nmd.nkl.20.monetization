using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IAPPackButton : IAPButton
{
    [Header("Pack labels")]
    public string fallbackTitle;
    public string fallbackDescription;

    protected override void RefreshUI()
    {
        base.RefreshUI();

        if (!string.IsNullOrEmpty(fallbackTitle) && titleText != null)
            titleText.text = fallbackTitle;

        if (!string.IsNullOrEmpty(fallbackDescription) && descriptionText != null)
            descriptionText.text = fallbackDescription;
    }
}
