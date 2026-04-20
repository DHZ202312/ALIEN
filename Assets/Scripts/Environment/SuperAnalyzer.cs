using System.Collections;
using UnityEngine;

public class SuperAnalyzer : MonoBehaviour
{
    public GameObject Drawer;
    public PowerUnit pu;

    [Header("Hatch")]
    public Transform glassHatch;
    public float hatchOpenDuration = 0.8f;

    [Header("Drawer Settings")]
    public float retractDistance = 0.5f;
    public float retractDuration = 0.3f;
    public float analyzingTime = 7f;

    private Vector3 startLocalPos;
    private Vector3 targetLocalPos;

    private Quaternion hatchStartLocalRot;
    private Quaternion hatchOpenLocalRot;

    private bool hasRetracted = false;
    private bool hatchOpened = false;

    void Start()
    {
        startLocalPos = Drawer.transform.localPosition;
        targetLocalPos = new Vector3(-0.5445496f, 1.24f, -0.314781f);

        if (glassHatch != null)
        {
            hatchStartLocalRot = glassHatch.localRotation;
            hatchOpenLocalRot = hatchStartLocalRot * Quaternion.Euler(0f, 180f, 0f);
        }
    }

    public void DrawerRetract()
    {
        if (hasRetracted) return;

        hasRetracted = true;
        StartCoroutine(RetractRoutine());
        StartCoroutine(PowerOut());
    }

    IEnumerator RetractRoutine()
    {
        float t = 0f;

        Vector3 from = Drawer.transform.localPosition;
        Vector3 to = targetLocalPos;

        while (t < 1f)
        {
            t += Time.deltaTime / retractDuration;
            Drawer.transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }

        Drawer.transform.localPosition = to;
    }

    IEnumerator PowerOut()
    {
        yield return new WaitForSeconds(2f);
        pu.PowerDrain();
    }

    public void WaitThenOpenHatch()
    {
        StartCoroutine(waitthenHatch());
    }

    IEnumerator waitthenHatch()
    {
        yield return new WaitForSeconds(analyzingTime);
        yield return StartCoroutine(OpenHatchRoutine());
    }

    IEnumerator OpenHatchRoutine()
    {
        if (glassHatch == null || hatchOpened)
            yield break;

        hatchOpened = true;

        float t = 0f;
        Quaternion from = glassHatch.localRotation;
        Quaternion to = hatchOpenLocalRot;

        while (t < 1f)
        {
            t += Time.deltaTime / hatchOpenDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            glassHatch.localRotation = Quaternion.Slerp(from, to, easedT);
            yield return null;
        }

        glassHatch.localRotation = to;
    }
}