﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using HyPlayer.Classes;
using HyPlayer.Controls;
using HyPlayer.HyPlayControl;
using NeteaseCloudMusicApi;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages
{
    /// <summary>
    ///     可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ArtistPage : Page
    {
        private NCArtist artist;
        private readonly List<NCSong> songs = new List<NCSong>();

        public ArtistPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var (isOk, res) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.ArtistDetail,
                new Dictionary<string, object> {{"id", (string) e.Parameter}});
            if (isOk)
            {
                artist = NCArtist.CreateFromJson(res["data"]["artist"]);
                if (res["data"]["artist"]["cover"].ToString().StartsWith("http"))
                    ImageRect.ImageSource =
                        new BitmapImage(new Uri(res["data"]["artist"]["cover"] + "?param=" +
                                                StaticSource.PICSIZE_ARTIST_DETAIL_COVER));
                TextBoxArtistName.Text = res["data"]["artist"]["name"].ToString();
                if (res["data"]["artist"]["transNames"].HasValues)
                    TextboxArtistNameTranslated.Text =
                        "译名: " + string.Join(",", res["data"]["artist"]["transNames"].ToObject<string[]>());
                else
                    TextboxArtistNameTranslated.Visibility = Visibility.Collapsed;
                TextBlockDesc.Text = res["data"]["artist"]["briefDesc"].ToString();
                TextBlockInfo.Text = "歌曲数: " + res["data"]["artist"]["musicSize"] + " | 专辑数: " +
                                     res["data"]["artist"]["albumSize"] + " | 视频数: " +
                                     res["data"]["artist"]["mvSize"];
                LoadHotSongs();
            }
        }

        private async void LoadHotSongs()
        {
            var (isok, j1) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.ArtistSongs,
                new Dictionary<string, object> {{"id", artist.id}, {"limit", "10"}});
            if (isok)
            {
                var idx = 0;
                var (isOk, json) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.SongDetail,
                    new Dictionary<string, object>
                        {["ids"] = string.Join(",", j1["songs"].ToList().Select(t => t["id"]))});
                foreach (var jToken in json["songs"])
                {
                    var ncSong = NCSong.CreateFromJson(jToken);
                    var canplay =
                        json["privileges"].ToList().Find(x => x["id"].ToString() == jToken["id"].ToString())[
                            "st"].ToString() == "0";
                    if (canplay) songs.Add(ncSong);

                    HotSongContainer.Children.Add(new SingleNCSong(ncSong, idx++, canplay));
                }
            }
        }

        private void ButtonPlayAll_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Common.Invoke(async () =>
                {
                    HyPlayList.RemoveAllSong();
                    var (isok, json) = await Common.ncapi.RequestAsync(CloudMusicApiProviders.SongUrl,
                        new Dictionary<string, object>
                            {{"id", string.Join(',', songs.Select(t => t.sid))}, {"br", Common.Setting.audioRate}});
                    if (isok)
                    {
                        var arr = json["data"].ToList();
                        for (var i = 0; i < songs.Count; i++)
                        {
                            var token = arr.Find(jt => jt["id"].ToString() == songs[i].sid);
                            if (!token.HasValues) continue;

                            var ncSong = songs[i];

                            var tag = "";
                            if (token["type"].ToString().ToLowerInvariant() == "flac")
                                tag = "SQ";
                            else
                                tag = token["br"].ToObject<int>() / 1000 + "k";
                            var ncp = new PlayItem
                            {
                                tag = tag,
                                Album = ncSong.Album,
                                Artist = ncSong.Artist,
                                subext = token["type"].ToString(),
                                Type = HyPlayItemType.Netease,
                                id = ncSong.sid,
                                Name = ncSong.songname,
                                url = token["url"].ToString(),
                                LengthInMilliseconds = ncSong.LengthInMilliseconds,
                                size = token["size"].ToString(),
                                //md5 = token["md5"].ToString()
                            };
                            HyPlayList.AppendNCPlayItem(ncp);
                        }

                        HyPlayList.SongAppendDone();

                        HyPlayList.SongMoveTo(0);
                    }
                });
            });
        }
    }
}