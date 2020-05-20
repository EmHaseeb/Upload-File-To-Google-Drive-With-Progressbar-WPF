using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Upload_To_Google_Drive
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void btBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 
            dlg.Filter = "All Files|*.*";
            //dlg.DefaultExt = ".db";
            //dlg.Filter = "Backup Files (*.bak)|*.bak|Database Files (*.db)|*.db|SQLite Files (*.sqlite)|*.sqlite|SQL Files (*.sql)|*.sql";
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                string filename = dlg.FileName;
                tbFilepath.Text = filename;
            }
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            Authorize();
        }

        private void Authorize()
        {
            string[] scopes = new string[] { DriveService.Scope.Drive,DriveService.Scope.DriveFile,};
            // Create Client ID & Client Secret From here : https://developers.google.com/drive/api/v3/quickstart/dotnet 
            // Client ID & Client Secret  goes down here.....
            var clientId = "503273467216-0nbq4kpd998cvcktpdius70kr66jdki5.apps.googleusercontent.com";  
            var clientSecret = "11Xrjot6JBqXExIXk96NlAmD";

            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            }, scopes,
            // here is where we Request the user to give us access, or use the Refresh Token that was previously stored in C:\Users\Your_User_Name\AppData\Roaming\MyAppsToken
            Environment.UserName, CancellationToken.None, new FileDataStore("MyAppsToken")).Result;
            //Once consent is recieved, your token will be stored locally on the AppData directory, so that next time you wont be prompted for consent.   

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyAppName",

            });
            service.HttpClient.Timeout = TimeSpan.FromMinutes(100);
            //Long Operations like file uploads might timeout. 100 is just precautionary value, can be set to any reasonable value depending on what you use your service for  
   
            var respocne = uploadFile(service, tbFilepath.Text, "");
            // Third parameter is empty it means it would upload to root directory, if you want to upload under a folder, pass folder's id here Uncomment Line 94.

            MessageBox.Show("Process completed--- Response--" + respocne);
        }

        public Google.Apis.Drive.v3.Data.File uploadFile(DriveService _service, string _uploadFile, string _parent, string _descrp = "Uploaded with .NET!")
        {
            if (System.IO.File.Exists(_uploadFile))
            {
                Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
                body.Name = System.IO.Path.GetFileName(_uploadFile);
                body.Description = _descrp;
                body.MimeType = GetMimeType(_uploadFile);
                // UN-comment the following line if you want to upload to a folder(ID of parent folder need to be send as paramter in above method)
                //body.Parents = new List<string> { _parent };
                byte[] byteArray = System.IO.File.ReadAllBytes(_uploadFile);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = _service.Files.Create(body, stream, GetMimeType(_uploadFile));
                    request.SupportsTeamDrives = true;
                    // You can bind event handler with progress changed event and response recieved(completed event)
                    request.ProgressChanged += Request_ProgressChanged;
                    request.ResponseReceived += Request_ResponseReceived;
                    request.Upload();
                    return request.ResponseBody;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error Occured");
                    return null;
                }
            }
            else
            {
                MessageBox.Show("The file does not exist.", "404");
                return null;
            }
        }

        private static string GetMimeType(string fileName) { string mimeType = "application/unknown"; string ext = System.IO.Path.GetExtension(fileName).ToLower(); Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext); if (regKey != null && regKey.GetValue("Content Type") != null) mimeType = regKey.GetValue("Content Type").ToString(); System.Diagnostics.Debug.WriteLine(mimeType); return mimeType; }

        private void Request_ProgressChanged(Google.Apis.Upload.IUploadProgress obj)
        {
            MessageBox.Show(obj.Status + "\r\r " + obj.BytesSent);
        }

        private void Request_ResponseReceived(Google.Apis.Drive.v3.Data.File obj)
        {
            if (obj != null)
            {
                MessageBox.Show("File was uploaded sucessfully--" + obj.Id);
            }
        }
    }
}
