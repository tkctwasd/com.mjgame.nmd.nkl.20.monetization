using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseModule : IService, IDisposable
{
    public abstract void Dispose();

    public abstract void Initialize();

    public abstract void SetInfo();
}
