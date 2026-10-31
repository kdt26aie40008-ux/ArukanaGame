using System.Collections.Generic;
using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// 効果音と BGM を鳴らす（音は SoundSynth がプログラムで作る）。
    /// 音の ON / OFF は Progress.SoundOn に保存する。
    /// </summary>
    public static class SoundKit
    {
        private static AudioSource sfx, keys, music;
        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static bool On
        {
            get => Progress.SoundOn;
            set
            {
                Progress.SoundOn = value;
                if (!value) StopBgm();
            }
        }

        private static void Ensure()
        {
            if (sfx != null) return;
            var go = new GameObject("Sound");
            Object.DontDestroyOnLoad(go);
            sfx = go.AddComponent<AudioSource>();
            keys = go.AddComponent<AudioSource>();
            music = go.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.45f;
            // 音を聞く耳（AudioListener）がシーンになければつける
            if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
        }

        private static AudioClip Clip(string key, System.Func<float[]> make)
        {
            if (clips.TryGetValue(key, out var c) && c != null) return c;
            var data = make();
            c = AudioClip.Create(key, data.Length, 1, SoundSynth.Rate, false);
            c.SetData(data, 0);
            clips[key] = c;
            return c;
        }

        private static void Play(string key, System.Func<float[]> make, float volume = 1f)
        {
            if (!On) return;
            Ensure();
            sfx.PlayOneShot(Clip(key, make), volume);
        }

        /// <summary>キーを打った音。少しずつ高さを変えて、同じ音ばかりにならないように</summary>
        public static void Key()
        {
            if (!On) return;
            Ensure();
            keys.pitch = Random.Range(0.92f, 1.1f);
            keys.PlayOneShot(Clip("key", SoundSynth.Key), 0.8f);
        }

        public static void Miss() => Play("miss", SoundSynth.Miss);
        public static void Pop() => Play("pop", SoundSynth.Pop);
        public static void Chime() => Play("chime", SoundSynth.Chime, 0.9f);
        public static void Hurt() => Play("hurt", SoundSynth.Hurt);

        public static void Step(bool grass) { if (grass) Play("grass", SoundSynth.GrassStep, 1f); else Play("step", SoundSynth.Step, 1f); }
        public static void Talk() => Play("talk", SoundSynth.Talk, 0.7f);
        public static void Win() => Play("win", SoundSynth.Win);
        public static void Lose() => Play("lose", SoundSynth.Lose);
        public static void QuestClear() => Play("questClear", SoundSynth.QuestClear);
        public static void QuestFail() => Play("questFail", SoundSynth.QuestFail);
        public static void Launch() => Play("launch", SoundSynth.Launch, 0.8f);
        public static void GolemStep() => Play("golemStep", SoundSynth.GolemStep, 0.7f);
        public static void Tick() => Play("tick", SoundSynth.Tick, 0.6f);
        public static void Meow() => Play("meow", SoundSynth.Meow);
        public static void Coin() => Play("coin", SoundSynth.Coin);
        public static void Stamp() => Play("stamp", SoundSynth.Stamp);

        public static void BalloonBgm() => PlayBgm("bgm_balloon", SoundSynth.BalloonBgm);
        public static void BattleBgm() => PlayBgm("bgm_battle", SoundSynth.BattleBgm);
        public static void TitleBgm() => PlayBgm("bgm_title", SoundSynth.TitleBgm);
        public static void TownBgm() => PlayBgm("bgm_town", SoundSynth.TownBgm);
        public static void AreaBgm() => PlayBgm("bgm_area", SoundSynth.AreaBgm);
        public static void InnBgm() => PlayBgm("bgm_inn", SoundSynth.InnBgm);
        public static void QuestBgm() => PlayBgm("bgm_quest", SoundSynth.QuestBgm);

        private static void PlayBgm(string key, System.Func<float[]> make)
        {
            if (!On) return;
            Ensure();
            var c = Clip(key, make);
            if (music.clip == c && music.isPlaying) return;
            music.clip = c;
            music.Play();
        }

        public static void StopBgm()
        {
            if (music != null && music.isPlaying) music.Stop();
        }

        /// <summary>「音 ON / OFF」のボタン（風船割り・バトルの画面に出す）</summary>
        public static void ToggleButton(Rect r)
        {
            if (UIKit.ColorButton(r, On ? "♪ 音 ON" : "音 OFF", On ? new Color(0.35f, 0.6f, 0.85f) : Color.gray, UIKit.SmallButton))
                On = !On;
        }
    }
}
