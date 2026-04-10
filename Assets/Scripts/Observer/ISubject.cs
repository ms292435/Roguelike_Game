using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public interface ISubject
    {
        void AddObserver(IObserver pObserver);
        void RemoveObserver(IObserver pObserver);
    }
}