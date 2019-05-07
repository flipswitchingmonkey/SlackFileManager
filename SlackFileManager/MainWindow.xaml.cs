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

namespace SlackFileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SlackClient client;
        // Hardcoded - bad idea

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

        int maxFilesTotal, maxFilesPerPage;
        int apiCallDelay = 1250;

        struct FileListOptions
        {
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
            SetStatus("Connecting...");
            if (ConnectToSlack() == null) { SetStatus("Can't connect. Maybe invalid Oauth?"); return; }

            
            ClearslackFiles();
            
            //client.GetChannelList((response) => {
            //    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { AddChannels(response.channels); }));
            //});
            maxFilesTotal = (int)MaxFiles.Value;
            maxFilesPerPage = Math.Min(maxFilesTotal, 100);

            if (ComboUser.Items.Count == 0)
                GetUsersFromSlack();

            // First Run
            var options = new FileListOptions()
            {
                userId = (ComboUser.SelectedItem == null) ? null : (ComboUser.SelectedItem as ResponseUser).id,
                from = DateFrom.SelectedDate,
                to = DateTo.SelectedDate,
                count = maxFilesPerPage,
                page = null,
                types = (FileTypes)ComboFileTypes.SelectedItem
            };

            GetFilesFromSlack(options);
            //client.GetFiles((response) => {
            //                    SetStatus("Receiving files...");
            //                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { AddFiles(response); }));
            //                }, 
            //                userId: null,
            //                from: DateFrom.SelectedDate,
            //                to: DateTo.SelectedDate,
            //                count: maxFilesPerPage,
            //                page: null,
            //                types: (FileTypes)ComboFileTypes.SelectedItem
            //);
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
                            userId: options.userId,
                            from: options.from,
                            to: options.to,
                            count: options.count,
                            page: options.page,
                            types: options.types
            );
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
            SetStatus($"Userlist received. ({responseUsers.Count} users)");
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
                    newFile.username = res.First().name;
                }
                responseFiles.Add(newFile);
            }
            slackFiles.Items.Refresh();
            SetStatus($"File list received. Page {response.paging.page}/{response.paging.pages}");
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
            propertyGrid.SelectedObject = slackFiles.SelectedItem;
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
            var file = slackFiles.SelectedItem as ResponseFile;
            MessageBoxResult dialogResult = MessageBox.Show($"Are you sure you wish to delete the file: {file.name}", "Are you sure?", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
            {
                client.DeleteFile((response) => {
                    if (response.ok)
                    {
                        RemoveItemFromSlackFiles(file);
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

        private void RemoveItemFromSlackFiles(ResponseFile file)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
                responseFiles.Remove(file);
                slackFiles.Items.Refresh();
            }));
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
            if (FilterByName.Text == "" && FilterByUserName.Text == "")
            {
                slackFiles.Items.Filter = null;
            }
            else
            {
                slackFiles.Items.Filter = new Predicate<object>(
                    item => (
                        ((ResponseFile)item).name.Contains(FilterByName.Text) &&
                        ((ResponseFile)item).username.Contains(FilterByUserName.Text)
                        ));
            }
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

        private void DeleteFiles(List<ResponseFile> files, int currentFile=0)
        {
            if (files == null || currentFile > files.Count-1) return;

            client.DeleteFile((response) => {
                if (response.ok)
                {
                    RemoveItemFromSlackFiles(files[currentFile]);
                    Debug.WriteLine($"Deleted {files[currentFile].name}: {response.ok}");
                }
                else
                {
                    Debug.WriteLine($"Error deleting file {files[currentFile].name}: {response.ok}");
                }
                if (currentFile+1 < files.Count-1)
                {
                    Task.Delay(apiCallDelay).ContinueWith(t => {
                        DeleteFiles(files, currentFile+1);
                    });
                }
            }, files[currentFile].id);
        }
        
        private void SetStatus(string statusText)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { StatusLabel.Content = statusText; }));
        }

    }
}
