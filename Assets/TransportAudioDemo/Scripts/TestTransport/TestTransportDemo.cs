using System.Collections.Generic;
using UnityEngine;

public sealed class TestTransportDemo : MonoBehaviour
{
    [SerializeField] private TransportAudioProfile[] audioProfiles;

    private readonly List<PrimitiveTransportVehicle> vehicles = new List<PrimitiveTransportVehicle>();
    private readonly List<TransportEngineAudio> engines = new List<TransportEngineAudio>();
    private readonly List<string> vehicleNames = new List<string>();
    private GameObject runtimeRoot;
    private TransportDemoCamera demoCamera;
    private int activeIndex;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private const float ArenaScale = 4f;
    private const float BaseVehicleLimitX = 27f;
    private const float BaseVehicleLimitZ = 17f;

    private static readonly Vector3[] VehiclePositions =
    {
        new Vector3(-14f, 0.05f, 0f),
        new Vector3(-7f, 0.05f, 0f),
        new Vector3(0f, 0.05f, 0f),
        new Vector3(7f, 0.05f, 0f),
        new Vector3(14f, 0.05f, 0f),
        new Vector3(21f, 0.05f, 0f),
        new Vector3(28f, 0.05f, 0f),
        new Vector3(35f, 0.05f, 0f)
    };

    private static readonly Color[] VehicleColors =
    {
        new Color(0.28f, 0.42f, 0.34f),
        new Color(0.38f, 0.43f, 0.25f),
        new Color(0.25f, 0.34f, 0.23f),
        new Color(0.32f, 0.23f, 0.15f),
        new Color(0.22f, 0.25f, 0.28f),
        new Color(0.35f, 0.29f, 0.18f),
        new Color(0.18f, 0.38f, 0.42f),
        new Color(0.42f, 0.2f, 0.16f)
    };

    private void Start()
    {
        Application.targetFrameRate = 60;
        BuildArena();
        BuildVehicles();
        ConfigureCamera();
    }

    private void Update()
    {
        if (vehicles.Count == 0)
        {
            return;
        }

        HandleVehicleSelection();

        Vector2 driveInput = ReadDriveInput();
        vehicles[activeIndex].Simulate(driveInput);

        for (int i = 0; i < engines.Count; i++)
        {
            PrimitiveTransportVehicle vehicle = vehicles[i];
            float throttle = i == activeIndex ? vehicle.Throttle01 : 0f;
            engines[i].Tick(throttle, vehicle.NormalizedSpeed);
        }
    }

    private void BuildArena()
    {
        runtimeRoot = new GameObject("TEST Runtime Demo");
        CreatePrimitive("Arena Ground", PrimitiveType.Plane, runtimeRoot.transform, new Vector3(0f, 0f, 0f), Vector3.one * (6f * ArenaScale), new Color(0.14f, 0.17f, 0.18f));
        CreatePrimitive("Arena North Border", PrimitiveType.Cube, runtimeRoot.transform, new Vector3(0f, 0.45f, 19f * ArenaScale), new Vector3(58f * ArenaScale, 0.9f, 0.35f), new Color(0.08f, 0.1f, 0.11f));
        CreatePrimitive("Arena South Border", PrimitiveType.Cube, runtimeRoot.transform, new Vector3(0f, 0.45f, -19f * ArenaScale), new Vector3(58f * ArenaScale, 0.9f, 0.35f), new Color(0.08f, 0.1f, 0.11f));
        CreatePrimitive("Arena East Border", PrimitiveType.Cube, runtimeRoot.transform, new Vector3(29f * ArenaScale, 0.45f, 0f), new Vector3(0.35f, 0.9f, 38f * ArenaScale), new Color(0.08f, 0.1f, 0.11f));
        CreatePrimitive("Arena West Border", PrimitiveType.Cube, runtimeRoot.transform, new Vector3(-29f * ArenaScale, 0.45f, 0f), new Vector3(0.35f, 0.9f, 38f * ArenaScale), new Color(0.08f, 0.1f, 0.11f));

        for (int i = 0; i < Mathf.Min(audioProfiles == null ? 0 : audioProfiles.Length, VehiclePositions.Length); i++)
        {
            CreatePrimitive("Display Pad " + (i + 1), PrimitiveType.Cube, runtimeRoot.transform, VehiclePositions[i] + Vector3.down * 0.01f, new Vector3(5.6f, 0.08f, 5.2f), new Color(0.2f, 0.22f, 0.23f));
        }
    }

