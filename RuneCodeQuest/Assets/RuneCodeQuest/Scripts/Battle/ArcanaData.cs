using System.Collections.Generic;

namespace RuneCodeQuest
{
    /// <summary>アルカナの種類（1つの村に ふつう12・旅の道4・レア3・旅の道のぬし1 の20体）</summary>
    public enum ArcanaRole { Normal, Field, Rare, FieldBoss }

    /// <summary>
    /// アルカナ120体の表（6つの村 × 20体）。
    /// 1行が1体：村・種類・名前・形・色・強さのちがい（体力の倍率）・説明。
    /// 体力・こうげき・こうげきの間かく・金貨は、村と種類と倍率から自動で決まる（BattleData で使う）。
    /// 絵は Resources/Art/Arcana/〈名前〉.png があればその画像、なければ 形と色のプログラムの絵。
    /// </summary>
    public static class ArcanaData
    {
        private struct Row
        {
            public int Region;
            public ArcanaRole Role;
            public string Name;
            public MonsterShape Shape;
            public (byte r, byte g, byte b) Color;
            public float Hp;     // 体力の倍率（1.0 がふつう）
            public string Desc;
        }

        private static Row R(int region, ArcanaRole role, string name, MonsterShape shape, byte r, byte g, byte b, float hp, string desc) =>
            new Row { Region = region, Role = role, Name = name, Shape = shape, Color = (r, g, b), Hp = hp, Desc = desc };

        private const ArcanaRole N = ArcanaRole.Normal, F = ArcanaRole.Field, RA = ArcanaRole.Rare, B = ArcanaRole.FieldBoss;
        private const MonsterShape Slime = MonsterShape.Slime, Bug = MonsterShape.Bug, Ghost = MonsterShape.Ghost, Serpent = MonsterShape.Serpent,
            Mimic = MonsterShape.Mimic, King = MonsterShape.King, Fish = MonsterShape.Fish, Bat = MonsterShape.Bat, Mushroom = MonsterShape.Mushroom,
            Robot = MonsterShape.Robot, Eyeball = MonsterShape.Eyeball, Rock = MonsterShape.Rock, Jelly = MonsterShape.Jelly, Bird = MonsterShape.Bird,
            Scorpion = MonsterShape.Scorpion, Crystal = MonsterShape.Crystal;

