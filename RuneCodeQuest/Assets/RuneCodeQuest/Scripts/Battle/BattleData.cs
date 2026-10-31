using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>タイピングの相手の種類</summary>
    public enum BattleKind
    {
        Bugmon,    // アルカナとのバトル
        Fishing,   // つり（時間内に打ち終わると魚がつれる）
    }

    /// <summary>アルカナ（草むらから出てくる敵）・魚</summary>
    public class EnemyInfo
    {
        public string Name;
        public int Hp;
        public int Attack;           // 1回のこうげきのダメージ（0 なら攻撃してこない）
        public float Interval;       // 何秒ごとにこうげきしてくるか
        public int Coins;            // たおすともらえる金貨
        public (byte r, byte g, byte b) Color;
        public bool Boss;
        public MonsterShape Shape;   // 見た目の形（MonsterArt）
        public BattleKind Kind;
        public float TimeLimit;      // 0 より大きければ、この秒数で にげられる（つり）
        public int Tier;             // 強さの段階（ランク10ごとに1つ上がる。BattleData.Scaled）
        public bool Rare;            // めったに出ないアルカナ
        public string Description;   // 図鑑の説明
        public ArcanaRole Role;      // ふつう・旅の道・レア・旅の道のぬし
        public int Region = -1;      // 出る村（0〜5）。ラスボス・魚は -1
        public bool Variant;         // 凶暴な亜種（ほかの国に あらわれた、強いアルカナ）
        public string DexName;       // 亜種の 元のアルカナの名前（絵をさがすときに使う）

        /// <summary>図鑑に記録する名前（亜種は「凶暴な〇〇」として 亜種の欄に記録する）</summary>
        public string DexKey => Name;

        /// <summary>絵をさがす名前（亜種は 元のアルカナの絵を使う）</summary>
        public string ArtKey => DexName ?? Name;
    }

    /// <summary>杖（攻撃力）・ローブ（体力と守り）</summary>
    public class GearInfo
    {
        public string Name;
        public int Price;
        public int Attack;     // 杖：1文字あたりのダメージに足す
        public int MaxHp;      // ローブ：体力の最大値に足す
        public int Defense;    // ローブ：受けるダメージをへらす
        public string Description;
        public int[] Recipe;   // 素材で作る装備：CraftMaterial の順に使う数（null なら金貨で買う）
    }

    /// <summary>タイピングで打つ言葉</summary>
    public class TypingWord
    {
        public string Display;   // 画面に出す文字（漢字・ひらがな・英語・日本語の意味）
        public string Reading;   // 日本語：ひらがなの読み（ローマ字で打つ）／英語：そのまま打つ文字
        public string Meaning;   // 英語のときに下に出す意味（なければ null）
    }

    /// <summary>
    /// バトルのデータ。敵の強さ・言葉・装備の値段などは、ここを書きかえて調整します。
    /// </summary>
    public static class BattleData
    {
        // ------------------------------------------------------------------
        // アルカナ（区画ごと。町のどこの草むらかで出る敵が変わる）
        // ------------------------------------------------------------------

        /// <summary>アルカナが出る6つの土地（ストーリーの村：ルーネ村と5つの国）。章と同じ順番</summary>
        public static readonly string[] RegionNames = { "ルーネ村", "アクアリス", "シルフィード", "サンドリア", "ギアルド", "ステラリア" };

        /// <summary>それぞれの村から次へ行く 旅の道の名前（旅の道のアルカナ・ぬしが出る所）</summary>
        public static readonly string[] FieldNames = { "まよいの森", "きりの湿原", "風の峡谷", "かれた鉱山", "星見の山道", "忘れられた遺跡" };

        // アルカナ120体（ArcanaData の表から作る）
        private static readonly List<EnemyInfo>[,] arcana = ArcanaData.Build();

        private static EnemyInfo[][] ByRole(ArcanaRole role)
        {
            var a = new EnemyInfo[ArcanaData.RegionCount][];
            for (int r = 0; r < a.Length; r++) a[r] = arcana[r, (int)role].ToArray();
            return a;
        }

        /// <summary>村のまわりの草むら・遺跡のエリアに出る ふつうのアルカナ（各村12体）</summary>
        public static readonly EnemyInfo[][] EnemiesByRegion = ByRole(ArcanaRole.Normal);

        /// <summary>旅の道のアルカナ（各村4体）。旅の道ができるまでは、その村の章の遺跡のエリアに出る</summary>
        public static readonly EnemyInfo[][] FieldByRegion = ByRole(ArcanaRole.Field);

        /// <summary>めったに出ない、レアなアルカナ（各村3体）。強いが、金貨をたくさん持っている</summary>
        public static readonly EnemyInfo[][] RaresByRegion = ByRole(ArcanaRole.Rare);

        /// <summary>旅の道のぬし（各村1体）。旅の道の出口で待っている</summary>
        public static readonly EnemyInfo[][] FieldBossByRegion = ByRole(ArcanaRole.FieldBoss);

        public const int RarePercent = 2;            // 出会ったときに レアになる確率（%）（レア3体の合計。ふつうの1体より ずっと出にくい）
        public const float NormalRateScale = 0.75f;  // ふつうのアルカナの出やすさ（前の 0.75 倍＝少し出にくい）

        private static readonly System.Random encounterRng = new System.Random();

        /// <summary>土地の番号をそろえる（0〜5）</summary>
        public static int ClampRegion(int region) => System.Math.Max(0, System.Math.Min(EnemiesByRegion.Length - 1, region));

        /// <summary>その章の土地（第1章＝ルーネ村 … 第6章＝ステラリア）</summary>
        public static int RegionOfChapter(int chapter) => ClampRegion(chapter - 1);

        /// <summary>かならず出会うとき（退治の依頼）：その土地のアルカナ（ときどきレア）</summary>
        public static EnemyInfo Encounter(int region)
        {
            region = ClampRegion(region);
            if (encounterRng.Next(100) < RarePercent) return PickRare(region);
            var list = EnemiesByRegion[region];
            return list[encounterRng.Next(list.Length)];
        }

        /// <summary>
        /// 草むらを1歩あるいたとき：出会うアルカナ（出会わなければ null）。baseRate は前の「1歩で出会う確率」。
        /// ふつうのアルカナは 前より少し出にくく（0.75倍）、レアは baseRate の 2%。
        /// 凶暴な亜種（ストーリーが進むと出る）は、ふつうのアルカナとは べつに出る（ふつうのアルカナは へらない）。
        /// </summary>
        public static EnemyInfo RollEncounter(int region, float baseRate, bool withField = false, int storyChapter = 0)
        {
            region = ClampRegion(region);
            float rare = baseRate * RarePercent / 100f;
            float normal = baseRate * (1f - RarePercent / 100f) * NormalRateScale;
            float variant = normal * VariantPercent(storyChapter) / 100f;
            double r = encounterRng.NextDouble();
            if (r < rare) return PickRare(region);
            if (r < rare + normal)
            {
                // 遺跡のエリアでは、3回に1回くらい 旅の道のアルカナ
                var list = withField && FieldByRegion[region].Length > 0 && encounterRng.Next(3) == 0 ? FieldByRegion[region] : EnemiesByRegion[region];
                return list[encounterRng.Next(list.Length)];
            }
            if (r < rare + normal + variant) return PickVariant(region, storyChapter);
            return null;
        }

        // ------------------------------------------------------------------
        // 凶暴な亜種：ストーリーが進むと、ほかの国のアルカナが 強くなって あらわれる
        // ------------------------------------------------------------------

        /// <summary>今いる村（村ごとのマップができるまでは、町＝ルーネ村）</summary>
        public static int CurrentVillage = 0;

        /// <summary>亜種の出やすさ（ふつうのアルカナの何%ぶん）。第1章は0、章が進むほど ふえる</summary>
        public static int VariantPercent(int storyChapter) => storyChapter <= 1 ? 0 : System.Math.Min(35, 5 + (storyChapter - 1) * 6);

        /// <summary>
        /// ほかの国（ストーリーで出てきた国まで）のアルカナの 凶暴な亜種をえらぶ（ほかの国がまだなければ null）
        /// </summary>
        private static EnemyInfo PickVariant(int village, int storyChapter)
        {
            int top = ClampRegion(storyChapter - 1);
            var regions = new List<int>();
            for (int r = 0; r <= top; r++) if (r != village) regions.Add(r);
            if (regions.Count == 0) return null;
            int from = regions[encounterRng.Next(regions.Count)];
            var pool = new List<EnemyInfo>(EnemiesByRegion[from]);
            pool.AddRange(FieldByRegion[from]);
            return MakeVariant(pool[encounterRng.Next(pool.Count)], village);
        }

        // ----- 凶暴な亜種の強さ（村ごとに決まっている） -----
        /// <summary>バトルが続く長さのめやす（秒）。強い杖に持ちかえて この時間 がんばって打つと たおせる体力にする</summary>
        public const float VariantFightSeconds = 35f;
        /// <summary>そのあいだに 受けるダメージ ＝ 体力の最大値 × これ（強い杖なら ギリギリ勝てる）</summary>
        public const float VariantHpShare = 0.9f;
        /// <summary>
        /// 村ごとの「この杖に持ちかえたら 勝てるかな？」の杖の攻撃力。
        /// その村のころ持っていそうな杖（ArcanaData の めやす）より 強い杖：
        /// ルーネ村＝見習いの杖・アクアリス＝水晶の杖・シルフィード＝ルーンの杖・サンドリア＝伝説の杖・ギアルドとステラリア＝虹の杖
        /// </summary>
        private static readonly int[] VariantWeapon = { 1, 4, 6, 9, 11, 11 };

        /// <summary>
        /// 凶暴な亜種：村ごとに強さが決まっている。今の杖のままだと まず勝てないが、
        /// 強い杖に持ちかえたら「がんばって打てば 勝てるかな？」くらいの強さ。
        /// 強さは、出た村と 元のアルカナの国の 強いほうに合わせる。色は赤っぽく、金貨は多め
        /// </summary>
        public static EnemyInfo MakeVariant(EnemyInfo b, int village)
        {
            int region = System.Math.Max(ClampRegion(village), ClampRegion(b.Region));
            float interval = System.Math.Max(4f, b.Interval - 0.3f);
            var (hp, attack) = ArcanaData.Tune(ArcanaData.ExpectedPlayer(region, VariantWeapon[region]), VariantFightSeconds, VariantHpShare, interval);
            return new EnemyInfo
            {
                Name = "凶暴な" + b.Name, DexName = b.Name, Variant = true,
                Shape = b.Shape, Description = b.Description, Role = b.Role, Region = b.Region, Kind = b.Kind,
                Color = ((byte)System.Math.Min(255, b.Color.r + 50), (byte)(b.Color.g * 0.6f), (byte)(b.Color.b * 0.6f)),
                Hp = hp,
                Attack = attack,
                Interval = interval,
                Coins = System.Math.Max(b.Coins * 2, hp / 2),
            };
        }

        private static EnemyInfo PickRare(int region)
        {
            var rares = RaresByRegion[region];
            return rares[encounterRng.Next(rares.Length)];
        }

        private static List<EnemyInfo> allVariants;

        /// <summary>図鑑の「凶暴な亜種」の欄：すべての国の ふつう・旅の道のアルカナの亜種（強さは その国に出たとき）</summary>
        public static List<EnemyInfo> AllVariants()
        {
            if (allVariants != null) return allVariants;
            allVariants = new List<EnemyInfo>();
            for (int r = 0; r < EnemiesByRegion.Length; r++)
            {
                foreach (var e in EnemiesByRegion[r]) allVariants.Add(MakeVariant(e, r));
                foreach (var e in FieldByRegion[r]) allVariants.Add(MakeVariant(e, r));
            }
            return allVariants;
        }

        /// <summary>図鑑にのるアルカナ（土地の順に ふつう→レア、さいごにボス）</summary>
        public static IEnumerable<EnemyInfo> AllMonsters()
        {
            for (int r = 0; r < EnemiesByRegion.Length; r++)
            {
                foreach (var e in EnemiesByRegion[r]) yield return e;
                foreach (var e in FieldByRegion[r]) yield return e;
                foreach (var e in RaresByRegion[r]) yield return e;
                foreach (var e in FieldBossByRegion[r]) yield return e;
            }
            yield return Boss;
        }

        /// <summary>そのアルカナが出る土地（0〜5。ボスは -1）</summary>
        public static int RegionOfMonster(EnemyInfo e) => e.Region;

        // ------------------------------------------------------------------
        // ランクと強さ：草むらのアルカナは、その村のめやすのランク（ArcanaData.TargetRank）を
        // 10こえるごとに少しだけ強くなる（めやすのランクまでは そのまま＝ギリギリ勝てるかな？の強さ）
        // ------------------------------------------------------------------

        public const int RanksPerTier = 10;
        public const int HpPercentPerTier = 10;   // 1段階ごとに体力 +10%
        public const int CoinPercentPerTier = 10; // 1段階ごとに金貨 +10%

        /// <summary>そのランクでの強さの段階（村のめやすのランク＋0〜9は0、＋10〜19は1、…）</summary>
        public static int TierOf(int rank, int region) =>
            System.Math.Max(0, (rank - ArcanaData.TargetRank[ClampRegion(region)]) / RanksPerTier);

        /// <summary>ランクに合わせて少し強くしたアルカナ（元のデータは書きかえない）</summary>
        public static EnemyInfo Scaled(EnemyInfo e, int rank)
        {
            if (e.Boss || e.Variant || e.Kind == BattleKind.Fishing || e.Region < 0) return e; // 亜種は 村ごとに強さが決まっている
            int tier = TierOf(rank, e.Region);
            if (tier == 0) return e;
            return new EnemyInfo
            {
                Name = e.Name, Color = e.Color, Boss = e.Boss, Shape = e.Shape, Kind = e.Kind, TimeLimit = e.TimeLimit, Interval = e.Interval,
                Rare = e.Rare, Description = e.Description, Role = e.Role, Region = e.Region, Variant = e.Variant, DexName = e.DexName,
                Hp = e.Hp * (100 + HpPercentPerTier * tier) / 100,
                Attack = e.Attack + (tier + 1) / 2,  // こうげきは 2段階で +1 くらい（段階1で+1、段階3で+2…）
                Coins = e.Coins * (100 + CoinPercentPerTier * tier) / 100,
                Tier = tier,
            };
        }

        /// <summary>つりの魚（まれに金のさかな）。時間内に打ち終わるとつれる</summary>
        public static EnemyInfo Fish(Random rng, int rank)
        {
            int roll = rng.Next(100);
            var (name, color, coins) = roll < 8 ? ("金のさかな", ((byte)250, (byte)200, (byte)60), 60)
                : roll < 35 ? ("ナマズ", ((byte)120, (byte)110, (byte)90), 20)
                : roll < 65 ? ("マス", ((byte)150, (byte)170, (byte)200), 12)
                : ("コイ", ((byte)230, (byte)120, (byte)70), 8);
            return new EnemyInfo
            {
                Name = name, Hp = 8 + rank / 2 + (roll < 8 ? 6 : 0), Attack = 0, Interval = 999f, Coins = coins, Color = color,
                Shape = MonsterShape.Fish, Kind = BattleKind.Fishing, TimeLimit = 12f,
            };
        }

        /// <summary>星の神殿のラスボス：守り神オルディナ</summary>
        public static readonly EnemyInfo Boss = MakeBoss();

        // ----- ラスボスの強さ：ランク100・いちばん強い装備で「ギリギリ勝てるかな？」 -----
        public const int BossTargetRank = 100;
        /// <summary>バトルの長さのめやす（秒）。60秒 がんばって打ち続けると たおせる体力</summary>
        public const float BossFightSeconds = 60f;
        /// <summary>そのあいだに 受けるダメージ ＝ 体力の最大値 × これ（体力が1割くらい のこる）</summary>
        public const float BossHpShare = 0.9f;
        private const float BossInterval = 5f;

        /// <summary>守り神オルディナ：ランク100で 虹の杖・虹のローブのプレイヤーが ギリギリ勝てるかな？の強さ</summary>
        private static EnemyInfo MakeBoss()
        {
            // PlayerStats と同じ計算（虹の杖＝攻撃+11、虹のローブ＝体力+70・守り+8）
            var player = (attack: 1 + 11 + (BossTargetRank - 1) / 5, maxHp: 30 + (BossTargetRank - 1) * 4 + 70, defense: 8);
            var (hp, attack) = ArcanaData.Tune(player, BossFightSeconds, BossHpShare, BossInterval);
            return new EnemyInfo
            {
                Name = "オルディナ", Hp = hp, Attack = attack, Interval = BossInterval, Coins = 500, Color = (200, 40, 60), Boss = true, Shape = MonsterShape.King,
                Description = "世界を見守る 守り神。アルカナを苦しめる人間に いかり、世界を はじめから やりなおそうとした。",
            };
        }

        // ------------------------------------------------------------------
        // 装備
        // ------------------------------------------------------------------

        public static readonly GearInfo[] Weapons =
        {
            new GearInfo { Name = "木の杖", Price = 0, Attack = 0, Description = "さいしょから持っている杖。" },
            new GearInfo { Name = "見習いの杖", Price = 40, Attack = 1, Description = "1文字のダメージ +1" },
            new GearInfo { Name = "銀の杖", Price = 120, Attack = 2, Description = "1文字のダメージ +2" },
            new GearInfo { Name = "水晶の杖", Price = 300, Attack = 4, Description = "1文字のダメージ +4" },
            new GearInfo { Name = "ルーンの杖", Price = 700, Attack = 6, Description = "1文字のダメージ +6" },
            new GearInfo { Name = "伝説の杖", Price = 1500, Attack = 9, Description = "1文字のダメージ +9" },
            // 風船割りの素材で作る杖（レアな素材ほど強い）
            new GearInfo { Name = "風の杖", Attack = 3, Description = "1文字のダメージ +3", Recipe = new[] { 8, 2, 0, 0 } },
            new GearInfo { Name = "星の杖", Attack = 7, Description = "1文字のダメージ +7", Recipe = new[] { 15, 0, 3, 0 } },
            new GearInfo { Name = "虹の杖", Attack = 11, Description = "1文字のダメージ +11（いちばん強い）", Recipe = new[] { 0, 0, 5, 3 } },
        };

        public static readonly GearInfo[] Armors =
        {
            new GearInfo { Name = "布のローブ", Price = 0, MaxHp = 0, Defense = 0, Description = "さいしょから着ているローブ。" },
            new GearInfo { Name = "革のローブ", Price = 50, MaxHp = 10, Defense = 1, Description = "体力 +10　守り +1" },
            new GearInfo { Name = "鉄のローブ", Price = 150, MaxHp = 20, Defense = 2, Description = "体力 +20　守り +2" },
            new GearInfo { Name = "魔法のローブ", Price = 400, MaxHp = 35, Defense = 4, Description = "体力 +35　守り +4" },
            new GearInfo { Name = "星のローブ", Price = 900, MaxHp = 55, Defense = 6, Description = "体力 +55　守り +6" },
            // 風船割りの素材で作るローブ
            new GearInfo { Name = "風のローブ", MaxHp = 28, Defense = 3, Description = "体力 +28　守り +3", Recipe = new[] { 10, 3, 0, 0 } },
            new GearInfo { Name = "虹のローブ", MaxHp = 70, Defense = 8, Description = "体力 +70　守り +8（いちばん強い）", Recipe = new[] { 0, 0, 5, 3 } },
        };

        /// <summary>素材で作る装備を作れるか（素材がそろっているか）</summary>
        public static bool CanCraft(GearInfo g, Func<CraftMaterial, int> have)
        {
            if (g.Recipe == null) return false;
            for (int i = 0; i < g.Recipe.Length; i++) if (have((CraftMaterial)i) < g.Recipe[i]) return false;
            return true;
        }

        /// <summary>作るのに必要な素材の文（例：ルーン鉱石 3/8・風の結晶 0/2）</summary>
        public static string RecipeText(GearInfo g, Func<CraftMaterial, int> have)
        {
            var parts = new List<string>();
            for (int i = 0; i < g.Recipe.Length; i++)
                if (g.Recipe[i] > 0) parts.Add($"{Balloon.CraftNames[i]} {Math.Min(have((CraftMaterial)i), g.Recipe[i])}/{g.Recipe[i]}");
            return string.Join("・", parts);
        }

        public const int HerbPrice = 10, HerbHeal = 20;
        public const int BellPrice = 15, BellSteps = 60;

        // ------------------------------------------------------------------
        // タイピングの言葉
        // ------------------------------------------------------------------

        // 日本語（表示する文字, ひらがなの読み）。ランクが上がると漢字や文になる
        private static readonly (string d, string r)[] JapaneseShort =
        {
            ("まほう", "まほう"), ("すすむ", "すすむ"), ("ひろう", "ひろう"), ("つかう", "つかう"), ("かぎ", "かぎ"), ("ほし", "ほし"),
            ("いし", "いし"), ("はし", "はし"), ("まち", "まち"), ("つえ", "つえ"), ("くさ", "くさ"), ("みず", "みず"), ("たまご", "たまご"),
            ("ごーれむ", "ごーれむ"), ("あるかな", "あるかな"), ("きんか", "きんか"), ("やくそう", "やくそう"),
            // ふやした言葉
            ("かぜ", "かぜ"), ("そら", "そら"), ("うみ", "うみ"), ("やま", "やま"), ("もり", "もり"), ("かわ", "かわ"),
            ("ふね", "ふね"), ("とり", "とり"), ("さかな", "さかな"), ("はな", "はな"), ("つき", "つき"), ("たいよう", "たいよう"),
            ("くも", "くも"), ("にじ", "にじ"), ("すな", "すな"), ("ゆき", "ゆき"), ("こおり", "こおり"), ("ひかり", "ひかり"),
            ("かげ", "かげ"), ("たから", "たから"), ("ちず", "ちず"), ("かばん", "かばん"), ("くつ", "くつ"), ("ぼうし", "ぼうし"),
            ("ろーぶ", "ろーぶ"), ("けん", "けん"), ("たて", "たて"), ("とびら", "とびら"), ("まど", "まど"), ("いど", "いど"),
            ("とう", "とう"), ("しろ", "しろ"), ("むら", "むら"), ("みせ", "みせ"), ("やど", "やど"), ("ぱん", "ぱん"),
            ("りんご", "りんご"), ("みかん", "みかん"), ("ねこ", "ねこ"), ("いぬ", "いぬ"), ("うさぎ", "うさぎ"), ("かめ", "かめ"),
            ("かに", "かに"), ("ほたる", "ほたる"), ("どんぐり", "どんぐり"), ("きのこ", "きのこ"), ("はっぱ", "はっぱ"), ("いかり", "いかり"),
            ("るーん", "るーん"), ("せきばん", "せきばん"), ("ほのお", "ほのお"), ("かみなり", "かみなり"), ("ともだち", "ともだち"), ("ゆうき", "ゆうき"),
        };

        private static readonly (string d, string r)[] JapaneseLong =
        {
            ("くりかえし", "くりかえし"), ("じょうけん", "じょうけん"), ("へんすう", "へんすう"), ("めいれい", "めいれい"),
            ("じゅんばん", "じゅんばん"), ("しゅくしゃ", "しゅくしゃ"), ("としょかん", "としょかん"), ("とけいだい", "とけいだい"),
            ("すいしゃごや", "すいしゃごや"), ("かじや", "かじや"), ("ぷろぐらむ", "ぷろぐらむ"), ("るーんこーど", "るーんこーど"),
            // ふやした言葉
            ("だいぼうけん", "だいぼうけん"), ("たからさがし", "たからさがし"), ("まほうのつえ", "まほうのつえ"), ("ほしのしんでん", "ほしのしんでん"), ("かぜのたいじゅ", "かぜのたいじゅ"), ("すなのくに", "すなのくに"),
            ("みずのくに", "みずのくに"), ("きかいのみやこ", "きかいのみやこ"), ("とうだいもり", "とうだいもり"), ("ふなだいく", "ふなだいく"), ("ばしゃのぎょしゃ", "ばしゃのぎょしゃ"), ("やくそうや", "やくそうや"),
            ("よろずや", "よろずや"), ("ひみつのちず", "ひみつのちず"), ("ふしぎなせきばん", "ふしぎなせきばん"), ("きらきらのいし", "きらきらのいし"), ("あるかなずかん", "あるかなずかん"), ("ごーれむのはし", "ごーれむのはし"),
            ("しんぴのみずうみ", "しんぴのみずうみ"), ("ゆうきのしるし", "ゆうきのしるし"), ("ゆうやけのそら", "ゆうやけのそら"), ("ほしふるよる", "ほしふるよる"), ("たびのはじまり", "たびのはじまり"), ("まもりがみ", "まもりがみ"),
        };

        private static readonly (string d, string r)[] JapaneseKanji =
        {
            ("魔法", "まほう"), ("命令", "めいれい"), ("順番", "じゅんばん"), ("変数", "へんすう"), ("宿舎", "しゅくしゃ"),
            ("図書館", "としょかん"), ("時計台", "とけいだい"), ("石像", "せきぞう"), ("灯台", "とうだい"), ("宝箱", "たからばこ"),
            ("条件", "じょうけん"), ("水晶", "すいしょう"), ("星空", "ほしぞら"), ("町", "まち"),
            // ふやした言葉
            ("冒険", "ぼうけん"), ("勇気", "ゆうき"), ("仲間", "なかま"), ("地図", "ちず"), ("宝石", "ほうせき"), ("大樹", "たいじゅ"),
            ("砂漠", "さばく"), ("港町", "みなとまち"), ("神殿", "しんでん"), ("歯車", "はぐるま"), ("鍛冶屋", "かじや"), ("道具屋", "どうぐや"),
            ("薬草", "やくそう"), ("金貨", "きんか"), ("杖", "つえ"), ("船", "ふね"), ("橋", "はし"), ("森", "もり"),
            ("海", "うみ"), ("空", "そら"), ("風", "かぜ"), ("雪", "ゆき"), ("月", "つき"), ("虹", "にじ"),
            ("守り神", "まもりがみ"), ("図鑑", "ずかん"), ("看板", "かんばん"), ("依頼", "いらい"), ("旅人", "たびびと"), ("魔法使い", "まほうつかい"),
        };

        private static readonly (string d, string r)[] JapaneseSentence =
        {
            ("魔法をとなえる", "まほうをとなえる"), ("町を元にもどす", "まちをもとにもどす"), ("命令を順番にならべる", "めいれいをじゅんばんにならべる"),
            ("ゴールまでくり返す", "ごーるまでくりかえす"), ("アルカナをたおす", "あるかなをたおす"), ("宝箱を開ける", "たからばこをあける"),
            ("星を集める", "ほしをあつめる"), ("ゴーレムがすすむ", "ごーれむがすすむ"),
            // ふやした言葉
            ("杖をふりかざす", "つえをふりかざす"), ("仲間と力を合わせる", "なかまとちからをあわせる"), ("地図を広げる", "ちずをひろげる"),
            ("風の大樹をよみがえらせる", "かぜのたいじゅをよみがえらせる"), ("港に船がもどる", "みなとにふねがもどる"), ("砂漠をこえて旅をする", "さばくをこえてたびをする"),
            ("歯車がまわりだす", "はぐるまがまわりだす"), ("星空を見上げる", "ほしぞらをみあげる"), ("アルカナと友だちになる", "あるかなとともだちになる"),
            ("勇気を出してすすむ", "ゆうきをだしてすすむ"), ("もしかべなら右を向く", "もしかべならみぎをむく"), ("宝石をひろって集める", "ほうせきをひろってあつめる"),
            ("依頼をひきうける", "いらいをひきうける"), ("橋をわたって向こう岸へ", "はしをわたってむこうぎしへ"), ("守り神の声を聞く", "まもりがみのこえをきく"),
        };

        // 英語（打つ文字, 意味）
        private static readonly (string w, string m)[] EnglishWords =
        {
            ("use_item", "つかう"), ("pick_up", "ひろう"), ("use_magic", "まほう"), ("forward", "すすむ"), ("left", "左を向く"),
            ("right", "右を向く"), ("for", "くり返し"), ("if", "もし"), ("else", "でなければ"), ("while", "〜のあいだ"),
            ("range", "はんい"), ("read_sign", "看板を読む"), ("not", "〜でない"), ("pass", "何もしない"),
            ("at_goal", "ゴールに着いた"), ("wall_in_front", "前がかべ"), ("def", "関数を作る"),
            // ふやした言葉
            ("print", "表示する"), ("input", "入力する"), ("return", "もどす"), ("True", "正しい"), ("False", "まちがい"), ("and", "かつ"),
            ("or", "または"), ("in", "〜の中"), ("break", "ぬける"), ("continue", "つぎへ"), ("elif", "そうでなくもし"), ("len", "長さ"),
            ("list", "リスト"), ("count", "かぞえる"), ("step", "歩数"), ("goal", "ゴール"), ("magic", "まほう"), ("item", "アイテム"),
            ("key", "かぎ"), ("star", "ほし"), ("golem", "ゴーレム"), ("arcana", "アルカナ"), ("rune", "ルーン"), ("quest", "クエスト"),
            ("object_here", "足元にアイテム"), ("obstacle_in_front", "前にじゃまもの"), ("turn_left", "左を向く"), ("turn_right", "右を向く"),
        };

        private static readonly (string w, string m)[] EnglishLines =
        {
            ("forward()", "すすむ"), ("use_magic()", "まほう"), ("use_item()", "つかう"), ("for i in range(3):", "3回くり返す"),
            ("if obstacle_in_front():", "もし前にじゃまものなら"), ("while not at_goal():", "ゴールに着くまでくり返す"),
            ("n = read_sign()", "看板の数を n に入れる"), ("n = n - 1", "n を 1 へらす"), ("if object_here():", "もし足元にアイテムがあれば"),
            ("while n > 0:", "n が 0 より大きいあいだ"), ("left(90)", "左を向く"), ("else:", "でなければ"), ("def turn_corner():", "関数 turn_corner を作る"),
            // ふやした言葉
            ("print(\"hello\")", "hello と表示する"), ("for i in range(5):", "5回くり返す"), ("if wall_in_front():", "もし前がかべなら"),
            ("while object_here():", "足元にアイテムがあるあいだ"), ("n = n + 1", "n を 1 ふやす"), ("right(90)", "右を向く"),
            ("pick_up()", "ひろう"), ("if at_goal():", "もしゴールなら"), ("for i in range(n):", "n 回くり返す"),
            ("while not wall_in_front():", "前がかべになるまでくり返す"), ("count = 0", "count に 0 を入れる"), ("return n", "n をもどす"),
        };

        /// <summary>
        /// ランクに合わせて言葉を1つ選ぶ。
        /// 日本語：1〜5 ローマ字のお手本だけ／6〜10 ひらがな＋小さくローマ字／11〜15 ひらがなだけ／16〜20 漢字／21〜 漢字の文
        /// 英語：1〜5 単語＋意味／6〜10 単語だけ／11〜15 1行のコード／16〜 意味を見てコードを書く
        /// </summary>
        public static TypingWord PickWord(bool japanese, int rank, Random rng)
        {
            if (japanese)
            {
                (string d, string r) e;
                if (rank <= 5) e = JapaneseShort[rng.Next(JapaneseShort.Length)];
                else if (rank <= 15) e = rng.Next(2) == 0 ? JapaneseShort[rng.Next(JapaneseShort.Length)] : JapaneseLong[rng.Next(JapaneseLong.Length)];
                else if (rank <= 20) e = JapaneseKanji[rng.Next(JapaneseKanji.Length)];
                else e = rng.Next(3) == 0 ? JapaneseKanji[rng.Next(JapaneseKanji.Length)] : JapaneseSentence[rng.Next(JapaneseSentence.Length)];
                return new TypingWord { Display = e.d, Reading = e.r };
            }
            if (rank <= 10)
            {
                var w = EnglishWords[rng.Next(EnglishWords.Length)];
                return new TypingWord { Display = w.w, Reading = w.w, Meaning = rank <= 5 ? w.m : null };
            }
            var l = EnglishLines[rng.Next(EnglishLines.Length)];
            if (rank <= 15) return new TypingWord { Display = l.w, Reading = l.w, Meaning = l.m };
            // 意味だけを見て、コードを思い出して打つ
            return new TypingWord { Display = l.m, Reading = l.w, Meaning = null };
        }

        /// <summary>日本語の言葉で、お手本をどう見せるか</summary>
        public enum GuideStyle { RomajiOnly, KanaWithRomaji, KanaOnly, Kanji }

        public static GuideStyle JapaneseGuide(int rank) =>
            rank <= 5 ? GuideStyle.RomajiOnly : rank <= 10 ? GuideStyle.KanaWithRomaji : rank <= 15 ? GuideStyle.KanaOnly : GuideStyle.Kanji;
    }
}
