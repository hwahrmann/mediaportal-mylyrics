﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using MediaPortal.Music.Database;
using MediaPortal.Playlists;
using MediaPortal.TagReader;

using LyricsEngine;
using LyricsEngine.LRC;


namespace MyLyrics
{
    public partial class MyLyricsSetup_LyricsLibrary : UserControl
    {

        static string m_CurrentArtist = "";
        static string m_CurrentTitle = "";
        string m_OriginalLyric = "";

        Form m_Form = null;

        int m_NoOfArtists;
        int m_NoOfTitles;

        SimpleLRC lrc;
        const int SHIFT_WHEN_HIT = 15;
        internal static LyricsDatabase CurrentDB;
        private int currentDBIndex = 0;
        enum TYPEOFLYRICS : int {NONE, NORMAL, LRC};


        public MyLyricsSetup_LyricsLibrary(Form form)
        {
            this.m_Form = form;
            InitializeComponent();
            comboDatabase.SelectedIndex = 0;
            updateLyricsTree();
        }

        internal void updateLyricsTree()
        {
            if (CurrentDB == null)
            {
                return;
            }

            try
            {
                treeView.Nodes.Clear();
                m_NoOfArtists = 0;
                m_NoOfTitles = 0;

                foreach (KeyValuePair<string, LyricsItem> kvp in CurrentDB)
                {
                    AddSong(kvp.Value);
                }
            }
            catch {;}
            finally
            {
                treeView.Sort();
                updateLyricDatabaseStats();
            }
        }

        private void updateLyricDatabaseStats()
        {
            lbArtists2.Text = m_NoOfArtists.ToString();
            lbSongs2.Text = m_NoOfTitles.ToString();
        }

        private void resetFields()
        {
            lbArtists2.Text = "";
            lbTitle.Text = "";
            lbLRCTest.Text = "";
            lbSongs2.Text = "";
            lbSource.Text = "";
            tbLyrics.Text = "";
        }

        private ArrayList getTitlesByArtist(string artist)
        {
            ArrayList titles = new ArrayList();

            foreach (KeyValuePair<string, LyricsItem> kvp in CurrentDB)
            {
                if (kvp.Value.Artist.Equals(artist))
                {
                    titles.Add(kvp.Value.Title);
                }
            }
            return titles;
        }

        /// <summary>
        /// AddSong adds a lyric to the treeView and not to the lyric database
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="title"></param>
        private void AddSong(LyricsItem item)
        {
            string artist = LyricUtil.CapatalizeString(item.Artist);
            string title = LyricUtil.CapatalizeString(item.Title);

            // add artist, if it doesn't exists
            if (!treeView.Nodes.ContainsKey(artist))
            {
                treeView.Nodes.Add(artist, artist);
                ++m_NoOfArtists;
            }
             
            // add title, if it doesn't exists
            int artistIndex = treeView.Nodes.IndexOfKey(artist);
            if (!treeView.Nodes[artistIndex].Nodes.ContainsKey(title))
            {
                treeView.Nodes[artistIndex].Nodes.Add(title, title);
                treeView.Nodes[artistIndex].Nodes[treeView.Nodes[artistIndex].Nodes.Count - 1].Tag = item;
                ++m_NoOfTitles;
            }
        }