        private static readonly Row[] Rows =
        {
            // ===== ルーネ村（森・虫・キノコ）／旅の道：まよいの森 =====
            R(0, N, "プチアルカ", Slime, 120, 200, 120, 0.85f, "いちばんよく見かける小さなアルカナ。コードを書きかえられて、少し気があらくなっている。"),
            R(0, N, "ミスタイプン", Bug, 200, 200, 110, 1.05f, "打ちまちがいが大好物。あわてて打つと、どこからともなく あらわれる。"),
            R(0, N, "セミコロンバット", Bat, 150, 120, 200, 0.9f, "「;」の形のつばさで飛び回る。すばやく こうげきしてくる。"),
            R(0, N, "タイポキノコ", Mushroom, 220, 110, 90, 1.3f, "かさのもようが、まちがった文字に見えるキノコ。のんびりだが体力が多い。"),
            R(0, N, "コケスライム", Slime, 80, 150, 80, 1.0f, "こけをまとったスライム。森のしめった所が大好き。"),
            R(0, N, "ドングリン", Bug, 160, 110, 60, 1.1f, "どんぐりのような かたいこうら。ころころ転がって体当たりしてくる。"),
            R(0, N, "コノハバット", Bat, 110, 180, 90, 0.9f, "木の葉にまぎれるコウモリ。風がふくと いっしょに飛んでくる。"),
            R(0, N, "ドクダケ", Mushroom, 160, 90, 190, 1.0f, "むらさきのかさのキノコ。胞子をすうと くしゃみが止まらない。"),
            R(0, N, "モリノメ", Eyeball, 120, 190, 110, 0.95f, "木のうろから じっとのぞく目玉。見られると道に迷う。"),
            R(0, N, "カブトン", Bug, 170, 60, 50, 1.25f, "大きな角のカブトムシ。力くらべが大好き。"),
            R(0, N, "イモムシン", Serpent, 160, 220, 110, 1.0f, "葉っぱを もぐもぐ食べるイモムシ。いつか ちょうになるらしい。"),
            R(0, N, "ツタオバケ", Ghost, 100, 170, 100, 1.05f, "つるがからまった おばけ。足元にからみついてくる。"),
            R(0, F, "マヨイダケ", Mushroom, 80, 190, 190, 1.0f, "青く光るキノコ。光を追いかけると、道をまちがえてしまう。"),
            R(0, F, "ヨミフクロウ", Bird, 140, 110, 80, 0.95f, "夜の森で道を見はるフクロウ。首をぐるりと回して見つめる。"),
            R(0, F, "コダマ", Ghost, 220, 240, 210, 0.9f, "森の声のアルカナ。よびかけると、まねをして返事する。"),
            R(0, F, "ネッコロック", Rock, 120, 130, 90, 1.25f, "木の根がからみついた岩。森の道をふさいでいる。"),
            R(0, RA, "ゴールデンプチ", Slime, 250, 205, 60, 1.0f, "金色にかがやくプチアルカ。見つけたら幸運のしるし！"),
            R(0, RA, "ニジイロカブト", Bug, 230, 90, 200, 1.0f, "虹色にかがやく角のカブトムシ。めったに姿を見せない。"),
            R(0, RA, "ホシフリダケ", Mushroom, 255, 210, 90, 1.0f, "かさから星の粉をふらせる金色のキノコ。"),
            R(0, B, "モリノヌシ", King, 90, 160, 80, 1.0f, "まよいの森を守る大きなスライム。兵器の首輪のせいで、目をさましてしまった。"),

            // ===== 水の国アクアリス（水・魚・クラゲ）／旅の道：きりの湿原 =====
            R(1, N, "グルグルン", Bug, 230, 150, 70, 0.9f, "同じところを ぐるぐる回る。止まった水車のまわりに すみついている。"),
            R(1, N, "ムゲンループ", Serpent, 70, 140, 230, 1.15f, "自分のしっぽを追いかけて、いつまでも終わらない水ヘビ。止めるには条件が大事。"),
            R(1, N, "アワクラゲ", Jelly, 140, 210, 240, 1.0f, "せき止められた川で ふえた泡のクラゲ。ぷかぷか同じ動きをくり返す。"),
            R(1, N, "グルグルコウモリ", Bat, 100, 90, 160, 0.95f, "水車のまわりを 輪をえがいて飛ぶ。目が回るほど速い。"),
            R(1, N, "シズクスライム", Slime, 120, 190, 250, 0.9f, "水のしずくのスライム。ぽたぽた落ちては また集まる。"),
            R(1, N, "カワガニン", Bug, 220, 90, 70, 1.15f, "川辺のカニのアルカナ。横歩きが とくい。"),
            R(1, N, "ウズマキガイ", Rock, 200, 180, 220, 1.3f, "うずまきの貝がら。中で 同じ音が ずっとくり返し鳴っている。"),
            R(1, N, "ナミノリウオ", Fish, 90, 150, 230, 0.95f, "波にのって 何度もとびはねる魚。"),
            R(1, N, "スイシャメダマ", Eyeball, 80, 160, 220, 1.0f, "水車のように ぐるぐる回る目玉。"),
            R(1, N, "アマガッパ", Ghost, 110, 200, 170, 1.05f, "雨の日にあらわれる水のおばけ。"),
            R(1, N, "カワセミン", Bird, 60, 170, 220, 0.9f, "水にとびこむ青い鳥。魚を見つけると目が光る。"),
            R(1, N, "ミズクミロボ", Robot, 90, 140, 200, 1.2f, "水をくみ続けるロボット。止まり方を わすれてしまった。"),
            R(1, F, "キリモヤ", Ghost, 200, 210, 220, 0.95f, "霧のかたまりのアルカナ。気づくと まわりをかこまれている。"),
            R(1, F, "ヌマスライム", Slime, 100, 140, 80, 1.15f, "ぬまの どろのスライム。足がしずんで動きにくくなる。"),
            R(1, F, "ハスノハクラゲ", Jelly, 140, 200, 140, 1.0f, "はすの葉を かさにしたクラゲ。"),
            R(1, F, "サギドリ", Bird, 230, 230, 240, 0.95f, "一本足で立つ白い鳥。じっと待ってから 一気にこうげきする。"),
            R(1, RA, "メビウスヘビ", Serpent, 240, 120, 220, 1.0f, "表も裏もない、ふしぎな輪のヘビ。ムゲンループの親分らしい。"),
            R(1, RA, "シンジュガイ", Mimic, 250, 240, 230, 1.0f, "真珠をかかえた貝の宝箱。中の真珠は とてもきれい。"),
            R(1, RA, "スイリュウ", Serpent, 60, 220, 230, 1.0f, "水の流れそのもののような竜。めったに姿をあらわさない。"),
            R(1, B, "ダムノヌシ", Rock, 90, 110, 170, 1.0f, "川をせき止めた 大きな岩のアルカナ。水車が止まったのは このせい。"),

            // ===== 風の国シルフィード（鳥・風・雲）／旅の道：風の峡谷 =====
            R(2, N, "イフスライム", Slime, 110, 200, 220, 0.9f, "「もし〜なら」で形が変わるスライム。風がふくと やわらかくなる。"),
            R(2, N, "ブンキメダマ", Eyeball, 240, 200, 90, 1.0f, "分かれ道を どっちも見はる大きな目玉。見られると ドキッとする。"),
            R(2, N, "カザミドリ", Bird, 170, 230, 160, 1.05f, "風の向きで 進む道を決める鳥。兵器にされて、どちらにも行けず暴れている。"),
            R(2, N, "エルスゴースト", Ghost, 170, 120, 230, 1.1f, "「でなければ」のほうから ふっと出てくる、ゆうれい。"),
            R(2, N, "クモスライム", Slime, 240, 245, 255, 0.95f, "雲のスライム。ふわふわ空にういている。"),
            R(2, N, "ツムジバット", Bat, 150, 210, 190, 0.9f, "つむじ風にのって飛ぶコウモリ。"),
            R(2, N, "カゼキリドリ", Bird, 120, 200, 240, 1.0f, "風を切って まっすぐ飛ぶ鳥。とても速い。"),
            R(2, N, "シルベロボ", Robot, 180, 200, 120, 1.2f, "道しるべのロボット。右か左か、いつも まよっている。"),
            R(2, N, "タンポポダケ", Mushroom, 250, 220, 90, 0.95f, "わた毛を風にとばすキノコ。"),
            R(2, N, "カゼヘビ", Serpent, 160, 220, 120, 1.05f, "風にのって 空をおよぐヘビ。"),
            R(2, N, "テンキバコ", Mimic, 130, 180, 230, 1.15f, "あけるたびに 天気が変わる ふしぎな箱。"),
            R(2, N, "カミナリクラゲ", Jelly, 250, 230, 90, 1.0f, "ピリピリしびれる 雲のクラゲ。"),
            R(2, F, "タツマキン", Serpent, 200, 220, 230, 1.0f, "小さな竜巻のアルカナ。まきこまれると 目が回る。"),
            R(2, F, "イワワシ", Bird, 160, 130, 100, 1.1f, "がけの上に すむワシ。岩のような かたいつばさ。"),
            R(2, F, "スナカゼゴースト", Ghost, 220, 200, 160, 0.95f, "砂まじりの風のおばけ。峡谷のおくから ふいてくる。"),
            R(2, F, "ガケロック", Rock, 170, 160, 150, 1.25f, "がけから転がり落ちてきた岩のアルカナ。"),
            R(2, RA, "トゥルーファルス", Mimic, 80, 220, 140, 1.0f, "「はい」と「いいえ」を気まぐれに入れかえる、なぞの宝箱。"),
            R(2, RA, "ニジドリ", Bird, 255, 140, 200, 1.0f, "虹色のはねの鳥。雨上がりにだけ 姿を見せる。"),
            R(2, RA, "ソラクジラ", Fish, 150, 190, 250, 1.0f, "空をおよぐクジラの子。雲の上で くらしている。"),
            R(2, B, "テンクウノヌシ", Bird, 240, 230, 120, 1.0f, "風の峡谷の空を守る 大きな金の鳥。"),

            // ===== 砂の国サンドリア（砂・サソリ・岩）／旅の道：かれた鉱山 =====
            R(3, N, "エンドレスサソリ", Scorpion, 210, 160, 90, 1.05f, "ゴールが見えるまで 砂の中をどこまでも進むサソリ。しっぽの針がするどい。"),
            R(3, N, "シンキロウ", Ghost, 240, 210, 150, 0.9f, "砂漠のかげろうから生まれた まぼろし。あると思えばない、ないと思えばある。"),
            R(3, N, "スタックゴーレム", Rock, 180, 140, 100, 1.3f, "砂岩を つみ上げて できた岩のアルカナ。つみすぎると くずれる。"),
            R(3, N, "スナスライム", Slime, 220, 190, 120, 0.95f, "砂のスライム。あつい日は さらさらになる。"),
            R(3, N, "サボテンダケ", Mushroom, 110, 170, 90, 1.0f, "サボテンに そっくりなキノコ。トゲに注意。"),
            R(3, N, "スナザメ", Fish, 200, 170, 110, 1.1f, "砂の中を およぐサメ。背びれだけが 見えている。"),
            R(3, N, "ミイラゴースト", Ghost, 230, 220, 190, 1.05f, "ほうたいを まいたおばけ。ゴールに着くまで 追いかけてくる。"),
            R(3, N, "スカラベン", Bug, 60, 140, 120, 0.95f, "玉を転がし続ける虫のアルカナ。止まることを知らない。"),
            R(3, N, "オアシスクラゲ", Jelly, 110, 200, 220, 0.9f, "オアシスの水から生まれたクラゲ。"),
            R(3, N, "ハゲタカン", Bird, 140, 100, 80, 1.0f, "砂漠の空を まわり続けるタカ。"),
            R(3, N, "ピラミッドロック", Rock, 230, 200, 120, 1.25f, "ピラミッドの形の岩。中に なにかかくれているらしい。"),
            R(3, N, "ツボモドキ", Mimic, 190, 120, 70, 1.1f, "古いつぼに 化けている。近づくと とびかかってくる。"),
            R(3, F, "トロッコロボ", Robot, 150, 110, 80, 1.15f, "鉱山のレールを 走り続けるロボット。"),
            R(3, F, "コウセキゴーレム", Rock, 120, 140, 170, 1.25f, "鉱石が かたまってできた岩のアルカナ。"),
            R(3, F, "モグラムシ", Bug, 110, 90, 70, 0.95f, "土の中を ほり進む虫。いきなり足元から 出てくる。"),
            R(3, F, "カンテラゴースト", Ghost, 250, 200, 120, 0.9f, "古いカンテラの火のおばけ。くらい坑道を ただよう。"),
            R(3, RA, "キンノサソリ", Scorpion, 255, 215, 70, 1.0f, "砂漠の砂金を食べて 金色になったサソリ。めったに砂から出てこない。"),
            R(3, RA, "ルビースカラベ", Bug, 230, 50, 80, 1.0f, "ルビーのように赤い こうらの虫。"),
            R(3, RA, "オウゴンゾウ", Rock, 250, 210, 100, 1.0f, "金の像のアルカナ。遺跡の おくに いるらしい。"),
            R(3, B, "サバクノヌシ", Scorpion, 180, 80, 50, 1.0f, "砂あらしの中にいる 大きなサソリ。鉱山の入り口を ふさいでいる。"),

            // ===== 機械の国ギアルド（機械・歯車・ロボ）／旅の道：星見の山道 =====
            R(4, N, "リピートロボ", Robot, 150, 160, 180, 0.95f, "工場で改造されたアルカナ。同じ命令を何回でも くり返す。"),
            R(4, N, "ヘンスウモドキ", Mimic, 230, 110, 170, 1.05f, "変数の箱に化けている。開けると中身ではなく、キバが出てくる。"),
            R(4, N, "ジョウケンロボ", Robot, 90, 180, 170, 1.15f, "数を調べてから動く、かしこいロボット。守りがかたい。"),
            R(4, N, "ハグルマムシ", Bug, 180, 180, 190, 1.0f, "歯車の こうらの虫。カチカチ音を立てる。"),
            R(4, N, "ネジスライム", Slime, 160, 170, 180, 0.9f, "ネジが ささったスライム。"),
            R(4, N, "カウンターロボ", Robot, 220, 140, 60, 1.1f, "数を数えるロボット。数えまちがえると おこる。"),
            R(4, N, "スチームゴースト", Ghost, 200, 200, 210, 0.95f, "工場の湯気のおばけ。あつい息を ふきかける。"),
            R(4, N, "コイルクラゲ", Jelly, 120, 200, 250, 1.0f, "電気のコイルの足をもつクラゲ。"),
            R(4, N, "ドローンバット", Bat, 90, 100, 120, 0.9f, "プロペラで飛ぶ 機械のコウモリ。"),
            R(4, N, "センサーメダマ", Eyeball, 230, 60, 60, 1.0f, "工場の見はりカメラの目玉。"),
            R(4, N, "コンテナモドキ", Mimic, 110, 120, 140, 1.2f, "荷物の箱に化けたアルカナ。"),
            R(4, N, "テツヘビ", Serpent, 140, 150, 160, 1.05f, "ベルトコンベアのような 鉄のヘビ。"),
            R(4, F, "ヤマビコ", Ghost, 180, 190, 230, 0.95f, "山の声のおばけ。同じことばを 何度も返してくる。"),
            R(4, F, "ホシマネキ", Bird, 90, 90, 160, 1.0f, "夜空の星を まねく鳥。"),
            R(4, F, "イワボシ", Rock, 130, 120, 170, 1.25f, "星のかけらが 岩になったアルカナ。"),
            R(4, F, "ヨゾラバット", Bat, 40, 40, 90, 0.9f, "夜空にまぎれる まっ黒なコウモリ。"),
            R(4, RA, "メモリリーク", Ghost, 200, 60, 90, 1.0f, "工場で記憶をこぼしながら さまようゆうれい。改造に失敗したアルカナらしい。"),
            R(4, RA, "オーバークロック", Robot, 250, 80, 80, 1.0f, "速く動かしすぎて まっ赤になったロボット。"),
            R(4, RA, "ゴールドギア", Bug, 250, 200, 60, 1.0f, "金の歯車でできた虫。とても めずらしい。"),
            R(4, B, "コウジョウノヌシ", Robot, 100, 110, 130, 1.0f, "アルカナを改造する工場の 大きなロボット。"),

            // ===== 星の国ステラリア（星・結晶・ゆうれい）／旅の道：忘れられた遺跡 =====
            R(5, N, "ナルポインタ", Ghost, 90, 90, 110, 1.05f, "何もない場所を指さす、こわいゆうれい。落ちてくる星の音にまぎれて あらわれる。"),
            R(5, N, "オーバーフロー", Eyeball, 90, 200, 230, 0.95f, "数が大きくなりすぎて、あふれ出した目玉。こうげきが とても速い。"),
            R(5, N, "スターコール", Crystal, 250, 230, 120, 1.15f, "呪文を組み合わせて 星を呼ぶ結晶のアルカナ。守り神の力を あびて光っている。"),
            R(5, N, "ホシクズスライム", Slime, 150, 130, 230, 0.9f, "星くずを まぜたスライム。夜になると光る。"),
            R(5, N, "リュウセイバット", Bat, 120, 110, 200, 0.9f, "流れ星のように 飛ぶコウモリ。"),
            R(5, N, "ツキノメ", Eyeball, 240, 230, 150, 1.0f, "月のような 丸い目玉。"),
            R(5, N, "セイウンクラゲ", Jelly, 170, 120, 240, 1.0f, "星雲のような もようのクラゲ。"),
            R(5, N, "ミカヅキドリ", Bird, 230, 220, 120, 1.0f, "三日月の形の はねをもつ鳥。"),
            R(5, N, "ルーンゴースト", Ghost, 120, 180, 250, 1.1f, "古いルーンを となえ続ける おばけ。"),
            R(5, N, "スイショウゴーレム", Rock, 170, 220, 250, 1.3f, "水晶でできた 岩のアルカナ。"),
            R(5, N, "ホシバコ", Mimic, 90, 80, 150, 1.1f, "星の宝箱に 化けたアルカナ。"),
            R(5, N, "ヨビダシヘビ", Serpent, 210, 160, 250, 1.05f, "名前をよぶと とんでくるヘビ。関数みたいな アルカナ。"),
            R(5, F, "イセキゴーレム", Rock, 150, 150, 130, 1.25f, "遺跡を守る 古い石のアルカナ。"),
            R(5, F, "ワスレゴースト", Ghost, 160, 160, 190, 0.95f, "自分が だれだったか わすれてしまった おばけ。"),
            R(5, F, "コダイメダマ", Eyeball, 200, 180, 120, 1.0f, "古代の遺跡のかべに うまった目玉。"),
            R(5, F, "ルーンクリスタル", Crystal, 120, 250, 220, 1.1f, "ルーンの文字が うかぶ結晶。"),
            R(5, RA, "ミラクルスター", Crystal, 255, 170, 230, 1.0f, "流れ星が結晶になったアルカナ。出会えたら願いがかなうと言われている。"),
            R(5, RA, "ニジクラゲ", Jelly, 180, 140, 255, 1.0f, "夜空をただよう虹色のクラゲ。光るたびに 色が変わる。"),
            R(5, RA, "ギンガクジラ", Fish, 60, 60, 140, 1.0f, "銀河を およぐ大きなクジラ。"),
            R(5, B, "イセキノヌシ", Crystal, 200, 80, 220, 1.0f, "忘れられた遺跡を守る 大きな結晶。守り神の力を あびている。"),
        };

