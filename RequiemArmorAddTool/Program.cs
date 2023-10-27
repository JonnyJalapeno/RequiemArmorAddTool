using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins;
using Noggog;
using System.Security.Policy;
using System.Collections.Generic;
using DynamicData;
using Mutagen.Bethesda.Plugins.Order;
using System.Collections;
using System.Linq;
using System.IO;

namespace RequiemArmorAddTool
{
    public class ArmorValues {
        private string? bodypart;
        private string? armorclass;
        private float? armorvalue;
        private int count = 0;
        public string? BodyPart {
            get { return bodypart; }
            set { bodypart = value; count++; }
        }
        public string? ArmorClass{
            get { return armorclass; }
            set { armorclass = value; count++; }
        }
        public float? ArmorValue {
            get => armorvalue;
            set => armorvalue = value;
        }
        public int Count { 
            get => count;
        }
    }
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static bool CheckIfAlreadyExist(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, FormKey key) {

            bool check = false;      
            foreach (var item in state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()) {
                if (item.Entries != null)
                {
                    foreach (var entry in item.Entries)
                    {
                        if (entry.Data != null && entry.Data.Reference.FormKey == key) {
                            return check;
                        }
                    }
                }
            }
            return check;
        }

        public static HashSet<FormKey> PopulateArmorList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {
            string LeveledListPrefixName = "REQ_CLI_EquipSet_Bandit";
            string LeveledListPrefixNameDragonborn = "FZR_CLI_EquipSet_Bandit";
            HashSet<FormKey> tempArmors = new HashSet<FormKey>();
            foreach (var armor in state.LoadOrder.PriorityOrder.Armor().WinningOverrides()) {
                if (armor.BodyTemplate != null && armor.TemplateArmor.IsNull) {
                    if (armor.BodyTemplate.ArmorType != ArmorType.Clothing) tempArmors.Add(armor.FormKey);
                }
            }
            HashSet<FormKey> result = new HashSet<FormKey>();
            foreach (var lvllist in state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()) {
                if(lvllist.Entries != null && lvllist.EditorID != null && (lvllist.EditorID.Contains(LeveledListPrefixName) || lvllist.EditorID.Contains(LeveledListPrefixNameDragonborn)))
                {
                    foreach (var entry in lvllist.Entries) {
                        if (entry.Data != null && tempArmors.Contains(entry.Data.Reference.FormKey) && !result.Contains(entry.Data.Reference.FormKey)) {
                            result.Add(entry.Data.Reference.FormKey);
                        }
                    }
                }
            }
            return result;
        }

        public static ArmorValues? CheckArmorValues(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IArmorGetter armor)
        {
            //List<string> list = new List<string>();
            ArmorValues armorValues = new ArmorValues();
            if (armor.Keywords != null)
            {
                foreach (var keyword in armor.Keywords)
                {
                    if (keyword != null && armorValues.Count != 2)
                    {
                        keyword.TryResolveIdentifier(state.LinkCache, out var identifier);
                        if (identifier != null)
                        {
                            if (identifier.Contains("Light"))
                            {
                                armorValues.ArmorClass = "Light";
                            }
                            if (identifier.Contains("Heavy"))
                            {
                                armorValues.ArmorClass = "Heavy";
                            }
                            if (identifier.Contains("Helmet"))
                            {
                                armorValues.BodyPart = "Helmet";
                            }
                            if (identifier.Contains("Boots"))
                            {
                                armorValues.BodyPart = "Boots";
                            }
                            if (identifier.Contains("Cuirass"))
                            {
                                armorValues.BodyPart = "Cuirass";
                            }
                            if (identifier.Contains("Gauntlets"))
                            {
                                armorValues.BodyPart = "Gauntlets";
                            }
                        }
                    }
                    else {
                        break;
                    }
                }
            }
            if (armorValues.Count == 2) {
                armorValues.ArmorValue = armor.ArmorRating;
                return armorValues;
            }
            else {
                return null;
            }
        }

        public static void AddToList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IArmorGetter armor) {
            string LeveledListPrefixName = "REQ_CLI_EquipSet_Bandit";
            string LeveledListPrefixNameDragonborn = "FZR_CLI_EquipSet_Bandit";

            foreach (var lvln in state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides()) { 
                if(lvln != null && lvln.EditorID != null && (lvln.EditorID.Contains(LeveledListPrefixName) || lvln.EditorID.Contains(LeveledListPrefixNameDragonborn)))
                {
                    if(lvln.Entries != null)
                    {
                        List<LeveledItemEntry?> list = new List<LeveledItemEntry?>();
                        foreach(var entry in lvln.Entries)
                        {
                            LeveledItemEntry entry1 = new LeveledItemEntry();
                            LeveledItemEntryData data = new LeveledItemEntryData();
                            if(entry != null && entry.Data != null)
                            {
                                var count = entry.Data.Count;
                                var level = entry.Data.Level;
                                var refer = entry.Data.Reference;
                                data.Count = count;
                                data.Level = level;
                                data.Reference.SetTo(refer);
                                entry1.Data = data;
                                list.Add(entry1);
                            } 
                        }
                        LeveledItemEntry entry2 = new LeveledItemEntry();
                        LeveledItemEntryData data2 = new LeveledItemEntryData();
                        short count2 = 1;
                        short level2 = 1;
                        var refer2 = armor.FormKey;
                        data2.Level = level2;
                        data2.Reference.SetTo(refer2);
                        data2.Count = count2;
                        entry2.Data = data2;
                        list.Add(entry2);
                    }
                }

            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {          
            var result = PopulateArmorList(state);
            var test = state.LoadOrder.PriorityOrder.Where(x => x.FileName.Contains("Requiem")).Select(x => x.ModKey).ToArray();
            var test2 = state.LoadOrder.Count;
            ModKey key = new ModKey();
            ModKey key2 = new ModKey();
            foreach (var testItem in test)
            {
                if (testItem != null)
                {
                    if (testItem.FileName == "Requiem for the Indifferent.esp")
                    {
                        key = testItem;
                    }
                    else
                    {
                        key2 = testItem; break;
                    }
                }
            }
            int index = state.LoadOrder.IndexOf(key);
            int index2 = state.LoadOrder.IndexOf(key2);
            LoadOrder<ISkyrimModGetter> listing = new LoadOrder<ISkyrimModGetter>();
            for (int i = index2+1; i < index; i++) {
                var obj = state.LoadOrder.TryGetAtIndex(i);
                if(obj != null && obj.Mod != null)
                {
                    listing.Add(obj.Mod);
                }
            }          
            foreach (var armor in listing.ListedOrder.Armor().WinningOverrides()) { 
                if(armor != null) {
                    var armorval = CheckArmorValues(state, armor);
                    if (!result.Contains(armor.FormKey) && armorval != null) {
                        AddToList(state, armor);
                    }
                }
            }
        }
    }
}