    private void BuildVehicles()
    {
        if (audioProfiles == null)
        {
            return;
        }

        for (int i = 0; i < audioProfiles.Length && i < VehiclePositions.Length; i++)
        {
            TransportAudioProfile profile = audioProfiles[i];
            if (profile == null)
            {
                continue;
            }

            GameObject vehicleObject = new GameObject("Transport " + profile.displayName);
            vehicleObject.transform.SetParent(runtimeRoot.transform);
            vehicleObject.transform.SetPositionAndRotation(VehiclePositions[i], Quaternion.identity);

            PrimitiveTransportVehicle vehicle = vehicleObject.AddComponent<PrimitiveTransportVehicle>();
            vehicle.Configure(profile.maxSpeed, profile.acceleration, profile.braking, profile.turnSpeed, BaseVehicleLimitX * ArenaScale, BaseVehicleLimitZ * ArenaScale);
            BuildVehicleVisual(vehicleObject.transform, i, VehicleColors[i]);

            TransportEngineAudio engine = vehicleObject.AddComponent<TransportEngineAudio>();
            engine.Initialize(profile);
            engine.SetActive(false);

            vehicles.Add(vehicle);
            engines.Add(engine);
            vehicleNames.Add(profile.displayName);
        }

        if (vehicles.Count > 0)
        {
            SelectVehicle(0);
        }
    }