        // ------------------------------------------------------------------
        // 村ごとの強さ：「その村のめやすのランク・装備のプレイヤーが、ギリギリ勝てるかな？」に合わせる
        // （風の国シルフィードは ランク50で勝てるかな？くらい）
        // ------------------------------------------------------------------

        /// <summary>村ごとの めやすのランク（このランクで ギリギリ勝てるかな？くらい）</summary>
        public static readonly int[] TargetRank = { 1, 25, 50, 65, 80, 95 };
        /// <summary>そのころ持っていそうな杖の攻撃力（木 → 見習い → 風 → 水晶 → ルーン → 星）</summary>
        private static readonly int[] ExpectedWeapon = { 0, 1, 3, 4, 6, 7 };
        /// <summary>そのころ着ていそうなローブ（体力・守り）（布 → 革 → 鉄 → 風 → 魔法 → 星）</summary>
        private static readonly int[] ExpectedArmorHp = { 0, 10, 20, 28, 35, 55 };
        private static readonly int[] ExpectedArmorDef = { 0, 1, 2, 3, 4, 6 };

        /// <summary>1秒あたりのダメージ ＝ 攻撃力 × これ（1秒に1.5文字くらい打つ ＋ 言葉を打ち終わったボーナス）</summary>
        public const float DamagePerAttack = 2.0f;

