using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：はじめの村（南西の区画）でできること。
    /// ・町長の「見習いの手帳」：はじめての冒険のおつかい。できたら町長に話すと ごほうび
    /// ・噴水の願いごと：金貨を投げると おみくじ（大吉ほど いいことがある）
    /// ・ねこ：村をうろうろ歩いている。なでると体力が少し回復
    /// </summary>
    public partial class GameManager
    {
        // ------------------------------------------------------------------
        // 見習いの手帳
        // ------------------------------------------------------------------

        /// <summary>おつかいができた（はじめてのときだけ、お知らせを出す）</summary>
        private bool MarkMission(int id, bool toast = true)
        {
            int done = Progress.MissionsDone;
            if ((done & (1 << id)) != 0) return false;
            Progress.MissionsDone = done | (1 << id);
            if (toast) ShowToast($"見習いの手帳：「{Missions.Names[id]}」ができた！ 町長に話すと ごほうびがもらえるよ。", 4f);
            return true;
        }

        private void TalkToMayor(NpcInfo npc, List<string> lines)
        {
            int done = Progress.MissionsDone, claimed = Progress.MissionsClaimed;
            // できているのに、まだごほうびをもらっていないもの
            int newCount = 0;
            for (int i = 0; i < Missions.Count; i++)
                if ((done & (1 << i)) != 0 && (claimed & (1 << i)) == 0)
                {
                    claimed |= 1 << i;
                    newCount++;
                }
            int doneCount = 0;
            for (int i = 0; i < Missions.Count; i++) if ((done & (1 << i)) != 0) doneCount++;

            // 手帳の一覧
            var sb = new System.Text.StringBuilder($"見習いの手帳（{doneCount} / {Missions.Count}）\n");
            for (int i = 0; i < Missions.Count; i++)
            {
                bool ok = (done & (1 << i)) != 0;
                sb.Append(ok ? "◎ " : "・ ").Append(Missions.Names[i]).Append(ok ? "（できた！）" : "").Append(i % 2 == 1 ? "\n" : "　");
            }
            lines.Add(sb.ToString());

            if (newCount > 0)
            {
                int coins = newCount * Missions.RewardCoins;
                Progress.AddItem(ItemKind.Coin, coins);
                SoundKit.Coin();
                lines.Add($"よくがんばったな！ できたおつかい {newCount}つ ぶんの ごほうびに、金貨 {coins}枚 をあげよう。");
            }
            if (doneCount == Missions.Count && (claimed & (1 << Missions.BonusBit)) == 0)
            {
                claimed |= 1 << Missions.BonusBit;
                Progress.AddItem(ItemKind.Coin, Missions.BonusCoins);
                Progress.AddItem(ItemKind.Herb, Missions.BonusHerbs);
                SoundKit.QuestClear();
                lines.Add($"手帳のおつかいを全部できたな！ もう りっぱな冒険者じゃ。とくべつに 金貨 {Missions.BonusCoins}枚 と 薬草 {Missions.BonusHerbs}こ をあげよう！");
            }
            else if (doneCount < Missions.Count && newCount == 0)
                lines.Add("◎ がついていない おつかいをやってみよう。できたら、また話しにおいで。");
            Progress.MissionsClaimed = claimed;
            dialog.Show(npc.Name, lines.ToArray());
        }

        // ------------------------------------------------------------------
        // 噴水の願いごと（おみくじ）
        // ------------------------------------------------------------------

        private const int WishCost = 5;
        private readonly System.Random wishRng = new System.Random();

        private static readonly string[] WishTips =
        {
            "命令は上から順番に動く。まよったら、ゴーレムの気持ちになって1つずつ動きを追ってみよう。",
            "同じ命令が何回も続いたら、くり返しでまとめるチャンス！",
            "宿舎のベッドで休むと、体力が全回復するよ。",
            "草むらに入る前に、体力をたしかめよう。薬草を持っていると安心。",
            "風船割りで練習すると、タイピングが速くなって、バトルが楽になる。",
            "地図（M）でピンを立てると、行きたい場所まで あと何マスか分かるよ。",
            "まちがえても大丈夫。解説を見れば、次はきっとできる！",
        };

        private bool NearFountain(Vector2 p)
        {
            for (int y = 0; y < TownData.H; y++)
                for (int x = 0; x < TownData.W; x++)
                    if (town.CharAt(town.ToWorld(x, y)) == 'F' && Vector2.Distance(p, town.ToWorld(x, y)) < 1.3f) return true;
            return false;
        }

        private void Wish()
        {
            KeyInput.Clear();
            if (!Progress.Spend(WishCost))
            {
                dialog.Show("噴水", $"願いごとには 金貨が {WishCost}枚 いるよ。");
                return;
            }
            SoundKit.Coin();
            MarkMission(Missions.Wished);
            int roll = wishRng.Next(100);
            string tip = WishTips[wishRng.Next(WishTips.Length)];
            string result;
            if (roll < 10)
            {
                Progress.AddItem(ItemKind.Herb, 2);
                Progress.AddItem(ItemKind.Coin, 15);
                result = "★大吉★！ 噴水がきらきら光った！ 薬草 2こ と 金貨 15枚 が わいてきた！";
            }
            else if (roll < 30)
            {
                Progress.AddItem(ItemKind.Coin, 8);
                result = "中吉！ 噴水の底から、金貨が 8枚 出てきた。";
            }
            else if (roll < 60)
            {
                Progress.AddItem(ItemKind.Herb, 1);
                result = "小吉。噴水のまわりに、薬草が 1こ 生えていた。";
            }
            else if (roll < 90) result = "吉。ふしぎな声が聞こえた…。";
            else result = "凶…。でも、気をつけて進めば大丈夫！";
            dialog.Show("噴水", new[] { $"金貨を {WishCost}枚 投げて、願いごとをした…。", result, "ふしぎな声：「" + tip + "」" });
        }

        // ------------------------------------------------------------------
        // ねこ（村をうろうろ）
        // ------------------------------------------------------------------

        private GameObject cat;
        private SpriteRenderer catRenderer;
        private Vector2Int catTile;
        private Vector2 catFrom, catTo;
        private float catMove = 1f;   // 0〜1：となりのマスへ動いている途中
        private float catWait;
        private readonly System.Random catRng = new System.Random();

        private static readonly Vector2Int CatHome = new Vector2Int(14, 23);

        /// <summary>ねこが歩いていい村のはんい（南西の区画の、草むらでない道）</summary>
        private bool CatCanStand(Vector2Int t)
        {
            if (t.x < 2 || t.x > 17 || t.y < 15 || t.y > 25) return false;
            char c = town.CharAt(town.ToWorld(t.x, t.y));
            if (c != '.' && c != ',') return false;
            if (town.NpcAt(town.ToWorld(t.x, t.y)) != null) return false;
            foreach (var b in TownData.Buildings) if (t.x == b.DoorX && t.y == b.DoorY) return false; // 入り口はふさがない
            return true;
        }

        private void CreateCat()
        {
            cat = new GameObject("Cat");
            cat.transform.SetParent(worldRoot.transform, false);
            catRenderer = cat.AddComponent<SpriteRenderer>();
            catRenderer.sprite = TownArt.Cat;
            catRenderer.sortingOrder = 9;
            catTile = CatHome;
            catFrom = catTo = town.ToWorld(catTile.x, catTile.y);
            cat.transform.position = catFrom;
            catMove = 1f;
            catWait = 1f;
        }

        private Vector2 CatWorld => cat != null ? (Vector2)cat.transform.position : Vector2.zero;

        private void UpdateCat()
        {
            if (cat == null) return;
            float dt = Time.deltaTime;
            if (catMove < 1f)
            {
                catMove = Mathf.Min(1f, catMove + dt * 2.5f);
                cat.transform.position = Vector2.Lerp(catFrom, catTo, catMove);
                return;
            }
            catWait -= dt;
            if (catWait > 0f) return;
            // となりのマスへ、ときどき とことこ歩く
            catWait = 0.6f + (float)catRng.NextDouble() * 2f;
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            var d = dirs[catRng.Next(dirs.Length)];
            var next = catTile + d;
            if (!CatCanStand(next)) return;
            catTile = next;
            catFrom = cat.transform.position;
            catTo = town.ToWorld(next.x, next.y);
            catMove = 0f;
            if (d.x != 0) catRenderer.flipX = d.x > 0;
        }

        private bool NearCat(Vector2 p) => cat != null && Vector2.Distance(p, CatWorld) < 1.2f;

        private void PetCat()
        {
            KeyInput.Clear();
            SoundKit.Meow();
            PlayerStats.Heal(5);
            MarkMission(Missions.PettedCat);
            catWait = 2f; // なでているあいだは、じっとしている
            dialog.Show("ねこ", "にゃーん♪（ごろごろ…）　なでると、心がほっこりした。体力が 5 回復した。");
        }
    }
}