    private void ConfigureCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || vehicles.Count == 0)
        {
            return;
        }

        mainCamera.fieldOfView = 58f;
        demoCamera = mainCamera.GetComponent<TransportDemoCamera>();
        if (demoCamera == null)
        {
            demoCamera = mainCamera.gameObject.AddComponent<TransportDemoCamera>();
        }

        demoCamera.SetTarget(vehicles[activeIndex].transform);
    }

    private void HandleVehicleSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SelectVehicle(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SelectVehicle(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SelectVehicle(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SelectVehicle(3);
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) SelectVehicle(4);
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) SelectVehicle(5);
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) SelectVehicle(6);
        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) SelectVehicle(7);
    }

    private void SelectVehicle(int index)
    {
        if (index < 0 || index >= vehicles.Count)
        {
            return;
        }

        for (int i = 0; i < engines.Count; i++)
        {
            engines[i].SetActive(i == index);
        }

        activeIndex = index;
        if (demoCamera != null)
        {
            demoCamera.SetTarget(vehicles[activeIndex].transform);
        }
    }

    private static Vector2 ReadDriveInput()
    {
        Vector2 input = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
        return Vector2.ClampMagnitude(input, 1f);
    }

    private void BuildVehicleVisual(Transform parent, int index, Color baseColor)
    {
        if (index < 2)
        {
            CreatePrimitive("Armored Body", PrimitiveType.Cube, parent, new Vector3(0f, 0.82f, 0f), new Vector3(2.8f, 0.9f, 4.0f), baseColor);
            CreatePrimitive("Cabin", PrimitiveType.Cube, parent, new Vector3(0f, 1.48f, -0.45f), new Vector3(2.2f, 0.95f, 1.6f), Color.Lerp(baseColor, Color.black, 0.2f));
            CreatePrimitive("Front Plate", PrimitiveType.Cube, parent, new Vector3(0f, 1.02f, 1.95f), new Vector3(2.5f, 0.35f, 0.25f), Color.Lerp(baseColor, Color.white, 0.08f));
            int wheelCount = index == 0 ? 4 : 6;
            for (int wheel = 0; wheel < wheelCount; wheel++)
            {
                float z = Mathf.Lerp(-1.45f, 1.45f, wheel / (float)(wheelCount - 1));
                CreateWheel(parent, new Vector3(-1.5f, 0.48f, z), baseColor);
                CreateWheel(parent, new Vector3(1.5f, 0.48f, z), baseColor);
            }

            return;
        }

        float bodyLength = index == 4 ? 4.8f : 4.3f;
        CreatePrimitive("Tank Hull", PrimitiveType.Cube, parent, new Vector3(0f, 0.9f, 0f), new Vector3(3.5f, 0.95f, bodyLength), baseColor);
        CreatePrimitive("Left Track", PrimitiveType.Cube, parent, new Vector3(-2f, 0.58f, 0f), new Vector3(0.75f, 0.65f, bodyLength + 0.15f), Color.Lerp(baseColor, Color.black, 0.45f));
        CreatePrimitive("Right Track", PrimitiveType.Cube, parent, new Vector3(2f, 0.58f, 0f), new Vector3(0.75f, 0.65f, bodyLength + 0.15f), Color.Lerp(baseColor, Color.black, 0.45f));
        for (int wheel = 0; wheel < 4; wheel++)
        {
            float z = Mathf.Lerp(-1.55f, 1.55f, wheel / 3f);
            CreateWheel(parent, new Vector3(-2.05f, 0.58f, z), baseColor);
            CreateWheel(parent, new Vector3(2.05f, 0.58f, z), baseColor);
        }

        PrimitiveType turretType = index == 3 ? PrimitiveType.Cylinder : PrimitiveType.Cube;
        Vector3 turretScale = index == 4 ? new Vector3(2.4f, 0.62f, 2.4f) : new Vector3(2.1f, 0.58f, 2.1f);
        CreatePrimitive("Turret", turretType, parent, new Vector3(0f, 1.7f, -0.2f), turretScale, Color.Lerp(baseColor, Color.white, 0.08f));
        CreatePrimitive("Cannon", PrimitiveType.Cube, parent, new Vector3(0f, 1.78f, bodyLength * 0.48f + 0.9f), new Vector3(0.34f, 0.34f, 2.7f), Color.Lerp(baseColor, Color.black, 0.5f));
    }

    private static void CreateWheel(Transform parent, Vector3 localPosition, Color baseColor)
    {
        CreatePrimitive("Wheel", PrimitiveType.Cylinder, parent, localPosition, new Vector3(0.62f, 0.24f, 0.62f), Color.Lerp(baseColor, Color.black, 0.65f), Quaternion.Euler(0f, 0f, 90f));
    }

    private static GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform parent, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.transform.SetParent(parent);
        primitive.transform.localPosition = position;
        primitive.transform.localRotation = rotation ?? Quaternion.identity;
        primitive.transform.localScale = scale;

        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(color);
        }

        return primitive;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.15f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.35f);
        }

        return material;
    }

    private void OnGUI()
    {
        if (vehicles.Count == 0)
        {
            return;
        }

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }

        PrimitiveTransportVehicle vehicle = vehicles[activeIndex];
        TransportEngineAudio engine = engines[activeIndex];
        GUILayout.BeginArea(new Rect(18f, 18f, 450f, 260f), GUI.skin.box);
        GUILayout.Label("TEST / TRANSPORT AUDIO DEMO", headerStyle);
        GUILayout.Label("1-8: select transport    W/S: throttle    A/D: steer", bodyStyle);
        GUILayout.Space(4f);
        GUILayout.Label("Active: " + vehicleNames[activeIndex], bodyStyle);
        GUILayout.Label(string.Format("Speed: {0:0.0} m/s    RPM: {1:0}", Mathf.Abs(vehicle.CurrentSpeed), engine.Rpm), bodyStyle);
        GUILayout.Label(string.Format("Throttle: {0:0}%    Engine layer blend: {1:0}%", vehicle.Throttle01 * 100f, engine.Rpm01 * 100f), bodyStyle);
        GUILayout.Space(4f);
        GUILayout.Label("Audio profiles:", bodyStyle);
        for (int i = 0; i < vehicleNames.Count; i++)
        {
            GUILayout.Label(string.Format("{0}: {1}{2}", i + 1, vehicleNames[i], i == activeIndex ? "  < active" : string.Empty), bodyStyle);
        }

        GUILayout.EndArea();
    }
}
