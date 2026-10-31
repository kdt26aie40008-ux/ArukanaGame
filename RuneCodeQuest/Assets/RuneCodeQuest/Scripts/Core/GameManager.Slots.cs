using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：タイトルのセーブデータえらび（最大5こ）。
    /// ・はじめから → 保存する場所をえらぶ →（使っている場所なら 消してよいか聞く）→ 名前をきめる → キャラクターづくり
    /// ・つづきから → セーブデータをえらぶ → つづきから／セーブした所から／キャラクターを作りなおす／消す
    /// </summary>
    public partial class GameManager
    {
        private readonly SaveSlots.Info[] slotInfos = new SaveSlots.Info[SaveSlots.Max + 1];
        private int pickedSlot = 1;          // はじめから：えらんだ場所
        private string slotNameEdit = "";    // はじめから：入力中のセーブデータの名前
        private bool focusSlotName;          // 名前の入力らんに、はじめにカーソルを置く

        /// <summary>一覧の中身を読みなおす（一覧を開くとき）</summary>
        private void RefreshSlots()
        {
            for (int i = 1; i <= SaveSlots.Max; i++) slotInfos[i] = SaveSlots.Read(i);
        }

        private void GoTitlePhase(TitlePhase phase)
        {
            if (phase == TitlePhase.SlotLoad || phase == TitlePhase.SlotNew) RefreshSlots();
            titlePhase = phase;
            KeyInput.Clear();
        }

        /// <summary>タイトルのメニュー：はじめから・つづきから</summary>
        private void DrawTitleMenu()
        {
            bool any = SaveSlots.AnyExists();
            if (UIKit.ColorButton(new Rect(440, 270, 400, 76), "はじめから", any ? Color.gray : UIKit.AccentColor, UIKit.BigButton, primary: !any))
            {
                titleNote = null;
                GoTitlePhase(TitlePhase.SlotNew);
            }
            if (UIKit.ColorButton(new Rect(440, 370, 400, 76), "つづきから", any ? UIKit.AccentColor : Color.gray, UIKit.BigButton, enabled: any, primary: any))
            {
                titleNote = null;
                GoTitlePhase(TitlePhase.SlotLoad);
            }
            if (!any) UIKit.Label(new Rect(0, 452, UIKit.RefWidth, 30), "（まだセーブデータがありません）", UIKit.SmallCenter);
            if (titleNote != null) UIKit.Label(new Rect(0, 500, UIKit.RefWidth, 40), UIKit.Colorize(titleNote, UIKit.WarnColor), UIKit.Center);
            UIKit.HelpBar($"はじめから：新しいセーブデータを作る（{SaveSlots.Max}こまで）　つづきから：セーブデータをえらんで続きを遊ぶ");
        }

        /// <summary>セーブデータの一覧（forNew なら はじめから＝保存する場所えらび、でなければ つづきから）</summary>
        private void DrawSlotList(bool forNew)
        {
            UIKit.Label(new Rect(0, 206, UIKit.RefWidth, 40),
                UIKit.Colorize(forNew ? "はじめから：保存する場所をえらんでね" : "つづきから：遊ぶセーブデータをえらんでね", UIKit.AccentColor), UIKit.Center);
            for (int i = 1; i <= SaveSlots.Max; i++)
            {
                var info = slotInfos[i];
                var row = new Rect(170, 250 + (i - 1) * 72, 940, 64);
                bool enabled = forNew || info.Exists;
                Color color = info.Exists ? new Color(0.25f, 0.32f, 0.5f) : new Color(0.22f, 0.24f, 0.3f);
                if (UIKit.ColorButton(row, "", color, UIKit.Button, enabled, primary: i == 1))
                    PickSlot(i, forNew);
                UIKit.Label(new Rect(row.x + 16, row.y, 110, row.height), UIKit.Colorize($"データ{i}", UIKit.WarnColor), UIKit.Body);
                if (info.Exists)
                {
                    UIKit.Label(new Rect(row.x + 120, row.y + 2, 380, 34), info.Name, UIKit.Body);
                    UIKit.Label(new Rect(row.x + 120, row.y + 32, 400, 30), $"{info.Hero}　ランク {info.Rank}　クリア {info.Clears} / {QuestDatabase.LevelCount}", UIKit.Small);
                    UIKit.Label(new Rect(row.x + 540, row.y, 390, row.height), string.IsNullOrEmpty(info.Place) ? "" : $"前回：{info.Place}", UIKit.Small);
                }
                else UIKit.Label(new Rect(row.x + 120, row.y, 500, row.height), forNew ? "（空き）ここに作る" : "（空き）", UIKit.Body);
            }
            if (UIKit.ColorButton(new Rect(60, 620, 180, 48), "もどる", Color.gray, UIKit.SmallButton))
                GoTitlePhase(TitlePhase.Menu);
            UIKit.HelpBar(forNew ? "使っている場所をえらぶと、そのデータを消して新しく作るよ　Esc：もどる" : "Esc：もどる");
        }

        private void PickSlot(int slot, bool forNew)
        {
            titleNote = null;
            if (forNew)
            {
                pickedSlot = slot;
                SaveSlots.Current = slot; // 「消しますか？」で、そのデータの記録を見せるため
                if (slotInfos[slot].Exists) GoTitlePhase(TitlePhase.ConfirmOverwrite);
                else OpenSlotName();
                return;
            }
            SaveSlots.Current = slot;
            GoTitlePhase(TitlePhase.SlotDetail);
        }

        private void OpenSlotName()
        {
            slotNameEdit = $"セーブデータ{pickedSlot}";
            focusSlotName = true;
            GoTitlePhase(TitlePhase.SlotName);
        }

        /// <summary>はじめから：セーブデータの名前をきめる</summary>
        private void DrawSlotName()
        {
            var box = new Rect(290, 220, 700, 330);
            UIKit.Panel(box, UIKit.SubPanelColor);
            UIKit.Label(new Rect(box.x, box.y + 20, box.width, 44), $"データ{pickedSlot} に新しく作る：セーブデータの名前をきめよう", UIKit.Center);

            // 日本語入力（IME）が使える入力らん
            slotNameEdit = UIKit.ImeTextField(new Rect(box.x + 100, box.y + 90, 500, 60), slotNameEdit, UIKit.InputField, "SlotName", SaveSlots.MaxNameLength);
            if (focusSlotName)
            {
                GUI.FocusControl("SlotName");
                focusSlotName = false;
            }
            UIKit.Label(new Rect(box.x + 100, box.y + 154, 500, 30), $"{SaveSlots.MaxNameLength}文字まで（日本語もOK：半角/全角キーで切りかえ。入力がおわったら Enter）", UIKit.Small);

            if (UIKit.ColorButton(new Rect(box.x + 370, box.y + 230, 270, 64), "これでスタート！", UIKit.AccentColor, UIKit.BigButton, primary: true))
                BeginNewGameInSlot();
            if (UIKit.ColorButton(new Rect(box.x + 60, box.y + 236, 200, 52), "もどる", Color.gray, UIKit.BigButton))
            {
                GUIUtility.keyboardControl = 0;
                GoTitlePhase(TitlePhase.SlotNew);
            }
            UIKit.HelpBar("名前は あとから見分けるためのもの（キャラクターの名前は このあと決めるよ）");
        }

        /// <summary>えらんだ場所を まっさらにして、名前をつけて、キャラクターづくりへ</summary>
        private void BeginNewGameInSlot()
        {
            GUIUtility.keyboardControl = 0;
            SaveSlots.Current = pickedSlot;
            ResetSave();
            SaveSlots.SetName(pickedSlot, slotNameEdit);
            ResetRunState();
            OpenCharacterCreate(false);
        }

        /// <summary>つづきから：えらんだセーブデータで何をするか</summary>
        private void DrawSlotDetail()
        {
            int slot = SaveSlots.Current;
            var info = new Rect(240, 220, 800, 96);
            UIKit.Panel(info, UIKit.SubPanelColor);
            UIKit.Label(new Rect(info.x, info.y + 6, info.width, 40), $"{UIKit.Colorize($"データ{slot}", UIKit.WarnColor)}　{SaveSlots.Name(slot)}", UIKit.Center);
            UIKit.Label(new Rect(info.x, info.y + 48, info.width, 40),
                $"{Progress.Look.Name}　ランク {PlayerStats.Rank}　クリア {Progress.ClearedCount} / {QuestDatabase.LevelCount}", UIKit.Center);

            var auto = Progress.GetSave(false);
            var manual = Progress.GetSave(true);
            if (UIKit.ColorButton(new Rect(340, 336, 340, 60), "つづきから", UIKit.AccentColor, UIKit.BigButton, primary: true))
                ContinueFrom(false);
            UIKit.Label(new Rect(690, 346, 560, 40), auto.Valid ? $"オートセーブ：{SaveLabel(auto)}" : "", UIKit.Small);
            if (UIKit.ColorButton(new Rect(340, 406, 340, 60), "セーブした所から", manual.Valid ? UIKit.AccentColor : Color.gray, UIKit.BigButton, enabled: manual.Valid))
                ContinueFrom(true);
            UIKit.Label(new Rect(690, 416, 560, 40), manual.Valid ? $"セーブ：{SaveLabel(manual)}" : "（まだセーブしていない）", UIKit.Small);

            if (UIKit.ColorButton(new Rect(60, 620, 180, 48), "もどる", Color.gray, UIKit.SmallButton))
                GoTitlePhase(TitlePhase.SlotLoad);
            if (UIKit.ColorButton(new Rect(400, 620, 280, 48), "キャラクターを作りなおす", Color.gray, UIKit.SmallButton))
                OpenCharacterCreate(true);
            if (UIKit.ColorButton(new Rect(1000, 620, 220, 48), "このデータを消す", new Color(0.8f, 0.4f, 0.4f), UIKit.SmallButton))
            {
                titleNote = null;
                GoTitlePhase(TitlePhase.ConfirmDelete);
            }
            if (titleNote != null) UIKit.Label(new Rect(0, 480, UIKit.RefWidth, 40), UIKit.Colorize(titleNote, UIKit.WarnColor), UIKit.Center);
            UIKit.HelpBar("つづきから：オートセーブの所から　セーブした所から：メニューでセーブした所から　Esc：もどる");
        }

        /// <summary>このデータを消す</summary>
        private void DeleteCurrentSlot()
        {
            int slot = SaveSlots.Current;
            string name = SaveSlots.Name(slot);
            SaveSlots.Delete(slot);
            ResetRunState();
            BuildTown();
            titleNote = $"「{name}」を消しました。";
            GoTitlePhase(SaveSlots.AnyExists() ? TitlePhase.SlotLoad : TitlePhase.Menu);
        }

        /// <summary>タイトルで Esc：1つ前の画面へ</summary>
        private void HandleTitleBack()
        {
            var ev = Event.current;
            if (ev.type != EventType.KeyDown || ev.keyCode != KeyCode.Escape || GUIUtility.keyboardControl != 0) return;
            TitlePhase back;
            switch (titlePhase)
            {
                case TitlePhase.SlotLoad:
                case TitlePhase.SlotNew: back = TitlePhase.Menu; break;
                case TitlePhase.SlotDetail: back = TitlePhase.SlotLoad; break;
                case TitlePhase.SlotName:
                case TitlePhase.ConfirmOverwrite: back = TitlePhase.SlotNew; break;
                case TitlePhase.ConfirmDelete: back = TitlePhase.SlotDetail; break;
                default: return;
            }
            ev.Use();
            GoTitlePhase(back);
        }

        /// <summary>セーブデータを切りかえたときに、前のデータの遊びの途中の状態を消す</summary>
        private void ResetRunState()
        {
            townPin = null;
            areaPin = null;
            if (hideActive) EndHide();
            hideActive = false;
            quiz = null;
            quizAttempt = 0;
        }
    }
}