        private static readonly int[] BaseCoins = { 6, 14, 24, 34, 46, 56 };
        private static readonly float[] BaseInterval = { 7f, 6.6f, 6.2f, 5.8f, 5.5f, 5.2f };

        public static int RegionCount => TargetRank.Length;

        /// <summary>その村のめやすのランクのプレイヤーの強さ（weapon を指定すると その杖を持っているとき）</summary>
        public static (int attack, int maxHp, int defense) ExpectedPlayer(int region, int weapon = -1)
        {
            int rank = TargetRank[region];
            if (weapon < 0) weapon = ExpectedWeapon[region];
            // PlayerStats と同じ計算
            return (1 + weapon + (rank - 1) / 5, 30 + (rank - 1) * 4 + ExpectedArmorHp[region], ExpectedArmorDef[region]);
        }

        /// <summary>
        /// プレイヤーが fightSeconds 秒 打ち続けると たおせる体力と、
        /// そのあいだに 体力の最大値 × hpShare くらい へらすこうげき力を決める
        /// </summary>
        public static (int hp, int attack) Tune((int attack, int maxHp, int defense) p, float fightSeconds, float hpShare, float interval)
        {
            int hp = System.Math.Max(10, (int)System.Math.Round(p.attack * DamagePerAttack * fightSeconds));
            int perHit = System.Math.Max(2, (int)System.Math.Ceiling(p.maxHp * hpShare * interval / fightSeconds));
            return (hp, perHit + p.defense); // 守りで へるぶんを 足しておく
        }

