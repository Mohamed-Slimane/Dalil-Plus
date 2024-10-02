using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;


namespace Dalil
{
    public partial class MainWindow : Window
    {
        public string appFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); //Get the location of the application
        public string projectId = string.Empty; //The project ID
        public string openProjectData = string.Empty; //The project data
        public MainWindow()
        {
            InitializeComponent();
            
            string customFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appdata");
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
            CoreWebView2Environment environment = CoreWebView2Environment.CreateAsync(null, customFolder).Result;


            WebFrame.EnsureCoreWebView2Async(environment);
            WebFrame.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            WebFrame.ContextMenuOpening += WebView_ContextMenuOpening;
            WebFrame.ContentLoading += WebView_ContentLoading;
            WebFrame.NavigationStarting += WebFrame_NavigationStarting;
            WebFrame.NavigationCompleted += WebFrame_NavigationCompleted;
            Closing += MainWindow_Closing;

            WebFrame.Source = new Uri(Path.Combine(appFolder, "wwwroot", "index.html"));
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            WebFrame.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebFrame.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebFrame.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            WebFrame.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            WebFrame.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            if (e.IsSuccess)
            {
                WebFrame.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            }
        }
        private void WebFrame_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!e.Uri.Contains("index.html") && !e.Uri.Contains("app.html") && !e.Uri.Contains("wwwroot"))
            {
                Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }
            
        }

        private void WebFrame_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                if (!openProjectData.Equals(string.Empty))
                {
                    WebFrame.CoreWebView2.PostWebMessageAsString(openProjectData);
                    openProjectData = string.Empty;
                }
            }
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            Title = WebFrame.CoreWebView2.DocumentTitle;
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
                string intentData = e.TryGetWebMessageAsString(); //Read the intent data
                dynamic intentJson = JsonConvert.DeserializeObject(intentData); //Convert the intent data to JSON
                string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); //Get the user's documents folder
                
                string outputsFolder = Path.Combine(appFolder, "outputs"); //Get the location of the outputs folder
                string projectsFolder = Path.Combine(appFolder, "projects"); //Get the location of the projects folder
                if (!Directory.Exists(outputsFolder)) { Directory.CreateDirectory(outputsFolder); } //Create the outputs folder if it doesn't exist
                if (!Directory.Exists(projectsFolder)) { Directory.CreateDirectory(projectsFolder); } //Create the projects folder if it doesn't exist
                
                string projectsListFile = Path.Combine(projectsFolder, "list.json"); //Get the location of the projects list file
                
                if (!File.Exists(projectsListFile)){ //If the projects list file doesn't exist
                    JObject emptyProjects = new JObject();
                    File.WriteAllText(projectsListFile, JsonConvert.SerializeObject(emptyProjects, Formatting.Indented)); // Write the empty projects object to the file
                }

                if (intentJson.type == "loadProjects"){ //Get the projects list
                    string projectsList = File.ReadAllText(projectsListFile); //Read the projects list
                    WebFrame.CoreWebView2.PostWebMessageAsString(JsonConvert.SerializeObject(new { type = "loadProjects", data = projectsList })); //Send the projects list
                    return;
                }else if (intentJson.type == "openStart") { //Open the start page
                    WebFrame.Source = new Uri(Path.Combine(appFolder, "wwwroot", "index.html")); //Navigate to the start page
                    return;
                } else if (intentJson.type == "newProject") { //Create a new project

                    string projectsList = File.ReadAllText(projectsListFile); //Read the projects list
                    JObject projectsListJson = JsonConvert.DeserializeObject<JObject>(projectsList); // Convert to JObject

                    string projectName = intentJson.name; //Get the project name
                    Guid uuid = Guid.NewGuid(); //Generate a UUID
                    projectId = uuid.ToString().Replace("-", "").Substring(0, 15); //Generate the project ID
                    projectsListJson[projectId] = projectName; //Add the project to the projects list

                    File.WriteAllText(projectsListFile, JsonConvert.SerializeObject(projectsListJson, Formatting.Indented)); //Write the updated projects list to the projects list file
                    
                    WebFrame.Source = new Uri(Path.Combine(appFolder, "wwwroot", "app.html"));
                    return;

                } else if (intentJson.type == "saveProject") { //Save the project

                    string projectFilePath = Path.Combine(projectsFolder, $"{projectId}.json"); //Get the location of the project file
                    string projectData = JsonConvert.SerializeObject(intentJson.data); //Get the project data
                    File.WriteAllText(projectFilePath, projectData); //Write the project data to the project file
                    MessageBox.Show("تم حفظ المشروع بنجاح!");
                    return;
                }else if (intentJson.type == "openProject") { //Open the project
                    projectId = intentJson.id;
                    try
                    {

                        string projectFilePath = Path.Combine(projectsFolder, $"{projectId}.json"); //Get the location of the project file
                        string projectData = File.ReadAllText(projectFilePath); //Read the project data
                        openProjectData = JsonConvert.SerializeObject(new { type = "openProject", data = projectData });
                    }
                    catch (Exception ex){}
                    WebFrame.Source = new Uri(Path.Combine(appFolder, "wwwroot", "app.html"));
                    return;
                }else if (intentJson.type == "buildProject"){ //Build the project

                    string buildFolderPath = Path.Combine(outputsFolder, projectId);
                    if (!Directory.Exists(buildFolderPath))
                    {
                        Directory.CreateDirectory(buildFolderPath);
                    }
                    string templateFolderPath = Path.Combine(appFolder, "wwwroot", "template");
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(templateFolderPath, buildFolderPath, true);
                    string indexPath = Path.Combine(buildFolderPath, "index.html");
                    string page_content = "let content_data = " + intentJson.data + ";";
                    File.WriteAllText(indexPath, File.ReadAllText(indexPath).Replace("/*datdatdat*/", page_content));
                    MessageBox.Show("تم البناء بنجاح!");
                    return ;
                }else if(intentJson.type == "deleteProject"){ //Delete the project
                    string deleteId = intentJson.id;
                    try
                    {
                        string projectFilePath = Path.Combine(projectsFolder, $"{deleteId}.json"); //Get the location of the project file
                        File.Delete(projectFilePath); //Delete the project 
                    }
                    catch (Exception ex){}
                    string projectsList = File.ReadAllText(projectsListFile); //Read the projects list
                    JObject projectsListJson = JsonConvert.DeserializeObject<JObject>(projectsList); // Convert to JObject
                    projectsListJson.Remove(deleteId); //Remove the project from the projects list

                    File.WriteAllText(projectsListFile, JsonConvert.SerializeObject(projectsListJson, Formatting.Indented)); //Write the updated projects list to the projects list file

                    WebFrame.Reload(); //Reload the page
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error receiving HTML: {ex.Message}");
                return ;
            }
        }

    }
}

