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
using static Mutagen.Bethesda.Skyrim.Furniture;
using System.Globalization;
using Mutagen.Bethesda.Skyrim.Assets;
using System.Security.Cryptography;
using Mutagen.Bethesda.Strings;
using System.Reflection.Metadata.Ecma335;

namespace RequiemArmorAddTool
{
    public class ArmorValues {
        private string bodypart;
        private string armorclass;
        private string armormmaterial;
        private int armorvalue;
        private FormKey formkey;

        public ArmorValues(string bodypart, string armorclass, string armormaterial, int armorvalue, FormKey formkey) {
            this.bodypart = bodypart ?? throw new ArgumentException();
            this.armorclass = armorclass ?? throw new ArgumentException();
            this.armormmaterial = armormaterial ?? throw new ArgumentException();
            this.armorvalue = armorvalue;
            this.formkey = formkey;
        }

        public string BodyPart {
            get => bodypart ?? throw new ArgumentNullException();
            set { bodypart = value ?? throw new ArgumentNullException();}
        }
        public string ArmorClass {
            get => armorclass ?? throw new ArgumentNullException();
            set { armorclass = value ?? throw new ArgumentNullException();}
        }
        public string ArmorMaterial {
            get => armormmaterial ?? throw new ArgumentNullException();
            set { armormmaterial = value ?? throw new ArgumentNullException();}
        }
        public int ArmorValue {
            get {
                if (armorvalue != 0)
                {
                    return armorvalue;
                }
                else {
                    throw new ArgumentException("Armor value not set");
                }
            }
            set {
                if (value != 0)
                {
                    armorvalue = value;
                }
                else {
                    throw new ArgumentException("Armor value set to zero");
                }
            }
        }
        public FormKey FormKey {
            get {
                if (formkey != null)
                {
                    return formkey;
                }
                else {
                    throw new ArgumentException("FormKey not set");
                }
            }
            set
            {
                if (value != null)
                {
                    formkey = value;
                }
                else
                {
                    throw new ArgumentException("Fo");
                }
            }
        }
    }

    public class ArmorIncrement {
        
        private string bodypart;
        private string armorclass;
        private List<int> brackets;

        public string BodyPart
        {
            get => bodypart ?? throw new ArgumentNullException();
            set => bodypart = value ?? throw new ArgumentNullException();
        }
        public string ArmorClass
        {
            get => armorclass ?? throw new ArgumentNullException();
            set => armorclass = value ?? throw new ArgumentNullException();
        }
        public List<int> Brackets
        {
            get { return brackets ?? throw new ArgumentNullException(); }
            set => brackets = value ?? throw new ArgumentNullException();
        }
        public ArmorIncrement(string body, string armo, List<int> dd)
        {
            bodypart = body ?? throw new ArgumentNullException();
            armorclass = armo ?? throw new ArgumentNullException();
            brackets = dd ?? throw new ArgumentNullException();

        }
    }

    public class ArmorEnchantments : ArmorValues {

        private string enchantment;
        private short enchlvl = 1;
        private FormKey enchkey;
        private string enchname;

        public ArmorEnchantments(string bodypart, string armorclass, string armormaterial, int armorvalue, FormKey formkey, string enchantment, FormKey enchkey, string enchname) : base(bodypart, armorclass, armormaterial, armorvalue, formkey) {
            this.enchantment = enchantment ?? throw new ArgumentNullException();
            this.enchkey = enchkey;
            this.enchname = enchname;
        }
        public string? Enchantment
        {
            get => enchantment ?? throw new ArgumentException();
            set => enchantment = value ?? throw new ArgumentException();
        }

        public short EnchLvl
        {
            get
            {
                if (enchlvl != null)
                {
                    return enchlvl;
                }
                else
                {
                    throw new ArgumentException("Enchantment level not set");
                }
            }
            set
            {
                if (value != null)
                {
                    enchlvl = value;
                }
                else
                {
                    throw new ArgumentException("Enchantment level value set to null");
                }
            }
        }
        public FormKey EnchKey {
            get
            {
                if (enchkey != null)
                {
                    return enchkey;
                }
                else
                {
                    throw new ArgumentException("Enchantment level not set");
                }
            }
            set
            {
                if (value != null)
                {
                    enchkey = value;
                }
                else
                {
                    throw new ArgumentException("Enchantment level value set to null");
                }
            }

        }
        public string EnchName {
            get { return enchname; }
            set { enchname = value; }
        }
    }

