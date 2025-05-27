using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreApp
{
    public static class Matcher
    {
        /// <summary>
        /// Legacy entry: fingerprints the WAV internally then matches.
        /// </summary>
        public static (string songId, int offset, int votes)
            Match(WavFile queryWav, Database db)
        {
            var queryCodes = Fingerprinter.Extract(queryWav, "__QUERY__");
            var (songId, offset, votes) = Match(queryCodes, db);
            return (songId, offset, votes);
        }

        /// <summary>
        /// Match using precomputed query codes. Returns (songId, offset, votes).
        /// </summary>
        public static (string songId, int offset, int votes)
            Match(List<(ulong code, string _, int frame)> queryCodes,
                  Database db)
        {
            // vote histogram: (songId, deltaFrame) → count
            var votes = new Dictionary<(string, int), int>();

            foreach (var (code, _, qFrame) in queryCodes)
            {
                foreach (var (songId, dbFrame) in db.Query(code))
                {
                    int delta = qFrame - dbFrame;
                    var key = (songId, delta);
                    votes[key] = votes.TryGetValue(key, out var c) ? c + 1 : 1;
                }
            }

            if (votes.Count == 0)
                return ("<no match>", 0, 0);

            var best = votes.OrderByDescending(kvp => kvp.Value).First();
            var (songIdBest, deltaBest) = best.Key;
            return (songIdBest, deltaBest, best.Value);
        }
    }
}
