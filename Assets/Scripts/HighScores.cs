using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Result of a finished Game mode run.
    /// </summary>
    [Serializable]
    public class HighScoreEntry
    {
        public int mKills;
        public float mTime;
        public int mLevel;
        public bool mVictory;
        public string mDate;
    }

    /// <summary>
    /// Best runs of the Game mode, kept in a JSON file in the player's persistent data folder
    /// (%USERPROFILE%/AppData/LocalLow/&lt;company&gt;/&lt;product&gt; on Windows).
    /// Runs are ranked by victory first, then by kills, then by time survived.
    /// </summary>
    public static class HighScores
    {
        private const int MaxEntries = 10;
        private const string FileName = "highscores.json";

        [Serializable]
        private class HighScoreTable
        {
            public List<HighScoreEntry> mEntries = new();
        }

        private static HighScoreTable sTable;

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// Best runs, from the best to the worst.
        /// </summary>
        public static IReadOnlyList<HighScoreEntry> Entries
        {
            get
            {
                Load();
                return sTable.mEntries;
            }
        }

        /// <summary>
        /// Records a finished run if it is good enough for the table, and saves the table.
        /// </summary>
        /// <param name="pEntry">The finished run.</param>
        /// <returns>Rank of the run in the table (1 for the best), or 0 if it did not make it.</returns>
        public static int Submit(HighScoreEntry pEntry)
        {
            Load();

            List<HighScoreEntry> lEntries = sTable.mEntries;
            lEntries.Add(pEntry);
            lEntries.Sort(Compare);
            if (lEntries.Count > MaxEntries) lEntries.RemoveRange(MaxEntries, lEntries.Count - MaxEntries);

            int lRank = lEntries.IndexOf(pEntry) + 1;
            if (lRank > 0) Save();
            return lRank;
        }

        /// <summary>
        /// Orders two runs: victories first, then more kills, then longer survival.
        /// </summary>
        private static int Compare(HighScoreEntry pA, HighScoreEntry pB)
        {
            if (pA.mVictory != pB.mVictory) return pA.mVictory ? -1 : 1;
            if (pA.mKills != pB.mKills) return pB.mKills.CompareTo(pA.mKills);
            return pB.mTime.CompareTo(pA.mTime);
        }

        private static void Load()
        {
            if (sTable != null) return;

            sTable = new HighScoreTable();
            try
            {
                if (File.Exists(FilePath))
                {
                    sTable = JsonUtility.FromJson<HighScoreTable>(File.ReadAllText(FilePath)) ?? new HighScoreTable();
                }
            }
            catch (Exception lException)
            {
                // A corrupted or unreadable file must not prevent the game from running: start a new table
                Debug.LogWarning($"HighScores: could not read {FilePath} ({lException.Message}).");
            }
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(sTable, true));
            }
            catch (Exception lException)
            {
                Debug.LogWarning($"HighScores: could not write {FilePath} ({lException.Message}).");
            }
        }
    }
}
