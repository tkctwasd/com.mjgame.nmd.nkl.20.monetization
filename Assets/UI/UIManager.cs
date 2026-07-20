using System;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Transform panelParent;
    private readonly Dictionary<Type, UIPanel> typedPanels = new();
    private readonly Dictionary<Type, Type> panelMapping = new()
    {
    };
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
        InitializePanels();

    }

    private void Start()
    {
        ShowPanel<PanelLoading>();
    }

    private void InitializePanels()
    {
        UIPanel[] foundPanels = panelParent.GetComponentsInChildren<UIPanel>(true);

        foreach (var panel in foundPanels)
        {
            RegisterPanel(panel);
        }

        Debug.Log($"<color=yellow>[UIManager]</color> Registered {typedPanels.Count} panels.");
    }

    public void RegisterPanel(UIPanel panel)
    {
        if (panel == null) return;

        var type = panel.GetType();
        if (!typedPanels.ContainsKey(type))
        {
            typedPanels.Add(type, panel);
        }

        panel.gameObject.SetActive(false);
    }

    public T GetPanel<T>() where T : UIPanel
    {
        var type = typeof(T);

        if (typedPanels.TryGetValue(type, out var panel))
            return panel as T;

        Debug.LogError($"[UIManager] Panel of type {type.Name} not found!");
        return null;
    }

    public T ShowPanel<T>() where T : UIPanel
    {
        var panel = GetPanel<T>();
        if (panel == null) return null;

        panel.Show();
        return panel;
    }

    public void HidePanel<T>() where T : UIPanel
    {
        var panel = GetPanel<T>();
        if (panel != null && panel.IsShown)
            panel.Hide();
    }

    public void HideAllPanels()
    {
        foreach (var panel in typedPanels.Values)
        {
            if (panel.IsShown)
                panel.Hide();
        }
    }

    public void SetAllPanelsOpacity(float alpha)
    {
        foreach (var panel in typedPanels.Values)
        {
            panel.SetOpacity(alpha);
        }
    }

    public bool HasAnyPanelActiveExcept(params Type[] exceptPanels)
    {
        foreach (var panel in typedPanels.Values)
        {
            bool isExcept = false;
            foreach (var exType in exceptPanels)
            {
                if (panel.GetType() == exType)
                {
                    isExcept = true;
                    break;
                }
            }

            if (isExcept) continue;

            if (panel.IsShown)
                return true;
        }

        return false;
    }

}
