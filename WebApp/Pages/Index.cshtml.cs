using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        _db = db;
        _spotify = spotify;
    }

    [BindProperty] public IFormFile? Snippet { get; set; }

    public string? MatchSongId { get; private set; }
    public int? MatchOffset    { get; private set; }
    public int? MatchVotes     { get; private set; }
    public FullTrack? Track    { get; private set; }

    public async Task OnPostAsync()
    {
        if (Snippet == null) return;

        // save to temp file
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".wav");
        await using (var fs = System.IO.File.Create(tmp))
            await Snippet.CopyToAsync(fs);

        // identify
        var wav = WavFile.Load(tmp);
        var (songId, delta, votes) = Matcher.Match(wav, _db);

        MatchSongId = songId;
        MatchOffset = delta;
        MatchVotes  = votes;

        // fetch from Spotify
        try
        {
            Track = await _spotify.Tracks.Get(songId);
        }
        catch
        {
            // fallback to search by name
            var res = await _spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, songId));
            Track = res.Tracks.Items.FirstOrDefault();
        }
    }
}
