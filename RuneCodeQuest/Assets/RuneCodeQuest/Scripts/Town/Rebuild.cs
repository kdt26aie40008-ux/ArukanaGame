using System;
using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>町の再建に使う資材</summary>
    public enum Material { Wood, Stone, Gear, BugShard }

    /// <summary>クエストをどの依頼として受けたか</summary>
    public enum RequestKind { Main, Extra, Finish, Replay }

    /// <summary>
    /// 町の再建のルール（Unity の機能は使っていません）。
    ///
    /// ・100ステージは 6つの村の建物に分かれている（建物1つに3〜4ステージ。ルーネ村 1〜24、アクアリス 25〜39 … ステラリア 85〜100）
    /// ・ステージは「町づくりの依頼（本線）」と「おまけの依頼」に分かれる
    ///   本線は学ぶ順番どおりに1つずつ出てくる。新しい命令が出てくるステージと、建物の仕上げは必ず本線
    /// ・依頼をクリアすると資材（木材・石材・歯車）がもらえる。アルカナのかけらはタイピングバトルで手に入る
    /// ・建物の本線の依頼を全部終えて資材がそろったら、建物の看板から「仕上げクエスト」（その建物の最後のステージ）
    /// ・仕上げクエストを終えると、資材を使って建物が完成する
    /// </summary>
    public static class Rebuild
    {
        public const int MaterialKinds = 4;
        public static readonly string[] MaterialNames = { "木材", "石材", "歯車", "アルカナのかけら" };

        /// <summary>新しい命令が出てくるステージ（おまけの依頼にはしない）</summary>
        private static readonly HashSet<int> NewCommandStages = new HashSet<int> { 1, 3, 5, 7, 13, 21, 25, 27, 32, 43, 61, 68, 74, 81, 86 };

        /// <summary>
        /// おまけの依頼のステージ（クリアしなくても先へ進める）。
        /// 依頼が4つ以上ある建物だけ、2つ目（新しい命令のステージなら その次）を1つ。仕上げ（建物の最後）は入れない
        /// </summary>
        private static readonly HashSet<int> Extras = MakeExtras();

        private static HashSet<int> MakeExtras()
        {
            var set = new HashSet<int>();
            foreach (var b in TownData.AllBuildings)
            {
                if (b.StageCount < 4) continue;
                for (int s = b.FirstStage + 1; s < b.LastStage; s++)
                    if (!NewCommandStages.Contains(s)) { set.Add(s); break; }
            }
            return set;
        }

        public static bool IsExtra(int stage) => Extras.Contains(stage);

        public static bool IsFinish(int stage)
        {
            foreach (var b in TownData.AllBuildings) if (b.LastStage == stage) return true;
            return false;
        }

        /// <summary>その建物の本線の依頼（仕上げはふくまない）</summary>
        public static List<int> MainStages(BuildingInfo b)
        {
            var list = new List<int>();
            for (int s = b.FirstStage; s < b.LastStage; s++) if (!IsExtra(s)) list.Add(s);
            return list;
        }

        /// <summary>
        /// そのステージの依頼でもらえる資材。木材→石材→歯車 の順に回る
        /// （歯車はルーネ村の鍛冶屋の工事から。はじめの2つの建物は木材と石材だけ）
        /// </summary>
        public static Material MaterialOf(int stage)
        {
            var b = TownData.BuildingOfStage(stage);
            bool early = b.Village == 0 && b.Number <= 2;
            int kinds = early ? 2 : 3;
            int order = IsExtra(stage) ? stage : MainStages(b).IndexOf(stage) + b.Number - 1;
            return (Material)(Math.Max(0, order) % kinds);
        }

        /// <summary>その建物の完成に必要な資材（本線の依頼1つにつき3こ + アルカナのかけら）</summary>
        public static int[] Needs(BuildingInfo b)
        {
            var need = new int[MaterialKinds];
            foreach (int s in MainStages(b)) need[(int)MaterialOf(s)] += 3;
            need[(int)Material.BugShard] = b.Number + 1 + b.Village;
            return need;
        }

        public static bool HasEnough(BuildingInfo b, Func<Material, int> have)
        {
            var need = Needs(b);
            for (int i = 0; i < MaterialKinds; i++) if (have((Material)i) < need[i]) return false;
            return true;
        }

        /// <summary>依頼をクリアしたときにもらえる資材の数（星が多いほど多い）</summary>
        public static int ClearReward(RequestKind kind, int stars) =>
            kind == RequestKind.Extra ? 3 + stars : kind == RequestKind.Main ? 2 + stars : 1;

        /// <summary>次の本線の依頼（まだ遊んでいない、いちばん小さい本線のステージ）。全部終わっていれば 0</summary>
        public static int NextMain(Func<int, bool> played)
        {
            for (int s = 1; s <= QuestDatabase.LevelCount; s++)
                if (!IsExtra(s) && !played(s)) return s;
            return 0;
        }

        /// <summary>
        /// 今いる村で受けられるおまけの依頼。学ぶ順番がくずれないように、次の本線より前のステージだけ。なければ 0
        /// </summary>
        public static int NextExtra(Func<int, bool> played)
        {
            int limit = NextMain(played);
            if (limit == 0) limit = QuestDatabase.LevelCount + 1;
            var v = TownData.Current;
            for (int s = v.FirstStage; s < limit && s <= v.LastStage; s++)
                if (IsExtra(s) && !played(s)) return s;
            return 0;
        }

        // ------------------------------------------------------------------
        // 依頼の文章
        // ------------------------------------------------------------------

        public static readonly string[] Givers = { "大工さん", "石工さん", "発電所の技師" };

        private static readonly string[] GiverLines =
        {
            "家を建てる木材が足りないんだ。",
            "かべを直す石材がほしい。",
            "町の発電所を動かす歯車が足りない！",
        };

        public static string GiverOf(int stage) => Givers[(int)MaterialOf(stage) % Givers.Length];

        /// <summary>依頼の説明（だれが・どこで・何をすると・何がもらえるか）</summary>
        public static string RequestText(int stage)
        {
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            return $"{GiverLines[(int)MaterialOf(stage) % GiverLines.Length]}" +
                   $"「{th.Area}」で{QuestTheme.GoalPlain[(int)th.Goal]}と、{MaterialNames[(int)MaterialOf(stage)]}が見つかるらしい。";
        }

        /// <summary>
        /// そのクエストの目的地（遺跡の入り口）。行ける区画の中から、ステージごとに決まる。
        /// いちばん新しく行けるようになった区画が選ばれやすい（新しい草むらを通るように）
        /// </summary>
        public static SpotInfo DestinationOf(int stage, Func<int, bool> repaired)
        {
            var open = new List<SpotInfo>();
            int newest = 0;
            foreach (var sp in TownData.QuestSpots)
            {
                if (!AreaOpen(sp.Area, repaired)) continue;
                open.Add(sp);
                newest = Math.Max(newest, sp.Area);
            }
            // 新しい区画の目的地は2回入れて、選ばれやすくする
            foreach (var sp in TownData.QuestSpots)
                if (sp.Area == newest && newest > 0) open.Add(sp);
            return open[(stage * 7 + 3) % open.Count];
        }

        // ------------------------------------------------------------------
        // アルカナ退治の依頼
        // ------------------------------------------------------------------

        /// <summary>退治の依頼の草むらの名前（今いる村の、区画ごと）</summary>
        public static string[] AreaNames => TownData.Current.GrassNames;

        /// <summary>退治の依頼でたおす数</summary>
        public const int HuntTarget = 3;

        /// <summary>その区画に行けるか（がれきが片づいているか）。repaired(n) は n 番の建物が直っているか</summary>
        public static bool AreaOpen(int area, Func<int, bool> repaired)
        {
            int gate = TownData.Current.AreaGate[area];
            return gate == 0 || repaired(gate);
        }

        /// <summary>次に出す退治の依頼の区画（行ける区画を順番に回る）</summary>
        public static int HuntAreaOffer(int huntsDone, Func<int, bool> repaired)
        {
            var open = new List<int>();
            for (int a = 0; a < AreaNames.Length; a++) if (AreaOpen(a, repaired)) open.Add(a);
            // いちばん新しく行けるようになった区画から
            return open[open.Count - 1 - huntsDone % open.Count];
        }

        public static int HuntShards => 3;
        public static int HuntCoins(int area) => 15 * (area + 1) + 20 * TownData.Index;
    }
}