    public static class ObjectUtility {
        public static bool ArePropertiesNotNull<T>(this T obj)
        {
            return typeof(T).GetProperties().All(propertyInfo => propertyInfo.GetValue(obj) != null);
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

        /*public static bool CheckIfAlreadyExist(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, FormKey key) {

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
        }*/

        public static List<ArmorEnchantments> FillEnchantments(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            List<ArmorEnchantments> armList = new List<ArmorEnchantments>();
            foreach (var armor in state.LoadOrder.PriorityOrder.Armor().WinningOverrides()){
                if (armor != null && !armor.TemplateArmor.IsNull && armor.EditorID != null && !armor.EditorID.Contains("NULL"))
                {
                    ArmorValues? armval = CheckArmorValues(state, armor);
                    if(armval != null)
                    {                            
                        armor.ObjectEffect.TryResolve<IObjectEffectGetter>(state.LinkCache, out var name);
                        if(name != null && name.EditorID != null && name.EditorID.Contains("Ench") && name.Name != null && name.Name.String != null) {      
                            armor.TemplateArmor.TryResolve<IArmorGetter>(state.LinkCache, out var tmpltarmor);
                            if (tmpltarmor != null && tmpltarmor.Name != null) {
                                    
                                string min = name.EditorID.Substring(name.EditorID.Length - 1);
                                if (min == "1" || min == "2" || min == "3" || min == "4" || min == "5" || min == "6")
                                {
                                    TranslatedString rr = new TranslatedString(Language.English);
                                    rr = tmpltarmor.Name.String;
                                    TranslatedString ee = new TranslatedString(Language.English);
                                    if(armor.Name != null)
                                    {
                                        ee = armor.Name.String;
                                    }
                                    int length = rr.ToString().Length;
                                    if (length > ee.ToString().Length) {
                                        continue;
                                    }
                                    string? dd = ee.ToString().Substring(length);
                                    ArmorEnchantments armench = new ArmorEnchantments(armval.BodyPart, armval.ArmorClass, armval.ArmorMaterial, armval.ArmorValue, armval.FormKey, name.EditorID, name.FormKey, dd);
                                    short min2 = short.Parse(min);
                                    armench.EnchLvl = min2;
                                    if (armList.Count == 0)
                                    {
                                        armList.Add(armench);
                                    }
                                    else
                                    {
                                        var test = armList.Where(x => x.BodyPart == armench.BodyPart && x.ArmorClass == armench.ArmorClass && x.ArmorMaterial == armench.ArmorMaterial && x.Enchantment == armench.Enchantment && x.EnchLvl == armench.EnchLvl).SingleOrDefault();
                                        if (test == null)
                                        {
                                            armList.Add(armench);
                                        }
                                    }
                                }
                                else
                                {
                                    continue;
                                }   
                            } 
                                                    
                        }   
                    }
                }
            }
            return armList;
        }

        //Summary
        //Iterate over armors discarding all the clothing, and then iterate over leveled item list to check if said armors are on the list, if they are populate hash list with armors on the list
        /*public static HashSet<FormKey> PopulateArmorList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {
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
        }*/

        public static void AddToLeveledItemList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ArmorValues armvalues) {
            string LeveledListPrefixName = "REQ_CLI_EquipSet_Bandit" + armvalues.ArmorClass;
            string LeveledListPrefixNameDragonborn = "FZR_CLI_EquipSet_Bandit" + armvalues.ArmorClass;

            //Dictionary<string, List<int>> keyValuePairs = new Dictionary<string, List<int>>();

            List<ArmorIncrement> keyValuePairs = new List<ArmorIncrement>
            {
                new ArmorIncrement("Helmet", "Light", new List<int> { 3, 6, 9, 12, 15, 18 }),
                new ArmorIncrement("Helmet", "Heavy", new List<int> { 6, 12, 18, 24, 30, 36 }),
                new ArmorIncrement("Cuirass", "Light", new List<int> { 15, 30, 45, 60, 75, 90 }),
                new ArmorIncrement("Cuirass", "Heavy", new List<int> { 40, 80, 120, 160, 200, 240 }),
                new ArmorIncrement("Gauntlets", "Light", new List<int> { 3, 5, 7, 9, 11, 13 }),
                new ArmorIncrement("Gauntlets", "Heavy", new List<int> { 5, 10, 15, 20, 25, 30 }),
                new ArmorIncrement("Boots", "Light", new List<int> { 3, 5, 7, 9, 11, 13 }),
                new ArmorIncrement("Boots", "Heavy", new List<int> { 5, 10, 15, 20, 25, 30 })
            };

            List<string> lvliNames = new List<string>();
            LeveledItemEntryData lvlData = new LeveledItemEntryData();
            LeveledItemEntry lvlEnt = new LeveledItemEntry();
            lvlData.Level = 1;
            lvlData.Reference.SetTo(armvalues.FormKey);

            var lvlin2 = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x => x.EditorID != null && x.EditorID.Contains(armvalues.BodyPart) && x.EditorID.Contains(armvalues.ArmorClass) && !x.EditorID.Contains("1") && !x.EditorID.Contains("2") && !x.EditorID.Contains("3") && !x.EditorID.Contains("4") && !x.EditorID.Contains("5") && !x.EditorID.Contains("6") && !x.EditorID.Contains("Ench") && !x.EditorID.Contains("Imperial"));
            foreach (var itm in lvlin2) {
                var ll = itm.DeepCopy();
                LeveledItemEntryData lld = new LeveledItemEntryData();
                lld.Count = 1;
                lld.Level = 1;
                lld.Reference.FormKey = armvalues.FormKey;
                LeveledItemEntry lle = new LeveledItemEntry();
                lle.Data = lld;
                ll.Entries?.Add(lle);
                state.PatchMod.LeveledItems.Set(ll);
            }


