using UnityEngine;
using VContainer;

/// <summary>Scene-owned town clock. One day takes twenty real minutes, persisted per character.</summary>
public sealed class TownDayNight : MonoBehaviour
{
    public Light Sun;
    [Min(60)] public float DayDurationSeconds = 1200;
    public TownResident[] Residents = System.Array.Empty<TownResident>();
    public float Hour { get; private set; } = 8;
    public int Day { get; private set; } = 1;
    public bool IsNight => Hour < 6 || Hour >= 20;
    PlayerProgressService progress;
    float nextUpdate, nextSave;
    Quaternion originalRotation;
    Color originalColor, originalAmbient;
    float originalIntensity, originalAmbientIntensity;
    Material originalSky, runtimeSky;
    float skyExposure;
    Color skyTint;
    void Start()
    {
        var container=GameLifetimeScope.Instance?.Container;
        if(container!=null)progress=container.Resolve<PlayerProgressService>();
        if(progress!=null){Hour=progress.TownHour;Day=progress.TownDay;}
        originalAmbient=RenderSettings.ambientLight;originalAmbientIntensity=RenderSettings.ambientIntensity;
        if(Sun!=null){originalRotation=Sun.transform.rotation;originalColor=Sun.color;originalIntensity=Sun.intensity;}
        originalSky=RenderSettings.skybox;
        if(originalSky!=null)
        {
            runtimeSky=new Material(originalSky);runtimeSky.name="Town sky (runtime)";
            if(runtimeSky.HasProperty("_Exposure"))skyExposure=runtimeSky.GetFloat("_Exposure");
            if(runtimeSky.HasProperty("_SkyTint"))skyTint=runtimeSky.GetColor("_SkyTint");
            RenderSettings.skybox=runtimeSky;
        }
        foreach(var resident in Residents)if(resident!=null)resident.Clock=this;
        nextSave=Time.time+30;ApplyLighting();
    }
    void Update()
    {
        Hour+=Time.deltaTime*24/Mathf.Max(60,DayDurationSeconds);
        if(Hour>=24){Hour-=24;Day=Mathf.Min(1000000,Day+1);}
        if(Time.time>=nextUpdate){nextUpdate=Time.time+.2f;ApplyLighting();progress?.SetTownTime(Hour,Day);}
        if(Time.time>=nextSave){nextSave=Time.time+30;progress?.Save();}
    }
    void ApplyLighting()
    {
        float daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Sin((Hour-6)/24*Mathf.PI*2)*2));
        if(Sun!=null)
        {
            float pitch=IsNight?45:Mathf.Lerp(10,170,Mathf.InverseLerp(6,20,Hour));
            Sun.transform.rotation=Quaternion.Euler(pitch,originalRotation.eulerAngles.y,0);
            Sun.intensity=Mathf.Lerp(Mathf.Max(.25f,originalIntensity*.24f),originalIntensity,daylight);
            Sun.color=Color.Lerp(new Color(.38f,.53f,.85f),originalColor,daylight);
        }
        RenderSettings.ambientLight=Color.Lerp(new Color(.21f,.28f,.43f),originalAmbient,daylight);
        RenderSettings.ambientIntensity=Mathf.Lerp(Mathf.Max(.35f,originalAmbientIntensity*.4f),originalAmbientIntensity,daylight);
        if(runtimeSky!=null)
        {
            if(runtimeSky.HasProperty("_Exposure"))runtimeSky.SetFloat("_Exposure",Mathf.Lerp(.12f,skyExposure,daylight));
            if(runtimeSky.HasProperty("_SkyTint"))runtimeSky.SetColor("_SkyTint",Color.Lerp(new Color(.06f,.08f,.19f),skyTint,daylight));
        }
    }
    void OnEnable() { if(runtimeSky!=null)RenderSettings.skybox=runtimeSky; }
    void OnDisable()
    {
        if(progress==null)return;
        progress.SetTownTime(Hour,Day);
        if(Sun!=null){Sun.transform.rotation=originalRotation;Sun.color=originalColor;Sun.intensity=originalIntensity;}
        if (gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene())
        { RenderSettings.ambientLight=originalAmbient;RenderSettings.ambientIntensity=originalAmbientIntensity; }
        if(RenderSettings.skybox==runtimeSky)RenderSettings.skybox=originalSky;
    }
    void OnDestroy() { if(runtimeSky!=null)Destroy(runtimeSky); }
}
