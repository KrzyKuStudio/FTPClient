using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ClientFTP
{
    public partial class FormMain : Form
    {
        private FtpClient _client = new FtpClient();

        public FormMain()
        {
            InitializeComponent();
            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            buttonDownload.Enabled = false;
            buttonUpload.Enabled = false;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                textBoxLocalPath.Text = folderBrowserDialog1.SelectedPath;
        }
        private void GetFtpContent(ArrayList directoriesList) {
            {
                listBoxFtpDir.Items.Clear();
                listBoxFtpDir.Items.Add("[...]");
                directoriesList.Sort();
                foreach(string name in directoriesList)
                {
                    string position = name.Substring(name.LastIndexOf(' ') + 1, name.Length - name.LastIndexOf(' ') - 1);
                    if (position != ".." && position != ".")
                    {
                        switch (name[0])
                        {
                            case 'd':
                                listBoxFtpDir.Items.Add("[" + position + "]");
                                break;
                            case 'l':
                                listBoxFtpDir.Items.Add("->" + position);
                                break;
                            default:
                                listBoxFtpDir.Items.Add(position);
                                break;
                        }
                    }

                }
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if(comboBoxServer.Text != string.Empty && comboBoxServer.Text.Trim()!= string.Empty)
            {
                try
                {
                    string serverName = comboBoxServer.Text;
                    if (serverName.StartsWith("ftp://"))
                        serverName = serverName.Replace("ftp://", "");
                    _client = new FtpClient(serverName, textBoxLogin.Text, maskedTextBoxPass.Text);
                    _client.DownProgressChanged += new FtpClient.DownProgressChangedEventHandler(client_DownProgressChanged);
                    _client.DownCompleted += new FtpClient.DownCompletedEventHandler(client_DownloadFileCompleted);
                    _client.UpProgressChanged += new FtpClient.UpProgressChangedEventHandler(client_UploadProgressChanged);
                    _client.UpCompleted += new FtpClient.UpCompletedEventHandler(client_UploadFileCompleted);
                    GetFtpContent(_client.GetDirectiories());
                    textBoxFtpPath.Text = _client.FtpDirectory;
                    toolStripStatusLabelServer.Text = "Server: ftp://" + _client.Host;
                    buttonConnect.Enabled = false;
                    buttonDisconnect.Enabled = true;
                    buttonDownload.Enabled = true;
                    buttonUpload.Enabled = true;
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Entern the FTP Server name", "Error");
                comboBoxServer.Text = string.Empty;
            }
        }

        private void client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
                _client.UploadCompleted = true;
                buttonDownload.Enabled = true;
                buttonUpload.Enabled = true;
                return;
            }
            _client.UploadCompleted = true;
            buttonDownload.Enabled = true;
            buttonUpload.Enabled = true;
            MessageBox.Show("File uploaded");
            try
            {
                GetFtpContent(_client.GetDirectiories());
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error: " + exc.Message);
            }

            _client.DownloadCompleted = true;

        }

        private void client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            toolStripStatusLabelDownload.Text = "Uploaded: " + (e.BytesSent / (double)1024).ToString() + " kB";
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
            }
            else
                MessageBox.Show("File Downloaded");
            _client.DownloadCompleted = true;
            buttonDownload.Enabled = true;
            buttonUpload.Enabled = true;
        }

        private void client_DownProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            toolStripStatusLabelDownload.Text = "Downloaded: " + (e.BytesReceived / (double)1024).ToString() + " kB";
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            int index = listBoxFtpDir.SelectedIndex;
            if(listBoxFtpDir.Items[index].ToString()[0] != '[')
            {
                if(MessageBox.Show("Download the File?","File Download", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    try
                    {
                        string localFile = textBoxLocalPath.Text + "\\" + listBoxFtpDir.Items[index].ToString();
                        FileInfo fi = new FileInfo(localFile);
                        if (fi.Exists == false)
                        {
                            _client.DownloadFileAsync(listBoxFtpDir.Items[index].ToString(), localFile);
                            buttonDownload.Enabled = false;
                            buttonUpload.Enabled = false;
                        }
                        else
                            MessageBox.Show("File don't exits.");
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "Error");
                    }
                }
            }
        }

        private void listBoxFtpDir_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBoxFtpDir.SelectedIndex;
            try
            {
                if(index > -1)
                {
                    if (index == 0)
                        GetFtpContent(_client.ChangeDirectoryUp());
                    else
                    {
                        if (listBoxFtpDir.Items[index].ToString()[0] == '[')
                        {
                            string directory = listBoxFtpDir.Items[index].ToString().Substring(1, listBoxFtpDir.Items[index].ToString().Length - 2);
                            GetFtpContent(_client.ChangeDirectory(directory));
                        }
                        else
                        {
                            if (listBoxFtpDir.Items[index].ToString()[0] == '-' && listBoxFtpDir.Items[index].ToString()[2] == '.')
                            {
                                string link = listBoxFtpDir.Items[index].ToString().Substring(5, listBoxFtpDir.Items[index].ToString().Length - 5);
                                _client.FtpDirectory = "ftp://" + _client.Host;
                                GetFtpContent(_client.ChangeDirectory(link));
                            }
                            listBoxFtpDir.SelectedIndex = 0;
                        }
                    }
                }
                textBoxFtpPath.Text = _client.FtpDirectory;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void listBoxFtpDir_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.listBoxFtpDir_MouseDoubleClick(sender, null);
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _client.UploadFileAsync(openFileDialog1.FileName);
                    buttonDownload.Enabled = false;
                    buttonUpload.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            _client.DownProgressChanged -= new FtpClient.DownProgressChangedEventHandler(client_DownProgressChanged);
            _client.DownCompleted -= new FtpClient.DownCompletedEventHandler(client_DownloadFileCompleted);
            _client.UpProgressChanged -= new FtpClient.UpProgressChangedEventHandler(client_UploadProgressChanged);
            _client.UpCompleted -= new FtpClient.UpCompletedEventHandler(client_UploadFileCompleted);
            listBoxFtpDir.Items.Clear();
            textBoxFtpPath.Text = "";

            buttonConnect.Enabled = true;
            buttonDisconnect.Enabled = false;
            buttonDownload.Enabled = false;
            buttonUpload.Enabled = false;
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F8)
            {
                int index = listBoxFtpDir.SelectedIndex;
                if(index > -1)
                    if(listBoxFtpDir.Items[index].ToString()[0] != '[')
                    {
                        try
                        {
                            MessageBox.Show(_client.DeleteFile(listBoxFtpDir.Items[index].ToString()));
                            GetFtpContent(_client.GetDirectiories());
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("Cannon delete file (" + exc.Message + ")");
                        }
                    }
            }
        }
    }
}
