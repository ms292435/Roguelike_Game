using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    public string Name;
    public Sprite Icon;
    [TextArea] public string Description;
}

