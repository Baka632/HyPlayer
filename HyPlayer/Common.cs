﻿using HyPlayer.Classes;
using HyPlayer.Controls;
using HyPlayer.Pages;
using NeteaseCloudMusicApi;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Kawazu;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HyPlayer
{
    internal class Common
    {
        public static NeteaseCloudMusicApi.CloudMusicApi ncapi = new CloudMusicApi();
        public static bool Logined = false;
        public static NCUser LoginedUser;
        public static ExpandedPlayer PageExpandedPlayer;
        public static MainPage PageMain;
        public static PlayBar BarPlayBar;
        public static Frame BaseFrame;
        public static BasePage PageBase;
        public static Setting Setting = new Setting();
        public static bool ShowLyricSound = true;
        public static bool ShowLyricTrans = true;
        public static Dictionary<string, object> GLOBAL = new Dictionary<string, object>();
        public static List<string> LikedSongs = new List<string>();
        public static KawazuConverter KawazuConv = null;
        public static List<NCPlayList> MySongLists = new List<NCPlayList>();
        public static List<NCSong> ListedSongs = new List<NCSong>();

        public static async void Invoke(Action action, Windows.UI.Core.CoreDispatcherPriority Priority = Windows.UI.Core.CoreDispatcherPriority.Normal)
        {
            try
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Priority,
                    () => { action(); });
            }
            catch (Exception e)
            {
#if RELEASE
                Crashes.TrackError(e);
#endif
                /*
                Invoke((async () =>
                {
                    await new ContentDialog
                    {
                        Title = "发生错误",
                        Content = "Error: " + e.Message + "\r\n" + e.StackTrace,
                        CloseButtonText = "关闭",
                        DefaultButton = ContentDialogButton.Close
                    }.ShowAsync();
                }));
                */
            }

        }
    }

    internal class Setting : INotifyPropertyChanged
    {
        public string audioRate
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("audioRate"))
                    return ApplicationData.Current.LocalSettings.Values["audioRate"].ToString();
                return "999000";
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["audioRate"] = value;
                OnPropertyChanged();
            }
        }

        public int Volume
        {
            get
            {
                try
                {
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Volume"))
                        return int.Parse(ApplicationData.Current.LocalSettings.Values["Volume"].ToString());
                }
                catch
                {
                    return 50;
                }
                return 50;
            }

            set => ApplicationData.Current.LocalSettings.Values["Volume"] = value;
        }

        public string downloadDir
        {
            get
            {
                if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("downloadDir"))
                    ApplicationData.Current.LocalSettings.Values["downloadDir"] = KnownFolders.MusicLibrary.CreateFolderAsync("HyPlayer", CreationCollisionOption.OpenIfExists).AsTask().Result.Path;
                return ApplicationData.Current.LocalSettings.Values["downloadDir"].ToString();

            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["downloadDir"] = value;
                OnPropertyChanged();
            }
        }

        public bool toastLyric
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("toastLyric"))
                    return ApplicationData.Current.LocalSettings.Values["toastLyric"].ToString() == "true";
                return false;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["toastLyric"] = value ? "true" : "false";
                OnPropertyChanged();
            }
        }

        public bool expandAnimation
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("expandAnimation"))
                    return ApplicationData.Current.LocalSettings.Values["expandAnimation"].ToString() != "false";
                return true;
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["expandAnimation"] = value ? "true" : "false";
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public async void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                 {
                     PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                 });
        }
    }
    internal class HistoryManagement
    {

        public static void InitializeHistoryTrack()
        {
            var list = new List<string>();
            if (ApplicationData.Current.LocalSettings.Values["songHistory"] == null)
                ApplicationData.Current.LocalSettings.Values["songHistory"] = JsonConvert.SerializeObject(list);
            if (ApplicationData.Current.LocalSettings.Values["songHistory"].ToString().StartsWith("[{"))
                ApplicationData.Current.LocalSettings.Values["songHistory"] = JsonConvert.SerializeObject(list);
            if (ApplicationData.Current.LocalSettings.Values["searchHistory"] == null)
                ApplicationData.Current.LocalSettings.Values["searchHistory"] = JsonConvert.SerializeObject(list);
            if (ApplicationData.Current.LocalSettings.Values["songlistHistory"] == null)
                ApplicationData.Current.LocalSettings.Values["songlistHistory"] = JsonConvert.SerializeObject(list);

            if (ApplicationData.Current.LocalSettings.Values["songlistHistory"].ToString().StartsWith("[{"))
                ApplicationData.Current.LocalSettings.Values["songlistHistory"] = JsonConvert.SerializeObject(list);
        }

        public static void AddNCSongHistory(string songid)
        {
            var list = new List<string>();
            list = JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["songHistory"].ToString());

            list.Remove(songid);
            list.Insert(0, songid);
            if (list.Count >= 300)
                list.RemoveRange(9, list.Count - 300);
            ApplicationData.Current.LocalSettings.Values["songHistory"] = JsonConvert.SerializeObject(list);
        }
        public static void AddSearchHistory(String Text)
        {
            var list = new List<string>();
            list = JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["searchHistory"].ToString());
            if (!list.Contains(Text))
                list.Insert(0, Text);
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == Text)
                        list.Remove(list[i]);
                }
                list.Insert(0, Text);
            }
            ApplicationData.Current.LocalSettings.Values["searchHistory"] = JsonConvert.SerializeObject(list);
        }
        public static void AddSonglistHistory(string playListid)
        {
            var list = new List<string>();
            list = JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["songlistHistory"].ToString());

            list.Remove(playListid);
            list.Insert(0, playListid);
            if (list.Count >= 100)
                list.RemoveRange(100, list.Count - 100);
            ApplicationData.Current.LocalSettings.Values["songlistHistory"] = JsonConvert.SerializeObject(list);
        }
        public static void ClearHistory()
        {
            var list = new List<string>();
            ApplicationData.Current.LocalSettings.Values["songlistHistory"] = JsonConvert.SerializeObject(list);
            ApplicationData.Current.LocalSettings.Values["songHistory"] = JsonConvert.SerializeObject(list);
            ApplicationData.Current.LocalSettings.Values["searchHistory"] = JsonConvert.SerializeObject(list);
        }
        public static async Task<List<NCSong>> GetNCSongHistory()
        {
            var retsongs = new List<NCSong>();
            var (isOk, json) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.SongDetail, new Dictionary<string, object> { ["ids"] = string.Join(",", JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["songHistory"].ToString())) });
            if (isOk)
            {
                return json["songs"].ToArray().Select(t => NCSong.CreateFromJson(t)).ToList();
            }
            return new List<NCSong> { };
        }
        public static async Task<List<NCPlayList>> GetSonglistHistory()
        {
            int i = 0;
            var queries = new Dictionary<string, object>();
            foreach (string plid in JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["songlistHistory"].ToString()))
            {
                queries["/api/v6/playlist/detail" + new string('/', i++)] = JsonConvert.SerializeObject(new Dictionary<string, object>
                {
                    ["id"] = plid,
                    ["n"] = 100000,
                    ["s"] = 8
                });
            }
            if (queries.Count == 0) return new List<NCPlayList>();
            var (isOk, json) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.Batch, queries);
            var ret = new List<NCPlayList>();
            for (int k = 0; k < json.Count - 1; k++)
            {
                ret.Add(NCPlayList.CreateFromJson(json["/api/v6/playlist/detail" + new string('/', k)]["playlist"]));
            }
            return ret;
        }
        public static List<String> GetSearchHistory()
        {
            return JsonConvert.DeserializeObject<List<string>>(ApplicationData.Current.LocalSettings.Values["searchHistory"].ToString());
        }
    }
    internal static class Extensions
    {
        public static byte[] ToByteArrayUtf8(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static string ToHexStringLower(this byte[] value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte t in value)
            {
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string ToHexStringUpper(this byte[] value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte t in value)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString();
        }

        public static string ToBase64String(this byte[] value)
        {
            return Convert.ToBase64String(value);
        }

        public static byte[] ComputeMd5(this byte[] value)
        {
            MD5 md5 = MD5.Create();
            return md5.ComputeHash(value);
        }

        public static byte[] RandomBytes(this Random random, int length)
        {
            byte[] buffer = new byte[length];
            random.NextBytes(buffer);
            return buffer;
        }

        public static string Get(this CookieCollection cookies, string name, string defaultValue)
        {
            return cookies[name]?.Value ?? defaultValue;
        }
    }
}
