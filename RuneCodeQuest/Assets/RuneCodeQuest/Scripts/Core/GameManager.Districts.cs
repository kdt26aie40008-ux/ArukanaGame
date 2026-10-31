using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：南東・北東・北西の区画でできること。
    /// ・南東「配達屋」：町の人に荷物を届ける（届け先に地図のピンが立つ）
    /// ・北東「畑」：たねをまいて、冒険で歩きまわると薬草が育つ
    /// ・北西「かくれんぼ」：宝さがしの子を1分以内にさがす（あつい・つめたいが手がかり）
    /// </summary>
    public partial class GameManager
    {
        private static readonly string[] DistrictNames = { "南西", "南東", "北東", "北西" };

        /// <summary>地図にのせる、町の人の役わり</summary>
        private static string RoleLabel(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Training: return "訓練場";
                case NpcRole.Fishing: return "つり場";
                case NpcRole.MaterialTrader: return "資材屋";
                case NpcRole.Mayor: return "町長";
                case NpcRole.Courier: return "配達屋";
                case NpcRole.Gardener: return "畑";
                case NpcRole.HideSeek: return "かくれんぼ";
                default: return "";
            }
        }

        /// <summary>1マス歩いた（足音を鳴らして、歩いた数を数える。畑の作物はこれで育つ）</summary>
        private void OnStep(bool grass)
        {
            SoundKit.Step(grass);
            Progress.Steps++;
        }

        // ------------------------------------------------------------------
        // 南東：配達
        // ------------------------------------------------------------------

        private static readonly string[] DeliveryItems = { "手紙", "焼きたてのパン", "薬のびん", "図書館の本", "花たば", "工具箱", "毛糸の手ぶくろ", "りんごのかご" };
        private const int DeliveryBonusEvery = 5; // この回数ごとに資材のおまけ
        private readonly System.Random deliveryRng = new System.Random();

        private static int TileDistance(int ax, int ay, int bx, int by) => Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);

        private void TalkToCourier(NpcInfo npc, List<string> lines)
        {
            int target = Progress.Delivery - 1;
            if (target >= 0 && target < TownData.Npcs.Length)
            {
                var to = TownData.Npcs[target];
                townPin = new Vector2Int(to.X, to.Y); questPinned = false;
                dialog.Show(npc.Name, $"「{DeliveryItems[Progress.DeliveryItem % DeliveryItems.Length]}」は、{DistrictNames[TownData.AreaOf(to.X, to.Y)]}の「{to.Name}」に届けてね。地図にピンを立てなおしたよ！");
                return;
            }

            // 届け先：ほかの区画にいる町の人（いなければ、同じ区画の人）
            var near = new List<int>();
            var far = new List<int>();
            int home = TownData.AreaOf(npc.X, npc.Y);
            foreach (var n in town.ActiveNpcs())
            {
                if (n == npc || n.Role == NpcRole.Training) continue;
                int i = System.Array.IndexOf(TownData.Npcs, n);
                (TownData.AreaOf(n.X, n.Y) != home ? far : near).Add(i);
            }
            var pool = far.Count > 0 ? far : near;
            if (pool.Count == 0)
            {
                dialog.Show(npc.Name, "今日は届ける荷物がないんだ。また来てね！");
                return;
            }
            int pick = pool[deliveryRng.Next(pool.Count)];
            var dest = TownData.Npcs[pick];
            Progress.Delivery = pick + 1;
            Progress.DeliveryItem = deliveryRng.Next(DeliveryItems.Length);
            townPin = new Vector2Int(dest.X, dest.Y); questPinned = false;
            if (Progress.DeliveriesDone > 0) lines.Add($"これまでに {Progress.DeliveriesDone}回 届けてくれたね。いつもありがとう！");
            lines.Add($"今回の荷物は「{DeliveryItems[Progress.DeliveryItem]}」。届け先は {DistrictNames[TownData.AreaOf(dest.X, dest.Y)]}の「{dest.Name}」だよ。地図（M）にピンを立てておいたからね！");
            dialog.Show(npc.Name, lines.ToArray());
        }

        /// <summary>話しかけた人が配達の届け先なら、荷物をわたす。わたしたら true</summary>
        private bool TryDeliver(NpcInfo npc)
        {
            if (TownData.Index != 0) return false; // 配達は ルーネ村の中だけ
            int index = System.Array.IndexOf(TownData.Npcs, npc);
            if (index < 0 || Progress.Delivery != index + 1) return false;
            KeyInput.Clear();

            string item = DeliveryItems[Progress.DeliveryItem % DeliveryItems.Length];
            NpcInfo courier = null;
            foreach (var n in TownData.Npcs) if (n.Role == NpcRole.Courier) courier = n;
            int dist = courier != null ? TileDistance(courier.X, courier.Y, npc.X, npc.Y) : 20;
            int coins = 10 + dist / 3; // 遠いところほど、金貨が多い

            Progress.Delivery = 0;
            Progress.DeliveriesDone++;
            Progress.AddItem(ItemKind.Coin, coins);
            if (townPin.HasValue && townPin.Value == new Vector2Int(npc.X, npc.Y)) townPin = null;
            SoundKit.Coin();

            var lines = new List<string>
            {
                $"わあ、「{item}」を届けてくれたの？ ありがとう！",
                $"配達のお礼に 金貨 {coins}枚 もらった！（届けた回数：{Progress.DeliveriesDone}回）",
            };
            if (Progress.DeliveriesDone % DeliveryBonusEvery == 0)
            {
                var m = (Material)deliveryRng.Next((int)Material.BugShard); // 木材・石材・歯車のどれか
                Progress.AddMaterial(m, 2);
                lines.Add($"{DeliveryBonusEvery}回 届けたおまけに、{Rebuild.MaterialNames[(int)m]}を 2こ もらった！");
            }
            lines.Add("また配達屋で 荷物を受け取ってね。");
            dialog.Show(npc.Name, lines.ToArray());
            return true;
        }

        // ------------------------------------------------------------------
        // 北東：畑
        // ------------------------------------------------------------------

        private const int GrowSteps = 120; // この歩数で収穫できる
        private readonly List<SpriteRenderer> gardenPlants = new List<SpriteRenderer>();
        private readonly System.Random gardenRng = new System.Random();

        /// <summary>畑の育ち具合（0：何も植わっていない 1：芽 2：葉っぱ 3：収穫できる）</summary>
        private static int GardenStage(int bed)
        {
            int planted = Progress.GardenPlanted(bed);
            if (planted < 0) return 0;
            int grown = Progress.Steps - planted;
            return grown >= GrowSteps ? 3 : grown >= GrowSteps / 2 ? 2 : 1;
        }

        private static int StepsLeft(int bed) => Mathf.Max(0, GrowSteps - (Progress.Steps - Progress.GardenPlanted(bed)));

        private void CreateGarden()
        {
            gardenPlants.Clear();
            foreach (var (x, y) in TownData.GardenBeds)
            {
                var go = new GameObject("Plant");
                go.transform.SetParent(worldRoot.transform, false);
                go.transform.position = town.ToWorld(x, y);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 4;
                gardenPlants.Add(sr);
            }
            UpdateGarden(true);
        }

        private int gardenShownSteps = -1; // 最後に畑の絵を合わせたときの歩数

        private void UpdateGarden(bool force = false)
        {
            if (!force && gardenShownSteps == Progress.Steps) return;
            gardenShownSteps = Progress.Steps;
            for (int i = 0; i < gardenPlants.Count; i++)
            {
                int stage = GardenStage(i);
                var sprite = stage == 0 ? null : TownArt.Plant(stage);
                if (gardenPlants[i].sprite != sprite) gardenPlants[i].sprite = sprite;
            }
        }

        /// <summary>近くの畑の番号（なければ -1）</summary>
        private int NearGardenBed(Vector2 p)
        {
            int best = -1;
            float bestD = 1.3f;
            for (int i = 0; i < TownData.GardenBeds.Length; i++)
            {
                float d = Vector2.Distance(p, town.ToWorld(TownData.GardenBeds[i].x, TownData.GardenBeds[i].y));
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private static string GardenPrompt(int bed)
        {
            int stage = GardenStage(bed);
            return stage == 0 ? "畑にたねをまく"
                : stage == 3 ? "薬草を収穫する"
                : $"畑を見る（あと {StepsLeft(bed)}歩 で育つ）";
        }

        private void TendGarden(int bed)
        {
            KeyInput.Clear();
            int stage = GardenStage(bed);
            if (stage == 0)
            {
                Progress.SetGardenPlanted(bed, Progress.Steps);
                SoundKit.Chime();
                dialog.Show("畑", $"たねをまいた！ {GrowSteps}歩 歩くと 収穫できるよ。町やエリアを冒険して、育つのを待とう。");
            }
            else if (stage < 3)
            {
                dialog.Show("畑", stage == 1
                    ? $"小さな芽が出ている。あと {StepsLeft(bed)}歩 で収穫できそう。"
                    : $"葉っぱが ぐんぐんのびている！ あと {StepsLeft(bed)}歩 で収穫できそう。");
            }
            else
            {
                Progress.SetGardenPlanted(bed, -1);
                int roll = gardenRng.Next(100);
                string got;
                if (roll < 5)
                {
                    Progress.AddItem(ItemKind.Coin, 40);
                    got = "★金色の薬草★が とれた！ 高く売れて 金貨 40枚 になった！";
                }
                else if (roll < 15)
                {
                    Progress.AddItem(ItemKind.Herb, 1);
                    Progress.AddItem(ItemKind.Coin, 20);
                    got = "薬草 1こ と、土の中から 金貨 20枚 が出てきた！";
                }
                else if (roll < 40)
                {
                    Progress.AddItem(ItemKind.Herb, 3);
                    got = "大豊作！ 薬草が 3こ とれた！";
                }
                else
                {
                    Progress.AddItem(ItemKind.Herb, 2);
                    got = "薬草が 2こ とれた！";
                }
                SoundKit.QuestClear();
                dialog.Show("畑", new[] { got, "畑が空いたよ。また たねをまこう！" });
            }
            UpdateGarden(true);
        }

        private void TalkToGardener(NpcInfo npc, List<string> lines)
        {
            int ready = 0, growing = 0;
            for (int i = 0; i < TownData.GardenBeds.Length; i++)
            {
                int s = GardenStage(i);
                if (s == 3) ready++;
                else if (s > 0) growing++;
            }
            lines.Add(ready > 0 ? $"おや、{ready}こ 収穫できるよ！ 畑を調べてごらん。"
                : growing > 0 ? $"{growing}こ 育っているところだよ。たくさん歩けば すぐ大きくなるさ。"
                : "畑は空っぽだね。となりの土を調べて、たねをまいてごらん。");
            dialog.Show(npc.Name, lines.ToArray());
        }

        // ------------------------------------------------------------------
        // 北西：かくれんぼ
        // ------------------------------------------------------------------

        private const float HideTime = 60f;
        private bool hideActive;
        private Vector2Int hideSpot;
        private int lastHideSpot = -1;
        private float hideTimer;
        private NpcInfo hideKid;
        private readonly System.Random hideRng = new System.Random();

        private void TalkToHideSeeker(NpcInfo npc, List<string> lines)
        {
            if (Progress.HideFound > 0) lines.Add($"これまでに {Progress.HideFound}回 見つけられちゃった。今度は負けないぞ！");
            lines.Add("目をつぶって 10 数えてね。…もういいかい？");
            dialog.Show(npc.Name, lines.ToArray(), () => StartHide(npc));
        }

        private void StartHide(NpcInfo kid)
        {
            int i;
            do i = hideRng.Next(TownData.HideSpots.Length); while (i == lastHideSpot && TownData.HideSpots.Length > 1);
            lastHideSpot = i;
            hideSpot = new Vector2Int(TownData.HideSpots[i].x, TownData.HideSpots[i].y);
            hideKid = kid;
            hideActive = true;
            hideTimer = HideTime;
            town.SetNpcHidden(kid, true);
            ShowToast("「もういいよー！」 宝さがしの子が 北西のどこかに かくれた。上の「あつい・つめたい」を手がかりに さがそう！", 4f);
        }

        private void EndHide()
        {
            hideActive = false;
            if (hideKid != null) town.SetNpcHidden(hideKid, false);
        }

        /// <summary>かくれんぼの時間を進める（町の画面で、会話していないときだけ）</summary>
        private void UpdateHideSeek()
        {
            if (!hideActive) return;
            if (area != null) { EndHide(); return; } // 遺跡や宿舎に行ったら、おしまい
            if (State != GameState.Town || dialog.Active || showMap || showInventory) return;
            hideTimer -= Time.deltaTime;
            if (hideTimer > 0f) return;
            EndHide();
            SoundKit.Miss();
            townPin = hideSpot; questPinned = false; // どこにかくれていたか、地図のピンで教えてくれる
            dialog.Show("宝さがしの子", "時間切れ〜！ どこにかくれてたか、地図（M）にピンを立てておいたよ。また遊ぼうね！");
        }

        /// <summary>かくれている場所のとなりまで来たら、見つけた！</summary>
        private void CheckHideFound(Vector2 p)
        {
            if (!hideActive) return;
            if (Vector2.Distance(p, town.ToWorld(hideSpot.x, hideSpot.y)) > 1.1f) return;
            EndHide();
            player.StopWalking();
            int coins = 10 + Mathf.CeilToInt(hideTimer) / 4; // 早く見つけるほど多い
            Progress.HideFound++;
            Progress.AddItem(ItemKind.Coin, coins);
            var lines = new List<string>
            {
                $"「わっ、見つかっちゃった！」（のこり {Mathf.CeilToInt(hideTimer)}秒）",
                $"ごほうびに 金貨 {coins}枚 もらった！",
            };
            if (Progress.HideFound % 3 == 0)
            {
                Progress.AddItem(ItemKind.Herb, 1);
                lines.Add($"{Progress.HideFound}回目のおまけに、ひみつの薬草を 1こ くれた！");
            }
            SoundKit.QuestClear();
            dialog.Show("宝さがしの子", lines.ToArray());
        }

        /// <summary>かくれんぼの手がかり（かくれている場所までのマスの数で、あつい・つめたい）</summary>
        private string HideHint()
        {
            var t = town.ToTile(player.transform.position);
            int d = TileDistance(t.x, t.y, hideSpot.x, hideSpot.y);
            return d <= 3 ? UIKit.Colorize("とても あつい！ すぐ近くだ！", new Color(1f, 0.35f, 0.3f))
                : d <= 6 ? UIKit.Colorize("あつい！ 近づいてきた", new Color(1f, 0.6f, 0.3f))
                : d <= 11 ? UIKit.Colorize("あたたかい", new Color(1f, 0.85f, 0.4f))
                : UIKit.Colorize("つめたい… 遠いみたい", new Color(0.5f, 0.75f, 1f));
        }

        /// <summary>区画の物の名前（畑）と、かくれんぼの表示</summary>
        private void DrawDistrictLabels()
        {
            for (int i = 0; i < TownData.GardenBeds.Length; i++)
                if (GardenStage(i) == 3)
                    DrawWorldLabel(town.ToWorld(TownData.GardenBeds[i].x, TownData.GardenBeds[i].y) + Vector2.down * 0.9f, UIKit.Colorize("収穫できる！", UIKit.AccentColor));

            if (!hideActive || State != GameState.Town) return;
            var r = new Rect(440, 140, 400, 84);
            UIKit.Fill(r, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Label(new Rect(r.x, r.y + 2, r.width, 38), $"かくれんぼ　のこり {Mathf.CeilToInt(hideTimer)}秒", UIKit.Center);
            UIKit.Label(new Rect(r.x, r.y + 40, r.width, 40), HideHint(), UIKit.Center);
        }
    }
}
