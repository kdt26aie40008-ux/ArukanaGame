using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    public enum GameState
    {
        Title,           // タイトル画面
        Town,            // 町を歩く
        Quest,           // クエスト画面（プログラミング）
        Battle,          // タイピングバトル
        Shop,            // お店・図書館
        StageSelect,     // ステージ一覧（前のクエストにもう一度挑戦）
        Ending,          // ラスボスをたおした
        CharacterCreate, // キャラクリ
        RequestBoard,    // 依頼板
        Area,            // 遺跡のエリア（依頼で行く場所）を歩く
        Loading,         // 場所を移動するときの読みこみ画面
        Balloon,         // 風船割り（タイピングの練習）
        Travel,          // 馬車の旅（ほかの国の村へ行く）
    }

    /// <summary>
    /// ゲーム全体の司令塔。
    ///
    /// ・町（TownMap）を作り、プレイヤーを歩かせる。カメラはプレイヤーについていく
    /// ・依頼板で依頼を受ける → クエスト（プログラミング）→ クリアすると資材がもらえる
    /// ・資材がそろったら建物の工事の看板から仕上げクエスト → 建物が直る
    /// ・直った建物は、宿舎（回復）・お店・図書館などとして使える
    /// ・草むらを歩くと、アルカナが出てタイピングバトル
    /// ・今どの画面か（GameState）を管理して、画面を切り替える
    /// </summary>
    public partial class GameManager : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Title;

        private GameObject worldRoot;
        private TownMap town;
        private PlayerController player;

        private QuestScreen questScreen;
        private readonly BattleScreen battle = new BattleScreen();
        private readonly ShopScreen shop = new ShopScreen();
        private readonly DialogBox dialog = new DialogBox();
        private readonly RequestBoard board = new RequestBoard();
        private readonly BalloonScreen balloon = new BalloonScreen();

        // 今遊んでいるクエストを、どの依頼として受けたか（資材のほうびに使う）
        private int questStage;
        private RequestKind questKind;
        private bool questNew;        // 受けたとき、まだ遊んだことのないステージだったか
        private bool questRewarded;   // ほうびをもうもらったか

        private int grassSteps;       // 前のバトルから、草むらを何歩あるいたか
        private int battleArea = -1;  // 今のバトルの敵が出た区画

        private string toast;
        private float toastTimer;
        private int prologuePage;
        private Vector2Int lastTile;

        private enum TitlePhase { Start, Menu, SlotLoad, SlotNew, SlotDetail, SlotName, ConfirmOverwrite, ConfirmDelete, Prologue }
        private TitlePhase titlePhase = TitlePhase.Start; // 起動したら、まずスタート画面
        private string titleNote;

        // お話の番号（Progress.StorySeen）
        private const int StoryIntro = 0, StoryFirstBattle = 1, StoryBossBeaten = 2, StoryRepairedBase = 10;

        /// <summary>草むらで1歩あるいたとき、アルカナが出る確率</summary>
        private const float EncounterRate = 0.1f;

        private void Start()
        {
            Furigana.Enabled = Progress.Furigana;
            CreateQuestScreen();
            battle.OnFinished = OnBattleFinished;
            shop.OnClose = () => { State = GameState.Town; KeyInput.Clear(); };
            board.OnClose = () => { State = GameState.Town; KeyInput.Clear(); };
            board.OnStartQuest = AcceptQuest;
            balloon.OnClose = () => { State = GameState.Town; KeyInput.Clear(); };
            board.OnCancelQuest = RefreshDestination;
            BuildTown();
        }

        private void CreateQuestScreen()
        {
            questScreen = new QuestScreen
            {
                OnCleared = OnQuestCleared,
                OnProceed = BackToTown,
                OnClose = BackToTown,
            };
        }

        // ------------------------------------------------------------------
        // 町を作る
        // ------------------------------------------------------------------

        private void BuildTown()
        {
            // いまいる村（ルーネ村・アクアリス…）の地図を作る。アルカナも その国のものが出る
            TownData.SetVillage(TownMap.VillageOpen(Progress.Village) ? Progress.Village : 0);
            BattleData.CurrentVillage = TownData.Index;
            townPin = null;
            if (worldRoot != null) Destroy(worldRoot);
            worldRoot = new GameObject("Town");

            town = new GameObject("TownMap").AddComponent<TownMap>();
            town.transform.SetParent(worldRoot.transform, false);
            town.Build();

            player = new GameObject("Player").AddComponent<PlayerController>();
            player.transform.SetParent(worldRoot.transform, false);
            player.transform.position = town.ToWorld(TownData.StartX, TownData.StartY);
            player.Init(town);
            lastTile = town.ToTile(player.transform.position);

            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.6f;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.09f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            FollowCamera(true);
            RefreshDestination();
            if (TownData.Index == 0) CreateCat(); else cat = null; // ねこは ルーネ村にだけいる
            CreateGarden();
        }

        /// <summary>カメラをプレイヤーに合わせる（町のはしでは止まる）</summary>
        private void FollowCamera(bool snap)
        {
            var cam = Camera.main;
            if (cam == null || player == null) return;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector2 p = player.transform.position;
            // 今いるマップ（町 または 遺跡のエリア）のはしで止まる
            Vector2 o = area != null ? area.Origin : Vector2.zero;
            int w = area != null ? area.Width : town.Width, h = area != null ? area.Height : town.Height;
            // マップが画面より小さい向き（宿舎の中など）は、マップのまん中で固定して動かさない
            float x = w <= halfW * 2 ? o.x + (w - 1) * 0.5f : Mathf.Clamp(p.x, o.x + halfW - 0.5f, o.x + w - 0.5f - halfW);
            float y = h <= halfH * 2 ? o.y + (h - 1) * 0.5f : Mathf.Clamp(p.y, o.y + halfH - 0.5f, o.y + h - 0.5f - halfH);
            var target = new Vector3(x, y, -10f);
            cam.transform.position = snap ? target : Vector3.Lerp(cam.transform.position, target, 0.2f);
        }

        // ------------------------------------------------------------------
        // 毎フレームの処理
        // ------------------------------------------------------------------

        private void Update()
        {
            player.CanMove = (State == GameState.Town || State == GameState.Area) && !showInventory && !showMap && !showDex && !showMenu && !QuizOpen && !dialog.Active;
            if (!player.CanMove) player.StopWalking();
            if (toastTimer > 0f && !dialog.Active) toastTimer -= Time.deltaTime; // 会話中は、お知らせを待たせておく
            if (State != GameState.Town && State != GameState.Area) { showMap = false; showDex = false; showMenu = false; }
            UpdateAutoSave();
            UpdatePin();
            UpdateBgm();
            if (area == null) { UpdateCat(); UpdateGarden(); } // ねこは町にいるあいだ、うろうろ歩く
            UpdateHideSeek();

            switch (State)
            {
                case GameState.Town:
                    if (dialog.Active) break;
                    if (UpdateMenuKeys()) break;
                    if (UpdateDexKeys()) break;
                    if (UpdateMapKeys()) break;
                    if (KeyInput.WasPressed(KeyCode.I)) showInventory = !showInventory;
                    if (showInventory && KeyInput.WasPressed(KeyCode.Escape)) BagBack();
                    if (!showInventory && KeyInput.WasPressed(KeyCode.L)) { State = GameState.StageSelect; break; }
                    if (!showInventory) UpdateTown();
                    break;
                case GameState.Area:
                    if (dialog.Active || QuizOpen) break;
                    if (UpdateMenuKeys()) break;
                    if (UpdateDexKeys()) break;
                    if (UpdateMapKeys()) break;
                    if (KeyInput.WasPressed(KeyCode.I)) showInventory = !showInventory;
                    if (showInventory && KeyInput.WasPressed(KeyCode.Escape)) BagBack();
                    if (!showInventory) UpdateArea();
                    break;
                case GameState.Loading:
                    UpdateLoading();
                    break;
                case GameState.Balloon:
                    balloon.Update();
                    break;
                case GameState.Quest:
                    questScreen.Update();
                    break;
                case GameState.Battle:
                    battle.Update();
                    break;
                case GameState.StageSelect:
                    if (KeyInput.WasPressed(KeyCode.Escape)) State = GameState.Town;
                    break;
            }
        }

        private void LateUpdate()
        {
            if (State == GameState.Town || State == GameState.Area) FollowCamera(false);
            KeyInput.EndFrame();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) KeyInput.Clear();
        }

        private void UpdateTown()
        {
            Vector2 p = player.transform.position;

            // 調べる（建物の入り口の前・依頼板・町の人のとなり）
            if (KeyInput.InteractPressed())
            {
                var b = NearDoor(p);
                if (b != null) { InteractBuilding(b); return; }
                if (InteractTownObject(p)) return;
                if (NearDestination(p)) { EnterDestination(); return; }
                var npc = NearNpc(p);
                if (npc != null) { TalkTo(npc); return; }
            }

            // 1マス動いたとき
            var tile = town.ToTile(p);
            if (tile == lastTile) return;
            lastTile = tile;
            OnStep(town.IsTallGrass(p)); // 歩く音（草むらはガサッ）
            CheckHideFound(p);
            if (Progress.RepelSteps > 0) Progress.RepelSteps--;

            if (town.IsTallGrass(p))
            {
                // 退治の依頼の草むらでは、むしよけの鈴があっても出て、4歩までに必ず出る
                int area = town.AreaOf(p);
                bool hunt = Progress.HuntArea == area;
                grassSteps++;
                // 今いる村（町＝ルーネ村）のアルカナだけ。ストーリーが進むと、ときどき ほかの国の凶暴な亜種
                int region = BattleData.CurrentVillage;
                EnemyInfo foe = hunt
                    ? (Random.value < 0.3f || grassSteps >= 4 ? BattleData.Encounter(region) : null)
                    : (Progress.RepelSteps <= 0 ? BattleData.RollEncounter(region, EncounterRate, false, StoryChapter) : null);
                if (foe != null)
                {
                    battleArea = area;
                    StartBattle(BattleData.Scaled(foe, PlayerStats.Rank));
                }
            }
            // 体力は自然には回復しない（宿舎で休むか、薬草を使う）
        }

        private bool NearBoard(Vector2 p)
        {
            foreach (var w in town.BoardWorlds()) if (Vector2.Distance(p, w) < 1.3f) return true;
            return false;
        }

        /// <summary>近くのまだ開けていない宝箱の番号（なければ -1）</summary>
        private int NearTownChest(Vector2 p)
        {
            for (int i = 0; i < TownData.TownChests.Length; i++)
                if (!TownMap.ChestOpened(i) && Vector2.Distance(p, town.ChestWorld(i)) < 1.3f) return i;
            return -1;
        }

        /// <summary>近くの依頼板・宝箱を調べる。調べたら true</summary>
        private bool InteractTownObject(Vector2 p)
        {
            if (TryInvestigate(p)) return true; // ストーリークエストの 調べる場所
            if (NearBoard(p)) { OpenBoard(); return true; }
            int chest = NearTownChest(p);
            if (chest < 0)
            {
                if (NearCat(p)) { PetCat(); return true; }
                int bed = NearGardenBed(p);
                if (bed >= 0) { TendGarden(bed); return true; }
                if (NearFountain(p)) { Wish(); return true; }
                return false;
            }
            KeyInput.Clear();
            town.OpenChest(chest);
            MarkMission(Missions.OpenedChest);
            // 区画が奥ほど、中身がいい
            int coins = 30 + 25 * chest + 40 * TownData.Index;
            Progress.AddItem(ItemKind.Coin, coins);
            Progress.AddItem(ItemKind.Herb, 2);
            dialog.Show("宝箱", $"町の宝箱を見つけた！ 金貨 {coins}枚 と 薬草 2こ を手に入れた！（町の宝箱は {TownData.TownChests.Length}こ あるよ）");
            return true;
        }

        private bool NearDestination(Vector2 p) =>
            town.Destination != null && Vector2.Distance(p, town.SpotWorld(town.Destination)) < 1.1f;

        // ------------------------------------------------------------------
        // 依頼を受ける → 目的地（遺跡の入り口）まで歩く → クエスト
        // ------------------------------------------------------------------

        /// <summary>依頼板でクエストの依頼を引き受けた。目的地に印を出す</summary>
        private void AcceptQuest(int stage, RequestKind kind)
        {
            var spot = Rebuild.DestinationOf(stage, n => town.Repaired(TownData.Buildings[n - 1]));
            Progress.ActiveQuest = stage;
            Progress.ActiveQuestKind = kind;
            Progress.ActiveSpot = System.Array.IndexOf(TownData.QuestSpots, spot);
            MarkMission(Missions.Accepted);
            State = GameState.Town;
            KeyInput.Clear();
            RefreshDestination();
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            dialog.Show("依頼板", new[]
            {
                $"依頼「{th.Title}」を引き受けた！",
                $"「{th.Area}」への入り口は「{spot.Name}」にある。黄色い矢印の印を目指して歩こう。" +
                "草むらを通るときは、アルカナに気をつけて！",
            });
        }

        /// <summary>
        /// ステージ一覧から もう一度挑戦する：はじめてのときと同じように、今いる村の目的地（遺跡の入り口）へ行くと始まる。
        /// 依頼板の依頼を引き受けているときは、そちらが先
        /// </summary>
        private void AcceptReplay(int stage)
        {
            if (Progress.ActiveQuest > 0 && Progress.ActiveQuestKind != RequestKind.Replay)
            {
                State = GameState.Town;
                KeyInput.Clear();
                dialog.Show("依頼板", "いま 依頼板の依頼を引き受けている。先にそちらを終わらせてから、もう一度挑戦しよう。");
                return;
            }
            var spot = Rebuild.DestinationOf(stage, n => town.Repaired(TownData.Buildings[n - 1]));
            Progress.ActiveQuest = stage;
            Progress.ActiveQuestKind = RequestKind.Replay;
            Progress.ActiveSpot = System.Array.IndexOf(TownData.QuestSpots, spot);
            State = GameState.Town;
            KeyInput.Clear();
            RefreshDestination();
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            dialog.Show("ステージ一覧", new[]
            {
                $"ステージ{stage}「{th.Title}」に もう一度挑戦する！",
                $"「{th.Area}」への入り口は「{spot.Name}」にある。！の印を目指して歩こう。" +
                "草むらを通るときは、アルカナに気をつけて！",
            });
        }

        /// <summary>目的地の印を、今受けている依頼に合わせる（終わった依頼は消す）</summary>
        private void RefreshDestination()
        {
            int q = Progress.ActiveQuest;
            bool replay = Progress.ActiveQuestKind == RequestKind.Replay;
            if (q > 0 && !replay && Progress.StatusOf(q) != Progress.NotPlayed)
            {
                Progress.ActiveQuest = 0;
                q = 0;
            }
            int spot = Mathf.Clamp(Progress.ActiveSpot, 0, TownData.QuestSpots.Length - 1);
            // 目的地は、その依頼の村にいるときだけ出す（もう一度挑戦は、受けた村の目的地）
            town.SetDestination(q > 0 && (replay || TownData.Current.HasStage(q)) ? TownData.QuestSpots[spot] : null);
            RefreshStoryMarker();
        }

        /// <summary>目的地に着いた：クエストを始める</summary>
        private void EnterDestination()
        {
            if (Progress.ActiveQuest <= 0) return;
            int stage = Progress.ActiveQuest;
            var kind = Progress.ActiveQuestKind;
            var th = QuestTheme.Of(LevelFactory.TemplateOf(stage));
            // お話は はじめて行くときだけ読みこみ画面にも出す
            StartLoading($"「{th.Area}」へ向かっている…", AreaMap.IntroSeenFor(stage) ? null : th.Story, () => EnterArea(stage, kind));
        }

        private void OpenBoard()
        {
            KeyInput.Clear();
            board.Open();
            State = GameState.RequestBoard;
            MarkMission(Missions.BoardSeen);
        }

        private BuildingInfo NearDoor(Vector2 p)
        {
            foreach (var b in TownData.Buildings)
                if (Vector2.Distance(p, town.DoorWorld(b)) < 1.1f) return b;
            return null;
        }

        private NpcInfo NearNpc(Vector2 p)
        {
            foreach (var n in town.ActiveNpcs())
                if (Vector2.Distance(p, town.ToWorld(n.X, n.Y)) < 1.3f) return n;
            return null;
        }

        // ------------------------------------------------------------------
        // 建物・町の人
        // ------------------------------------------------------------------

        private void InteractBuilding(BuildingInfo b)
        {
            KeyInput.Clear();
            if (!town.Repaired(b))
            {
                if (!town.CanWorkOn(b))
                {
                    var prev = TownData.Buildings[b.Number - 2];
                    dialog.Show(b.Name + "（工事中）", $"まだ工事できない。先に「{prev.Name}」を直そう。");
                    return;
                }
                string sign = b.Name + "（工事の看板）";
                if (!TownMap.StoryGateOpen(b))
                {
                    var sq = StoryQuests.GateOf(b);
                    dialog.Show(sign, $"まだ工事の準備が できていない。先に ストーリークエスト「{sq.Title}」を進めよう（{sq.Goal}）。");
                    return;
                }
                int left = Rebuild.MainStages(b).Count - TownMap.MainDone(b);
                if (left > 0)
                {
                    dialog.Show(sign, new[]
                    {
                        $"「{b.Name}」を直すには、資材：{NeedsText(b)} が必要だ。",
                        $"噴水のそばの依頼板で「町づくりの依頼」を受けて、資材を集めよう（{b.Name}の依頼は あと{left}つ）。" +
                        "アルカナのかけらは、草むらのアルカナをたおすと手に入る。",
                    });
                    return;
                }
                if (!Rebuild.HasEnough(b, Progress.MaterialCount))
                {
                    dialog.Show(sign, new[]
                    {
                        $"{b.Name}の依頼は全部終わった！ でも、資材が まだ足りない：{MissingText(b)}",
                        "木材・石材・歯車は、依頼板の「おまけの依頼」や時計台でのやり直し、バトルの落とし物でも手に入る。" +
                        "アルカナのかけらは、アルカナをたおすか「退治の依頼」で集めよう。",
                    });
                    return;
                }
                dialog.Show(sign, new[]
                {
                    $"資材がそろった！ いよいよ「{b.Name}」の仕上げだ。",
                    "ゴーレムに命令して、仕上げクエストをクリアしよう！ 終わったら、集めた資材を使って建物が完成する。",
                }, () => OpenQuest(b.LastStage, RequestKind.Finish));
                return;
            }

            switch (b.Function)
            {
                case BuildingFunction.Inn:
                    // 宿舎の中へ（休む・セーブ・身じたく）
                    StartLoading("宿舎に入る…", null, EnterInn);
                    break;
                case BuildingFunction.ItemShop:
                case BuildingFunction.WeaponShop:
                case BuildingFunction.ArmorShop:
                case BuildingFunction.Library:
                    shop.Open(b.Function);
                    State = GameState.Shop;
                    break;
                case BuildingFunction.Mill:
                    int gain = (Progress.ClearedCount - Progress.MillCounter) * 5;
                    if (gain > 0)
                    {
                        Progress.AddItem(ItemKind.Coin, gain);
                        Progress.MillCounter = Progress.ClearedCount;
                        dialog.Show("水車小屋", $"水車がくるくる回って、金貨を {gain} 枚くれた！（クエストをクリアするほど たまるよ）");
                    }
                    else dialog.Show("水車小屋", "水車が回っている。クエストをクリアすると、また金貨がたまるよ。");
                    break;
                case BuildingFunction.ClockTower:
                    State = GameState.StageSelect;
                    break;
                case BuildingFunction.Castle:
                    if (Progress.StorySeen(StoryBossBeaten))
                        dialog.Show("ルーネ村の王さま", "世界は平和になった。人とアルカナが いっしょに くらせる世界じゃ。{name}、ありがとう！");
                    else
                        dialog.Show("ルーネ村の王さま", new[]
                        {
                            "{name}、村を直してくれて ありがとう。じつは、話しておきたいことがある。",
                            "この村をおそったアルカナは、もともと となりの国アクアリスの 海に住むアルカナじゃった。",
                            "いま、5つの国は アルカナを兵器にして 戦争をしておる。そのせいで、世界じゅうで アルカナが あばれだしたのじゃ。",
                            "村の馬車で、アクアリスへ行ってみてくれ。きっと 手がかりがあるはずじゃ。",
                        });
                    break;
                case BuildingFunction.Landmark:
                    dialog.Show(b.Name, b.VisitLines ?? new[] { b.RepairedText });
                    break;
                case BuildingFunction.Ship:
                    dialog.Show("港の船長", new[] { "船の じゅんびは できてるぞ！ 沖へ出て、大物を つり上げよう！", "（沖のさかなは、港の 2倍の金貨と交換してもらえる）" }, () =>
                    {
                        battleArea = -1;
                        var fish = BattleData.Fish(fishRng, PlayerStats.Rank);
                        fish.Name = "沖の" + fish.Name;
                        fish.Coins *= 2;
                        StartBattle(fish);
                    });
                    break;
                case BuildingFunction.Shrine:
                    if (Progress.StorySeen(StoryBossBeaten))
                        dialog.Show("星の神殿", "神殿は しずかだ。オルディナの やさしい光が、世界を見守っている。");
                    else
                        dialog.Show("オルディナ", new[]
                        {
                            "…よくぞ ここまで来た、人の子 {name}よ。わたしは この世界の守り神、オルディナ。",
                            "人は アルカナを苦しめ、戦争の道具にした。だから わたしは、世界を はじめから やりなおすのだ。",
                            "おまえが 本当に 人とアルカナが いっしょに生きられると言うのなら…その力を 見せてみよ！",
                        }, () => { battleArea = -1; StartBattle(BattleData.Boss); });
                    break;
            }
        }

        private void TalkTo(NpcInfo npc)
        {
            KeyInput.Clear();
            if (TryDeliver(npc)) return; // 配達の荷物の届け先なら、まず荷物をわたす
            if (TryStoryTalk(npc)) return; // ストーリークエストで 話を聞く相手なら、その話
            var lines = new List<string>(npc.Lines);
            if (npc == TownData.Npcs[0]) lines.Add(ObjectiveText()); // 師匠は、次にやることを教えてくれる
            switch (npc.Role)
            {
                case NpcRole.Training:
                    if (Progress.BestBalloons > 0) lines.Add($"これまでの最高は {Progress.BestBalloons}こ。合計 {Progress.TotalBalloons}こ 割ったね！");
                    dialog.Show(npc.Name, lines.ToArray(), OpenBalloon);
                    break;
                case NpcRole.Fishing:
                    if (Progress.FishCaught > 0) lines.Add($"これまでに {Progress.FishCaught}ひき つったね。金のさかなは めったにつれないよ！");
                    dialog.Show(npc.Name, lines.ToArray(), () => { battleArea = -1; StartBattle(BattleData.Fish(fishRng, PlayerStats.Rank)); });
                    break;
                case NpcRole.MaterialTrader:
                    dialog.Show(npc.Name, lines.ToArray(), () => { shop.Open(BuildingFunction.MaterialTrader); State = GameState.Shop; });
                    break;
                case NpcRole.Mayor:
                    TalkToMayor(npc, lines);
                    break;
                case NpcRole.Courier:
                    TalkToCourier(npc, lines);
                    break;
                case NpcRole.Gardener:
                    TalkToGardener(npc, lines);
                    break;
                case NpcRole.HideSeek:
                    TalkToHideSeeker(npc, lines);
                    break;
                case NpcRole.Coach:
                    lines.Add("どこへ行く？");
                    dialog.Show(npc.Name, lines.ToArray(), OpenTravel);
                    break;
                default:
                    // 師匠と話したあとは、まだ見ていないストーリークエストの始まりの話
                    dialog.Show(npc.Name, lines.ToArray(), npc == TownData.Npcs[0] ? () => AnnounceStory() : (System.Action)null);
                    break;
            }
        }

        private readonly System.Random fishRng = new System.Random();

        /// <summary>今の画面に合わせて BGM を変える（バトル・風船割りは、それぞれの画面が鳴らす）</summary>
        private void UpdateBgm()
        {
            switch (State)
            {
                case GameState.Title:
                case GameState.CharacterCreate:
                case GameState.Ending:
                    SoundKit.TitleBgm();
                    break;
                case GameState.Town:
                case GameState.Shop:
                case GameState.RequestBoard:
                case GameState.StageSelect:
                case GameState.Travel:
                    SoundKit.TownBgm();
                    break;
                case GameState.Area:
                    if (area != null && area.IsInn) SoundKit.InnBgm(); else SoundKit.AreaBgm();
                    break;
                case GameState.Quest:
                    SoundKit.QuestBgm();
                    break;
                case GameState.Loading:
                    SoundKit.StopBgm();
                    break;
            }
        }

        private void OpenBalloon()
        {
            player.StopWalking();
            KeyInput.Clear();
            balloon.Start();
            State = GameState.Balloon;
        }

        /// <summary>町の人の近くに出る案内（役わりがある人は、何ができるか）</summary>
        private static string NpcPrompt(NpcInfo n) =>
            n.Role == NpcRole.Training ? "風船割りで遊ぶ（タイピングの練習）"
            : n.Role == NpcRole.Fishing ? "つり人と話す（つりをする）"
            : n.Role == NpcRole.MaterialTrader ? "資材屋で買い物"
            : n.Role == NpcRole.Mayor ? "町長と話す（見習いの手帳）"
            : n.Role == NpcRole.Courier ? "配達屋と話す（荷物を届ける）"
            : n.Role == NpcRole.Gardener ? "畑のおばあさんと話す"
            : n.Role == NpcRole.HideSeek ? "宝さがしの子と話す（かくれんぼ）"
            : n.Role == NpcRole.Coach ? "馬車に乗る（ほかの国の村へ行く）"
            : $"{n.Name}と話す";

        /// <summary>次にやること（師匠が教えてくれる・左上にも出る）</summary>
        private string ObjectiveText()
        {
            if (town.Destination != null)
                return $"引き受けた依頼の目的地は「{town.Destination.Name}」じゃ。黄色い矢印の印を目指すんじゃ。";
            string story = StoryObjective(false);
            if (story != null) return story;
            var b = CurrentBuilding();
            if (b == null)
                return VillageDoneText();
            int left = Rebuild.MainStages(b).Count - TownMap.MainDone(b);
            if (left > 0)
                return $"次は「{b.Name}」を直そう。依頼板で依頼を受けて、資材を集めるんじゃ（{b.Name}の依頼は あと{left}つ）。";
            if (!Rebuild.HasEnough(b, Progress.MaterialCount))
                return $"「{b.Name}」の資材が あと少し足りない：{MissingText(b)}";
            return $"資材がそろった！「{b.Name}」の工事の看板を調べて、仕上げクエストに挑戦じゃ！";
        }

        /// <summary>いま直している建物（全部直っていれば null）</summary>
        private BuildingInfo CurrentBuilding()
        {
            foreach (var b in TownData.Buildings)
                if (!town.Repaired(b)) return b;
            return null;
        }

        /// <summary>必要な資材の文章（例：木材6・石材3・アルカナのかけら2）</summary>
        private static string NeedsText(BuildingInfo b)
        {
            var need = Rebuild.Needs(b);
            var parts = new List<string>();
            for (int i = 0; i < need.Length; i++)
                if (need[i] > 0) parts.Add($"{Rebuild.MaterialNames[i]}{need[i]}");
            return string.Join("・", parts);
        }

        /// <summary>足りない資材の文章（例：木材あと2・アルカナのかけらあと1）</summary>
        private static string MissingText(BuildingInfo b)
        {
            var need = Rebuild.Needs(b);
            var parts = new List<string>();
            for (int i = 0; i < need.Length; i++)
            {
                int lack = need[i] - Progress.MaterialCount((Material)i);
                if (lack > 0) parts.Add($"{Rebuild.MaterialNames[i]}あと{lack}");
            }
            return string.Join("・", parts);
        }

        // ------------------------------------------------------------------
        // クエスト
        // ------------------------------------------------------------------

        /// <summary>依頼を受けてクエストを始める</summary>
        private void OpenQuest(int stage, RequestKind kind)
        {
            KeyInput.Clear();
            questStage = stage;
            questKind = kind;
            questNew = Progress.StatusOf(stage) == Progress.NotPlayed;
            questRewarded = false;
            State = GameState.Quest;
            questScreen.Open(stage);
        }

        /// <summary>クエストをクリアしたとき。ほうびの文章を返す（クエストの結果に出る）</summary>
        private string OnQuestCleared(int stage, int stars)
        {
            if (stage != questStage || questRewarded) return null;
            questRewarded = true;
            return GiveQuestReward(stars, true);
        }

        /// <summary>
        /// 依頼のほうび。はじめてのステージ：クリアで（2〜3）+星の数、通過（挑戦回数を使い切る）で1こ。
        /// やり直し：クリアで1こ。仕上げクエスト：資材を使って建物が完成する
        /// </summary>
        private string GiveQuestReward(int stars, bool cleared)
        {
            if (questKind == RequestKind.Finish && questNew)
            {
                var b = TownData.BuildingOfStage(questStage);
                var need = Rebuild.Needs(b);
                for (int i = 0; i < need.Length; i++) Progress.AddMaterial((Material)i, -need[i]);
                return $"資材を使って「{b.Name}」の工事が完成した！ 町にもどって見てみよう。";
            }
            int count = !questNew ? (cleared ? 1 : 0)
                : cleared ? Rebuild.ClearReward(questKind == RequestKind.Extra ? RequestKind.Extra : RequestKind.Main, stars) : 1;
            if (count <= 0) return null;
            var m = Rebuild.MaterialOf(questStage);
            Progress.AddMaterial(m, count);
            return $"{Rebuild.MaterialNames[(int)m]}を {count}こ 手に入れた！（町の再建に使おう）";
        }

        /// <summary>クエストからもどる。通過したときのほうび、直った建物があればお祝いの会話</summary>
        private void BackToTown()
        {
            State = GameState.Town;
            KeyInput.Clear();
            string passReward = null;
            // もう一度挑戦が終わった：目的地の印を消す
            if (questKind == RequestKind.Replay && Progress.ActiveQuestKind == RequestKind.Replay && Progress.ActiveQuest == questStage)
                Progress.ActiveQuest = 0;
            if (questStage > 0 && !questRewarded && questNew && Progress.StatusOf(questStage) != Progress.NotPlayed)
            {
                // クリアできなかったけれど、解説を見て通過した
                questRewarded = true;
                passReward = GiveQuestReward(0, false);
            }
            town.Refresh();
            RefreshDestination();
            if (area != null)
            {
                // 遺跡のエリアにもどる。はじめて終えたときは、ゴーレムが橋を直してくれる
                State = GameState.Area;
                area.Refresh();
                bool justDone = questStage == area.Stage && questNew && Progress.StatusOf(area.Stage) != Progress.NotPlayed;
                if (justDone)
                    dialog.Show("ゴーレム", new[]
                    {
                        (passReward != null ? passReward + "\n" : "") + "ゴーゴー！（クエストを終えたゴーレムが、こわれた橋を直してくれた！）",
                        "向こう岸に行けるようになった。大きな宝箱をさがしてみよう！ 帰るときは、下の出口から町へもどれるよ。",
                    });
                else if (passReward != null) ShowToast(passReward, 5f);
                return;
            }
            foreach (var b in TownData.Buildings)
            {
                if (!town.Repaired(b) || RepairNoteSeen(b)) continue;
                MarkRepairNote(b);
                var note = new List<string> { $"やったな {{name}}！「{b.Name}」が直ったぞ！", b.RepairedText };
                if (b.VisitLines != null) note.AddRange(b.VisitLines); // 村のシンボル：手がかりの話
                note.Add(ObjectiveText());
                dialog.Show("師匠", note.ToArray(), () => AnnounceStory()); // 建物が直って始まるストーリークエスト
                return;
            }
            if (AnnounceStory()) { if (passReward != null) ShowToast(passReward, 5f); return; }
            ShowToast(passReward != null ? passReward + "\n" + ObjectiveText() : ObjectiveText(), 6f);
        }

        // ------------------------------------------------------------------
        // バトル
        // ------------------------------------------------------------------

        private void StartBattle(EnemyInfo e)
        {
            player.StopWalking();
            KeyInput.Clear();
            grassSteps = 0;
            meetNewMonster = DexOnMeet(e);
            battle.Start(e);
            State = GameState.Battle;
        }

        private bool meetNewMonster; // 今のバトルの敵に、はじめて出会った

        /// <summary>ストーリーが どこまで進んだか（遊べるようになった いちばん先のクエストの章）</summary>
        private static int StoryChapter => QuestDatabase.ChapterOf(Mathf.Clamp(Progress.Unlocked, 1, QuestDatabase.LevelCount)).Number;

        private void OnBattleFinished(bool won, bool fled, EnemyInfo e)
        {
            State = area != null ? GameState.Area : GameState.Town;
            KeyInput.Clear();
            var storyWon = won ? storyBattle : null; // ストーリークエストの 調べる場所で出たアルカナ
            storyBattle = null;
            AutoSave();
            // 図鑑：たおした回数を記録
            string dexNote = "";
            if (e.Kind != BattleKind.Fishing)
            {
                if (won && Progress.AddDexDefeat(e.DexKey)) dexNote = "　図鑑に記録！";
                else if (meetNewMonster)
                {
                    dexNote = "　図鑑にのった！";
                    if (!won) ShowToast($"「{e.Name}」が図鑑にのった！ たおすと強さが記録されるよ。", 4f);
                }
            }
            if (e.Kind == BattleKind.Fishing)
            {
                // つり：金貨は BattleScreen でもらっている。金のさかなは資材もくれる
                if (!won) return;
                Progress.FishCaught++;
                string got = $"{e.Name}を つった！ 金貨+{e.Coins}";
                if (e.Name == "金のさかな")
                {
                    var target = CurrentBuilding();
                    var m = target != null ? Rebuild.MaterialOf(target.LastStage - 1) : Material.Gear;
                    Progress.AddMaterial(m, 2);
                    got += $"　{Rebuild.MaterialNames[(int)m]}+2（金のさかなのおまけ）";
                }
                ShowToast(got, 4f);
                return;
            }
            if (won && e.Boss)
            {
                Progress.MarkStory(StoryBossBeaten);
                dialog.Show("オルディナ", new[]
                {
                    "…見事だ、{name}。おまえは アルカナを たおすだけでなく、町を直し、アルカナを いやしてきた。",
                    "人とアルカナが 手をとりあえることを、おまえが 見せてくれた。世界を やりなおすのは やめよう。",
                    "師匠「やったな {name}！ 5つの国の戦争も終わり、世界のルーンコードが 元にもどったぞ！」",
                }, () => State = GameState.Ending);
                return;
            }
            if (won)
            {
                string got = $"{e.Name}をたおした！ 金貨+{e.Coins}　アルカナのかけら+1";
                Progress.AddMaterial(Material.BugShard, 1);
                // ときどき、今の建物で足りない資材を落としていく
                var target = CurrentBuilding();
                if (target != null && Random.value < 0.4f)
                {
                    var need = Rebuild.Needs(target);
                    for (int i = 0; i < (int)Material.BugShard; i++)
                    {
                        if (Progress.MaterialCount((Material)i) >= need[i]) continue;
                        Progress.AddMaterial((Material)i, 1);
                        got += $"　{Rebuild.MaterialNames[i]}+1";
                        break;
                    }
                }
                if (MarkMission(Missions.BeatBugmon, false)) got += "　（見習いの手帳：達成！）";
                if (e.Rare) got = "レアモンスターを たおした！ " + got;
                ShowToast(got + dexNote, 4f);
                if (storyWon != null) { CompleteStory(storyWon); return; }

                // 退治の依頼
                if (battleArea >= 0 && Progress.HuntArea == battleArea)
                {
                    Progress.HuntCount++;
                    if (Progress.HuntCount >= Rebuild.HuntTarget)
                    {
                        int area = battleArea;
                        Progress.HuntArea = -1;
                        Progress.HuntsDone++;
                        Progress.AddMaterial(Material.BugShard, Rebuild.HuntShards);
                        Progress.AddItem(ItemKind.Coin, Rebuild.HuntCoins(area));
                        dialog.Show("依頼板", $"退治の依頼を達成した！ {Rebuild.AreaNames[area]}が静かになった。" +
                            $"ほうびに アルカナのかけら {Rebuild.HuntShards}こ と 金貨 {Rebuild.HuntCoins(area)}枚 をもらった！");
                        return;
                    }
                    ShowToast(got + $"\n退治の依頼：{Progress.HuntCount} / {Rebuild.HuntTarget}体", 3.5f);
                }
                if (!Progress.StorySeen(StoryFirstBattle))
                {
                    Progress.MarkStory(StoryFirstBattle);
                    dialog.Show("師匠", "はじめてのバトル、よくやった！ アルカナのかけらは建物の資材になる。金貨がたまったら、お店で装備を買うといい。");
                }
                return;
            }
            if (!fled) WakeUpAtInn("目が覚めたか。アルカナは手ごわかったな。装備を見直したり、星を集めてランクを上げたりしてみよう。");
        }

        /// <summary>体力が0になった：宿舎（まだ直っていなければ町のはじめの場所）で目を覚ます。持ち物はへらない</summary>
        private void WakeUpAtInn(string masterLine)
        {
            if (area != null) LeaveArea();
            PlayerStats.FullHeal();
            var inn = TownData.Buildings[0];
            player.transform.position = town.Repaired(inn) ? town.DoorWorld(inn) : town.ToWorld(TownData.StartX, TownData.StartY);
            lastTile = town.ToTile(player.transform.position);
            FollowCamera(true);
            AutoSave(); // 宿舎の前で起きたところを記録
            dialog.Show("師匠", masterLine);
        }

        private void ShowToast(string text, float seconds)
        {
            toast = text;
            toastTimer = seconds;
        }

        // ------------------------------------------------------------------
        // タイトル・はじめから・つづきから
        // ------------------------------------------------------------------

        private void NextPrologue()
        {
            if (prologuePage < QuestDatabase.Prologue.Length - 1) prologuePage++;
            else StartGame();
        }

        /// <summary>セーブデータを消して、はじめの状態にもどす</summary>
        private void ResetSave()
        {
            Progress.ResetAll();
            CreateQuestScreen();
            BuildTown();
        }

        private void StartGame()
        {
            State = GameState.Town;
            KeyInput.Clear();
            if (!Progress.StorySeen(StoryIntro))
            {
                Progress.MarkStory(StoryIntro);
                TalkTo(TownData.Npcs[0]);
            }
            else if (!AnnounceStory()) ShowToast(ObjectiveText(), 5f);
        }

        // ------------------------------------------------------------------
        // 画面表示（IMGUI）
        // ------------------------------------------------------------------

        private void OnGUI()
        {
            KeyInput.ProcessEvent(Event.current);

            // マウス・タッチパッドで町をタップ（拡大縮小をかける前の画面座標で判定）
            if ((State == GameState.Town || State == GameState.Area) && !showInventory && !showMap && !showDex && !showMenu && !QuizOpen && !dialog.Active && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (State == GameState.Town) HandleWorldClick(Event.current.mousePosition);
                else HandleAreaClick(Event.current.mousePosition);
            }

            UIKit.Begin();

            switch (State)
            {
                case GameState.Title: DrawTitle(); break;
                case GameState.Town: DrawTownHud(); break;
                case GameState.Quest: questScreen.Draw(); break;
                case GameState.Battle: battle.Draw(); break;
                case GameState.Shop: DrawTownHud(false); shop.Draw(); break;
                case GameState.RequestBoard: DrawTownHud(false); board.Draw(); break;
                case GameState.StageSelect: DrawStageSelect(); break;
                case GameState.Ending: DrawEnding(); break;
                case GameState.Area: DrawAreaHud(); break;
                case GameState.Loading: DrawLoading(); break;
                case GameState.Balloon: balloon.Draw(); break;
                case GameState.Travel: DrawTownHud(false); DrawTravel(); break;
                case GameState.CharacterCreate: DrawCharacterCreate(); break;
            }
            if (State == GameState.Town || State == GameState.Area) dialog.Draw();

            UIKit.End();
        }

        /// <summary>町をタップ：建物 → 入り口まで歩いて調べる、町の人 → となりまで歩いて話す、ほか → そこまで歩く</summary>
        private void HandleWorldClick(Vector2 guiPos)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 ui = UIKit.ScreenToUI(guiPos);
            if ((ui.x < 520 && ui.y < 132) || (ui.x > 950 && ui.y < 92) || ui.y > UIKit.RefHeight - 40) return;

            Vector2 world = cam.ScreenToWorldPoint(new Vector3(guiPos.x, Screen.height - guiPos.y, 0f));
            Vector2 p = player.transform.position;
            Event.current.Use();

            var b = town.BuildingAt(world);
            if (b != null)
            {
                var door = town.DoorWorld(b);
                if (Vector2.Distance(p, door) < 1.1f) InteractBuilding(b);
                else WalkThen(town.FindPath(p, door), () => InteractBuilding(b));
                return;
            }
            if (town.Destination != null && town.ToTile(world) == new Vector2Int(town.Destination.X, town.Destination.Y))
            {
                if (NearDestination(p)) { EnterDestination(); return; }
                WalkThen(town.FindPath(p, town.SpotWorld(town.Destination)), EnterDestination);
                return;
            }
            if (cat != null && Vector2.Distance(world, CatWorld) < 0.7f)
            {
                // ねこ：となりまで歩いてなでる
                if (NearCat(p)) { PetCat(); return; }
                WalkThen(PathNextTo(p, town.ToWorld(catTile.x, catTile.y)), () => { if (NearCat(player.transform.position)) PetCat(); });
                return;
            }
            char tapped = town.CharAt(world);
            if (tapped == 'F')
            {
                // 噴水：となりまで歩いて願いごと
                var at = town.ToWorld(town.ToTile(world).x, town.ToTile(world).y);
                if (NearFountain(p)) { Wish(); return; }
                WalkThen(PathNextTo(p, at), () => { if (NearFountain(player.transform.position)) Wish(); });
                return;
            }
            if (tapped == 'G')
            {
                // 畑：となりまで歩いて調べる
                var at = town.ToWorld(town.ToTile(world).x, town.ToTile(world).y);
                if (Vector2.Distance(p, at) < 1.3f) { InteractTownObject(p); return; }
                WalkThen(PathNextTo(p, at), () => InteractTownObject(player.transform.position));
                return;
            }
            if (tapped == 'Q' || tapped == 'C')
            {
                // 依頼板・宝箱：となりまで歩いて調べる
                var at = town.ToWorld(town.ToTile(world).x, town.ToTile(world).y);
                if (Vector2.Distance(p, at) < 1.3f) { InteractTownObject(p); return; }
                WalkThen(PathNextTo(p, at), () => InteractTownObject(player.transform.position));
                return;
            }
            var npc = town.NpcAt(world);
            if (npc != null)
            {
                var at = town.ToWorld(npc.X, npc.Y);
                if (Vector2.Distance(p, at) < 1.3f) { TalkTo(npc); return; }
                WalkThen(PathNextTo(p, at), () => TalkTo(npc));
                return;
            }
            var path = town.FindPath(p, new Vector2(Mathf.Round(world.x), Mathf.Round(world.y)));
            if (path != null) player.WalkTo(path);
            else
            {
                char c = town.CharAt(world);
                if (c >= '1' && c <= '8')
                    ShowToast($"がれきで通れない。「{TownData.Buildings[c - '1'].Name}」を直すと片づくよ。", 3f);
            }
        }

        /// <summary>at のとなり（上下左右）までの、いちばん近い道</summary>
        private List<Vector2> PathNextTo(Vector2 from, Vector2 at) => WalkPath.NextTo(town, from, at);

        private void WalkThen(List<Vector2> path, System.Action arrive)
        {
            if (path == null)
            {
                ShowToast("そこまで行く道が見つからない…", 3f);
                return;
            }
            player.WalkTo(path, () => { if (State == GameState.Town || State == GameState.Area) arrive(); });
        }

        /// <summary>左上：キャラの強さ（名前・ランク・体力・金貨）と目的</summary>
        private void DrawStatusBox(string objective)
        {
            var box = new Rect(10, 10, 510, 120);
            UIKit.Fill(box, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Label(new Rect(22, 12, 490, 40), $"{UIKit.Colorize(Progress.Look.Name, UIKit.AccentColor)}　ランク {PlayerStats.Rank}　★{Progress.TotalStars}", UIKit.Body);
            UIKit.Fill(new Rect(22, 56, 220, 14), new Color(0, 0, 0, 0.6f));
            UIKit.Fill(new Rect(22, 56, 220 * PlayerStats.Hp / (float)PlayerStats.MaxHp, 14), new Color(0.3f, 0.85f, 0.4f));
            UIKit.Label(new Rect(250, 46, 140, 32), $"{PlayerStats.Hp}/{PlayerStats.MaxHp}", UIKit.Small);
            UIKit.Texture(new Rect(390, 48, 26, 26), SpriteFactory.TextureOf(QuestArt.Item(ItemKind.Coin)));
            UIKit.Label(new Rect(420, 46, 100, 32), $"{Progress.Coins}", UIKit.Small);
            UIKit.Label(new Rect(22, 82, 490, 44), objective, UIKit.Small);
        }

        /// <summary>町の表示：左上にキャラの強さと目的、建物の名前、町の人の名前</summary>
        private void DrawTownHud(bool interactive = true)
        {
            if (interactive) UIKit.Nav(showInventory ? "inventory" : "town", showInventory);

            DrawStatusBox(ObjectiveShort());

            if (interactive)
            {
                DrawPocketButton();
                DrawMapButton();
                DrawRecordButtons();
                if (!showInventory && UIKit.ColorButton(new Rect(1100, 14, 160, 40), "ステージ一覧", Color.gray, UIKit.SmallButton))
                    State = GameState.StageSelect;
                // ステージ一覧のボタンの下：いま やること（師匠が教えてくれるのと 同じ）
                if (!showInventory && !showMap && !showDex && !showMenu)
                {
                    var todo = new Rect(1000, 118, 270, 104);
                    UIKit.Fill(todo, new Color(0f, 0f, 0f, 0.7f));
                    UIKit.Label(new Rect(todo.x + 8, todo.y + 4, todo.width - 16, todo.height - 8),
                        UIKit.Colorize("いま やること", UIKit.WarnColor) + "\n" + ObjectiveText(), UIKit.Small);
                }
            }

            // 建物と町の人の名前
            foreach (var b in TownData.Buildings)
            {
                var top = town.ToWorld(b.X + b.W / 2, b.Y) + new Vector2(b.W % 2 == 0 ? -0.5f : 0f, 0.9f);
                DrawWorldLabel(top, BuildingLabel(b));
            }
            bool boardTime = BoardTime(); // 次は 依頼板で依頼を受けるとき：「！」
            foreach (var w in town.BoardWorlds()) DrawWorldLabel(w + Vector2.up * town.BoardLabelUp, boardTime ? UIKit.Colorize("！ 依頼板", new Color(1f, 0.6f, 1f)) : UIKit.Colorize("依頼板", UIKit.AccentColor));
            for (int i = 0; i < TownData.TownChests.Length; i++)
                if (!TownMap.ChestOpened(i)) DrawWorldLabel(town.ChestWorld(i) + Vector2.up * 0.8f, "宝箱");
            foreach (var n in town.ActiveNpcs())
                DrawWorldLabel(town.ToWorld(n.X, n.Y) + Vector2.up * 0.8f, IsMarkedNpc(n) ? UIKit.Colorize("！ " + n.Name, new Color(1f, 0.6f, 1f)) : n.Name);
            DrawStoryMarker();
            DrawQuestExclamation();
            if (cat != null) DrawWorldLabel(CatWorld + Vector2.up * 0.7f, "ねこ");
            DrawDistrictLabels();
            if (town.Destination != null)
            {
                DrawWorldLabel(town.SpotWorld(town.Destination) + Vector2.up * 1.6f, UIKit.Colorize("目的地：" + town.Destination.Name, UIKit.WarnColor));
                DrawDestinationPointer();
            }
            DrawPinIndicator();

            // 近くの物の案内
            if (interactive && !dialog.Active && !showInventory)
            {
                Vector2 p = player.transform.position;
                var b = NearDoor(p);
                bool nearBoard = b == null && NearBoard(p);
                bool nearChest = b == null && !nearBoard && NearTownChest(p) >= 0;
                bool nearCat = b == null && !nearBoard && !nearChest && NearCat(p);
                int nearBed = b == null && !nearBoard && !nearChest && !nearCat ? NearGardenBed(p) : -1;
                bool nearFountain = b == null && !nearBoard && !nearChest && !nearCat && nearBed < 0 && NearFountain(p);
                bool nearSpot = b == null && !nearBoard && !nearChest && !nearCat && nearBed < 0 && !nearFountain && NearDestination(p);
                var n = b == null && !nearBoard && !nearChest && !nearCat && nearBed < 0 && !nearFountain && !nearSpot ? NearNpc(p) : null;
                string prompt = b != null
                    ? (town.Repaired(b) ? $"E・スペース：{b.Name}に入る" : $"E・スペース：{b.Name}の工事の看板を調べる")
                    : nearBoard ? "E・スペース：依頼板を見る"
                    : nearChest ? "E・スペース：宝箱を開ける"
                    : nearCat ? "E・スペース：ねこをなでる"
                    : nearBed >= 0 ? $"E・スペース：{GardenPrompt(nearBed)}"
                    : nearFountain ? $"E・スペース：噴水に金貨を投げて願いごと（{WishCost}枚）"
                    : nearSpot ? "E・スペース：遺跡に入る（クエスト開始）"
                    : NearStorySpot(p) ? "E・スペース：調べる（ストーリークエスト）"
                    : n != null ? $"E・スペース：{NpcPrompt(n)}" : null;
                if (prompt != null)
                {
                    var r = new Rect(390, 420, 500, 50);
                    UIKit.Panel(r);
                    UIKit.Label(r, prompt, UIKit.Center);
                }
                if (Progress.RepelSteps > 0) UIKit.Label(new Rect(22, 134, 400, 30), UIKit.Colorize($"むしよけ のこり {Progress.RepelSteps}歩", UIKit.WarnColor), UIKit.Small);
            }

            if (toastTimer > 0f && !string.IsNullOrEmpty(toast) && !dialog.Active)
            {
                var t = new Rect(160, 586, 960, 88);
                UIKit.Fill(t, new Color(0.1f, 0.2f, 0.35f, 0.9f));
                UIKit.Label(t, toast, UIKit.Center);
            }

            if (interactive)
            {
                if (showInventory) DrawInventory();
                if (showMap) DrawMap();
                if (showDex) DrawDex();
                if (showMenu) DrawMenu();
            }
        }

        /// <summary>目的地が画面の外にあるとき、画面のはしに「→ 目的地」を出す</summary>
        private void DrawDestinationPointer() => DrawEdgePointer(town.SpotWorld(town.Destination), "！目的地", UIKit.WarnColor, 0);

        /// <summary>
        /// world が画面の外にあるとき、画面のはしに「→ 名前」を出す。
        /// offsetY は、ほかの矢印と重ならないように下へずらす量
        /// </summary>
        private void DrawEdgePointer(Vector2 world, string text, Color color, float offsetY)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(world);
            Vector2 ui = UIKit.ScreenToUI(new Vector2(sp.x, Screen.height - sp.y));
            const float left = 100, right = UIKit.RefWidth - 100, top = 170, bottom = UIKit.RefHeight - 140;
            if (ui.x >= left && ui.x <= right && ui.y >= top && ui.y <= bottom) return;
            string arrow = (ui.y < top ? "↑" : ui.y > bottom ? "↓" : "") + (ui.x < left ? "←" : ui.x > right ? "→" : "");
            var at = new Vector2(Mathf.Clamp(ui.x, left, right), Mathf.Clamp(ui.y, top, bottom) + offsetY);
            var r = new Rect(at.x - 95, at.y - 22, 190, 44);
            UIKit.Fill(r, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Label(r, UIKit.Colorize(arrow + " " + text, color), UIKit.SmallCenter);
        }

        private string ObjectiveShort()
        {
            if (town.Destination != null) return $"目的：「{town.Destination.Name}」へ行く（黄色い矢印）";
            string story = StoryObjective(true);
            if (story != null) return story;
            var b = CurrentBuilding();
            if (b == null) return VillageDoneShort();
            int left = Rebuild.MainStages(b).Count - TownMap.MainDone(b);
            if (left > 0) return $"目的：依頼板で依頼を受けて「{b.Name}」の資材を集める";
            if (!Rebuild.HasEnough(b, Progress.MaterialCount)) return $"目的：資材 {MissingText(b)}";
            return $"目的：「{b.Name}」の看板で仕上げクエスト！";
        }

        /// <summary>建物の上の名前（工事の進み具合つき）</summary>
        private string BuildingLabel(BuildingInfo b)
        {
            if (town.Repaired(b)) return b.Name;
            if (!town.CanWorkOn(b)) return UIKit.Colorize($"{b.Name}（工事中）", Color.gray);
            int left = Rebuild.MainStages(b).Count - TownMap.MainDone(b);
            string state = left > 0 ? $"依頼 あと{left}" : Rebuild.HasEnough(b, Progress.MaterialCount) ? "仕上げできる！" : "資材あつめ中";
            return UIKit.Colorize($"{b.Name}（{state}）", left == 0 && Rebuild.HasEnough(b, Progress.MaterialCount) ? UIKit.SuccessColor : UIKit.WarnColor);
        }

        private void DrawWorldLabel(Vector2 worldPos, string text)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            Vector2 ui = UIKit.ScreenToUI(new Vector2(sp.x, Screen.height - sp.y));
            if (ui.x < -100 || ui.x > UIKit.RefWidth + 100 || ui.y < -40 || ui.y > UIKit.RefHeight + 40) return;
            var r = new Rect(ui.x - 130, ui.y - 20, 260, 34);
            UIKit.Fill(new Rect(r.x + 40, r.y, r.width - 80, r.height), new Color(0f, 0f, 0f, 0.55f));
            UIKit.Label(r, text, UIKit.SmallCenter);
        }

        // ------------------------------------------------------------------
        // タイトル
        // ------------------------------------------------------------------

        private void DrawTitle()
        {
            if (titlePhase == TitlePhase.Start) { DrawStartScreen(); return; }
            UIKit.Nav("title-" + titlePhase, true);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.04f, 0.05f, 0.10f, 0.92f));
            UIKit.Label(new Rect(0, 90, UIKit.RefWidth, 70), QuestDatabase.GameTitle, UIKit.Title);
            UIKit.Label(new Rect(0, 156, UIKit.RefWidth, 44), QuestDatabase.GameSubtitle, UIKit.Center);

            HandleTitleBack();
            switch (titlePhase)
            {
                case TitlePhase.Menu: DrawTitleMenu(); return;
                case TitlePhase.SlotLoad: DrawSlotList(false); return;
                case TitlePhase.SlotNew: DrawSlotList(true); return;
                case TitlePhase.SlotDetail: DrawSlotDetail(); return;
                case TitlePhase.SlotName: DrawSlotName(); return;
                case TitlePhase.ConfirmOverwrite:
                    DrawConfirm($"データ{pickedSlot}「{SaveSlots.Name(pickedSlot)}」を消して、ここに新しく作りますか？", "はい、ここに作る", OpenSlotName, TitlePhase.SlotNew);
                    return;
                case TitlePhase.ConfirmDelete:
                    DrawConfirm($"データ{SaveSlots.Current}「{SaveSlots.Name(SaveSlots.Current)}」を消しますか？", "消す", DeleteCurrentSlot, TitlePhase.SlotDetail);
                    return;
            }

            // あらすじは1文ずつ。「つぎへ」で前の文が消えて次の文になる
            var story = new Rect(200, 250, 880, 200);
            UIKit.Panel(story, UIKit.SubPanelColor);
            UIKit.Label(new Rect(story.x + 40, story.y + 20, story.width - 80, story.height - 60),
                QuestDatabase.Prologue[prologuePage].Replace("{name}", Progress.Look.Name), UIKit.Center);
            string dots = "";
            for (int i = 0; i < QuestDatabase.Prologue.Length; i++)
                dots += UIKit.Colorize(i == prologuePage ? "●" : "○", i == prologuePage ? UIKit.AccentColor : Color.gray) + " ";
            UIKit.Label(new Rect(story.x, story.yMax - 40, story.width, 30), dots, UIKit.SmallCenter);

            bool last = prologuePage >= QuestDatabase.Prologue.Length - 1;
            if (UIKit.ColorButton(new Rect(490, 490, 300, 64), last ? "はじめる" : "つぎへ ▶", UIKit.AccentColor, UIKit.BigButton, primary: true))
                NextPrologue();
            if (!last && UIKit.ColorButton(new Rect(1040, 580, 160, 40), "とばす ≫", Color.gray, UIKit.SmallButton))
                StartGame();
            UIKit.HelpBar("クリック または Enter: つぎへ");
        }

        private void DrawConfirm(string question, string yesLabel, System.Action onYes, TitlePhase back)
        {
            var box = new Rect(290, 240, 700, 260);
            UIKit.Panel(box, UIKit.SubPanelColor);
            UIKit.Label(new Rect(box.x + 30, box.y + 30, box.width - 60, 50), question, UIKit.Center);
            UIKit.Label(new Rect(box.x + 30, box.y + 90, box.width - 60, 40),
                UIKit.Colorize($"クリアした {Progress.ClearedCount} ステージ・キャラ・持ち物の記録が消えて、もとにもどせません。", UIKit.WarnColor), UIKit.Center);
            if (UIKit.ColorButton(new Rect(box.x + 60, box.y + 170, 270, 60), yesLabel, new Color(0.85f, 0.35f, 0.35f), UIKit.BigButton))
                onYes();
            if (UIKit.ColorButton(new Rect(box.x + 370, box.y + 170, 270, 60), "やめる", Color.gray, UIKit.BigButton, primary: true))
                GoTitlePhase(back);
            UIKit.HelpBar("セーブデータを消すと、最初からになります");
        }

        // ------------------------------------------------------------------
        // ステージ一覧（前のクエストにもう一度挑戦する）
        // ------------------------------------------------------------------

        private void DrawStageSelect()
        {
            UIKit.Nav("stages", true, 10);
            // 時計台の中の絵（Resources/Art/Shop/時計台の中.png）があれば うしろに しく
            var inside = ArtOverrides.Get("Art/Shop/時計台の中");
            if (inside != null)
            {
                var tex = inside.texture;
                float bw = UIKit.RefWidth, bh = bw * tex.height / tex.width;
                if (bh < UIKit.RefHeight) { bh = UIKit.RefHeight; bw = bh * tex.width / tex.height; }
                UIKit.Texture(new Rect((UIKit.RefWidth - bw) / 2f, (UIKit.RefHeight - bh) / 2f, bw, bh), tex);
                UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.04f, 0.05f, 0.10f, 0.72f));
            }
            else UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.04f, 0.05f, 0.10f, 0.95f));
            UIKit.Label(new Rect(80, 16, 600, 50), "ステージ一覧", UIKit.Heading);
            UIKit.Label(new Rect(80, 56, 1150, 30),
                $"★ {Progress.TotalStars} / {QuestDatabase.LevelCount * 3}　　遊んだクエストに もう一度挑戦できる（クリアで資材1こ・星3つを目指そう）。" +
                UIKit.Colorize("「通過」のクエストも、クリアすれば星がもらえる", UIKit.WarnColor) + "　Esc: もどる", UIKit.Small);

            bool first = true;
            for (int i = 0; i < QuestDatabase.LevelCount; i++)
            {
                int n = i + 1;
                var r = new Rect(80 + (i % 10) * 112, 96 + (i / 10) * 48, 104, 42);
                int st = Progress.StarsOf(n);
                bool played = Progress.StatusOf(n) != Progress.NotPlayed;
                bool passedOnly = Progress.StatusOf(n) == Progress.Passed;
                string mark = passedOnly ? "通過" : played ? QuestStars.Mark(st) : Rebuild.IsExtra(n) ? "おまけ" : "";
                var b = TownData.BuildingOfStage(n);
                var color = passedOnly ? new Color(0.85f, 0.5f, 0.2f) : played ? Color.HSVToRGB(b.Village / 6f, 0.3f + 0.08f * ((b.Number - 1) % 4), 0.75f) : new Color(0.3f, 0.3f, 0.3f);
                if (UIKit.ColorButton(r, $"{n} {mark}", color, UIKit.SmallButton, played, primary: played && first))
                    AcceptReplay(n);
                if (played) first = false;
            }

            float x = 80;
            foreach (var v in TownData.Villages)
            {
                UIKit.Fill(new Rect(x, 594, 16, 16), Color.HSVToRGB(v.Index / 6f, 0.4f, 0.75f));
                UIKit.Label(new Rect(x + 20, 584, 150, 34), $"{v.Name} {v.FirstStage}〜{v.LastStage}", UIKit.Small);
                x += 172;
            }
            if (UIKit.ColorButton(new Rect(1080, 626, 150, 44), "もどる", Color.gray))
                State = GameState.Town;
            if (UIKit.ColorButton(new Rect(80, 626, 200, 44), "タイトルへ", Color.gray))
            {
                titlePhase = TitlePhase.Menu;
                titleNote = null;
                State = GameState.Title;
            }
        }

        private void DrawEnding()
        {
            UIKit.Nav("ending", true);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.04f, 0.05f, 0.10f, 0.92f));
            UIKit.Label(new Rect(0, 60, UIKit.RefWidth, 90), "世界に 平和が もどった！ おめでとう！", UIKit.Title);

            var r = new Rect(200, 165, 880, 380);
            UIKit.Panel(r, UIKit.SubPanelColor);
            var sb = new System.Text.StringBuilder();
            sb.Append($"{Progress.Look.Name}　ランク {PlayerStats.Rank}\n\n");
            sb.Append($"クリアしたステージ: {Progress.ClearedCount} / {QuestDatabase.LevelCount}　　星: {Progress.TotalStars} / {QuestDatabase.LevelCount * 3}\n");
            sb.Append($"装備: {PlayerStats.Weapon.Name}・{PlayerStats.Armor.Name}　　金貨: {Progress.Coins} 枚\n\n");
            sb.Append("プログラミングで 6つの村を直し、タイピングでアルカナを しずめた。\n人とアルカナは、また いっしょに くらしはじめた。あなたはもう、立派なルーンコード使いです！\n\n");
            sb.Append("時計台で、星3つを目指して もう一度挑戦できます。");
            UIKit.Label(new Rect(r.x + 30, r.y + 18, r.width - 60, r.height - 30), sb.ToString(), UIKit.Body);

            if (UIKit.ColorButton(new Rect(340, 570, 280, 60), "町にもどる", Color.gray, UIKit.BigButton, primary: true))
            {
                State = GameState.Town;
                KeyInput.Clear();
            }
            if (UIKit.ColorButton(new Rect(660, 570, 280, 60), "タイトルへ", UIKit.AccentColor, UIKit.BigButton))
            {
                prologuePage = 0;
                titlePhase = TitlePhase.Menu;
                BuildTown();
                State = GameState.Title;
            }
        }
    }
}
