using System;

namespace RuneCodeQuest
{
    /// <summary>
    /// 効果音と BGM を、プログラムで作る（音のファイルはいらない。Unity の機能も使っていません）。
    /// どれも -1〜1 の数の並び（1秒に Rate 個）を返す。鳴らすのは SoundKit。
    /// 音の高さは MIDI の番号（60 = ド（C4）、69 = ラ（A4、440Hz））で書く。
    /// </summary>
    public static class SoundSynth
    {
        public const int Rate = 22050;

        public enum Wave { Square, Triangle, Sine, Noise }

        public static double Freq(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

        private static float Osc(Wave w, double cycles, Random rng)
        {
            double t = cycles - Math.Floor(cycles);
            switch (w)
            {
                case Wave.Square: return t < 0.5 ? 1f : -1f;
                case Wave.Triangle: return (float)(t < 0.5 ? 4 * t - 1 : 3 - 4 * t);
                case Wave.Sine: return (float)Math.Sin(cycles * 2 * Math.PI);
                default: return (float)(rng.NextDouble() * 2 - 1);
            }
        }

        /// <summary>
        /// 音を1つ data に足す。start（秒）から seconds のあいだ、freq から freqEnd へ高さを変えながら鳴らす。
        /// はじめは すっと大きく、だんだん小さくなる（プツッという音が出ないように）
        /// </summary>
        public static void Add(float[] data, double start, double seconds, double freq, double freqEnd, Wave w, float volume, Random rng, double decay = 3)
        {
            int s0 = (int)(start * Rate), n = (int)(seconds * Rate);
            double phase = 0;
            for (int i = 0; i < n && s0 + i < data.Length; i++)
            {
                double t = i / (double)n;
                double f = freq + (freqEnd - freq) * t;
                phase += f / Rate;
                double attack = Math.Min(1, i / (Rate * 0.004));
                double env = attack * Math.Exp(-decay * t) * (1 - Math.Pow(t, 8));
                data[s0 + i] += (float)(Osc(w, phase, rng) * volume * env);
            }
        }

        private static float[] Make(double seconds, Action<float[], Random> draw)
        {
            var data = new float[(int)(seconds * Rate)];
            draw(data, new Random(7));
            for (int i = 0; i < data.Length; i++) data[i] = Math.Max(-1f, Math.Min(1f, data[i]));
            return data;
        }

        // ------------------------------------------------------------------
        // 効果音
        // ------------------------------------------------------------------

        /// <summary>キーを打った音（カチッ）</summary>
        public static float[] Key() => Make(0.04, (d, r) =>
        {
            Add(d, 0, 0.035, 1800, 1400, Wave.Triangle, 0.35f, r, 8);
            Add(d, 0, 0.012, 0, 0, Wave.Noise, 0.12f, r, 6);
        });

        /// <summary>まちがえた音（ブッ）</summary>
        public static float[] Miss() => Make(0.16, (d, r) =>
        {
            Add(d, 0, 0.07, 200, 180, Wave.Square, 0.18f, r, 1);
            Add(d, 0.075, 0.08, 150, 130, Wave.Square, 0.18f, r, 2);
        });

        /// <summary>風船が割れた音（パーン！）</summary>
        public static float[] Pop() => Make(0.3, (d, r) =>
        {
            Add(d, 0, 0.22, 0, 0, Wave.Noise, 0.55f, r, 7);
            Add(d, 0, 0.12, 900, 200, Wave.Sine, 0.35f, r, 4);
            Add(d, 0.05, 0.2, Freq(84), Freq(91), Wave.Triangle, 0.12f, r, 4);
        });

        /// <summary>言葉を打ち終わった音（キラーン）</summary>
        public static float[] Chime() => Make(0.3, (d, r) =>
        {
            Add(d, 0.00, 0.12, Freq(84), Freq(84), Wave.Triangle, 0.25f, r, 3);
            Add(d, 0.06, 0.12, Freq(88), Freq(88), Wave.Triangle, 0.25f, r, 3);
            Add(d, 0.12, 0.18, Freq(91), Freq(91), Wave.Triangle, 0.25f, r, 3);
        });

        /// <summary>アルカナにこうげきされた音（ドン）</summary>
        public static float[] Hurt() => Make(0.25, (d, r) =>
        {
            Add(d, 0, 0.2, 140, 60, Wave.Square, 0.22f, r, 4);
            Add(d, 0, 0.1, 0, 0, Wave.Noise, 0.25f, r, 6);
        });

        /// <summary>道を歩く音（トッ）</summary>
        public static float[] Step() => Make(0.06, (d, r) =>
        {
            Add(d, 0, 0.06, 130, 80, Wave.Triangle, 0.6f, r, 5);
            Add(d, 0, 0.025, 0, 0, Wave.Noise, 0.14f, r, 7);
        });

        /// <summary>草むらを歩く音（ガサッ）</summary>
        public static float[] GrassStep() => Make(0.09, (d, r) =>
        {
            Add(d, 0, 0.09, 0, 0, Wave.Noise, 0.4f, r, 5);
        });

        /// <summary>しゃべっている音（ポッ）。会話の文字が出るたびに鳴らす</summary>
        public static float[] Talk() => Make(0.05, (d, r) =>
        {
            Add(d, 0, 0.045, 620, 580, Wave.Square, 0.14f, r, 3);
        });

        /// <summary>バトルに勝った音（ファンファーレ）</summary>
        public static float[] Win() => Make(1.2, (d, r) =>
        {
            int[] notes = { 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++) Add(d, i * 0.11, 0.14, Freq(notes[i]), Freq(notes[i]), Wave.Square, 0.12f, r, 2);
            foreach (int n in new[] { 72, 76, 79, 84 }) Add(d, 0.48, 0.7, Freq(n), Freq(n), Wave.Triangle, 0.12f, r, 2);
        });

        /// <summary>バトルに負けた音（しょんぼり）</summary>
        public static float[] Lose() => Make(1.3, (d, r) =>
        {
            int[] notes = { 67, 66, 65, 64 };
            for (int i = 0; i < notes.Length; i++) Add(d, i * 0.25, 0.3, Freq(notes[i]), Freq(notes[i]) * 0.98, Wave.Triangle, 0.22f, r, 1.5);
            Add(d, 0.75, 0.5, Freq(52), Freq(50), Wave.Square, 0.08f, r, 2);
        });

        /// <summary>クエストクリアのファンファーレ（大きめ）</summary>
        public static float[] QuestClear() => Make(1.6, (d, r) =>
        {
            int[] notes = { 67, 72, 76, 79, 76, 79 };
            double[] at = { 0, 0.12, 0.24, 0.36, 0.6, 0.72 };
            for (int i = 0; i < notes.Length; i++) Add(d, at[i], i == notes.Length - 1 ? 0.8 : 0.16, Freq(notes[i]), Freq(notes[i]), Wave.Square, 0.12f, r, i == notes.Length - 1 ? 1.5 : 2);
            foreach (int n in new[] { 60, 64, 67, 72 }) Add(d, 0.72, 0.85, Freq(n), Freq(n), Wave.Triangle, 0.1f, r, 1.5);
        });

        /// <summary>クエストで目標にとどかなかった音</summary>
        public static float[] QuestFail() => Make(0.6, (d, r) =>
        {
            Add(d, 0, 0.22, Freq(64), Freq(64), Wave.Square, 0.1f, r, 2);
            Add(d, 0.24, 0.32, Freq(60), Freq(58), Wave.Square, 0.1f, r, 2);
        });

        /// <summary>「▶ 起動」の音（ギュイーン）</summary>
        public static float[] Launch() => Make(0.35, (d, r) =>
        {
            Add(d, 0, 0.3, 300, 1200, Wave.Square, 0.08f, r, 1);
            Add(d, 0, 0.3, 302, 1205, Wave.Triangle, 0.12f, r, 1);
        });

        /// <summary>ゴーレムが1歩うごく音（ズン）</summary>
        public static float[] GolemStep() => Make(0.1, (d, r) =>
        {
            Add(d, 0, 0.09, 90, 55, Wave.Square, 0.16f, r, 5);
        });

        /// <summary>ねこの鳴き声（にゃーん）</summary>
        public static float[] Meow() => Make(0.45, (d, r) =>
        {
            Add(d, 0, 0.18, 650, 1050, Wave.Triangle, 0.3f, r, 0.5);
            Add(d, 0.16, 0.26, 1050, 560, Wave.Triangle, 0.3f, r, 2);
        });

        /// <summary>噴水に金貨を投げる音（チャリン…ポチャン）</summary>
        public static float[] Coin() => Make(0.5, (d, r) =>
        {
            Add(d, 0, 0.08, Freq(96), Freq(96), Wave.Square, 0.1f, r, 4);
            Add(d, 0.06, 0.12, Freq(100), Freq(100), Wave.Square, 0.1f, r, 4);
            Add(d, 0.25, 0.2, 500, 180, Wave.Sine, 0.3f, r, 4);
        });

        /// <summary>数が1つ増える音（ピッ）。結果のカウントアップに使う</summary>
        public static float[] Tick() => Make(0.04, (d, r) =>
        {
            Add(d, 0, 0.035, 1400, 1400, Wave.Square, 0.16f, r, 4);
        });

        /// <summary>はんこを押す音（ドン！キラッ）</summary>
        public static float[] Stamp() => Make(0.5, (d, r) =>
        {
            Add(d, 0, 0.18, 110, 50, Wave.Square, 0.25f, r, 5);
            Add(d, 0, 0.08, 0, 0, Wave.Noise, 0.3f, r, 8);
            Add(d, 0.12, 0.3, Freq(96), Freq(96), Wave.Triangle, 0.12f, r, 4);
        });

        // ------------------------------------------------------------------
        // BGM（くり返して流す）
        // ------------------------------------------------------------------

        /// <summary>
        /// 8分音符ごとの メロディー（ピコピコ）・ベース（やわらかい音）・リズム（シャッ）で曲を作る。0 は休み
        /// </summary>
        public static float[] Music(int[] melody, int[] bass, int bpm)
        {
            double eighth = 60.0 / bpm / 2;
            return Make(eighth * melody.Length, (d, r) =>
            {
                for (int i = 0; i < melody.Length; i++)
                {
                    double t = i * eighth;
                    if (melody[i] > 0) Add(d, t, eighth * 0.9, Freq(melody[i]), Freq(melody[i]), Wave.Square, 0.07f, r, 2);
                    if (bass[i] > 0) Add(d, t, eighth * 0.95, Freq(bass[i]), Freq(bass[i]), Wave.Triangle, 0.22f, r, 1);
                    Add(d, t, 0.03, 0, 0, Wave.Noise, i % 2 == 1 ? 0.05f : 0.025f, r, 8);
                }
            });
        }

        private static int[] Bass(params int[] roots)
        {
            // 1小節（8分音符8つ）ごとに、根音と1オクターブ上を交互に
            var b = new int[roots.Length * 8];
            for (int bar = 0; bar < roots.Length; bar++)
                for (int k = 0; k < 8; k++) b[bar * 8 + k] = roots[bar] + (k % 2 == 0 ? 0 : 12);
            return b;
        }

        /// <summary>風船割りの BGM（明るいハ長調・速め）</summary>
        public static float[] BalloonBgm() => Music(new[]
        {
            76, 79, 84, 79, 76, 79, 81, 79,
            77, 81, 84, 81, 79, 76, 74, 72,
            76, 79, 84, 79, 81, 84, 88, 84,
            86, 84, 83, 81, 79, 77, 76, 0,
        }, Bass(48, 53, 48, 43), 140);

        /// <summary>タイトルの BGM（はじまりのわくわく）</summary>
        public static float[] TitleBgm() => Music(new[]
        {
            72, 0, 76, 79, 84, 0, 79, 76,
            77, 0, 81, 84, 81, 0, 77, 74,
            76, 0, 79, 84, 88, 86, 84, 79,
            81, 79, 77, 74, 72, 0, 0, 0,
        }, Bass(48, 53, 48, 43), 110);

        /// <summary>町の BGM（のんびり）</summary>
        public static float[] TownBgm() => Music(new[]
        {
            72, 0, 74, 76, 79, 0, 76, 0,
            77, 0, 76, 74, 72, 0, 69, 0,
            71, 0, 72, 74, 76, 0, 74, 0,
            72, 0, 71, 0, 72, 0, 0, 0,
        }, Bass(48, 53, 55, 48), 96);

        /// <summary>遺跡のエリアの BGM（ぼうけん）</summary>
        public static float[] AreaBgm() => Music(new[]
        {
            62, 65, 69, 74, 72, 69, 65, 67,
            69, 72, 74, 77, 76, 74, 72, 69,
            62, 65, 69, 74, 76, 77, 79, 76,
            74, 72, 69, 67, 69, 0, 62, 0,
        }, Bass(38, 46, 41, 45), 124);

        /// <summary>宿舎の BGM（ほっとする）</summary>
        public static float[] InnBgm() => Music(new[]
        {
            79, 0, 0, 78, 79, 0, 74, 0,
            76, 0, 0, 74, 72, 0, 71, 0,
            72, 0, 0, 74, 76, 0, 79, 0,
            78, 0, 76, 0, 74, 0, 0, 0,
        }, Bass(43, 48, 48, 50), 84);

        /// <summary>クエストの BGM（考えごと）</summary>
        public static float[] QuestBgm() => Music(new[]
        {
            72, 0, 79, 0, 76, 0, 79, 0,
            74, 0, 79, 0, 77, 0, 79, 0,
            72, 0, 79, 0, 76, 0, 81, 0,
            79, 0, 77, 0, 74, 0, 0, 0,
        }, Bass(48, 43, 45, 43), 100);

        /// <summary>バトルの BGM（どきどきするイ短調）</summary>
        public static float[] BattleBgm() => Music(new[]
        {
            69, 72, 76, 72, 69, 72, 76, 77,
            76, 74, 72, 71, 69, 71, 72, 74,
            69, 72, 76, 72, 81, 79, 77, 76,
            74, 72, 71, 68, 69, 0, 69, 0,
        }, Bass(45, 41, 43, 40), 150);
    }
}
