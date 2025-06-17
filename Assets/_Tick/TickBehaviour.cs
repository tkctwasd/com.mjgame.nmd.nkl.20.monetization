using System;
using UnityEngine;

public class TickBehaviour : MonoBehaviour
{
    private bool isRegister = false;
    internal Action<bool> OnRegisterCallback { get; set; }

    protected virtual void Awake()
    {
        if (!isRegister)
        {
            TickUpdateManager.AddItem(this);
            isRegister = true;
            OnRegisterCallback?.Invoke(true);
        }
    }

    protected virtual void OnDisable()
    {
        if (isRegister)
        {
            TickUpdateManager.RemoveSpecificItem(this);
            isRegister = false;
            OnRegisterCallback?.Invoke(false);
        }
    }

    protected virtual void OnDestroy()
    {
        if (isRegister)
        {
            TickUpdateManager.RemoveSpecificItem(this);
            isRegister = false;
            OnRegisterCallback?.Invoke(false);
        }
    }

    protected virtual void OnEnable()
    {
        if (!isRegister)
        {
            TickUpdateManager.AddItem(this);
            isRegister = true;
            OnRegisterCallback?.Invoke(true);
        }
    }

    public virtual void OnUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnLateUpdate() { }

    public virtual void OnSlowUpdate() { }

    private Transform m_cachedTransform = null;
    public Transform CachedTransform
    {
        get
        {
            if (m_cachedTransform == null) m_cachedTransform = transform;
            return m_cachedTransform;
        }
    }
}