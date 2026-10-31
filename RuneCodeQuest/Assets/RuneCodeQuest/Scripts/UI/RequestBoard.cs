using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 依頼板の画面。
    /// ・町づくりの依頼（本線）：学ぶ順番どおりに1つずつ。クリアすると資材
    /// ・おまけの依頼：次の本線より前のステージだけ。資材が多めにもらえる
    /// ・アルカナ退治の依頼：決まった草むらでアルカナをたおす。アルカナのかけら・金貨
    /// 下には、いま直している建物に必要な資材が出る。
    /// </summary>
    public class RequestBoard
    {
        public System.Action OnClose;
        /// <summary>クエストの依頼を引き受けたとき（ステージ、依頼の種類）</summary>
        public System.Action<int, RequestKind> OnStartQuest;
        /// <summary>引き受けていた依頼をやめたとき</summary>
        public System.Action OnCancelQuest;

        private string note;

        public void Open()
        {
            note = null;
            showDone = false;
        }

        private static bool Played(int s) => Progress.StatusOf(s) != Progress.NotPlayed;
        private static bool Repaired(int number) => TownMap.IsRepaired(TownData.Buildings[number - 1]);

        public void Draw()
        {
            UIKit.Nav("board", true);
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                OnClose?.Invoke();
                return;
            }

            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.6f));
            var p = new Rect(90, 24, 1100, 660);
            UIKit.Panel(p);
            UIKit.Texture(new Rect(p.x + 26, p.y + 16, 44, 44), SpriteFactory.TextureOf(TownArt.Board));
            UIKit.Label(new Rect(p.x + 80, p.y + 12, 500, 50), "依頼板", UIKit.Heading);
            if (UIKit.ColorButton(new Rect(p.xMax - 140, p.y + 16, 120, 46), "とじる", new Color(0.8f, 0.4f, 0.4f)))
            {
                OnClose?.Invoke();
                return;
            }

            // 上のタブ：今受けられる依頼／引き受けて終わった依頼
            int doneCount = DoneStages().Count;
            if (UIKit.ColorButton(new Rect(p.x + 330, p.y + 16, 200, 46), "依頼", showDone ? Color.gray : UIKit.AccentColor, UIKit.Button, true, primary: !showDone))
                showDone = false;
            if (UIKit.ColorButton(new Rect(p.x + 540, p.y + 16, 300, 46), $"引き受け済み（{doneCount}）", showDone ? UIKit.AccentColor : Color.gray, UIKit.Button, true, primary: showDone))
                showDone = true;

            if (showDone) DrawDoneList(new Rect(p.x + 24, p.y + 74, p.width - 48, p.height - 130));
            else
            {
                float y = p.y + 74;
                DrawMain(new Rect(p.x + 24, y, p.width - 48, 132));
                DrawExtra(new Rect(p.x + 24, y + 142, p.width - 48, 132));
                DrawHunt(new Rect(p.x + 24, y + 284, p.width - 48, 132));
                // ステージ一覧で もう一度挑戦を受けているときは、資材のかわりに その依頼を出す（ここで やめられる）
                if (ReplayActive) DrawReplay(new Rect(p.x + 24, y + 428, p.width - 48, 108));
                else DrawMaterials(new Rect(p.x + 24, y + 428, p.width - 48, 100));
            }
            if (note != null) UIKit.Label(new Rect(p.x + 24, p.yMax - 48, p.width - 48, 40), UIKit.Colorize(note, UIKit.WarnColor), UIKit.Small);
            UIKit.HelpBar("矢印キー・Tab でえらんで Enter（マウスでもOK）　Esc: とじる");
        }

        // ----- 引き受け済み（終わった依頼の一覧） -----

        private bool showDone;
        private Vector2 doneScroll;

        /// <summary>終えたステージ（クリアしたもの・解説を見て通過したもの）</summary>
        private static List<int> DoneStages()
        {
            var list = new List<int>();
            for (int s = 1; s <= QuestDatabase.LevelCount; s++) if (Played(s)) list.Add(s);
            return list;
        }

        private void DrawDoneList(Rect r)
        {
            UIKit.Fill(r, UIKit.SubPanelColor);
            var done = DoneStages();
            string head = $"終えた依頼 {done.Count}こ（クリア {Progress.ClearedCount}・通過 {done.Count - Progress.ClearedCount}）　" +
                          $"アルカナ退治 {Progress.HuntsDone}回　星 ★{Progress.TotalStars}";
            UIKit.Label(new Rect(r.x + 16, r.y + 6, r.width - 32, 34), head, UIKit.Body);
            var view = new Rect(r.x + 8, r.y + 46, r.width - 16, r.height - 54);
            if (done.Count == 0)
            {
                UIKit.Label(new Rect(view.x + 10, view.y + 10, view.width - 20, 60), "まだ終えた依頼はないよ。「依頼」のタブから引き受けてみよう！", UIKit.Body);
                return;
            }
            const float rowH = 46;
            doneScroll = UIKit.BeginScrollView(view, doneScroll, new Rect(0, 0, view.width - 20, done.Count * rowH));
            // 新しく終えたものが上（ステージの大きい順）
            for (int i = 0; i < done.Count; i++)
            {
                int s = done[done.Count - 1 - i];
                var row = new Rect(0, i * rowH, view.width - 20, rowH - 4);
                UIKit.Fill(row, i % 2 == 0 ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.1f));
                var th = QuestTheme.Of(LevelFactory.TemplateOf(s));
                bool cleared = Progress.StatusOf(s) == Progress.Cleared;
                string kind = Rebuild.IsFinish(s) ? "仕上げ" : Rebuild.IsExtra(s) ? "おまけ" : "町づくり";
                Color kindColor = Rebuild.IsFinish(s) ? UIKit.SuccessColor : Rebuild.IsExtra(s) ? new Color(0.7f, 0.6f, 1f) : UIKit.AccentColor;
                int stars = Progress.StarsOf(s);
                UIKit.Fill(new Rect(row.x + 6, row.y + 8, 86, 26), kindColor * new Color(1, 1, 1, 0.8f));
                UIKit.Label(new Rect(row.x + 6, row.y + 8, 86, 26), kind, UIKit.SmallCenter);
                UIKit.Label(new Rect(row.x + 100, row.y + 4, 640, 36), $"ステージ{s}「{th.Title}」（{th.Area}）", UIKit.Body);
                UIKit.Label(new Rect(row.xMax - 250, row.y + 4, 240, 36),
                    cleared ? UIKit.Colorize(new string('★', stars) + new string('☆', 3 - stars), new Color(1f, 0.85f, 0.2f)) + "　クリア"
                            : UIKit.Colorize("通過（解説を見た）", Color.gray), UIKit.Body);
            }
            UIKit.EndScrollView();
        }

        // ----- 町づくりの依頼（本線） -----

        private void DrawMain(Rect r)
        {
            int next = Rebuild.NextMain(Played);
            if (next == 0)
            {
                Card(r, "町づくり", UIKit.AccentColor, "町づくりの依頼は すべて終わった！", "時計台で、星3つを目指して もう一度挑戦できるよ。", null);
                return;
            }
            int nv = TownData.VillageOfStage(next);
            if (nv != TownData.Index)
            {
                // 次の依頼は ほかの村にある
                var to = TownData.Villages[nv];
                Card(r, "町づくり", UIKit.AccentColor,
                    nv > TownData.Index ? "この村の町づくりの依頼は すべて終わった！" : $"「{to.Name}」の町づくりが まだ残っている",
                    $"次の依頼は「{to.Name}」にある。馬車の御者に話しかけて、{to.Name}へ行こう。", null);
                return;
            }
            var b = TownData.BuildingOfStage(next);
            if (!TownMap.StoryGateOpen(b))
            {
                // その建物のストーリークエストを終えると、依頼が受けられる
                var q = StoryQuests.GateOf(b);
                Card(r, "ストーリー", new Color(1f, 0.6f, 1f), $"先に ストーリークエスト「{q.Title}」を進めよう",
                    $"やること：{q.Goal}\n終わると「{b.Name}」の町づくりの依頼が 受けられるようになる。", null);
                return;
            }
            if (Rebuild.IsFinish(next))
            {
                bool ready = Rebuild.HasEnough(b, Progress.MaterialCount);
                Card(r, "仕上げ", UIKit.SuccessColor, $"「{b.Name}」の仕上げクエスト",
                    ready ? $"資材がそろった！ {b.Name}の前の工事の看板を調べて、仕上げクエストに挑戦しよう。"
                          : $"{b.Name}の依頼は全部終わった。下の資材がそろったら、{b.Name}の工事の看板で仕上げクエストだ。",
                    null);
                return;
            }
            if (QuestCard(r, "町づくり", UIKit.AccentColor, next, RequestKind.Main))
                OnStartQuest?.Invoke(next, RequestKind.Main);
        }

        // ----- おまけの依頼 -----

        private void DrawExtra(Rect r)
        {
            int extra = Rebuild.NextExtra(Played);
            if (extra == 0)
            {
                Card(r, "おまけ", new Color(0.6f, 0.5f, 0.9f), "今は おまけの依頼はない",
                    "町づくりの依頼を進めると、また出てくるよ。おまけの依頼は「しばり」つきの むずかしい問題で、資材が多めにもらえる。", null);
                return;
            }
            if (QuestCard(r, "おまけ", new Color(0.6f, 0.5f, 0.9f), extra, RequestKind.Extra))
                OnStartQuest?.Invoke(extra, RequestKind.Extra);
        }

        /// <summary>
        /// クエストの依頼のカード。引き受けたら true。
        /// 受けられるクエストの依頼は1つずつ（受けている依頼は「やめる」ことができる）
        /// </summary>
        private bool QuestCard(Rect r, string tag, Color tagColor, int stage, RequestKind kind)
        {
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            var m = Rebuild.MaterialOf(stage);
            int min = Rebuild.ClearReward(kind, 1), max = Rebuild.ClearReward(kind, 3);
            string title = $"{Rebuild.GiverOf(stage)}の依頼「{th.Title}」（ステージ{stage}）";
            string reward = UIKit.Colorize($"ほうび：{Rebuild.MaterialNames[(int)m]} {min}〜{max}こ（星が多いほど たくさん）", UIKit.WarnColor);
            if (kind == RequestKind.Extra)
                reward = UIKit.Colorize("しばりあり：ブロックを置ける数が決まっている（こえて置くと失敗）", UIKit.ErrorColor) + "\n" + reward;
            int active = Progress.ActiveQuest;
            if (active == stage)
            {
                var spot = TownData.QuestSpots[Mathf.Clamp(Progress.ActiveSpot, 0, TownData.QuestSpots.Length - 1)];
                if (Card(r, tag, tagColor, title + "　引き受け中",
                        $"お話：「{th.Area}」{th.Story}\n目的地「{spot.Name}」の遺跡の入り口へ行くと、クエストが始まる（黄色い矢印の印）。\n" + reward,
                        "やめる", TownArt.MaterialIcon(m)))
                {
                    Progress.ActiveQuest = 0;
                    note = "依頼をやめた。また引き受けることもできるよ。";
                    OnCancelQuest?.Invoke();
                }
                return false;
            }
            bool busy = active > 0 && Progress.ActiveQuestKind != RequestKind.Replay; // もう一度挑戦は、依頼を引き受けると 取り消し
            return Card(r, tag, tagColor, title,
                Rebuild.RequestText(stage) + "\n" + (busy ? UIKit.Colorize("（いま ほかの依頼を引き受けている。先にそちらを終わらせよう）", Color.gray) : reward),
                "引き受ける", TownArt.MaterialIcon(m), !busy);
        }

        // ----- もう一度挑戦（ステージ一覧で受けた依頼） -----

        private static bool ReplayActive => Progress.ActiveQuest > 0 && Progress.ActiveQuestKind == RequestKind.Replay;

        private void DrawReplay(Rect r)
        {
            int stage = Progress.ActiveQuest;
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            var spot = TownData.QuestSpots[Mathf.Clamp(Progress.ActiveSpot, 0, TownData.QuestSpots.Length - 1)];
            if (Card(r, "もう一度", new Color(0.3f, 0.7f, 0.8f), $"ステージ{stage}「{th.Title}」　引き受け中",
                    $"目的地「{spot.Name}」の遺跡の入り口へ行くと、クエストが始まる（！の印）。\n" +
                    UIKit.Colorize("ほうび：クリアで資材1こ（星3つを目指そう）", UIKit.WarnColor),
                    "やめる", TownArt.MaterialIcon(Rebuild.MaterialOf(stage))))
            {
                // やめたら 依頼板からも消える（また受けるときは ステージ一覧から）
                Progress.ActiveQuest = 0;
                note = "もう一度挑戦を やめた。また挑戦するときは、ステージ一覧から えらんでね。";
                OnCancelQuest?.Invoke();
            }
        }

        // ----- アルカナ退治 -----

        private void DrawHunt(Rect r)
        {
            var tagColor = new Color(0.85f, 0.4f, 0.4f);
            int area = Progress.HuntArea;
            if (area >= 0)
            {
                bool cancel = Card(r, "退治", tagColor, $"{Rebuild.AreaNames[area]}のアルカナを {Rebuild.HuntTarget}体たおせ（{Progress.HuntCount} / {Rebuild.HuntTarget}）",
                    $"{Rebuild.AreaNames[area]}を歩くと、すぐにアルカナが出てくる（むしよけの鈴を使っていても出る）。\n" +
                    UIKit.Colorize($"ほうび：アルカナのかけら {Rebuild.HuntShards}こ・金貨 {Rebuild.HuntCoins(area)}枚", UIKit.WarnColor),
                    "やめる", TownArt.MaterialIcon(Material.BugShard));
                if (cancel)
                {
                    Progress.HuntArea = -1;
                    note = "退治の依頼をやめた。";
                }
                return;
            }
            int offer = Rebuild.HuntAreaOffer(Progress.HuntsDone, Repaired);
            if (Card(r, "退治", tagColor, $"{Rebuild.AreaNames[offer]}のアルカナを {Rebuild.HuntTarget}体たおせ",
                    "アルカナが草むらで あばれている！ タイピングで追いはらってくれ。\n" +
                    UIKit.Colorize($"ほうび：アルカナのかけら {Rebuild.HuntShards}こ・金貨 {Rebuild.HuntCoins(offer)}枚（1体たおすごとに かけら1こ も もらえる）", UIKit.WarnColor),
                    "引き受ける", TownArt.MaterialIcon(Material.BugShard)))
            {
                Progress.HuntArea = offer;
                note = $"退治の依頼を引き受けた！ {Rebuild.AreaNames[offer]}へ行こう。";
            }
        }

        // ----- 今の建物に必要な資材 -----

        private static void DrawMaterials(Rect r)
        {
            UIKit.Fill(r, new Color(0.12f, 0.16f, 0.12f));
            BuildingInfo target = null;
            foreach (var b in TownData.Buildings)
                if (!TownMap.IsRepaired(b)) { target = b; break; }
            if (target == null)
            {
                UIKit.Label(new Rect(r.x + 16, r.y, r.width - 32, r.height), "この村の建物は すべて直った！ あまった資材は、次の村で使おう。", UIKit.Body);
                return;
            }
            UIKit.Label(new Rect(r.x + 16, r.y + 4, r.width - 32, 34),
                $"「{target.Name}」に必要な資材（本線の依頼 {TownMap.MainDone(target)} / {Rebuild.MainStages(target).Count}）", UIKit.Small);
            DrawNeeds(new Rect(r.x + 16, r.y + 44, r.width - 32, 48), target);
        }

        /// <summary>必要な資材を アイコン・持っている数 / 必要な数 で並べる</summary>
        public static void DrawNeeds(Rect r, BuildingInfo b)
        {
            var need = Rebuild.Needs(b);
            float x = r.x;
            for (int i = 0; i < Rebuild.MaterialKinds; i++)
            {
                if (need[i] == 0) continue;
                var m = (Material)i;
                int have = Progress.MaterialCount(m);
                UIKit.Texture(new Rect(x, r.y + 6, 36, 36), SpriteFactory.TextureOf(TownArt.MaterialIcon(m)));
                string text = $"{Rebuild.MaterialNames[i]} {Mathf.Min(have, need[i])}/{need[i]}";
                UIKit.Label(new Rect(x + 42, r.y, 220, r.height), have >= need[i] ? UIKit.Colorize(text + " OK", UIKit.SuccessColor) : text, UIKit.Body);
                x += 262;
            }
        }

        /// <summary>依頼のカード1枚。ボタンが押されたら true</summary>
        private static bool Card(Rect r, string tag, Color tagColor, string title, string body, string button, Sprite icon = null, bool enabled = true)
        {
            UIKit.Fill(r, UIKit.SubPanelColor);
            UIKit.Fill(new Rect(r.x, r.y, 110, 34), tagColor);
            UIKit.Label(new Rect(r.x, r.y, 110, 34), tag, UIKit.SmallCenter);
            float textX = r.x + 16;
            if (icon != null)
            {
                UIKit.Texture(new Rect(r.x + 26, r.y + 50, 56, 56), SpriteFactory.TextureOf(icon));
                textX = r.x + 100;
            }
            UIKit.Label(new Rect(r.x + 124, r.y, r.width - 360, 36), title, UIKit.Body);
            UIKit.Label(new Rect(textX, r.y + 40, r.width - (textX - r.x) - 230, r.height - 44), body, UIKit.Small);
            if (button == null) return false;
            return UIKit.ColorButton(new Rect(r.xMax - 210, r.y + 40, 194, 56), button,
                button == "やめる" ? Color.gray : UIKit.AccentColor, UIKit.Button, enabled, primary: enabled && button != "やめる");
        }
    }
}
