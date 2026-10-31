using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 進み具合の保存（PlayerPrefs を使うので、ゲームを終了しても残ります）。
    /// ・どのステージまで遊べるか／各ステージの結果と星
    /// ・キャラクター（見た目）・持ち物・金貨
    /// ・体力・装備（杖とローブ）・むしよけ・タイピングの言葉・お話の進み具合
    /// </summary>
    public static class Progress
    {
        private const string KeyUnlocked = "rcq.unlocked";
        private const string KeyStatus = "rcq.status";
        private const string KeyStars = "rcq.stars";
        private const string KeyMode = "rcq.mode";
        private const string KeyFurigana = "rcq.furigana";
        private const string KeySimilar = "rcq.similar";
        private const string KeyLook = "rcq.look";
        private const string KeyItems = "rcq.items";
        private const string KeyMadeCharacter = "rcq.madeChar";
        private const string KeyHp = "rcq.hp";
        private const string KeyWeapon = "rcq.weapon";
        private const string KeyArmor = "rcq.armor";
        private const string KeyOwnedWeapons = "rcq.ownW";
        private const string KeyOwnedArmors = "rcq.ownA";
        private const string KeyRepel = "rcq.repel";
        private const string KeyTypingLang = "rcq.typing";
        private const string KeyStory = "rcq.story";
        private const string KeyMill = "rcq.mill";
        private const string KeyMaterials = "rcq.mat";
        private const string KeyHuntArea = "rcq.hunt";
        private const string KeyHuntCount = "rcq.huntN";
        private const string KeyHuntsDone = "rcq.huntDone";
        private const string KeyActiveQuest = "rcq.activeQ";
        private const string KeyActiveKind = "rcq.activeK";
        private const string KeyActiveSpot = "rcq.activeS";
        private const string KeyAreaFlags = "rcq.area";
        private const string KeyTownChests = "rcq.townChest";
        private const string KeyVillage = "rcq.village";
        private const string KeyVillageFlags = "rcq.villageFlags";
        private const string KeyRepairNotes = "rcq.repairNotes";   // 直ったお祝いを見た建物（BuildingInfo.Id を , でならべる）
        private const string KeyStoryQuests = "rcq.storyDone";       // 終えたストーリークエスト（番号を , でならべる）
        private const string KeyStoryAnnounced = "rcq.storyStarted";  // 始まりの話を見たストーリークエスト（番号を , でならべる）
        private const string KeyStoryTalked = "rcq.storyTalked";     // 今のストーリークエストで 話を聞いた人（ビット）
        private const string KeyTotalBalloons = "rcq.balloons";
        private const string KeyBestBalloons = "rcq.bestBalloons";
        private const string KeySavedAtInn = "rcq.savedInn";
        private const string KeyCraft = "rcq.craft";
        private const string KeySound = "rcq.sound";
        private const string KeyMissions = "rcq.missions";
        private const string KeyMissionsClaimed = "rcq.missionsClaimed";
        private const string KeyFish = "rcq.fish";
        private const string KeySteps = "rcq.steps";
        private const string KeyGarden = "rcq.garden";
        private const string KeyDelivery = "rcq.delivery";
        private const string KeyDeliveryItem = "rcq.deliveryItem";
        private const string KeyDeliveries = "rcq.deliveries";
        private const string KeyHideFound = "rcq.hide";
        private const string KeyDex = "rcq.dex";
        private const string KeyEnglish = "rcq.english"; // 見た 英語のことば（かばんの 英語ことばカード）
        private const string KeyHeroArt = "rcq.heroArt";
        private const string KeyLookMode = "rcq.lookMode";
        private const string KeyAutoSave = "rcq.autoSave";
        private const string KeyManualSave = "rcq.manualSave";

        public const int NotPlayed = 0, Passed = 1, Cleared = 2;

        private static bool loaded;
        private static int unlocked = 1;
        private static char[] status;
        private static char[] stars;
        private static int similarClears;

        // ------------------------------------------------------------------
        // ステージ
        // ------------------------------------------------------------------

        public static int Unlocked { get { Load(); return unlocked; } }
        public static int SimilarClears { get { Load(); return similarClears; } }

        public static int StatusOf(int level)
        {
            Load();
            return level >= 1 && level <= status.Length ? status[level - 1] - '0' : NotPlayed;
        }

        public static int ClearedCount
        {
            get
            {
                Load();
                int c = 0;
                foreach (char ch in status) if (ch == '2') c++;
                return c;
            }
        }

        /// <summary>そのステージの星（0〜3）</summary>
        public static int StarsOf(int level)
        {
            Load();
            return level >= 1 && level <= stars.Length ? stars[level - 1] - '0' : 0;
        }

        /// <summary>集めた星の合計（キャラのランクになる）</summary>
        public static int TotalStars
        {
            get
            {
                Load();
                int c = 0;
                foreach (char ch in stars) c += ch - '0';
                return c;
            }
        }

        public static bool HasPlayedAnything => StatusOf(1) != NotPlayed || Unlocked > 1;

        public static void MarkCleared(int level, int starCount)
        {
            Load();
            status[level - 1] = '2';
            if (starCount > StarsOf(level)) stars[level - 1] = (char)('0' + Mathf.Clamp(starCount, 0, 3));
            Unlock(level + 1);
            Save();
        }

        public static void MarkPassed(int level)
        {
            Load();
            if (status[level - 1] == '0') status[level - 1] = '1';
            Unlock(level + 1);
            Save();
        }

        public static void AddSimilarClear()
        {
            Load();
            similarClears++;
            Save();
        }

        private static void Unlock(int level)
        {
            if (level <= QuestDatabase.LevelCount && level > unlocked) unlocked = level;
        }

        // ------------------------------------------------------------------
        // 設定
        // ------------------------------------------------------------------

        public static SolveMode PreferredMode
        {
            get => Prefs.GetInt(KeyMode, 0) == 1 ? SolveMode.Python : SolveMode.Scratch;
            set { Prefs.SetInt(KeyMode, value == SolveMode.Python ? 1 : 0); Prefs.Save(); }
        }

        public static bool HasChosenMode => Prefs.HasKey(KeyMode);

        public static bool Furigana
        {
            get => Prefs.GetInt(KeyFurigana, 1) == 1;
            set { Prefs.SetInt(KeyFurigana, value ? 1 : 0); Prefs.Save(); }
        }

        /// <summary>音（BGM・効果音）を鳴らすか（はじめからやり直しても残る設定）</summary>
        public static bool SoundOn
        {
            get => Prefs.GetInt(KeySound, 1) == 1;
            set { Prefs.SetInt(KeySound, value ? 1 : 0); Prefs.Save(); }
        }

        public enum TypingLanguage { Auto = 0, Japanese = 1, English = 2 }

        /// <summary>タイピングの言葉の設定（Auto ならワードブロック→日本語、Python→英語）</summary>
        public static TypingLanguage TypingLang
        {
            get => (TypingLanguage)Prefs.GetInt(KeyTypingLang, 0);
            set { Prefs.SetInt(KeyTypingLang, (int)value); Prefs.Save(); }
        }

        public static bool TypingIsJapanese =>
            TypingLang == TypingLanguage.Japanese || (TypingLang == TypingLanguage.Auto && PreferredMode == SolveMode.Scratch);

        // ------------------------------------------------------------------
        // キャラクター
        // ------------------------------------------------------------------

        private static CharacterLook look;

        /// <summary>プレイヤーが作ったキャラクター（なまえ・見た目）</summary>
        public static CharacterLook Look
        {
            get
            {
                if (look == null) look = CharacterLook.Decode(Prefs.GetString(KeyLook, ""));
                return look;
            }
            set
            {
                look = value;
                Prefs.SetString(KeyLook, value.Encode());
                Prefs.SetInt(KeyMadeCharacter, 1);
                Prefs.Save();
            }
        }

        public static bool HasMadeCharacter => Prefs.GetInt(KeyMadeCharacter, 0) == 1;

        /// <summary>主人公の見た目の作り方</summary>
        public const int LookIllust = 0, LookParts = 1, LookDot = 2;

        /// <summary>主人公の見た目：0 イラスト（主人公.png）・1 パーツきせかえ（CharacterParts）・2 ドットきせかえ（CharacterArt）</summary>
        public static int LookMode
        {
            get
            {
                if (Prefs.HasKey(KeyLookMode)) return Prefs.GetInt(KeyLookMode, LookIllust);
                return Prefs.GetInt(KeyHeroArt, 1) == 1 ? LookIllust : LookDot; // 前のバージョンの「イラストを使うか」から
            }
            set { Prefs.SetInt(KeyLookMode, value); Prefs.Save(); }
        }

        // ------------------------------------------------------------------
        // 持ち物（アイテムの数）。金貨（ItemKind.Coin）はお店で使うお金にもなる
        // ------------------------------------------------------------------

        private static int[] items;

        public static int ItemCount(ItemKind kind)
        {
            LoadItems();
            return items[(int)kind];
        }

        public static void AddItem(ItemKind kind, int count)
        {
            if (kind == ItemKind.None || count == 0) return;
            LoadItems();
            items[(int)kind] = Mathf.Clamp(items[(int)kind] + count, 0, 99999);
            Prefs.SetString(KeyItems, string.Join(",", items));
            Prefs.Save();
        }

        /// <summary>お金（金貨）</summary>
        public static int Coins => ItemCount(ItemKind.Coin);

        /// <summary>お金を使う。足りなければ false</summary>
        public static bool Spend(int coins)
        {
            if (Coins < coins) return false;
            AddItem(ItemKind.Coin, -coins);
            return true;
        }

        private static void LoadItems()
        {
            if (items != null) return;
            items = new int[System.Enum.GetValues(typeof(ItemKind)).Length];
            var parts = Prefs.GetString(KeyItems, "").Split(',');
            for (int i = 0; i < items.Length && i < parts.Length; i++) int.TryParse(parts[i], out items[i]);
        }

        // ------------------------------------------------------------------
        // 体力・装備
        // ------------------------------------------------------------------

        /// <summary>今の体力（-1 なら満タン）</summary>
        public static int Hp
        {
            get => Prefs.GetInt(KeyHp, -1);
            set { Prefs.SetInt(KeyHp, value); Prefs.Save(); }
        }

        public static int Weapon
        {
            get => Prefs.GetInt(KeyWeapon, 0);
            set { Prefs.SetInt(KeyWeapon, value); Prefs.Save(); }
        }

        public static int Armor
        {
            get => Prefs.GetInt(KeyArmor, 0);
            set { Prefs.SetInt(KeyArmor, value); Prefs.Save(); }
        }

        /// <summary>持っている杖・ローブ（ビットで記録。0番はさいしょから持っている）</summary>
        public static bool OwnsWeapon(int id) => id == 0 || (Prefs.GetInt(KeyOwnedWeapons, 0) & (1 << id)) != 0;
        public static bool OwnsArmor(int id) => id == 0 || (Prefs.GetInt(KeyOwnedArmors, 0) & (1 << id)) != 0;

        public static void GiveWeapon(int id)
        {
            Prefs.SetInt(KeyOwnedWeapons, Prefs.GetInt(KeyOwnedWeapons, 0) | (1 << id));
            Prefs.Save();
        }

        public static void GiveArmor(int id)
        {
            Prefs.SetInt(KeyOwnedArmors, Prefs.GetInt(KeyOwnedArmors, 0) | (1 << id));
            Prefs.Save();
        }

        /// <summary>むしよけの鈴：あと何歩、アルカナが出ないか</summary>
        public static int RepelSteps
        {
            get => Prefs.GetInt(KeyRepel, 0);
            set { Prefs.SetInt(KeyRepel, Mathf.Max(0, value)); Prefs.Save(); }
        }

        // ------------------------------------------------------------------
        // お話の進み具合（見た会話を1つずつビットで記録）
        // ------------------------------------------------------------------

        public static bool StorySeen(int id) => (Prefs.GetInt(KeyStory, 0) & (1 << id)) != 0;

        public static void MarkStory(int id)
        {
            Prefs.SetInt(KeyStory, Prefs.GetInt(KeyStory, 0) | (1 << id));
            Prefs.Save();
        }

        /// <summary>水車小屋で最後にコインをもらったときの、クリアしたステージの数</summary>
        public static int MillCounter
        {
            get => Prefs.GetInt(KeyMill, 0);
            set { Prefs.SetInt(KeyMill, value); Prefs.Save(); }
        }

        // ------------------------------------------------------------------
        // 再建の資材・アルカナ退治の依頼
        // ------------------------------------------------------------------

        private static int[] materials;

        public static int MaterialCount(Material m)
        {
            LoadMaterials();
            return materials[(int)m];
        }

        /// <summary>資材をふやす（マイナスで使う。0 より少なくはならない）</summary>
        public static void AddMaterial(Material m, int count)
        {
            if (count == 0) return;
            LoadMaterials();
            materials[(int)m] = Mathf.Clamp(materials[(int)m] + count, 0, 9999);
            Prefs.SetString(KeyMaterials, string.Join(",", materials));
            Prefs.Save();
        }

        private static void LoadMaterials()
        {
            if (materials != null) return;
            materials = new int[Rebuild.MaterialKinds];
            var parts = Prefs.GetString(KeyMaterials, "").Split(',');
            for (int i = 0; i < materials.Length && i < parts.Length; i++) int.TryParse(parts[i], out materials[i]);
        }

        /// <summary>受けているアルカナ退治の依頼の区画（-1 なら受けていない）</summary>
        public static int HuntArea
        {
            get => Prefs.GetInt(KeyHuntArea, 0) - 1;
            set { Prefs.SetInt(KeyHuntArea, value + 1); Prefs.SetInt(KeyHuntCount, 0); Prefs.Save(); }
        }

        /// <summary>退治の依頼で、たおした数</summary>
        public static int HuntCount
        {
            get => Prefs.GetInt(KeyHuntCount, 0);
            set { Prefs.SetInt(KeyHuntCount, value); Prefs.Save(); }
        }

        /// <summary>引き受けて、目的地に向かっているクエストのステージ（0 ならなし）</summary>
        public static int ActiveQuest
        {
            get => Prefs.GetInt(KeyActiveQuest, 0);
            set { Prefs.SetInt(KeyActiveQuest, value); Prefs.Save(); }
        }

        /// <summary>引き受けたクエストの依頼の種類</summary>
        public static RequestKind ActiveQuestKind
        {
            get => (RequestKind)Prefs.GetInt(KeyActiveKind, 0);
            set { Prefs.SetInt(KeyActiveKind, (int)value); Prefs.Save(); }
        }

        /// <summary>引き受けたクエストの目的地（TownData.QuestSpots の番号）</summary>
        public static int ActiveSpot
        {
            get => Prefs.GetInt(KeyActiveSpot, 0);
            set { Prefs.SetInt(KeyActiveSpot, value); Prefs.Save(); }
        }

        /// <summary>遺跡のエリアで取った宝箱・薬草（ステージごとのビット）</summary>
        public static int AreaFlags(int stage)
        {
            var parts = Prefs.GetString(KeyAreaFlags, "").Split(',');
            return stage >= 1 && stage <= parts.Length && int.TryParse(parts[stage - 1], out int v) ? v : 0;
        }

        public static void SetAreaFlags(int stage, int flags)
        {
            if (stage < 1 || stage > QuestDatabase.LevelCount) return;
            var list = new System.Collections.Generic.List<string>(Prefs.GetString(KeyAreaFlags, "").Split(','));
            while (list.Count < QuestDatabase.LevelCount) list.Add("0");
            list[stage - 1] = flags.ToString();
            Prefs.SetString(KeyAreaFlags, string.Join(",", list));
            Prefs.Save();
        }

        /// <summary>いまいる村（0=ルーネ村 … 5=ステラリア）</summary>
        public static int Village
        {
            get => Prefs.GetInt(KeyVillage, 0);
            set { Prefs.SetInt(KeyVillage, value); Prefs.Save(); }
        }

        /// <summary>村の話を見たかの記録（id は 0〜30）。建物が直ったお祝い・はじめて着いたときの話</summary>
        public static bool VillageFlag(int id) => (Prefs.GetInt(KeyVillageFlags, 0) & (1 << id)) != 0;

        public static void MarkVillageFlag(int id)
        {
            Prefs.SetInt(KeyVillageFlags, Prefs.GetInt(KeyVillageFlags, 0) | (1 << id));
            Prefs.Save();
        }

        /// <summary>そのストーリークエスト（StoryQuest.Id）を終えたか</summary>
        public static bool RepairNoteSeen(int buildingId) => HasId(KeyRepairNotes, buildingId);

        public static void MarkRepairNote(int buildingId) => AddId(KeyRepairNotes, buildingId);

        public static bool StoryQuestDone(int id) => HasId(KeyStoryQuests, id);

        public static void MarkStoryQuestDone(int id) => AddId(KeyStoryQuests, id);

        public static bool StoryAnnounced(int id) => HasId(KeyStoryAnnounced, id);

        public static void MarkStoryAnnounced(int id) => AddId(KeyStoryAnnounced, id);

        /// <summary>番号のリスト（"3,12,40" のような文字）に その番号があるか</summary>
        private static bool HasId(string key, int id) => ("," + Prefs.GetString(key, "") + ",").Contains("," + id + ",");

        private static void AddId(string key, int id)
        {
            if (HasId(key, id)) return;
            string s = Prefs.GetString(key, "");
            Prefs.SetString(key, s.Length == 0 ? id.ToString() : s + "," + id);
            Prefs.Save();
        }

        /// <summary>今のストーリークエストで、話を聞いた人（StoryQuest.Targets の番号のビット）</summary>
        public static int StoryTalked
        {
            get => Prefs.GetInt(KeyStoryTalked, 0);
            set { Prefs.SetInt(KeyStoryTalked, value); Prefs.Save(); }
        }

        /// <summary>開けた町の宝箱（村ごとに4つずつ。TownData の村の番号×4 + 宝箱の番号 のビット）</summary>
        public static int TownChests
        {
            get => Prefs.GetInt(KeyTownChests, 0);
            set { Prefs.SetInt(KeyTownChests, value); Prefs.Save(); }
        }

        /// <summary>風船割りで、これまでに割った風船の合計（キャラクリのほうびに使う）</summary>
        public static int TotalBalloons
        {
            get => Prefs.GetInt(KeyTotalBalloons, 0);
            set { Prefs.SetInt(KeyTotalBalloons, value); Prefs.Save(); }
        }

        /// <summary>風船割りの、1回で割った数の最高記録</summary>
        public static int BestBalloons
        {
            get => Prefs.GetInt(KeyBestBalloons, 0);
            set { Prefs.SetInt(KeyBestBalloons, value); Prefs.Save(); }
        }

        /// <summary>宿舎でセーブしたか（「つづきから」で宿舎の中から始める）</summary>
        public static bool SavedAtInn
        {
            get => Prefs.GetInt(KeySavedAtInn, 0) == 1;
            set { Prefs.SetInt(KeySavedAtInn, value ? 1 : 0); Prefs.Save(); }
        }

        private static int[] craft;

        /// <summary>装備を作るための素材の数</summary>
        public static int CraftCount(CraftMaterial m)
        {
            LoadCraft();
            return craft[(int)m];
        }

        public static void AddCraft(CraftMaterial m, int count)
        {
            if (count == 0) return;
            LoadCraft();
            craft[(int)m] = Mathf.Clamp(craft[(int)m] + count, 0, 9999);
            Prefs.SetString(KeyCraft, string.Join(",", craft));
            Prefs.Save();
        }

        private static void LoadCraft()
        {
            if (craft != null) return;
            craft = new int[Balloon.CraftKinds];
            var parts = Prefs.GetString(KeyCraft, "").Split(',');
            for (int i = 0; i < craft.Length && i < parts.Length; i++) int.TryParse(parts[i], out craft[i]);
        }

        /// <summary>見習いの手帳：できたおつかい（Missions の番号のビット）</summary>
        public static int MissionsDone
        {
            get => Prefs.GetInt(KeyMissions, 0);
            set { Prefs.SetInt(KeyMissions, value); Prefs.Save(); }
        }

        /// <summary>見習いの手帳：ごほうびをもらったおつかい（ビット 7 は「全部できた」のごほうび）</summary>
        public static int MissionsClaimed
        {
            get => Prefs.GetInt(KeyMissionsClaimed, 0);
            set { Prefs.SetInt(KeyMissionsClaimed, value); Prefs.Save(); }
        }

        /// <summary>歩いたマスの数（町・エリア・宿舎）。畑の作物は、これで育つ</summary>
        public static int Steps
        {
            get => Prefs.GetInt(KeySteps, 0);
            set => Prefs.SetInt(KeySteps, value); // 1歩ごとなので Save はしない（ほかの記録といっしょに保存される）
        }

        /// <summary>畑：その畑にたねをまいたときの Steps（-1 なら何も植わっていない）</summary>
        public static int GardenPlanted(int bed)
        {
            var parts = Prefs.GetString(KeyGarden, "").Split(',');
            return bed < parts.Length && int.TryParse(parts[bed], out int v) ? v : -1;
        }

        public static void SetGardenPlanted(int bed, int steps)
        {
            var parts = Prefs.GetString(KeyGarden, "").Split(',');
            var list = new System.Collections.Generic.List<string>(parts);
            while (list.Count <= bed) list.Add("-1");
            for (int i = 0; i < list.Count; i++) if (list[i] == "") list[i] = "-1";
            list[bed] = steps.ToString();
            Prefs.SetString(KeyGarden, string.Join(",", list));
            Prefs.Save();
        }

        /// <summary>配達：届け先の町の人（TownData.Npcs の番号 + 1。0 なら配達していない）</summary>
        public static int Delivery
        {
            get => Prefs.GetInt(KeyDelivery, 0);
            set { Prefs.SetInt(KeyDelivery, value); Prefs.Save(); }
        }

        /// <summary>配達：運んでいる荷物の番号</summary>
        public static int DeliveryItem
        {
            get => Prefs.GetInt(KeyDeliveryItem, 0);
            set { Prefs.SetInt(KeyDeliveryItem, value); Prefs.Save(); }
        }

        /// <summary>届けた荷物の数</summary>
        public static int DeliveriesDone
        {
            get => Prefs.GetInt(KeyDeliveries, 0);
            set { Prefs.SetInt(KeyDeliveries, value); Prefs.Save(); }
        }

        /// <summary>かくれんぼで見つけた回数</summary>
        public static int HideFound
        {
            get => Prefs.GetInt(KeyHideFound, 0);
            set { Prefs.SetInt(KeyHideFound, value); Prefs.Save(); }
        }

        // ------------------------------------------------------------------
        // アルカナ図鑑：見つけたか・何回たおしたか（名前ごと）
        // 「名前=たおした数;名前=たおした数;…」の形で記録する（のっていれば見つけた）
        // ------------------------------------------------------------------

        private static Dictionary<string, int> dex;

        private static Dictionary<string, int> Dex
        {
            get
            {
                if (dex != null) return dex;
                dex = new Dictionary<string, int>();
                foreach (var part in Prefs.GetString(KeyDex, "").Split(';'))
                {
                    int eq = part.LastIndexOf('=');
                    if (eq > 0 && int.TryParse(part.Substring(eq + 1), out int n)) dex[part.Substring(0, eq)] = n;
                }
                // 名前が かわった アルカナ（前は「〇〇バグ」）の 記録を、新しい名前に うつす
                foreach (var (oldName, newName) in RenamedArcana)
                    foreach (var pre in new[] { "", "凶暴な" })
                        if (dex.TryGetValue(pre + oldName, out int m)) { dex.Remove(pre + oldName); dex[pre + newName] = Mathf.Max(m, dex.TryGetValue(pre + newName, out int k) ? k : 0); }
                return dex;
            }
        }

        private static readonly (string, string)[] RenamedArcana =
        {
            ("プチバグ", "プチアルカ"), ("ドングリバグ", "ドングリン"), ("ループバグ", "グルグルン"), ("ハグルマバグ", "ハグルマムシ"),
        };

        private static void SaveDex()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in Dex) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
            Prefs.SetString(KeyDex, sb.ToString());
            Prefs.Save();
        }

        public static bool DexSeen(string name) => Dex.ContainsKey(name);

        // ----- 英語ことばカード：クエストで 見た 英単語 -----
        private static HashSet<string> english;
        private static HashSet<string> English => english ?? (english = new HashSet<string>(Prefs.GetString(KeyEnglish, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)));

        public static bool EnglishSeen(string word) => English.Contains(word);
        public static int EnglishSeenCount => English.Count;

        /// <summary>英単語を 見たことにする（あたらしく ふえたら true）</summary>
        public static bool MarkEnglish(IEnumerable<string> words)
        {
            bool added = false;
            foreach (var w in words) added |= English.Add(w);
            if (added) { Prefs.SetString(KeyEnglish, string.Join(",", English)); Prefs.Save(); }
            return added;
        }
        public static int DexDefeated(string name) => Dex.TryGetValue(name, out int n) ? n : 0;

        /// <summary>図鑑に見つけた記録をつける。はじめて見つけたら true</summary>
        public static bool MarkDexSeen(string name)
        {
            if (Dex.ContainsKey(name)) return false;
            Dex[name] = 0;
            SaveDex();
            return true;
        }

        /// <summary>たおした記録をつける。はじめてたおしたら true</summary>
        public static bool AddDexDefeat(string name)
        {
            int n = DexDefeated(name);
            Dex[name] = n + 1;
            SaveDex();
            return n == 0;
        }

        // ------------------------------------------------------------------
        // セーブした場所（オートセーブ・自分でセーブ）。持ち物やクリアの記録は、いつも自動で記録される
        // ------------------------------------------------------------------

        public struct SavePoint
        {
            public bool Valid;
            public bool Inn;      // 宿舎の中
            public int X, Y;      // 町のマス（宿舎のときは使わない）
            public string When;   // セーブした日時（表示用）
            public string Place;  // 場所の名前（表示用）
            public int Village;   // どの村か（0=ルーネ村）
        }

        public static SavePoint GetSave(bool manual)
        {
            var parts = Prefs.GetString(manual ? KeyManualSave : KeyAutoSave, "").Split('|');
            if (parts.Length >= 5 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                return new SavePoint
                {
                    Valid = true, Inn = parts[0] == "inn", X = x, Y = y, When = parts[3], Place = parts[4],
                    Village = parts.Length >= 6 && int.TryParse(parts[5], out int v) ? v : 0, // 村がない古いデータは ルーネ村
                };
            // 前のバージョン：宿舎の本で記録していたら、宿舎から
            if (manual && SavedAtInn) return new SavePoint { Valid = true, Inn = true, When = "", Place = "宿舎の中" };
            return new SavePoint();
        }

        public static void SetSave(bool manual, SavePoint p)
        {
            string place = (p.Place ?? "").Replace("|", " ");
            Prefs.SetString(manual ? KeyManualSave : KeyAutoSave, $"{(p.Inn ? "inn" : "town")}|{p.X}|{p.Y}|{p.When}|{place}|{p.Village}");
            Prefs.Save();
        }

        /// <summary>つった魚の数</summary>
        public static int FishCaught
        {
            get => Prefs.GetInt(KeyFish, 0);
            set { Prefs.SetInt(KeyFish, value); Prefs.Save(); }
        }

        /// <summary>終えた退治の依頼の数</summary>
        public static int HuntsDone
        {
            get => Prefs.GetInt(KeyHuntsDone, 0);
            set { Prefs.SetInt(KeyHuntsDone, value); Prefs.Save(); }
        }

        // ------------------------------------------------------------------

        /// <summary>最初からやり直す（ふりがな・タイピングの言葉・音の設定は残る）</summary>
        /// <summary>読みこんだ記録を忘れる（ちがうセーブデータに切りかえたとき）</summary>
        public static void ClearCaches()
        {
            dex = null;
            look = null;
            items = null;
            materials = null;
            craft = null;
            loaded = false;
        }

        public static void ResetAll()
        {
            dex = null;
            english = null;
            foreach (var key in new[]
            {
                KeyUnlocked, KeyStatus, KeyStars, KeyMode, KeySimilar, KeyLook, KeyItems, KeyMadeCharacter,
                KeyHp, KeyWeapon, KeyArmor, KeyOwnedWeapons, KeyOwnedArmors, KeyRepel, KeyStory, KeyMill,
                KeyMaterials, KeyHuntArea, KeyHuntCount, KeyHuntsDone, KeyActiveQuest, KeyActiveKind, KeyActiveSpot, KeyAreaFlags, KeyTownChests, KeyVillage, KeyVillageFlags, KeyRepairNotes, KeyStoryQuests, KeyStoryAnnounced, KeyStoryTalked, KeyFish, KeyTotalBalloons, KeyBestBalloons, KeySavedAtInn, KeyCraft, KeyMissions, KeyMissionsClaimed,
                KeySteps, KeyGarden, KeyDelivery, KeyDeliveryItem, KeyDeliveries, KeyHideFound, KeyDex, KeyEnglish, KeyAutoSave, KeyManualSave, KeyHeroArt, KeyLookMode,
            })
                Prefs.DeleteKey(key);
            Prefs.Save();
            look = null;
            items = null;
            materials = null;
            craft = null;
            loaded = false;
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            unlocked = Mathf.Clamp(Prefs.GetInt(KeyUnlocked, 1), 1, QuestDatabase.LevelCount);
            similarClears = Prefs.GetInt(KeySimilar, 0);
            status = ReadDigits(KeyStatus, '2');
            stars = ReadDigits(KeyStars, '3');
        }

        private static char[] ReadDigits(string key, char max)
        {
            string s = Prefs.GetString(key, "");
            var arr = new char[QuestDatabase.LevelCount];
            for (int i = 0; i < arr.Length; i++) arr[i] = i < s.Length && s[i] >= '0' && s[i] <= max ? s[i] : '0';
            return arr;
        }

        private static void Save()
        {
            Prefs.SetInt(KeyUnlocked, unlocked);
            Prefs.SetString(KeyStatus, new string(status));
            Prefs.SetString(KeyStars, new string(stars));
            Prefs.SetInt(KeySimilar, similarClears);
            Prefs.Save();
        }
    }
}
