using UnityEngine;

public class PowerSourceTrigger : MonoBehaviour
{
    public PowerSource powerSource;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        PowerUnit unit = other.GetComponentInChildren<PowerUnit>();

        if (unit == null)
            return;

        if (powerSource == null)
            return;

        unit.PowerOff();
        powerSource.ClearInsertedUnit();
    }
}
