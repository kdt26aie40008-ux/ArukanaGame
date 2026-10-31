using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// スクラッチ風のブロックエディタ。
    ///
    /// 操作（マウス）
    ///   ・左の「ブロック置き場」からブロックをドラッグして、右の「▶起動したとき」の下につなげる
    ///   ・置いたブロックはドラッグで動かせる（中に入っているブロックもいっしょに動く）
    ///   ・ブロック置き場の上（または外）に落とすと消える
    ///   ・「くり返す」の − ＋ で回数を変える／「もし」の〈 〉をクリックすると調べることが変わる
    ///
    /// ブロックは Node（プログラムの部品）そのものなので、Python との変換や実行はそのまま行えます。
    /// </summary>
    public class ScratchEditor
    {
        // スクラッチの色
        public static readonly Color MotionColor = new Color(0.30f, 0.59f, 1.00f);
        public static readonly Color MagicColor = new Color(0.60f, 0.40f, 1.00f);
        public static readonly Color ControlColor = new Color(1.00f, 0.67f, 0.10f);
        public static readonly Color SensingColor = new Color(0.36f, 0.69f, 0.84f);
        public static readonly Color EventColor = new Color(1.00f, 0.75f, 0.00f);
        public static readonly Color VariableColor = new Color(1.00f, 0.50f, 0.20f);
        public static readonly Color FunctionColor = new Color(1.00f, 0.40f, 0.52f); // スクラッチの「ブロック定義」の色

        private const float RowH = 36f, HeaderH = 40f, FooterH = 16f, EmptyBodyH = 28f, ElseH = 30f, Indent = 18f;
        private const float CmdW = 150f, AssignW = 310f, CWidth = 290f, HatH = 44f;
        private const float AssignH = 42f;       // 変数のブロックは ふりがなの分 少し太く
        private const float PaletteW = 240f;     // 左の置き場の幅（変数のブロックの長い文字が入るように）
        private const float PaletteBlockH = 31f; // 置き場のブロックの高さ（文字が入りきらないときは のばす）
        private const float Gap = 4f;          // ならんだブロックとブロックのすき間（つなぎ目を見やすく）
        private const float OutlineW = 2f;     // ブロックのまわりの黒いふちの太さ
        public static readonly Color OutlineColor = new Color(0.08f, 0.08f, 0.13f, 1f);

        private class Item
        {
            public Node Node;
            public Rect Full, Header, Footer, ElseBar, LeftBar;
        }

        private struct Slot
        {
            public List<Node> List;
            public int Index;
            public Vector2 Pos;
        }

        public Node Highlight;   // 実行中のブロック（黄色く光る）
        public Node Failed;      // 失敗したブロック（赤く光る）

        private Vector2 scroll;
        private Node dragging;
        private Vector2 dragPos;      // マウスの位置（1280x720 座標）
        private Vector2 grabOffset;   // ブロックの左上からマウスまでのずれ
        private int controlId;

        private readonly List<Item> items = new List<Item>();
        private readonly List<Slot> slots = new List<Slot>();

        /// <summary>エディタ全体を描く。editable が false なら見るだけ（実行中など）</summary>
        public void Draw(Rect area, Level level, List<Node> script, bool editable)
        {
            controlId = GUIUtility.GetControlID(FocusType.Passive);
            currentLevel = level;
            currentScript = script;
            var e = Event.current;
            float s = UIKit.Scale;

            UIKit.Fill(area, UIKit.SubPanelColor);
            var palette = new Rect(area.x, area.y, PaletteW, area.height);
            var view = new Rect(area.x + PaletteW + 6, area.y + 32, area.width - PaletteW - 6, area.height - 32);
            lastView = view;

            if (editable) DrawPalette(palette, level);
            else
            {
                UIKit.Fill(palette, new Color(0.12f, 0.13f, 0.19f));
                UIKit.Label(new Rect(palette.x + 10, palette.y + 10, palette.width - 20, 120), "見るだけモード\n（ブロックは\n動かせません）", UIKit.Small);
            }

            // ブロックの数
            int count = ProgramText.Count(script);
            string limit = level.MaxBlocks > 0 ? $" / 上限 {level.MaxBlocks}" : "";
            var countColor = level.MaxBlocks > 0 && count > level.MaxBlocks ? UIKit.ErrorColor : UIKit.SubTextColor;
            UIKit.Label(new Rect(view.x + 8, area.y + 2, view.width - 16, 30),
                UIKit.Colorize($"ブロックの数 {count}{limit}", countColor), UIKit.Small);

            // ---- ドラッグ中・持っているときのマウスの動き ----
            if (dragging != null)
            {
                var type = e.rawType;
                if (carrying)
                {
                    // タップで持っているとき：ブロックがマウスについてくる。次のタップで置く
                    if (type == EventType.Repaint || type == EventType.MouseMove || type == EventType.MouseDrag) dragPos = e.mousePosition / s;
                    if (type == EventType.MouseDown)
                    {
                        dragPos = e.mousePosition / s;
                        if (e.button == 1) CancelDrag();   // 右クリック（2本指タップ）で やめる
                        else Drop(view, script);
                        carrying = false;
                        e.Use();
                    }
                }
                else if (type == EventType.MouseDrag)
                {
                    dragPos = e.mousePosition / s;
                    e.Use();
                }
                else if (type == EventType.MouseUp)
                {
                    dragPos = e.mousePosition / s;
                    GUIUtility.hotControl = 0;
                    // ほとんど動かさずに はなした（タップした）ときは「持った」ままにする
                    if ((dragPos - pressPos).magnitude < 6f) carrying = true;
                    else Drop(view, script);
                    e.Use();
                }
            }

            // ---- 並べたブロック ----
            items.Clear();
            slots.Clear();
            var hat = new Rect(8, 8, 190, HatH);
            owners.Clear();
            float bottom = Layout(script, 8, hat.yMax + Gap, 0);
            if (editable) HandleKeys(view, level, script);
            float maxX = 8 + CWidth;
            foreach (var it in items) maxX = Mathf.Max(maxX, it.Full.xMax);
            var content = new Rect(0, 0, Mathf.Max(view.width - 20, maxX + 20), Mathf.Max(view.height - 20, bottom + 80));

            UIKit.Fill(view, new Color(0.95f, 0.95f, 0.97f));
            scroll = UIKit.BeginScrollView(view, scroll, content);

            UIKit.Round(Grow(hat, OutlineW), OutlineColor);
            UIKit.Round(hat, EventColor);
            UIKit.Label(new Rect(hat.x + 12, hat.y + 4, hat.width - 20, hat.height - 8), "▶ 起動したとき", UIKit.BlockText);

            if (editable && dragging == null && e.type == EventType.Repaint)
            {
                Vector2 hm = e.mousePosition / s;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (!Grabbable(items[i], hm)) continue;
                    var hr = items[i].Node.IsContainer ? items[i].Full : items[i].Header;
                    UIKit.Round(new Rect(hr.x - 3, hr.y - 3, hr.width + 6, hr.height + 6), new Color(1f, 1f, 1f, 0.9f));
                    break;
                }
            }
            foreach (var it in items) DrawItem(it, Vector2.zero, editable);

            // キーボードのカーソル（ここにブロックが置かれる）
            if (editable && keyboardActive && dragging == null && cursorSlot < slots.Count)
            {
                var cs = slots[cursorSlot];
                UIKit.Fill(new Rect(cs.Pos.x, cs.Pos.y - 3, CmdW + 30, 6), new Color(1f, 0.75f, 0f));
                UIKit.Label(new Rect(cs.Pos.x + CmdW + 34, cs.Pos.y - 14, 120, 28), UIKit.Colorize("◀ ここに置く", new Color(0.8f, 0.45f, 0f)), UIKit.Small);
            }

            // ドラッグ中：はめこむ場所を表示
            if (dragging != null && view.Contains(dragPos) && FindSlot(view, out var slot))
            {
                UIKit.Fill(new Rect(slot.Pos.x, slot.Pos.y - 2, CmdW, 4), new Color(0.2f, 0.2f, 0.25f, 0.9f));
                // からっぽの くり返し・もし を 置くと、下のブロックが ぜんぶ口の中に入る：入るブロックを 光らせる
                if (WillWrap(dragging, slot))
                {
                    var wrapped = new HashSet<Node>(slot.List.GetRange(slot.Index, slot.List.Count - slot.Index));
                    foreach (var it in items)
                        if (wrapped.Contains(it.Node))
                            UIKit.Fill(Grow(it.Full, 3), new Color(1f, 0.75f, 0.1f, 0.35f));
                    UIKit.Label(new Rect(slot.Pos.x + CmdW + 8, slot.Pos.y - 14, 260, 28),
                        UIKit.Colorize("▼ 下のブロックを ぜんぶ中に入れる", new Color(0.8f, 0.45f, 0f)), UIKit.Small);
                }
            }

            // 2本指タップ（右クリック）で、そのブロックを消す
            if (editable && dragging == null && e.type == EventType.MouseDown && e.button == 1)
            {
                Vector2 m = e.mousePosition / s;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (!Grabbable(items[i], m)) continue;
                    RemoveNode(script, items[i].Node);
                    e.Use();
                    break;
                }
            }

            // ブロックをつかむ
            if (editable && dragging == null && e.type == EventType.MouseDown && e.button == 0)
            {
                Vector2 m = e.mousePosition / s; // スクロールの中の座標
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var it = items[i];
                    if (Grabbable(it, m))
                    {
                        FindParent(script, it.Node, out originList, out originIndex);
                        RemoveNode(script, it.Node);
                        StartDrag(it.Node, m - it.Full.position, e);
                        break;
                    }
                }
            }
            UIKit.EndScrollView();

            if (script.Count == 0 && dragging == null)
                UIKit.Label(new Rect(view.x + 20, view.y + 70, view.width - 40, 60),
                    UIKit.Colorize("左のブロックをドラッグして、\n「▶ 起動したとき」の下につなげよう", new Color(0.35f, 0.35f, 0.45f)), UIKit.Small);

            if (carrying && dragging != null)
                UIKit.Label(new Rect(view.x + 8, view.yMax - 34, view.width - 16, 30),
                    UIKit.Colorize("置きたい場所をタップ（クリック）。右クリック・Esc で やめる", new Color(0.8f, 0.4f, 0f)), UIKit.Small);

            // つかんでいるブロックを、いちばん上に描く
            if (dragging != null)
            {
                var savedItems = new List<Item>(items);
                items.Clear();
                var tl = dragPos - grabOffset;
                Layout(new List<Node> { dragging }, tl.x, tl.y, 0);
                foreach (var it in items) DrawItem(it, Vector2.zero, false);
                items.Clear();
                items.AddRange(savedItems);
            }
        }

        // ------------------------------------------------------------------
        // ブロック置き場（パレット）
        // ------------------------------------------------------------------

        private Vector2 paletteScroll;

        // ----- キーボード操作 -----
        private int cursorSlot;                       // ブロックを置く場所（slots の番号）
        private int paletteIndex;                     // えらんでいる置き場のブロック
        private bool keyboardActive;                  // キーボードで操作中（カーソルを表示する）
        private List<Node> pendingList;               // 次の回に、カーソルをこの場所へ動かす
        private int pendingIndex;
        private readonly Dictionary<List<Node>, Node> owners = new Dictionary<List<Node>, Node>(); // 中身のリスト → そのくり返し・もし

        private void DrawPalette(Rect r, Level level)
        {
            UIKit.Fill(r, new Color(0.12f, 0.13f, 0.19f));
            var e = Event.current;

            // 先に、置き場に並べるものを決める（多いときはスクロールする）
            var entries = new List<(string header, Node node, Color color)>();
            BuildPaletteEntries(level, currentScript, entries);
            // ブロックの高さは 文字（ふりがな・折り返しをふくむ）が 枠に入るように決める。
            // 幅は スクロールバーが出たときの せまいほうで はかる
            float labelW = r.width - 20 - 16 - 12;
            var heights = new float[entries.Count];
            float contentH = 6;
            for (int i = 0; i < entries.Count; i++)
            {
                var en = entries[i];
                if (en.header != null) { contentH += 24; continue; }
                heights[i] = Mathf.Max(PaletteBlockH, UIKit.CalcHeight(PaletteLabel(en.node), UIKit.BlockText, labelW) + 6);
                contentH += heights[i] + 4;
            }
            var inner = new Rect(0, 0, r.width - (contentH > r.height ? 20 : 0), Mathf.Max(contentH, r.height - 4));
            paletteScroll = UIKit.BeginScrollView(r, paletteScroll, inner);
            float y = 6;
            int nodeIndex = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                var en = entries[i];
                if (en.header != null)
                {
                    UIKit.Label(new Rect(8, y, inner.width - 16, 24), en.header, UIKit.Small);
                    y += 24;
                    continue;
                }
                nodeIndex++;
                var rect = new Rect(8, y, inner.width - 16, heights[i]);
                if (keyboardActive && nodeIndex == paletteIndex)
                    UIKit.Round(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), new Color(1f, 0.85f, 0.1f));
                DrawBlockBody(rect, en.color);
                UIKit.Label(new Rect(rect.x + 8, rect.y + 1, rect.width - 12, rect.height - 2), PaletteLabel(en.node), UIKit.BlockText);
                if (dragging == null && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition / UIKit.Scale))
                {
                    // スクロールの中の座標 → 画面の座標
                    Vector2 outside = r.position + e.mousePosition / UIKit.Scale - paletteScroll / UIKit.Scale;
                    StartDrag(en.node.Clone(), e.mousePosition / UIKit.Scale - rect.position, e, outside);
                }
                y += heights[i] + 4;
            }
            UIKit.EndScrollView();
        }

        private static void BuildPaletteEntries(Level level, List<Node> script, List<(string header, Node node, Color color)> list)
        {
            void Header(string text) => list.Add((text, null, default));
            void Item(Node template, Color color) => list.Add((null, template, color));

            Header("動き");
            foreach (var c in level.Commands)
                if (c == CommandType.Forward || c == CommandType.TurnLeft || c == CommandType.TurnRight)
                    Item(Node.Cmd(c), MotionColor);
            Header("アクション");
            foreach (var c in level.Commands)
                if (c == CommandType.Magic || c == CommandType.Pick || c == CommandType.Use)
                    Item(Node.Cmd(c), MagicColor);

            if (level.Structures.Count > 0)
            {
                Header("制御");
                var firstCond = level.Conditions.Count > 0 ? level.Conditions[0] : ConditionType.IsObstacle;
                if (level.Structures.Contains(NodeKind.Repeat)) Item(Node.Repeat(3), ControlColor);
                if (level.Structures.Contains(NodeKind.If))
                {
                    Item(Node.If(firstCond, false, new Node[0]), ControlColor);
                    if (level.AllowElse) Item(Node.If(firstCond, false, new Node[0], new Node[0]), ControlColor);
                }
                if (level.Structures.Contains(NodeKind.While)) Item(Node.While(ConditionType.AtGoal, true), ControlColor);
            }

            if (level.AllowVariables)
            {
                Header("変数");
                Item(Node.Assign("n", Expr.Of(Term.Read())), VariableColor);
                Item(Node.Assign("n", Expr.Num(0)), VariableColor);
                Item(Node.Assign("n", Expr.Calc(Term.Var("n"), '-', Term.Num(1))), VariableColor);
                Item(Node.RepeatBy(Expr.Of(Term.Var("n"))), ControlColor);
            }

            if (level.AllowFunctions)
            {
                // 関数：「関数を作る」と、作った関数を使うブロック
                Header("関数");
                Item(Node.Define(NextFunctionName(script)), FunctionColor);
                if (script != null)
                    foreach (var n in script)
                        if (n.Kind == NodeKind.Define) Item(Node.Call(n.Var), FunctionColor);
            }
        }

        /// <summary>関数の名前の候補（Python でもそのまま使える名前）</summary>
        public static readonly string[] FunctionNames = { "turn_corner", "step_forward", "turn_around", "go_ahead", "check_item", "my_move" };

        /// <summary>まだ使っていない関数の名前</summary>
        private static string NextFunctionName(List<Node> script, string after = null)
        {
            var used = new HashSet<string>();
            if (script != null) foreach (var n in script) if (n.Kind == NodeKind.Define) used.Add(n.Var);
            int start = after != null ? System.Array.IndexOf(FunctionNames, after) + 1 : 0;
            for (int k = 0; k < FunctionNames.Length; k++)
            {
                string name = FunctionNames[(start + k) % FunctionNames.Length];
                if (!used.Contains(name)) return name;
            }
            return after ?? FunctionNames[0];
        }

        private static string PaletteLabel(Node n)
        {
            switch (n.Kind)
            {
                case NodeKind.Repeat: return n.CountExpr != null ? $"{n.CountExpr} 回くり返す" : "○回くり返す";
                case NodeKind.Assign: return ProgramText.AssignLabel(n);
                case NodeKind.If: return n.HasElse ? "もし／でなければ" : "もし〜なら";
                case NodeKind.While: return "〜までくり返す";
                case NodeKind.Define: return "関数を作る";
                case NodeKind.Call: return $"{n.Var} をする";
                default: return CommandInfo.Get(n.Command).BlockLabel;
            }
        }

        // ------------------------------------------------------------------
        // ならべ方（位置の計算）
        // ------------------------------------------------------------------

        private float Layout(List<Node> list, float x, float y, int depth)
        {
            slots.Add(new Slot { List = list, Index = 0, Pos = new Vector2(x, y) });
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                var it = new Item { Node = n };
                items.Add(it);
                if (!n.IsContainer)
                {
                    bool assign = n.Kind == NodeKind.Assign;
                    it.Full = it.Header = new Rect(x, y, assign ? AssignW : CmdW, assign ? AssignH : RowH);
                    y += it.Full.height + Gap;
                }
                else
                {
                    float top = y;
                    it.Header = new Rect(x, y, CWidth, HeaderH);
                    y += HeaderH;
                    owners[n.Body] = n;
                    float bodyEnd = Layout(n.Body, x + Indent, y, depth + 1);
                    y = n.Body.Count == 0 ? y + EmptyBodyH : bodyEnd;
                    if (n.Kind == NodeKind.If && n.HasElse)
                    {
                        it.ElseBar = new Rect(x, y, CWidth, ElseH);
                        y += ElseH;
                        owners[n.Else] = n;
                        float elseEnd = Layout(n.Else, x + Indent, y, depth + 1);
                        y = n.Else.Count == 0 ? y + EmptyBodyH : elseEnd;
                    }
                    it.Footer = new Rect(x, y, CWidth, FooterH);
                    y += FooterH;
                    it.LeftBar = new Rect(x, top, Indent, y - top);
                    it.Full = new Rect(x, top, CWidth, y - top);
                    y += Gap;
                }
                slots.Add(new Slot { List = list, Index = i + 1, Pos = new Vector2(x, y) });
            }
            return y;
        }

        private static bool Grabbable(Item it, Vector2 m)
        {
            if (!it.Node.IsContainer) return it.Full.Contains(m);
            return it.Header.Contains(m) || it.Footer.Contains(m) || it.LeftBar.Contains(m) ||
                   (it.ElseBar.width > 0 && it.ElseBar.Contains(m));
        }

        // ------------------------------------------------------------------
        // ブロックを描く
        // ------------------------------------------------------------------

        private void DrawItem(Item it, Vector2 off, bool editable)
        {
            var n = it.Node;
            Color color = ColorOf(n);
            Color dark = color * 0.75f;
            dark.a = 1f;

            if (n == Highlight) UIKit.Round(Grow(n.IsContainer ? it.Full : it.Header, 4), new Color(1f, 0.95f, 0.2f));
            if (n == Failed) UIKit.Round(Grow(n.IsContainer ? it.Header : it.Full, 4), new Color(1f, 0.3f, 0.3f));

            if (!n.IsContainer)
            {
                DrawBlockBody(it.Full, color);
                if (n.Kind == NodeKind.Assign) DrawAssign(it.Full, n, editable);
                else if (n.Kind == NodeKind.Call)
                    UIKit.Label(new Rect(it.Full.x + 10, it.Full.y + 1, it.Full.width - 14, it.Full.height - 2), $"{n.Var} をする", UIKit.BlockText);
                else UIKit.Label(new Rect(it.Full.x + 10, it.Full.y + 1, it.Full.width - 14, it.Full.height - 2),
                    CommandInfo.Get(n.Command).BlockLabel, UIKit.BlockText);
                return;
            }

            // C の形
            UIKit.Round(Grow(it.LeftBar, OutlineW), OutlineColor);
            UIKit.Round(Grow(it.Header, OutlineW), OutlineColor);
            if (it.ElseBar.width > 0) UIKit.Round(Grow(it.ElseBar, OutlineW), OutlineColor);
            UIKit.Round(Grow(it.Footer, OutlineW), OutlineColor);
            UIKit.Round(it.LeftBar, color);
            UIKit.Round(it.Header, color);
            Shade(it.Header, dark);
            if (it.ElseBar.width > 0)
            {
                UIKit.Round(it.ElseBar, color);
                UIKit.Label(new Rect(it.ElseBar.x + 12, it.ElseBar.y, 200, it.ElseBar.height), "でなければ", UIKit.BlockText);
            }
            UIKit.Round(it.Footer, color);

            var h = it.Header;
            switch (n.Kind)
            {
                case NodeKind.Repeat when n.CountExpr != null:
                    // 回数が変数のとき：[n] を押すと n → m → 数 にもどる
                    bool simpleVar = n.CountExpr.Op == '\0' && n.CountExpr.A.Kind == TermKind.Variable;
                    string countLabel = n.CountExpr.ToString();
                    if (UIKit.ColorButton(new Rect(h.x + 8, h.y + 5, 100, 30), countLabel + " ▼", VariableColor, UIKit.SmallButton, editable))
                    {
                        if (simpleVar && n.CountExpr.A.Name == "n") n.CountExpr = Expr.Of(Term.Var("m"));
                        else n.CountExpr = null;
                    }
                    UIKit.Label(new Rect(h.x + 108, h.y + 2, 170, h.height - 4), "回くり返す", UIKit.BlockText);
                    break;
                case NodeKind.Repeat:
                    if (currentLevel != null && currentLevel.AllowVariables &&
                        UIKit.ColorButton(new Rect(h.x + 230, h.y + 5, 54, 30), "変数", VariableColor, UIKit.SmallButton, editable))
                        n.CountExpr = Expr.Of(Term.Var("n"));
                    if (UIKit.ColorButton(new Rect(h.x + 6, h.y + 5, 34, 30), "−", Color.white, UIKit.SmallButton, editable))
                        n.Count = Mathf.Max(1, n.Count - 1);
                    UIKit.Round(new Rect(h.x + 44, h.y + 6, 40, 28), Color.white);
                    UIKit.Label(new Rect(h.x + 44, h.y + 6, 40, 28), UIKit.Colorize(n.Count.ToString(), new Color(0.2f, 0.2f, 0.25f)), UIKit.SmallCenter);
                    if (UIKit.ColorButton(new Rect(h.x + 88, h.y + 5, 34, 30), "＋", Color.white, UIKit.SmallButton, editable))
                        n.Count = Mathf.Min(MiniPython.MaxRepeat, n.Count + 1);
                    UIKit.Label(new Rect(h.x + 128, h.y + 2, 100, h.height - 4), "回くり返す", UIKit.BlockText);
                    break;
                case NodeKind.If:
                    UIKit.Label(new Rect(h.x + 10, h.y + 2, 40, h.height - 4), "もし", UIKit.BlockText);
                    ConditionButton(new Rect(h.x + 50, h.y + 4, 190, 32), n, editable);
                    UIKit.Label(new Rect(h.x + 246, h.y + 2, 44, h.height - 4), "なら", UIKit.BlockText);
                    break;
                case NodeKind.Define:
                    // 関数 [名前 ▼] を作る：名前を押すと、まだ使っていない名前に変わる（その関数を使うブロックの名前も変わる）
                    UIKit.Label(new Rect(h.x + 10, h.y + 2, 44, h.height - 4), "関数", UIKit.BlockText);
                    if (UIKit.ColorButton(new Rect(h.x + 54, h.y + 4, 158, 32), n.Var + " ▼", new Color(0.85f, 0.30f, 0.42f), UIKit.SmallButton, editable))
                        RenameFunction(n, NextFunctionName(currentScript, n.Var));
                    UIKit.Label(new Rect(h.x + 218, h.y + 2, 70, h.height - 4), "を作る", UIKit.BlockText);
                    break;
                case NodeKind.While:
                    ConditionButton(new Rect(h.x + 8, h.y + 4, 154, 32), n, editable);
                    UIKit.Label(new Rect(h.x + 166, h.y + 2, 124, h.height - 4), n.Negate ? "までくり返す" : n.Cmp != null ? "のあいだくり返す" : "あいだくり返す", UIKit.BlockText);
                    break;
            }
        }

        /// <summary>調べること（〈前が穴〉など）のボタン。クリックすると次の選択肢に変わる</summary>
        private void ConditionButton(Rect r, Node n, bool editable)
        {
            string label = n.Kind == NodeKind.While
                ? (n.Cmp != null ? n.Cmp.Label : ConditionInfo.Get(n.Condition).Label)
                : ProgramText.CondLabel(n);
            if (UIKit.ColorButton(r, label + " ▼", n.Cmp != null ? VariableColor : SensingColor, UIKit.SmallButton, editable)) CycleCondition(n);
        }

        /// <summary>調べることを、次の選択肢に変える</summary>
        private void CycleCondition(Node n)
        {
            if (currentLevel == null) return;
            // 選べる条件の一覧（調べること × 「まで／あいだ」、変数が使えるなら「n > 0」「n = 0」も）
            bool isWhile = n.Kind == NodeKind.While;
            var options = new List<(ConditionType cond, bool neg, Compare cmp)>();
            foreach (var c in currentLevel.Conditions)
            {
                options.Add((c, isWhile, null));
                options.Add((c, !isWhile, null));
            }
            if (currentLevel.AllowVariables)
            {
                var n0 = new Compare { Left = Expr.Of(Term.Var("n")), Op = ">", Right = Expr.Num(0) };
                var nz = new Compare { Left = Expr.Of(Term.Var("n")), Op = "==", Right = Expr.Num(0) };
                options.Add((ConditionType.IsObstacle, false, n0));
                options.Add((ConditionType.IsObstacle, isWhile, nz));
            }
            if (options.Count == 0) return;
            string Key(ConditionType c, bool neg, Compare cmp) => (cmp != null ? cmp.ToString() : c.ToString()) + neg;
            string current = Key(n.Condition, n.Negate, n.Cmp);
            int idx = options.FindIndex(o => Key(o.cond, o.neg, o.cmp) == current);
            var next = options[(idx + 1) % options.Count];
            n.Condition = next.cond;
            n.Negate = next.neg;
            n.Cmp = next.cmp;
        }

        /// <summary>
        /// キーボードだけでブロックを組み立てる
        ///   ↑↓ 置く場所　←→ 置き場のブロック　Space/Enter 置く　BackSpace 上のブロックを消す　Delete 下のブロックを消す
        ///   ＋/− 回数や数を変える　C 条件を変える　V 回数を変数にする・n⇔m
        /// </summary>
        private void HandleKeys(Rect view, Level level, List<Node> script)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown) keyboardActive = false;

            // 前の回に決めた場所へカーソルを動かす
            if (pendingList != null)
            {
                int found = slots.FindIndex(sl => sl.List == pendingList && sl.Index == pendingIndex);
                if (found >= 0) cursorSlot = found;
                pendingList = null;
            }
            cursorSlot = Mathf.Clamp(cursorSlot, 0, Mathf.Max(0, slots.Count - 1));

            if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0 || dragging != null || slots.Count == 0) return;
            var palette = new List<(string header, Node node, Color color)>();
            BuildPaletteEntries(level, script, palette);
            var nodes = palette.FindAll(p => p.node != null);
            var slot = slots[cursorSlot];
            Node above = slot.Index > 0 ? slot.List[slot.Index - 1] : (owners.TryGetValue(slot.List, out var owner) ? owner : null);
            bool handled = true;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow: cursorSlot = Mathf.Max(0, cursorSlot - 1); break;
                case KeyCode.DownArrow: cursorSlot = Mathf.Min(slots.Count - 1, cursorSlot + 1); break;
                case KeyCode.LeftArrow: if (nodes.Count > 0) paletteIndex = (paletteIndex - 1 + nodes.Count) % nodes.Count; break;
                case KeyCode.RightArrow: if (nodes.Count > 0) paletteIndex = (paletteIndex + 1) % nodes.Count; break;
                case KeyCode.Space:
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (!keyboardActive || nodes.Count == 0) break; // 最初の1回はカーソルを出すだけ
                    var node = nodes[Mathf.Clamp(paletteIndex, 0, nodes.Count - 1)].node.Clone();
                    Place(script, slot.List, slot.Index, node);
                    // くり返し・もし を置いたら中へ、ほかは次の場所へ
                    if (node.IsContainer) { pendingList = node.Body; pendingIndex = 0; }
                    else { pendingList = slot.List; pendingIndex = slot.Index + 1; }
                    break;
                case KeyCode.Backspace:
                    if (slot.Index > 0)
                    {
                        slot.List.RemoveAt(slot.Index - 1);
                        pendingList = slot.List;
                        pendingIndex = slot.Index - 1;
                    }
                    break;
                case KeyCode.Delete:
                    if (slot.Index < slot.List.Count) slot.List.RemoveAt(slot.Index);
                    pendingList = slot.List;
                    pendingIndex = slot.Index;
                    break;
                case KeyCode.Equals:
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                case KeyCode.Semicolon: // 日本語キーボードの ＋ のキー
                    ChangeNumber(above, +1);
                    break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    ChangeNumber(above, -1);
                    break;
                case KeyCode.C:
                    if (above != null && (above.Kind == NodeKind.If || above.Kind == NodeKind.While)) CycleCondition(above);
                    break;
                case KeyCode.V:
                    if (above != null && level.AllowVariables) ToggleVariable(above);
                    break;
                default:
                    handled = false;
                    break;
            }
            if (!handled) return;
            keyboardActive = true;
            e.Use();

            // カーソルが見えるようにスクロールする
            if (cursorSlot < slots.Count)
            {
                float cy = slots[cursorSlot].Pos.y * UIKit.Scale;
                float viewH = (view.height - 40) * UIKit.Scale;
                if (cy < scroll.y) scroll.y = Mathf.Max(0, cy - 20 * UIKit.Scale);
                else if (cy > scroll.y + viewH) scroll.y = cy - viewH;
            }
        }

        /// <summary>＋−キー：くり返す回数や、変数の数を変える</summary>
        private static void ChangeNumber(Node n, int d)
        {
            if (n == null) return;
            if (n.Kind == NodeKind.Repeat && n.CountExpr == null) n.Count = Mathf.Clamp(n.Count + d, 1, MiniPython.MaxRepeat);
            else if (n.Kind == NodeKind.Assign)
            {
                var v = n.Value;
                if (v.Op == '\0' && v.A.Kind == TermKind.Number) n.Value = Expr.Num(Mathf.Clamp(v.A.Number + d, -99, 99));
                else if (v.Op != '\0' && v.A.Kind == TermKind.Variable && v.A.Name == n.Var && v.B.Kind == TermKind.Number)
                {
                    int k = Mathf.Clamp((v.Op == '+' ? v.B.Number : -v.B.Number) + d, -99, 99);
                    n.Value = Expr.Calc(Term.Var(n.Var), k >= 0 ? '+' : '-', Term.Num(Mathf.Abs(k)));
                }
            }
        }

        /// <summary>V キー：くり返す回数を 数 → n → m → 数 と変える／変数のブロックは n ⇔ m</summary>
        private static void ToggleVariable(Node n)
        {
            if (n.Kind == NodeKind.Repeat)
            {
                if (n.CountExpr == null) n.CountExpr = Expr.Of(Term.Var("n"));
                else if (n.CountExpr.Op == '\0' && n.CountExpr.A.Kind == TermKind.Variable && n.CountExpr.A.Name == "n") n.CountExpr = Expr.Of(Term.Var("m"));
                else n.CountExpr = null;
            }
            else if (n.Kind == NodeKind.Assign)
            {
                string other = n.Var == "n" ? "m" : "n";
                var v = n.Value;
                if (v.Op != '\0' && v.A.Kind == TermKind.Variable && v.A.Name == n.Var) n.Value = Expr.Calc(Term.Var(other), v.Op, v.B);
                n.Var = other;
            }
        }

        /// <summary>変数のブロック：「n を 看板の数 にする」「n を 3 にする」「n を -1 ずつ変える」</summary>
        private void DrawAssign(Rect r, Node n, bool editable)
        {
            var v = n.Value;
            bool isChange = v.Op != '\0' && v.A.Kind == TermKind.Variable && v.A.Name == n.Var && v.B.Kind == TermKind.Number;
            bool isNumber = v.Op == '\0' && v.A.Kind == TermKind.Number;
            // ボタンは ふつうのブロックの高さで、太くしたブロックの まん中に ならべる（文字は ブロックの高さいっぱいを使う）
            float full = r.height, top = r.y;
            r = new Rect(r.x, r.y + (r.height - RowH) * 0.5f, r.width, RowH);

            // 変数の名前（n ⇔ m）
            if (UIKit.ColorButton(new Rect(r.x + 6, r.y + 3, 44, 30), n.Var, Color.white, UIKit.SmallButton, editable))
            {
                string other = n.Var == "n" ? "m" : "n";
                if (isChange) n.Value = Expr.Calc(Term.Var(other), v.Op, v.B);
                n.Var = other;
            }
            float x = r.x + 54;
            if (v.IsRead)
            {
                UIKit.Label(new Rect(x, top, r.xMax - x - 4, full), "を 看板の数 にする", UIKit.BlockText);
                return;
            }
            if (!isChange && !isNumber)
            {
                UIKit.Label(new Rect(x, top, r.xMax - x - 4, full), $"を {v} にする", UIKit.BlockText);
                return;
            }
            int k = isNumber ? v.A.Number : (v.Op == '+' ? v.B.Number : -v.B.Number);
            UIKit.Label(new Rect(x, top, 24, full), "を", UIKit.BlockText);
            int nk = k;
            if (UIKit.ColorButton(new Rect(x + 22, r.y + 3, 30, 30), "−", Color.white, UIKit.SmallButton, editable)) nk = Mathf.Max(-99, k - 1);
            UIKit.Round(new Rect(x + 54, r.y + 4, 40, 28), Color.white);
            UIKit.Label(new Rect(x + 54, r.y + 4, 40, 28), UIKit.Colorize((isChange && k > 0 ? "+" : "") + k, new Color(0.2f, 0.2f, 0.25f)), UIKit.SmallCenter);
            if (UIKit.ColorButton(new Rect(x + 96, r.y + 3, 30, 30), "＋", Color.white, UIKit.SmallButton, editable)) nk = Mathf.Min(99, k + 1);
            UIKit.Label(new Rect(x + 130, top, r.xMax - (x + 130) - 4, full), isChange ? "ずつ変える" : "にする", UIKit.BlockText);
            if (nk != k)
                n.Value = isNumber ? Expr.Num(nk) : Expr.Calc(Term.Var(n.Var), nk >= 0 ? '+' : '-', Term.Num(Mathf.Abs(nk)));
        }

        private Level currentLevel;
        private List<Node> currentScript;

        /// <summary>
        /// ブロックを置く。「関数を作る」は、いちばん外側の上に置く
        /// （Python では、関数は使う行より上で、字下げなしで作るきまりなので）
        /// </summary>
        private static void Place(List<Node> script, List<Node> list, int index, Node node)
        {
            if (node.Kind == NodeKind.Define)
            {
                int top = 0;
                while (top < script.Count && script[top].Kind == NodeKind.Define) top++;
                script.Insert(top, node);
                return;
            }
            list.Insert(index, node);
        }

        /// <summary>関数の名前を変えて、その関数を使うブロックの名前も合わせる</summary>
        private void RenameFunction(Node def, string newName)
        {
            if (newName == def.Var) return;
            string old = def.Var;
            void Walk(List<Node> list)
            {
                foreach (var n in list)
                {
                    if (n.Kind == NodeKind.Call && n.Var == old) n.Var = newName;
                    Walk(n.Body);
                    Walk(n.Else);
                }
            }
            if (currentScript != null) Walk(currentScript);
            def.Var = newName;
        }

        /// <summary>ドラッグをやめる（画面を切り替えるときなど）</summary>
        /// <summary>タップでブロックを持っているか（Esc でやめられる）</summary>
        public bool IsCarrying => dragging != null && carrying;

        private bool carrying;          // タップで持っている（次のタップで置く）
        private Vector2 pressPos;       // 押した場所（動かしたか調べる）
        private List<Node> originList;  // 並べたブロックを持ったときの、もとの場所
        private int originIndex;

        /// <summary>ドラッグ・持つのをやめる。並べてあったブロックは、もとの場所にもどす</summary>
        public void CancelDrag()
        {
            if (dragging != null && originList != null) originList.Insert(Mathf.Min(originIndex, originList.Count), dragging);
            originList = null;
            carrying = false;
            dragging = null;
            if (GUIUtility.hotControl == controlId) GUIUtility.hotControl = 0;
        }

        /// <summary>ブロック1つ：まわりに黒いふち、下のほうに少し暗い帯（立体に見える）</summary>
        internal static void DrawBlockBody(Rect r, Color color)
        {
            UIKit.Round(Grow(r, OutlineW), OutlineColor);
            UIKit.Round(r, color);
            Color dark = color * 0.75f;
            dark.a = 1f;
            Shade(r, dark);
        }

        private static void Shade(Rect r, Color dark)
        {
            if (r.height > 12) UIKit.Fill(new Rect(r.x + 4, r.yMax - 4, r.width - 8, 3), dark);
        }

        internal static Color ColorOf(Node n)
        {
            if (n.Kind == NodeKind.Define || n.Kind == NodeKind.Call) return FunctionColor;
            if (n.IsContainer) return ControlColor;
            if (n.Kind == NodeKind.Assign) return VariableColor;
            return n.Command == CommandType.Forward || n.Command == CommandType.TurnLeft || n.Command == CommandType.TurnRight ? MotionColor : MagicColor;
        }

        private static Rect Grow(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + d * 2, r.height + d * 2);
        private static Rect Shrink(Rect r, float d) => Grow(r, -d);

        // ------------------------------------------------------------------
        // ドラッグ＆ドロップ
        // ------------------------------------------------------------------

        private void StartDrag(Node node, Vector2 offset, Event e, Vector2? outside = null)
        {
            if (outside != null) originList = null; // 置き場から取ったブロックには、もどる場所がない
            dragging = node;
            grabOffset = offset;
            dragPos = outside ?? MouseOutside(e);
            pressPos = dragPos;
            carrying = false;
            GUIUtility.hotControl = controlId;
            GUIUtility.keyboardControl = 0;
            e.Use();
        }

        /// <summary>スクロールの中で押したときも、外の座標でのマウス位置を求める</summary>
        private Vector2 MouseOutside(Event e)
        {
            return lastView.position + e.mousePosition / UIKit.Scale - scroll / UIKit.Scale;
        }

        private Rect lastView;

        /// <summary>落とした場所にいちばん近い「はめこむ場所」に入れる。エディタの外なら消える</summary>
        private void Drop(Rect view, List<Node> script)
        {
            var node = dragging;
            dragging = null;
            originList = null;
            carrying = false;
            if (!view.Contains(dragPos)) return; // 置き場や外に落とした → 消える

            items.Clear();
            slots.Clear();
            Layout(script, 8, 8 + HatH + Gap, 0);
            if (FindSlot(view, out var slot))
            {
                if (WillWrap(node, slot))
                {
                    // くり返し・もし の口の中に、その下のブロックを いちばん下まで ぜんぶ入れる（スクラッチと同じ）
                    var below = slot.List.GetRange(slot.Index, slot.List.Count - slot.Index);
                    slot.List.RemoveRange(slot.Index, below.Count);
                    node.Body.AddRange(below);
                }
                Place(script, slot.List, Mathf.Min(slot.Index, slot.List.Count), node);
            }
            else Place(script, script, script.Count, node);
        }

        /// <summary>
        /// 置くと 下のブロックを口の中に入れるか：からっぽの くり返し・もし を、ブロックが続いている場所の上に置くとき
        /// （関数を作るブロックは 入れない）
        /// </summary>
        private static bool WillWrap(Node node, Slot slot)
        {
            if (node == null || !node.IsContainer || node.Kind == NodeKind.Define || node.Body.Count > 0) return false;
            if (slot.List == null || slot.Index >= slot.List.Count) return false;
            for (int i = slot.Index; i < slot.List.Count; i++)
                if (slot.List[i].Kind == NodeKind.Define) return false;
            return true;
        }

        private bool FindSlot(Rect view, out Slot best)
        {
            lastView = view;
            best = default;
            if (slots.Count == 0) return false;
            Vector2 tl = dragPos - grabOffset;
            Vector2 local = tl - view.position + scroll / UIKit.Scale;
            float bestD = float.MaxValue;
            foreach (var sl in slots)
            {
                float d = Mathf.Abs(local.y - sl.Pos.y) + 0.4f * Mathf.Abs(local.x - sl.Pos.x);
                if (d < bestD)
                {
                    bestD = d;
                    best = sl;
                }
            }
            return true;
        }

        private static bool FindParent(List<Node> list, Node target, out List<Node> parent, out int index)
        {
            index = list.IndexOf(target);
            parent = index >= 0 ? list : null;
            if (parent != null) return true;
            foreach (var n in list)
                if (FindParent(n.Body, target, out parent, out index) || FindParent(n.Else, target, out parent, out index)) return true;
            return false;
        }

        private static bool RemoveNode(List<Node> list, Node target)
        {
            if (list.Remove(target)) return true;
            foreach (var n in list)
                if (RemoveNode(n.Body, target) || RemoveNode(n.Else, target)) return true;
            return false;
        }
    }
}
