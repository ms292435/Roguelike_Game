using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public interface IObserver
    {
        void OnNotify(ISubject pSubject, string pEventName);
    }
}