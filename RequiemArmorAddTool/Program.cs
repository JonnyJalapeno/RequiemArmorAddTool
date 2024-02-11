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
using CommandLine.Text;

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

        public static void AddToListHelper(IPatcherState<ISkyrimMod, ISkyrimModGetter> state,List<string> lvliNames, ArmorValues armvalues, List<ArmorIncrement> keyValuePairs) {

            for (int i = 0; i < lvliNames.Count; i++)
            {
                bool check = false;
                var lvlin = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x => x.EditorID == lvliNames[i]).SingleOrDefault();
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
                                    value += arm.ArmorRating * itm.Data.Count;
                                    nritems += itm.Data.Count;
                                }
                            }
                        }
                        float average = value / nritems;
                        double itmnr = nritems * 0.1;
                        var result = keyValuePairs.Where(x => x.BodyPart == armvalues.BodyPart && x.ArmorClass == armvalues.ArmorClass).First();
                        var result2 = result.Brackets[i];
                        if (armvalues.ArmorValue < (average + result2) && armvalues.ArmorValue > (average - result2))
                        {
                            check = true;
                        }
                        if (check)
                        {
                            itmnr = (short)Math.Floor(itmnr);
                            if (itmnr > 0)
                            {
                                LeveledItemEntryData lvlData = new LeveledItemEntryData();
                                LeveledItemEntry lvlEnt = new LeveledItemEntry();
                                lvlData.Level = 1;
                                lvlData.Reference.SetTo(armvalues.FormKey);
                                lvlData.Count = (short)Math.Floor(itmnr);
                                lvlEnt.Data = lvlData;
                                temp.Entries.Add(lvlEnt);
                                state.PatchMod.LeveledItems.Set(temp);
                            }
                        }
                    }
                }
            }
        }

        public static void AddToLeveledItemList(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, ArmorValues armvalues, IArmorGetter armor) {
            string LeveledListPrefixName = "REQ_CLI_EquipSet_Bandit" + armvalues.ArmorClass;
            string LeveledListPrefixNameDragonborn = "FZR_CLI_EquipSet_Bandit" + armvalues.ArmorClass;

            //Dictionary<string, List<int>> keyValuePairs = new Dictionary<string, List<int>>();

            List<ArmorIncrement> keyValuePairs = new List<ArmorIncrement>
            {
                new ArmorIncrement("Helmet", "Light", new List<int> { 4, 6, 10, 14, 18, 22 }),
                new ArmorIncrement("Helmet", "Heavy", new List<int> { 8, 14, 20, 26, 32, 38 }),
                new ArmorIncrement("Cuirass", "Light", new List<int> { 10, 16, 22, 28, 34, 40 }),
                new ArmorIncrement("Cuirass", "Heavy", new List<int> { 30, 40, 50, 60, 70, 80 }),
                new ArmorIncrement("Gauntlets", "Light", new List<int> { 2, 4, 6, 8, 10, 12 }),
                new ArmorIncrement("Gauntlets", "Heavy", new List<int> { 4, 8, 12, 16, 20, 24 }),
                new ArmorIncrement("Boots", "Light", new List<int> { 2, 4, 6, 8, 10, 12 }),
                new ArmorIncrement("Boots", "Heavy", new List<int> { 4, 8, 12, 16, 20, 24 }),
            };

            List<string> lvliNames = new List<string>();
            List<string> lvliNamesDragonborn = new List<string>();


            //Add to standard non-indexed lists
            var lvlin2 = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x => x.EditorID != null && x.EditorID.Contains(armvalues.BodyPart) && x.EditorID.Contains(armvalues.ArmorClass) && !x.EditorID.Contains("1") && !x.EditorID.Contains("2") && !x.EditorID.Contains("3") && !x.EditorID.Contains("4") && !x.EditorID.Contains("5") && !x.EditorID.Contains("6") && !x.EditorID.Contains("Ench") && !x.EditorID.Contains("Imperial") && !x.EditorID.Contains("Stormcloak"));
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

            //Add to indexed lists
            for (int i = 1; i <= 6; i++)
            {
                lvliNames.Add(LeveledListPrefixName + "_0" + i + "_" + armvalues.BodyPart);
            }
            for (int i = 1; i <= 6; i++)
            {
                lvliNamesDragonborn.Add(LeveledListPrefixNameDragonborn + "_0" + i + "_" + armvalues.BodyPart);
            }

            AddToListHelper(state, lvliNames, armvalues, keyValuePairs);
            AddToListHelper(state, lvliNamesDragonborn, armvalues, keyValuePairs);

            //Add quality variation items to leveled lists
            
            if(armor != null && armor.Name != null && armor.Name.String != null) { 
                string prefix = "REQ_LI_Armor_";
                string armName = armor.Name.String;
                armName = String.Concat(armName.Where(c => !Char.IsWhiteSpace(c)));
                short value = 1;
                string[] keyw = {"const","fall","rise"};
                for(int z = 0;z<keyw.Length;z++)
                {
                    string resultName = prefix + armName + "_Quality" + value.ToString() + "_N_" + keyw[z];
                    ExtendedList<LeveledItemEntry> list = new ExtendedList<LeveledItemEntry>();
                    LeveledItemEntry lle = new LeveledItemEntry();
                    LeveledItemEntryData lld = new LeveledItemEntryData();
                    lld.Count = 1;
                    lld.Level = 1;
                    lld.Reference.FormKey = armvalues.FormKey;
                    lle.Data = lld;
                    list.Add(lle);
                    var lli = state.PatchMod.LeveledItems.AddNew();
                    lli.Entries = list;
                    lli.EditorID = resultName;
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
                        var cont = state.LoadOrder.PriorityOrder.LeveledItem().WinningOverrides().Where(x => x.EditorID == lvllistwhole).SingleOrDefault();
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
                            if (identifier.Equals("ArmorLight"))
                            {
                                armorclass = "Light";
                                count++;
                            }
                            if (identifier.Equals("ArmorHeavy"))
                            {
                                armorclass = "Heavy";
                                count++;
                            }
                            if (identifier.Equals("ArmorHelmet"))
                            {
                                bodypart = "Helmet";
                                count++;
                            }
                            if (identifier.Equals("ArmorBoots"))
                            {
                                bodypart = "Boots";
                                count++;
                            }
                            if (identifier.Equals("ArmorCuirass"))
                            {
                                bodypart = "Cuirass";
                                count++;
                            }
                            if (identifier.Equals("ArmorGauntlets"))
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
                        AddToLeveledItemList(state, armorval, armor);
                        AddToEnchantedLists(state, enchList, armorval, armor);
                    }
                }
            }
        }
    }
}
