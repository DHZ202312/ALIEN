using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class TriggerController : MonoBehaviour
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
    public enum AnalyzerTriggerAction
    {
        Activate
    }
    public enum EnemyTriggerAction
    {
        DropWaitActive,
        Activate,
        WaitAndStare,
        Deactivated
    }
    public enum FakeEnemyTriggerAction
    {
        Activate,
        Deactivate,
        StartPatrol,
        Stop,
        TriggerVentDrop
    }
    public enum TriggerTriggerAction
    {
        Activate
    }

    [System.Serializable]
    public class LampEntry
    {
        [Header("Root")]
        public GameObject lampRoot;

        [Header("Auto Found Refs")]
        public MeshRenderer meshRenderer;
        public Light pointLight;
        public FlickeringLights flickeringLightScript;
        public ParticleSystem breakParticles;
        public AudioSource ads;

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

            ads = lampRoot.GetComponentInChildren<AudioSource>();

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

    [System.Serializable]
    public class AnalyzerEntry
    {
        [Header("Analyzer")]
        public SuperAnalyzer analyzerController;

        [Header("Action")]
        public AnalyzerTriggerAction action = AnalyzerTriggerAction.Activate;
    }
    [System.Serializable]
    public class EnemyEntry
    {
        [Header("Enemy Root")]
        public GameObject ERoot;
        public Rigidbody rb;
        [Header("Enemy Anim Controller")]
        public EnemyAnim ea;

        [Header("Enemy AI")]
        public EnemyAI ai;

        [Header("Action")]
        public EnemyTriggerAction action = EnemyTriggerAction.Activate;

        public void AutoResolve()
        {
            if (ERoot == null)
                return;

            if (ea == null)
            {
                ea = ERoot.GetComponent<EnemyAnim>();
                if (ai == null)
                    ai = ERoot.GetComponent<EnemyAI>();
                if(rb == null)
                    rb = ERoot.GetComponent<Rigidbody>();
            }
        }
    }
    [System.Serializable]
    public class TriggerEntry
    {
        [Header("Trigger Root")]
        public GameObject TRoot;
        [Header("TriggerScript")]
        public TriggerController tc;

        [Header("Action")]
        public TriggerTriggerAction action = TriggerTriggerAction.Activate;

        public void AutoResolve()
        {
            if (TRoot == null)
                return;

            if (tc == null)
            {
                tc = TRoot.GetComponent<TriggerController>();
            }
        }
    }
    [System.Serializable]
    public class FakeEnemyEntry
    {
        [Header("Fake Enemy Root")]
        public GameObject fakeEnemyRoot;

        [Header("Auto Found Ref")]
        public VentCrawlSoundPatrol fakeEnemy;

        [Header("Linked Vent (Optional)")]
        public VentCover linkedVent;

        [Header("Action")]
        public FakeEnemyTriggerAction action = FakeEnemyTriggerAction.Activate;

        public void AutoResolve()
        {
            if (fakeEnemyRoot == null)
                return;

            if (fakeEnemy == null)
            {
                fakeEnemy = fakeEnemyRoot.GetComponent<VentCrawlSoundPatrol>();
                if (fakeEnemy == null)
                    fakeEnemy = fakeEnemyRoot.GetComponentInChildren<VentCrawlSoundPatrol>(true);
            }
        }
    }
    public float WaitBeforeAnim = 5f;
    public float WaitBeforeTrigger = 1f;


    [Header("Trigger Settings")]
    public List<LampEntry> lamps = new List<LampEntry>();
    public List<DoorEntry> doors = new List<DoorEntry>();
    public List<VentEntry> vents = new List<VentEntry>();
    public List<AnalyzerEntry> analyzers = new List<AnalyzerEntry>();
    public List<EnemyEntry> enemies = new List<EnemyEntry>();
    public List<TriggerEntry> triggers = new List<TriggerEntry>();
    public List<FakeEnemyEntry> fakeEnemies = new List<FakeEnemyEntry>();

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
        ExecuteAllAnalyzerActions();
        ExecuteAllEnemyActions();
        ExecuteAllTriggerActions();
        ExecuteAllFakeEnemyActions();
        if (triggerOnce)
            hasTriggered = true;
    }

    [ContextMenu("Auto Resolve All")]
    public void AutoResolveAll()
    {
        AutoResolveAllLamps();
        AutoResolveAllDoors();
        AutoResolveAllVents();
        AutoResolveAllEnemies();
        AutoResolveAllTriggers();
        AutoResolveAllFakeEnemies();
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

    public void AutoResolveAllEnemies()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                enemies[i].AutoResolve();
        }
    }
    public void AutoResolveAllFakeEnemies()
    {
        for (int i = 0; i < fakeEnemies.Count; i++)
        {
            if (fakeEnemies[i] != null)
                fakeEnemies[i].AutoResolve();
        }
    }
    public void AutoResolveAllTriggers()
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            if (triggers[i] != null)
                triggers[i].AutoResolve();
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
    public void ExecuteAllAnalyzerActions()
    {
        for (int i = 0; i < analyzers.Count; i++)
        {
            ExecuteAnalyzerAction(analyzers[i]);
        }
    }
    public void ExecuteAllEnemyActions()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            ExecuteEnemyAction(enemies[i]);
        }
    }
    public void ExecuteAllTriggerActions()
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            ExecuteTriggerAction(triggers[i]);
        }
    }
    public void ExecuteAllFakeEnemyActions()
    {
        for (int i = 0; i < fakeEnemies.Count; i++)
        {
            ExecuteFakeEnemyAction(fakeEnemies[i]);
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
                lamp.ads.enabled = true;
                lamp.ads.loop = false;
                lamp.ads.volume = 0.4f;
                lamp.ads.clip = lamp.flickeringLightScript.clips[1];
                lamp.ads.Play();

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
    private void ExecuteAnalyzerAction(AnalyzerEntry analyzer)
    {
        switch (analyzer.action)
        {
            case AnalyzerTriggerAction.Activate:
                analyzer.analyzerController.WaitThenOpenHatch();
                break;
        }
    }
    private void ExecuteEnemyAction(EnemyEntry enemy)
    {
        switch (enemy.action)
        {
            case EnemyTriggerAction.DropWaitActive:
                StartCoroutine(WaitThenActivateAnim());
                break;
            case EnemyTriggerAction.Activate:
                enemy.ai.enabled = true;
                break;
            case EnemyTriggerAction.WaitAndStare:
                enemy.ea.enabled = true;
                enemy.ai.enabled = true;
                enemy.ea.StartStare(enemy.ai.player);
                break;
            case EnemyTriggerAction.Deactivated:
                enemy.ea.gameObject.SetActive(false);
                break;
        }
        IEnumerator WaitThenActivateAnim()
        {
            yield return new WaitForSeconds(25);
            enemy.rb.isKinematic = false;
            yield return new WaitForSeconds(WaitBeforeAnim);
            enemy.ea.enabled = true;
        }
    }
    private void ExecuteTriggerAction(TriggerEntry trigger)
    {
        switch (trigger.action)
        {
            case TriggerTriggerAction.Activate:
                StartCoroutine(WaitThenActivate());
                break;
        }
        IEnumerator WaitThenActivate()
        {
            yield return new WaitForSeconds(WaitBeforeTrigger);
            trigger.TRoot.SetActive(true);
        }
    }
    private void ExecuteFakeEnemyAction(FakeEnemyEntry entry)
    {
        if (entry == null)
            return;

        if (entry.fakeEnemy == null)
            entry.AutoResolve();

        if (entry.fakeEnemy == null)
            return;

        switch (entry.action)
        {
            case FakeEnemyTriggerAction.Activate:
                entry.fakeEnemy.gameObject.SetActive(true);
                break;

            case FakeEnemyTriggerAction.Deactivate:
                entry.fakeEnemy.gameObject.SetActive(false);
                break;

            case FakeEnemyTriggerAction.StartPatrol:
                entry.fakeEnemy.StartPatrolFromFirstPoint();
                break;

            case FakeEnemyTriggerAction.Stop:
                entry.fakeEnemy.enabled = false;
                break;

            case FakeEnemyTriggerAction.TriggerVentDrop:
                if (entry.linkedVent != null)
                    entry.linkedVent.ventWaitDrop();
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