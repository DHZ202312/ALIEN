using UnityEngine;

public class TerminalToggleDoor : TerminalAction
{
    public DoorController door;

    public override void Execute()
    {
        door.ToggleDoor();
    }
}
