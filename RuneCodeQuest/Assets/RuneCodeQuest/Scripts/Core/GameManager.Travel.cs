using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：馬車の旅（世界地図）。
    /// ・どの村にもいる「馬車の御者」に話しかけると、6つの国の村を行き来できる
    /// ・ほかの村へ行けるのは、ひとつ前の村の さいごの建物（村のシンボル）を直したあと
    /// ・はじめて着いた村では、その国の お話が出る
    /// </summary>
    public partial class GameManager
    {
        /// <summary>はじめて その村に着いたときの話を見たかの記録（Progress.VillageFlag の 20〜25）</summary>
        private const int ArrivalFlagBase = 20;

        private void OpenTravel()
        {
            player.StopWalking();
            KeyInput.Clear();
            State = GameState.Travel;
        }

        /// <summary>その村の建物が直ったお祝いを 見たか（ルーネ村はむかしの記録をそのまま使う）</summary>
        private static bool RepairNoteSeen(BuildingInfo b) =>
            b.Village == 0 ? Progress.StorySeen(StoryRepairedBase + b.Number) : Progress.RepairNoteSeen(b.Id);

        private static void MarkRepairNote(BuildingInfo b)
        {
            if (b.Village == 0) Progress.MarkStory(StoryRepairedBase + b.Number);
            else Progress.MarkRepairNote(b.Id);
        }

        /// <summary>次の町づくりの依頼がある村（全部終わっていれば さいごの村）</summary>
        private static int NextWorkVillage()
        {
            int next = Rebuild.NextMain(s => Progress.StatusOf(s) != Progress.NotPlayed);
            return next > 0 ? TownData.VillageOfStage(next) : TownData.VillageCount - 1;
        }

        /// <summary>今いる村の建物が全部直っているときの、次にやること</summary>
        private string VillageDoneText()
        {
            int target = NextWorkVillage();
            if (target != TownData.Index)
                return $"この村の建物は 全部直った！ 馬車の御者に話しかけて、「{TownData.Villages[target].Name}」へ旅立つのじゃ！";
            return Progress.StorySeen(StoryBossBeaten)
                ? "世界は平和になった！ 時計台で、星3つを目指してみるのもいいぞ。"
                : "星の神殿のおくで、守り神オルディナが待っておる。神殿に入るのじゃ！";
        }

        private string VillageDoneShort()
        {
            int target = NextWorkVillage();
            if (target != TownData.Index) return $"目的：馬車で「{TownData.Villages[target].Name}」へ旅立つ";
            return Progress.StorySeen(StoryBossBeaten) ? "目的：時計台で星3つを集めよう" : "目的：星の神殿でオルディナを止める！";
        }

        /// <summary>馬車で その村へ行く</summary>
        private void TravelTo(int v)
        {
            var to = TownData.Villages[v];
            bool first = to.Arrival != null && !Progress.VillageFlag(ArrivalFlagBase + v);
            // 受けていた依頼・退治の依頼は、村をはなれると とりけし（目的地や草むらが 村ごとにちがうので）
            bool dropped = Progress.ActiveQuest > 0 || Progress.HuntArea >= 0;
            Progress.ActiveQuest = 0;
            Progress.HuntArea = -1;
            StartLoading($"馬車で「{to.Name}」へ向かっている…", $"{to.Kind}「{to.Name}」", () =>
            {
                Progress.Village = v;
                BuildTown();
                State = GameState.Town;
                var lines = new List<string>();
                if (first)
                {
                    Progress.MarkVillageFlag(ArrivalFlagBase + v);
                    lines.AddRange(to.Arrival);
                }
                else lines.Add($"「{to.Name}」に着いた。");
                if (dropped) lines.Add("（村をはなれたので、引き受けていた依頼は とりけしになった。もう一度 依頼板で受けられるよ）");
                lines.Add(ObjectiveText());
                AutoSave();
                dialog.Show(to.Name, lines.ToArray(), () => AnnounceStory());
            });
        }

        /// <summary>馬車の旅の画面（6つの国の一覧。行ける村には「行く」ボタン）</summary>
        private void DrawTravel()
        {
            UIKit.Nav("travel", true);
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                State = GameState.Town;
                return;
            }

            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.6f));
            var p = new Rect(130, 24, 1020, 670);
            UIKit.Panel(p);
            UIKit.Label(new Rect(p.x + 30, p.y + 12, 640, 50), "馬車の旅（世界地図）", UIKit.Heading);
            if (UIKit.ColorButton(new Rect(p.xMax - 140, p.y + 16, 120, 46), "とじる", new Color(0.8f, 0.4f, 0.4f)))
            {
                State = GameState.Town;
                return;
            }

            int suggest = NextWorkVillage();
            for (int i = 0; i < TownData.VillageCount; i++)
            {
                var v = TownData.Villages[i];
                bool here = i == TownData.Index, open = TownMap.VillageOpen(i);
                var row = new Rect(p.x + 24, p.y + 76 + i * 96, p.width - 48, 88);
                UIKit.Fill(row, here ? new Color(0.25f, 0.35f, 0.5f, 0.8f) : new Color(0f, 0f, 0f, 0.3f));
                UIKit.Fill(new Rect(row.x, row.y, 10, row.height), Color.HSVToRGB(i / 6f, 0.45f, open ? 0.85f : 0.35f));

                string name = open ? $"{v.Name}　{UIKit.Colorize(v.Kind, new Color(1f, 1f, 1f, 0.7f))}" : UIKit.Colorize($"{v.Name}　（まだ行けない）", Color.gray);
                UIKit.Label(new Rect(row.x + 24, row.y + 4, 640, 40), name, UIKit.Body);
                string info;
                if (!open)
                {
                    var prev = TownData.Villages[i - 1];
                    info = $"「{prev.Name}」の「{prev.LastBuilding.Name}」を直して、ストーリークエストを全部終えると 行けるようになる";
                }
                else
                {
                    int fixedCount = TownMap.RepairedCount(v);
                    info = $"直した建物 {fixedCount} / {v.Buildings.Length}　（クエスト ステージ{v.FirstStage}〜{v.LastStage}）" +
                           (fixedCount == v.Buildings.Length ? UIKit.Colorize("　すべて直った！", UIKit.SuccessColor) : "");
                }
                UIKit.Label(new Rect(row.x + 24, row.y + 44, 760, 36), info, UIKit.Small);

                if (here)
                    UIKit.Label(new Rect(row.xMax - 190, row.y + 22, 170, 44), UIKit.Colorize("いまいる村", UIKit.AccentColor), UIKit.SmallCenter);
                else if (open && UIKit.ColorButton(new Rect(row.xMax - 190, row.y + 20, 170, 48), "行く", UIKit.AccentColor, UIKit.Button, true, primary: i == suggest))
                {
                    TravelTo(i);
                    return;
                }
            }
        }
    }
}
