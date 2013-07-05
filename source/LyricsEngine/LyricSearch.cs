using System;
using System.Threading;
using LyricsEngine.LyricsSites;
using Timer = System.Timers.Timer;

namespace LyricsEngine
{
  /// <summary>
  /// Class emulates long process which runs in worker thread
  /// and makes synchronous user UI operations.
  /// </summary>
  public class LyricSearch : IDisposable
  {
    #region Members

    // Reference to the lyric controller used to make syncronous user interface calls:

    private const int TIME_LIMIT = 30 * 1000;
    private const int TIME_LIMIT_FOR_SITE = 15 * 1000;
    public static string[] LyricsSites;

    private readonly bool m_allowAllToComplete;
    private readonly string m_artist = "";
    private readonly bool m_automaticUpdate;

    // Uses to inform the specified site searches to stop searching and exit
    private readonly ManualResetEvent m_EventStop_SiteSearches;
    private readonly LyricsController m_lc;
    private readonly string m_originalArtist = "";
    private readonly string m_originalTrack = "";
    private readonly string m_title = "";
    private readonly Timer timer;
    private bool lyricFound;
    private bool m_SearchHasEnded;
    private int m_sitesSearched;

    #endregion

    #region Functions

    internal LyricSearch(LyricsController lc, string artist, string title, string strippedArtistName, bool allowAllToComplete, bool automaticUpdate)
    {
      m_lc = lc;

      m_artist = strippedArtistName;
      m_title = title;

      m_originalArtist = artist;
      m_originalTrack = title;

      m_allowAllToComplete = allowAllToComplete;
      m_automaticUpdate = automaticUpdate;

      m_EventStop_SiteSearches = new ManualResetEvent(false);

      timer = new Timer();
      timer.Enabled = true;
      timer.Interval = TIME_LIMIT;
      timer.Elapsed += StopDueToTimeLimit;
      timer.Start();
    }

    public void Dispose()
    {
      m_SearchHasEnded = true;
      m_EventStop_SiteSearches.Set();
      timer.Enabled = false;
      timer.Stop();
      timer.Close();
      timer.Dispose();
    }

