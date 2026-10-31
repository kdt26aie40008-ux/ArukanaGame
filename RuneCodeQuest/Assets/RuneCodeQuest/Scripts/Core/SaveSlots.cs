using UnityEngine;

namespace RuneCodeQuest
{
    /// <summary>
    /// セーブデータの場所（最大5こ）。
    /// 進み具合のキー（rcq.〇〇）を、今えらんでいる場所ごとに分けて保存する。
    /// ・1番目は 前のバージョンと同じキー（rcq.〇〇）なので、今までのセーブデータは 1番目になる
    /// ・2〜5番目は rcq.s2.〇〇 のように番号をつける
    /// ・ふりがな・タイピングの言葉・音の設定は、どのセーブデータでも同じ（分けない）
    /// </summary>
    public static class SaveSlots
    {
        public const int Max = 5;
        public const int MaxNameLength = 12;

        private const string KeyCurrent = "rcq.slot";
        private const string KeyNamePrefix = "rcq.slotName";
        private static readonly string[] SharedKeys = { "rcq.furigana", "rcq.typing", "rcq.sound" };

        private static int current = -1;

        /// <summary>今えらんでいるセーブデータの番号（1〜5）</summary>
        public static int Current
        {
            get
            {
                if (current < 0) current = Mathf.Clamp(PlayerPrefs.GetInt(KeyCurrent, 1), 1, Max);
                return current;
            }
            set
            {
                int v = Mathf.Clamp(value, 1, Max);
                if (v == Current) return;
                current = v;
                PlayerPrefs.SetInt(KeyCurrent, v);
                PlayerPrefs.Save();
                Progress.ClearCaches(); // ほかのセーブデータを読みなおす
            }
        }

        /// <summary>保存に使うキー（今のセーブデータの場所に合わせる）</summary>
        public static string Key(string key)
        {
            if (Current == 1 || !key.StartsWith("rcq.")) return key;
            foreach (var s in SharedKeys) if (key == s) return key;
            return $"rcq.s{Current}.{key.Substring(4)}";
        }

        /// <summary>その場所にセーブデータがあるか</summary>
        public static bool Exists(int slot)
        {
            if (PlayerPrefs.HasKey(KeyNamePrefix + slot)) return true;
            // 前のバージョンのセーブデータ（名前がない）は 1番目
            return slot == 1 && (PlayerPrefs.HasKey("rcq.unlocked") || PlayerPrefs.HasKey("rcq.look") || PlayerPrefs.HasKey("rcq.status"));
        }

        public static bool AnyExists()
        {
            for (int i = 1; i <= Max; i++) if (Exists(i)) return true;
            return false;
        }

        /// <summary>セーブデータの名前</summary>
        public static string Name(int slot)
        {
            string n = PlayerPrefs.GetString(KeyNamePrefix + slot, "");
            return n.Length > 0 ? n : $"セーブデータ{slot}";
        }

        public static void SetName(int slot, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) name = $"セーブデータ{slot}";
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            PlayerPrefs.SetString(KeyNamePrefix + slot, name);
            PlayerPrefs.Save();
        }

        /// <summary>その場所のセーブデータを消す</summary>
        public static void Delete(int slot)
        {
            int keep = Current;
            Current = slot;
            Progress.ResetAll();
            PlayerPrefs.DeleteKey(KeyNamePrefix + slot);
            PlayerPrefs.Save();
            Current = keep;
            Progress.ClearCaches();
        }

        /// <summary>一覧に出す、セーブデータの中身</summary>
        public struct Info
        {
            public bool Exists;
            public string Name, Hero, Place;
            public int Rank, Clears;
        }

        /// <summary>その場所のセーブデータの中身を読む（読んだあと、元のセーブデータにもどす）</summary>
        public static Info Read(int slot)
        {
            var info = new Info { Exists = Exists(slot), Name = Name(slot) };
            if (!info.Exists) return info;
            int keep = Current;
            current = slot;           // 読むだけなので、えらんだ番号は保存しない
            Progress.ClearCaches();
            info.Hero = Progress.Look.Name;
            info.Rank = PlayerStats.Rank;
            info.Clears = Progress.ClearedCount;
            var auto = Progress.GetSave(false);
            info.Place = auto.Valid ? (string.IsNullOrEmpty(auto.When) ? auto.Place : $"{auto.Place}・{auto.When}") : "";
            current = keep;
            Progress.ClearCaches();
            return info;
        }
    }

    /// <summary>PlayerPrefs と同じ使い方で、今のセーブデータの場所に保存する</summary>
    internal static class Prefs
    {
        public static int GetInt(string key, int def = 0) => PlayerPrefs.GetInt(SaveSlots.Key(key), def);
        public static void SetInt(string key, int v) => PlayerPrefs.SetInt(SaveSlots.Key(key), v);
        public static string GetString(string key, string def = "") => PlayerPrefs.GetString(SaveSlots.Key(key), def);
        public static void SetString(string key, string v) => PlayerPrefs.SetString(SaveSlots.Key(key), v);
        public static float GetFloat(string key, float def = 0f) => PlayerPrefs.GetFloat(SaveSlots.Key(key), def);
        public static void SetFloat(string key, float v) => PlayerPrefs.SetFloat(SaveSlots.Key(key), v);
        public static bool HasKey(string key) => PlayerPrefs.HasKey(SaveSlots.Key(key));
        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(SaveSlots.Key(key));
        public static void Save() => PlayerPrefs.Save();
    }
}
