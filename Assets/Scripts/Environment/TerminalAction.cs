using UnityEngine;

public abstract class TerminalAction : MonoBehaviour
{
    public string actionName;

    public abstract void Execute();
}
