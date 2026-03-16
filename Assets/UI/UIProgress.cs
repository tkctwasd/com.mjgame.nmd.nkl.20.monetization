using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIProgress : MonoBehaviour
{
    [SerializeField] bool showText = true;
    [SerializeField] bool fractions = false;
    [SerializeField] bool percent = true;

    [SerializeField] protected TMP_Text text;
    [SerializeField] protected Image imgFill;
    [SerializeField] protected Image imgFillBackground;

    [SerializeField] protected Color colorFill = Color.green;
    [SerializeField] protected Color colorBackground = Color.gray;

    [SerializeField] protected float progress = 1f;
    [SerializeField] protected float min = 0f;
    [SerializeField] protected float max = 1f;

    public float Progress
    {
        get => progress;
        set
        {
            progress = Mathf.Clamp(value, min, max);
            UpdateProgress();
        }
    }

    public void SetColor(Color color)
    {
        if (imgFill != null)
            imgFill.color = color;
    }

    public void SetMin(float value) => min = value;
    public void SetMax(float value) => max = value;

    protected virtual void OnValidate()
    {
        if (imgFillBackground != null)
            imgFillBackground.color = colorBackground;
        if (imgFill != null)
            imgFill.color = colorFill;
    }

    public void UpdateProgress()
    {
        float fillAmount = Mathf.InverseLerp(min, max, progress);
        if (imgFill != null)
            imgFill.fillAmount = fillAmount;

        if (text != null)
        {
            if (showText)
            {
                if (percent)
                    text.text = $"{(int)(fillAmount * 100)}%";
                else if (fractions)
                    text.text = $"{(int)progress}/{(int)max}";
                else
                    text.text = $"{progress:F1}";
            }
            else
            {
                text.text = "";
            }
        }
    }
}