        /// <summary>
        /// AddSong with tree parameters adds a lyric to the treeView and the lyric database
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="title"></param>
        /// <param name="lyric"></param>
        private bool AddSong(string artist, string title, string lyrics, string site)
        {
            LyricsItem item = new LyricsItem(artist, title, lyrics, site);

            if (DatabaseUtil.IsTrackInLyricsDatabase(CurrentDB, artist, title).Equals(DatabaseUtil.LYRIC_NOT_FOUND))
            {
                CurrentDB.Add(DatabaseUtil.CorrectKeyFormat(artist, title), item);
                AddSong(item);
                treeView.Update();
                DatabaseUtil.SerializeDB(CurrentDB);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void RemoveArtist(string artist)
        {
            m_NoOfTitles -= getTitlesByArtist(artist).Count;
            --m_NoOfArtists;
            int artistIndex = treeView.Nodes.IndexOfKey(artist);
            treeView.Nodes.RemoveAt(artistIndex);
            treeView.Update();
        }

        public void RemoveSong(string artist, string title)
        {
            try
            {
                int artistIndex = treeView.Nodes.IndexOfKey(artist);
                int titleIndex = treeView.Nodes[artistIndex].Nodes.IndexOfKey(title);
                treeView.Nodes[artistIndex].Nodes.RemoveAt(titleIndex);
                --m_NoOfTitles;

                // remove title from treeView
                if (treeView.Nodes[artistIndex].Nodes.Count == 0)
                {
                    treeView.Nodes.RemoveAt(artistIndex);
                    --m_NoOfArtists;
                }
            }
            catch
            {
                if (artist.Length == 0 && title.Length == 0)
                {
                    treeView.Nodes.RemoveAt(0);
                    --m_NoOfArtists;
                    treeView.Update();
                }
            }
            finally
            {
                treeView.Update();

                // remove title from database
                CurrentDB.Remove(DatabaseUtil.CorrectKeyFormat(artist, title));
                DatabaseUtil.SerializeDB(CurrentDB);
            }
        }

        private bool isSelectedLyricALRC()
        {
            if (treeView.SelectedNode != null && treeView.SelectedNode.Parent != null)
            {
                string artist = treeView.SelectedNode.Parent.Text;
                string title = treeView.SelectedNode.Text;

                if (artist.Length != 0 && title.Length != 0)
                {
                    m_CurrentArtist = artist;
                    m_CurrentTitle = LyricUtil.CapatalizeString(title);

                    string lyricsText = (string)CurrentDB[DatabaseUtil.CorrectKeyFormat(artist, title)].Lyrics;

                    SimpleLRC lrc = new SimpleLRC(artist, title, lyricsText);
                    if (lrc.IsValid)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public void updateInfo()
        {
            m_CurrentArtist = "";
            m_CurrentTitle = "";
            tbLyrics.Text = "";
            lbTitle.Text = "";
            lbSource.Text = "";
            lbLRCTest.Text = "";

            tbLyrics.Enabled = false;
            
            // Selected a title
            if (treeView.SelectedNode != null && treeView.SelectedNode.Parent != null)
            {
                string artist = treeView.SelectedNode.Parent.Text;
                string title = treeView.SelectedNode.Text;

                if (artist.Length != 0 && title.Length != 0)
                {
                    m_CurrentArtist = LyricUtil.CapatalizeString(artist);
                    m_CurrentTitle = LyricUtil.CapatalizeString(title);

                    if (DatabaseUtil.IsTrackInLyricsDatabase(CurrentDB, m_CurrentArtist, m_CurrentTitle).Equals(DatabaseUtil.LYRIC_FOUND))
                    {
                        LyricsItem item = CurrentDB[DatabaseUtil.CorrectKeyFormat(m_CurrentArtist, m_CurrentTitle)];
                        string lyricsText = item.Lyrics;

                        lyricsText = LyricsEngine.LyricUtil.ReturnEnvironmentNewLine(lyricsText);

                        m_OriginalLyric = lyricsText;
                        tbLyrics.Text = m_OriginalLyric;
                        tbLyrics.Enabled = true;

                        lbTitle.Text = "\"" + m_CurrentArtist + " - " + m_CurrentTitle + "\"";
                        lbSource.Text = "(" + item.Source + ")";
                    }
                }
            }
            // Selected an artist
            else if (treeView.SelectedNode != null)
            {
                string artist = treeView.SelectedNode.Text;
                m_CurrentArtist = LyricUtil.CapatalizeString(artist);
            }
        }

        private MusicTag GetTag(Song song)
        {
            MusicTag tag = new MusicTag();
            tag.Album = song.Album;
            tag.Artist = song.Artist;
            tag.Duration = song.Duration;
            tag.Genre = song.Genre;
            tag.Title = song.Title;
            tag.Track = song.Track;
            tag.Year = song.Year;
            return tag;
        }


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                /*if(components != null)
                        {
                            components.Dispose();
                        }*/
            }
            base.Dispose(disposing);
        }

        private void btSave_Click(object sender, EventArgs e)
        {
            CurrentDB[DatabaseUtil.CorrectKeyFormat(LyricUtil.CapatalizeString(m_CurrentArtist), LyricUtil.CapatalizeString(m_CurrentTitle))].Lyrics = tbLyrics.Text;
            DatabaseUtil.SerializeDB(CurrentDB);
            btSave.Enabled = false;
            treeView.Focus();
        }

        private void tbLyrics_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_OriginalLyric.Equals(tbLyrics.Text) || m_CurrentTitle.Length == 0)
                btSave.Enabled = false;
            else
                btSave.Enabled = true;
        }

        private void btImport_Click(object sender, EventArgs e)
        {
        }


        private void btDelete_Click(object sender, EventArgs e)
        {
            if (m_CurrentTitle.Length == 0)
            {
                ArrayList titles = getTitlesByArtist(m_CurrentArtist);
                if (titles != null)
                {
                    for (int i = 0; i < titles.Count; i++)
                    {
                        RemoveSong(m_CurrentArtist, (string)titles[i]);
                    }
                }
            }
            else
            {

                RemoveSong(m_CurrentArtist, m_CurrentTitle);
                highlightSong(m_CurrentArtist, m_CurrentTitle, true);
            }
            updateLyricDatabaseStats();
            treeView.Focus();
            treeView.Select();
        }


        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            updateInfo();
            if (isSelectedLyricALRC())
            {
                lbLRCTest.Text = "(valid LRC)";
            }
            else
            {
                lbLRCTest.Text = "";
            }
        }

