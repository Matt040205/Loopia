using UnityEngine;

/// <summary>Billboard com densidade 3D, sincronizado com o sol e com a neblina do shader.</summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CloudController : MonoBehaviour
{
    [Header("Movimento individual")]
    public Vector3 driftDirection = Vector3.right;
    public float driftSpeed = 0.3f;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 0.5f;
    public bool wrapAround = true;
    [Min(1f)] public float wrapDistance = 40f;

    [Header("Volume")]
    public bool controlledByLayer;
    public bool faceCamera = true;
    public float cloudSeed = 1f;
    [Range(0f, 1f)] public float opacityMultiplier = 1f;
    public float billboardTilt;

    [Header("Sincronia dia/noite")]
    public DayNightController dayNightController;
    [Range(0f, 1f)] public float minOpacityAtNight = 0.15f;
    [Range(0f, 1f)] public float maxOpacityAtDay = 0.85f;
    public float peakSunIntensity = 1.4f;
    public bool tintWithSunColor = true;
    [Range(0f, 1f)] public float tintStrength = 0.25f;

    Vector3 basePosition;
    Renderer rend;
    MaterialPropertyBlock mpb;
    static readonly int ColorID = Shader.PropertyToID("_Color");
    static readonly int SeedID = Shader.PropertyToID("_CloudSeed");

    void OnEnable()
    {
        basePosition = transform.position;
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        // O quad não tem espessura, mas o shader amostra uma caixa de volume.
        rend.localBounds = new Bounds(Vector3.zero, Vector3.one);
    }

    void Update()
    {
        if (!controlledByLayer)
        {
            Camera camera3d = Camera.main;
            if (faceCamera && camera3d != null)
                transform.rotation = camera3d.transform.rotation * Quaternion.Euler(0f, 0f, billboardTilt);
            if (!Application.isPlaying) basePosition = transform.position;
            else UpdateMovement(camera3d);
        }
        AtualizarAparencia();
    }

    void UpdateMovement(Camera camera3d)
    {
        Vector3 direction = Vector3.ProjectOnPlane(driftDirection, Vector3.up).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.right;
        basePosition += direction * driftSpeed * Time.deltaTime;
        if (wrapAround && camera3d != null)
        {
            var plane = new Plane(Vector3.up, basePosition);
            float min = float.MaxValue, max = float.MinValue;
            bool projected = true;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = camera3d.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
                if (!plane.Raycast(ray, out float distance)) { projected = false; break; }
                float position = Vector3.Dot(ray.GetPoint(distance), direction);
                min = Mathf.Min(min, position); max = Mathf.Max(max, position);
            }
            float margin = Mathf.Max(rend.bounds.extents.magnitude, 1f);
            if (projected && Vector3.Dot(basePosition, direction) > max + margin)
                basePosition -= direction * (max - min + margin * 2f);
        }
        transform.position = basePosition + Vector3.up *
            (Mathf.Sin(Time.time * bobSpeed + cloudSeed * 1.71f) * bobAmplitude);
    }

    public void AtualizarAparencia()
    {
        if (rend == null) return;
        if (dayNightController == null) dayNightController = FindFirstObjectByType<DayNightController>();
        Color color = Color.white;
        float daylight = 1f;
        if (dayNightController != null && dayNightController.sunLight != null)
        {
            Light sun = dayNightController.sunLight;
            daylight = Mathf.Clamp01(sun.intensity / Mathf.Max(0.01f, peakSunIntensity));
            if (tintWithSunColor) color = Color.Lerp(Color.white, sun.color, tintStrength);
        }
        color.a = Mathf.Lerp(minOpacityAtNight, maxOpacityAtDay, daylight) * opacityMultiplier;
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(ColorID, color);
        mpb.SetFloat(SeedID, cloudSeed);
        rend.SetPropertyBlock(mpb);
    }

    void OnDisable()
    {
        if (rend == null) return;
        rend.SetPropertyBlock(null);
        rend.ResetLocalBounds();
    }
}