            for (int i = 1; i <= 6; i++)
            {
                lvliNames.Add(LeveledListPrefixName + "_0" + i + "_" + armvalues.BodyPart);
            }
            for (int i = 1; i <= 6; i++)
            {
                lvliNames.Add(LeveledListPrefixNameDragonborn + "_0" + i + "_" + armvalues.BodyPart);
            }
            
            for(int i = 0;i<lvliNames.Count;i++)
            {
                bool check = false;
                var lvlin = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x=>x.EditorID == lvliNames[i]).First();
                if (lvlin != null)
                {
                    var temp = lvlin.DeepCopy();
                    float value = 0;
                    int nritems = 0;
                    if (temp != null && temp.Entries != null)
                    {
                        foreach (var itm in temp.Entries)
                        {
                            if (itm != null && itm.Data != null)
                            {
                                itm.Data.Reference.TryResolve<IArmorGetter>(state.LinkCache, out var arm);
                                if (arm != null)
                                {
                                    value += arm.ArmorRating*itm.Data.Count;
                                    nritems += itm.Data.Count;
                                }

                            }
                        }
                        float average = value / nritems;
                        double itmnr = nritems * 0.1;
                        var result = keyValuePairs.Where(x => x.BodyPart == armvalues.BodyPart && x.ArmorClass == armvalues.ArmorClass).First();
                        foreach(var arminc in result.Brackets)
                        {
                            if (armvalues.ArmorValue > (arminc + average))
                            {
                                check = true;
                                itmnr *= 0.75;
                            }
                        }
                        if(check)
                        {
                            lvlData.Count = (short)Math.Floor(itmnr);
                            lvlEnt.Data = lvlData;
                            temp.Entries.Add(lvlEnt);
                            state.PatchMod.LeveledItems.Set(temp);
                        }
                    }
                }
            }
        }

        public static void AddToEnchantedLists(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, List<ArmorEnchantments> enchList, ArmorValues armval, IArmorGetter armor) { 
                
            List<ArmorEnchantments> result = enchList.Where(x=>x.BodyPart == armval.BodyPart && x.ArmorClass == armval.ArmorClass && x.ArmorMaterial == armval.ArmorMaterial).ToList();
            List<LeveledItem> enchantLists = new List<LeveledItem>();
            foreach(var itm in result)
            {
                if (armor.ObjectEffect.IsNull)
                {
                    Armor test = state.PatchMod.Armors.AddNew();
                    test.DeepCopyIn(armor);
                    test.EditorID = armor.EditorID + " " + itm.Enchantment;
                    test.Name = (Mutagen.Bethesda.Strings.TranslatedString?)armor.Name + itm.EnchName;
                    test.ObjectEffect.FormKey = itm.EnchKey;
                    test.TemplateArmor.FormKey = armor.FormKey;

                    string lvllistname = "SublistEnch_" + armor.EditorID + itm.EnchLvl.ToString();
                    var ee = enchantLists.Where(x => x.EditorID == lvllistname).SingleOrDefault();
                    
                    LeveledItemEntry enchantEntry = new LeveledItemEntry();
                    LeveledItemEntryData enchantEntryData = new LeveledItemEntryData();
                    enchantEntryData.Level = 1;
                    enchantEntryData.Count = 1;
                    enchantEntryData.Reference.FormKey = test.FormKey;
                    enchantEntry.Data = enchantEntryData;

                    if (ee == null) {
                        LeveledItem enchantOneList = state.PatchMod.LeveledItems.AddNew();
                        enchantOneList.EditorID = lvllistname;
                        ExtendedList<LeveledItemEntry> ff = new ExtendedList<LeveledItemEntry>();
                        enchantOneList.Entries = ff;
                        enchantOneList.Entries.Add(enchantEntry);
                        enchantLists.Add(enchantOneList);

                        string lvllistwhole = "LItemEnchArmor" + armval.ArmorClass + armval.BodyPart;
                        var cont = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x => x.EditorID == lvllistwhole).First();
                        if (cont != null && cont.Entries != null)
                        {
                            LeveledItem lli = cont.DeepCopy();
                            LeveledItemEntry lle = new LeveledItemEntry();
                            LeveledItemEntryData lled = new LeveledItemEntryData();
                            lled.Count = 1;
                            lled.Level = 1;
                            lled.Reference.FormKey = enchantOneList.FormKey;
                            lle.Data = lled;
                            lli.Entries?.Add(lle);
                            state.PatchMod.LeveledItems.Set(lli);
                        }
                    }
                    else
                    {                  
                        ee.Entries?.Add(enchantEntry);
                    }
                } 
            } 
        }


        //Summary
        //Check armor type[Heavy or Light], appropriate bodypart and it's armor value, then return the object with said values
        public static ArmorValues? CheckArmorValues(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IArmorGetter armor)
        {
            //List<string> list = new List<string>();           
            string? armorclass = null;
            string? bodypart = null;
            string? armormaterial = null;
            short count = 0;
            if (armor.Keywords != null)
            {
                foreach (var keyword in armor.Keywords)
                {
                    if (keyword != null && count != 3)
                    {
                        keyword.TryResolveIdentifier(state.LinkCache, out var identifier);
                        if (identifier != null)
                        {
                            if (identifier.Contains("Light"))
                            {
                                armorclass = "Light";
                                count++;
                            }
                            if (identifier.Contains("Heavy"))
                            {
                                armorclass = "Heavy";
                                count++;
                            }
                            if (identifier.Contains("Helmet"))
                            {
                                bodypart = "Helmet";
                                count++;
                            }
                            if (identifier.Contains("Boots"))
                            {
                                bodypart = "Boots";
                                count++;
                            }
                            if (identifier.Contains("Cuirass"))
                            {
                                bodypart = "Cuirass";
                                count++;
                            }
                            if (identifier.Contains("Gauntlets"))
                            {
                                bodypart = "Gauntlets";
                                count++;
                            }
                            if (identifier.Contains("ArmorMaterial") || identifier.Contains("ArmorSet")) {
                                armormaterial = identifier;
                                count++;
                            }
                        }
                    }
                    else {
                        break;
                    }
                }
            }
            if (armorclass != null && bodypart != null && armormaterial!= null) {
                ArmorValues armorValues = new ArmorValues(bodypart,armorclass,armormaterial,(int)armor.ArmorRating,armor.FormKey);
                return armorValues;
            }
            else {
                return null;
            }
        }

        public static LoadOrder<ISkyrimModGetter> ReturnModList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state) {
            var test = state.LoadOrder.PriorityOrder.Where(x => x.FileName.Contains("Requiem")).Select(x => x.ModKey).First();
            var test2 = state.LoadOrder.Count;
            int index = state.LoadOrder.IndexOf(test);
            LoadOrder<ISkyrimModGetter> listing = new LoadOrder<ISkyrimModGetter>();
            for (int i = index + 1; i < test2-1; i++)
            {
                var obj = state.LoadOrder.TryGetAtIndex(i);
                if (obj != null && obj.Mod != null)
                {
                    listing.Add(obj.Mod);
                }
            }
            return listing;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            LoadOrder<ISkyrimModGetter> listing = ReturnModList(state);
            List<ArmorEnchantments> enchList = FillEnchantments(state);
            foreach (var armor in listing.ListedOrder.Armor().WinningOverrides()) { 
                if(armor != null) {
                    var armorval = CheckArmorValues(state, armor);
                    if(armorval != null)
                    {
                        AddToLeveledItemList(state, armorval);
                        AddToEnchantedLists(state, enchList, armorval, armor);
                    }
                }
            }
        }
    }
}
