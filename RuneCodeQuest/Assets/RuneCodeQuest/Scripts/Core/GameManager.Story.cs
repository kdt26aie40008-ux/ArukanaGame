using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：ストーリークエスト（StoryQuests.cs のデータ）。
    /// ・村の建物が直ると、その村のストーリークエストが 順番に始まる（師匠が教えてくれる）
    /// ・話を聞くクエスト：頭に「！」が出ている町の人に 話しかける
    /// ・調べるクエスト：キラキラの印の場所を調べる（アルカナが出たら、たおすとクリア）
    /// ・村のストーリークエストを全部終えて、さいごの建物も直すと、次の国へ旅立てる
    /// </summary>
    public partial class GameManager
    {
        private StoryQuest storyBattle; // 調べていて出てきたアルカナのバトル中（たおすとクリア）
        private readonly System.Random storyRng = new System.Random();

        private static bool StoryAvailable(StoryQuest q) =>
            q.After == 0 || TownMap.IsRepaired(TownData.Villages[q.Village].Buildings[q.After - 1]);

        /// <summary>今いる村で 進めているストーリークエスト（順番に1つずつ。なければ null）</summary>
        private static StoryQuest ActiveStory()
        {
            foreach (var q in StoryQuests.Of(TownData.Index))
            {
                if (Progress.StoryQuestDone(q.Id)) continue;
                return StoryAvailable(q) ? q : null;
            }
            return null;
        }

        /// <summary>話を聞くクエストで、まだ話していない相手か</summary>
        private static bool IsStoryTarget(NpcInfo n)
        {
            var q = ActiveStory();
            if (q == null || q.Kind != StoryKind.Talk) return false;
            int i = System.Array.IndexOf(q.Targets, n.Name);
            return i >= 0 && (Progress.StoryTalked & (1 << i)) == 0;
        }

        /// <summary>
        /// 次の国へ旅立つときか（この村のストーリークエストが終わって、次の依頼が ほかの行ける村にある）。
        /// このときは 馬車の御者に「！」をつける
        /// </summary>
        private static bool TravelTime()
        {
            if (ActiveStory() != null) return false;
            int target = NextWorkVillage();
            return target != TownData.Index && TownMap.VillageOpen(target);
        }

        /// <summary>
        /// 次は 依頼板で 町づくりの依頼を受けるときか（ストーリークエストが終わっていて、
        /// この村の 次の依頼が 受けられる。仕上げは 工事の看板からなので のぞく）。このときは 依頼板に「！」
        /// </summary>
        private static bool BoardTime()
        {
            if ((Progress.ActiveQuest > 0 && Progress.ActiveQuestKind != RequestKind.Replay) || ActiveStory() != null) return false;
            int next = Rebuild.NextMain(s => Progress.StatusOf(s) != Progress.NotPlayed);
            if (next == 0 || TownData.VillageOfStage(next) != TownData.Index || Rebuild.IsFinish(next)) return false;
            return TownMap.StoryGateOpen(TownData.BuildingOfStage(next));
        }

        /// <summary>行ける区画にある 依頼板（「！」をつける）</summary>
        private IEnumerable<Vector2Int> OpenBoards()
        {
            foreach (var (bx, by) in TownData.Boards)
                if (Rebuild.AreaOpen(TownData.AreaOf(bx, by), n => town.Repaired(TownData.Buildings[n - 1])))
                    yield return new Vector2Int(bx, by);
        }

        /// <summary>「！」をつける町の人（ストーリーで話を聞く人・旅立つときの馬車の御者）</summary>
        private static bool IsMarkedNpc(NpcInfo n) => IsStoryTarget(n) || (n.Role == NpcRole.Coach && TravelTime());

        /// <summary>まだ見ていない ストーリークエストの始まりの話を出す。出したら true</summary>
        private bool AnnounceStory()
        {
            var q = ActiveStory();
            if (q == null || Progress.StoryAnnounced(q.Id)) return false;
            Progress.MarkStoryAnnounced(q.Id);
            RefreshStoryMarker();
            var lines = new List<string> { $"ストーリークエスト「{q.Title}」が はじまった！" };
            lines.AddRange(q.Start);
            lines.Add($"やること：{q.Goal}");
            var unlock = UnlockOf(q);
            if (unlock != null) lines.Add($"（このクエストを終えると、依頼板で「{unlock.Name}」の依頼が 受けられるようになる）");
            dialog.Show("師匠", lines.ToArray());
            return true;
        }

        /// <summary>調べる場所の印を、今のストーリークエストに合わせる</summary>
        private void RefreshStoryMarker()
        {
            var q = ActiveStory();
            town.SetStoryMarker(q != null && q.Kind == StoryKind.Investigate ? new Vector2Int(q.X, q.Y) : (Vector2Int?)null);
        }

        private bool NearStorySpot(Vector2 p)
        {
            var q = ActiveStory();
            return q != null && q.Kind == StoryKind.Investigate && Vector2.Distance(p, town.ToWorld(q.X, q.Y)) < 1.1f;
        }

        /// <summary>調べる場所の近くなら調べる。調べたら true</summary>
        private bool TryInvestigate(Vector2 p)
        {
            if (!NearStorySpot(p)) return false;
            var q = ActiveStory();
            KeyInput.Clear();
            Progress.MarkStoryAnnounced(q.Id);
            if (q.Battle)
                dialog.Show(q.SpotName, q.Look, () =>
                {
                    storyBattle = q;
                    battleArea = -1;
                    var list = BattleData.FieldByRegion[BattleData.ClampRegion(q.Village)];
                    StartBattle(BattleData.Scaled(list[storyRng.Next(list.Length)], PlayerStats.Rank));
                });
            else dialog.Show(q.SpotName, q.Look, () => CompleteStory(q));
            return true;
        }

        /// <summary>話を聞くクエストの相手なら、その人の話をする。話したら true</summary>
        private bool TryStoryTalk(NpcInfo npc)
        {
            if (!IsStoryTarget(npc)) return false;
            var q = ActiveStory();
            int i = System.Array.IndexOf(q.Targets, npc.Name);
            Progress.MarkStoryAnnounced(q.Id); // 始まりの話より先に 話しかけたときも
            Progress.StoryTalked |= 1 << i;
            int left = 0;
            for (int k = 0; k < q.Targets.Length; k++) if ((Progress.StoryTalked & (1 << k)) == 0) left++;
            var lines = new List<string>(q.TalkLines[i]);
            if (left > 0) lines.Add($"（ストーリークエスト「{q.Title}」：あと {left}人に 話を聞こう）");
            dialog.Show(npc.Name, lines.ToArray(), left == 0 ? () => CompleteStory(q) : (System.Action)null);
            return true;
        }

        /// <summary>ストーリークエストを終えた：ほうびと お話。次のクエストがあれば始める</summary>
        private void CompleteStory(StoryQuest q)
        {
            if (Progress.StoryQuestDone(q.Id)) return;
            Progress.MarkStoryQuestDone(q.Id);
            Progress.StoryTalked = 0;
            Progress.AddItem(ItemKind.Coin, q.Coins);
            Progress.AddItem(ItemKind.Herb, 1);
            RefreshStoryMarker();
            AutoSave();
            var lines = new List<string>(q.Done);
            lines.Add($"ストーリークエスト「{q.Title}」クリア！ 金貨 {q.Coins}枚 と 薬草 1こ を手に入れた！");
            var unlock = UnlockOf(q);
            if (unlock != null) lines.Add($"依頼板で「{unlock.Name}」の 町づくりの依頼が 受けられるようになった！");
            var v = TownData.Current;
            if (v.Index + 1 < TownData.VillageCount && TownMap.VillageOpen(v.Index + 1))
                lines.Add($"馬車で 次の国「{TownData.Villages[v.Index + 1].Name}」へ行けるようになった！");
            lines.Add(ObjectiveText());
            dialog.Show("師匠", lines.ToArray(), () => AnnounceStory());
        }

        /// <summary>そのストーリークエストを終えると 依頼が受けられるようになる建物（旅立ちのクエストなら null）</summary>
        private static BuildingInfo UnlockOf(StoryQuest q)
        {
            var list = TownData.Villages[q.Village].Buildings;
            return q.After < list.Length ? list[q.After] : null;
        }

        /// <summary>
        /// 次に行く所（自動のピンを立てる場所）。町では：依頼の目的地 → ストーリーの調べる場所・話を聞く人
        /// → 依頼板 → 馬車の御者 → 仕上げの工事の看板。遺跡では：クエストを まだしていなければ ゴーレム
        /// </summary>
        private Vector2Int? QuestTarget()
        {
            if (area != null)
            {
                // もう一度挑戦で来たときも、ゴーレムに「！」
                bool replayHere = areaKind == RequestKind.Replay && Progress.ActiveQuest == area.Stage;
                if (area.IsInn || (Progress.StatusOf(area.Stage) != Progress.NotPlayed && !replayHere)) return null;
                return new Vector2Int(area.Layout.GolemX, area.Layout.GolemY);
            }
            if (town == null) return null;
            if (town.Destination != null) return new Vector2Int(town.Destination.X, town.Destination.Y);
            var q = ActiveStory();
            if (q != null)
            {
                if (q.Kind == StoryKind.Investigate) return new Vector2Int(q.X, q.Y);
                foreach (var n in town.ActiveNpcs()) if (IsStoryTarget(n)) return new Vector2Int(n.X, n.Y);
                return null;
            }
            if (BoardTime())
            {
                Vector2Int? best = null; float bd = float.MaxValue; Vector2 me = player.transform.position;
                foreach (var b in OpenBoards()) { float d = Vector2.Distance(me, town.ToWorld(b.x, b.y)); if (d < bd) { bd = d; best = b; } }
                return best;
            }
            if (TravelTime())
                foreach (var n in town.ActiveNpcs()) if (n.Role == NpcRole.Coach) return new Vector2Int(n.X, n.Y);
            // 資材がそろった建物：工事の看板（入り口）
            foreach (var b in TownData.Buildings)
            {
                if (town.Repaired(b)) continue;
                if (town.CanWorkOn(b) && TownMap.StoryGateOpen(b) && Rebuild.MainStages(b).Count == TownMap.MainDone(b) && Rebuild.HasEnough(b, Progress.MaterialCount))
                    return new Vector2Int(b.DoorX, b.DoorY);
                break;
            }
            return null;
        }

        /// <summary>
        /// 「！」が まだ出ていない 次の場所（依頼の目的地・調べる場所・仕上げの工事の看板）。なければ null。
        /// 話を聞く人・依頼板・馬車の御者は、名前のところに もう「！」が出ている
        /// </summary>
        private Vector2Int? ExtraMarkTarget()
        {
            if (area != null || town == null) return null;
            if (town.Destination != null) return new Vector2Int(town.Destination.X, town.Destination.Y);
            var q = ActiveStory();
            if (q != null) return q.Kind == StoryKind.Investigate ? new Vector2Int(q.X, q.Y) : (Vector2Int?)null;
            if (BoardTime() || TravelTime()) return null;
            return QuestTarget(); // 仕上げの工事の看板
        }

        /// <summary>町・遺跡の 次の場所に「！」（ピンク色・ふわふわ上下）</summary>
        private void DrawQuestExclamation()
        {
            Vector2 w;
            if (area != null)
            {
                var g = QuestTarget(); if (!g.HasValue) return;
                w = area.ToWorld(g.Value.x, g.Value.y);
            }
            else
            {
                var t = ExtraMarkTarget(); if (!t.HasValue) return;
                w = town.ToWorld(t.Value.x, t.Value.y);
            }
            float bob = 0.12f * Mathf.Sin(Time.time * 5f);
            DrawWorldLabel(w + Vector2.up * (2.05f + bob), UIKit.Colorize("！", new Color(1f, 0.45f, 0.9f)));
            // 画面の外なら、画面のはしに 矢印（目的地・調べる場所は ほかで 出しているので、ゴーレムと 工事の看板だけ）
            if (area != null) DrawEdgePointer(w, "！ゴーレム", new Color(1f, 0.6f, 1f), 112);
            else if (town.Destination == null && ActiveStory() == null) DrawEdgePointer(w, "！工事の看板", new Color(1f, 0.6f, 1f), 112);
        }

        private string questPinKey = "";   // 自動のピンの 行き先（かわったら ピンを立てなおす）
        private bool questPinned;          // 今のピンは 自動で立てたもの

        /// <summary>次に行く所に 自動でピンを立てる（自分で立てたピンは そのまま）</summary>
        private void UpdateQuestPin()
        {
            if (State != GameState.Town && State != GameState.Area) return;
            var t = QuestTarget();
            string key = (area != null ? "a" + area.Stage : "t" + TownData.Index) + ":" + (t.HasValue ? t.Value.ToString() : "-");
            if (key == questPinKey) return;
            questPinKey = key;
            if (t.HasValue) { if (CurrentPin == null || questPinned) { CurrentPin = t; questPinned = true; } }
            else if (questPinned) { CurrentPin = null; questPinned = false; }
        }

        /// <summary>左上の目的・師匠の話に出す、ストーリークエストの文（なければ null）</summary>
        private static string StoryObjective(bool shortText)
        {
            var q = ActiveStory();
            if (q == null) return null;
            return shortText ? $"目的：「{q.Title}」{q.Goal}" : $"ストーリークエスト「{q.Title}」が 進んでおる。やること：{q.Goal}";
        }

        /// <summary>町の表示：調べる場所の名前と、画面の外にあるときの矢印</summary>
        private void DrawStoryMarker()
        {
            // 依頼を受けるとき：いちばん近い依頼板が 画面の外なら、画面のはしに矢印
            if (BoardTime())
            {
                Vector2 me = player.transform.position; Vector2? best = null;
                foreach (var b in OpenBoards()) { var bw = town.ToWorld(b.x, b.y); if (best == null || Vector2.Distance(me, bw) < Vector2.Distance(me, best.Value)) best = bw; }
                if (best.HasValue) DrawEdgePointer(best.Value, "！依頼板", new Color(1f, 0.6f, 1f), 112);
            }
            // 旅立つとき：馬車の御者が画面の外なら、画面のはしに矢印
            if (TravelTime())
                foreach (var n in town.ActiveNpcs())
                    if (n.Role == NpcRole.Coach) DrawEdgePointer(town.ToWorld(n.X, n.Y), "！馬車の御者", new Color(1f, 0.6f, 1f), 112);
            var q = ActiveStory();
            // 話を聞くとき：まだ話していない人のうち いちばん近い人が 画面の外なら、矢印
            if (q != null && q.Kind == StoryKind.Talk)
            {
                Vector2 me = player.transform.position; NpcInfo near = null; float nd = float.MaxValue;
                foreach (var n in town.ActiveNpcs()) { if (!IsStoryTarget(n)) continue; float d = Vector2.Distance(me, town.ToWorld(n.X, n.Y)); if (d < nd) { nd = d; near = n; } }
                if (near != null) DrawEdgePointer(town.ToWorld(near.X, near.Y), "！" + near.Name, new Color(1f, 0.6f, 1f), 112);
            }
            if (q == null || q.Kind != StoryKind.Investigate) return;
            var w = town.ToWorld(q.X, q.Y);
            DrawWorldLabel(w + Vector2.up * 1.2f, UIKit.Colorize("調べる：" + q.SpotName, new Color(1f, 0.6f, 1f)));
            DrawEdgePointer(w, "！" + q.SpotName, new Color(1f, 0.6f, 1f), 112);
        }
    }
}
