using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class LampTriggerController : MonoBehaviour
{
    public enum LampTriggerAction
    {
        EnableFlicker,
        TurnOffAndPlayParticles,
        EnableFlickerAndPlayParticles,
        TurnOnLightOnly,
        TurnOffLightOnly
    }

    public enum DoorTriggerAction
    {
        Lock,
        Unlock,
        ToggleLock
    }
    public enum VentTriggerAction
    {
        Drop,
        WaitThenDrop
    }

    [System.Serializable]
    public class LampEntry
    {
        [Header("Root")]
        public GameObject lampRoot;

        [Header("Auto Found Refs")]
        public MeshRenderer meshRenderer;
        public Light pointLight;
        public MonoBehaviour flickeringLightScript;
        public ParticleSystem breakParticles;

        [Header("Action")]
        public LampTriggerAction action = LampTriggerAction.EnableFlicker;

        [Header("Options")]
        public bool disableFlickerWhenTurningOff = true;
        public bool playParticlesOnlyOnce = true;

        public void AutoResolve()
        {
            if (lampRoot == null)
                return;

            Transform root = lampRoot.transform;

            if (meshRenderer == null)
            {
                meshRenderer = lampRoot.GetComponent<MeshRenderer>();
            }

            if (pointLight == null)
            {
                pointLight = root.GetComponentInChildren<Light>(true);
            }

            if (breakParticles == null)
            {
                ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
                if (particles != null && particles.Length > 0)
                {
                    breakParticles = particles[0];
                }
            }

            if (flickeringLightScript == null)
            {
                FlickeringLights[] scripts = root.GetComponentsInChildren<FlickeringLights>(true);
                for (int i = 0; i < scripts.Length; i++)
                {
                    if (scripts[i] == null) continue;

                    flickeringLightScript = scripts[i];
                    break;
                }
            }

            if (pointLight != null && flickeringLightScript == null)
            {
                FlickeringLights[] scripts = pointLight.GetComponents<FlickeringLights>();
                for (int i = 0; i < scripts.Length; i++)
                {
                    if (scripts[i] == null) continue;

                    flickeringLightScript = scripts[i];
                    break;
                }
            }
        }
    }

    [System.Serializable]
    public class DoorEntry
    {
        [Header("Door Root")]
        public GameObject doorRoot;

        [Header("Auto Found Ref")]
        public DoorController doorController;

        [Header("Action")]
        public DoorTriggerAction action = DoorTriggerAction.Lock;

        public void AutoResolve()
        {
            if (doorRoot == null)
                return;

            if (doorController == null)
            {
                doorController = doorRoot.GetComponent<DoorController>();
                if (doorController == null)
                    doorController = doorRoot.GetComponentInChildren<DoorController>(true);
            }
        }
    }

    [System.Serializable]
    public class VentEntry
    {
        [Header("Vent Root")]
        public GameObject ventRoot;

        [Header("Auto Found Ref")]
        public VentCover VentCoverController;

        [Header("Action")]
        public VentTriggerAction action = VentTriggerAction.Drop;

        public void AutoResolve()
        {
            if (ventRoot == null)
                return;

            if (VentCoverController == null)
            {
                VentCoverController = ventRoot.GetComponent<VentCover>();
                if (VentCoverController == null)
                    VentCoverController = ventRoot.GetComponentInChildren<VentCover>(true);
            }
        }
    }

    [Header("Trigger Settings")]
    public List<LampEntry> lamps = new List<LampEntry>();
    public List<DoorEntry> doors = new List<DoorEntry>();
    public List<VentEntry> vents = new List<VentEntry>();

    public string playerTag = "Player";
    public bool triggerOnce = true;
    public bool triggerOnEnter = true;
    public bool autoResolveOnAwake = true;

    [Header("Lamp Materials")]
    public Material lightOnMaterial;
    public Material lightOffMaterial;

    private bool hasTriggered;

    private void Awake()
    {
        if (autoResolveOnAwake)
        {
            AutoResolveAll();
        }
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter)
            return;

        TryTrigger(other);
    }

    private void TryTrigger(Collider other)
    {
        if (hasTriggered && triggerOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        ExecuteAllLampActions();
        ExecuteAllDoorActions();
        ExecuteAllVentActions();

        if (triggerOnce)
            hasTriggered = true;
    }

    [ContextMenu("Auto Resolve All")]
    public void AutoResolveAll()
    {
        AutoResolveAllLamps();
        AutoResolveAllDoors();
        AutoResolveAllVents();
    }

    [ContextMenu("Auto Resolve All Lamps")]
    public void AutoResolveAllLamps()
    {
        for (int i = 0; i < lamps.Count; i++)
        {
            if (lamps[i] != null)
                lamps[i].AutoResolve();
        }
    }

    [ContextMenu("Auto Resolve All Doors")]
    public void AutoResolveAllDoors()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            if (doors[i] != null)
                doors[i].AutoResolve();
        }
    }
    [ContextMenu("Auto Resolve All Vents")]
    public void AutoResolveAllVents()
    {
        for (int i = 0; i < vents.Count; i++)
        {
            if (vents[i] != null)
                vents[i].AutoResolve();
        }
    }

    public void ExecuteAllLampActions()
    {
        for (int i = 0; i < lamps.Count; i++)
        {
            ExecuteLampAction(lamps[i]);
        }
    }

    public void ExecuteAllDoorActions()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            ExecuteDoorAction(doors[i]);
        }
    }
    public void ExecuteAllVentActions()
    {
        for (int i = 0; i < vents.Count; i++)
        {
            ExecuteVentAction(vents[i]);
        }
    }

    private void ExecuteLampAction(LampEntry lamp)
    {
        if (lamp == null)
            return;

        if (lamp.meshRenderer == null || lamp.pointLight == null || lamp.breakParticles == null || lamp.flickeringLightScript == null)
        {
            lamp.AutoResolve();
        }

        switch (lamp.action)
        {
            case LampTriggerAction.EnableFlicker:
                EnablePointLight(lamp, true);
                EnableFlicker(lamp, true);
                ApplyLampMaterial(lamp, false);
                break;

            case LampTriggerAction.TurnOffAndPlayParticles:
                if (lamp.disableFlickerWhenTurningOff)
                    EnableFlicker(lamp, false);

                EnablePointLight(lamp, false);
                PlayParticles(lamp);
                ApplyLampMaterial(lamp, false);
                break;

            case LampTriggerAction.EnableFlickerAndPlayParticles:
                EnablePointLight(lamp, true);
                EnableFlicker(lamp, true);
                PlayParticles(lamp);
                ApplyLampMaterial(lamp, false);
                break;

            case LampTriggerAction.TurnOnLightOnly:
                EnablePointLight(lamp, true);
                EnableFlicker(lamp, false);
                ApplyLampMaterial(lamp, true);
                break;

            case LampTriggerAction.TurnOffLightOnly:
                if (lamp.disableFlickerWhenTurningOff)
                    EnableFlicker(lamp, false);

                EnablePointLight(lamp, false);
                ApplyLampMaterial(lamp, false);
                break;
        }
    }

    private void ExecuteDoorAction(DoorEntry door)
    {
        if (door == null)
            return;

        if (door.doorController == null)
        {
            door.AutoResolve();
        }

        if (door.doorController == null)
            return;

        switch (door.action)
        {
            case DoorTriggerAction.Lock:
                if (!door.doorController.isLocked)
                {
                    door.doorController.LockDoor();
                }
                break;

            case DoorTriggerAction.Unlock:
                if (door.doorController.isLocked)
                {
                    door.doorController.UnlockDoor();
                }
                break;

            case DoorTriggerAction.ToggleLock:
                door.doorController.SetLocked(!door.doorController.isLocked);
                break;
        }
    }
    private void ExecuteVentAction(VentEntry vent)
    {
        if (vent == null)
            return;
        if (vent.VentCoverController == null)
        {
            vent.AutoResolve();
        }
        if (vent.VentCoverController == null)
            return;
        switch (vent.action)
        {
            case VentTriggerAction.Drop:
                vent.VentCoverController.ventDrop();
                break;
            case VentTriggerAction.WaitThenDrop:
                vent.VentCoverController.ventWaitDrop();
                break;
        }
    }

    private void EnablePointLight(LampEntry lamp, bool enabledState)
    {
        if (lamp.pointLight != null)
            lamp.pointLight.enabled = enabledState;
    }

    private void EnableFlicker(LampEntry lamp, bool enabledState)
    {
        if (lamp.flickeringLightScript != null)
            lamp.flickeringLightScript.enabled = enabledState;
    }

    private void PlayParticles(LampEntry lamp)
    {
        if (lamp.breakParticles == null)
            return;

        lamp.breakParticles.gameObject.SetActive(true);

        if (lamp.playParticlesOnlyOnce)
        {
            if (!lamp.breakParticles.isPlaying && lamp.breakParticles.time <= 0f)
            {
                lamp.breakParticles.Play();
            }
        }
        else
        {
            lamp.breakParticles.Play();
        }
    }

    private void ApplyLampMaterial(LampEntry lamp, bool isOn)
    {
        if (lamp.meshRenderer == null)
            return;

        Material targetMat = isOn ? lightOnMaterial : lightOffMaterial;
        if (targetMat == null)
            return;

        lamp.meshRenderer.material = targetMat;
    }
}