        private void btAdd_Click(object sender, EventArgs e)
        {
            MyLyricsSetup_AddNewSong addNewSongForm = new MyLyricsSetup_AddNewSong(this);
            updateLyricDatabaseStats();
        }

        internal void addNewSongToDatabase(string artist, string title, string lyrics)
        {
            m_CurrentArtist = LyricUtil.CapatalizeString(artist);
            m_CurrentTitle = LyricUtil.CapatalizeString(title);

            if (AddSong(m_CurrentArtist, m_CurrentTitle, lyrics, "Manual added"))
            {
                highlightSong(m_CurrentArtist, m_CurrentTitle, false);
                updateLyricDatabaseStats();
            }
            else
            {
                MessageBox.Show("The title \"" + artist + " - " + title + "\" is already in the database");
            }
        }

        internal void highlightSong(string artist, string title, bool previousSong)
        {
            if (artist.Length == 0 || title.Length == 0)
            {
                return;
            }

            treeView.Select();
            treeView.Focus();

            int artistIndex = treeView.Nodes.IndexOfKey(artist);
            TreeNode artistNode = treeView.Nodes[artistIndex];

            if (title.Length == 0)
            {
                treeView.SelectedNode = artistNode;
                return;
            }

            int titleIndex = artistNode.Nodes.IndexOfKey(title);
            if (previousSong && titleIndex > 0)
                titleIndex -= 1;
            treeView.SelectedNode = artistNode.Nodes[titleIndex];
        }

        private void btClose_Click(object sender, EventArgs e)
        {
            m_Form.Close();
        }

