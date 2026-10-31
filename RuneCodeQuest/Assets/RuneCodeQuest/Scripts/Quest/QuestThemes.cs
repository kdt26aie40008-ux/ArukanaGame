namespace RuneCodeQuest
{
    /// <summary>ゴールにあるもの（「つかう」で使う）</summary>
    public enum GoalKind { Beacon, Door, Chest, Circle, Bell, Statue, Fountain, Flag }

    /// <summary>道をふさぐじゃまもの（「まほう」で消す）</summary>
    public enum ObstacleKind { Hole, Rock, Vine, Ice, Barrier }

    /// <summary>ひろって集めるアイテム（None なら集めない）</summary>
    public enum ItemKind { None, Crystal, Key, Herb, Egg, Star, Coin, Flower, Book }

    /// <summary>クエスト1つぶんの物語（エリア・目的・見た目）。問題の形は LevelFactory が決める</summary>
    public class QuestTheme
    {
        public string Area;      // エリアの名前
        public string Title;     // クエストの名前
        public string Story;     // ひとことストーリー
        public GoalKind Goal;
        public ObstacleKind Obstacle;
        public ItemKind Item;

        public bool HasItems => Item != ItemKind.None;
        public string GoalName => GoalNames[(int)Goal];
        public string GoalAction => GoalActions[(int)Goal];
        public string ObstacleName => ObstacleNames[(int)Obstacle];
        public string ItemName => ItemNames[(int)Item];

        public static readonly string[] GoalNames = { "灯台", "扉", "宝箱", "魔法陣", "鐘", "石像", "泉", "旗" };
        public static readonly string[] GoalActions =
            { "灯りをともそう", "扉を開けよう", "宝箱を開けよう", "魔法陣を起動しよう", "鐘を鳴らそう", "石像を目覚めさせよう", "泉をよみがえらせよう", "旗を立てよう" };
        /// <summary>「つかう」でゴールのものにすること（説明用）</summary>
        public static readonly string[] GoalPlain =
            { "灯台に灯りをともす", "扉を開ける", "宝箱を開ける", "魔法陣を起動する", "鐘を鳴らす", "石像を目覚めさせる", "泉をよみがえらせる", "旗を立てる" };

        /// <summary>「つかう」で使うもの（アイテムがあれば集めたアイテム、なければ まほうの杖）</summary>
        public string UseTool => HasItems ? $"集めた{ItemName}" : "まほうの杖";

        /// <summary>「つかう」で何が起きるか</summary>
        public string UseText => $"ゴールで{UseTool}を使って、{GoalPlain[(int)Goal]}。";

        public static readonly string[] GoalDone =
            { "灯台に灯りがともった！", "扉が開いた！", "宝箱が開いた！", "魔法陣が光りだした！", "鐘が鳴りひびいた！", "石像が目を覚ました！", "泉に水がわきだした！", "旗が立った！" };
        public static readonly string[] ObstacleNames = { "穴", "岩", "いばら", "氷", "結界" };
        public static readonly string[] ItemNames = { "", "水晶", "かぎ", "薬草", "たまご", "星のかけら", "金貨", "花", "まほうの本" };
        public static readonly string[] ItemDescriptions =
        {
            "", "まほうの力がつまった、すきとおった石。", "どこかの扉を開ける、古いかぎ。", "けがをなおす、森の草。",
            "まだ何が生まれるか分からない、ふしぎなたまご。", "夜空から落ちてきた、光るかけら。", "むかしの王国で使われていたお金。",
            "遺跡にさく、いい香りの花。", "古いルーンコードが書かれた本。",
        };

        private static QuestTheme Q(string area, string title, string story, GoalKind g, ObstacleKind o, ItemKind i = ItemKind.None) =>
            new QuestTheme { Area = area, Title = title, Story = story, Goal = g, Obstacle = o, Item = i };

        public static QuestTheme Of(int stage) => All[System.Math.Max(1, System.Math.Min(All.Length, stage)) - 1];

        /// <summary>
        /// 100個のクエスト。順番がステージ番号（1から）。
        /// 文章やゴール・じゃまもの・アイテムの種類は、ここを書きかえるだけで変えられます。
        /// </summary>
        public static readonly QuestTheme[] All =
        {
            // ===== 第1章 はじまりの遺跡（1〜15） =====
            Q("はじまりの遺跡", "はじまりの橋", "こわれた橋をなおしながら、向こう岸の灯台まで進もう。", GoalKind.Beacon, ObstacleKind.Hole),
            Q("苔むした回廊", "回廊の奥へ", "岩でふさがれた回廊の奥に、古い扉がねむっている。", GoalKind.Door, ObstacleKind.Rock),
            Q("ひだまりの中庭", "かれた泉", "中庭のかれた泉まで行って、「つかう」で水をよみがえらせよう。", GoalKind.Fountain, ObstacleKind.Hole),
            Q("見はりの塔", "塔の鐘を鳴らせ", "塔の上の鐘を鳴らして、遺跡に朝を知らせよう。", GoalKind.Bell, ObstacleKind.Rock),
            Q("宝物庫", "かぎと宝箱", "落ちているかぎをひろって、宝箱を開けよう。", GoalKind.Chest, ObstacleKind.Hole, ItemKind.Key),
            Q("星見の台", "星のかけらを集めろ", "ちらばった星のかけらを集めて、魔法陣を起動しよう。", GoalKind.Circle, ObstacleKind.Hole, ItemKind.Star),
            Q("ゴーレムの寝床", "ねむる石像", "ゴーレムの仲間の石像が、ずっとねむっている。", GoalKind.Statue, ObstacleKind.Rock),
            Q("曲がりくねった地下道", "地下道の灯台", "暗い地下道は曲がり角だらけ。向きに気をつけよう。", GoalKind.Beacon, ObstacleKind.Hole),
            Q("迷いの庭", "庭のまん中の旗", "金貨をひろいながら、庭のまん中に旗を立てよう。", GoalKind.Flag, ObstacleKind.Rock, ItemKind.Coin),
            Q("古井戸", "井戸の水をとりもどせ", "かれた古井戸の泉に、水をとりもどそう。", GoalKind.Fountain, ObstacleKind.Hole),
            Q("遺跡の図書室", "まほうの本を集めよ", "図書室にちらばった本を集めて、奥の扉を開けよう。", GoalKind.Door, ObstacleKind.Rock, ItemKind.Book),
            Q("鐘つき堂", "鐘つき堂の鐘", "鐘つき堂への道は、曲がり角が多い。", GoalKind.Bell, ObstacleKind.Hole),
            Q("鏡の間", "鏡の魔法陣", "水晶を集めると、鏡の魔法陣が動きだす。", GoalKind.Circle, ObstacleKind.Rock, ItemKind.Crystal),
            Q("王の間", "王の宝箱", "金貨をすべてひろって、王の宝箱をおさめよう。", GoalKind.Chest, ObstacleKind.Hole, ItemKind.Coin),
            Q("遺跡の出口", "はじまりの遺跡をぬけろ", "かぎをひろって、遺跡の出口の扉を開けよう。", GoalKind.Door, ObstacleKind.Rock, ItemKind.Key),

            // ===== 第2章 砂の遺跡（16〜40） =====
            Q("砂の大橋", "長い長い橋", "砂の谷にかかる、とても長い橋をわたろう。", GoalKind.Beacon, ObstacleKind.Hole),
            Q("らくだの道", "金貨の道", "らくだが落とした金貨が、道にずらりとならんでいる。", GoalKind.Flag, ObstacleKind.Hole, ItemKind.Coin),
            Q("オアシス", "オアシスをよみがえらせろ", "かれたオアシスの泉に、もう一度水を。", GoalKind.Fountain, ObstacleKind.Hole),
            Q("砂の市場", "市場の鐘", "岩がもようのようにならぶ道。市場の鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Rock),
            Q("流砂の谷", "流砂をこえて", "流砂の穴をよけながら、水晶をひろって進もう。", GoalKind.Beacon, ObstacleKind.Hole, ItemKind.Crystal),
            Q("太陽の神殿", "太陽の魔法陣", "太陽の神殿の床には、ふしぎなもようがある。", GoalKind.Circle, ObstacleKind.Rock),
            Q("月の神殿", "月の魔法陣", "星のかけらを集めて、月の魔法陣を起動しよう。", GoalKind.Circle, ObstacleKind.Hole, ItemKind.Star),
            Q("ピラミッドの階段", "階段をのぼれ", "ピラミッドの階段をのぼって、てっぺんに旗を。", GoalKind.Flag, ObstacleKind.Rock),
            Q("王家の墓", "墓の扉", "階段のとちゅうのかぎをひろって、墓の扉を開けよう。", GoalKind.Door, ObstacleKind.Rock, ItemKind.Key),
            Q("たかい見はり台", "見はり台の灯台", "階段を岩がふさいでいる。見はり台に灯りを。", GoalKind.Beacon, ObstacleKind.Rock),
            Q("くずれた階段", "くずれた階段", "くずれた階段のあちこちに、金貨が落ちている。", GoalKind.Chest, ObstacleKind.Hole, ItemKind.Coin),
            Q("地下の水路", "水路をくだれ", "階段をくだって、地下の泉に水を流そう。", GoalKind.Fountain, ObstacleKind.Rock),
            Q("へびの谷", "ジグザグの谷", "へびのようにくねる谷で、薬草をつもう。", GoalKind.Bell, ObstacleKind.Rock, ItemKind.Herb),
            Q("さそりの巣", "さそりの巣をぬけろ", "穴だらけのジグザグ道をぬけて、扉の向こうへ。", GoalKind.Door, ObstacleKind.Hole),
            Q("砂の塔", "砂の塔のてっぺん", "長い階段のとちゅうで、星のかけらをひろおう。", GoalKind.Flag, ObstacleKind.Hole, ItemKind.Star),
            Q("巨大ピラミッド", "大きな階段", "とても大きな階段。くり返しの中にくり返しを使おう。", GoalKind.Statue, ObstacleKind.Rock),
            Q("スフィンクスの前", "スフィンクスの石像", "水晶を集めて、スフィンクスを目覚めさせよう。", GoalKind.Statue, ObstacleKind.Rock, ItemKind.Crystal),
            Q("星の砂漠", "星のかけらの階段", "夜の砂漠に、星のかけらがふってきた。", GoalKind.Circle, ObstacleKind.Rock, ItemKind.Star),
            Q("砂あらしの道", "砂あらしの中で", "砂あらしで道が穴だらけ。灯台をたよりに進もう。", GoalKind.Beacon, ObstacleKind.Hole),
            Q("キャラバンの道", "大きなジグザグ", "キャラバンが落とした金貨を、ひろい集めよう。", GoalKind.Chest, ObstacleKind.Rock, ItemKind.Coin),
            Q("地下迷宮", "地下迷宮の扉", "迷宮のかぎをひろって、奥の扉を開けよう。", GoalKind.Door, ObstacleKind.Hole, ItemKind.Key),
            Q("くだりの大階段", "大階段をおりろ", "地下深くの泉まで、大階段をおりていこう。", GoalKind.Fountain, ObstacleKind.Rock),
            Q("古代の工房", "工房の鐘", "工房のまほうの本を集めて、鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Hole, ItemKind.Book),
            Q("砂丘の道", "長いジグザグ", "砂丘にはえた薬草をつみながら進もう。", GoalKind.Flag, ObstacleKind.Rock, ItemKind.Herb),
            Q("砂の王の間", "砂の王の宝", "かぎを集めて、砂の王の宝箱を開けよう。", GoalKind.Chest, ObstacleKind.Hole, ItemKind.Key),

            // ===== 第3章 森の遺跡（41〜60） =====
            Q("森の入り口", "いばらの小道", "いばらの生える場所は、来るたびに変わる。", GoalKind.Beacon, ObstacleKind.Vine),
            Q("小鳥の森", "小鳥の鐘", "小鳥たちのために、森の鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Vine),
            Q("薬草の谷", "薬草を集めろ", "薬草がどこにはえているかは、行ってみないとわからない。", GoalKind.Fountain, ObstacleKind.Vine, ItemKind.Herb),
            Q("大樹の根", "大樹の根っこ道", "大きな木の根っこが、道をふさいでいる。", GoalKind.Statue, ObstacleKind.Vine),
            Q("巣箱の木", "小鳥のたまご", "巣から落ちたたまごを、ぜんぶ集めてあげよう。", GoalKind.Bell, ObstacleKind.Vine, ItemKind.Egg),
            Q("こもれびの道", "こもれびの魔法陣", "木もれ日の階段の上に、魔法陣がある。", GoalKind.Circle, ObstacleKind.Rock),
            Q("ようせいの庭", "ようせいの花", "ようせいのために、花をつんであげよう。", GoalKind.Fountain, ObstacleKind.Vine, ItemKind.Flower),
            Q("森の迷路", "かわる曲がり角", "曲がり角の場所が、来るたびに変わる迷路。", GoalKind.Door, ObstacleKind.Vine),
            Q("くもの巣の道", "くもの巣をぬけて", "くもの巣のような道を、かべにそって進もう。", GoalKind.Beacon, ObstacleKind.Vine),
            Q("ふくろうの木", "ふくろうの鐘", "星のかけらを集めて、ふくろうの鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Rock, ItemKind.Star),
            Q("右まわりの森", "右へ右へ", "この森の道は、いつも右に曲がる。", GoalKind.Flag, ObstacleKind.Vine),
            Q("ほたるの池", "ほたるの池", "水晶の光で、ほたるの池をよみがえらせよう。", GoalKind.Fountain, ObstacleKind.Rock, ItemKind.Crystal),
            Q("くまの洞くつ", "洞くつの宝箱", "くまがかくした金貨を集めよう。", GoalKind.Chest, ObstacleKind.Rock, ItemKind.Coin),
            Q("森の神殿", "森の神殿の扉", "いばらにおおわれた神殿の、かぎを探そう。", GoalKind.Door, ObstacleKind.Vine, ItemKind.Key),
            Q("うずまきの林", "うずまきの林", "右へ右へと、うずまきのように進む林。", GoalKind.Circle, ObstacleKind.Vine),
            Q("花ばたけ", "花ばたけの旗", "花をつみながら、花ばたけのまん中に旗を。", GoalKind.Flag, ObstacleKind.Vine, ItemKind.Flower),
            Q("木の上の道", "木の上の灯台", "木と木をつなぐ橋は、穴だらけ。", GoalKind.Beacon, ObstacleKind.Hole),
            Q("森の図書館", "森の本だな", "森にちらばったまほうの本を、本だなにもどそう。", GoalKind.Statue, ObstacleKind.Vine, ItemKind.Book),
            Q("霧の森", "霧の中の鐘", "霧で先が見えない。薬草をつみながら進もう。", GoalKind.Bell, ObstacleKind.Vine, ItemKind.Herb),
            Q("森の守り神", "森の守り神", "水晶を集めて、森の守り神を目覚めさせよう。", GoalKind.Statue, ObstacleKind.Vine, ItemKind.Crystal),

            // ===== 第4章 雪の遺跡（61〜80） =====
            Q("雪原", "どこまでも続く雪原", "雪原の道の長さは、来るたびにちがう。", GoalKind.Beacon, ObstacleKind.Ice),
            Q("こおった川", "こおった川の旗", "こおった川の上を、向こう岸までわたろう。", GoalKind.Flag, ObstacleKind.Ice),
            Q("雪の村", "雪の村の鐘", "雪にうもれた金貨をひろって、村の鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Ice, ItemKind.Coin),
            Q("つららの洞くつ", "つららの洞くつ", "つららの氷を魔法でとかしながら進もう。", GoalKind.Door, ObstacleKind.Ice),
            Q("白くまの道", "白くまのたまご", "白くまが見つけたたまごを、ひろってあげよう。", GoalKind.Circle, ObstacleKind.Ice, ItemKind.Egg),
            Q("オーロラの丘", "オーロラの魔法陣", "星のかけらを集めると、オーロラが光る。", GoalKind.Circle, ObstacleKind.Ice, ItemKind.Star),
            Q("雪山のふもと", "ふもとの泉", "こおった泉を、もう一度よみがえらせよう。", GoalKind.Fountain, ObstacleKind.Ice),
            Q("らせんの雪道", "らせんの雪道", "左へ左へと、らせんのように続く雪道。", GoalKind.Flag, ObstacleKind.Ice),
            Q("氷の神殿", "氷の神殿", "水晶を集めて、氷の石像を目覚めさせよう。", GoalKind.Statue, ObstacleKind.Ice, ItemKind.Crystal),
            Q("雪だるまの庭", "雪だるまの宝箱", "雪だるまが守る宝箱。金貨を集めて開けよう。", GoalKind.Chest, ObstacleKind.Ice, ItemKind.Coin),
            Q("なだれの谷", "なだれのあと", "なだれで岩が道をふさいでしまった。", GoalKind.Beacon, ObstacleKind.Rock),
            Q("氷のめいろ", "氷のめいろ", "氷のめいろのかぎを探そう。", GoalKind.Door, ObstacleKind.Ice, ItemKind.Key),
            Q("右まわりの峠", "峠の鐘", "右へ右へと曲がる峠の道。", GoalKind.Bell, ObstacleKind.Ice),
            Q("ペンギンの浜", "ペンギンのたまご", "ペンギンのたまごを、ぜんぶ見つけてあげよう。", GoalKind.Fountain, ObstacleKind.Ice, ItemKind.Egg),
            Q("氷河", "氷河をわたれ", "星のかけらがうまった氷河をわたろう。", GoalKind.Flag, ObstacleKind.Ice, ItemKind.Star),
            Q("雪の図書館", "雪の図書館", "まほうの本を集めて、図書館の石像を目覚めさせよう。", GoalKind.Statue, ObstacleKind.Ice, ItemKind.Book),
            Q("吹雪の道", "吹雪の中で", "吹雪で前が見えない。かべを調べながら進もう。", GoalKind.Beacon, ObstacleKind.Ice),
            Q("氷の城門", "氷の城門", "かぎを集めて、氷の城門を開けよう。", GoalKind.Door, ObstacleKind.Ice, ItemKind.Key),
            Q("雪の女王の庭", "女王の花", "雪の中にさく花を、女王にとどけよう。", GoalKind.Fountain, ObstacleKind.Ice, ItemKind.Flower),
            Q("雪山の頂上", "頂上の旗", "水晶を集めて、雪山の頂上に旗を立てよう。", GoalKind.Flag, ObstacleKind.Ice, ItemKind.Crystal),

            // ===== 第5章 やみの城（81〜100） =====
            Q("やみの城の門", "城の門", "やみの城の道は、右に左にくねっている。", GoalKind.Door, ObstacleKind.Barrier),
            Q("くらやみの回廊", "回廊の灯台", "まっくらな回廊に、灯りをともそう。", GoalKind.Beacon, ObstacleKind.Barrier),
            Q("よろいの間", "よろいの間の鐘", "よろいがならぶ部屋をぬけて、鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Rock),
            Q("まよいの階段", "まよいの階段", "かぎをひろいながら、まよいの階段をのぼろう。", GoalKind.Flag, ObstacleKind.Barrier, ItemKind.Key),
            Q("時計の塔", "時計の塔", "星のかけらを集めて、止まった時計の鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Barrier, ItemKind.Star),
            Q("地下ろう", "地下ろうの扉", "結界をとかして、地下ろうの扉を開けよう。", GoalKind.Door, ObstacleKind.Barrier),
            Q("まほうの実験室", "実験室の魔法陣", "実験室の水晶を集めて、魔法陣を起動しよう。", GoalKind.Circle, ObstacleKind.Barrier, ItemKind.Crystal),
            Q("かがみの迷宮", "かがみの迷宮", "かがみの迷宮のおくに、石像がねむっている。", GoalKind.Statue, ObstacleKind.Barrier),
            Q("星の天文台", "天文台の星", "天文台にちらばった星のかけらを集めよう。", GoalKind.Circle, ObstacleKind.Barrier, ItemKind.Star),
            Q("宝物の間", "やみの宝箱", "金貨を集めて、やみの宝箱を開けよう。", GoalKind.Chest, ObstacleKind.Rock, ItemKind.Coin),
            Q("こうもりの洞くつ", "こうもりの洞くつ", "こうもりがとびかう、長い洞くつ。", GoalKind.Beacon, ObstacleKind.Rock),
            Q("やみの図書室", "ふういんされた本", "ふういんされたまほうの本を集めよう。", GoalKind.Statue, ObstacleKind.Barrier, ItemKind.Book),
            Q("空中回廊", "空中回廊", "空にうかぶ回廊は、穴だらけ。", GoalKind.Flag, ObstacleKind.Hole, ItemKind.Star),
            Q("ほのおの間", "ほのおの間の泉", "水晶の力で、ほのおの間に泉をよみがえらせよう。", GoalKind.Fountain, ObstacleKind.Barrier, ItemKind.Crystal),
            Q("王座への道", "王座への道", "かぎを集めて、王座の間への扉を開けよう。", GoalKind.Door, ObstacleKind.Barrier, ItemKind.Key),
            Q("大回廊", "大回廊の灯台", "とても長い回廊。金貨をひろいながら進もう。", GoalKind.Beacon, ObstacleKind.Barrier, ItemKind.Coin),
            Q("やみの庭", "かれた庭", "かれた庭に、花をとりもどそう。", GoalKind.Fountain, ObstacleKind.Barrier, ItemKind.Flower),
            Q("竜のねむる間", "竜のたまご", "竜のたまごを集めて、魔法陣にかえそう。", GoalKind.Circle, ObstacleKind.Barrier, ItemKind.Egg),
            Q("最後の扉", "最後の扉", "かぎを集めて、遺跡の心臓への扉を開けよう。", GoalKind.Door, ObstacleKind.Barrier, ItemKind.Key),
            Q("遺跡の心臓", "大魔法陣をよみがえらせろ", "すべての力を使って、遺跡の大魔法陣を起動しよう！", GoalKind.Circle, ObstacleKind.Barrier, ItemKind.Crystal),

            // ===== 変数の章 からくりの都（形の番号 101〜120。LevelFactory.StageTemplates でステージ61〜80に使う） =====
            Q("からくりの門", "数の看板", "看板に書かれた数だけ進むと、門が開くしかけ。", GoalKind.Door, ObstacleKind.Rock),
            Q("歯車の広場", "看板のとおりに", "広場にはかべがない。看板の数をたよりに進もう。", GoalKind.Bell, ObstacleKind.Rock),
            Q("じょうきの通り", "二つの看板", "曲がった先にも看板がある。もう一度読もう。", GoalKind.Beacon, ObstacleKind.Rock),
            Q("からくり屋敷", "三つの看板", "看板を3回読んで、屋敷の宝箱へ。", GoalKind.Chest, ObstacleKind.Rock),
            Q("ねじまき工場", "看板をくり返し読む", "「読む・進む・曲がる」をくり返して、水晶を集めよう。", GoalKind.Circle, ObstacleKind.Rock, ItemKind.Crystal),
            Q("ぜんまいの塔", "四つの角", "4つの角で、毎回看板を読もう。", GoalKind.Flag, ObstacleKind.Rock),
            Q("からくり倉庫", "じゃまものと看板", "倉庫の中は岩だらけ。看板の数だけ進もう。", GoalKind.Chest, ObstacleKind.Rock),
            Q("うずまき時計", "へっていく道", "進むたびに、道が1マスずつ短くなる。", GoalKind.Statue, ObstacleKind.Rock),
            Q("時計の文字ばん", "うずまきの時計", "時計の文字ばんを、うずまきのように進もう。", GoalKind.Circle, ObstacleKind.Rock),
            Q("歯車のうずまき", "金貨のうずまき", "うずまきの道に、金貨が落ちている。", GoalKind.Chest, ObstacleKind.Rock, ItemKind.Coin),
            Q("ひろがる広場", "ふえていく道", "進むたびに、道が1マスずつ長くなる。", GoalKind.Flag, ObstacleKind.Rock),
            Q("からくりの庭", "大きくなるうずまき", "外へ外へと広がる道を進もう。", GoalKind.Fountain, ObstacleKind.Rock),
            Q("発明家の家", "発明家のかぎ", "発明家がなくしたかぎを、ひろってあげよう。", GoalKind.Door, ObstacleKind.Rock, ItemKind.Key),
            Q("ふりこの間", "0になるまで", "n が 0 になるまでくり返すと、鐘が鳴る。", GoalKind.Bell, ObstacleKind.Rock),
            Q("からくり迷路", "数がつきるまで", "道が何回曲がるかは、看板の数しだい。", GoalKind.Statue, ObstacleKind.Rock),
            Q("ぜんまいの心臓部", "星のかけらのうずまき", "星のかけらを集めながら、うずまきの中心へ。", GoalKind.Circle, ObstacleKind.Rock, ItemKind.Star),
            Q("じょうき機関室", "じゃまものだらけ", "岩をまほうで消しながら、変数で進もう。", GoalKind.Beacon, ObstacleKind.Rock),
            Q("からくりの研究所", "研究所の扉", "結界をとかしながら、研究所の奥へ。", GoalKind.Door, ObstacleKind.Barrier),
            Q("時計塔の中", "時計塔の水晶", "水晶を集めて、時計塔の鐘を鳴らそう。", GoalKind.Bell, ObstacleKind.Barrier, ItemKind.Crystal),
            Q("からくりの王の間", "からくりの王", "変数の力で、からくりの王を目覚めさせよう！", GoalKind.Statue, ObstacleKind.Barrier, ItemKind.Coin),
        };
    }
}
