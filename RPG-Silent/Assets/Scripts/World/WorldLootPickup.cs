using System;
using System.Collections.Generic;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;

/// <summary>A scene-owned drop. Only a successful nearby manual transaction consumes it.</summary>
public sealed class WorldLootPickup : MonoBehaviour
{
    static readonly HashSet<WorldLootPickup> active = new();
    public static IEnumerable<WorldLootPickup> Active => active;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetActive() => active.Clear();
    public string PickupId { get; private set; }
    public ItemDefinition Item { get; private set; }
    public int Gold { get; private set; }
    public int Experience { get; private set; }
    public int Count { get; private set; }
    public bool IsCollected { get; private set; }
    public Color QualityColor => Item != null ? Item.QualityColor : new Color(1f, .78f, .25f);
    public string DisplayName => Item != null ? Item.DisplayName + (Count > 1 ? " ×" + Count : "") : $"金币 ×{Gold} · 经验 ×{Experience}";
    public string ColoredName => $"<color=#{ColorUtility.ToHtmlStringRGB(QualityColor)}>{DisplayName}</color>";
    public event Action<WorldLootPickup> Collected;
    Transform modelPivot, label;
    Camera viewer;
    float baseHeight;
    float nextCurrencyAttempt;
    const float AttractionRadius = 12f;
    ItemStackData payload;
    public ItemStackData Payload => payload?.Copy();
    public void SetLabelVisible(bool visible) { if (label != null) label.gameObject.SetActive(visible); }

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);
    public void Initialize(ItemDefinition item, int count, int gold, int experience, LootPresentation presentation)
    {
        var entry = item != null ? EquipmentInstances.Create(item, item.IsStackable ? null : new System.Random()) : null;
        if (entry != null) entry.count = count;
        InitializeRolled(item, entry, gold, experience, presentation);
    }
    public void InitializeRolled(ItemDefinition item, ItemStackData entry, int gold, int experience, LootPresentation presentation)
    {
        if (PickupId != null) throw new InvalidOperationException("Drop already initialized");
        if ((item != null && (!InventoryData.ValidItem(entry) || entry.itemId != item.Id)) || (item == null && entry != null)) throw new ArgumentException("掉落实例无效。");
        payload = entry?.Copy();
        PickupId = Guid.NewGuid().ToString("N"); Item = item; Count = entry?.count ?? 0; Gold = gold; Experience = experience;
        name = "Loot: " + DisplayName; viewer = Camera.main;
        BuildVisual(presentation);
    }
    public bool TryCollect(PlayerLootInteraction interaction, PlayerProgressService progress, out string message)
    {
        message = "请靠近物品后按交互键拾取。";
        if (Item == null || IsCollected || interaction == null || progress == null || !interaction.CanReach(this)) return false;
        var items = new[] { payload.Copy() };
        if (!progress.TryCollectWorldLoot(PickupId, items, Gold, Experience, out message)) return false;
        Consume(); return true;
    }
    void Consume() { IsCollected = true; active.Remove(this); Collected?.Invoke(this); gameObject.SetActive(false); Destroy(gameObject); }
    void Update()
    {
        if (Item != null || IsCollected || PickupId == null || Time.unscaledTime < nextCurrencyAttempt) return;
        PlayerLootInteraction nearest = null; float distance = AttractionRadius * AttractionRadius;
        foreach (var candidate in PlayerLootInteraction.Active)
        {
            if (!candidate.CanReceiveCurrency) continue;
            float next = (candidate.AttractionPoint - transform.position).sqrMagnitude;
            if (next < distance) { distance = next; nearest = candidate; }
        }
        if (nearest == null) return;
        transform.position = Vector3.MoveTowards(transform.position, nearest.AttractionPoint, 12f * Time.deltaTime);
        if ((nearest.AttractionPoint - transform.position).sqrMagnitude > .16f) return;
        if (nearest.ReceiveCurrency(this, out _)) Consume();
        else nextCurrencyAttempt = Time.unscaledTime + 1f;
    }
    void BuildVisual(LootPresentation presentation)
    {
        modelPivot = new GameObject("Floating item").transform; modelPivot.SetParent(transform, false);
        var prefab = Item?.Weapon != null ? Item.Weapon.ModelPrefab : Item != null ? presentation.ItemModel : presentation.CurrencyModel;
        if (prefab != null)
        {
            var model = Instantiate(prefab, modelPivot);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>()) { body.isKinematic = true; body.detectCollisions = false; }
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float extent = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                float scale = (Item?.Weapon != null ? .9f : .32f) / Mathf.Max(.001f, extent);
                model.transform.localScale *= scale; model.transform.localPosition -= modelPivot.InverseTransformPoint(bounds.center) * scale;
            }
            if (Item?.Weapon != null) model.transform.localRotation = Quaternion.Euler(0, 0, 25);
        }
        baseHeight = Item?.Weapon != null ? .7f : .35f;
        modelPivot.localPosition = Vector3.up * baseHeight;
        if (presentation.GlowPrefab != null)
        {
            var glow = Instantiate(presentation.GlowPrefab, transform); glow.name = "Quality glow"; glow.transform.localScale = Vector3.one * .4f;
            // Reuse the neutral halo/spark children; the source's cyan crystal sprite is not a loot model.
            var sourceCrystal = glow.GetComponent<Renderer>(); if (sourceCrystal != null) sourceCrystal.enabled = false;
            foreach (var particles in glow.GetComponentsInChildren<ParticleSystem>())
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main; main.startColor = QualityColor; main.maxParticles = Mathf.Min(24, main.maxParticles);
                var tint = particles.colorOverLifetime;
                if (tint.enabled) { var gradient = new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(QualityColor,0),new GradientColorKey(QualityColor,1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)}); tint.color = gradient; }
                particles.Play();
            }
        }
        var ring = new GameObject("Quality ring").AddComponent<LineRenderer>(); ring.transform.SetParent(transform, false);
        ring.sharedMaterial = presentation.GlowMaterial; ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = .035f;
        ring.startColor = ring.endColor = QualityColor; ring.positionCount = 40;
        for (int i = 0; i < 40; i++) { float angle = i * Mathf.PI * 2 / 40; ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * .42f, .04f, Mathf.Sin(angle) * .42f)); }
        var beam = new GameObject("Quality beam").AddComponent<LineRenderer>(); beam.transform.SetParent(transform, false);
        beam.sharedMaterial = presentation.GlowMaterial; beam.useWorldSpace = false; beam.positionCount = 2;
        beam.SetPosition(0, Vector3.up * .08f); beam.SetPosition(1, Vector3.up * (1.2f + (Item != null ? (int)Item.Rarity * .35f : 0)));
        beam.startWidth = .12f; beam.endWidth = .015f; beam.startColor = QualityColor;
        var faded = QualityColor; faded.a = 0; beam.endColor = faded;
        var text = new GameObject("Loot label").AddComponent<TextMeshPro>(); label = text.transform; label.SetParent(transform, false);
        label.localPosition = Vector3.up * 1.35f; text.font = presentation.Font; text.fontSize = 2.2f;
        text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(4.5f, .5f);
        text.text = Item != null && !DisplayName.StartsWith(Item.RarityLabel, StringComparison.Ordinal) ? Item.RarityLabel + " · " + DisplayName : DisplayName; text.color = QualityColor;
    }
    void LateUpdate()
    {
        if (modelPivot != null)
        {
            modelPivot.localPosition = Vector3.up * (baseHeight + Mathf.Sin(Time.time * 2f) * .06f);
            modelPivot.Rotate(Vector3.up, 28f * Time.deltaTime, Space.Self);
        }
        if (label != null && viewer != null) label.rotation = viewer.transform.rotation;
    }
}
