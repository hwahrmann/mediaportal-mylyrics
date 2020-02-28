using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;


namespace LyricsEngine.LyricsSites
{
  public class LyricWiki : AbstractSite
  {
    #region const

    // Name
    private const string SiteName = "LyricWiki";

    // Base url
    private const string SiteBaseUrl = "https://lyrics.fandom.com/wiki";

    #endregion const

    #region patterns

    // lyrics mark pattern 
    private const string LyricsMarkPattern = @".*<div class='lyricbox'>(.*?)<div";

    #endregion patterns

    public LyricWiki(string artist, string title, WaitHandle mEventStopSiteSearches, int timeLimit) : base(artist, title, mEventStopSiteSearches, timeLimit)
    {
    }

    #region interface implemetation

    protected override void FindLyricsWithTimer()
    {
      // Clean artist name
      var artist = LyricUtil.RemoveFeatComment(Artist);
      artist = LyricUtil.CapitalizeString(artist);
      artist = artist.Replace(" ", "_");

      // Clean title name
      var title = LyricUtil.TrimForParenthesis(Title);
      title = LyricUtil.CapitalizeString(title);
      title = title.Replace(" ", "_");
      title = title.Replace("?", "%3F");

      // Validate not empty
      if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
      {
        return;
      }

      var urlString = SiteBaseUrl + "/" + artist + ":" + title;

      var uri = new Uri(urlString);
      var client = new LyricsWebClient();
      client.OpenReadCompleted += CallbackMethod;
      client.OpenReadAsync(uri);

      while (Complete == false)
      {
        if (MEventStopSiteSearches.WaitOne(500, true))
        {
          Complete = true;
        }
      }
    }

    public override LyricType GetLyricType()
    {
      return LyricType.UnsyncedLyrics;
    }

    public override SiteType GetSiteType()
    {
      return SiteType.Scrapper;
    }

    public override SiteComplexity GetSiteComplexity()
    {
      return SiteComplexity.OneStep;
    }

    public override SiteSpeed GetSiteSpeed()
    {
      return SiteSpeed.Medium;
    }

    public override bool SiteActive()
    {
      return true;
    }

    public override string Name => SiteName;

    public override string BaseUrl => SiteBaseUrl;

    #endregion interface implemetation

    #region private methods

    private void CallbackMethod(object sender, OpenReadCompletedEventArgs e)
    {
      Stream reply = null;
      StreamReader reader = null;

      try
      {
        reply = e.Result;
        reader = new StreamReader(reply, Encoding.UTF8);

        var line = reader.ReadToEnd();
        var match = Regex.Match(line, LyricsMarkPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
        {
          LyricText = match.Groups[1].Value;
          LyricText = HttpUtility.HtmlDecode(LyricText);
        }

        if (LyricText.Length > 0)
        {
          CleanLyrics();
        }
        else
        {
          LyricText = NotFound;
        }
      }
      catch
      {
        LyricText = NotFound;
      }
      finally
      {
        reader?.Close();
        reply?.Close();
        Complete = true;
      }
    }

    // Cleans the lyrics
    private void CleanLyrics()
    {
      LyricText = LyricText.Replace("%quot;", "\"");
      LyricText = LyricText.Replace("<br>", "\r\n");
      LyricText = LyricText.Replace("<br />", "\r\n");
      LyricText = LyricText.Replace("<BR>", "\r\n");
      LyricText = LyricText.Replace("&amp;", "&");
      LyricText = LyricText.Trim();
    }

    #endregion private methods
  }
}