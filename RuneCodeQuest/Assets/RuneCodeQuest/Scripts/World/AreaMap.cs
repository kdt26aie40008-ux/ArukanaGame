using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 遺跡のエリア（依頼で行く場所）のマップ。AreaLayout の地図から、章ごとの景色でタイルを並べる。
    /// 町とはなれた場所（Origin）に作るので、町はそのまま残しておける。
    /// </summary>
    public class AreaMap : MonoBehaviour, IWalkMap
    {
        public int Stage { get; private set; }
        public AreaLayout Layout { get; private set; }
        public QuestTheme Theme { get; private set; }
        public int Chapter { get; private set; }
        public Vector2 Origin { get; private set; }

        public int Width => Layout.Width;
        public int Height => Layout.Height;

        /// <summary>宿舎の中か（遺跡のエリアではない）</summary>
        public bool IsInn => Layout.IsInn;

        /// <summary>クエストを終えると、ゴーレムが橋を直す</summary>
        public bool BridgeFixed => Progress.StatusOf(Stage) != Progress.NotPlayed;

        private readonly List<SpriteRenderer> bridges = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> chests = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> herbs = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> digs = new List<SpriteRenderer>();
        private SpriteRenderer goal;
        private Sprite bridgeArt; // 村の橋の画像（直った橋）
        private SpriteRenderer bridgeBig; // 村の橋の画像を、たてに1枚で 橋ぜんたいに のばしたもの

        public void Build(int stage, Vector2 origin)
        {
            Stage = stage;
            Origin = origin;
            Layout = AreaLayout.Generate(stage);
            Theme = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            Chapter = QuestDatabase.ChapterOf(stage).Number;
            // 景色は 今いる村と同じ（村の地面の画像 Resources/Art/Town/〈村〉_地面・地面2・草・草むら・水・橋・木.png。なければ プログラムでかいた絵）
            int theme = TownData.Current.TileTheme;
            bool desert = TownData.Current.Desert; // さばくの村：枯れ草むらと 枯れ木
            string vn = TownData.Current.Name;
            Sprite Art(string n) => ArtOverrides.Sized($"Art/Town/{vn}_{n}", 1f, 0.5f);
            Sprite gGround = Art("地面"), gGround2 = Art("地面2"), gGrass = Art("草"), gTall = Art("草むら"), gWater = Art("水");
            var tallKinds = TownMap.TallKinds(Art);
            Sprite gOuter = Art("外壁"), gPillar = Art("外壁柱"), gCorner = Art("外壁かど");
            bridgeArt = Art("橋");
            var gTree = ArtOverrides.Grounded($"Art/Town/{vn}_木", 1.3f, 0.5f);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = Layout.Grid[x, y];
                    var pos = ToWorld(x, y);
                    Sprite plain = gGround != null ? (gGround2 != null && (x * 7 + y * 13) % 6 == 0 ? gGround2 : gGround) : null;
                    Sprite ground = c == '#' ? TownMap.WallTile(Layout.Grid, x, y, gOuter, gPillar, gCorner) ?? SpriteFactory.WallOf(theme)
                        : c == '~' || c == '=' ? gWater ?? SpriteFactory.WaterOf(theme)
                        : c == ',' ? gGrass ?? SpriteFactory.GrassOf(theme)
                        : c == 'w' ? (gTall != null ? plain ?? SpriteFactory.GrassOf(theme) : desert ? TownArt.DryGrass : TownArt.TallGrass) // 草むらの下は 地面
                        : plain ?? SpriteFactory.FloorOf(theme);
                    Tile(pos, ground, 0);
                    if (c == 'w' && gTall != null) TownMap.PlaceTall(Tile(pos, gTall, 1), tallKinds, x, y); // 草むら（アルカナが出る）
                    switch (c)
                    {
                        case '=': bridges.Add(Tile(pos, SpriteFactory.BridgeBroken, 1)); break;
                        case 'T': Tile(pos, gTree ?? (desert ? TownArt.DeadTree : QuestArt.Decor(3)), 2); break;
                        case 'R': Tile(pos, QuestArt.Obstacle(ObstacleKind.Rock), 2); break;
                        case 'E': Tile(pos, SpriteFactory.Gate, 1); break;
                        case 'G': Tile(pos, SpriteFactory.Golem, 5); break;
                        case 'g': goal = Tile(pos, QuestArt.Goal(Theme.Goal, false), 4); break;
                        case 'S': Tile(pos, TownArt.Board, 2); break;
                        case 'o': Tile(pos, TownArt.Spring(Chapter), 2); break;
                        case 'x': digs.Add(Tile(pos, TownArt.Sparkle, 1)); break;
                        case 'v': Person(pos, CharacterArt.Build(new CharacterLook { HairStyle = 9, Hair = 5, Cloth = 7, Skin = 2, Hat = 6, Outfit = 5, Eyes = 1, Accessory = 1 })); break;
                    }
                }
            }
            if (bridgeArt != null) PlaceBridgeArt();
            for (int i = 0; i < Layout.Chests.Count; i++)
            {
                var (x, y) = Layout.Chests[i];
                var sr = Tile(ToWorld(x, y), QuestArt.Goal(GoalKind.Chest, false), 4);
                if (i == Layout.Chests.Count - 1) sr.transform.localScale = Vector3.one * 1.3f; // 大きな宝箱
                chests.Add(sr);
            }
            foreach (var (x, y) in Layout.Herbs) herbs.Add(Tile(ToWorld(x, y), QuestArt.Item(ItemKind.Herb), 4));
            Refresh();
        }

        /// <summary>
        /// 村の橋の画像は 横にわたる橋なので、90度まわして たてにする。
        /// 橋のマス（=）の 四角いっぱいに 1枚で のばす（両はしの 岸の草は 少し はみ出す）
        /// </summary>
        private void PlaceBridgeArt()
        {
            int x0 = Width, y0 = Height, x1 = -1, y1 = -1;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                if (Layout.Grid[x, y] == '=') { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
            if (x1 < 0) return;
            float w = x1 - x0 + 1, h = y1 - y0 + 1 + 0.5f;
            bridgeBig = Tile((ToWorld(x0, y0) + ToWorld(x1, y1)) / 2f, bridgeArt, 1);
            var size = bridgeArt.bounds.size;
            // まわすと 絵の横（x）が たてになる
            bridgeBig.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bridgeBig.transform.localScale = new Vector3(h / size.x, w / size.y, 1f);
        }

        /// <summary>宿舎の中を作る（木の床・かべ・家具・おかみ）</summary>
        public void BuildInn(Vector2 origin)
        {
            Stage = 0;
            Origin = origin;
            Layout = AreaLayout.Inn();
            Chapter = 1;
            // 画像の じゅうたん・カウンターは、1マスずつ ならべず、場所いっぱいに 1枚で のばす
            var rugArt = ArtOverrides.Inn("じゅうたん");
            var counterArt = ArtOverrides.Inn("カウンター");
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = Layout.Grid[x, y];
                    var pos = ToWorld(x, y);
                    Tile(pos, c == '#' ? InnArt.Wall : c == 'v' ? InnArt.WallWindow : c == 'r' && rugArt == null ? InnArt.Rug : InnArt.Floor, 0);
                    var furniture = c == 'c' && counterArt != null ? null : InnArt.Furniture(c);
                    if (furniture != null) Tile(pos, furniture, 2);
                    if (c == 'n')
                        Person(pos, ArtOverrides.Character("宿舎のおかみ") ?? CharacterArt.Build(new CharacterLook { HairStyle = 3, Hair = 0, Cloth = 9, Skin = 1, Outfit = 4, Accessory = 2 }));
                }
            }
            if (rugArt != null) Stretch(rugArt, 'r', 1, 0f);
            if (counterArt != null) Stretch(counterArt, 'c', 3, 0.6f);
            // かべの ランプ（かざり）
            var lamp = ArtOverrides.Inn("ランプ");
            if (lamp != null)
                for (int x = 2; x < Width - 1; x += 5)
                    if (Layout.Grid[x, 0] == '#') { var sr = Tile(ToWorld(x, 0) + new Vector2(0f, -0.15f), lamp, 1); sr.transform.localScale = Vector3.one * 0.6f; }
        }

        /// <summary>
        /// 同じ文字のマスが ならんでいる所（いちばん外の四角）に、絵を1枚 のばして置く。
        /// extraUp：上へ 少し はみ出させる量（カウンターは 前から見ると 高さがあるので）
        /// </summary>
        private void Stretch(Sprite art, char c, int order, float extraUp)
        {
            int x0 = Width, y0 = Height, x1 = -1, y1 = -1;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                if (Layout.Grid[x, y] == c) { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y); }
            if (x1 < 0) return;
            float w = x1 - x0 + 1, h = y1 - y0 + 1 + extraUp;
            // 四角の まん中（下はしは 一番下のマスの下）
            var bottomLeft = ToWorld(x0, y1) - new Vector2(0.5f, 0.5f);
            var sr = Tile(bottomLeft + new Vector2(w / 2f, h / 2f), art, order);
            var size = art.bounds.size;
            sr.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
        }

        /// <summary>橋・ゴール・宝箱・薬草の見た目を、今の進み具合に合わせる</summary>
        public void Refresh()
        {
            bool done = BridgeFixed;
            foreach (var sr in bridges)
            {
                sr.sprite = done ? SpriteFactory.Bridge : SpriteFactory.BridgeBroken;
                sr.enabled = !done || bridgeBig == null; // 村の橋の画像があれば、直った橋は その1枚
            }
            if (bridgeBig != null) bridgeBig.enabled = done;
            if (goal != null) goal.sprite = QuestArt.Goal(Theme.Goal, done);
            for (int i = 0; i < chests.Count; i++) chests[i].sprite = QuestArt.Goal(GoalKind.Chest, ChestOpened(i));
            for (int i = 0; i < herbs.Count; i++) herbs[i].enabled = !HerbTaken(i);
            for (int i = 0; i < digs.Count; i++) digs[i].enabled = !DigFound(i);
        }

        // ------------------------------------------------------------------
        // 宝箱・薬草（取ったかどうかは Progress.AreaFlags に記録）
        //   ビット 0〜2: 宝箱（最後が大きな宝箱）  4〜5: 薬草  8: お話を見た
        // ------------------------------------------------------------------

        /// <summary>ビット 8: このエリアのお話（着いたときの説明）を見たか。2回目からは看板で読む</summary>
        private const int IntroBit = 1 << 8;

        public static bool IntroSeenFor(int stage) => (Progress.AreaFlags(stage) & IntroBit) != 0;
        public bool IntroSeen => IntroSeenFor(Stage);
        public void MarkIntroSeen() => Progress.SetAreaFlags(Stage, Progress.AreaFlags(Stage) | IntroBit);

        public bool ChestOpened(int i) => (Progress.AreaFlags(Stage) & (1 << i)) != 0;

        // ビット 9〜10: キラキラ光る地面を調べた　11: 旅人のクイズに正解した
        public bool DigFound(int i) => (Progress.AreaFlags(Stage) & (1 << (9 + i))) != 0;
        public void MarkDig(int i) { Progress.SetAreaFlags(Stage, Progress.AreaFlags(Stage) | (1 << (9 + i))); Refresh(); }
        public bool QuizDone => (Progress.AreaFlags(Stage) & (1 << 11)) != 0;
        public void MarkQuiz() => Progress.SetAreaFlags(Stage, Progress.AreaFlags(Stage) | (1 << 11));

        /// <summary>回復の泉の名前（砂の遺跡はオアシス、雪の遺跡は温泉）</summary>
        public string SpringName => Chapter == 2 ? "オアシス" : Chapter == 4 ? "温泉" : "回復の泉";
        public Vector2 SpringWorld => ToWorld(Layout.SpringX, Layout.SpringY);
        public Vector2 TravelerWorld => ToWorld(Layout.TravelerX, Layout.TravelerY);
        public Vector2 DigWorld(int i) => ToWorld(Layout.DigSpots[i].x, Layout.DigSpots[i].y);
        public int DigAt(Vector2Int t) => Layout.DigSpots.IndexOf((t.x, t.y));

        /// <summary>砂の遺跡のサボテン（ぶつかるとトゲが ささる）の場所</summary>
        public IEnumerable<Vector2> Cacti()
        {
            if (Chapter != 2 || IsInn) yield break;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (Layout.Grid[x, y] == 'T') yield return ToWorld(x, y);
        }
        public bool HerbTaken(int i) => (Progress.AreaFlags(Stage) & (1 << (4 + i))) != 0;
        public bool IsBigChest(int i) => i == Layout.Chests.Count - 1;

        public void MarkChest(int i) { Progress.SetAreaFlags(Stage, Progress.AreaFlags(Stage) | (1 << i)); Refresh(); }
        public void MarkHerb(int i) { Progress.SetAreaFlags(Stage, Progress.AreaFlags(Stage) | (1 << (4 + i))); Refresh(); }

        /// <summary>その場所にある宝箱の番号（なければ -1）</summary>
        public int ChestAt(Vector2Int t) => Layout.Chests.IndexOf((t.x, t.y));
        public int HerbAt(Vector2Int t) => Layout.Herbs.IndexOf((t.x, t.y));

        // ------------------------------------------------------------------
        // 座標
        // ------------------------------------------------------------------

        public Vector2 ToWorld(int x, int y) => Origin + new Vector2(x, Height - 1 - y);

        public Vector2Int ToTile(Vector2 world)
        {
            var local = world - Origin;
            return new Vector2Int(Mathf.RoundToInt(local.x), Height - 1 - Mathf.RoundToInt(local.y));
        }

        public char CharAt(Vector2 world)
        {
            var t = ToTile(world);
            return Layout.At(t.x, t.y);
        }

        public bool IsWalkable(Vector2 world) => AreaLayout.Walkable(CharAt(world), BridgeFixed);

        public bool IsTallGrass(Vector2 world) => CharAt(world) == 'w';

        public Vector2 ExitWorld => ToWorld(Layout.ExitX, Layout.ExitY);
        public Vector2 GolemWorld => ToWorld(Layout.GolemX, Layout.GolemY);
        public Vector2 SignWorld => ToWorld(Layout.SignX, Layout.SignY);
        public Vector2 ChestWorld(int i) => ToWorld(Layout.Chests[i].x, Layout.Chests[i].y);

        public List<Vector2> FindPath(Vector2 from, Vector2 to) => WalkPath.Find(this, from, to);

        /// <summary>人（旅人・おかみ）：町の人と 同じ大きさ（足もとは マスの下のまま）</summary>
        private void Person(Vector2 pos, Sprite sprite)
        {
            var sr = Tile(pos + new Vector2(0f, (TownMap.NpcScale - 1f) * 0.5f), sprite, 5);
            sr.transform.localScale = new Vector3(TownMap.NpcScale, TownMap.NpcScale, 1f);
        }

        private SpriteRenderer Tile(Vector2 pos, Sprite sprite, int order)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
