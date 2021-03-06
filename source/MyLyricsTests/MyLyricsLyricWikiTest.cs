﻿using System.Diagnostics;
using System.Threading;
using LyricsEngine.LyricsSites;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MyLyricsTests
{
    [TestClass]
    public class MyLyricsLyricWikiTest
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        [TestInitialize]
        public void SetUp()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }

        [TestCleanup]
        public void TearDown()
        {
            _stopwatch.Stop();
            Debug.WriteLine("Test duration: " + _stopwatch.Elapsed);
        }

        [TestMethod]
        public void TestLyricWiki()
        {
            var site = new LyricWiki("U2", "With Or Without You", new ManualResetEvent(false), 300000);
            if (site.SiteActive())
            {
                site.FindLyrics();
                var splitLyrics = site.Lyric.Split(' ');
                Assert.AreEqual("See", splitLyrics[0]);
                Assert.AreEqual("without", splitLyrics[splitLyrics.Length - 2]);
            }
        }

        [TestMethod]
        public void TestLyricWikiNotFound()
        {
            var site = new LyricWiki("Foo", "Bar", new ManualResetEvent(false), 30000);
            if (site.SiteActive())
            {
                site.FindLyrics();
                var splitLyrics = site.Lyric.Split(' ');
                Assert.AreEqual("Not", splitLyrics[0]);
                Assert.AreEqual("found", splitLyrics[splitLyrics.Length - 1]);
            }
        }
    }
}