using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：宿舎の中。
    ///
    /// 宿舎の入口 → 読みこみ → 宿舎の中の部屋（AreaMap.BuildInn）
    /// ・ベッド：休む（体力が全回復）
    /// ・記録の本：セーブ（次の「つづきから」は宿舎の中から始まる）
    /// ・鏡・クローゼット：身じたく（キャラクリ。風船割りでもらった服・ぼうしも着られる）
    /// ・宿舎のおかみ：お話
    /// ・出口：町（宿舎の前）へもどる
    /// </summary>
    public partial class GameManager
    {
        private static BuildingInfo InnBuilding => TownData.Buildings[0];

        /// <summary>宿舎の中に入る（読みこみ画面のあとに呼ぶ）</summary>
        private void EnterInn()
        {
            if (area != null) Destroy(area.gameObject);
            area = new GameObject("Inn").AddComponent<AreaMap>();
            area.BuildInn(AreaOrigin);
            areaPin = null;
            townReturn = town.DoorWorld(InnBuilding);
            player.SetMap(area, area.ExitWorld + Vector2.up);
            lastTile = area.ToTile(player.transform.position);
            State = GameState.Area;
            KeyInput.Clear();
            FollowCamera(true);
            ShowToast("宿舎の中。ベッドで休む・本でセーブ・鏡で身じたく ができるよ。", 4f);
            AutoSave();
        }

        /// <summary>宿舎の中で、その文字のもの（ベッドなど）の近くにいるか</summary>
        private bool NearInn(Vector2 p, string chars)
        {
            var lay = area.Layout;
            for (int y = 0; y < lay.Height; y++)
                for (int x = 0; x < lay.Width; x++)
                    if (chars.IndexOf(lay.Grid[x, y]) >= 0 && Vector2.Distance(p, area.ToWorld(x, y)) < 1.3f) return true;
            return false;
        }

        /// <summary>宿舎の中の、その文字のものの場所（いちばん左上のもの）</summary>
        private Vector2 InnSpot(char c)
        {
            var lay = area.Layout;
            for (int y = 0; y < lay.Height; y++)
                for (int x = 0; x < lay.Width; x++)
                    if (lay.Grid[x, y] == c) return area.ToWorld(x, y);
            return area.ExitWorld;
        }

        private void UpdateInn()
        {
            Vector2 p = player.transform.position;
            if (KeyInput.InteractPressed()) TryInteractInn(p);
            var tile = area.ToTile(p);
            if (tile != lastTile)
            {
                lastTile = tile;
                OnStep(false);
            }
        }

        /// <summary>近くのものを調べる。調べたら true</summary>
        private bool TryInteractInn(Vector2 p)
        {
            KeyInput.Clear();
            if (Vector2.Distance(p, area.ExitWorld) < 1.1f)
            {
                StartLoading("町へ もどっている…", null, LeaveArea);
                return true;
            }
            if (NearInn(p, "b"))
            {
                PlayerStats.FullHeal();
                dialog.Show("ベッド", "ふかふかのベッドで休んだ。……ぐっすり眠った！ 体力が全回復した。");
                return true;
            }
            if (NearInn(p, "k"))
            {
                SaveNow(); // 記録の本：宿舎の中でセーブ
                return true;
            }
            if (NearInn(p, "md"))
            {
                dialog.Show("鏡", "鏡の前で、身じたくをととのえよう。", () => OpenCharacterCreate(false, true));
                return true;
            }
            if (NearInn(p, "n"))
            {
                var lines = new List<string>
                {
                    "いらっしゃい！ ベッドで休むと、体力が全回復するよ。",
                    "机の本に記録しておけば、次は宿舎から始められるからね。",
                };
                lines.Add(Progress.TotalBalloons > 0
                    ? $"風船割りで もらった服やぼうしは、鏡の前で着がえられるよ。（これまでに割った風船：{Progress.TotalBalloons}こ）"
                    : "南東の風船割りで遊ぶと、かわいい服やぼうしがもらえるらしいよ。");
                dialog.Show("宿舎のおかみ", lines.ToArray());
                return true;
            }
            return false;
        }

        /// <summary>宿舎の中をタップ：もの → となりまで歩いて調べる、出口 → 歩いて出る、ほか → そこまで歩く</summary>
        private void HandleInnClick(Vector2 guiPos)
        {
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
                if (Vector2.Distance(p, area.ExitWorld) < 1.1f) TryInteractInn(p);
                else WalkThen(area.FindPath(p, area.ExitWorld), () => TryInteractInn(player.transform.position));
                return;
            }
            if ("bkmdn".IndexOf(c) >= 0)
            {
                var at = area.ToWorld(t.x, t.y);
                if (Vector2.Distance(p, at) < 1.3f) { TryInteractInn(p); return; }
                WalkThen(WalkPath.NextTo(area, p, at), () => TryInteractInn(player.transform.position));
                return;
            }
            var path = area.FindPath(p, new Vector2(Mathf.Round(world.x), Mathf.Round(world.y)));
            if (path != null) player.WalkTo(path);
        }

        private void DrawInnHud()
        {
            UIKit.Nav(showInventory ? "inventory" : "inn", showInventory);
            DrawStatusBox("宿舎：ベッドで休む・本でセーブ・鏡で身じたく");
            DrawPocketButton();
            DrawMapButton();
            DrawRecordButtons();

            var title = new Rect(530, 10, 420, 44);
            UIKit.Fill(title, new Color(0f, 0f, 0f, 0.6f));
            UIKit.Label(title, "宿舎の中", UIKit.Center);

            DrawWorldLabel(InnSpot('b') + Vector2.up * 0.9f, "ベッド（休む）");
            DrawWorldLabel(InnSpot('k') + Vector2.up * 0.9f, "記録の本（セーブ）");
            DrawWorldLabel(InnSpot('m') + Vector2.up * 0.9f, "鏡（身じたく）");
            DrawWorldLabel(InnSpot('n') + Vector2.up * 0.9f, "宿舎のおかみ");
            DrawWorldLabel(area.ExitWorld + Vector2.up * 0.9f, "出口（町へ）");

            if (!dialog.Active && !showInventory && !showMap)
            {
                Vector2 p = player.transform.position;
                string prompt = Vector2.Distance(p, area.ExitWorld) < 1.1f ? "E・スペース：町へもどる"
                    : NearInn(p, "b") ? "E・スペース：ベッドで休む"
                    : NearInn(p, "k") ? "E・スペース：記録の本にセーブする"
                    : NearInn(p, "md") ? "E・スペース：鏡で身じたくをする"
                    : NearInn(p, "n") ? "E・スペース：宿舎のおかみと話す"
                    : null;
                if (prompt != null)
                {
                    var r = new Rect(390, 420, 500, 50);
                    UIKit.Panel(r);
                    UIKit.Label(r, prompt, UIKit.Center);
                }
            }

            if (toastTimer > 0f && !string.IsNullOrEmpty(toast) && !dialog.Active)
            {
                var t = new Rect(160, 586, 960, 88);
                UIKit.Fill(t, new Color(0.1f, 0.2f, 0.35f, 0.9f));
                UIKit.Label(t, toast, UIKit.Center);
            }

            DrawPinIndicator();
            UIKit.HelpBar("移動: WASD・矢印・タップ　調べる: E・スペース・タップ　持ち物: I　地図: M　下の出口: 町へもどる");
            if (showInventory) DrawInventory();
            if (showMap) DrawMap();
            if (showDex) DrawDex();
            if (showMenu) DrawMenu();
        }
    }
}
