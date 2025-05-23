using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreApp
{
    public static class Matcher
    {
        public static (string songId, int offset, int votes) Match(
            WavFile queryWav,
            Database db)
        {
            // 1. Fingerprint the query
            var queryCodes = Fingerprinter.Extract(queryWav, "__QUERY__");

            // 2. Lookup & vote
            //    Key: (songId, deltaFrame) → count
            var votes = new Dictionary<(string, int), int>();

            foreach (var (code, _, qFrame) in queryCodes)
            {
                var entries = db.Query(code);
                foreach (var (songId, dbFrame) in entries)
                {
                    int delta = qFrame - dbFrame;
                    var key = (songId, delta);
                    votes[key] = votes.TryGetValue(key, out var c) ? c + 1 : 1;
                }
            }

            // 3. Pick top vote
            if (votes.Count == 0)
                return ("<no match>", 0, 0);

            var best = votes.OrderByDescending(kvp => kvp.Value).First();
            var (songIdBest, deltaBest) = best.Key;
            return (songIdBest, deltaBest, best.Value);
        }
    }
}
