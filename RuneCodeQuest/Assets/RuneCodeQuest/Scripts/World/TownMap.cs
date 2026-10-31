using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 村のマップ（いまいる村の TownData の地図から、タイル・建物・町の人を並べる）。
    /// 建物が直ると、見た目が変わり、その建物の番号のがれきが消えて先へ進めるようになる。
    /// </summary>
    public class TownMap : MonoBehaviour, IWalkMap
    {
        public int Width => TownData.W;
        public int Height => TownData.H;

        private char[,] layout;
        private bool[] repaired = new bool[TownData.Buildings.Length + 1]; // [番号] 1〜（村の建物の数）
        private readonly List<(int number, SpriteRenderer sr)> rubble = new List<(int, SpriteRenderer)>();
        private readonly Dictionary<int, SpriteRenderer> buildingSprites = new Dictionary<int, SpriteRenderer>();
        private readonly List<(NpcInfo npc, GameObject go)> npcs = new List<(NpcInfo, GameObject)>();

        /// <summary>町の人の絵：その村だけの絵（〈村〉_名前。馬車の御者は 国ごとに服がちがう）があれば それ、なければ 名前の絵</summary>
        private static Sprite NpcArt(string name) =>
            ArtOverrides.Character($"{TownData.Current.Name}_{name}") ?? ArtOverrides.Character(name);

        /// <summary>町の人の絵の 大きさ（全員 同じ。足もとは マスの下のまま、上に のびる）</summary>
        public const float NpcScale = 1.3f;

        /// <summary>その建物の仕上げクエストを終えたか（解説を見て通過したときもふくむ）</summary>
        public static bool IsRepaired(BuildingInfo b) => Progress.StatusOf(b.LastStage) != Progress.NotPlayed;

        /// <summary>その建物の本線の依頼のうち、終えた数</summary>
        public static int MainDone(BuildingInfo b)
        {
            int c = 0;
            foreach (int s in Rebuild.MainStages(b))
                if (Progress.StatusOf(s) != Progress.NotPlayed) c++;
            return c;
        }

        /// <summary>
        /// その村へ行けるか（ルーネ村は はじめから。ほかの村は、ひとつ前の村の さいごの建物が直って、
        /// その村のストーリークエストも全部終えたら）
        /// </summary>
        public static bool VillageOpen(int v) => v <= 0 || (v < TownData.VillageCount && VillageCleared(v - 1));

        /// <summary>その建物の依頼が 受けられるか（その建物のストーリークエストを終えていれば）</summary>
        public static bool StoryGateOpen(BuildingInfo b)
        {
            var q = StoryQuests.GateOf(b);
            return q == null || Progress.StoryQuestDone(q.Id);
        }

        /// <summary>その村の さいごの建物を直して、ストーリークエストも全部終えたか</summary>
        public static bool VillageCleared(int v)
        {
            if (!IsRepaired(TownData.Villages[v].LastBuilding)) return false;
            foreach (var q in StoryQuests.Of(v)) if (!Progress.StoryQuestDone(q.Id)) return false;
            return true;
        }

        /// <summary>その村の建物を いくつ直したか</summary>
        public static int RepairedCount(VillageInfo v)
        {
            int c = 0;
            foreach (var b in v.Buildings) if (IsRepaired(b)) c++;
            return c;
        }

        /// <summary>工事できるか（ひとつ前の建物が直っていれば）</summary>
        public bool CanWorkOn(BuildingInfo b) => b.Number == 1 || repaired[b.Number - 1];

        public bool Repaired(BuildingInfo b) => repaired[b.Number];

        public void Build()
        {
            layout = TownData.BuildLayout(TownData.Current, out levels);
            repaired = new bool[TownData.Buildings.Length + 1];
            int theme = TownData.Current.TileTheme; // 村ごとの地面の景色
            // 噴水の画像（Resources/Art/Town/〈村〉_噴水.png）があれば、噴水のマスには 床をしいて、上に 広場ごと 大きく置く
            var fountainArt = ArtOverrides.Sized($"Art/Town/{TownData.Current.Name}_噴水", 4f, 0.5f);
            // 依頼板の画像（Resources/Art/Town/〈村〉_依頼板.png）：横2マス、足元を マスの下に そろえる
            var boardArt = ArtOverrides.Grounded($"Art/Town/{TownData.Current.Name}_依頼板", 2f, 0.5f);
            BoardLabelUp = boardArt != null ? 1.5f : 0.8f;
            // 村の地面の画像（Resources/Art/Town/〈村〉_地面・地面2・草・草むら・水・橋・木.png）。なければ プログラムでかいた絵
            string vn = TownData.Current.Name;
            Sprite Art(string n) => ArtOverrides.Sized($"Art/Town/{vn}_{n}", 1f, 0.5f);
            Sprite gGround = Art("地面"), gGround2 = Art("地面2"), gGrass = Art("草"), gTall = Art("草むら"), gWater = Art("水"), gBridge = Art("橋");
            var tallKinds = TallKinds(Art);
            // 区画のかべ・まわりのかべ（#）の絵（〈村〉_外壁＝横・外壁柱＝たて と かど）
            Sprite gOuter = Art("外壁"), gPillar = Art("外壁柱"), gCorner = Art("外壁かど");
            bool rimWalls = gCorner != null; // かどの絵がある村（サンドリア）は、がけのふち（h）も かべの絵にする
            var gHarbor = Art("港"); // 低い所（高さ0）の地面：港の 木の岸壁・桟橋
            // がけの正面（H）の 石のかべ・石の階段（L）。横に ならべて つながる絵（Resources/Art/Town/〈村〉_壁・階段.png）
            var gWall = ArtOverrides.Grounded($"Art/Town/{vn}_壁", 1f, 0.5f); // 下はしを マスの下に。上は 少し 上のマスに はみ出す
            var gLadder = ArtOverrides.Grounded($"Art/Town/{vn}_はしご", 1f, 0.5f); // はしごの かかった 石のかべ
            ladderArt = gLadder != null;
            var gStairs = Art("階段");
            var gTree = ArtOverrides.Grounded($"Art/Town/{vn}_木", 1.3f, 0.5f);
            // さばくの町：草むらは 枯れ草むら、DeadTrees の木は 枯れ木
            bool desert = TownData.Current.Desert;
            var deadTrees = new HashSet<(int, int)>(TownData.Current.DeadTrees);
            bool NextToTallGrass(int tx, int ty) =>
                (tx > 0 && layout[tx - 1, ty] == 'w') || (tx < Width - 1 && layout[tx + 1, ty] == 'w') ||
                (ty > 0 && layout[tx, ty - 1] == 'w') || (ty < Height - 1 && layout[tx, ty + 1] == 'w');
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = layout[x, y];
                    int lv = levels[x, y];
                    var pos = ToWorld(x, y);
                    bool deadTree = c == 'T' && deadTrees.Contains((x, y));
                    // 草原の地面（画像があれば。ときどき 花のある地面）
                    Sprite plain = gHarbor != null && lv == 0 ? gHarbor
                        : gGround != null ? (gGround2 != null && (x * 7 + y * 13) % 6 == 0 ? gGround2 : gGround) : null;
                    Sprite ground = c == '#' ? WallTile(layout, x, y, gOuter, gPillar, gCorner, "#") ?? SpriteFactory.WallOf(theme)
                        : c == 'h' && rimWalls ? WallTile(layout, x, y, gOuter, gPillar, gCorner, "#Hh")
                        : c == '~' ? gWater ?? SpriteFactory.WaterOf(theme)
                        : c == ',' ? gGrass ?? SpriteFactory.GrassOf(theme)
                        : c == 'w' ? (gTall != null ? gGround ?? plain ?? SpriteFactory.GrassOf(theme) : desert ? TownArt.DryGrass : TownArt.TallGrass) // 草むらの下は 港でも 地面
                        : deadTree && NextToTallGrass(x, y) ? TownArt.DryGrass // 草むらの枯れ木は 枯れ草むらの中に立つ（町の枯れ木は ふつうの地面）
                        : c == 'F' ? (fountainArt != null ? SpriteFactory.FloorOf(theme) : TownArt.Fountain)
                        : (c == 'H' && gWall != null) || (c == '|' && gLadder != null) ? plain ?? TownArt.HighGround(theme, lv) // かべの絵の下（のこぎり形の すき間）は 上の地面
                        : c == 'H' || c == '|' ? TownArt.Cliff(theme, lv)
                        : c == 'L' ? gStairs ?? TownArt.Stairs(theme)
                        : c == 'l' ? TownArt.StairsSide(theme)
                        : c == '/' ? TownArt.Slope(theme, lv)
                        : c == '=' ? gBridge ?? SpriteFactory.Bridge
                        : c == 'G' ? TownArt.Soil
                        : c >= '1' && c <= '8' && TownData.Current.CanalGates ? gBridge ?? SpriteFactory.Bridge // 運河の門は 橋（がれきは上にのる）
                        : plain != null ? plain
                        : lv > 0 ? TownArt.HighGround(theme, lv) // 高い所ほど 明るい地面
                        : SpriteFactory.FloorOf(theme);
                    Tile(pos, ground, 0);
                    if (c == 'w' && gTall != null) PlaceTall(Tile(pos, gTall, 1), tallKinds, x, y); // 草むら（アルカナが出る）
                    if (c == 'H' && gWall != null) Tile(pos, gWall, 1);  // 石のかべ
                    if (c == '|' && gLadder != null) Tile(pos, gLadder, 1); // はしごの かかった 石のかべ
                    if (!(c == 'h' && rimWalls)) DrawHeightDetail(x, y, c, lv, pos);
                    if (c == 'T') Tile(pos, gTree ?? (deadTree ? TownArt.DeadTree : QuestArt.Decor(3)), 2); // 村の木の絵があれば それ（さばくの村は 枯れ木の絵）
                    if (c == 'Q') Tile(pos, boardArt ?? TownArt.Board, 2);
                    if (c >= '1' && c <= '8') rubble.Add((c - '0', Tile(pos, QuestArt.Obstacle(ObstacleKind.Rock), 1)));
                }
            }

            if (fountainArt != null) PlaceFountain(fountainArt);

            // 港の小舟（横2マス。左のマスの位置から 右へ）
            foreach (var (bx, by) in TownData.Current.Boats)
            {
                var boat = Tile(ToWorld(bx, by) + new Vector2(0.5f, 0f), ArtOverrides.Sized($"Art/Town/{TownData.Current.Name}_小舟", 1.7f, 0.5f) ?? TownArt.Rowboat, 2);
                boat.name = "Boat";
            }

            // 町の宝箱（TownData.TownChests と同じ順に作る）
            foreach (var (cx, cy) in TownData.TownChests) chests.Add(Tile(ToWorld(cx, cy), QuestArt.Goal(GoalKind.Chest, false), 2));

            foreach (var b in TownData.Buildings)
            {
                var go = new GameObject(b.Name);
                go.transform.SetParent(transform, false);
                // 建物の絵のまん中に置く
                go.transform.localPosition = new Vector3(b.X + (b.W - 1) * 0.5f, Height - 1 - (b.Y + (b.H - 1) * 0.5f), 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 3;
                buildingSprites[b.Number] = sr;
            }

            for (int i = 0; i < TownData.Npcs.Length; i++)
            {
                var info = TownData.Npcs[i];
                var go = new GameObject(info.Name);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = ToWorld(info.X, info.Y);
                var sr = go.AddComponent<SpriteRenderer>();
                // 画像ファイル（Resources/Art/Character/〈村〉_名前.png か 名前.png）があれば、その絵を使う
                var art = NpcArt(info.Name);
                sr.sprite = art ?? (info.Role == NpcRole.Training ? TownArt.BalloonStand : CharacterArt.Build(new CharacterLook
                {
                    HairStyle = (i * 5 + 1) % CharacterArt.HairStyleNames.Length, Hair = (i + 2) % 7, Cloth = (i + 3) % 7, Skin = i % 4,
                    Hat = i == 0 ? 1 : (i * 3) % CharacterArt.HatNames.Length, Eyes = i % CharacterArt.EyeNames.Length,
                    Outfit = i == 0 ? 0 : (i * 2) % CharacterArt.OutfitNames.Length, Accessory = i == 0 ? 0 : i % CharacterArt.AccessoryNames.Length,
                }));
                sr.sortingOrder = 9;
                // 町の人は 全員 同じ大きさ（足もとは マスの下のまま、上に のびる）
                go.transform.localScale = new Vector3(NpcScale, NpcScale, 1f);
                go.transform.localPosition += new Vector3(0f, (NpcScale - 1f) * 0.5f, 0f);
                npcs.Add((info, go));
            }
            Refresh();
        }

        /// <summary>直った建物・がれき・町の人を、今の進み具合に合わせる</summary>
        public void Refresh()
        {
            foreach (var b in TownData.Buildings) repaired[b.Number] = IsRepaired(b);
            foreach (var (number, sr) in rubble) sr.enabled = !repaired[number];
            // 建物の絵：画像ファイルがあれば その絵（ルーネ村の宿舎など）
            foreach (var b in TownData.Buildings) buildingSprites[b.Number].sprite = ArtOverrides.Building(b, repaired[b.Number]) ?? TownArt.Building(b, repaired[b.Number]);
            foreach (var (npc, go) in npcs) go.SetActive(npc != hiddenNpc && (npc.AppearAfter == 0 || repaired[npc.AppearAfter]));
            for (int i = 0; i < chests.Count; i++) chests[i].sprite = QuestArt.Goal(GoalKind.Chest, ChestOpened(i));
        }

        // ------------------------------------------------------------------
        // 町の高さ（段々）
        // ------------------------------------------------------------------

        private int[,] levels; // マスごとの高さ（0〜3）

        /// <summary>そのマスの高さ</summary>
        public int LevelAt(Vector2 world)
        {
            var t = ToTile(world);
            return t.x < 0 || t.y < 0 || t.x >= Width || t.y >= Height ? 0 : levels[t.x, t.y];
        }

        /// <summary>階段・坂・はしごの上か（少しゆっくり歩く）</summary>
        public bool IsStairs(Vector2 world)
        {
            char c = CharAt(world);
            return c == 'L' || c == 'l' || c == '/' || c == '|';
        }

        /// <summary>
        /// かべ（#）の絵：横に つながる所は かべ、たてに つながる所は たてのかべ、かど・まじわる所は はしらの頭（絵がなければ null）
        /// </summary>
        public static Sprite WallTile(char[,] g, int x, int y, Sprite wall, Sprite pillar, Sprite corner = null, string family = "#")
        {
            if (wall == null) return null;
            int w = g.GetLength(0), h = g.GetLength(1);
            bool Wall(int tx, int ty) => tx >= 0 && ty >= 0 && tx < w && ty < h && family.IndexOf(g[tx, ty]) >= 0;
            bool across = Wall(x - 1, y) || Wall(x + 1, y), down = Wall(x, y - 1) || Wall(x, y + 1);
            if (across && !down) return wall;
            if (down && !across) return pillar ?? wall;
            return corner ?? pillar ?? wall; // かど・まじわる所・1つだけ
        }

        /// <summary>草むらの絵の しゅるい（〈村〉_草むら・草むら2〜5）。2しゅるい以上あれば しげみの絵</summary>
        public static Sprite[] TallKinds(System.Func<string, Sprite> art)
        {
            var list = new List<Sprite>();
            foreach (var n in new[] { "草むら", "草むら2", "草むら3", "草むら4", "草むら5" }) { var s = art(n); if (s != null) list.Add(s); }
            return list.ToArray();
        }

        /// <summary>
        /// しげみの絵が 何しゅるいか あるとき：マスごとに しゅるい・場所・大きさを 少しずつ変えて、
        /// ます目に きっちり ならばない 自然な しげみにする（同じマスは いつも同じ形）
        /// </summary>
        public static void PlaceTall(SpriteRenderer sr, Sprite[] kinds, int x, int y)
        {
            if (kinds.Length < 2) return; // 1まいだけ（マスいっぱいの 草むらの絵）は そのまま
            int h = (x * 73856093 ^ y * 19349663) & 0x7fffffff;
            sr.sprite = kinds[h % kinds.Length];
            float k = 1.0f + (h >> 18) % 100 / 100f * 0.3f;
            sr.transform.localScale = new Vector3((h >> 9) % 2 == 0 ? k : -k, k, 1f); // ときどき 左右を 反対に
            sr.transform.localPosition += new Vector3(((h >> 4) % 100 / 100f - 0.5f) * 0.3f, ((h >> 11) % 100 / 100f - 0.5f) * 0.2f, 0f);
            sr.sortingOrder = 1 + (h >> 25) % 2;
        }

        private bool ladderArt; // はしごの かべの絵がある（そのときは はしごの小さな絵を かさねない）

        private bool LowerAt(int x, int y, int lv) =>
            x >= 0 && y >= 0 && x < Width && y < Height && layout[x, y] != '#' && levels[x, y] < lv;

        /// <summary>がけのふち（横・上が低い側）・はしご・がけの下のかげ を かさねる</summary>
        private void DrawHeightDetail(int x, int y, char c, int lv, Vector2 pos)
        {
            if (c == 'h' || c == 'H')
            {
                if (LowerAt(x - 1, y, lv)) Tile(pos, TownArt.Rim(0), 1);
                if (LowerAt(x + 1, y, lv)) Tile(pos, TownArt.Rim(1), 1);
                if (c == 'h' && LowerAt(x, y - 1, lv)) Tile(pos, TownArt.Rim(2), 1);
            }
            if (c == '|' && !ladderArt) Tile(pos, TownArt.Ladder, 1);
            // がけの正面（H）の すぐ下のマスには かげ
            if (y > 0 && c != 'H' && c != 'h' && c != '#' && (layout[x, y - 1] == 'H' || layout[x, y - 1] == '|') && levels[x, y - 1] > lv)
            {
                var sr = Tile(pos, TownArt.CliffShadow, 1);
                sr.color = new Color(0f, 0f, 0f, 0.32f);
            }
        }

        /// <summary>依頼板の名前を出す高さ（画像の依頼板は 大きいので 上に）</summary>
        public float BoardLabelUp { get; private set; } = 0.8f;

        /// <summary>噴水の画像を、噴水のマス（F）の まん中に置く（地面の上・町の人や依頼板より下）</summary>
        private void PlaceFountain(Sprite art)
        {
            float sx = 0, sy = 0; int n = 0;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) if (layout[x, y] == 'F') { var p = ToWorld(x, y); sx += p.x; sy += p.y; n++; }
            if (n == 0) return;
            Tile(new Vector2(sx / n, sy / n), art, 1).name = "Fountain";
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

        // ------------------------------------------------------------------
        // 座標（地図の x,y は左上が 0、世界の座標は下が 0）
        // ------------------------------------------------------------------

        public Vector2 ToWorld(int x, int y) => new Vector2(x, Height - 1 - y);

        public Vector2Int ToTile(Vector2 world) => new Vector2Int(Mathf.RoundToInt(world.x), Height - 1 - Mathf.RoundToInt(world.y));

        public char CharAt(Vector2 world)
        {
            var t = ToTile(world);
            if (t.x < 0 || t.y < 0 || t.x >= Width || t.y >= Height) return '#';
            return layout[t.x, t.y];
        }

        public bool IsWalkable(Vector2 world)
        {
            if (!TownData.Walkable(CharAt(world), repaired)) return false;
            var t = ToTile(world);
            foreach (var (npc, go) in npcs)
                if (go.activeSelf && npc.X == t.x && npc.Y == t.y) return false;
            return true;
        }

        public bool IsWalkable(Vector2Int worldTile) => IsWalkable(new Vector2(worldTile.x, worldTile.y));

        /// <summary>アルカナが出る草むらか</summary>
        public bool IsTallGrass(Vector2 world) => CharAt(world) == 'w';

        /// <summary>町のどの区画か（0=南西 1=南東 2=北東 3=北西）。出てくるアルカナが変わる</summary>
        public int AreaOf(Vector2 world)
        {
            var t = ToTile(world);
            return TownData.AreaOf(t.x, t.y);
        }

        /// <summary>その場所にある建物（なければ null）</summary>
        public BuildingInfo BuildingAt(Vector2 world)
        {
            var t = ToTile(world);
            foreach (var b in TownData.Buildings)
                if (t.x >= b.X && t.x < b.X + b.W && t.y >= b.Y && t.y < b.Y + b.H) return b;
            return null;
        }

        public Vector2 DoorWorld(BuildingInfo b) => ToWorld(b.DoorX, b.DoorY);

        /// <summary>依頼板の場所（世界の座標）</summary>
        public IEnumerable<Vector2> BoardWorlds()
        {
            foreach (var (x, y) in TownData.Boards) yield return ToWorld(x, y);
        }

        // ----- 町の宝箱（TownData.TownChests の順。開けたかは Progress.TownChests） -----

        private readonly List<SpriteRenderer> chests = new List<SpriteRenderer>();

        /// <summary>いまいる村の i 番の宝箱を開けたか（記録は 村ごとに4つずつ ずらして ならべる）</summary>
        public static bool ChestOpened(int i) => (Progress.TownChests & (1 << (TownData.Current.ChestBit + i))) != 0;

        public void OpenChest(int i)
        {
            Progress.TownChests |= 1 << (TownData.Current.ChestBit + i);
            Refresh();
        }

        public Vector2 ChestWorld(int i) => ToWorld(TownData.TownChests[i].x, TownData.TownChests[i].y);

        // ------------------------------------------------------------------
        // クエストの目的地（遺跡の入り口の印）
        // ------------------------------------------------------------------

        private SpriteRenderer destination;
        private GameObject destinationArrow;

        /// <summary>今の目的地（なければ null）</summary>
        public SpotInfo Destination { get; private set; }

        public void SetDestination(SpotInfo spot)
        {
            Destination = spot;
            if (destination == null)
            {
                destination = Tile(Vector2.zero, TownArt.RuinGate, 4);
                destination.name = "Destination";
                destinationArrow = Tile(Vector2.zero, TownArt.Marker, 8).gameObject;
            }
            destination.gameObject.SetActive(spot != null);
            destinationArrow.SetActive(spot != null);
            if (spot != null) destination.transform.localPosition = ToWorld(spot.X, spot.Y);
        }

        public Vector2 SpotWorld(SpotInfo spot) => ToWorld(spot.X, spot.Y);

        // ----- ストーリークエストの 調べる場所（キラキラの印） -----

        private SpriteRenderer storyMarker;
        private Vector2Int? storySpot;

        public void SetStoryMarker(Vector2Int? spot)
        {
            storySpot = spot;
            if (storyMarker == null)
            {
                storyMarker = Tile(Vector2.zero, TownArt.Sparkle, 4);
                storyMarker.name = "StoryMarker";
            }
            storyMarker.gameObject.SetActive(spot.HasValue);
            if (spot.HasValue) storyMarker.transform.localPosition = ToWorld(spot.Value.x, spot.Value.y);
        }

        private void Update()
        {
            // 調べる場所のキラキラを、ちかちか光らせる
            if (storySpot.HasValue && storyMarker != null)
                storyMarker.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 3f)));
            // 目的地の上の印を、ふわふわ上下させる
            if (Destination != null && destinationArrow != null)
                destinationArrow.transform.localPosition = ToWorld(Destination.X, Destination.Y) + Vector2.up * (0.9f + 0.15f * Mathf.Sin(Time.time * 4f));
        }

        /// <summary>その場所にいる町の人（いなければ null）</summary>
        private NpcInfo hiddenNpc; // かくれんぼでかくれている人（見えない・通れる）

        /// <summary>町の人をかくす（かくれんぼ）／元にもどす</summary>
        public void SetNpcHidden(NpcInfo n, bool hidden)
        {
            hiddenNpc = hidden ? n : null;
            foreach (var (npc, go) in npcs)
                if (npc == n) go.SetActive(!hidden && (npc.AppearAfter == 0 || repaired[npc.AppearAfter]));
        }

        public NpcInfo NpcAt(Vector2 world)
        {
            var t = ToTile(world);
            foreach (var (npc, go) in npcs)
                if (go.activeSelf && npc.X == t.x && npc.Y == t.y) return npc;
            return null;
        }

        public IEnumerable<NpcInfo> ActiveNpcs()
        {
            foreach (var (npc, go) in npcs) if (go.activeSelf) yield return npc;
        }

        /// <summary>from から to まで、歩けるマスを通る道を探す（タップで移動するとき用）。なければ null</summary>
        public List<Vector2> FindPath(Vector2 from, Vector2 to) => WalkPath.Find(this, from, to);

        public Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x, 0, Width - 1), Mathf.Clamp(p.y, 0, Height - 1));
    }
}