        // 種類ごと：バトルの長さ（秒）・そのあいだに へらされる体力の割合
        private const float NormalSeconds = 20f, FieldSeconds = 22f, RareSeconds = 30f, BossSeconds = 45f;
        private const float NormalShare = 0.6f, FieldShare = 0.65f, RareShare = 0.75f, BossShare = 0.85f;

        /// <summary>表から、村ごと・種類ごとのアルカナを作る</summary>
        public static List<EnemyInfo>[,] Build()
        {
            var lists = new List<EnemyInfo>[RegionCount, 4];
            for (int r = 0; r < RegionCount; r++) for (int k = 0; k < 4; k++) lists[r, k] = new List<EnemyInfo>();
            foreach (var row in Rows)
            {
                int r = row.Region;
                float coins = BaseCoins[r] * (0.8f + row.Hp * 0.2f);
                float interval = BaseInterval[r];
                // 形ごとの くせ：飛ぶものは速く、かたいものは ゆっくり
                if (row.Shape == Bat || row.Shape == Bird || row.Shape == Eyeball) interval -= 0.4f;
                if (row.Shape == Rock || row.Shape == Mimic || row.Shape == Robot) interval += 0.3f;
                float seconds = NormalSeconds, share = NormalShare;
                switch (row.Role)
                {
                    case ArcanaRole.Field: seconds = FieldSeconds; share = FieldShare; coins *= 1.1f; break;
                    case ArcanaRole.Rare: seconds = RareSeconds; share = RareShare; coins *= 5.5f; interval -= 0.3f; break;
                    case ArcanaRole.FieldBoss: seconds = BossSeconds; share = BossShare; coins *= 10f; interval -= 0.4f; break;
                }
                // こうげきは ふつうの長さで決めて、体力だけ 倍率をかける（体力の多いアルカナは 長引くぶん手ごわい）
                var (hp, atk) = Tune(ExpectedPlayer(r), seconds, share, interval);
                hp = System.Math.Max(10, (int)System.Math.Round(hp * row.Hp));
                lists[r, (int)row.Role].Add(new EnemyInfo
                {
                    Name = row.Name, Shape = row.Shape, Color = row.Color, Description = row.Desc,
                    Hp = hp, Attack = atk, Interval = interval, Coins = (int)System.Math.Round(coins),
                    Rare = row.Role == ArcanaRole.Rare, Role = row.Role, Region = r,
                });
            }
            return lists;
        }
    }
}
