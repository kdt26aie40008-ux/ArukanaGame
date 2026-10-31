using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// GameManager の続き：キャラクリ（自分のキャラクターを作る画面）と、持ち物リスト（ポケット）。
    /// </summary>
    public partial class GameManager
    {
        private const int MaxNameLength = 10;

        private CharacterLook editLook;     // 作っている途中の見た目
        private int editLookMode;           // 見た目の作り方（イラスト・パーツきせかえ・ドットきせかえ）
        private bool charFromMenu;          // タイトルのメニューから来たか（はじめからのときは false）
        private bool charFromInn;           // 宿舎の「身じたく」から来たか（名前は変えられない）
        private bool showInventory;         // 持ち物リストを開いているか

        // ------------------------------------------------------------------
        // キャラクリ
        // ------------------------------------------------------------------

        private void OpenCharacterCreate(bool fromMenu, bool fromInn = false)
        {
            editLook = CharacterLook.Decode(Progress.Look.Encode());
            editLookMode = Progress.LookMode;
            CharacterParts.Clamp(editLook);
            charFromMenu = fromMenu;
            charFromInn = fromInn;
            State = GameState.CharacterCreate;
            KeyInput.Clear();
        }

        private void FinishCharacterCreate()
        {
            string name = (editLook.Name ?? "").Trim();
            if (name.Length == 0) name = "アル";
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            editLook.Name = name;
            CharacterArt.RemoveLocked(editLook); // まだもらっていない服・ぼうしは着られない
            Progress.Look = editLook;
            Progress.LookMode = editLookMode;
            GUIUtility.keyboardControl = 0;

            if (charFromInn)
            {
                // 宿舎の身じたく：宿舎の中にもどって、見た目だけ変える
                player.RefreshLook();
                State = GameState.Area;
                KeyInput.Clear();
                dialog.Show("鏡", "身じたくをととのえた！ よく にあっているよ。");
                return;
            }

            // 新しい見た目でマップを作りなおす
            BuildTown();
            State = GameState.Title;
            if (charFromMenu)
            {
                titlePhase = TitlePhase.SlotDetail;
                titleNote = $"{name} の見た目を保存しました。";
            }
            else
            {
                prologuePage = 0;
                titlePhase = TitlePhase.Prologue;
            }
        }

        private void DrawCharacterCreate()
        {
            UIKit.Nav("char", true);
            UIKit.Fill(new Rect(0, 0, UIKit.RefWidth, UIKit.RefHeight), new Color(0.05f, 0.06f, 0.11f, 1f));
            UIKit.Label(new Rect(0, 18, UIKit.RefWidth, 56), charFromInn ? "身じたく" : "キャラクターをつくろう", UIKit.Title);

            // 見本（大きく表示）。まだもらっていない服・ぼうしは、ふつうのもので見せる
            var shown = CharacterLook.Decode(editLook.Encode());
            CharacterArt.RemoveLocked(shown);
            var preview = new Rect(110, 100, 320, 420);
            UIKit.Panel(preview, UIKit.SubPanelColor);
            // 見本の絵（二回り大きく：280）
            UIKit.Fill(new Rect(preview.x + 10, preview.y + 10, 300, 300), new Color(0.25f, 0.45f, 0.3f));
            UIKit.Texture(new Rect(preview.x + 20, preview.y + 20, 280, 280), SpriteFactory.TextureOf(SpriteFactory.LookSprite(shown, editLookMode)));
            // 見た目の作り方の切りかえ：イラスト → パーツきせかえ → ドットきせかえ（画像がないものは とばす）
            if (UIKit.ColorButton(new Rect(preview.x + 20, preview.yMax + 10, preview.width - 40, 44), $"見た目：{LookModeName(editLookMode)}（切りかえ）", UIKit.AccentColor, UIKit.SmallButton))
                editLookMode = NextLookMode(editLookMode);
            string shownName = string.IsNullOrWhiteSpace(editLook.Name) ? "（なまえ）" : editLook.Name;
            UIKit.Label(new Rect(preview.x, preview.y + 308, preview.width, 44), shownName, UIKit.Center);
            UIKit.Label(new Rect(preview.x + 20, preview.y + 350, preview.width - 40, 64),
                $"見習い魔法使い\n割った風船：{Progress.TotalBalloons}こ", UIKit.SmallCenter);

            // なまえ（宿舎の身じたくでは変えない）
            if (charFromInn)
            {
                UIKit.Label(new Rect(480, 100, 700, 56), $"なまえ：{editLook.Name}　（なまえはタイトルの「キャラクターを作りなおす」で変えられる）", UIKit.Body);
            }
            else
            {
                UIKit.Label(new Rect(480, 100, 150, 56), "なまえ", UIKit.Body);
                // 日本語入力（IME）が使える入力らん
                editLook.Name = UIKit.ImeTextField(new Rect(640, 102, 400, 54), editLook.Name, UIKit.InputField, "CharName", MaxNameLength);
                if (UIKit.ColorButton(new Rect(1050, 104, 120, 50), "入力", Color.gray))
                    GUI.FocusControl("CharName");
                UIKit.Label(new Rect(640, 156, 400, 28), $"{MaxNameLength}文字まで（日本語もOK。入力がおわったら Enter）", UIKit.Small);
            }

            // 見た目のえらびかた（見た目の作り方ごとに ちがう）
            if (editLookMode == Progress.LookIllust)
            {
                UIKit.Label(new Rect(470, 220, 760, 120), "主人公のイラストを使います。\n左の「見た目」ボタンで「パーツきせかえ」や「ドットきせかえ」にすると、自分で見た目をえらべます。", UIKit.Body);
                DrawCharButtons();
                return;
            }
            if (editLookMode == Progress.LookParts)
            {
                DrawPartsRows();
                DrawCharButtons();
                return;
            }

            // ドットきせかえ（左：顔とかみ　右：ふくと小物）
            float y = 200;
            Row(470, ref y, "かみがた", CharacterArt.HairStyleNames[editLook.HairStyle], null, d => editLook.HairStyle += d);
            Row(470, ref y, "かみの色", CharacterArt.HairColors[editLook.Hair].name, CharacterArt.HairColors[editLook.Hair].color, d => editLook.Hair += d);
            Row(470, ref y, "目", CharacterArt.EyeNames[editLook.Eyes], null, d => editLook.Eyes += d);
            Row(470, ref y, "目の色", CharacterArt.EyeColors[editLook.EyeColor].name, CharacterArt.EyeColors[editLook.EyeColor].color, d => editLook.EyeColor += d);
            Row(470, ref y, "はだの色", CharacterArt.SkinColors[editLook.Skin].name, CharacterArt.SkinColors[editLook.Skin].color, d => editLook.Skin += d);
            y = 200;
            Row(870, ref y, "ふく", RewardLabel(false, editLook.Outfit, CharacterArt.OutfitNames[editLook.Outfit]), null, d => editLook.Outfit += d);
            Row(870, ref y, "ふくの色", CharacterArt.ClothColors[editLook.Cloth].name, CharacterArt.ClothColors[editLook.Cloth].color, d => editLook.Cloth += d);
            Row(870, ref y, "ぼうし", RewardLabel(true, editLook.Hat, CharacterArt.HatNames[editLook.Hat]), null, d => editLook.Hat += d);
            Row(870, ref y, "小物", CharacterArt.AccessoryNames[editLook.Accessory], null, d => editLook.Accessory += d);
            editLook.Clamp();
            if (!CharacterArt.HatUnlocked(editLook.Hat) || !CharacterArt.OutfitUnlocked(editLook.Outfit))
                UIKit.Label(new Rect(870, y, 400, 56), UIKit.Colorize("？ は、南東の風船割りで風船を割るともらえるよ。", UIKit.WarnColor), UIKit.Small);

            if (UIKit.ColorButton(new Rect(470, 548, 220, 56), "おまかせ", Color.gray, UIKit.BigButton))
            {
                editLook.HairStyle = Random.Range(0, CharacterArt.HairStyleNames.Length);
                editLook.Hair = Random.Range(0, CharacterArt.HairColors.Length);
                editLook.Cloth = Random.Range(0, CharacterArt.ClothColors.Length);
                editLook.Skin = Random.Range(0, CharacterArt.SkinColors.Length);
                editLook.Hat = Random.Range(0, CharacterArt.HatNames.Length);
                editLook.Eyes = Random.Range(0, CharacterArt.EyeNames.Length);
                editLook.EyeColor = Random.Range(0, CharacterArt.EyeColors.Length);
                editLook.Outfit = Random.Range(0, CharacterArt.OutfitNames.Length);
                editLook.Accessory = Random.Range(0, CharacterArt.AccessoryNames.Length);
                CharacterArt.RemoveLocked(editLook);
            }
            DrawCharButtons();
        }

        /// <summary>見た目の作り方の名前</summary>
        private static string LookModeName(int mode) =>
            mode == Progress.LookIllust ? "イラスト" : mode == Progress.LookParts ? "パーツきせかえ" : "ドットきせかえ";

        /// <summary>次の見た目の作り方（画像がない作り方は とばす）</summary>
        private static int NextLookMode(int mode)
        {
            for (int k = 1; k <= 3; k++)
            {
                int m = (mode + k) % 3;
                if (m == Progress.LookIllust && ArtOverrides.Character("主人公") == null) continue;
                if (m == Progress.LookParts && !CharacterParts.Available) continue;
                return m;
            }
            return Progress.LookDot;
        }

        /// <summary>パーツきせかえ：性別と、部品の番号をえらぶ</summary>
        private void DrawPartsRows()
        {
            int g = editLook.Gender;
            string Num(int v, string cat) => $"{v + 1} / {CharacterParts.Count(g, cat)}";
            float y = 200;
            Row(470, ref y, "せいべつ", CharacterParts.Genders[g], null, d => { editLook.Gender = 1 - editLook.Gender; });
            Row(470, ref y, "かみがた", Num(editLook.PHair, "髪"), null, d => editLook.PHair += d);
            Row(470, ref y, "目", Num(editLook.PEyes, "目"), null, d => editLook.PEyes += d);
            Row(470, ref y, "口", Num(editLook.PMouth, "口"), null, d => editLook.PMouth += d);
            Row(470, ref y, "鼻", Num(editLook.PNose, "鼻"), null, d => editLook.PNose += d);
            y = 200;
            // コーデ（全身の服）をえらぶと、ふく・ズボン・くつは まとめて きがえる
            if (CharacterParts.Count(g, "全身") > 0)
                Row(870, ref y, CharacterParts.SeparateClothes(g) ? "コーデ" : "ふく", editLook.POutfit == 0 ? "なし" : $"{editLook.POutfit} / {CharacterParts.Count(g, "全身")}", null, d => editLook.POutfit += d);
            if (editLook.POutfit == 0 && CharacterParts.SeparateClothes(g))
            {
                Row(870, ref y, "ふく", Num(editLook.PTop, "服"), null, d => editLook.PTop += d);
                Row(870, ref y, g == 0 ? "ズボン" : "スカート", Num(editLook.PBottom, "下"), null, d => editLook.PBottom += d);
                Row(870, ref y, "くつ", Num(editLook.PShoes, "靴"), null, d => editLook.PShoes += d);
            }
            if (CharacterParts.Count(g, "帽子") > 0)
                Row(870, ref y, "ぼうし", editLook.PHat == 0 ? "なし" : $"{editLook.PHat} / {CharacterParts.Count(g, "帽子")}", null, d => editLook.PHat += d);
            CharacterParts.Clamp(editLook);
            if (UIKit.ColorButton(new Rect(470, 548, 220, 56), "おまかせ", Color.gray, UIKit.BigButton))
                CharacterParts.Randomize(editLook);
        }

        /// <summary>「これで決定！」と「やめる」</summary>
        private void DrawCharButtons()
        {
            if (UIKit.ColorButton(new Rect(900, 600, 320, 66), "これで決定！", UIKit.AccentColor, UIKit.BigButton, primary: true))
                FinishCharacterCreate();
            if ((charFromMenu || charFromInn) && UIKit.ColorButton(new Rect(110, 600, 200, 52), "やめる", Color.gray, UIKit.BigButton))
            {
                GUIUtility.keyboardControl = 0;
                if (charFromInn) State = GameState.Area;
                else
                {
                    State = GameState.Title;
                    titlePhase = TitlePhase.SlotDetail;
                }
            }

            UIKit.HelpBar("◀ ▶ で見た目をえらぶ　なまえは「入力」を押してキーボードで（日本語もOK）　矢印キー＋Enter でも操作できる");
        }

        /// <summary>風船割りのほうびの服・ぼうしは、もらうまで「？（あと○こ）」と出す</summary>
        private static string RewardLabel(bool isHat, int index, string name)
        {
            int need = Balloon.NeedFor(isHat, index);
            if (need == 0 || Progress.TotalBalloons >= need) return name;
            return $"？（風船あと{need - Progress.TotalBalloons}こ）";
        }

        /// <summary>「◀ 値 ▶」の1行</summary>
        private void Row(float x, ref float y, string label, string value, Color32? swatch, System.Action<int> change)
        {
            UIKit.Label(new Rect(x, y + 6, 110, 50), label, UIKit.Body);
            if (UIKit.ColorButton(new Rect(x + 110, y, 52, 52), "◀", Color.gray, UIKit.BigButton)) change(-1);
            var box = new Rect(x + 168, y, 180, 52);
            UIKit.Fill(box, UIKit.SubPanelColor);
            if (swatch.HasValue)
            {
                UIKit.Fill(new Rect(box.x + 8, box.y + 12, 28, 28), Color.white);
                UIKit.Fill(new Rect(box.x + 10, box.y + 14, 24, 24), swatch.Value);
            }
            UIKit.Label(swatch.HasValue ? new Rect(box.x + 30, box.y, box.width - 30, box.height) : box, value, UIKit.Center);
            if (UIKit.ColorButton(new Rect(x + 354, y, 52, 52), "▶", Color.gray, UIKit.BigButton)) change(+1);
            y += 66;
        }

        // ------------------------------------------------------------------
        // 持ち物リスト（ポケット）
        // ------------------------------------------------------------------

        /// <summary>町の右上のポケットのボタン</summary>
        private void DrawPocketButton()
        {
            var r = new Rect(1030, 8, 60, 52);
            if (UIKit.ColorButton(r, "", showInventory ? UIKit.AccentColor : Color.gray, UIKit.SmallButton))
            {
                showInventory = !showInventory;
                KeyInput.Clear();
            }
            UIKit.Texture(new Rect(r.x + 12, r.y + 8, 36, 36), SpriteFactory.TextureOf(QuestArt.Pocket));
            UIKit.Label(new Rect(r.x - 10, r.yMax, r.width + 20, 26), "持ち物", UIKit.SmallCenter);
        }
    }
}
