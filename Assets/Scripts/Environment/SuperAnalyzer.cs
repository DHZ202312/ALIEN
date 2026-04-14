using System.Collections;
using UnityEngine;

public class SuperAnalyzer : MonoBehaviour
{
    public GameObject Drawer;
    public PowerUnit pu;

    [Header("Drawer Settings")]
    public float retractDistance = 0.5f;
    public float retractDuration = 0.3f;

    private Vector3 startLocalPos;
    private Vector3 targetLocalPos;

    private bool hasRetracted = false;

    void Start()
    {
        startLocalPos = Drawer.transform.localPosition;
        targetLocalPos = new Vector3(-0.5445496f, 1.24f, -0.314781f);
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
}