    public void Run()
    {
      bool lrcFinderThreadIsRunning = false;
      bool actionextIsRunning = false;
      bool lyrDBIsRunning = false;
      bool lyrics007IsRunning = false;
      bool lyricsOnDemandIsRunning = false;
      bool hotLyricsIsRunning = false;
      bool shironetIsRunning = false;

      while (m_SearchHasEnded == false)
      {
        if (Array.IndexOf(LyricsSites, "LrcFinder") != -1 && lrcFinderThreadIsRunning == false)
        {
          lrcFinderThreadIsRunning = true;
          Thread lrcFinderThread;
          ThreadStart job = delegate { SearchLrcFinder(m_artist, m_title); };
          lrcFinderThread = new Thread(job);
          lrcFinderThread.Start();
        }

        if (Array.IndexOf(LyricsSites, "LyricsOnDemand") != -1 && lyricsOnDemandIsRunning == false)
        {
          lyricsOnDemandIsRunning = true;
          Thread lyricsOnDemandThread;
          ThreadStart job = delegate
                                {
                                  var lyricsOnDemand = new LyricsOnDemand(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                  lyricsOnDemand.FindLyrics();
                                  if (m_allowAllToComplete)
                                  {
                                    ValidateSearchOutputForAllowAllToComplete(lyricsOnDemand.Lyric,
                                                                              "LyricsOnDemand");
                                  }
                                  else
                                  {
                                    ValidateSearchOutput(lyricsOnDemand.Lyric, "LyricsOnDemand");
                                  }
                                };
          lyricsOnDemandThread = new Thread(job);
          lyricsOnDemandThread.Start();
        }

        if (Array.IndexOf(LyricsSites, "Lyrics007") != -1 && lyrics007IsRunning == false)
        {
          lyrics007IsRunning = true;
          Thread lyrics007Thread;
          ThreadStart job = delegate
                                {
                                  Lyrics007 lyrics007 = new Lyrics007(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                    lyrics007.FindLyrics();
                                  if (m_allowAllToComplete)
                                  {
                                    ValidateSearchOutputForAllowAllToComplete(lyrics007.Lyric, "Lyrics007");
                                  }
                                  else
                                  {
                                    ValidateSearchOutput(lyrics007.Lyric, "Lyrics007");
                                  }
                                };
          lyrics007Thread = new Thread(job);
          lyrics007Thread.Start();
        }

        if (Array.IndexOf(LyricsSites, "Actionext") != -1 && actionextIsRunning == false)
        {
          actionextIsRunning = true;
          Thread actionextThread;
          ThreadStart job = delegate
                                {
                                  var actionext = new Actionext(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                  actionext.FindLyrics();
                                  if (m_allowAllToComplete)
                                  {
                                    ValidateSearchOutputForAllowAllToComplete(actionext.Lyric, "Actionext");
                                  }
                                  else
                                  {
                                    ValidateSearchOutput(actionext.Lyric, "Actionext");
                                  }
                                };
          actionextThread = new Thread(job);
          actionextThread.Start();
        }

        if (Array.IndexOf(LyricsSites, "LyrDB") != -1 && lyrDBIsRunning == false)
        {
          lyrDBIsRunning = true;
          Thread lyrDBThread;
          ThreadStart job = delegate
                                {
                                  var lyrDb = new LyrDb(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                  lyrDb.FindLyrics();
                                  if (m_allowAllToComplete)
                                  {
                                    ValidateSearchOutputForAllowAllToComplete(lyrDb.Lyric, "LyrDB");
                                  }
                                  else
                                  {
                                    ValidateSearchOutput(lyrDb.Lyric, "LyrDB");
                                  }
                                };
          lyrDBThread = new Thread(job);
          lyrDBThread.Start();
        }


        if (Array.IndexOf(LyricsSites, "HotLyrics") != -1 && hotLyricsIsRunning == false)
        {
          hotLyricsIsRunning = true;
          Thread hotLyricsThread;
          ThreadStart job = delegate
                                {
                                  var hotLyrics = new HotLyrics(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                  hotLyrics.FindLyrics();
                                  if (m_allowAllToComplete)
                                  {
                                    ValidateSearchOutputForAllowAllToComplete(hotLyrics.Lyric, "HotLyrics");
                                  }
                                  else
                                  {
                                    ValidateSearchOutput(hotLyrics.Lyric, "HotLyrics");
                                  }
                                };
          hotLyricsThread = new Thread(job);
          hotLyricsThread.Start();
        }

        if (Array.IndexOf(LyricsSites, "Shironet") != -1 && shironetIsRunning == false)
        {
          shironetIsRunning = true;
          Thread shironetThread;
          ThreadStart job = delegate
                              {
                                  
                                var shironet = new Shironet(m_artist, m_title, m_EventStop_SiteSearches, TIME_LIMIT_FOR_SITE);
                                  shironet.FindLyrics();
                                if (m_allowAllToComplete)
                                {
                                    ValidateSearchOutputForAllowAllToComplete(shironet.Lyric, shironet.Name);
                                }
                                else
                                {
                                    ValidateSearchOutput(shironet.Lyric, shironet.Name);
                                }
                              };
          shironetThread = new Thread(job);
          shironetThread.Start();
        }

        Thread.Sleep(300);
      }

      Thread.CurrentThread.Abort();
    }

    #endregion

    private bool SearchLrcFinder(string artist, string title)
    {
      string lrc = "";

      var lrcFinder = new LrcFinder(artist, title, null, TIME_LIMIT_FOR_SITE);
      lrcFinder.FindLyrics();
      lrc = lrcFinder.Lyric;

      bool validLyric = m_allowAllToComplete
                            ? ValidateSearchOutputForAllowAllToComplete(lrc, "LrcFinder")
                            : ValidateSearchOutput(lrc, "LrcFinder");
      return validLyric;
    }


    public bool ValidateSearchOutput(string lyric, string site)
    {
      if (m_SearchHasEnded == false)
      {
        Monitor.Enter(this);
        try
        {
          ++m_sitesSearched;

          // Parse the lyrics and find a suitable lyric, if any
          if (!lyric.Equals("Not found") && lyric.Length != 0)
          {
            // if the lyrics hasn't been found by another site, then we have found the lyrics to count!
            if (lyricFound == false)
            {
              lyricFound = true;
              m_lc.LyricFound(lyric, m_originalArtist, m_originalTrack, site);
              Dispose();
              return true;
            }
            // if another was quicker it is just too bad... return
            else
            {
              return false;
            }
          }
          // still other lyricsites to search
          else if (m_sitesSearched < LyricsSites.Length)
          {
            return false;
          }
          // the search got to end due to no more sites to search
          else
          {
            m_lc.LyricNotFound(m_originalArtist, m_originalTrack, "A matching lyric could not be found!",
                               site);
            Dispose();
            return false;
          }
        }
        finally
        {
          Monitor.Exit(this);
        }
      }
      else
      {
        return false;
      }
    }

    public bool ValidateSearchOutputForAllowAllToComplete(string lyric, string site)
    {
      if (m_SearchHasEnded == false)
      {
        Monitor.Enter(this);
        try
        {
          if (!lyric.Equals("Not found") && lyric.Length != 0)
          {
            lyricFound = true;
            m_lc.LyricFound(lyric, m_originalArtist, m_originalTrack, site);
            if (++m_sitesSearched == LyricsSites.Length || m_automaticUpdate)
            {
              Dispose();
            }
            return true;
          }
          else
          {
            m_lc.LyricNotFound(m_originalArtist, m_originalTrack, "A matching lyric could not be found!",
                               site);
            if (++m_sitesSearched == LyricsSites.Length)
            {
              Dispose();
            }
            return false;
          }
        }
        finally
        {
          Monitor.Exit(this);
        }
      }
      else
      {
        return false;
      }
    }

    private void StopDueToTimeLimit(object sender, EventArgs e)
    {
      m_lc.LyricNotFound(m_originalArtist, m_originalTrack, "A matching lyric could not be found!", "All (timed out)");
      Dispose();
    }

    #region Properties

    public bool SearchHasEnded
    {
      get { return m_SearchHasEnded; }
      set { m_SearchHasEnded = value; }
    }

    #endregion
  }
}