using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>A presentation-only rig: no player input, combat components or save access.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class CharacterModelPreview : MonoBehaviour, IDragHandler, IScrollHandler
{
    const int PreviewLayer = 31;
    [SerializeField] Color background = Color.clear;
    GameObject stage, model, sourcePrefab, weaponModel;
    Animator animator;
    ModularArmorAppearance armor;
    WeaponDefinition shownWeapon;
    AnimationClip shownIdle;
    bool shownSheathed;
    Camera previewCamera;
    RenderTexture texture;
    PlayableGraph animationGraph;
    RawImage image;
    float yaw = 165f, distance = 4.1f;
    float framedDistance = 4.1f, targetHeight = 1.05f;
    public GameObject Model => model;
    public PlayerClassDefinition Definition { get; private set; }
    public PlayerGender Gender { get; private set; }

    public void Show(PlayerClassDefinition definition, PlayerGender gender)
        => ShowPreview(definition, gender, System.Array.Empty<ItemDefinition>(), definition?.StartingWeapon, false, definition?.PreviewIdle, true);

    public void ShowLoadout(PlayerClassDefinition definition, PlayerGender gender, IReadOnlyList<ItemDefinition> equipped,
        WeaponDefinition weapon, bool sheathed, AnimationClip idle)
        => ShowPreview(definition, gender, equipped, weapon, sheathed, idle, false);

    void ShowPreview(PlayerClassDefinition definition, PlayerGender gender, IReadOnlyList<ItemDefinition> equipped,
        WeaponDefinition weapon, bool sheathed, AnimationClip idle, bool resetView)
    {
        if (definition == null) { Hide(); return; }
        var visualGender = definition.GetVisualGender(gender, equipped.Any(item => item != null && item.Appearance != null));
        var prefab = definition.GetModel(visualGender);
        if (prefab == null) { Hide(); return; }
        bool reset = resetView || model == null || Definition != definition || Gender != gender;
        float zoom = distance / Mathf.Max(.01f, framedDistance);
        if (model == null || sourcePrefab != prefab)
        {
            ClearModel(); EnsureStage(); sourcePrefab = prefab;
            model = Instantiate(prefab, stage.transform);
            model.name = "Preview - " + definition.Id + " - " + gender;
            model.transform.localPosition = Vector3.zero; model.transform.localScale = Vector3.one; model.SetActive(true);
            animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = null; animator.applyRootMotion = false;
            animator.fireEvents = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var rig = model.GetComponent<CharacterRigProfile>();
            if (rig != null && !string.IsNullOrEmpty(rig.ArmorMeshRoot))
                armor = new ModularArmorAppearance(model.transform, rig, visualGender);
        }
        Definition = definition; Gender = gender;
        armor?.Apply(equipped);
        if (shownIdle != idle)
        {
            if (animationGraph.IsValid()) animationGraph.Destroy();
            shownIdle = idle;
            if (idle != null)
            {
                animationGraph = PlayableGraph.Create("Character preview idle");
                animationGraph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
                var output = AnimationPlayableOutput.Create(animationGraph, "Idle", animator);
                output.SetSourcePlayable(AnimationClipPlayable.Create(animationGraph, idle));
                animationGraph.Play(); animationGraph.Evaluate(0);
            }
        }
        if (shownWeapon != weapon || shownSheathed != sheathed)
        {
            if (weaponModel != null) { weaponModel.SetActive(false); Release(weaponModel); }
            weaponModel = null; shownWeapon = weapon; shownSheathed = sheathed;
            var profile = model.GetComponent<CharacterRigProfile>();
            if (weapon != null && weapon.ModelPrefab != null && animator.isHuman)
            {
                var bone = animator.GetBoneTransform(sheathed ? weapon.SheathBone : weapon.HandBone);
                if (bone != null)
                {
                    weaponModel = Instantiate(weapon.ModelPrefab, bone);
                    weaponModel.transform.localPosition = profile != null ? profile.MountPosition(weapon, !sheathed) : sheathed ? weapon.SheathPosition : weapon.HandPosition;
                    weaponModel.transform.localRotation = profile != null ? profile.MountRotation(weapon, !sheathed) : Quaternion.Euler(sheathed ? weapon.SheathRotation : weapon.HandRotation);
                    weaponModel.transform.localScale = profile != null ? profile.Scale(weapon.ModelScale) : weapon.ModelScale;
                }
            }
        }
        foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer;
        foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
        FrameModel();
        if (reset) ResetView();
        else { distance = framedDistance * zoom; UpdateView(); }
    }

    void FrameModel()
    {
        if (animator != null && animator.isHuman)
        {
            float top = animator.GetBoneTransform(HumanBodyBones.Head).position.y - stage.transform.position.y + .3f;
            float bottom = Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,
                animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y) - stage.transform.position.y - .1f;
            float radius = .65f;
            foreach (var propRenderer in model.GetComponentsInChildren<MeshRenderer>())
            {
                if (!propRenderer.gameObject.activeInHierarchy) continue;
                top = Mathf.Max(top, propRenderer.bounds.max.y - stage.transform.position.y + .1f);
                bottom = Mathf.Min(bottom, propRenderer.bounds.min.y - stage.transform.position.y - .1f);
                var center = propRenderer.bounds.center - stage.transform.position;
                radius = Mathf.Max(radius, new Vector2(center.x, center.z).magnitude +
                    new Vector2(propRenderer.bounds.extents.x, propRenderer.bounds.extents.z).magnitude);
            }
            targetHeight = (top + bottom) * .5f;
            var rect = ((RectTransform)transform).rect;
            float aspect = rect.width / Mathf.Max(1, rect.height);
            framedDistance = Mathf.Max(3f, (top - bottom) * 2.2f,
                radius * 1.05f / (Mathf.Tan(previewCamera.fieldOfView * Mathf.Deg2Rad * .5f) * Mathf.Max(.25f, aspect)));
        }
    }

    void EnsureStage()
    {
        if (stage != null) return;
        image = GetComponent<RawImage>();
        stage = new GameObject("Character creation preview") { hideFlags = HideFlags.DontSave };
        // Outside playable scenes and every gameplay camera's far clip; no gameplay layer is reused.
        stage.transform.position = new Vector3(0, -10000, 0);
        var cameraObject = new GameObject("Character preview camera");
        cameraObject.transform.SetParent(stage.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = background;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.nearClipPlane = .05f; previewCamera.farClipPlane = 12f;
        previewCamera.fieldOfView = 34f;
        previewCamera.allowHDR = false; previewCamera.allowMSAA = false;
        var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false; data.renderShadows = false;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        texture = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32)
            { name = "Character creation portrait", hideFlags = HideFlags.DontSave, antiAliasing = 1 };
        texture.Create(); previewCamera.targetTexture = texture;
        image.texture = texture;
        AddLight("Warm key", new Vector3(-2, 3, -3), new Color(1, .87f, .69f), 28);
        AddLight("Cool fill", new Vector3(2, 2, -2), new Color(.59f, .79f, 1), 18);
        AddLight("Rim", new Vector3(0, 2, 2), new Color(.6f, .86f, 1), 22);
    }
    void AddLight(string name, Vector3 position, Color color, float intensity)
    {
        var go = new GameObject(name); go.transform.SetParent(stage.transform, false);
        go.transform.localPosition = position;
        var light = go.AddComponent<Light>(); light.type = LightType.Point;
        light.color = color; light.intensity = intensity; light.range = 8;
        light.cullingMask = 1 << PreviewLayer; light.shadows = LightShadows.None;
    }
    public void ResetView() { yaw = 165f; distance = framedDistance; UpdateView(); }
    public void Rotate(float degrees) { yaw = Mathf.Repeat(yaw + degrees, 360); UpdateView(); }
    public void OnDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            Rotate(-e.delta.x * .35f / Mathf.Max(.01f, GetComponentInParent<Canvas>()?.scaleFactor ?? 1f));
    }
    public void OnScroll(PointerEventData e) { distance = Mathf.Clamp(distance - e.scrollDelta.y * .16f, framedDistance * .7f, framedDistance * 1.2f); UpdateView(); }
    void UpdateView()
    {
        if (model == null || previewCamera == null) return;
        model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        previewCamera.transform.localPosition = new Vector3(0, targetHeight, -distance);
        previewCamera.transform.LookAt(stage.transform.position + new Vector3(0, targetHeight, 0));
    }
    void LateUpdate()
    {
        if (previewCamera == null) return;
        // Match the UI rectangle so the model never stretches when the window changes size.
        var rect = ((RectTransform)transform).rect;
        previewCamera.aspect = rect.width / Mathf.Max(1, rect.height);
        if (texture != null && Mathf.Abs((float)texture.width / texture.height - previewCamera.aspect) > .02f)
        {
            previewCamera.targetTexture = null; texture.Release();
            texture.width = Mathf.Clamp(Mathf.RoundToInt(1024 * previewCamera.aspect), 256, 1536);
            texture.Create(); previewCamera.targetTexture = texture;
        }
    }
    void ClearModel()
    {
        if (animationGraph.IsValid()) animationGraph.Destroy();
        armor?.Dispose(); armor = null;
        if (model != null) { model.SetActive(false); Release(model); model = null; }
        sourcePrefab = weaponModel = null; shownWeapon = null; shownIdle = null; animator = null;
    }
    public void Hide()
    {
        ClearModel();
        if (stage != null) { stage.SetActive(false); Release(stage); stage = null; }
        if (image != null) image.texture = null;
        if (texture != null) { texture.Release(); Release(texture); texture = null; }
        previewCamera = null;
    }
    static void Release(Object target) { if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); }
    void OnDisable() => Hide();
}
