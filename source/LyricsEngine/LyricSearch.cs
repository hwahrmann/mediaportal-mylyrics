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

        private const int TimeLimit = 30 * 1000;
        private const int TimeLimitForSite = 15 * 1000;

        public static string[] LyricsSites;
        private const string Lrcfinder = "LrcFinder";
        private const string Lyrics007 = "Lyrics007";
        private const string Shironet = "Shironet";
        private const string Hotlyrics = "HotLyrics";
        private const string Lyricsondemand = "LyricsOnDemand";
        private const string Actionext = "Actionext";
        private const string Lyrdb = "LyrDB";


        private readonly bool _mAllowAllToComplete;
        private readonly string _mArtist = "";
        private readonly bool _mAutomaticUpdate;

        // Uses to inform the specified site searches to stop searching and exit
        private readonly ManualResetEvent _mEventStopSiteSearches;
        private readonly LyricsController _mLyricsController;
        private readonly string _mOriginalArtist = "";
        private readonly string _mOriginalTrack = "";
        private readonly string _mTitle = "";
        private readonly Timer _timer;
        private bool _lyricFound;
        private bool _mSearchHasEnded;
        private int _mSitesSearched;

        #endregion

        #region Functions

    internal LyricSearch(LyricsController lyricsController, string artist, string title, string strippedArtistName, bool allowAllToComplete, bool automaticUpdate)
        {
            _mLyricsController = lyricsController;

            _mArtist = strippedArtistName;
            _mTitle = title;

            _mOriginalArtist = artist;
            _mOriginalTrack = title;

            _mAllowAllToComplete = allowAllToComplete;
            _mAutomaticUpdate = automaticUpdate;

            _mEventStopSiteSearches = new ManualResetEvent(false);

            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = TimeLimit;
            _timer.Elapsed += StopDueToTimeLimit;
            _timer.Start();
        }

        public void Dispose()
        {
            _mSearchHasEnded = true;
            _mEventStopSiteSearches.Set();
            _timer.Enabled = false;
            _timer.Stop();
            _timer.Close();
            _timer.Dispose();
        }

        public void Run()
        {
            bool lrcFinderThreadIsRunning = false;
            bool actionextIsRunning = false;
            bool lyrDbIsRunning = false;
            bool lyrics007IsRunning = false;
            bool lyricsOnDemandIsRunning = false;
            bool hotLyricsIsRunning = false;
            bool shironetIsRunning = false;

            while (_mSearchHasEnded == false)
            {
                if (Array.IndexOf(LyricsSites, Lrcfinder) != -1 && lrcFinderThreadIsRunning == false)
                {
                    lrcFinderThreadIsRunning = true;
                    Thread lrcFinderThread;
                    ThreadStart job = delegate
                        {
                            var lrcFinder = LyricsSiteFactory.Create(Lrcfinder, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            lrcFinder.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(lrcFinder.Lyric, Lrcfinder);
                            }
                            else
                            {
                                ValidateSearchOutput(lrcFinder.Lyric, Lrcfinder);
                            }
                        };
                    lrcFinderThread = new Thread(job);
                    lrcFinderThread.Start();
                }

                if (Array.IndexOf(LyricsSites, Lyricsondemand) != -1 && lyricsOnDemandIsRunning == false)
                {
                    lyricsOnDemandIsRunning = true;
                    Thread lyricsOnDemandThread;
                    ThreadStart job = delegate
                        {
                            var lyricsOnDemand = LyricsSiteFactory.Create(Lyricsondemand, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            lyricsOnDemand.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(lyricsOnDemand.Lyric, Lyricsondemand);
                            }
                            else
                            {
                                ValidateSearchOutput(lyricsOnDemand.Lyric, Lyricsondemand);
                            }
                        };
                    lyricsOnDemandThread = new Thread(job);
                    lyricsOnDemandThread.Start();
                }

                if (Array.IndexOf(LyricsSites, Lyrics007) != -1 && lyrics007IsRunning == false)
                {
                    lyrics007IsRunning = true;
                    Thread lyrics007Thread;
                    ThreadStart job = delegate
                        {
                            var lyrics007 = LyricsSiteFactory.Create(Lyrics007, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            lyrics007.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(lyrics007.Lyric, Lyrics007);
                            }
                            else
                            {
                                ValidateSearchOutput(lyrics007.Lyric, Lyrics007);
                            }
                        };
                    lyrics007Thread = new Thread(job);
                    lyrics007Thread.Start();
                }

                if (Array.IndexOf(LyricsSites, Actionext) != -1 && actionextIsRunning == false)
                {
                    actionextIsRunning = true;
                    Thread actionextThread;
                    ThreadStart job = delegate
                        {
                            var actionext = LyricsSiteFactory.Create(Actionext, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            actionext.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(actionext.Lyric, Actionext);
                            }
                            else
                            {
                                ValidateSearchOutput(actionext.Lyric, Actionext);
                            }
                        };
                    actionextThread = new Thread(job);
                    actionextThread.Start();
                }

                if (Array.IndexOf(LyricsSites, Lyrdb) != -1 && lyrDbIsRunning == false)
                {
                    lyrDbIsRunning = true;
                    Thread lyrDBThread;
                    ThreadStart job = delegate
                        {
                            var lyrDb = LyricsSiteFactory.Create(Lyrdb, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            lyrDb.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(lyrDb.Lyric, Lyrdb);
                            }
                            else
                            {
                                ValidateSearchOutput(lyrDb.Lyric, Lyrdb);
                            }
                        };
                    lyrDBThread = new Thread(job);
                    lyrDBThread.Start();
                }


                if (Array.IndexOf(LyricsSites, Hotlyrics) != -1 && hotLyricsIsRunning == false)
                {
                    hotLyricsIsRunning = true;
                    Thread hotLyricsThread;
                    ThreadStart job = delegate
                        {
                            var hotLyrics = LyricsSiteFactory.Create(Hotlyrics, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            hotLyrics.FindLyrics();
                            if (_mAllowAllToComplete)
                            {
                                ValidateSearchOutputForAllowAllToComplete(hotLyrics.Lyric, Hotlyrics);
                            }
                            else
                            {
                                ValidateSearchOutput(hotLyrics.Lyric, Hotlyrics);
                            }
                        };
                    hotLyricsThread = new Thread(job);
                    hotLyricsThread.Start();
                }

                if (Array.IndexOf(LyricsSites, Shironet) != -1 && shironetIsRunning == false)
                {
                    shironetIsRunning = true;
                    Thread shironetThread;
                    ThreadStart job = delegate
                        {
                            var shironet = LyricsSiteFactory.Create(Shironet, _mArtist, _mTitle, _mEventStopSiteSearches, TimeLimitForSite);
                            shironet.FindLyrics();
                            if (_mAllowAllToComplete)
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

        public bool ValidateSearchOutput(string lyric, string site)
        {
            if (_mSearchHasEnded == false)
            {
                Monitor.Enter(this);
                try
                {
                    ++_mSitesSearched;

                    // Parse the lyrics and find a suitable lyric, if any
                    if (!lyric.Equals(AbstractSite.NotFound) && lyric.Length != 0)
                    {
                        // if the lyrics hasn't been found by another site, then we have found the lyrics to count!
                        if (_lyricFound == false)
                        {
                            _lyricFound = true;
                            _mLyricsController.LyricFound(lyric, _mOriginalArtist, _mOriginalTrack, site);
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
                    else if (_mSitesSearched < LyricsSites.Length)
                    {
                        return false;
                    }
                        // the search got to end due to no more sites to search
                    else
                    {
                        _mLyricsController.LyricNotFound(_mOriginalArtist, _mOriginalTrack, "A matching lyric could not be found!",
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
            return false;
        }

        public bool ValidateSearchOutputForAllowAllToComplete(string lyric, string site)
        {
            if (_mSearchHasEnded == false)
            {
                Monitor.Enter(this);
                try
                {
                    if (!lyric.Equals("Not found") && lyric.Length != 0)
                    {
                        _lyricFound = true;
                        _mLyricsController.LyricFound(lyric, _mOriginalArtist, _mOriginalTrack, site);
                        if (++_mSitesSearched == LyricsSites.Length || _mAutomaticUpdate)
                        {
                            Dispose();
                        }
                        return true;
                    }
                    else
                    {
                        _mLyricsController.LyricNotFound(_mOriginalArtist, _mOriginalTrack, "A matching lyric could not be found!",
                                           site);
                        if (++_mSitesSearched == LyricsSites.Length)
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
            return false;
        }

        private void StopDueToTimeLimit(object sender, EventArgs e)
        {
      _mLyricsController.LyricNotFound(_mOriginalArtist, _mOriginalTrack, "A matching lyric could not be found!", "All (timed out)");
            Dispose();
        }

        #region Properties

        public bool SearchHasEnded
        {
            get { return _mSearchHasEnded; }
            set { _mSearchHasEnded = value; }
        }

        #endregion
    }
}