        private void btImportSingle_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] files = openFileDialog1.FileNames;
                    for (int i = 0; i < files.Length; i++)
                    {
                        string fileName = new FileInfo(files[i]).Name;
                        if (fileName.Contains("-"))
                        {
                            if (inspectFileNameAndAddToDatabaseIfValidLyrics(files[i]) != (int)TYPEOFLYRICS.NONE)
                            {
                                updateLyricDatabaseStats();
                            }
                            else
                            {
                                MessageBox.Show("The title is already in the database");
                            }
                            
                        }
                        else
                        {
                            MessageBox.Show("The file \"" + fileName + "\" does not have a valid filename ([Artist]-[Title].txt or *.lrc).");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private int inspectFileNameAndAddToDatabaseIfValidLyrics(string filePath)
        {
            TextReader textReader = null;


            FileInfo fileInfo = new FileInfo(filePath);


            if (fileInfo.Extension.Equals(".txt"))
            {

                string fileStringArtist = "";
                string fileStringTitle = "";
                string fileName = fileInfo.Name;


                int index = fileName.IndexOf("-");
                fileStringArtist = fileName.Substring(0, index);
                fileStringTitle = fileName.Substring(index + 1);
                fileStringArtist = fileStringArtist.Trim();
                fileStringTitle = fileStringTitle.Trim();

                index = fileStringTitle.LastIndexOf('.');
                fileStringTitle = fileStringTitle.Substring(0, index);

                textReader = new StreamReader(filePath);
                string line = "";
                string lyrics = "";

                while ((line = textReader.ReadLine()) != null)
                {
                    lyrics += line + Environment.NewLine;
                }
                lyrics = lyrics.Trim();
                textReader.Close();

                string capArtist = LyricUtil.CapatalizeString(fileStringArtist);
                string capTitle = LyricUtil.CapatalizeString(fileStringTitle);

                if (AddSong(capArtist, capTitle, lyrics, "Text file"))
                {
                    return (int)TYPEOFLYRICS.NORMAL;
                }
                else
                {
                    return (int)TYPEOFLYRICS.NONE;
                }
            }
            else
            {
                lrc = new SimpleLRC(filePath);

                if (lrc.IsValid && lrc.Artist.Length != 0 && lrc.Title.Length != 0)
                {
                    if (AddSong(lrc.Artist, lrc.Title, lrc.Lyric.Trim(), "LRC-file"))
                    {
                        return (int)TYPEOFLYRICS.LRC;
                    }
                    else
                    {
                        return (int)TYPEOFLYRICS.NONE;
                    }
                }
                else
                {
                    return (int)TYPEOFLYRICS.NONE;
                }
            }
        }


        private void btUpgradeDatabase_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure the lyrics database should be upgraded?", "Upgrade lyrics database", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                updateLyricsTree();
            }
        }

        private void btImportDIRS_Click(object sender, EventArgs e)
        {

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string path = folderBrowserDialog1.SelectedPath;

                    int numberOfLyrics = 0;
                    int numberOfLRCS = 0;
                    int numberOfExisting = 0;

                    DirectoryInfo dirInfo = new DirectoryInfo(path);
                    FileInfo[] fileInfos = dirInfo.GetFiles("*-*.txt", SearchOption.AllDirectories);

                    foreach (FileInfo fileInfo in fileInfos)
                    {
                        int typeAdded;
                        if ((typeAdded = inspectFileNameAndAddToDatabaseIfValidLyrics(fileInfo.FullName)) == (int)TYPEOFLYRICS.NORMAL)
                        {
                            ++numberOfLyrics;
                        }
                        else if (typeAdded == (int)TYPEOFLYRICS.LRC)
                        {
                            ++numberOfLRCS;
                        }
                        else
                        {
                            ++numberOfExisting;
                        }
                    }

                    dirInfo = new DirectoryInfo(path);
                    fileInfos = dirInfo.GetFiles("*.lrc", SearchOption.AllDirectories);

                    foreach (FileInfo fileInfo in fileInfos)
                    {
                        int typeAdded;
                        if ((typeAdded = inspectFileNameAndAddToDatabaseIfValidLyrics(fileInfo.FullName)) == (int)TYPEOFLYRICS.NORMAL)
                        {
                            ++numberOfLyrics;
                        }
                        else if (typeAdded == (int)TYPEOFLYRICS.LRC)
                        {
                            ++numberOfLRCS;
                        }
                        else
                        {
                            ++numberOfExisting;
                        }
                    }


                    updateLyricDatabaseStats();

                    MessageBox.Show("Number of lyris added :" + (numberOfLRCS + numberOfLyrics + numberOfExisting) + Environment.NewLine
                                    + "Number of basic lyrics added:" + numberOfLyrics + Environment.NewLine
                                    + "Number of LRCs added: " + numberOfLRCS + Environment.NewLine
                                    + "Number of lyrics already in database: " + numberOfExisting);

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }


