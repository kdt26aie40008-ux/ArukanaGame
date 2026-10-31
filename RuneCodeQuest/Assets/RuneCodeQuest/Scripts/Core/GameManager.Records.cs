using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：アルカナ図鑑とセーブ。
    /// ・図鑑（Z）：出会ったアルカナと、たおした回数。まだ見ていないものは「？？？」
    /// ・セーブ：持ち物やクリアの記録は いつも自動で記録される。場所は
    ///   オートセーブ（30秒ごと・バトルのあと・遺跡に出入りしたとき）と、自分でセーブした所の2つを覚えておく
    /// </summary>
    public partial class GameManager
    {
        // ------------------------------------------------------------------
        // 図鑑
        // ------------------------------------------------------------------

        private bool showDex;
        private int dexTab;    // 0〜5：村、6：凶暴な亜種、7：ラスボス
        private int dexIndex;  // タブの中の何番目か

        private const int DexCols = 5;
        private const int DexPageSize = 20;  // 1ページ：5列 × 4行
        private static int DexVariantTab => BattleData.RegionNames.Length;
        private static int DexTabs => BattleData.RegionNames.Length + 2;

        /// <summary>タブの中のアルカナ：村は ふつう12・旅の道4・レア3・ぬし1 の20体、つぎは凶暴な亜種（全部の国）、最後のタブはラスボス</summary>
        private static List<EnemyInfo> DexList(int tab)
        {
            var list = new List<EnemyInfo>();
            if (tab < BattleData.RegionNames.Length)
            {
                list.AddRange(BattleData.EnemiesByRegion[tab]);
                list.AddRange(BattleData.FieldByRegion[tab]);
                list.AddRange(BattleData.RaresByRegion[tab]);
                list.AddRange(BattleData.FieldBossByRegion[tab]);
            }
            else if (tab == DexVariantTab) list.AddRange(BattleData.AllVariants());
            else list.Add(BattleData.Boss);
            return list;
        }

        private static string DexTabName(int tab) =>
            tab < BattleData.RegionNames.Length ? BattleData.RegionNames[tab] : tab == DexVariantTab ? "凶暴な亜種" : "ラスボス";

        private void ToggleDex()
        {
            showDex = !showDex;
            KeyInput.Clear();
            if (!showDex)
            {
                if (dexFromBag) showInventory = true;
                dexFromBag = false;
                return;
            }
            dexFromBag = false;
            showInventory = false;
            showMap = false;
            player.StopWalking();
        }

        /// <summary>図鑑のキー（Esc で閉じる・矢印でえらぶ・Q／E でタブ）。図鑑が開いていたら true</summary>
        private bool UpdateDexKeys()
        {
            if (!showDex) return false;
            if (KeyInput.WasPressed(KeyCode.Escape)) { ToggleDex(); return false; }
            if (KeyInput.WasPressed(KeyCode.Q)) SetDexTab(dexTab - 1);
            if (KeyInput.WasPressed(KeyCode.E)) SetDexTab(dexTab + 1);
            int count = DexList(dexTab).Count;
            int d = KeyInput.WasPressed(KeyCode.RightArrow) ? 1 : KeyInput.WasPressed(KeyCode.LeftArrow) ? -1
                  : KeyInput.WasPressed(KeyCode.DownArrow) ? DexCols : KeyInput.WasPressed(KeyCode.UpArrow) ? -DexCols : 0;
            if (d != 0) dexIndex = Mathf.Clamp(dexIndex + d, 0, count - 1);
            return true;
        }

        private void SetDexTab(int tab)
        {
            dexTab = (tab % DexTabs + DexTabs) % DexTabs;
            dexIndex = 0;
        }

        private bool dexFromBag; // 持ち物から開いた（閉じたら持ち物にもどる）

        private void OpenDexFromBag()
        {
            showInventory = false;
            ToggleDex();
            dexFromBag = true;
        }

        private static Color DexRoleColor(EnemyInfo e) =>
            e.Variant ? new Color(0.5f, 0.25f, 0.15f)
            : e.Boss || e.Role == ArcanaRole.FieldBoss ? new Color(0.5f, 0.2f, 0.25f)
            : e.Rare ? new Color(0.55f, 0.45f, 0.2f)
            : e.Role == ArcanaRole.Field ? new Color(0.2f, 0.38f, 0.3f)
            : UIKit.SubPanelColor;

        private void DrawDex()
        {
            UIKit.Nav("dex", false);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.55f));
            var panel = new Rect(50, 40, 1180, 640);
            UIKit.Panel(panel);

            int total = 0, seen = 0, beaten = 0;
            foreach (var e in BattleData.AllMonsters())
            {
                total++;
                if (Progress.DexSeen(e.Name)) seen++;
                if (Progress.DexDefeated(e.Name) > 0) beaten++;
            }
            UIKit.Label(new Rect(panel.x + 20, panel.y + 6, 400, 44), UIKit.Colorize("アルカナ図鑑", UIKit.AccentColor), UIKit.Body);
            UIKit.Label(new Rect(panel.x + 300, panel.y + 10, 620, 40), $"見つけた {seen} / {total}　　たおした {beaten} / {total}", UIKit.Body);
            if (UIKit.ColorButton(new Rect(panel.xMax - 140, panel.y + 8, 124, 40), "閉じる", Color.gray, UIKit.SmallButton)) ToggleDex();

            // 村のタブ（その村の見つけた数も出す）
            float tw = (panel.width - 32) / DexTabs;
            for (int t = 0; t < DexTabs; t++)
            {
                var list = DexList(t);
                int s = 0;
                foreach (var e in list) if (Progress.DexSeen(e.Name)) s++;
                var tr = new Rect(panel.x + 16 + t * tw, panel.y + 54, tw - 6, 44);
                if (UIKit.ColorButton(tr, $"{DexTabName(t)} {s}/{list.Count}", t == dexTab ? UIKit.AccentColor : Color.gray, UIKit.SmallButton))
                    SetDexTab(t);
            }

            // 一覧：5列 × 4行（ふつう → 旅の道 → レア → ぬし の順）
            var items = DexList(dexTab);
            dexIndex = Mathf.Clamp(dexIndex, 0, items.Count - 1);
            int pages = Mathf.Max(1, (items.Count + DexPageSize - 1) / DexPageSize);
            int page = Mathf.Max(0, dexIndex) / DexPageSize;
            for (int i = page * DexPageSize; i < items.Count && i < (page + 1) * DexPageSize; i++)
            {
                var e = items[i];
                int k = i - page * DexPageSize;
                var card = new Rect(panel.x + 16 + (k % DexCols) * 230, panel.y + 106 + (k / DexCols) * 92, 222, 86);
                if (UIKit.ColorButton(card, "", i == dexIndex ? UIKit.AccentColor : DexRoleColor(e), UIKit.SmallButton)) dexIndex = i;
                DrawDexCard(card, e);
            }
            UIKit.Label(new Rect(panel.x + 16, panel.y + 476, panel.width - 32, 26),
                UIKit.Colorize("■ふつう", new Color(0.6f, 0.65f, 0.85f)) + "　" + UIKit.Colorize("■旅の道", new Color(0.45f, 0.8f, 0.55f)) + "　" +
                UIKit.Colorize("■レア", new Color(1f, 0.85f, 0.3f)) + "　" + UIKit.Colorize("■ぬし・ボス", new Color(1f, 0.45f, 0.45f)) + "　" +
                UIKit.Colorize("■凶暴な亜種", new Color(1f, 0.6f, 0.35f)), UIKit.Small);
            if (pages > 1)
            {
                // ページ切りかえ（亜種の欄）
                UIKit.Label(new Rect(panel.xMax - 330, panel.y + 476, 120, 26), $"{page + 1} / {pages} ページ", UIKit.Small);
                if (page > 0 && UIKit.ColorButton(new Rect(panel.xMax - 200, panel.y + 472, 88, 32), "◀ まえ", Color.gray, UIKit.SmallButton))
                    dexIndex = (page - 1) * DexPageSize;
                if (page < pages - 1 && UIKit.ColorButton(new Rect(panel.xMax - 104, panel.y + 472, 88, 32), "つぎ ▶", Color.gray, UIKit.SmallButton))
                    dexIndex = (page + 1) * DexPageSize;
            }

            // えらんだアルカナのくわしい説明
            var info = new Rect(panel.x + 16, panel.yMax - 128, panel.width - 32, 116);
            UIKit.Fill(info, new Color(0f, 0f, 0f, 0.45f));
            if (items.Count > 0) DrawDexDetail(info, items[dexIndex]);
            UIKit.HelpBar("矢印・クリック：えらぶ　Q／E：村のタブ　Esc：閉じる（持ち物から開いたときは持ち物にもどる）");
        }

        private static void DrawDexCard(Rect card, EnemyInfo e)
        {
            bool seen = Progress.DexSeen(e.Name);
            int beaten = Progress.DexDefeated(e.Name);
            var tex = SpriteFactory.TextureOf(MonsterArt.Build(e));
            float size = card.height - 8;
            var pic = new Rect(card.x + 4, card.y + 4, size, size);
            if (seen) UIKit.Texture(pic, tex);
            else UIKit.TextureTinted(pic, tex, new Color(0f, 0f, 0f, 0.85f)); // まだ見ていない：かげだけ
            UIKit.Label(new Rect(card.x + size + 10, card.y + 4, card.width - size - 12, 30), seen ? e.Name : "？？？", UIKit.Small);
            string status = beaten > 0 ? UIKit.Colorize($"たおした ×{beaten}", UIKit.SuccessColor)
                : seen ? UIKit.Colorize("見つけた", UIKit.WarnColor)
                : UIKit.Colorize("まだ", new Color(1f, 1f, 1f, 0.5f));
            UIKit.Label(new Rect(card.x + size + 10, card.y + card.height - 32, card.width - size - 12, 28), status, UIKit.Small);
        }

        /// <summary>出る場所の説明</summary>
        private static string DexWhere(EnemyInfo e)
        {
            if (e.Boss) return "お城の奥";
            int r = Mathf.Clamp(e.Region, 0, BattleData.RegionNames.Length - 1);
            string village = BattleData.RegionNames[r], field = BattleData.FieldNames[r];
            if (e.Variant) return $"{village}ではない ほかの国の草むら・遺跡のエリア（ストーリーが進むと ときどき出る。{village}の 元のアルカナが 凶暴になった）";
            switch (e.Role)
            {
                case ArcanaRole.Field: return $"旅の道「{field}」（今は {village}の章の遺跡のエリア）";
                case ArcanaRole.Rare: return $"{village}のまわりの草むら・遺跡のエリア（めったに出ない）";
                case ArcanaRole.FieldBoss: return $"旅の道「{field}」の出口（まだ行けない）";
                default: return $"{village}のまわりの草むら・遺跡のエリア";
            }
        }

        private static string DexKind(EnemyInfo e) =>
            e.Variant ? UIKit.Colorize("凶暴な亜種", new Color(1f, 0.6f, 0.35f))
            : e.Boss ? UIKit.Colorize("ボス", new Color(1f, 0.45f, 0.45f))
            : e.Role == ArcanaRole.FieldBoss ? UIKit.Colorize("旅の道のぬし", new Color(1f, 0.45f, 0.45f))
            : e.Rare ? UIKit.Colorize("レア", new Color(1f, 0.85f, 0.3f))
            : e.Role == ArcanaRole.Field ? UIKit.Colorize("旅の道", new Color(0.45f, 0.8f, 0.55f))
            : "";

        private static void DrawDexDetail(Rect r, EnemyInfo e)
        {
            bool seen = Progress.DexSeen(e.Name);
            int beaten = Progress.DexDefeated(e.Name);
            string where = DexWhere(e);
            if (!seen)
            {
                UIKit.Label(new Rect(r.x + 14, r.y + 8, r.width - 28, 40), "？？？　まだ出会っていない　" + DexKind(e), UIKit.Body);
                UIKit.Label(new Rect(r.x + 14, r.y + 56, r.width - 28, 50), $"出る場所：{where}", UIKit.Small);
                return;
            }
            UIKit.Label(new Rect(r.x + 14, r.y + 4, 520, 40), e.Name + "　" + DexKind(e), UIKit.Body);
            UIKit.Label(new Rect(r.x + 520, r.y + 8, r.width - 534, 36),
                beaten <= 0 ? "まだたおしていない（たおすと強さが記録される）"
                : e.Variant ? $"たおした回数：{beaten}　体力 {e.Hp}・こうげき {e.Attack}・金貨 {e.Coins}（もっと先の国では もっと強い）"
                : $"たおした回数：{beaten}　体力 {e.Hp}・こうげき {e.Attack}・金貨 {e.Coins}（ランク9までの強さ）", UIKit.Small);
            UIKit.Label(new Rect(r.x + 14, r.y + 46, r.width - 28, 66), $"{e.Description}\n出る場所：{where}", UIKit.Small);
        }


        /// <summary>バトルが始まった：図鑑に「見つけた」をつける。はじめてなら true</summary>
        private static bool DexOnMeet(EnemyInfo e) => e.Kind != BattleKind.Fishing && Progress.MarkDexSeen(e.DexKey);

        // ------------------------------------------------------------------
        // セーブ
        // ------------------------------------------------------------------

        private const float AutoSaveEvery = 30f; // 秒
        private float autoSaveTimer;
        private float autoSaveFlash;             // 「オートセーブしました」を出す残り時間

        /// <summary>今いる場所（遺跡のエリアの中なら、その入り口）</summary>
        private Progress.SavePoint CurrentSavePoint()
        {
            var p = new Progress.SavePoint { Valid = true, When = System.DateTime.Now.ToString("M/d HH:mm"), Village = TownData.Index };
            if (area != null && area.IsInn)
            {
                p.Inn = true;
                p.Place = "宿舎の中";
                return p;
            }
            Vector2 world = area != null ? townReturn : (Vector2)player.transform.position;
            var t = town.ToTile(world);
            p.X = t.x;
            p.Y = t.y;
            p.Place = area != null ? $"「{area.Theme.Area}」の入り口" : $"{TownData.Current.Name}の{DistrictNames[TownData.AreaOf(t.x, t.y)]}";
            return p;
        }

        /// <summary>オートセーブ（場所を覚える。持ち物などは いつも記録されている）</summary>
        private void AutoSave()
        {
            if (town == null || player == null) return;
            Progress.SetSave(false, CurrentSavePoint());
            autoSaveTimer = 0f;
            autoSaveFlash = 2f;
        }

        /// <summary>自分でセーブ（セーブボタン・宿舎の記録の本）</summary>
        private void SaveNow()
        {
            KeyInput.Clear();
            var p = CurrentSavePoint();
            Progress.SetSave(true, p);
            Progress.SetSave(false, p);
            Progress.SavedAtInn = p.Inn;
            autoSaveTimer = 0f;
            SoundKit.Chime();
            dialog.Show("セーブ", new[]
            {
                $"セーブしました！（{p.Place}・{p.When}）",
                "タイトルで「セーブした所から」を選ぶと、ここから始まるよ。持ち物やクリアの記録は、いつも自動で記録されているよ。",
            });
        }

        /// <summary>町・エリアにいるあいだ、30秒ごとにオートセーブ</summary>
        private void UpdateAutoSave()
        {
            if (autoSaveFlash > 0f) autoSaveFlash -= Time.deltaTime;
            if (State != GameState.Town && State != GameState.Area) return;
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= AutoSaveEvery) AutoSave();
        }

        /// <summary>「ゲームを終了しますか？」</summary>
        private void DrawQuitConfirm()
        {
            var box = new Rect(340, 220, 600, 260);
            UIKit.Panel(box);
            UIKit.Nav("quit", true);
            UIKit.Label(new Rect(box.x, box.y + 30, box.width, 50), "ゲームを終了して、スタート画面にもどりますか？", UIKit.Body);
            UIKit.Label(new Rect(box.x, box.y + 84, box.width, 40), "持ち物やクリアの記録と、今いる場所は自動で記録されるよ。", UIKit.SmallCenter);
            if (UIKit.ColorButton(new Rect(box.x + 50, box.y + 160, 240, 60), "終了する", new Color(0.85f, 0.35f, 0.35f), UIKit.BigButton))
                QuitGame();
            if (UIKit.ColorButton(new Rect(box.x + 310, box.y + 160, 240, 60), "やめる", Color.gray, UIKit.BigButton, primary: true))
            {
                confirmQuit = false;
                KeyInput.Clear();
            }
            UIKit.HelpBar("Esc：やめる");
        }

        /// <summary>今いる場所を記録して、スタート画面にもどる（「つづきから」で ここから始められる）</summary>
        private void QuitGame()
        {
            AutoSave();
            PlayerPrefs.Save();
            CloseMenu();
            showInventory = false;
            showMap = false;
            showDex = false;
            if (hideActive) EndHide();
            player.StopWalking();
            if (area != null)
            {
                Destroy(area.gameObject);
                area = null;
            }
            quiz = null;
            quizAttempt = 0;
            titleNote = null;
            titlePhase = TitlePhase.Start;
            State = GameState.Title;
        }

        // ------------------------------------------------------------------
        // メニュー（Esc）：セーブ・持ち物・地図
        // ------------------------------------------------------------------

        private bool showMenu;
        private int menuPage; // メニューのページ（0：セーブなど　1：操作方法・ふりがな）

        /// <summary>Esc でメニューを開く／閉じる（ほかの画面が開いていないとき）。メニューが開いていたら true</summary>
        private bool UpdateMenuKeys()
        {
            if (showMenu)
            {
                if (KeyInput.WasPressed(KeyCode.Escape))
                {
                    if (confirmQuit) { confirmQuit = false; KeyInput.Clear(); }
                    else CloseMenu();
                }
                return true;
            }
            if (!showInventory && !showMap && !showDex && KeyInput.WasPressed(KeyCode.Escape))
            {
                showMenu = true;
                menuPage = 0;
                player.StopWalking();
                KeyInput.Clear();
                return true;
            }
            return false;
        }

        private bool confirmQuit; // 「ゲームを終了しますか？」を出している

        private void CloseMenu()
        {
            showMenu = false;
            confirmQuit = false;
            KeyInput.Clear();
        }

        /// <summary>メニューの2ページ目：操作方法と、ふりがな あり／なし</summary>
        private void DrawMenuPage2(Rect p)
        {
            var help = new Rect(p.x + 30, p.y + 76, p.width - 60, 340);
            UIKit.Fill(help, new Color(0f, 0f, 0f, 0.3f));
            UIKit.Label(new Rect(help.x + 14, help.y + 8, help.width - 28, 36), UIKit.Colorize("操作方法", UIKit.WarnColor), UIKit.Body);
            UIKit.Label(new Rect(help.x + 14, help.y + 46, help.width - 28, help.height - 52),
                "移動：WASD・矢印キー・タップ\n調べる・話す：E・スペース・タップ\n持ち物・図鑑：I\n地図：M（地図をタップすると ピン）\nステージ一覧：L\nメニュー：Esc\n遺跡から町へ：下の出口", UIKit.Body);
            UIKit.Label(new Rect(p.x + 30, p.y + 432, p.width - 60, 36), UIKit.Colorize("ふりがな", UIKit.WarnColor), UIKit.Body);
            UIKit.FuriganaToggle(new Rect(p.x + 60, p.y + 470, p.width - 120, 52));
            UIKit.HelpBar("Esc：ゲームにもどる");
        }

        private void DrawMenu()
        {
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0f, 0f, 0f, 0.55f));
            if (confirmQuit) { DrawQuitConfirm(); return; }
            var p = new Rect(400, 70, 480, 590);
            UIKit.Panel(p);
            UIKit.Label(new Rect(p.x, p.y + 14, p.width, 50), UIKit.Colorize(menuPage == 0 ? "メニュー" : "メニュー（2ページ目）", UIKit.AccentColor), UIKit.Center);
            UIKit.Nav("menu" + menuPage, true);
            // ページの切りかえ（右上・左上の 小さなボタン）
            if (menuPage == 0 && UIKit.ColorButton(new Rect(p.xMax - 150, p.y + 18, 136, 40), "つぎ ▶", Color.gray, UIKit.SmallButton)) { menuPage = 1; KeyInput.Clear(); }
            if (menuPage == 1)
            {
                if (UIKit.ColorButton(new Rect(p.x + 14, p.y + 18, 136, 40), "◀ もどる", Color.gray, UIKit.SmallButton)) { menuPage = 0; KeyInput.Clear(); }
                DrawMenuPage2(p);
                return;
            }

            float y = p.y + 80;
            var save = new Rect(p.x + 60, y, p.width - 120, 60);
            if (UIKit.ColorButton(save, "", UIKit.AccentColor, UIKit.BigButton, primary: true))
            {
                CloseMenu();
                SaveNow();
            }
            UIKit.Texture(new Rect(save.x + 16, save.y + 12, 36, 36), SpriteFactory.TextureOf(TownArt.SaveIcon));
            UIKit.Label(save, "セーブする", UIKit.Center);
            var last = Progress.GetSave(true);
            UIKit.Label(new Rect(p.x, y + 62, p.width, 30), last.Valid ? $"前のセーブ：{SaveLabel(last)}" : "まだセーブしていない", UIKit.SmallCenter);

            if (UIKit.ColorButton(new Rect(p.x + 60, y + 104, p.width - 120, 56), "持ち物・図鑑", Color.gray, UIKit.BigButton))
            {
                CloseMenu();
                showInventory = true;
            }
            if (UIKit.ColorButton(new Rect(p.x + 60, y + 172, p.width - 120, 56), "地図", Color.gray, UIKit.BigButton))
            {
                CloseMenu();
                ToggleMap();
            }
            if (UIKit.ColorButton(new Rect(p.x + 60, y + 240, p.width - 120, 56), "ゲームにもどる", Color.gray, UIKit.BigButton))
                CloseMenu();
            if (UIKit.ColorButton(new Rect(p.x + 60, y + 308, p.width - 120, 56), "ゲーム終了", new Color(0.8f, 0.4f, 0.4f), UIKit.BigButton))
            {
                confirmQuit = true;
                KeyInput.Clear();
            }
            UIKit.Label(new Rect(p.x + 20, y + 376, p.width - 40, 90),
                UIKit.Colorize("持ち物やクリアの記録は いつも自動で記録されるよ。セーブすると、タイトルの「セーブした所から」で ここから始められる。", new Color(1f, 1f, 1f, 0.75f)), UIKit.SmallCenter);
            UIKit.HelpBar("矢印・Enter：えらぶ　Esc：ゲームにもどる");
        }

        /// <summary>右上：メニューの案内と、オートセーブのお知らせ</summary>
        private void DrawRecordButtons()
        {
            if (!showInventory && !showMap && !showDex && !showMenu)
                UIKit.Label(new Rect(940, 88, 170, 26), UIKit.Colorize("Esc：メニュー", new Color(1f, 1f, 1f, 0.8f)), UIKit.SmallCenter);
            if (autoSaveFlash > 0f)
                UIKit.Label(new Rect(1000, 226, 270, 30), UIKit.Colorize("オートセーブしました", new Color(1f, 1f, 1f, Mathf.Clamp01(autoSaveFlash))), UIKit.SmallCenter);
        }

        /// <summary>セーブした場所から始める（宿舎の中・町のそのマス）</summary>
        private void ContinueFrom(bool manual)
        {
            var p = Progress.GetSave(manual);
            if (!p.Valid && !manual) p = Progress.GetSave(true); // オートセーブがまだない古いデータ
            ResetRunState();
            CreateQuestScreen();
            if (p.Valid) Progress.Village = p.Village; // セーブした村から
            BuildTown();
            StartGame();
            if (!p.Valid || p.Village != TownData.Index) return;
            if (p.Inn)
            {
                if (town.Repaired(InnBuilding)) EnterInn();
                return;
            }
            var world = town.ToWorld(p.X, p.Y);
            if (p.X < 0 || p.Y < 0 || p.X >= TownData.W || p.Y >= TownData.H || !town.IsWalkable(world)) return;
            player.transform.position = world;
            lastTile = town.ToTile(world);
            FollowCamera(true);
        }

        private static string SaveLabel(Progress.SavePoint p) =>
            p.Valid ? (string.IsNullOrEmpty(p.When) ? p.Place : $"{p.Place}・{p.When}") : "";
    }
}
