using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api;

namespace DropBoxAPI
{
    /// <summary>
    /// Exemplo de utilização da API Dropbox
    /// </summary>
    public partial class MainWindow : Window
    {

        String LOCAL_FILE = Properties.Settings.Default.LOCAL_FILE;
        String TOKEN = Properties.Settings.Default.TOKEN;
        String REMOTE_FILE = Properties.Settings.Default.REMOTE_FILE;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task Run()
        {
            using (var dbx = Conect())
            {
                var full = await dbx.Users.GetCurrentAccountAsync();
                WriteToTextBox(String.Format("Nome: {0}\nEmail: {1}", full.Name.DisplayName, full.Email));
                //await ListRootFolder(dbx);
                //await Download(dbx, String.Empty, "SGV.exe");
                //await Upload(dbx, "/Teste", "teste.txt", @"C:\Users\walfr\Desktop\Teste.txt");
            }
        }

        private DropboxClient Conect()
        {
            return new DropboxClient(TOKEN);
        }

        private async Task Update()
        {
            ulong count = 0;
            using (var dbx = Conect())
            {
                while (!await Exist(dbx, "/update", "SGV.exe"))
                {
                    WriteToTextBox("\n\nProcurando pela atualização. Aguarde...\n\n", false);
                    count++;
                    if (count >= 10)
                    {
                        WriteToTextBox("\n\nAtualização não encontrada!\n\n", false);
                        EnabledButton(true);
                        return;
                    }
                }
                WriteToTextBox("Atualização encontrada!\n\n", false);
                WriteToTextBox("Removendo arquivo anterior!\n\n", true);
                File.Delete(LOCAL_FILE);
                await Download(dbx, REMOTE_FILE, LOCAL_FILE);
                EnabledButton(true);
            }
        }

        private async Task<bool> Exist(DropboxClient dbx, string folder, string name)
        {
            Dropbox.Api.Files.SearchArg arg = new Dropbox.Api.Files.SearchArg(folder, name);
            
            var result = await dbx.Files.SearchAsync(arg);
            foreach (var item in result.Matches.Where(i => i.Metadata.Name == name))
            {
                return true;
            }
            return false;
        }

        private async Task ListRootFolder(DropboxClient dbx, string folder = "")
        {
            var list = await dbx.Files.ListFolderAsync(folder);

            WriteToTextBox("\n\n### Listagem ###\n\n", true);

            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                WriteToTextBox(String.Format("D {0}/\n", item.Name), true);
            }

            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                WriteToTextBox(String.Format("F{0,8} {1}\n", item.AsFile.Size, item.Name), true);
            }
        }

        private async Task Download(DropboxClient dbx, string remoteFile, string localFile)
        {
            using (var response = await dbx.Files.DownloadAsync(remoteFile))
            {

                if (response != null)
                {
                    WriteToTextBox(String.Format("Download do arquivo {0} iniciado: ", response.Response.Name), true);
                }
                else
                {
                    WriteToTextBox("File not found\n\n", true);
                    return;
                }

                ulong fileSize = response.Response.Size;
                const int bufferSize = 1024 * 1024;
                var buffer = new byte[bufferSize];
                ulong percentageTemp = 0;
                ulong percetage = 0;

                using (var stream = await response.GetContentAsStreamAsync())
                {
                    using (var newFile = new FileStream(localFile, FileMode.Create))
                    {
                        var length = stream.Read(buffer, 0, bufferSize);

                        WriteToTextBox(String.Format("{0}%", percetage), true);
                        while (length > 0)
                        {
                            newFile.Write(buffer, 0, length);
                            percentageTemp = percetage;
                            percetage = 100 * (ulong)newFile.Length / fileSize;
                            WriteToTextBox(String.Format("{0}%",percetage), true,
                                           String.Format("{0}%", percentageTemp));
                            length = stream.Read(buffer, 0, bufferSize);
                        }
                        WriteToTextBox("\n\nDownload concluído!\n\n", true);
                    }
                }
            }

        }

        private async Task Upload(DropboxClient dbx, string remoteFile,  string localFile)
        {
            const int ChuckSize = 4096 * 1024;

            WriteToTextBox(String.Format("\nDownload do arquivo {0} iniciado: \n", localFile), true);
      
            using (var mem = File.Open(localFile, FileMode.Open))
            {
                if (mem.Length < ChuckSize)
                {
                     var updated = await dbx.Files.UploadAsync(remoteFile,
                                        Dropbox.Api.Files.WriteMode.Overwrite.Instance,
                                        body: mem);
                    WriteToTextBox(String.Format("\n\nSaved {0} rev {1}", remoteFile, updated.Rev), true);
                }
                else
                {
                    
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // var task = Task.Run(() => Run());
            //task.Wait();
            MessageBox.Show(REMOTE_FILE.Substring(REMOTE_FILE.LastIndexOf("/")+1));
        }

        private void Button_2_Click(object sender, RoutedEventArgs e)
        {
            button_2.IsEnabled = false;
            var update = Task.Run(() => Update());
            //update.Wait();
        }

        private void WriteToTextBox(string text, bool append = false, string replace = "")
        {
            if (this.label.Dispatcher.CheckAccess())
            {
                if (!append && string.IsNullOrEmpty(replace)) this.label.Content = text;
                if (append && string.IsNullOrEmpty(replace)) this.label.Content += text;
                if (!string.IsNullOrEmpty(replace)) this.label.Content = this.label.Content.ToString().Replace(replace, text);
            }
            else
            {
                this.label.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!append && string.IsNullOrEmpty(replace)) this.label.Content = text;
                    if (append && string.IsNullOrEmpty(replace)) this.label.Content += text;
                    if (!string.IsNullOrEmpty(replace)) this.label.Content = this.label.Content.ToString().Replace(replace, text);
                }));
            }
        }

        private void EnabledButton(bool isEnabled)
        {
            if (this.label.Dispatcher.CheckAccess())
            {
                button.IsEnabled = IsEnabled;
            }
            else
            {
                this.button_2.Dispatcher.BeginInvoke(new Action(() =>
                {
                    button_2.IsEnabled = isEnabled;
                }));
            }     
        }

        
    }
}
