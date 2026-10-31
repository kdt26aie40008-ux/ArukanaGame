using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：持ち物（かばん）。
    /// 左に キャラの強さと そうび、右に ます目（1マスに 1しゅるい・右下に ×こ数）。
    /// マスを えらぶと、下に くわしい説明。「英語ことばカード」で クエストに出てきた 英単語を 見返せる。
    /// </summary>
    public partial class GameManager
    {
        private int bagIndex;        // えらんでいる マス
        private bool bagCards;       // 英語ことばカードを 開いている
        private int cardPage;        // 英語ことばカードの ページ

        private const int BagCols = 6, BagRows = 4;
        private const int CardsPerPage = 12;

        /// <summary>かばんの マス 1つぶん</summary>
        private struct BagSlot
        {
            public Texture Icon;
            public string Name;
            public string Count;     // 右下の 数（×3 など）
            public string Detail;    // えらんだときの 説明
        }

        /// <summary>かばんに 入っているもの（持っているものだけ）</summary>
        private static List<BagSlot> BagSlots()
        {
            var list = new List<BagSlot>();
            void Add(Sprite icon, string name, int n, string detail, string unit = "")
            {
                if (n <= 0) return;
                list.Add(new BagSlot { Icon = SpriteFactory.TextureOf(icon), Name = name, Count = $"×{n}{unit}", Detail = detail });
            }
            Add(QuestArt.Item(ItemKind.Herb), "薬草", Progress.ItemCount(ItemKind.Herb), $"バトル中に Tab で使う。体力が {BattleData.HerbHeal} 回復する。");
            Add(QuestArt.Item(ItemKind.Star), "むしよけの鈴", Progress.RepelSteps, "草むらで アルカナが 出てこなくなる。数は のこりの 歩数。", "歩");
            for (int i = 0; i < Rebuild.MaterialKinds; i++)
                Add(TownArt.MaterialIcon((Material)i), Rebuild.MaterialNames[i], Progress.MaterialCount((Material)i), "町の 再建に使う 資材。工事の看板で 建物を直そう。");
            for (int i = 0; i < Balloon.CraftKinds; i++)
                Add(TownArt.CraftIcon((CraftMaterial)i), Balloon.CraftNames[i], Progress.CraftCount((CraftMaterial)i), "風船割りで もらえる 素材。鍛冶屋などで 強い そうびを 作れる。");
            for (int k = 1; k < QuestTheme.ItemNames.Length; k++)
            {
                var kind = (ItemKind)k;
                if (kind == ItemKind.Coin || kind == ItemKind.Herb) continue;
                Add(QuestArt.Item(kind), QuestTheme.ItemNames[k], Progress.ItemCount(kind), QuestTheme.ItemDescriptions[k] + "（クエストで 集めた アイテム）");
            }
            return list;
        }

        /// <summary>Esc：カードを開いていたら かばんに もどる。かばんなら とじる</summary>
        private void BagBack()
        {
            if (bagCards) bagCards = false;
            else showInventory = false;
        }

        private void DrawInventory()
        {
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.55f));
            var p = new Rect(110, 40, 1060, 640);
            UIKit.Panel(p);
            if (bagCards) { DrawWordCards(p); return; }

            var look = Progress.Look;
            UIKit.Texture(new Rect(p.x + 24, p.y + 14, 40, 40), SpriteFactory.TextureOf(QuestArt.Pocket));
            UIKit.Label(new Rect(p.x + 72, p.y + 12, 500, 50), $"{look.Name} の かばん", UIKit.Heading);
            if (UIKit.ColorButton(new Rect(p.xMax - 130, p.y + 14, 110, 46), "とじる", new Color(0.8f, 0.4f, 0.4f), primary: true))
            {
                showInventory = false;
                KeyInput.Clear();
            }
            var dexButton = new Rect(p.xMax - 360, p.y + 14, 220, 46);
            if (UIKit.ColorButton(dexButton, "", UIKit.AccentColor)) OpenDexFromBag();
            UIKit.Texture(new Rect(dexButton.x + 10, dexButton.y + 7, 32, 32), SpriteFactory.TextureOf(TownArt.DexIcon));
            UIKit.Label(new Rect(dexButton.x + 44, dexButton.y, dexButton.width - 48, dexButton.height), "アルカナ図鑑", UIKit.Center);
            var cardButton = new Rect(p.xMax - 610, p.y + 14, 240, 46);
            if (UIKit.ColorButton(cardButton, "", new Color(0.35f, 0.6f, 0.45f))) { bagCards = true; cardPage = 0; KeyInput.Clear(); }
            UIKit.Texture(new Rect(cardButton.x + 10, cardButton.y + 7, 32, 32), SpriteFactory.TextureOf(QuestArt.Item(ItemKind.Book)));
            UIKit.Label(new Rect(cardButton.x + 44, cardButton.y, cardButton.width - 48, cardButton.height), "英語ことばカード", UIKit.Center);

            // ----- 左：キャラと 強さ・そうび -----
            var left = new Rect(p.x + 20, p.y + 74, 300, 546);
            UIKit.Fill(left, UIKit.SubPanelColor);
            UIKit.Fill(new Rect(left.x + 90, left.y + 14, 120, 120), new Color(0.25f, 0.45f, 0.3f));
            UIKit.Texture(new Rect(left.x + 95, left.y + 19, 110, 110), SpriteFactory.TextureOf(SpriteFactory.Player));
            UIKit.Label(new Rect(left.x, left.y + 140, left.width, 34), UIKit.Colorize($"ランク {PlayerStats.Rank}", UIKit.AccentColor), UIKit.Center);
            UIKit.Label(new Rect(left.x, left.y + 172, left.width, 26), $"次のランクまで 星 あと{PlayerStats.StarsToNextRank}こ", UIKit.SmallCenter);
            float sy = left.y + 208;
            StatRow(new Rect(left.x + 16, sy, left.width - 32, 34), "体力", $"{PlayerStats.Hp} / {PlayerStats.MaxHp}", new Color(0.9f, 0.4f, 0.4f));
            StatRow(new Rect(left.x + 16, sy + 40, left.width - 32, 34), "攻撃", $"{PlayerStats.Attack}", new Color(1f, 0.6f, 0.3f));
            StatRow(new Rect(left.x + 16, sy + 80, left.width - 32, 34), "守り", $"{PlayerStats.Defense}", new Color(0.45f, 0.7f, 1f));
            var coin = new Rect(left.x + 16, sy + 124, left.width - 32, 40);
            UIKit.Fill(coin, new Color(0f, 0f, 0f, 0.25f));
            UIKit.Texture(new Rect(coin.x + 8, coin.y + 4, 32, 32), SpriteFactory.TextureOf(QuestArt.Item(ItemKind.Coin)));
            UIKit.Label(new Rect(coin.x + 48, coin.y + 4, coin.width - 56, 34), UIKit.Colorize($"金貨 {Progress.Coins} 枚", new Color(1f, 0.85f, 0.3f)), UIKit.Body);
            UIKit.Label(new Rect(left.x + 16, sy + 178, 200, 26), "そうび", UIKit.Small);
            GearSlot(new Rect(left.x + 16, sy + 206, left.width - 32, 58), QuestArt.Wand, "杖", PlayerStats.Weapon.Name);
            GearSlot(new Rect(left.x + 16, sy + 270, left.width - 32, 58), QuestArt.Item(ItemKind.Book), "ローブ", PlayerStats.Armor.Name);

            // ----- 右：ます目（1マスに 1しゅるい） -----
            var slots = BagSlots();
            var grid = new Rect(p.x + 340, p.y + 74, 700, 420);
            UIKit.Label(new Rect(grid.x, grid.y - 2, 400, 26), $"持ち物（{slots.Count}しゅるい）", UIKit.Small);
            int pages = Mathf.Max(1, (slots.Count + BagCols * BagRows - 1) / (BagCols * BagRows));
            bagIndex = Mathf.Clamp(bagIndex, 0, Mathf.Max(0, slots.Count - 1));
            int page = bagIndex / (BagCols * BagRows);
            const float cell = 108f, gap = 8f;
            for (int k = 0; k < BagCols * BagRows; k++)
            {
                int i = page * BagCols * BagRows + k;
                var r = new Rect(grid.x + (k % BagCols) * (cell + gap), grid.y + 26 + (k / BagCols) * (cell - 14 + gap), cell, cell - 14);
                // わく（実際のゲームの かばんのように）
                UIKit.Fill(r, new Color(0.45f, 0.38f, 0.28f));
                UIKit.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), i < slots.Count ? new Color(0.2f, 0.17f, 0.13f) : new Color(0.15f, 0.13f, 0.1f));
                if (i >= slots.Count) continue;
                var s = slots[i];
                if (i == bagIndex)
                {
                    float glow = 0.6f + 0.4f * Mathf.Sin(Time.time * 4f);
                    UIKit.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, 3), new Color(1f, 0.85f, 0.3f, glow));
                    UIKit.Fill(new Rect(r.x - 3, r.yMax, r.width + 6, 3), new Color(1f, 0.85f, 0.3f, glow));
                    UIKit.Fill(new Rect(r.x - 3, r.y, 3, r.height), new Color(1f, 0.85f, 0.3f, glow));
                    UIKit.Fill(new Rect(r.xMax, r.y, 3, r.height), new Color(1f, 0.85f, 0.3f, glow));
                }
                if (GUI.Button(UIKit.S(r), GUIContent.none, GUIStyle.none)) bagIndex = i;
                UIKit.Texture(new Rect(r.x + (r.width - 56) / 2, r.y + 8, 56, 56), s.Icon);
                UIKit.Label(new Rect(r.x + 4, r.yMax - 30, r.width - 8, 28), UIKit.Colorize(s.Count, Color.white), RightSmall);
            }
            if (pages > 1)
            {
                UIKit.Label(new Rect(grid.xMax - 260, grid.y - 2, 120, 26), $"{page + 1} / {pages}", UIKit.SmallCenter);
                if (page > 0 && UIKit.ColorButton(new Rect(grid.xMax - 130, grid.y - 6, 60, 30), "◀", Color.gray, UIKit.SmallButton)) bagIndex = (page - 1) * BagCols * BagRows;
                if (page < pages - 1 && UIKit.ColorButton(new Rect(grid.xMax - 64, grid.y - 6, 60, 30), "▶", Color.gray, UIKit.SmallButton)) bagIndex = (page + 1) * BagCols * BagRows;
            }

            // えらんだ マスの 説明
            var info = new Rect(p.x + 340, p.y + 516, 700, 104);
            UIKit.Fill(info, new Color(0f, 0f, 0f, 0.35f));
            if (slots.Count == 0)
                UIKit.Label(new Rect(info.x + 16, info.y + 10, info.width - 32, info.height - 20), "かばんは からっぽ。クエストや バトルで 手に入れたものが ここに入るよ。", UIKit.Body);
            else
            {
                var s = slots[bagIndex];
                UIKit.Texture(new Rect(info.x + 14, info.y + 20, 64, 64), s.Icon);
                UIKit.Label(new Rect(info.x + 92, info.y + 8, info.width - 100, 36), UIKit.Colorize(s.Name, UIKit.AccentColor) + "　" + s.Count, UIKit.Body);
                UIKit.Label(new Rect(info.x + 92, info.y + 44, info.width - 100, 56), s.Detail, UIKit.Small);
            }

            // 矢印キーで えらぶ
            var e = Event.current;
            if (e.type == EventType.KeyDown && slots.Count > 0)
            {
                int d = e.keyCode == KeyCode.RightArrow ? 1 : e.keyCode == KeyCode.LeftArrow ? -1 : e.keyCode == KeyCode.DownArrow ? BagCols : e.keyCode == KeyCode.UpArrow ? -BagCols : 0;
                if (d != 0) { bagIndex = Mathf.Clamp(bagIndex + d, 0, slots.Count - 1); e.Use(); }
            }
            UIKit.HelpBar("クリック・矢印：えらぶ　I キー・Esc：とじる");
        }

        private static GUIStyle rightSmall;
        private static GUIStyle RightSmall => rightSmall ?? (rightSmall = new GUIStyle(UIKit.SmallCenter) { alignment = TextAnchor.LowerRight, fontSize = 18 });

        private static void StatRow(Rect r, string label, string value, Color color)
        {
            UIKit.Fill(r, new Color(0f, 0f, 0f, 0.25f));
            UIKit.Fill(new Rect(r.x, r.y, 6, r.height), color);
            UIKit.Label(new Rect(r.x + 16, r.y + 2, 100, r.height), label, UIKit.Body);
            UIKit.Label(new Rect(r.x + 110, r.y + 2, r.width - 120, r.height), UIKit.Colorize(value, color), UIKit.Body);
        }

        private static void GearSlot(Rect r, Sprite icon, string kind, string name)
        {
            UIKit.Fill(r, new Color(0.45f, 0.38f, 0.28f));
            UIKit.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(0.2f, 0.17f, 0.13f));
            UIKit.Texture(new Rect(r.x + 10, r.y + 9, 40, 40), SpriteFactory.TextureOf(icon));
            UIKit.Label(new Rect(r.x + 58, r.y + 2, r.width - 64, 24), kind, UIKit.Small);
            UIKit.Label(new Rect(r.x + 58, r.y + 24, r.width - 64, 30), name, UIKit.Body);
        }

        // ------------------------------------------------------------------
        // 英語ことばカード：クエストに出てきた 英単語（見たことのない ことばは ？？？）
        // ------------------------------------------------------------------

        private void DrawWordCards(Rect p)
        {
            var all = EnglishWords.All;
            UIKit.Texture(new Rect(p.x + 24, p.y + 14, 40, 40), SpriteFactory.TextureOf(QuestArt.Item(ItemKind.Book)));
            UIKit.Label(new Rect(p.x + 72, p.y + 12, 500, 50), "英語ことばカード", UIKit.Heading);
            UIKit.Label(new Rect(p.x + 340, p.y + 20, 420, 36), $"集めた ことば {Progress.EnglishSeenCount} / {all.Length}", UIKit.Body);
            if (UIKit.ColorButton(new Rect(p.xMax - 200, p.y + 14, 180, 46), "かばんに もどる", Color.gray, primary: true)) { bagCards = false; KeyInput.Clear(); return; }

            int pages = (all.Length + CardsPerPage - 1) / CardsPerPage;
            cardPage = Mathf.Clamp(cardPage, 0, pages - 1);
            const float cw = 246f, ch = 158f, gap = 12f;
            for (int k = 0; k < CardsPerPage; k++)
            {
                int i = cardPage * CardsPerPage + k;
                if (i >= all.Length) break;
                var w = all[i];
                var r = new Rect(p.x + 24 + (k % 4) * (cw + gap), p.y + 76 + (k / 4) * (ch + gap), cw, ch);
                bool seen = Progress.EnglishSeen(w.Word);
                // カード（ふち・上の 色の おび）
                UIKit.Fill(r, seen ? new Color(0.95f, 0.9f, 0.78f) : new Color(0.3f, 0.3f, 0.36f));
                UIKit.Fill(new Rect(r.x, r.y, r.width, 8), seen ? CardColor(i) : new Color(0.4f, 0.4f, 0.46f));
                var ink = new Color(0.25f, 0.2f, 0.15f);
                if (!seen)
                {
                    UIKit.Label(new Rect(r.x, r.y + 30, r.width, 50), UIKit.Colorize("？？？", new Color(1f, 1f, 1f, 0.6f)), UIKit.Center);
                    UIKit.Label(new Rect(r.x + 10, r.y + 92, r.width - 20, 50), UIKit.Colorize("クエストで 出会うと カードになるよ", new Color(1f, 1f, 1f, 0.6f)), UIKit.SmallCenter);
                    continue;
                }
                UIKit.Label(new Rect(r.x + 12, r.y + 14, r.width - 24, 40), UIKit.Colorize(w.Word, CardColor(i) * 0.75f + Color.black * 0.25f), CardWord);
                UIKit.Label(new Rect(r.x + 12, r.y + 54, r.width - 24, 24), UIKit.Colorize(w.Reading, new Color(0.45f, 0.4f, 0.35f)), UIKit.Small);
                UIKit.Label(new Rect(r.x + 12, r.y + 76, r.width - 24, 30), UIKit.Colorize(w.Meaning, ink), UIKit.Body);
                UIKit.Label(new Rect(r.x + 12, r.y + 108, r.width - 24, 48), UIKit.Colorize(w.Note, new Color(0.35f, 0.3f, 0.25f)), UIKit.Small);
            }
            UIKit.Label(new Rect(p.x, p.yMax - 54, p.width, 30), $"{cardPage + 1} / {pages} ページ", UIKit.SmallCenter);
            if (cardPage > 0 && UIKit.ColorButton(new Rect(p.x + 24, p.yMax - 58, 150, 44), "◀ まえ", Color.gray)) cardPage--;
            if (cardPage < pages - 1 && UIKit.ColorButton(new Rect(p.xMax - 174, p.yMax - 58, 150, 44), "つぎ ▶", UIKit.AccentColor)) cardPage++;
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.LeftArrow))
            {
                cardPage = Mathf.Clamp(cardPage + (e.keyCode == KeyCode.RightArrow ? 1 : -1), 0, pages - 1);
                e.Use();
            }
            UIKit.HelpBar("← →：ページ　Esc：かばんに もどる");
        }

        private static GUIStyle cardWord;
        private static GUIStyle CardWord => cardWord ?? (cardWord = new GUIStyle(UIKit.Code) { fontSize = 30, richText = true });

        /// <summary>カードの 上の おびの色（なかまごとに ちがう色）</summary>
        private static Color CardColor(int i) =>
            i < 5 ? ScratchEditor.MotionColor : i < 10 ? ScratchEditor.MagicColor : i < 20 ? new Color(0.36f, 0.69f, 0.84f)
            : i < 26 ? ScratchEditor.ControlColor : i < 30 ? ScratchEditor.VariableColor : ScratchEditor.FunctionColor;
    }
}
