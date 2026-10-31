using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：地図とピン。
    ///
    /// ・持ち物の横の「地図」ボタン（または M キー）で、今いるマップ（町・遺跡のエリア）の地図を開く
    /// ・地図には 現在地（向きの矢印）・東西南北・建物や目的地 が出る
    /// ・地図をクリック（または矢印キー＋Enter）でピンを立てる。同じ場所をもう一度押すとピンが消える
    /// ・ピンを立てると、ゲーム画面にピンの印と「あと○マス」が出る。着いたらピンは消える
    /// </summary>
    public partial class GameManager
    {
        private bool showMap;
        private Texture2D mapTexture;
        private Vector2Int mapCursor;           // キーボードで動かす地図のカーソル
        private Vector2Int? townPin, areaPin;   // ピンのマス（町とエリアで別々）
        private GameObject pinObject;

        /// <summary>いまいる村の 区画の名前（地図に出す）</summary>
        private static string[] TownDistricts => TownData.Current.DistrictLong;

        private bool InArea => area != null;
        private int MapW => InArea ? area.Width : town.Width;
        private int MapH => InArea ? area.Height : town.Height;

        private Vector2Int? CurrentPin
        {
            get => InArea ? areaPin : townPin;
            set { if (InArea) areaPin = value; else townPin = value; }
        }

        private Vector2 TileToWorld(Vector2Int t) => InArea ? area.ToWorld(t.x, t.y) : town.ToWorld(t.x, t.y);
        private Vector2Int PlayerTile => InArea ? area.ToTile(player.transform.position) : town.ToTile(player.transform.position);

        private static string DirName(Vector2Int d) => d == Vector2Int.up ? "北" : d == Vector2Int.right ? "東" : d == Vector2Int.down ? "南" : "西";
        private static string DirArrow(Vector2Int d) => d == Vector2Int.up ? "▲" : d == Vector2Int.right ? "▶" : d == Vector2Int.down ? "▼" : "◀";

        /// <summary>今いる場所の名前</summary>
        private string PlaceName()
        {
            if (InArea) return area.IsInn ? "宿舎の中" : area.Theme.Area;
            var t = PlayerTile;
            return TownDistricts[TownData.AreaOf(t.x, t.y)];
        }

        // ------------------------------------------------------------------
        // 開く・とじる
        // ------------------------------------------------------------------

        /// <summary>M キーで地図を開く・とじる（Esc でもとじる）。地図を開いているあいだは true（ほかの操作はしない）</summary>
        private bool UpdateMapKeys()
        {
            if (!showInventory && KeyInput.WasPressed(KeyCode.M)) ToggleMap();
            else if (showMap && KeyInput.WasPressed(KeyCode.Escape)) ToggleMap();
            return showMap;
        }

        private void ToggleMap()
        {
            showMap = !showMap;
            KeyInput.Clear();
            if (!showMap) return;
            showInventory = false;
            player.StopWalking();
            mapCursor = CurrentPin ?? PlayerTile;
            BuildMapTexture();
        }

        /// <summary>地図の絵（1マス = 1ドット）を、今のマップから作る</summary>
        private void BuildMapTexture()
        {
            int w = MapW, h = MapH;
            if (mapTexture == null || mapTexture.width != w || mapTexture.height != h)
                mapTexture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    mapTexture.SetPixel(x, h - 1 - y, MapColor(x, y));
            mapTexture.Apply();
        }

        private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

        /// <summary>地図を さばくの色で かくか（砂の国の町・砂の国の章の遺跡）</summary>
        private bool DesertMap => InArea ? BattleData.RegionOfChapter(area.Chapter) == 3 : TownData.Current.Desert;

        private Color32 MapColor(int x, int y)
        {
            var world = TileToWorld(new Vector2Int(x, y));
            char c = InArea ? area.Layout.At(x, y) : town.CharAt(world);
            switch (c)
            {
                case '#': return C(70, 66, 72);
                case ',': return C(150, 195, 120);
                case 'w': return DesertMap ? C(200, 160, 90) : C(70, 150, 70);   // さばくは 枯れ草むら
                case '~': return C(70, 130, 215);
                case 'T': return C(40, 100, 50);
                case 'R': return C(130, 125, 120);
                case 'F': return C(140, 200, 240);
                case 'Q': case 'S': return C(160, 110, 60);
                case '=': return !InArea || area.BridgeFixed ? C(170, 120, 70) : C(90, 60, 40); // 町の橋は いつも通れる
                case '^': return InArea ? C(215, 200, 165) : C(232, 220, 188); // 町の高い所
                case 'H': return C(105, 92, 82); // がけの正面
                case 'L': case 'l': return C(190, 185, 175); // 石の階段
                case '/': return C(205, 190, 150); // 坂道
                case '|': return C(160, 110, 60);  // はしご
                case 'E': return C(240, 200, 90);
                case 'G': return InArea ? C(150, 130, 105) : C(130, 90, 55); // 町では畑の土
                case 'g': return C(255, 230, 90);
                case 'C': case 'K': return C(200, 150, 60);
                case 'h': return InArea ? C(90, 200, 100) : C(105, 92, 82); // エリアでは薬草・町では がけのふち
                // 宿舎の中
                case 'v': return area != null && area.IsInn ? C(150, 200, 235) : C(240, 130, 180); // 宿舎の窓・遺跡の旅人
                case 'o': return C(110, 200, 245);
                case 'x': return C(255, 225, 90);
                case 'r': return C(170, 50, 60);
                case 'b': return C(70, 120, 200);
                case 'f': return C(240, 130, 50);
                case 'm': return C(170, 215, 240);
                case 'd': case 'k': case 't': case 'c': return C(130, 85, 45);
                case 'p': return C(70, 160, 80);
                case 'n': return C(240, 130, 180);
                case 'B':
                    var b = town.BuildingAt(world);
                    return b != null && town.Repaired(b) ? new Color32(b.Roof.r, b.Roof.g, b.Roof.b, 255) : C(120, 115, 112);
                default:
                    if (c >= '1' && c <= '8') return town.IsWalkable(world) ? C(215, 200, 165) : C(120, 90, 60); // がれき
                    if (!InArea && town.LevelAt(world) > 0) return PixelArt.Add(C(215, 200, 165), 12 * town.LevelAt(world)); // 町の高い所ほど 明るい
                    return C(215, 200, 165); // 道
            }
        }

        // ------------------------------------------------------------------
        // ピン
        // ------------------------------------------------------------------

        /// <summary>そのマスにピンを立てる。もうそこにピンがあれば消す</summary>
        private void TogglePin(Vector2Int t)
        {
            if (t.x < 0 || t.y < 0 || t.x >= MapW || t.y >= MapH) return;
            if (CurrentPin.HasValue && CurrentPin.Value == t)
            {
                CurrentPin = null;
                ShowToast("ピンを消した。", 2f);
            }
            else CurrentPin = t;
            questPinned = false; // 自分で立てた・消したピンは 自動で 動かさない
        }

        /// <summary>ピンの印（ゲーム画面の中）を、今のマップのピンに合わせる。着いたらピンを消す</summary>
        private void UpdatePin()
        {
            if (pinObject == null)
            {
                pinObject = new GameObject("Pin");
                var sr = pinObject.AddComponent<SpriteRenderer>();
                sr.sprite = TownArt.Pin;
                sr.sortingOrder = 8;
            }
            bool active = (State == GameState.Town || State == GameState.Area) && CurrentPin.HasValue;
            pinObject.SetActive(active);
            if (!active) return;

            var world = TileToWorld(CurrentPin.Value);
            pinObject.transform.position = (Vector3)(world + Vector2.up * (0.6f + 0.1f * Mathf.Sin(Time.time * 5f)));
            // 歩けないマス（建物など）にピンを立てたときは、となりまで来たら着いたことにする
            bool walkable = InArea ? area.IsWalkable(world) : town.IsWalkable(world);
            if (!showMap && Vector2.Distance(player.transform.position, world) < (walkable ? 0.6f : 1.2f))
            {
                CurrentPin = null;
                pinObject.SetActive(false);
                ShowToast(questPinned ? "目的の場所に着いた！" : "目印のピンに着いた！ ピンは消えたよ。", 3f);
                questPinned = false;
            }
        }

        /// <summary>あと何マスか（たて＋よこ）</summary>
        private int PinDistance()
        {
            var p = PlayerTile;
            var t = CurrentPin.Value;
            return Mathf.Abs(p.x - t.x) + Mathf.Abs(p.y - t.y);
        }

        /// <summary>ゲーム画面：ピンの上に「目印 あと○マス」、画面の外なら はしに矢印</summary>
        private void DrawPinIndicator()
        {
            if (!CurrentPin.HasValue) return;
            var world = TileToWorld(CurrentPin.Value);
            string text = $"目印 あと{PinDistance()}マス";
            DrawWorldLabel(world + Vector2.up * 1.5f, UIKit.Colorize(text, new Color(1f, 0.5f, 0.5f)));
            DrawEdgePointer(world, text, new Color(1f, 0.5f, 0.5f), 56);
        }

        // ------------------------------------------------------------------
        // 画面
        // ------------------------------------------------------------------

        /// <summary>持ち物のボタンの横の「地図」ボタン</summary>
        private void DrawMapButton()
        {
            var r = new Rect(960, 8, 60, 52);
            if (UIKit.ColorButton(r, "", showMap ? UIKit.AccentColor : Color.gray, UIKit.SmallButton))
                ToggleMap();
            UIKit.Texture(new Rect(r.x + 12, r.y + 8, 36, 36), SpriteFactory.TextureOf(TownArt.MapIcon));
            UIKit.Label(new Rect(r.x - 10, r.yMax, r.width + 20, 26), "地図(M)", UIKit.SmallCenter);
        }

        private void DrawMap()
        {
            UIKit.Nav("map", false);
            HandleMapKeys();
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.55f));
            var panel = new Rect(50, 64, 1180, 610);
            UIKit.Panel(panel);

            var me = PlayerTile;
            var facing = player.Facing;
            UIKit.Label(new Rect(panel.x + 24, panel.y + 10, 900, 44),
                $"地図　{UIKit.Colorize("現在地：" + PlaceName(), UIKit.AccentColor)}　向き：{DirName(facing)} {DirArrow(facing)}", UIKit.Heading);
            if (UIKit.ColorButton(new Rect(panel.xMax - 130, panel.y + 12, 110, 44), "とじる", new Color(0.8f, 0.4f, 0.4f)))
            {
                ToggleMap();
                return;
            }

            // 地図（マスの大きさは、パネルに入るように決める）
            int w = MapW, h = MapH;
            var box = new Rect(panel.x + 24, panel.y + 66, 900, 520);
            float tile = Mathf.Floor(Mathf.Min(box.width / w, box.height / h));
            var mapRect = new Rect(box.x + (box.width - tile * w) / 2, box.y + (box.height - tile * h) / 2, tile * w, tile * h);
            UIKit.Fill(Grow(mapRect, 4), new Color(0.35f, 0.28f, 0.18f));
            UIKit.Texture(mapRect, mapTexture);
            Rect TileRect(Vector2Int t) => new Rect(mapRect.x + t.x * tile, mapRect.y + t.y * tile, tile, tile);

            // 東西南北
            UIKit.Label(new Rect(mapRect.center.x - 40, mapRect.y - 30, 80, 26), UIKit.Colorize("北 ▲", UIKit.WarnColor), UIKit.SmallCenter);
            UIKit.Label(new Rect(mapRect.center.x - 40, mapRect.yMax + 2, 80, 26), "南 ▼", UIKit.SmallCenter);
            UIKit.Label(new Rect(mapRect.x - 34, mapRect.center.y - 30, 30, 60), "◀\n西", UIKit.SmallCenter);
            UIKit.Label(new Rect(mapRect.xMax + 4, mapRect.center.y - 30, 30, 60), "▶\n東", UIKit.SmallCenter);

            // 名前（建物・目的地・エリアの物）
            if (InArea && area.IsInn)
            {
                foreach (var (ch, label) in new[] { ('b', "ベッド"), ('k', "記録の本"), ('m', "鏡"), ('n', "おかみ"), ('E', "出口") })
                    MapLabel(TileRect(area.ToTile(InnSpot(ch))), label);
            }
            else if (InArea)
            {
                MapLabel(TileRect(new Vector2Int(area.Layout.GolemX, area.Layout.GolemY)), "ゴーレム");
                MapLabel(TileRect(new Vector2Int(area.Layout.ExitX, area.Layout.ExitY)), "出口");
                for (int i = 0; i < area.Layout.Chests.Count; i++)
                    if (!area.ChestOpened(i)) MapLabel(TileRect(new Vector2Int(area.Layout.Chests[i].x, area.Layout.Chests[i].y)), "宝箱");
                MapLabel(TileRect(new Vector2Int(area.Layout.SpringX, area.Layout.SpringY)), area.SpringName);
                MapLabel(TileRect(new Vector2Int(area.Layout.TravelerX, area.Layout.TravelerY)), "旅人");
                for (int i = 0; i < area.Layout.DigSpots.Count; i++)
                    if (!area.DigFound(i)) MapLabel(TileRect(new Vector2Int(area.Layout.DigSpots[i].x, area.Layout.DigSpots[i].y)), "キラキラ");
            }
            else
            {
                foreach (var b in TownData.Buildings)
                    MapLabel(new Rect(mapRect.x + b.X * tile, mapRect.y + b.Y * tile, b.W * tile, b.H * tile), b.Name);
                foreach (var (bx, by) in TownData.Boards) MapLabel(TileRect(new Vector2Int(bx, by)), "依頼板");
                for (int i = 0; i < TownData.TownChests.Length; i++)
                    if (!TownMap.ChestOpened(i)) MapLabel(TileRect(new Vector2Int(TownData.TownChests[i].x, TownData.TownChests[i].y)), "宝箱");
                foreach (var npc in town.ActiveNpcs())
                    if (npc.Role != NpcRole.Talk)
                        MapLabel(TileRect(new Vector2Int(npc.X, npc.Y)),
                            RoleLabel(npc.Role));
                if (town.Destination != null)
                {
                    var d = TileRect(new Vector2Int(town.Destination.X, town.Destination.Y));
                    UIKit.Fill(Grow(d, 2), new Color(1f, 0.85f, 0.2f));
                    MapLabel(d, "★目的地");
                }
                DrawStoryOnMap(TileRect);
            }

            // ピン
            if (CurrentPin.HasValue)
            {
                var pr = TileRect(CurrentPin.Value);
                UIKit.Texture(new Rect(pr.center.x - 14, pr.center.y - 26, 28, 28), SpriteFactory.TextureOf(TownArt.Pin));
            }

            // 現在地（点滅する丸と、向きの矢印）
            var mr = TileRect(me);
            if (Mathf.Repeat(Time.realtimeSinceStartup, 1f) < 0.7f)
                UIKit.Fill(Grow(mr, Mathf.Max(3, tile * 0.3f)), new Color(0.2f, 0.6f, 1f));
            UIKit.Label(Grow(mr, 10), UIKit.Colorize(DirArrow(facing), Color.white), UIKit.SmallCenter);

            // キーボードのカーソル
            var cr = Grow(TileRect(mapCursor), 2);
            UIKit.Fill(new Rect(cr.x, cr.y, cr.width, 2), Color.yellow);
            UIKit.Fill(new Rect(cr.x, cr.yMax - 2, cr.width, 2), Color.yellow);
            UIKit.Fill(new Rect(cr.x, cr.y, 2, cr.height), Color.yellow);
            UIKit.Fill(new Rect(cr.xMax - 2, cr.y, 2, cr.height), Color.yellow);

            // マウス・タッチパッド：地図のマスを押すとピンを立てる／消す
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 ui = e.mousePosition / UIKit.Scale; // 画面の部品の中なので、拡大のぶんだけもどす
                if (mapRect.Contains(ui))
                {
                    var t = new Vector2Int(Mathf.FloorToInt((ui.x - mapRect.x) / tile), Mathf.FloorToInt((ui.y - mapRect.y) / tile));
                    mapCursor = t;
                    TogglePin(t);
                    e.Use();
                }
            }

            // 右がわ：説明
            var side = new Rect(panel.x + 944, panel.y + 70, 210, 520);
            UIKit.Fill(side, UIKit.SubPanelColor);
            string pinText = CurrentPin.HasValue ? $"ピン：あと {PinDistance()}マス" : "ピン：なし";
            UIKit.Label(new Rect(side.x + 12, side.y + 8, side.width - 24, side.height - 16),
                $"{UIKit.Colorize("●", new Color(0.2f, 0.6f, 1f))} いまいる場所\n" +
                $"（{DirArrow(facing)} は向いている方向）\n\n" +
                $"{UIKit.Colorize("●", new Color(1f, 0.4f, 0.4f))} ピン（目印）\n" +
                (InArea ? "" : $"{UIKit.Colorize("●", new Color(1f, 0.85f, 0.2f))} 依頼の目的地\n") +
                $"\n{pinText}\n\n" +
                "地図のマスを押すと ピンが立つ。同じ場所をもう一度押すと消える。\n\n" +
                "キーボード：\n矢印 カーソル\nEnter ピン\nM・Esc とじる", UIKit.Small);
            if (CurrentPin.HasValue && UIKit.ColorButton(new Rect(side.x + 12, side.yMax - 56, side.width - 24, 44), "ピンを消す", Color.gray))
                CurrentPin = null;
        }

        /// <summary>
        /// 地図：ストーリークエストの場所に「！」（調べる場所・まだ話を聞いていない人）。
        /// ピンク色の四角が ちかちか光る
        /// </summary>
        private void DrawStoryOnMap(System.Func<Vector2Int, Rect> tileRect)
        {
            var q = ActiveStory();
            bool travel = TravelTime(); // 旅立つときは 馬車の御者に「！」
            bool board = BoardTime();   // 依頼を受けるときは 依頼板に「！」
            var extra = ExtraMarkTarget(); // 目的地・工事の看板
            if (q == null && !travel && !board && !extra.HasValue) return;
            var pink = new Color(1f, 0.45f, 0.9f);
            bool blink = Mathf.Repeat(Time.realtimeSinceStartup, 0.8f) < 0.55f;

            void Mark(Vector2Int t, string label)
            {
                var r = tileRect(t);
                var box = new Rect(r.center.x - 13, r.center.y - 13, 26, 26);
                UIKit.Fill(Grow(box, 2), Color.white);
                UIKit.Fill(box, blink ? pink : pink * 0.75f);
                UIKit.Label(box, UIKit.Colorize("！", Color.white), UIKit.SmallCenter);
                MapLabel(new Rect(r.x, r.y - 8, r.width, r.height), label);
            }

            if (board) foreach (var b in OpenBoards()) Mark(b, "！依頼板");
            if (extra.HasValue && (q == null || q.Kind != StoryKind.Investigate)) Mark(extra.Value, town.Destination != null ? "！目的地" : "！工事の看板");
            if (q != null && q.Kind == StoryKind.Investigate)
            {
                Mark(new Vector2Int(q.X, q.Y), "！" + q.SpotName);
                return;
            }
            foreach (var n in town.ActiveNpcs())
                if (IsMarkedNpc(n)) Mark(new Vector2Int(n.X, n.Y), "！" + n.Name);
        }

        private static void MapLabel(Rect r, string text)
        {
            var lr = new Rect(r.center.x - 70, r.y - 24, 140, 24);
            UIKit.Fill(new Rect(lr.center.x - text.Length * 8 - 6, lr.y + 2, text.Length * 16 + 12, 20), new Color(0f, 0f, 0f, 0.6f));
            UIKit.Label(lr, text, UIKit.SmallCenter);
        }

        private static Rect Grow(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + d * 2, r.height + d * 2);

        private void HandleMapKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            var move = e.keyCode == KeyCode.UpArrow ? Vector2Int.down : e.keyCode == KeyCode.DownArrow ? Vector2Int.up
                : e.keyCode == KeyCode.LeftArrow ? Vector2Int.left : e.keyCode == KeyCode.RightArrow ? Vector2Int.right : Vector2Int.zero;
            if (move != Vector2Int.zero)
            {
                // 地図の上は「上」が北（マスの y は下に向かってふえる）
                mapCursor = new Vector2Int(Mathf.Clamp(mapCursor.x + move.x, 0, MapW - 1), Mathf.Clamp(mapCursor.y + move.y, 0, MapH - 1));
                e.Use();
            }
            else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space)
            {
                TogglePin(mapCursor);
                e.Use();
            }
        }
    }
}
