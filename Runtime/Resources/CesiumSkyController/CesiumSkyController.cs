using UnityEngine;
using UnityEditor;
using CesiumForUnity;

[ExecuteInEditMode]
public class CesiumSkyController : MonoBehaviour
{

    [SerializeField]
    Transform sunLight = default;

    [SerializeField]
    bool updateOnTick = false;

    [SerializeField]
    bool updateInEditor = false;  //Todo: Make this work

    //bool checkForSunUpdates = false;

    float latitude = 0.0f;
    float longitude = 0.0f;

    [SerializeField]
    [Range(0.0f, 24.0f)]
    public float timeOfDay = 12.0f;

    [SerializeField]
    [Range(0.0f, 360.0f)]
    float northOffset = 90.0f;


    [SerializeField]
    [Range(1, 31)]
    int date = 1;

    [SerializeField]
    [Range(1, 12)]
    int month = 6;

    [SerializeField]
    int year = 2022;

    float timeZone = 0.0f;




    //[SerializeField]
    [Range(0.0f, 1.0f)]
    float groundSpaceBlend = 0.0f;

    float lastBlendValue;

    float groundBlendHeight = 2000.0f;
    float spaceBlendHeight = 800000.0f;

    Camera activeCamera;

    CesiumGlobeAnchor globeAnchor;

    void Awake()
    {
        ResolveCamera();

    }

    void LateUpdate()
    { 
        if (updateOnTick) 
        {
            UpdateSky(); 

        }
    }

    public void UpdateSky()
    {
        SetSunPosition();
        GetCameraHeight();

    }

    void ResolveCamera()
    {
        if (Application.IsPlaying(gameObject))
        {
            activeCamera = Camera.main;
            globeAnchor = activeCamera.GetComponent<CesiumGlobeAnchor>();


        }
        else if (updateInEditor)
        {
            SceneView sceneWindow = SceneView.lastActiveSceneView;
            if (sceneWindow)
            {           
                if (sceneWindow.camera != null)
                {
                    activeCamera = sceneWindow.camera;

                }
            }
        }
    }

    void SetSunPosition()
    {
        float hourToAngle = ((timeOfDay*15.0f) - 90.0f);
        Vector3 newSunRotation = new Vector3(hourToAngle, northOffset, 0);

        if (sunLight != null) {
            sunLight.transform.localEulerAngles = newSunRotation;
            Shader.SetGlobalVector("_SunDirection", -sunLight.transform.forward); 
        }
    }

    void GetCameraHeight()
    {
        if (activeCamera != null)
        {
            float camHeight;
            if (globeAnchor) camHeight = (float)globeAnchor.height;
            else camHeight = activeCamera.transform.position.y;


            if (camHeight <= groundBlendHeight)
            {
                groundSpaceBlend = 0.0f;
            }
            else if (camHeight >= spaceBlendHeight)
            {
                groundSpaceBlend = 1.0f;
            }
            else
            {
                groundSpaceBlend = (camHeight - groundBlendHeight) / (spaceBlendHeight - groundBlendHeight);
            }

            // TODO: Add a check to see if the scene is using the Cesium skybox material
            if (groundSpaceBlend != lastBlendValue)
            {
                Shader.SetGlobalFloat("_GroundSpaceBlend", groundSpaceBlend);
                lastBlendValue = groundSpaceBlend;

                Debug.Log("camera height is " + camHeight + ". Blend factor is " + groundSpaceBlend + ". Disable Check for Camera Updates on Sky Controller.");
            }
        }
        else ResolveCamera();

    }
}
