using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>説明画面の1ページ</summary>
    public class InfoPage
    {
        public string Title;
        public string Body;
        public List<Node> Example;   // ブロックの絵で見せる お手本（なければ null）
        public List<Node> Before;    // くらべる「前」のプログラム（まとめる前 → まとめたあと）
        public string Caption;       // 絵の見出し
        public int Special;          // 0：ふつうの文　1：クエストの目的と星　2：使える命令と 英語ことば表

        public InfoPage(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    /// <summary>章（学ぶ考え方のまとまり）</summary>
    public class Chapter
    {
        public int Number;
        public string Name;              // 第1章 順次処理
        public string Topic;             // 学ぶ考え方
        public int FirstLevel, LastLevel;
        public List<string> Hints;       // 失敗するたびに順番に出すヒント
        public InfoPage Explanation;     // 解説ページ
        public string RealWorld;         // 身近な例
    }

    /// <summary>新しい考え方が出てくるレベルで、最初に見せる説明（何を学ぶか・何に役立つか）</summary>
    public class Concept
    {
        public string Name;
        public List<InfoPage> Pages;
    }

    /// <summary>
    /// しばり1つ：そのブロック（命令・くり返しなど）を、プログラムに何こまで置けるか。
    /// 数えるのは置いた数なので、くり返しの中で何回動いてもかまわない。Max が 0 なら使えない
    /// </summary>
    public class UsageLimit
    {
        public string Name;   // ブロックの名前（ProgramText.KindName）
        public int Max;
    }

    /// <summary>レベル1つぶんの問題</summary>
    public class Level
    {
        public int Number;              // ステージ番号（1〜100）
        public int Template;            // 問題の形の番号（LevelFactory.StageTemplates）
        public int Variant;             // 0 = 本番、1以上 = 似た問題
        public Chapter Chapter;
        public QuestTheme Theme;        // エリア・物語・ゴール・じゃまもの・アイテム
        public Concept NewConcept;      // このレベルで初めて出てくる考え方（なければ null）
        public string Title;
        public string Goal;             // 目標の文
        public string Tip;              // 解説で出す、このレベルのポイント
        public List<GridMap> Tests = new List<GridMap>(); // 今はいつも1つ（作るときは3つで確かめてから、1つ目だけ使う）
        public bool ChangesOnRetry;     // じゃまものの場所や道の長さが、「もう一度する」たびに変わる問題
        public int MaxBlocks;           // 命令（ブロック）の数の上限。0 なら上限なし
        public List<Node> Solution;     // お手本の答え
        public List<UsageLimit> UsageLimits; // おまけの「しばり」：ブロックの種類ごとに置ける数（null ならしばりなし）

        // このレベルで使えるもの（スクラッチのパレットと、説明の一覧に出る）
        public List<CommandType> Commands = new List<CommandType>();
        public List<NodeKind> Structures = new List<NodeKind>();
        public bool AllowElse;
        public bool AllowVariables;     // 変数（n = read_sign() など）が使えるか
        public bool AllowFunctions;     // 関数（def turn_corner():）が使えるか
        public List<ConditionType> Conditions = new List<ConditionType>();
    }
}
