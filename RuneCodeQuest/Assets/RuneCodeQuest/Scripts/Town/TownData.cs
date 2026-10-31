using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>直すと使えるようになる、建物のはたらき</summary>
    public enum BuildingFunction
    {
        Inn, ItemShop, WeaponShop, ArmorShop, Library, Mill, ClockTower, Castle,
        MaterialTrader, // 資材屋（建物ではなく町の人のお店）
        Landmark,       // 村のシンボル（灯台・大樹など）。調べると、その国の手がかりが分かる
        Shrine,         // 星の神殿（守り神オルディナとの さいごの戦い）
        Ship,           // 港の大きな船（直ると 沖へ出て 大物をつれる）
    }

    /// <summary>町の建物1つぶんのデータ</summary>
    public class BuildingInfo
    {
        public int Village;           // どの村の建物か（0=ルーネ村 … 5=ステラリア）
        public int Number;            // 村の中での番号 1〜（この順番に直していく）
        public string Name;
        public BuildingFunction Function;
        public int X, Y, W, H;        // 村の地図の上での場所（左上、マス）
        public int FirstStage, LastStage;
        public string RepairedText;   // 直ったときに、何ができるようになったか
        public string[] VisitLines;   // 村のシンボル：調べたときの話
        public (byte r, byte g, byte b) Roof;

        public bool DoorAbove;        // 入り口が 上がわ（船は 岸壁がわから乗る）

        public int DoorX => X + W / 2;
        public int DoorY => DoorAbove ? Y - 1 : Y + H; // 建物のすぐ下（入り口の前）。DoorAbove なら すぐ上
        public int StageCount => LastStage - FirstStage + 1;
        /// <summary>すべての村で ちがう番号（絵のキャッシュ・記録に使う）</summary>
        public int Id => Village * 10 + Number;
    }

    /// <summary>クエストの目的地（遺跡の入り口）</summary>
    public class SpotInfo
    {
        public string Name;
        public int X, Y;
        public int Area => TownData.AreaOf(X, Y);
    }

    /// <summary>町の人の役わり（話しかけると何ができるか）</summary>
    public enum NpcRole { Talk, Training, Fishing, MaterialTrader, Mayor, Courier, Gardener, HideSeek, Coach }

    /// <summary>町の人（話しかけられる）</summary>
    public class NpcInfo
    {
        public string Name;
        public int X, Y;
        public int AppearAfter;           // この番号の建物が直ったら出てくる（0 なら最初から）
        public string[] Lines;            // 話すこと
        public (byte r, byte g, byte b) Cloth;
        public NpcRole Role;              // Talk 以外は、話したあとに練習・つり・お店ができる
    }

    /// <summary>
    /// 1つの村（国）のデータ。村はそれぞれ べつのマップで、4つの区画に分かれている。
    /// 区画は、建物を直すと がれきが片づいて 1つずつ行けるようになる。
    /// </summary>
    public class VillageInfo
    {
        public int Index;                 // 0=ルーネ村 1=アクアリス 2=シルフィード 3=サンドリア 4=ギアルド 5=ステラリア
        public string Name;               // 村（国）の名前
        public string Kind;               // 港町・森の村 など
        public int W, H, StartX, StartY;
        public int SplitX, SplitY;        // 区画の分かれ目（x が SplitX より小さいと西、y が SplitY より大きいと南）
        public int TileTheme;             // 地面の絵（SpriteFactory の景色の番号）
        public (byte r, byte g, byte b) WallColor; // 建物のかべの色
        public int[] AreaGate;            // その区画（0=南西 1=南東 2=北東 3=北西）を開く建物の番号（0 なら最初から）
        public string[] DistrictLong;     // 地図に出す区画の名前
        public string[] GrassNames;       // 退治の依頼の草むらの名前（区画ごと）
        public string[] Arrival;          // はじめて着いたときの話
        public (int x, int y)[] Boards, Chests;
        public (int x, int y)[] GardenBeds = new (int, int)[0];
        public (int x, int y)[] HideSpots = new (int, int)[0];
        public (int x, int y)[] Boats = new (int, int)[0];   // 水にうかぶ小舟（かざり。横2マス）
        public char Edge = '#';           // 地図のまわり（ふつうは かべ。海の町は 海 '~'）
        public bool CanalGates;           // 区画の門（がれき）を 橋の上に かく（運河で区切る 海の町）
        public bool Desert;               // さばくの町：草むらは 枯れ草むら（草むらの画像がないとき）
        public (int x, int y)[] DeadTrees = new (int, int)[0]; // 枯れ木の場所（木と同じく通れない。Terrain で 'T' にする）
        public BuildingInfo[] Buildings;
        public NpcInfo[] Npcs;
        public SpotInfo[] QuestSpots;
        public System.Action<char[,], VillageInfo> Terrain; // かべ・草むら・水・木などを かく
        public System.Action<HeightMap> Hills;              // 町の高さ（段々・階段・坂・はしご）

        public int FirstStage => Buildings[0].FirstStage;
        public int LastStage => Buildings[Buildings.Length - 1].LastStage;
        public BuildingInfo LastBuilding => Buildings[Buildings.Length - 1];
        /// <summary>宝箱の記録（Progress.TownChests のビット）の はじまり。村ごとに4つずつ</summary>
        public int ChestBit => Index * 4;
        public bool HasStage(int stage) => stage >= FirstStage && stage <= LastStage;
    }

    /// <summary>
    /// 町の高さ（段々）。マスごとに 高さ（0〜3）を決めて、階段・坂・はしご の場所を置く。
    /// 高さがちがうところの さかいめは、自動で がけになる（TownData.ApplyHeights）：
    ///  ・下（南）が低い → がけの正面（H）　・横や上（北）が低い → がけのふち（h）
    ///  ・さかいめに 階段（L / 横向きは l）・坂（/）・はしご（|）を置くと、そこから のぼりおりできる
    /// </summary>
    public class HeightMap
    {
        public readonly int[,] Level;
        public readonly char[,] Special;

        public HeightMap(int w, int h)
        {
            Level = new int[w, h];
            Special = new char[w, h];
        }

        /// <summary>x0〜x1・y0〜y1 の高さを level にする</summary>
        public void Raise(int x0, int y0, int x1, int y1, int level)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Level[x, y] = level;
        }

        private void Put(int x0, int y0, int x1, int y1, char c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Special[x, y] = c;
        }

        /// <summary>石の階段（広い階段は x0〜x1 を広くする）</summary>
        public void Stairs(int x0, int y0, int x1, int y1) => Put(x0, y0, x1, y1, 'L');
        public void Stairs(int x, int y) => Put(x, y, x, y, 'L');
        /// <summary>坂道（段のない ゆるやかな のぼり）</summary>
        public void Slope(int x0, int y0, int x1, int y1) => Put(x0, y0, x1, y1, '/');
        /// <summary>はしご（がけの正面に かける）</summary>
        public void Ladder(int x, int y) => Put(x, y, x, y, '|');
    }

    /// <summary>
    /// 村のデータ（建物・町の人・地図）。6つの村があり、いまいる村（Current）のデータを返す。
    /// 地図の座標は「左上が (0,0)、右が x、下が y」です（文字の地図と同じ向き）。
    /// 村ごとのデータは VillageRune.cs（ルーネ村）と Villages.cs（ほかの5つの国）にあります。
    /// </summary>
    public static partial class TownData
    {
        public const int VillageCount = 6;

        public static readonly VillageInfo[] Villages = MakeVillages();

        private static VillageInfo[] MakeVillages()
        {
            var list = new[] { BuildRune(), BuildAquaris(), BuildSylphid(), BuildSandria(), BuildGeard(), BuildStellaria() };
            foreach (var v in list)
                foreach (var b in v.Buildings) b.Village = v.Index;
            return list;
        }

        /// <summary>いまいる村の番号</summary>
        public static int Index { get; private set; }

        public static VillageInfo Current => Villages[Index];

        /// <summary>いまいる村を変える（地図を作りなおす前に呼ぶ）</summary>
        public static void SetVillage(int v) => Index = v < 0 ? 0 : v >= VillageCount ? VillageCount - 1 : v;

        // ----- いまいる村のデータ -----
        public static int W => Current.W;
        public static int H => Current.H;
        public static int StartX => Current.StartX;
        public static int StartY => Current.StartY;
        public static (int x, int y)[] Boards => Current.Boards;
        public static (int x, int y)[] TownChests => Current.Chests;
        public static (int x, int y)[] GardenBeds => Current.GardenBeds;
        public static (int x, int y)[] HideSpots => Current.HideSpots;
        public static BuildingInfo[] Buildings => Current.Buildings;
        public static NpcInfo[] Npcs => Current.Npcs;
        public static SpotInfo[] QuestSpots => Current.QuestSpots;

        /// <summary>すべての村の建物（直す順）</summary>
        public static IEnumerable<BuildingInfo> AllBuildings
        {
            get { foreach (var v in Villages) foreach (var b in v.Buildings) yield return b; }
        }

        /// <summary>そのステージを直すクエストがある建物（どの村でも）</summary>
        public static BuildingInfo BuildingOfStage(int stage)
        {
            foreach (var b in AllBuildings) if (stage >= b.FirstStage && stage <= b.LastStage) return b;
            return Villages[VillageCount - 1].LastBuilding;
        }

        /// <summary>そのステージの依頼がある村</summary>
        public static int VillageOfStage(int stage) => BuildingOfStage(stage).Village;

        /// <summary>そのマスの区画（0=南西 1=南東 2=北東 3=北西）</summary>
        public static int AreaOf(int x, int y)
        {
            bool south = y > Current.SplitY, west = x < Current.SplitX;
            return south ? (west ? 0 : 1) : (west ? 3 : 2);
        }

        /// <summary>
        /// いまいる村の地図（文字）を作る。
        ///  # かべ  . 道  , 草（アルカナは出ない）  w 草むら（アルカナが出る）  ~ 水  T 木  F 噴水
        ///  H がけの正面・h がけのふち（通れない）  L 石の階段（l 横向き）  / 坂  | はしご  = 橋（高さは HeightMap）
        ///  1〜8 がれき（その番号の建物が直ると消える）  B 建物  Q 依頼板  C 宝箱  G 畑
        /// </summary>
        public static char[,] BuildLayout() => BuildLayout(Current, out _);

        /// <summary>地図（文字）と、マスごとの高さ（levels）を作る</summary>
        public static char[,] BuildLayout(VillageInfo v, out int[,] levels)
        {
            var g = new char[v.W, v.H];
            for (int y = 0; y < v.H; y++)
                for (int x = 0; x < v.W; x++)
                    g[x, y] = x == 0 || y == 0 || x == v.W - 1 || y == v.H - 1 ? v.Edge : '.';

            v.Terrain(g, v);
            var hm = new HeightMap(v.W, v.H);
            v.Hills?.Invoke(hm);
            ApplyHeights(g, hm);
            levels = hm.Level;

            foreach (var (x, y) in v.GardenBeds) g[x, y] = 'G';
            foreach (var (bx, by) in v.Boards) g[bx, by] = 'Q';
            foreach (var (cx, cy) in v.Chests) g[cx, cy] = 'C';

            // クエストの目的地（遺跡の入り口）のマスは道にする
            foreach (var sp in v.QuestSpots) g[sp.X, sp.Y] = '.';

            // 建物（入り口の前は道にする）
            foreach (var b in v.Buildings)
            {
                for (int y = b.Y; y < b.Y + b.H; y++)
                    for (int x = b.X; x < b.X + b.W; x++)
                        g[x, y] = 'B';
                g[b.DoorX, b.DoorY] = '.';
            }
            return g;
        }

        /// <summary>
        /// 高さのさかいめを がけにする（階段・坂・はしごの場所は のぼりおりできる）。
        /// かべ（#）・がれき（1〜8）・水（~）とのさかいめは がけにしない（区画の門は そのまま通れる。水は がけにならない）
        /// </summary>
        public static void ApplyHeights(char[,] g, HeightMap hm)
        {
            int w = g.GetLength(0), h = g.GetLength(1);
            // 地図のまわり・かべ・がれき・水 は がけにしない（水べは 岸として そのまま）
            bool Open(int x, int y) => x > 0 && y > 0 && x < w - 1 && y < h - 1 && g[x, y] != '#' && g[x, y] != '~' && !(g[x, y] >= '1' && g[x, y] <= '8');
            var result = (char[,])g.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!Open(x, y)) continue;
                    int lv = hm.Level[x, y];
                    bool Lower(int nx, int ny) => Open(nx, ny) && hm.Level[nx, ny] < lv;
                    bool s = Lower(x, y + 1), n = Lower(x, y - 1), e = Lower(x + 1, y), wl = Lower(x - 1, y);
                    if (!(s || n || e || wl)) continue;
                    char sp = hm.Special[x, y];
                    if (sp == 'L') result[x, y] = s || n ? 'L' : 'l';
                    else if (sp == '/' || sp == '|') result[x, y] = sp;
                    else result[x, y] = s ? 'H' : 'h';
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    g[x, y] = result[x, y];
        }

        /// <summary>そのマスを通れるか（repaired[k] は k 番の建物が直っているか）</summary>
        public static bool Walkable(char c, bool[] repaired)
        {
            switch (c)
            {
                case '#': case '~': case 'T': case 'F': case 'B': case 'Q': case 'C': case 'G': case 'H': case 'h': return false;
                case '.': case ',': case 'w': case '^': case 'L': case 'l': case '/': case '|': case '=': return true;
                default:
                    if (c >= '1' && c <= '8') return c - '0' < repaired.Length && repaired[c - '0'];
                    return true;
            }
        }
    }
}
