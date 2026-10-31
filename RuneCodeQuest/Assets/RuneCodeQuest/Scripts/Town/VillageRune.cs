using System.Collections.Generic;

namespace RuneCodeQuest
{
    public static partial class TownData
    {
        /// <summary>
        /// ルーネ村（主人公の村）：区画は 南西→南東→北東→北西 の順に開く。建物は8つ。
        /// 地図の大きさ 54x36。区画を分けるかべは 13行目と 19列目
        /// </summary>
        private static VillageInfo BuildRune() => new VillageInfo
        {
            Index = 0, Name = "ルーネ村", Kind = "はじまりの村", TileTheme = 1,
            W = 54, H = 36, StartX = 9, StartY = 23, SplitX = 19, SplitY = 13,
            WallColor = (238, 224, 190),
            AreaGate = new[] { 0, 2, 4, 6 },
            DistrictLong = new[]
            {
                "町の南西（宿舎・道具屋のあたり）", "町の南東（鍛冶屋・防具屋のあたり）",
                "町の北東（図書館・水車小屋のあたり）", "町の北西（時計台・お城のあたり）",
            },
            GrassNames = new[] { "南西の草むら", "南東の草むら", "北東の草むら", "北西の草むら" },
            // 依頼板（どれを見ても同じ依頼）。南西の噴水のそば・南東・北東・北西に1つずつ
            Boards = new[] { (11, 22), (24, 21), (39, 7), (15, 9) }, // 北東は 水車の川の東（依頼板の絵が 川に かからない所）
            // 町の宝箱（1回だけ開けられる）。区画ごとに1つ
            Chests = new[] { (1, 25), (52, 26), (52, 12), (2, 1) },
            // 北東の畑（たねをまいて、歩くと育つ）
            GardenBeds = new[] { (23, 1), (24, 1), (25, 1) },
            // 北西のかくれんぼで、宝さがしの子がかくれる場所
            HideSpots = new[] { (2, 3), (3, 1), (2, 6), (17, 10), (18, 3), (18, 9), (14, 10), (13, 12), (5, 11) },
            Buildings = new[]
            {
                new BuildingInfo { Number = 1, Name = "宿舎", Function = BuildingFunction.Inn, X = 3, Y = 15, W = 5, H = 3, FirstStage = 1, LastStage = 3, Roof = (200, 80, 70),
                    RepairedText = "宿舎で休むと、体力が全回復するようになった！ バトルで負けても、ここで目を覚ますよ。" },
                new BuildingInfo { Number = 2, Name = "道具屋", Function = BuildingFunction.ItemShop, X = 11, Y = 15, W = 5, H = 3, FirstStage = 4, LastStage = 6, Roof = (90, 160, 90),
                    RepairedText = "道具屋で、薬草や むしよけの鈴が買えるようになった！ 東の道のがれきも片づいたよ。" },
                new BuildingInfo { Number = 3, Name = "鍛冶屋", Function = BuildingFunction.WeaponShop, X = 22, Y = 15, W = 5, H = 3, FirstStage = 7, LastStage = 9, Roof = (110, 110, 130),
                    RepairedText = "鍛冶屋で、杖が買えるようになった！ 強い杖ほど、タイピングの攻撃力が上がる。" },
                new BuildingInfo { Number = 4, Name = "防具屋", Function = BuildingFunction.ArmorShop, X = 29, Y = 15, W = 5, H = 3, FirstStage = 10, LastStage = 12, Roof = (70, 110, 190),
                    RepairedText = "防具屋で、ローブが買えるようになった！ 体力と守りが上がる。北の道も通れるようになったよ。" },
                new BuildingInfo { Number = 5, Name = "図書館", Function = BuildingFunction.Library, X = 22, Y = 3, W = 6, H = 4, FirstStage = 13, LastStage = 15, Roof = (150, 90, 170),
                    RepairedText = "図書館で、タイピングの言葉（日本語・英語）を選べるようになった！" },
                new BuildingInfo { Number = 6, Name = "水車小屋", Function = BuildingFunction.Mill, X = 30, Y = 3, W = 5, H = 3, FirstStage = 16, LastStage = 18, Roof = (170, 130, 70),
                    RepairedText = "水車が回りだした！ クエストをクリアするたびに、水車小屋で金貨がもらえる。西の道も開いたよ。" },
                new BuildingInfo { Number = 7, Name = "時計台", Function = BuildingFunction.ClockTower, X = 4, Y = 2, W = 4, H = 5, FirstStage = 19, LastStage = 21, Roof = (210, 180, 60),
                    RepairedText = "時計台が動きだした！ 時計台では、前のクエストにもう一度挑戦できる。お城への門も開いたよ。" },
                new BuildingInfo { Number = 8, Name = "お城", Function = BuildingFunction.Castle, X = 10, Y = 1, W = 7, H = 4, FirstStage = 22, LastStage = 24, Roof = (90, 70, 140),
                    RepairedText = "お城がよみがえった！ 王さまが「世界の異変」について話があるそうだ。村の馬車で、となりの国「アクアリス」へ行けるようになった！" },
            },
            Npcs = new[]
            {
                new NpcInfo { Name = "師匠", X = 7, Y = 21, AppearAfter = 0, Cloth = (120, 60, 160), Lines = new[]
                {
                    "{name}、よく来た。町はアルカナにルーンコードをこわされてしまった。",
                    "噴水のそばの依頼板で依頼を受けて、建物を直す資材を集めるのじゃ。",
                    "資材がそろったら、建物の前の工事の看板を調べて「仕上げクエスト」に挑戦するんじゃ。",
                    "アルカナのかけらは、草むらのアルカナをタイピングでたおすと手に入る。",
                } },
                new NpcInfo { Name = "宿舎のおかみ", X = 8, Y = 19, AppearAfter = 1, Cloth = (220, 120, 140), Lines = new[]
                {
                    "宿舎を直してくれて ありがとう！ 疲れたら いつでも泊まっていってね。",
                    "体力が少ないときは、草むらに入る前に休むといいよ。",
                } },
                new NpcInfo { Name = "道具屋の店主", X = 17, Y = 20, AppearAfter = 2, Cloth = (90, 170, 90), Lines = new[]
                {
                    "薬草はバトル中に Tab キーで使えるよ。",
                    "むしよけの鈴を使うと、しばらくアルカナが出てこなくなる。",
                } },
                new NpcInfo { Name = "鍛冶屋の親方", X = 21, Y = 20, AppearAfter = 3, Cloth = (130, 90, 60), Lines = new[]
                {
                    "いい杖は、一文字打つたびのダメージが大きいぞ！",
                    "金貨は草むらのアルカナをたおすと手に入る。",
                } },
                new NpcInfo { Name = "図書館の司書", X = 27, Y = 9, AppearAfter = 5, Cloth = (160, 120, 200), Lines = new[]
                {
                    "クエストで星を集めると、ランクが上がって体力が増えるわ。",
                    "ランクが上がると、タイピングのお手本が少しずつ むずかしくなるの。",
                } },
                new NpcInfo { Name = "時計台の番人", X = 7, Y = 8, AppearAfter = 7, Cloth = (200, 170, 70), Lines = new[]
                {
                    "時計台では、前のクエストにもう一度挑戦できる。星3つを目指してみな。",
                } },

                // ----- 南西（はじめの村） -----
                new NpcInfo { Name = "馬車の御者", X = 13, Y = 25, Role = NpcRole.Coach, Cloth = (150, 100, 60), Lines = new[]
                {
                    "わしは馬車の御者。となりの国まで、旅人を乗せていく仕事をしておる。",
                } },
                new NpcInfo { Name = "町長", X = 6, Y = 23, Role = NpcRole.Mayor, Cloth = (120, 90, 160), Lines = new[]
                {
                    "ようこそ、見習い魔法使いさん。わしは この町の町長じゃ。",
                } },

                // ----- 南東（鍛冶屋・防具屋のあたり） -----
                new NpcInfo { Name = "風船割り屋", X = 27, Y = 21, Role = NpcRole.Training, Cloth = (200, 170, 90), Lines = new[]
                {
                    "風船割りへ ようこそ！ 1分のあいだに、言葉を打って風船をどんどん割ってね。",
                    "割った数で、杖やローブを作る素材がもらえるよ。たくさん割るほど、めずらしい素材がもらえる！",
                    "これまでに割った合計で、キャラクリの服やぼうしもプレゼント！ 宿舎の鏡で着がえられるよ。",
                } },
                new NpcInfo { Name = "資材屋", X = 37, Y = 16, Role = NpcRole.MaterialTrader, Cloth = (150, 110, 70), Lines = new[]
                {
                    "いらっしゃい！ 金貨で、木材・石材・歯車を売っているよ。ちょっと高いけどね。",
                } },
                new NpcInfo { Name = "配達屋", X = 30, Y = 25, Role = NpcRole.Courier, Cloth = (90, 130, 200), Lines = new[]
                {
                    "配達屋だよ！ 町の人に荷物を届けてくれる人を さがしているんだ。",
                    "届け先には地図のピンを立てておくね。届けたら金貨をあげる。5回届けるごとに資材もおまけ！",
                } },
                new NpcInfo { Name = "見習い鍛冶", X = 39, Y = 18, Cloth = (190, 90, 60), Lines = new[]
                {
                    "東の森の遺跡は、草むらを通らないと行けないんだ。体力に気をつけて。",
                    "風船割りの素材を持ってくると、親方が特別な杖を作ってくれるんだ。",
                } },

                // ----- 北東（図書館・水車小屋のあたり） -----
                new NpcInfo { Name = "つり人", X = 32, Y = 10, Role = NpcRole.Fishing, Cloth = (90, 140, 180), Lines = new[]
                {
                    "この池は魚がよくつれるんだ。言葉を打って、魚をつり上げてみな！ つれた魚は金貨と交換してあげるよ。",
                } },
                new NpcInfo { Name = "畑のおばあさん", X = 26, Y = 1, Role = NpcRole.Gardener, Cloth = (150, 180, 90), Lines = new[]
                {
                    "となりの畑に たねをまいてごらん。たくさん歩くと、薬草が育つよ。",
                    "冒険で歩きまわっているうちに、すくすく大きくなるからね。",
                } },
                new NpcInfo { Name = "水車小屋の粉屋", X = 29, Y = 8, Cloth = (230, 220, 200), Lines = new[]
                {
                    "水車小屋が直ってから、粉がたくさんひけるようになったよ。",
                    "クエストをクリアするほど、水車小屋にもらえる金貨がたまるんだ。",
                } },

                // ----- 北西（時計台・お城のあたり） -----
                new NpcInfo { Name = "お城の兵士", X = 11, Y = 9, Cloth = (120, 120, 150), Lines = new[]
                {
                    "お城の門は、時計台が直るまで開かないぞ。",
                    "となりの国では、アルカナを戦争の兵器にしているらしい…。この村をおそったアルカナも、そのせいかもしれん。",
                } },
                new NpcInfo { Name = "宝さがしの子", X = 4, Y = 7, Role = NpcRole.HideSeek, Cloth = (240, 160, 60), Lines = new[]
                {
                    "町のはしっこには、宝箱がかくれてるんだって！ 地図（M）で町のすみっこを見てみて。",
                    "ねえ、かくれんぼしよう！ ぼくが この北西のどこかに かくれるから、1分で見つけてね！",
                } },
            },
            QuestSpots = new[]
            {
                new SpotInfo { Name = "南西のほら穴", X = 4, Y = 33 },
                new SpotInfo { Name = "南の野原の遺跡", X = 15, Y = 33 },
                new SpotInfo { Name = "東の森の遺跡", X = 49, Y = 19 },
                new SpotInfo { Name = "南東の野原の遺跡", X = 36, Y = 34 },
                new SpotInfo { Name = "東の丘の塔", X = 46, Y = 6 },
                new SpotInfo { Name = "池のほとりの遺跡", X = 38, Y = 10 },
                new SpotInfo { Name = "北西の墓地", X = 2, Y = 3 },
                new SpotInfo { Name = "お城の裏の遺跡", X = 14, Y = 10 },
            },
            Terrain = RuneTerrain,
            Hills = RuneHills,
        };

