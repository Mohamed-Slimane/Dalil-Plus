using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Reflection;


namespace Dalil
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            string customFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, customFolder);
            await WebFrame.EnsureCoreWebView2Async(environment);
            WebFrame.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            WebFrame.ContextMenuOpening += WebView_ContextMenuOpening;
            WebFrame.ContentLoading += WebView_ContentLoading;
            Closing += MainWindow_Closing;
            WebFrame.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebFrame.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            WebFrame.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Title = "Dalil+";
            // Load html file
            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");            
        }

        
        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            
            if (e.IsSuccess)
            {
                WebFrame.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            }
        }

        private void WebView_ContentLoading(object sender, CoreWebView2ContentLoadingEventArgs e)
        {
            WebFrame.CoreWebView2.Settings.AreDevToolsEnabled = false;
        }
        private void WebView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = true;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to close the application?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string intent_content = e.TryGetWebMessageAsString();
                dynamic jsonData = JsonConvert.DeserializeObject(intent_content);
                if (jsonData.type == "buildProject")
                {
                    string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string projectFolderName = string.Empty;
                    bool isValidName = false;
                    while (!isValidName)
                    {
                        projectFolderName = Interaction.InputBox("الإسم النهائي للمشروع:", "بناء المشروع", "");

                        if (string.IsNullOrEmpty(projectFolderName))
                        {
                            MessageBox.Show("يجب عليك إدخال اسم المشروع. لا يمكنك ترك الحقل فارغاً.");
                        }
                        else if (ContainsInvalidChars(projectFolderName))
                        {
                            MessageBox.Show("اسم المشروع يحتوي على أحرف غير صالحة. يرجى إدخال اسم صالح.");
                        }
                        else
                        {
                            isValidName = true;
                        }
                    }
                    string newFolderName = "Dalil+\\Output\\" + projectFolderName;
                    string newFolderPath = Path.Combine(userFolder, newFolderName);
                    if (!Directory.Exists(newFolderPath))
                    {
                        Directory.CreateDirectory(newFolderPath);
                    }
                    string templateFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "template");
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(templateFolderPath, newFolderPath, true);
                    string jsFilePath = Path.Combine(newFolderPath, "assets", "data.js");

                    string page_content = "let content_data = " + jsonData.data;
                    File.WriteAllText(jsFilePath, page_content);
                    MessageBox.Show("تم البناء بنجاح!");
                    Process.Start("explorer.exe", newFolderPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving HTML: {ex.Message}");
            }
        }
        private bool ContainsInvalidChars(string folderName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return folderName.IndexOfAny(invalidChars) >= 0;
        }

    }
}

