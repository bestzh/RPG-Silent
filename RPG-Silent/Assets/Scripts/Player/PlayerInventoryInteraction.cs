using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public sealed class PlayerInventoryInteraction : MonoBehaviour
{
    public const string UiKey = "UI/InventoryUI";
    [Inject] IPlayerInputActions input;
    [Inject] IUIService ui;
    [Inject] ICursorService cursor;
    [Inject] PlayerProgressService progress;
    PlayerWeaponEquipment weapons;
    PlayerController player;
    bool opened;
    float openedAt;
    public bool IsOpen => opened;
    void Awake() { weapons = GetComponent<PlayerWeaponEquipment>(); player = GetComponent<PlayerController>(); }
    void OnEnable() { if (input != null) input.InventoryTriggered += Toggle; SceneManager.activeSceneChanged += SceneChanged; }
    void OnDisable() { if (input != null) input.InventoryTriggered -= Toggle; SceneManager.activeSceneChanged -= SceneChanged; Close(); }
    void SceneChanged(Scene before, Scene after) => Close();
    bool OtherModal => ui.IsUIOpen("UI/LoadingUI") || ui.IsUIOpen("UI/PortalUI") || ui.IsUIOpen("UI/TownNpcUI") || ui.IsUIOpen("UI/PauseUI") || ui.IsUIOpen("UI/SettingsUI") || ui.IsUIOpen(PlayerQuestInteraction.UiKey);
    public void Toggle()
    {
        if (opened) { Close(); return; }
        if (input == null || !input.GameplayInputEnabled || OtherModal || !weapons.CanChangeEquipment) return;
        opened = true; openedAt = Time.unscaledTime;
        input.SetGameplayInputBlocked(this, true); cursor.EnterUICursor();
        ui.SetRaycastEnabled("UI/MainUI", false); ui.OpenUI(UiKey, this);
    }
    void Update()
    {
        if (opened && (player.IsDead || OtherModal ||
            (!ui.IsUIOpen(UiKey) && Time.unscaledTime - openedAt > 8))) Close();
    }
    public bool EquipWeapon(int slot, WeaponDefinition weapon)
    {
        if (!opened || !progress.OwnsWeapon(weapon)) return false;
        Close(); return weapons.TryEquip(slot, weapon);
    }
    public bool EquipWeaponInstance(int slot, string key)
    {
        if (!opened || !progress.CanEquipWeaponInstance(key)) return false;
        Close();
        if (weapons.TryEquipInstance(slot, key)) return true;
        Toggle(); return false;
    }
    public void Close()
    {
        if (!opened) return;
        ui.CloseUI(UiKey); OnUiClosed();
    }
    public void OnUiClosed()
    {
        if (!opened) return;
        opened = false; input.SetGameplayInputBlocked(this, false);
        if (!OtherModal) ui.SetRaycastEnabled("UI/MainUI", true);
        if (input.GameplayInputEnabled) cursor.EnterGameplayCursor(); else cursor.EnterUICursor();
    }
}