        /// <summary>
        /// ルーネ村の高さ：町は 1段上（丘の上の村）。南の野原は低い。
        /// 東の丘・お城（へいの中）は さらに1段上
        /// </summary>
        private static void RuneHills(HeightMap h)
        {
            h.Raise(1, 1, 52, 34, 1);
            // 南の野原（南西・南東）は 低い：広い階段でおりる
            h.Raise(1, 25, 52, 34, 0);
            h.Stairs(9, 24, 11, 24);
            h.Stairs(17, 24, 17, 24);
            h.Stairs(25, 24, 27, 24);
            h.Stairs(34, 24, 35, 24);
            h.Stairs(48, 24, 49, 24);
            // 北東：東の丘（横の階段・南の広い階段・はしご）
            h.Raise(41, 1, 52, 10, 2);
            h.Stairs(41, 4);
            h.Stairs(41, 8);
            h.Stairs(46, 10, 47, 10);
            h.Ladder(51, 10);
            // 北西：お城の 高台（階段2つぶん）。1段目：お城の丘（広い階段）、2段目：へいの中（門の前の階段）
            h.Raise(8, 1, 18, 11, 2);
            h.Stairs(12, 11, 14, 11);
            h.Raise(9, 1, 17, 8, 3);
            h.Stairs(13, 8);
        }

