using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using SlackAPI;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using WK.Libraries.BetterFolderBrowserNS;
using Syroot.Windows.IO;

namespace SlackFileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SlackClient client;
        public string VERSIONSTRING { get; } = "Slack File Manager v0.2";

        public string Oauth_Value
        {
            get { return (string)GetValue(Oauth_Property); }
            set { SetValue(Oauth_Property, value); }
        }
        public static readonly DependencyProperty Oauth_Property =
            DependencyProperty.Register("Oauth_Value", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        List<ResponseChannel> responseChannels;
        List<ResponseUser> responseUsers;
        List<ResponseFile> responseFiles;
        HashSet<string> fileTypes;

        int maxFilesTotal, maxFilesPerPage;
        int apiCallDelay = 1250;

        struct FileListOptions
        {
            public string channel;
            public string userId;
            public DateTime? from;
            public DateTime? to;
            public int? count;
            public int? page;
            public FileTypes types;
        }


        public MainWindow()
        {
            InitializeComponent();
            responseChannels = new List<ResponseChannel>();
            responseUsers = new List<ResponseUser>();
            responseFiles = new List<ResponseFile>();
            slackFiles.DataContext = this;
            slackFiles.ItemsSource = responseFiles;
            slackFiles.Items.Refresh();

            Properties.Settings.Default.Reload();
            Oauth_Value = Properties.Settings.Default.oauth;
            SaveToken.IsChecked = Properties.Settings.Default.saveToken;

            ComboFileTypes.ItemsSource = Enum.GetValues(typeof(SlackAPI.FileTypes)).Cast<SlackAPI.FileTypes>();
            ComboFileTypes.SelectedIndex = ComboFileTypes.Items.Count - 1;

            SetStatus("Not connected");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // Just so we can call it via lambda which is nicer
        private void RaisePropertyChanged<T>(Expression<Func<T>> propertyNameExpression)
        {
            RaisePropertyChanged(((MemberExpression)propertyNameExpression.Body).Member.Name);
        }

        private void ClearslackFiles()
        {
            slackFiles.ItemsSource = null;
            responseFiles.Clear();
            slackFiles.ItemsSource = responseFiles;
            slackFiles.Items.Refresh();
        }

        private SlackClient ConnectToSlack()
        {
            if (client == null)
            {
                client = new SlackClient(Oauth_Value);
            }
            return client;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (Oauth_Value == "")
            {
                MessageBoxResult dialogResult = MessageBox.Show($"Oauth field is empty", "Error", MessageBoxButton.OK);
                return;
            }

            SetStatus("Connecting...");
            if (ConnectToSlack() == null) { SetStatus("Can't connect. Maybe invalid Oauth?"); return; }

            ClearslackFiles();

            maxFilesTotal = (int)MaxFiles.Value;
            maxFilesPerPage = Math.Min(maxFilesTotal, 100);

            if (ComboUser.Items.Count == 0) GetUsersFromSlack();

            // First Run
            var options = new FileListOptions()
            {
                channel = (ComboChannels.SelectedItem == null) ? null : (ComboChannels.SelectedItem as ResponseChannel).id,
                userId = (ComboUser.SelectedItem == null) ? null : (ComboUser.SelectedItem as ResponseUser).id,
                from = DateFrom.SelectedDate,
                to = DateTo.SelectedDate,
                count = maxFilesPerPage,
                page = null,
                types = (FileTypes)ComboFileTypes.SelectedItem
            };

            GetFilesFromSlack(options);

        }

        private void GetChannelsFromSlack()
        {
            if (client == null) return;
            client.GetChannelList((response) =>
            {
                SetStatus("Receiving channel list...");
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
                    AddChannels(response);
                }));

            });
        }

        private void GetUsersFromSlack()
        {
            if (client == null) return;
            client.GetUserList((response) =>
            {
                SetStatus("Receiving user list...");
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
                    AddUsers(response);
                }));

            });
        }

        private void GetFilesFromSlack(FileListOptions options)
        {
            if (client == null) return;
            client.GetFiles((response) => {
                                if (options.page == null)
                                    SetStatus($"Receiving first page...");
                                else
                                    SetStatus($"Receiving page {options.page}...");
                                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { AddFiles(response, options); }));
                            },
                            channel: options.channel,
                            userId: options.userId,
                            from: options.from,
                            to: options.to,
                            count: options.count,
                            page: options.page,
                            types: options.types
            );
        }

        private void PopulateFilterByFileType()
        {
            fileTypes = new HashSet<string>();
            fileTypes.Add("");
            foreach (var file in responseFiles)
            {
                fileTypes.Add(file.filetype);
            }
            FilterByFileType.ItemsSource = fileTypes;
            FilterByFileType.Items.Refresh();
        }

        private void AddChannels(ChannelListResponse response)
        {
            responseChannels.Clear();
            foreach (var channel in response.channels)
            {
                responseChannels.Add(ResponseChannel.FromChannel(channel));
            }
            ComboChannels.ItemsSource = null;
            responseChannels.Insert(0, ResponseChannel.GetAllChannel());  // insert the All Users user on top
            ComboChannels.ItemsSource = responseChannels;
            ComboChannels.Items.Refresh();
            ComboChannels.SelectedIndex = 0;
            SetStatus($"Channellist received. ({responseChannels.Count-1} channels)");
        }

        private void AddUsers(UserListResponse response)
        {
            //ComboUser.Items.Clear();
            responseUsers.Clear();
            foreach (var user in response.members)
            {
                responseUsers.Add(ResponseUser.FromUser(user));
            }
            ComboUser.ItemsSource = null;
            responseUsers.Insert(0, ResponseUser.GetAllUser());  // insert the All Users user on top
            ComboUser.ItemsSource = responseUsers;
            ComboUser.Items.Refresh();
            ComboUser.SelectedIndex = 0;
            SetStatus($"Userlist received. ({responseUsers.Count-1} users)");
        }

        private void AddFiles(FileListResponse response, FileListOptions options)
        {
            foreach (var file in response.files)
            {
                if (responseFiles.Count >= maxFilesTotal) break;
                var newFile = ResponseFile.FromFile(file);
                var res = responseUsers.Where(x => x.id == file.user);
                if (res.Count() > 0)
                {
                    newFile.linkedUser = res.First();
                    if (file.username == "")
                        newFile.username = res.First().name;
                }
                responseFiles.Add(newFile);
            }
            slackFiles.Items.Refresh();
            PopulateFilterByFileType();
            SetStatus($"Page received. (Received: {response.paging.page} Available: {response.paging.pages})");
            SetProgress((response.paging.page) / ((double)maxFilesTotal/maxFilesPerPage) * 100.0, $"Receiving {response.paging.page} / {Math.Ceiling((double)maxFilesTotal / (double)maxFilesPerPage)}");
            if (responseFiles.Count < maxFilesTotal && response.paging.page < response.paging.pages)
            {
                SetStatus($"Delaying next request by {apiCallDelay}ms...");
                Task.Delay(apiCallDelay).ContinueWith(t => {
                    options.page = response.paging.page + 1; GetFilesFromSlack(options);
                });
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (SaveToken.IsChecked == true)
            {
                if (Properties.Settings.Default.oauth == null)
                {
                    Properties.Settings.Default.oauth = "";
                }
                Properties.Settings.Default.oauth = Oauth_Value;
            }
            Properties.Settings.Default.saveToken = SaveToken.IsChecked == true ? true : false;
            Properties.Settings.Default.Save();
        }

        private void slackFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = slackFiles.SelectedItem as ResponseFile;
            propertyGrid.SelectedObject = selected;
            GetPreview(selected);
            
        }

        /// <summary>
        /// Fetch thumbnail image from slack. Automatically picks largest available or hides thumbnail if none.
        /// </summary>
        /// <param name="selected"></param>
        private async void GetPreview(ResponseFile selected)
        {
            if (selected == null)
            {
                PreviewBrowser.Visibility = Visibility.Hidden;
                PreviewThumbnail.Visibility = Visibility.Hidden;
                return;
            }

            Uri imgUrl;
            if (selected.thumb_480 != null) { imgUrl = new Uri(selected.thumb_480); }
            else if (selected.thumb_360 != null) { imgUrl = new Uri(selected.thumb_360); }
            else if (selected.thumb_160 != null) { imgUrl = new Uri(selected.thumb_160); }
            else
            {
                PreviewThumbnail.Visibility = Visibility.Hidden;
                PreviewBrowser.Visibility = Visibility.Visible;
                PreviewBrowser.NavigateToString(selected.preview);
                return;
            }
            
            var request_data = new NameValueCollection();
            request_data.Add("Content-Type", "application/x-www-form-urlencoded");
            request_data.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36");

            var webclient = new WebClient();
            webclient.Encoding = Encoding.UTF8;
            webclient.Proxy = WebRequest.DefaultWebProxy;
            webclient.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + Oauth_Value);

            try
            {
                var imageData = await webclient.DownloadDataTaskAsync(imgUrl);

                var bitmapImage = new BitmapImage { CacheOption = BitmapCacheOption.Default };
                var encoder = new PngBitmapEncoder();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(imageData);
                bitmapImage.EndInit();

                PreviewBrowser.Visibility = Visibility.Hidden;
                PreviewThumbnail.Visibility = Visibility.Visible;
                PreviewThumbnail.Source = bitmapImage;
            }
            catch
            {
                PreviewBrowser.Visibility = Visibility.Hidden;
                PreviewThumbnail.Visibility = Visibility.Hidden;
            }

            webclient.Dispose();
        }

        private void TogglePropertyGrid_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnPropertyGrid.Width.Value > 0)
            {
                ColumnPropertyGrid.Width = new GridLength(0);
            }
            else
            {
                ColumnPropertyGrid.Width = new GridLength(300);
            }
        }

        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            DoDelete();
        }

        private void DoDelete()
        {
            var file = slackFiles.SelectedItem as ResponseFile;
            MessageBoxResult dialogResult = MessageBox.Show($"Are you sure you wish to delete the file: {file.name}", "Are you sure?", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                client.DeleteFile((response) => {
                    if (response.ok)
                    {
                        file.is_deleted = true;
                        SlackFilesRefresh();
                        SetStatus($"Deleted file  {file.name}");
                        Debug.WriteLine($"Deleted {file.name}: {response.ok}");
                    }
                    else
                    {
                        Debug.WriteLine($"Error deleting file {file.name}: {response.ok}");
                    }
                }, file.id);
            }
            else if (dialogResult == MessageBoxResult.No)
            {
                return;
            }
        }

        private void RefreshChannels_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectToSlack() == null) { SetStatus("Can't connect. Maybe invalid Oauth?"); return; }
            GetChannelsFromSlack();
        }

        private void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectToSlack() == null) { SetStatus("Can't connect. Maybe invalid Oauth?"); return; }
            GetUsersFromSlack();
        }

        private void FilterByName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            slackFiles.Items.Filter = new Predicate<object>(
                item => (
                    ((ResponseFile)item).name.Contains(FilterByName.Text) &&
                    ((ResponseFile)item).username.Contains(FilterByUserName.Text) &&
                    ((ResponseFile)item).filetype.Contains(FilterByFileType.SelectedValue.ToString())
                    ));
        }

        private void FilterByUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var files = slackFiles.SelectedItems.Cast<ResponseFile>().ToList();
            if (files==null || files.Count == 0) return;

            MessageBoxResult dialogResult = MessageBox.Show($"Are you sure you wish to delete the {files.Count} files?", "Are you sure?", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                DeleteFiles(files);
            }
            else if (dialogResult == MessageBoxResult.No)
            {
                return;
            }
        }

        private void DownloadSelected_Click(object sender, RoutedEventArgs e)
        {
            DoDownload();
        }

        private async void DoDownload()
        {
            var files = slackFiles.SelectedItems.Cast<ResponseFile>().ToList();
            if (files == null || files.Count == 0) return;

            var bfb = new BetterFolderBrowser();
            bfb.Multiselect = false;
            bfb.Title = "Select Output Folder";
            bfb.RootFolder = KnownFolders.Downloads.Path;

            var bfbResult = bfb.ShowDialog();

            if (bfbResult == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(bfb.SelectedFolder))
                {
                    Directory.CreateDirectory(bfb.SelectedFolder);
                }
                await DownloadFilesAsync(files, bfb.SelectedFolder);
            }
            bfb.Dispose();
        }

        private async Task DownloadFilesAsync(List<ResponseFile> files, string outputFolder)
        {
            var request_data = new NameValueCollection();
            request_data.Add("Content-Type", "application/x-www-form-urlencoded");
            request_data.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.118 Safari/537.36");

            var webclient = new WebClient();
            webclient.Encoding = Encoding.UTF8;
            webclient.Proxy = WebRequest.DefaultWebProxy;
            webclient.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + Oauth_Value);

            int currentFile = 0;
            foreach (var file in files)
            {
                try
                {
                    string combinedPath = System.IO.Path.Combine(outputFolder, file.name);
                    SetStatus($"Downloading file {file.name}");
                    SetProgress((currentFile + 1) / (double)files.Count * 100.0, $"Downloading {currentFile + 1} / {files.Count}");
                    await webclient.DownloadFileTaskAsync(new Uri(file.url_private_download), combinedPath);
                }
                catch
                {
                    MessageBox.Show($"Could not download {file.name}");
                }
            }
            SetStatus($"Download finished.");
            SetProgress(100.0, $"");
            webclient.Dispose();
        }

        private void DeleteFiles(List<ResponseFile> files, int currentFile=0)
        {
            if (files == null || currentFile > files.Count-1) return;

            if (files[currentFile].is_deleted == true)
            {
                SetStatus($"Already deleted file {currentFile + 1} / {files.Count}: {files[currentFile].name}");
                SetProgress((currentFile + 1) / (double)files.Count * 100.0, $"Deleting {currentFile + 1} / {files.Count}");

                if (currentFile + 1 < files.Count - 1)
                {
                    DeleteFiles(files, currentFile + 1);
                }
                return;
            }

            client.DeleteFile((response) => {
                if (response.ok)
                {
                    files[currentFile].is_deleted = true;
                    SlackFilesRefresh();
                    SetStatus($"Deleted file {currentFile + 1} / {files.Count}: {files[currentFile].name}");
                    SetProgress((currentFile + 1) / (double)files.Count * 100.0, $"Deleting {currentFile+1} / {files.Count}");
                    Debug.WriteLine($"Deleted {files[currentFile].name}: {response.ok}");
                }
                else
                {
                    Debug.WriteLine($"Error deleting file {files[currentFile].name}: {response.ok}");
                }
                if (currentFile+1 < files.Count)
                {
                    Task.Delay(apiCallDelay).ContinueWith(t => {
                        DeleteFiles(files, currentFile+1);
                    });
                }
            }, files[currentFile].id);
        }
        
        private void SlackFilesRefresh()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { slackFiles.Items.Refresh(); }));
        }

        private void SetStatus(string statusText)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { StatusLabel.Content = statusText; }));
        }

        private void FilterByFileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SlackFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DoDelete();
            }
            else if (e.Key == Key.D && ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
            {
                DoDownload();
            }
        }

        private void SetProgress(double percent, string label="")
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { Progress.Value = percent; ProgressLabel.Content = label; }));
        }

    }
}
