using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    Button storyTab, craftingTab, recipeTab, reforgeTab;
    bool storyPage, craftingPage, reforgePage;
    int dialoguePage;
    string dialogueConfirmation, reforgeTarget, reforgeExpected, selectedStory;
    TownDialogueDefinition CurrentDialogue => progress.DialoguesFor(interaction.Conversation).FirstOrDefault(s => s.Id == selectedStory) ?? progress.DialogueFor(interaction.Conversation);

    bool ExtraNpcPage => storyPage || craftingPage;

    void BuildWorldTabs()
    {
        storyTab = UIView.Button(panel, "StoryTab", "交谈 / 故事", () =>
        {
            storyPage = true;
            craftingPage = questPage = supplyPage = false;
            dialoguePage = 0;
            dialogueConfirmation = selectedStory = null;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
        craftingTab = UIView.Button(panel, "CraftingTab", "锻造 / 重铸", () =>
        {
            craftingPage = true;
            storyPage = questPage = supplyPage = false;
            reforgeTarget = null;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
        recipeTab = UIView.Button(panel, "RecipeTab", "装备配方", () =>
        {
            reforgePage = false;
            reforgeTarget = null;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
        reforgeTab = UIView.Button(panel, "ReforgeTab", "词条重铸", () =>
        {
            reforgePage = true;
            reforgeTarget = null;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
    }

    void ResetWorldPages()
    {
        storyPage = craftingPage = reforgePage = false;
        dialoguePage = 0;
        selectedStory = dialogueConfirmation = reforgeTarget = reforgeExpected = null;
    }

    bool HandleWorldBack()
    {
        if (storyPage && dialogueConfirmation != null)
            dialogueConfirmation = null;
        else if (craftingPage && reforgePage && reforgeTarget != null)
            reforgeTarget = reforgeExpected = null;
        else if (!questPage && !supplyPage && !ExtraNpcPage && !specializationPage && advanceConfirmation != null)
            advanceConfirmation = null;
        else
            return false;
        status.text = "已取消确认，角色、物品和金币保持原状。";
        Refresh();
        return true;
    }

    void RefreshWorldTabs()
    {
        var npc = interaction.Conversation;
        storyTab.gameObject.SetActive(progress.DialogueFor(npc) != null);
        craftingTab.gameObject.SetActive(npc.Role == TownNpcRole.Blacksmith);
        storyTab.image.color = storyPage ? W.Accent : W.Card;
        craftingTab.image.color = craftingPage ? W.Accent : W.Card;
        recipeTab.gameObject.SetActive(craftingPage);
        reforgeTab.gameObject.SetActive(craftingPage);
        recipeTab.image.color = reforgePage ? W.Card : W.Accent;
        reforgeTab.image.color = reforgePage ? W.Accent : W.Card;
        if (ExtraNpcPage)
            serviceTab.image.color = W.Card;
        if (storyPage)
            details.text = $"好感度 {progress.NpcAffinity(npc.Role)} / 100 · 决定会随角色保存\n" + progress.StoryRequirementText(CurrentDialogue);
        if (craftingPage)
            details.text = reforgePage ? "随机重铸全部词条，数值可能升高或降低。白色装备无随机词条。\n强化、宝石、品质、模型和穿戴状态保留；锁定装备需先解锁。" : "使用已有装备与锻造材料制作。提交对应任务后永久解锁配方。\n成品带独立随机词条；背包满时进入待领取。";
    }

    float AddStoryRows(float y)
    {
        var story = CurrentDialogue;
        if (story == null)
            return 0;
        var allStories = progress.DialoguesFor(interaction.Conversation).ToArray();
        if (allStories.Length > 1)
        {
            foreach (var entry in allStories)
            {
                var option = entry;
                var choice = AddRow(option.Title);
                choice.SetSelected(option == story);
                choice.Select(() =>
                {
                    selectedStory = option.Id;
                    dialoguePage = 0;
                    dialogueConfirmation = null;
                    rows.anchoredPosition = Vector2.zero;
                    Refresh();
                });
            }
        }

        string chosen = progress.StoryChoice(story.Id);
        if (!progress.StoryUnlocked(story))
        {
            AddRow(story.Title, "这段后续故事尚未开放。\n" + progress.StoryRequirementText(story));
            return 0;
        }

        if (chosen.Length > 0)
        {
            var choice = story.Choices.First(x => x.Id == chosen);
            AddRow(story.Title, "已选择：" + choice.Label + "\n\n" + choice.Outcome + "\n\n决定已保存，可在任务页接取后续委托。");
            return 0;
        }

        var page = AddRow(story.Title + $" · {dialoguePage + 1}/{story.Pages.Length}", story.Pages[dialoguePage]);
        if (dialoguePage < story.Pages.Length - 1)
        {
            page.Action(0, "继续交谈", () =>
            {
                dialoguePage++;
                Refresh();
            });
            return 0;
        }

        foreach (var entry in story.Choices)
        {
            var choice = entry;
            var row = AddRow(choice.Label);
            row.SetSelected(dialogueConfirmation == choice.Id);
            row.Select(() =>
            {
                dialogueConfirmation = choice.Id;
                Refresh();
            });
        }

        if (dialogueConfirmation != null)
        {
            var choice = story.Choices.First(x => x.Id == dialogueConfirmation);
            var review = AddRow("确认这个决定？", choice.Outcome + $"\n金币 +{choice.Gold} · 经验 +{choice.Experience} · 好感 +{choice.Affinity}");
            review.Action(0, "确认决定", () =>
            {
                if (Validate(story.Role))
                {
                    progress.TryChooseDialogue(story.Id, choice.Id, interaction.Conversation, out string message);
                    status.text = message;
                    Refresh();
                }
            });
            review.Action(1, "重新考虑", () =>
            {
                dialogueConfirmation = null;
                Refresh();
            });
        }

        return 0;
    }

    static string Affixes(ItemStackData e) => e.affixes.Length == 0 ? "暂无随机词条" : string.Join(" · ", e.affixes.Select(a => (a.stat == ItemAffixStat.Health ? "生命" : a.stat == ItemAffixStat.Energy ? "能量" : a.stat == ItemAffixStat.AttackPercent ? "伤害" : "减伤") + " +" + a.value + (a.stat >= ItemAffixStat.AttackPercent ? "%" : "")));
    float AddCraftingRows(float y)
    {
        var inventory = progress.Inventory;
        if (!reforgePage)
        {
            foreach (var recipe in progress.Recipes)
            {
                var r = recipe;
                bool unlocked = progress.RecipeUnlocked(r);
                var row = AddRow($"{r.Result.DisplayName} · {r.Result.RarityLabel} · Lv.{r.Result.RequiredLevel}", $"{r.Gold} 金币 · 锻造碎片 ×{r.ForgeShards} · 晶核碎片 ×{r.CrystalShards}\n" + (unlocked ? "配方已解锁" : "先提交：" + progress.FindQuest(r.UnlockQuestId)?.Title), r.Result.Icon);
                row.Title.color = r.Result.QualityColor;
                row.Action(0, unlocked ? "锻造一件" : "配方未解锁", () =>
                {
                    if (Validate(TownNpcRole.Blacksmith))
                    {
                        progress.TryCraftEquipment(r.Id, interaction.Conversation, out string message);
                        status.text = message;
                        Refresh();
                    }
                }, unlocked && progress.Gold >= r.Gold && inventory.Count("forge-shard") >= r.ForgeShards && inventory.Count("crystal-shard") >= r.CrystalShards);
            }

            return 0;
        }

        if (reforgeTarget != null)
        {
            var entry = progress.FindInstance(reforgeTarget);
            var item = progress.FindItem(reforgeTarget);
            if (entry == null)
            {
                reforgeTarget = null;
                return AddCraftingRows(0);
            }

            var row = AddRow($"{item.DisplayName} +{entry.upgradeLevel} · 重铸核对", "当前：" + Affixes(entry) + $"\n将随机重铸全部 {EquipmentInstances.AffixCount(item.Rarity)} 条词条，结果可能变弱。\n{progress.ReforgeGold(entry.Key)} 金币 · 锻造碎片 ×{progress.ReforgeShards(entry.Key)} · 晶核碎片 ×1\n品质、强化、宝石与穿戴状态保留。", item.Icon);
            row.Title.color = item.QualityColor;
            row.Action(0, "确认消耗并重铸", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                {
                    progress.TryReforge(reforgeTarget, reforgeExpected, interaction.Conversation, out string message);
                    status.text = message;
                    reforgeTarget = null;
                    Refresh();
                }
            });
            row.Action(1, "取消", () =>
            {
                reforgeTarget = null;
                Refresh();
            });
            return 0;
        }

        var entries = inventory.bag.Where(e => e.IsInstance && !e.legacyReserve && progress.FindItem(e.itemId)?.Rarity > ItemRarity.Common).ToArray();
        if (entries.Length == 0)
            AddRow("暂无可重铸装备", "普通品质和堆叠物品不可重铸，锁定装备需先解锁。");
        foreach (var entry in entries)
        {
            string key = entry.Key;
            var item = progress.FindItem(key);
            var row = AddRow($"{item.DisplayName} +{entry.upgradeLevel}" + (entry.isLocked ? " · 已锁定" : ""), Affixes(entry) + $"\n{progress.ReforgeGold(key)} 金币 · 锻造碎片 ×{progress.ReforgeShards(key)} · 晶核碎片 ×1", item.Icon);
            row.Title.color = item.QualityColor;
            row.Action(0, "查看并核对重铸", () =>
            {
                reforgeTarget = key;
                reforgeExpected = progress.ReforgeQuote(key);
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            }, !entry.isLocked);
        }

        return 0;
    }
}