        /// <summary>ルーネ村の地面（かべ・がれき・草むら・池・木）</summary>
        private static void RuneTerrain(char[,] g, VillageInfo v)
        {
            int W = v.W, H = v.H;
            void Rect(int x0, int y0, int x1, int y1, char c)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        g[x, y] = c;
            }

            // 区画を分けるかべ（よこ：13行目、たて：19列目）
            Rect(1, 13, W - 2, 13, '#');
            Rect(19, 1, 19, H - 2, '#');
            // 区画をつなぐ道（がれきでふさがれている）
            Rect(19, 18, 19, 19, '2');   // 南西 → 南東（道具屋が直ると開く）
            Rect(26, 13, 27, 13, '4');   // 南東 → 北東（防具屋が直ると開く）
            Rect(19, 6, 19, 7, '6');     // 北東 → 北西（水車小屋が直ると開く）

            // お城のへい（時計台が直ると門が開く）
            Rect(9, 1, 9, 7, '#');
            Rect(17, 1, 17, 7, '#');
            Rect(9, 7, 17, 7, '#');
            g[13, 7] = '7';

            // 草むら（アルカナが出る）
            Rect(1, 19, 4, 24, 'w');      // 南西
            Rect(1, 26, 18, 31, 'w');     // 南西：南の野原
            Rect(34, 19, 38, 24, 'w');    // 南東
            Rect(30, 20, 32, 24, 'w');
            Rect(41, 14, 52, 24, 'w');    // 南東：東の森
            Rect(20, 27, 52, 32, 'w');    // 南東：南の野原
            Rect(21, 9, 26, 12, 'w');     // 北東
            Rect(28, 9, 31, 12, 'w');
            Rect(37, 8, 39, 12, 'w');
            Rect(41, 2, 51, 11, 'w');     // 北東：東の丘
            Rect(1, 9, 6, 12, 'w');       // 北西
            Rect(10, 9, 17, 11, 'w');
            Rect(1, 2, 3, 8, 'w');
            Rect(4, 1, 8, 1, 'w');

            // 池・噴水
            Rect(33, 8, 36, 11, '~');
            Rect(9, 20, 10, 21, 'F');

            // 草（かざり）
            Rect(13, 22, 17, 24, ',');
            Rect(21, 22, 28, 24, ',');
            Rect(10, 12, 17, 12, ',');

            // 木
            foreach (var (x, y) in new[] { (1, 14), (18, 14), (20, 14), (38, 14), (1, 1), (18, 1), (20, 1), (38, 1), (8, 24), (37, 6), (1, 34), (18, 34), (20, 34), (52, 34), (52, 1), (40, 14), (40, 24) })
                g[x, y] = 'T';

            // 北東：水車小屋の 水車を回す川（北のはしから 池へ。橋を1つ）
            River(g, 35, 1, 36, 7, (35, 2), (36, 2));
            // 南東：川と 2つの橋（東の森・南東の野原へ）
            River(g, 44, 14, 45, 34, (44, 20), (45, 20), (44, 30), (45, 30));
        }
    }
}
