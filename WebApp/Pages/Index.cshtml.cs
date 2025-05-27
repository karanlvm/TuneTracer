using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CoreApp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotifyAPI.Web;

public class IndexModel : PageModel
{
    private readonly Database _db;
    private readonly SpotifyClient _spotify;

    public IndexModel(Database db, SpotifyClient spotify)
    {
        _db      = db;
        _spotify = spotify;
    }

    [BindProperty] public IFormFile? Snippet { get; set; }

    // Match results
    public string? MatchSongId { get; private set; }
    public int?    MatchOffset { get; private set; }
    public int?    MatchStrength { get; private set; }

    // Confidence calculation
    public int    QueryFingerprintCount { get; private set; }
    public string? OffsetTime            { get; private set; }
    public int? Popularity           { get; private set; }

    // Spotify metadata
    public FullTrack? Track { get; private set; }
    public string?       AlbumName    { get; private set; }
    public string?       AlbumImage   { get; private set; }
    public string?       ReleaseDate  { get; private set; }
    public List<string>? Genres       { get; private set; }
    public string?       PreviewUrl   { get; private set; }

    public async Task OnPostAsync()
    {
        if (Snippet == null) return;

        // 1) Save uploaded snippet to temp file
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
        await using (var fs = System.IO.File.Create(tmp))
            await Snippet.CopyToAsync(fs);

        // 2) Load and fingerprint the snippet
        var wav = WavFile.Load(tmp);
        var queryCodes = Fingerprinter.Extract(wav, "__QUERY__");
        QueryFingerprintCount = queryCodes.Count;

        // 3) Match and get raw votes
        var (songId, delta, votes) = Matcher.Match(queryCodes, _db);
        MatchSongId     = songId;
        MatchOffset     = delta;
        MatchStrength   = votes;

        // 4) Compute human‐readable offset time
        double seconds = delta * 1024.0 / wav.SampleRate;
        var ts = TimeSpan.FromSeconds(Math.Abs(seconds));
        OffsetTime = ts.Minutes > 0
            ? $"{ts.Minutes}m {ts.Seconds}s"
            : $"{ts.Seconds}.{ts.Milliseconds / 100.0:F0}s";


        // 6) Fetch Spotify track details
        try
        {
            Track = await _spotify.Tracks.Get(songId);
        }
        catch
        {
            var res = await _spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, songId));
            Track = res.Tracks.Items.FirstOrDefault();
        }

        // 7) Populate additional metadata
        if (Track != null)
        {
            Popularity = Track.Popularity;                // 0–100 scale
            AlbumName   = Track.Album.Name;
            ReleaseDate = Track.Album.ReleaseDate;
            AlbumImage  = Track.Album.Images
                .OrderByDescending(i => i.Width)
                .FirstOrDefault()?.Url;

            PreviewUrl = Track.PreviewUrl;

            // Fetch artist genres
            var firstArtistId = Track.Artists.FirstOrDefault()?.Id;
            if (!string.IsNullOrEmpty(firstArtistId))
            {
                var artist = await _spotify.Artists.Get(firstArtistId);
                Genres = artist.Genres.ToList();
            }
        }
    }
}