        private void comboDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboDatabase.SelectedIndex == 0 && currentDBIndex != 0)
            {
                resetFields();
                currentDBIndex = 0;
                CurrentDB = MyLyricsSettings.LyricsDB;
                btImportFiles.Enabled = true;
                btImportDirs.Enabled = true;
                updateLyricsTree();
            }
            else if (comboDatabase.SelectedIndex == 1 && currentDBIndex != 1)
            {
                resetFields();
                currentDBIndex = 1;
                CurrentDB = MyLyricsSettings.LyricsMarkedDB;
                btImportFiles.Enabled = false;
                btImportDirs.Enabled = false;
                updateLyricsTree();
            }
        }

        private void btResetDatabase_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure the Lyrics database should be deleted?", "Delete Lyrics database", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                string path = MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Database) + "\\" + MyLyricsSettings.LyricsDBName;
                FileStream fs = new FileStream(path, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                MyLyricsSettings.LyricsDB = new LyricsDatabase();
                bf.Serialize(fs, MyLyricsSettings.LyricsDB);
                fs.Close();

                CurrentDB = MyLyricsSettings.LyricsDB;
                comboDatabase.SelectedIndex = 0;
                updateLyricsTree();
                updateInfo();
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Are you sure you want to delete the database with marked titles?", "Delete title database", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                string path = MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Database) + "\\" + MyLyricsSettings.LyricsMarkedDBName;
                FileStream fs = new FileStream(path, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                MyLyricsSettings.LyricsMarkedDB = new LyricsDatabase();
                bf.Serialize(fs, MyLyricsSettings.LyricsMarkedDB);
                fs.Close();

                CurrentDB = MyLyricsSettings.LyricsMarkedDB;
                comboDatabase.SelectedIndex = 1;
                updateLyricsTree();
                updateInfo();
            }
        }

        private void btSearchSingle_Click(object sender, EventArgs e)
        {
            string temp = "";
            string artist = "";
            string title = "";

            if (treeView.SelectedNode != null)
            {
                temp = treeView.SelectedNode.Text;

                if (treeView.SelectedNode.Parent != null)
                {
                    artist = treeView.SelectedNode.Parent.Text;
                    title = temp;
                }
                else
                {
                    artist = temp;
                }
            }


            MyLyricsSetup_SearchTitleDialog std = new MyLyricsSetup_SearchTitleDialog(this, artist, title, CurrentDB.Equals(MyLyricsSettings.LyricsMarkedDB));
        }

        private void btSwitch_Click(object sender, EventArgs e)
        {
            string temp = "";
            string artist = "";
            string title = "";

            if (treeView.SelectedNode != null)
            {
                temp = treeView.SelectedNode.Text;

                if (treeView.SelectedNode.Parent != null)
                {
                    artist = treeView.SelectedNode.Parent.Text;
                    title = temp;
                }
                else
                {
                    artist = temp;
                }
            }

            if (artist.Length == 0 && title.Length == 0)
            {
                MessageBox.Show("No artist or track selected");
            }
            else if (title.Length == 0)
            {
                TreeNode artistNode = treeView.SelectedNode;

                LyricsDatabase otherDatabase = null;
                if (CurrentDB.Equals(MyLyricsSettings.LyricsDB))
                {
                    otherDatabase = MyLyricsSettings.LyricsMarkedDB;
                }
                else
                {
                    otherDatabase = MyLyricsSettings.LyricsDB;
                }

                foreach (TreeNode node in artistNode.Nodes)
                {
                    string key = DatabaseUtil.CorrectKeyFormat(artist, node.Text);
                    LyricsItem item = CurrentDB[key];
                    CurrentDB.Remove(key);

                    if (!DatabaseUtil.IsTrackInLyricsDatabase(otherDatabase, artist, item.Title).Equals(DatabaseUtil.LYRIC_NOT_FOUND))
                    {
                        otherDatabase.Add(key, item);
                    }
                    else
                    {
                        otherDatabase[key] = item;
                    }
                }
                updateLyricsTree();
                DatabaseUtil.SerializeDBs();
            }
            else
            {
                string key = DatabaseUtil.CorrectKeyFormat(artist, title);
                LyricsItem item = CurrentDB[key]; 

                // remove song from treeview and current database
                RemoveSong(artist, title);

                // add song to other database and serialize it
                if (CurrentDB.Equals(MyLyricsSettings.LyricsDB))
                {
                    MyLyricsSettings.LyricsMarkedDB.Add(key, item);
                    DatabaseUtil.SerializeDB(MyLyricsSettings.LyricsMarkedDB);
                }
                else
                {
                    MyLyricsSettings.LyricsDB.Add(key, item);
                    DatabaseUtil.SerializeDB(MyLyricsSettings.LyricsDB);
                }
                updateLyricDatabaseStats();
            }

            treeView.Focus();
        }
    }
}
