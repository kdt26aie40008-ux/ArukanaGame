using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// お店・図書館の画面。
    /// 道具屋：薬草・むしよけの鈴　鍛冶屋：杖　防具屋：ローブ　図書館：タイピングの言葉の設定　資材屋：木材・石材・歯車
    /// </summary>
    public class ShopScreen
    {
        public System.Action OnClose;

        private BuildingFunction mode;
        private Vector2 gearScroll;

        /// <summary>資材屋の資材1つの値段</summary>
        public const int MaterialPrice = 30;
        private string note;

        public void Open(BuildingFunction function)
        {
            mode = function;
            note = null;
        }

        /// <summary>店の中の絵（なければ null）。道具屋・鍛冶屋・防具屋・資材屋・図書館</summary>
        private Sprite ShopInside()
        {
            // 村ごとの 店の中の絵（Resources/Art/Shop/〈村〉_〈建物〉の中.png）があれば それ
            foreach (var b in TownData.Buildings)
                if (b.Function == mode)
                {
                    var own = ArtOverrides.Get($"Art/Shop/{TownData.Current.Name}_{b.Name}の中");
                    if (own != null) return own;
                    break;
                }
            string name = mode == BuildingFunction.ItemShop ? "道具屋" : mode == BuildingFunction.WeaponShop ? "鍛冶屋"
                : mode == BuildingFunction.ArmorShop ? "防具屋" : mode == BuildingFunction.MaterialTrader ? "資材屋" : "図書館";
            return ArtOverrides.Get($"Art/Shop/{name}の中");
        }

        public void Draw()
        {
            UIKit.Nav("shop-" + mode, true);
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                OnClose?.Invoke();
                return;
            }

            // 店の中の絵（Resources/Art/Shop/〈店〉の中.png があれば、画面いっぱいに うしろに しく）
            var inside = ShopInside();
            if (inside != null)
            {
                var tex = inside.texture;
                float h = UIKit.RefHeight, w = h * tex.width / tex.height;
                if (w < UIKit.RefWidth) { w = UIKit.RefWidth; h = w * tex.height / tex.width; }
                UIKit.Texture(new Rect((UIKit.RefWidth - w) / 2f, (UIKit.RefHeight - h) / 2f, w, h), tex);
                UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.3f));
            }
            else UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.6f));
            var p = new Rect(160, 50, 960, 620);
            UIKit.Panel(p);
            string title = mode == BuildingFunction.ItemShop ? "道具屋" : mode == BuildingFunction.WeaponShop ? "鍛冶屋（杖）"
                : mode == BuildingFunction.ArmorShop ? "防具屋（ローブ）" : mode == BuildingFunction.MaterialTrader ? "資材屋" : "図書館";
            UIKit.Label(new Rect(p.x + 30, p.y + 14, 500, 50), title, UIKit.Heading);
            UIKit.Texture(new Rect(p.x + 560, p.y + 18, 36, 36), SpriteFactory.TextureOf(QuestArt.Item(ItemKind.Coin)));
            UIKit.Label(new Rect(p.x + 600, p.y + 16, 200, 44), $"{Progress.Coins} 枚", UIKit.Body);
            if (UIKit.ColorButton(new Rect(p.xMax - 140, p.y + 16, 120, 46), "とじる", new Color(0.8f, 0.4f, 0.4f)))
            {
                OnClose?.Invoke();
                return;
            }

            float y = p.y + 84;
            switch (mode)
            {
                case BuildingFunction.ItemShop:
                    ItemRow(ref y, p, QuestArt.Item(ItemKind.Herb), "薬草", $"体力を {BattleData.HerbHeal} 回復（バトル中に Tab）　持っている数: {Progress.ItemCount(ItemKind.Herb)}",
                        BattleData.HerbPrice, () => Progress.AddItem(ItemKind.Herb, 1));
                    ItemRow(ref y, p, QuestArt.Item(ItemKind.Star), "むしよけの鈴", $"{BattleData.BellSteps}歩のあいだ、アルカナが出ない　のこり: {Progress.RepelSteps}歩",
                        BattleData.BellPrice, () => Progress.RepelSteps += BattleData.BellSteps);
                    break;
                case BuildingFunction.WeaponShop:
                case BuildingFunction.ArmorShop:
                {
                    // 杖・ローブは数が多いので、スクロールできる一覧にする（下のほうに、素材で作る装備）
                    bool weapon = mode == BuildingFunction.WeaponShop;
                    int count = weapon ? BattleData.Weapons.Length : BattleData.Armors.Length;
                    var view = new Rect(p.x + 20, p.y + 76, p.width - 40, p.height - 76 - 64);
                    gearScroll = UIKit.BeginScrollView(view, gearScroll, new Rect(0, 0, view.width - 20, count * 80));
                    var inner = new Rect(-30, 0, view.width - 20 + 60, count * 80); // GearRow は p.x + 30 から描くので、そのぶん左へ
                    float gy = 0;
                    for (int i = 0; i < count; i++) GearRow(ref gy, inner, i, weapon);
                    UIKit.EndScrollView();
                    break;
                }
                case BuildingFunction.MaterialTrader:
                    // 資材が足りないときの助け（依頼でもらうより高い）。アルカナのかけらは売っていない
                    for (int i = 0; i < (int)Material.BugShard; i++)
                    {
                        var m = (Material)i;
                        ItemRow(ref y, p, TownArt.MaterialIcon(m), Rebuild.MaterialNames[i],
                            $"建物の再建に使う資材　持っている数: {Progress.MaterialCount(m)}", MaterialPrice, () => Progress.AddMaterial(m, 1));
                    }
                    UIKit.Label(new Rect(p.x + 30, y + 4, p.width - 60, 60),
                        "アルカナのかけらは売っていないよ。草むらのアルカナをたおして集めよう。", UIKit.Small);
                    break;
                default:
                    DrawLibrary(p);
                    break;
            }
            if (note != null) UIKit.Label(new Rect(p.x + 30, p.yMax - 56, p.width - 60, 44), UIKit.Colorize(note, UIKit.WarnColor), UIKit.Body);
            UIKit.HelpBar("矢印キー・Tab でえらんで Enter（マウスでもOK）　Esc: とじる");
        }

        private void ItemRow(ref float y, Rect p, Sprite icon, string name, string desc, int price, System.Action give)
        {
            var r = new Rect(p.x + 30, y, p.width - 60, 76);
            UIKit.Fill(r, UIKit.SubPanelColor);
            UIKit.Texture(new Rect(r.x + 12, r.y + 12, 52, 52), SpriteFactory.TextureOf(icon));
            UIKit.Label(new Rect(r.x + 80, r.y + 4, 400, 36), name, UIKit.Body);
            UIKit.Label(new Rect(r.x + 80, r.y + 40, 640, 32), desc, UIKit.Small);
            if (UIKit.ColorButton(new Rect(r.xMax - 200, r.y + 14, 186, 48), $"買う（{price}枚）", UIKit.AccentColor, UIKit.Button, true, primary: true))
            {
                if (Progress.Spend(price))
                {
                    give();
                    note = $"{name}を買った！";
                }
                else note = "金貨が足りない…。草むらのアルカナをたおすと、金貨が手に入るよ。";
            }
            y += 86;
        }

        private void GearRow(ref float y, Rect p, int id, bool weapon)
        {
            var g = weapon ? BattleData.Weapons[id] : BattleData.Armors[id];
            bool owned = weapon ? Progress.OwnsWeapon(id) : Progress.OwnsArmor(id);
            bool equipped = (weapon ? Progress.Weapon : Progress.Armor) == id;
            var r = new Rect(p.x + 30, y, p.width - 60, 72);
            UIKit.Fill(r, equipped ? new Color(0.25f, 0.35f, 0.25f) : UIKit.SubPanelColor);
            UIKit.Texture(new Rect(r.x + 12, r.y + 10, 52, 52), SpriteFactory.TextureOf(weapon ? QuestArt.Wand : QuestArt.Item(ItemKind.Book)));
            UIKit.Label(new Rect(r.x + 80, r.y + 2, 400, 36), g.Recipe != null ? g.Name + UIKit.Colorize("（素材で作る）", UIKit.AccentColor) : g.Name, UIKit.Body);
            string desc = g.Recipe != null && !owned ? $"{g.Description}　素材：{BattleData.RecipeText(g, Progress.CraftCount)}" : g.Description;
            UIKit.Label(new Rect(r.x + 80, r.y + 38, 560, 30), desc, UIKit.Small);

            var b = new Rect(r.xMax - 200, r.y + 12, 186, 48);
            if (equipped) UIKit.Label(b, UIKit.Colorize("装備中", UIKit.SuccessColor), UIKit.Center);
            else if (owned)
            {
                if (UIKit.ColorButton(b, "装備する", Color.gray))
                {
                    if (weapon) Progress.Weapon = id; else Progress.Armor = id;
                    note = $"{g.Name}を装備した！";
                }
            }
            else if (g.Recipe != null)
            {
                // 風船割りの素材で作る
                bool can = BattleData.CanCraft(g, Progress.CraftCount);
                if (UIKit.ColorButton(b, can ? "作る" : "素材が足りない", can ? UIKit.SuccessColor : Color.gray, UIKit.Button, can))
                {
                    for (int i = 0; i < g.Recipe.Length; i++) Progress.AddCraft((CraftMaterial)i, -g.Recipe[i]);
                    if (weapon) { Progress.GiveWeapon(id); Progress.Weapon = id; }
                    else { Progress.GiveArmor(id); Progress.Armor = id; }
                    note = $"{g.Name}を作って、装備した！";
                }
            }
            else if (UIKit.ColorButton(b, $"買う（{g.Price}枚）", UIKit.AccentColor, UIKit.Button, true, primary: id == FirstNotOwned(weapon)))
            {
                if (Progress.Spend(g.Price))
                {
                    if (weapon) { Progress.GiveWeapon(id); Progress.Weapon = id; }
                    else { Progress.GiveArmor(id); Progress.Armor = id; }
                    note = $"{g.Name}を買って、装備した！";
                }
                else note = "金貨が足りない…。草むらのアルカナをたおすと、金貨が手に入るよ。";
            }
            y += 80;
        }

        private static int FirstNotOwned(bool weapon)
        {
            int n = weapon ? BattleData.Weapons.Length : BattleData.Armors.Length;
            for (int i = 0; i < n; i++)
            {
                var g = weapon ? BattleData.Weapons[i] : BattleData.Armors[i];
                if (g.Recipe == null && !(weapon ? Progress.OwnsWeapon(i) : Progress.OwnsArmor(i))) return i;
            }
            return -1;
        }

        private void DrawLibrary(Rect p)
        {
            UIKit.Label(new Rect(p.x + 30, p.y + 80, p.width - 60, 120),
                "タイピングバトルで打つ言葉を えらべるよ。\n「おまかせ」なら、ワードブロックの人は日本語、Python の人は英語になる。", UIKit.Explain);
            var cur = Progress.TypingLang;
            string[] labels = { "おまかせ", "日本語（ローマ字）", "英語（Python）" };
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(p.x + 60 + i * 290, p.y + 230, 270, 70);
                bool on = (int)cur == i;
                if (UIKit.ColorButton(r, (on ? "● " : "") + labels[i], on ? UIKit.AccentColor : Color.gray, UIKit.Button, true, primary: on))
                {
                    Progress.TypingLang = (Progress.TypingLanguage)i;
                    note = $"タイピングの言葉を「{labels[i]}」にした。";
                }
            }
            UIKit.Label(new Rect(p.x + 30, p.y + 330, p.width - 60, 200),
                "ランクが上がると、お手本が少しずつ むずかしくなるよ。\n" +
                "日本語：ローマ字 → ひらがな → 漢字 → 文\n" +
                "英語：命令の単語 → 1行のコード → 意味を見てコードを書く\n" +
                "手が止まったり まちがえたりすると、お手本がうっすら出てくるので安心してね。", UIKit.Body);
        }
    }
}
