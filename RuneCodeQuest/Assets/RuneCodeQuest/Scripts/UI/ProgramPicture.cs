using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// プログラムを「絵」として見せる（さわれない）。旅人のクイズの答えで使う。
    /// ワードブロックの人にはクエストと同じ色・形のブロックで、Python の人には Python のコードで描く。
    /// </summary>
    public static class ProgramPicture
    {
        public static void Draw(Rect area, List<Node> program, SolveMode mode)
        {
            if (mode == SolveMode.Python) DrawCode(area, program);
            else DrawBlocks(area, program);
        }

        // ------------------------------------------------------------------
        // ブロック
        // ------------------------------------------------------------------

        /// <summary>ブロックの形で描く（入りきらないときは小さくする）</summary>
        public static void DrawBlocks(Rect area, List<Node> program)
        {
            float row = 34f;
            while (row > 18f && Measure(program, row) > area.height) row -= 2f;
            Draw(program, area.x, area.y, area.width, row);
        }

        private static float Indent(float row) => row * 0.55f;

        private static float Measure(List<Node> list, float row)
        {
            float h = 0;
            foreach (var n in list) h += MeasureOne(n, row);
            return h;
        }

        private static float MeasureOne(Node n, float row)
        {
            if (!n.IsContainer) return row + 4;
            float h = row + Mathf.Max(row * 0.5f, Measure(n.Body, row)) + row * 0.35f + 4;
            if (n.Kind == NodeKind.If && n.HasElse) h += row * 0.85f + Mathf.Max(row * 0.5f, Measure(n.Else, row));
            return h;
        }

        private static float Draw(List<Node> list, float x, float y, float width, float row)
        {
            foreach (var n in list) y = DrawOne(n, x, y, width, row);
            return y;
        }

        private static float DrawOne(Node n, float x, float y, float width, float row)
        {
            Color color = ScratchEditor.ColorOf(n);
            if (!n.IsContainer)
            {
                var r = new Rect(x, y, width, row);
                ScratchEditor.DrawBlockBody(r, color);
                Text(r, ProgramText.BlockHeader(n));
                return y + row + 4;
            }

            // C の形：上のふち（文字）・左のはしら・中身・下のふち
            float top = y;
            var header = new Rect(x, y, width, row);
            UIKit.Round(new Rect(header.x - 2, header.y - 2, header.width + 4, header.height + 4), ScratchEditor.OutlineColor);
            UIKit.Round(header, color);
            Text(header, ProgramText.BlockHeader(n));
            y += row;
            float bodyStart = y;
            y = n.Body.Count > 0 ? Draw(n.Body, x + Indent(row), y + 1, width - Indent(row), row) : y + row * 0.5f;
            if (n.Kind == NodeKind.If && n.HasElse)
            {
                var elseBar = new Rect(x, y, width, row * 0.85f);
                UIKit.Round(new Rect(elseBar.x - 2, elseBar.y - 2, elseBar.width + 4, elseBar.height + 4), ScratchEditor.OutlineColor);
                UIKit.Round(elseBar, color);
                Text(elseBar, "でなければ");
                y += row * 0.85f;
                y = n.Else.Count > 0 ? Draw(n.Else, x + Indent(row), y + 1, width - Indent(row), row) : y + row * 0.5f;
            }
            // 黒いふちを先に描いてから色をぬる（部品のつなぎ目には線が出ない）
            var pillar = new Rect(x, bodyStart - 2, Indent(row) - 2, y - bodyStart + 4);   // 左のはしら
            var footer = new Rect(x, y, Mathf.Min(width, 140f), row * 0.35f);              // 下のふち
            UIKit.Round(new Rect(pillar.x - 2, bodyStart, pillar.width + 4, y - bodyStart), ScratchEditor.OutlineColor);
            UIKit.Round(new Rect(footer.x - 2, footer.y - 2, footer.width + 4, footer.height + 4), ScratchEditor.OutlineColor);
            UIKit.Round(pillar, color);
            UIKit.Round(footer, color);
            y += row * 0.35f + 4;
            return y;
        }

        private static void Text(Rect r, string text)
        {
            UIKit.Label(new Rect(r.x + 8, r.y, r.width - 10, r.height), text, UIKit.BlockText);
        }

        // ------------------------------------------------------------------
        // Python のコード
        // ------------------------------------------------------------------

        public static void DrawCode(Rect area, List<Node> program)
        {
            UIKit.Fill(area, new Color(0.08f, 0.09f, 0.12f));
            var lines = ProgramText.ToPython(program).TrimEnd().Split('\n');
            float lineH = Mathf.Min(28f, (area.height - 12) / Mathf.Max(1, lines.Length));
            for (int i = 0; i < lines.Length; i++)
                UIKit.Label(new Rect(area.x + 12, area.y + 6 + i * lineH, area.width - 16, lineH + 4), lines[i], UIKit.Code);
        }
    }
}
