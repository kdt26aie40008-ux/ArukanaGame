using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：遺跡のエリア（依頼で行く場所）と、場所を移動するときの読みこみ画面。
    ///
    /// 町の目的地（遺跡の入り口）→ 読みこみ画面 → エリア
    /// ・ゴーレムを調べる → クエスト。終えると、ゴーレムがこわれた橋を直して向こう岸へ行ける
    /// ・宝箱（金貨・資材）・薬草・看板（エリアのお話）・草むら（アルカナ）
    /// ・下の出口 → 読みこみ画面 → 町（入り口の前）
    /// </summary>
    public partial class GameManager
    {
        /// <summary>エリアを作る場所（町からはなれた所に作るので、町はそのまま残る）</summary>
        private static readonly Vector2 AreaOrigin = new Vector2(200f, 0f);

        private AreaMap area;
        private RequestKind areaKind;
        private Vector2 townReturn;

        private string loadingTitle, loadingSub;
        private float loadingTimer;
        private System.Action loadingDone;

        private const float LoadingSeconds = 1.3f;
        private const float AreaEncounterRate = 0.12f;

        // ------------------------------------------------------------------
        // 読みこみ画面
        // ------------------------------------------------------------------

        private void StartLoading(string title, string sub, System.Action done)
        {
            player.StopWalking();
            KeyInput.Clear();
            loadingTitle = title;
            loadingSub = sub;
            loadingTimer = 0f;
            loadingDone = done;
            State = GameState.Loading;
        }

        private void UpdateLoading()
        {
            loadingTimer += Time.deltaTime;
            if (loadingTimer < LoadingSeconds) return;
            var done = loadingDone;
            loadingDone = null;
            done?.Invoke();
        }

        private void DrawLoading()
        {
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.03f, 0.03f, 0.06f, 1f));
            // 場所の名前（ふりがなで上にのびる）とお話が重ならないように、間をあける
            UIKit.Label(new Rect(0, 200, UIKit.RefWidth, 90), loadingTitle, UIKit.Title);
            if (!string.IsNullOrEmpty(loadingSub)) UIKit.Label(new Rect(140, 320, 1000, 90), loadingSub, UIKit.Center);
            var bar = new Rect(390, 440, 500, 16);
            UIKit.Fill(bar, new Color(1f, 1f, 1f, 0.15f));
            UIKit.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(loadingTimer / LoadingSeconds), bar.height), UIKit.AccentColor);
        }

        // ------------------------------------------------------------------
        // エリアに入る・出る
        // ------------------------------------------------------------------

        private void EnterArea(int stage, RequestKind kind)
        {
            if (area != null) Destroy(area.gameObject);
            area = new GameObject("Area").AddComponent<AreaMap>();
            area.Build(stage, AreaOrigin);
            areaKind = kind;
            MarkMission(Missions.EnteredRuin);
            areaPin = null; // エリアのピンは、そのエリアにいるあいだだけ
            townReturn = player.transform.position;
            player.SetMap(area, area.ExitWorld + Vector2.up);
            lastTile = area.ToTile(player.transform.position);
            State = GameState.Area;
            FollowCamera(true);
            AutoSave();

            // お話は、はじめて来たときに1回だけ。2回目からは看板で読める
            if (!area.IntroSeen && !area.BridgeFixed)
            {
                area.MarkIntroSeen();
                dialog.Show("案内", AreaStory());
            }
            else ShowToast($"「{area.Theme.Area}」に着いた。お話は「看板」で もう一度読めるよ。", 4f);
        }

        /// <summary>エリアのお話（着いたときと、看板で読むとき）</summary>
        private string[] AreaStory()
        {
            var th = area.Theme;
            return new[]
            {
                $"「{th.Area}」に着いた。{th.Story}",
                area.BridgeFixed
                    ? "ゴーレムがこわれた橋を直してくれたので、向こう岸へ行ける。大きな宝箱をさがしてみよう。"
                    : $"川の手前で、ゴーレムが待っている。ゴーレムを調べて、クエスト「{th.Title}」に挑戦しよう！",
                "クエストを終えると、ゴーレムがこわれた橋を直してくれる。宝箱や薬草もさがしてみよう。帰るときは下の出口から。",
            };
        }

        /// <summary>エリアから町（入り口の前）にもどる</summary>
        private void LeaveArea()
        {
            if (area == null) return;
            Destroy(area.gameObject);
            area = null;
            quiz = null;
            quizAttempt = 0;
            player.SetMap(town, townReturn);
            lastTile = town.ToTile(player.transform.position);
            State = GameState.Town;
            KeyInput.Clear();
            FollowCamera(true);
            ShowToast(ObjectiveText(), 6f);
            AutoSave();
        }

        private void ExitArea() => StartLoading("町へ もどっている…", null, LeaveArea);

        // ------------------------------------------------------------------
        // エリアを歩く
        // ------------------------------------------------------------------

        private void UpdateArea()
        {
            if (area.IsInn) { UpdateInn(); return; }
            Vector2 p = player.transform.position;
            if (CheckCactus(p)) return;
            if (KeyInput.InteractPressed() && TryInteractArea(p)) return;

            var tile = area.ToTile(p);
            if (tile == lastTile) return;
            lastTile = tile;
            OnStep(area.IsTallGrass(p));
            if (Progress.RepelSteps > 0) Progress.RepelSteps--;

            // 薬草：上を歩くと ひろう
            int herb = area.HerbAt(tile);
            if (herb >= 0 && !area.HerbTaken(herb))
            {
                area.MarkHerb(herb);
                Progress.AddItem(ItemKind.Herb, 1);
                ShowToast("薬草を ひろった！（バトル中に Tab で使える）", 3f);
            }

            if (area.IsTallGrass(p))
            {
                // 遺跡も 今いる村のアルカナ（＋旅の道のアルカナ）。ときどき ほかの国の凶暴な亜種
                var foe = Progress.RepelSteps <= 0 ? BattleData.RollEncounter(BattleData.CurrentVillage, AreaEncounterRate, true, StoryChapter) : null;
                if (foe != null)
                {
                    battleArea = -1; // 町の退治の依頼には数えない
                    StartBattle(BattleData.Scaled(foe, PlayerStats.Rank));
                }
            }
        }

        /// <summary>ステージが進むほど、エリアのアルカナも強くなる</summary>

        /// <summary>近くの物を調べる。調べたら true</summary>
        private bool TryInteractArea(Vector2 p)
        {
            if (Vector2.Distance(p, area.ExitWorld) < 1.1f) { ExitArea(); return true; }
            if (Vector2.Distance(p, area.GolemWorld) < 1.3f) { TalkToGolem(); return true; }
            for (int i = 0; i < area.Layout.Chests.Count; i++)
                if (Vector2.Distance(p, area.ChestWorld(i)) < 1.3f) { OpenChest(i); return true; }
            if (Vector2.Distance(p, area.SignWorld) < 1.3f) { ReadSign(); return true; }
            if (Vector2.Distance(p, area.SpringWorld) < 1.3f) { DrinkSpring(); return true; }
            if (Vector2.Distance(p, area.TravelerWorld) < 1.3f) { TalkToTraveler(); return true; }
            for (int i = 0; i < area.Layout.DigSpots.Count; i++)
                if (!area.DigFound(i) && Vector2.Distance(p, area.DigWorld(i)) < 1.1f) { Dig(i); return true; }
            return false;
        }

        private void TalkToGolem()
        {
            KeyInput.Clear();
            var th = area.Theme;
            if (!area.BridgeFixed)
            {
                dialog.Show("ゴーレム", $"ゴゴ…（命令を待っている。クエスト「{th.Title}」を始めよう）", () => OpenQuest(area.Stage, areaKind));
                return;
            }
            dialog.Show("ゴーレム", "ゴゴ！（橋は直した。もう一度クエストに挑戦して、星3つを目指す？）",
                () => OpenQuest(area.Stage, RequestKind.Replay));
        }

        private void OpenChest(int i)
        {
            KeyInput.Clear();
            if (area.ChestOpened(i))
            {
                ShowToast("宝箱は もう からっぽだ。", 2.5f);
                return;
            }
            area.MarkChest(i);
            int stage = area.Stage;
            var m = Rebuild.MaterialOf(stage);
            string got;
            if (area.IsBigChest(i))
            {
                int coins = 15 + stage / 2;
                Progress.AddMaterial(m, 2);
                Progress.AddItem(ItemKind.Coin, coins);
                got = $"大きな宝箱を開けた！ {Rebuild.MaterialNames[(int)m]}を 2こ と 金貨を {coins}枚 手に入れた！";
            }
            else if (i == 0)
            {
                int coins = 8 + stage / 3;
                Progress.AddItem(ItemKind.Coin, coins);
                got = $"宝箱を開けた！ 金貨を {coins}枚 手に入れた！";
            }
            else
            {
                Progress.AddMaterial(m, 1);
                got = $"宝箱を開けた！ {Rebuild.MaterialNames[(int)m]}を 1こ 手に入れた！";
            }
            dialog.Show("宝箱", got);
        }

        private void ReadSign()
        {
            KeyInput.Clear();
            dialog.Show("看板", AreaStory());
        }

        /// <summary>エリアをタップ：物 → となりまで歩いて調べる、出口 → 歩いて出る、ほか → そこまで歩く</summary>
        private void HandleAreaClick(Vector2 guiPos)
        {
            if (area.IsInn) { HandleInnClick(guiPos); return; }
            var cam = Camera.main;
            if (cam == null) return;
            Vector2 ui = UIKit.ScreenToUI(guiPos);
            if ((ui.x < 520 && ui.y < 132) || (ui.x > 950 && ui.y < 92) || ui.y > UIKit.RefHeight - 40) return;

            Vector2 world = cam.ScreenToWorldPoint(new Vector3(guiPos.x, Screen.height - guiPos.y, 0f));
            Vector2 p = player.transform.position;
            Event.current.Use();
            var t = area.ToTile(world);
            char c = area.Layout.At(t.x, t.y);

            if (c == 'E')
            {
                if (Vector2.Distance(p, area.ExitWorld) < 1.1f) ExitArea();
                else WalkThen(area.FindPath(p, area.ExitWorld), ExitArea);
                return;
            }
            if (c == 'x')
            {
                // キラキラ：その上まで歩いて調べる
                var at = area.ToWorld(t.x, t.y);
                if (Vector2.Distance(p, at) < 1.1f) { TryInteractArea(p); return; }
                WalkThen(area.FindPath(p, at), () => TryInteractArea(player.transform.position));
                return;
            }
            if (c == 'G' || c == 'g' || c == 'C' || c == 'K' || c == 'S' || c == 'o' || c == 'v')
            {
                Vector2 at = c == 'g' ? area.GolemWorld : area.ToWorld(t.x, t.y);
                if (Vector2.Distance(p, at) < 1.3f) { TryInteractArea(p); return; }
                WalkThen(WalkPath.NextTo(area, p, at), () => TryInteractArea(player.transform.position));
                return;
            }
            var path = area.FindPath(p, new Vector2(Mathf.Round(world.x), Mathf.Round(world.y)));
            if (path != null) player.WalkTo(path);
            else if (c == '=' || c == '~') ShowToast("橋がこわれていて わたれない。ゴーレムのクエストを終えると直るよ。", 3f);
        }

        // ------------------------------------------------------------------
        // エリアの表示
        // ------------------------------------------------------------------

        private void DrawAreaHud()
        {
            if (area.IsInn) { DrawInnHud(); return; }
            UIKit.Nav(showInventory ? "inventory" : "area", showInventory);
            var th = area.Theme;
            DrawStatusBox(area.BridgeFixed
                ? "目的：向こう岸の大きな宝箱をさがそう・出口から町へ"
                : $"目的：ゴーレムを調べて「{th.Title}」に挑戦");
            DrawPocketButton();
            DrawMapButton();
            DrawRecordButtons();

            // エリアの名前
            var title = new Rect(530, 10, 420, 44);
            UIKit.Fill(title, new Color(0f, 0f, 0f, 0.6f));
            UIKit.Label(title, th.Area, UIKit.Center);

            DrawQuestExclamation();
            DrawWorldLabel(area.GolemWorld + Vector2.up * 0.9f, UIKit.Colorize(area.BridgeFixed ? "ゴーレム" : "ゴーレム（クエスト）", UIKit.WarnColor));
            DrawWorldLabel(area.ExitWorld + Vector2.up * 0.8f, "出口（町へ）");
            DrawWorldLabel(area.SignWorld + Vector2.up * 0.8f, "看板");
            DrawWorldLabel(area.SpringWorld + Vector2.up * 0.9f, UIKit.Colorize(area.SpringName, new Color(0.6f, 0.85f, 1f)));
            DrawWorldLabel(area.TravelerWorld + Vector2.up * 0.9f, area.QuizDone ? "旅人" : UIKit.Colorize("旅人（クイズ）", UIKit.WarnColor));
            for (int i = 0; i < area.Layout.DigSpots.Count; i++)
                if (!area.DigFound(i)) DrawWorldLabel(area.DigWorld(i) + Vector2.up * 0.8f, "キラキラ？");
            for (int i = 0; i < area.Layout.Chests.Count; i++)
                if (!area.ChestOpened(i)) DrawWorldLabel(area.ChestWorld(i) + Vector2.up * 0.9f, area.IsBigChest(i) ? "大きな宝箱" : "宝箱");

            if (!dialog.Active && !showInventory)
            {
                Vector2 p = player.transform.position;
                string prompt = Vector2.Distance(p, area.ExitWorld) < 1.1f ? "E・スペース：町へもどる"
                    : Vector2.Distance(p, area.GolemWorld) < 1.3f ? "E・スペース：ゴーレムを調べる（クエスト）"
                    : Vector2.Distance(p, area.SignWorld) < 1.3f ? "E・スペース：看板を読む"
                    : Vector2.Distance(p, area.SpringWorld) < 1.3f ? $"E・スペース：{area.SpringName}で休む（体力回復）"
                    : Vector2.Distance(p, area.TravelerWorld) < 1.3f ? "E・スペース：旅人と話す（クイズ）"
                    : null;
                for (int i = 0; prompt == null && i < area.Layout.DigSpots.Count; i++)
                    if (!area.DigFound(i) && Vector2.Distance(p, area.DigWorld(i)) < 1.1f) prompt = "E・スペース：キラキラ光る地面を調べる";
                for (int i = 0; prompt == null && i < area.Layout.Chests.Count; i++)
                    if (Vector2.Distance(p, area.ChestWorld(i)) < 1.3f) prompt = "E・スペース：宝箱を開ける";
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

            DrawPinIndicator();
            if (showInventory) DrawInventory();
            if (showMap) DrawMap();
            if (showDex) DrawDex();
            if (showMenu) DrawMenu();
            if (QuizOpen) DrawQuiz();
        }
    }
}
