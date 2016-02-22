using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace ClientFTP
{
    public class FtpClient
    {
        #region Field & Property
        private string _host;
        private string _userName;
        private string _password;
        private string _ftpDirectory;
        private bool _downloadCompleted;
        private bool _uploadCompleted;

        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value;
            }
        }
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
            }
        }
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }

        public string FtpDirectory
        {
            get
            {
                if (_ftpDirectory.StartsWith("ftp://"))
                    return _ftpDirectory;
                else
                    return "ftp://" + _ftpDirectory;
            }
            set
            {
                _ftpDirectory = value;
            }
        }

        public bool DownloadCompleted
        {
            get
            {
                return _downloadCompleted;
            }
            set
            {
                _downloadCompleted = value;
            }
        }

        public bool UploadCompleted
        {
            get
            {
                return _uploadCompleted;
            }
            set
            {
                _uploadCompleted = value;
            }
        }

        #endregion

        public FtpClient()
        {
            _downloadCompleted = true;
            _uploadCompleted = true;
            _ftpDirectory = "";
        }
        
        public FtpClient(string host, string userName, string password)
        {
            _host = host;
            _userName = userName;
            _password = password;
            _ftpDirectory = "ftp://" + _host;
        }

        public ArrayList GetDirectiories()
        {
            ArrayList directories = new ArrayList();
            FtpWebRequest request;
            try
            {
                request =(FtpWebRequest)WebRequest.Create(_ftpDirectory);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(_userName, _password);
                request.KeepAlive = false;
                using(FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    using(StreamReader reader = new StreamReader(stream))
                    {
                        string directory;
                        while((directory = reader.ReadLine())!= null)
                        {
                            directories.Add(directory);
                        }
                    }
                }
                return directories;
            }
            catch
            {
                throw new Exception("Error: Cannot connect with: " + _host);
            }
        }

        public ArrayList ChangeDirectory(string directoryName)
        {
            _ftpDirectory += "/" + directoryName;
            return GetDirectiories();
        }

        public ArrayList ChangeDirectoryUp()
        {
            if(_ftpDirectory != "ftp://" + _host)
            {
                _ftpDirectory = _ftpDirectory.Remove(_ftpDirectory.LastIndexOf("/"), _ftpDirectory.Length - _ftpDirectory.LastIndexOf("/"));
                return GetDirectiories();
            }
            else
            {
                return GetDirectiories();
            }
        }

        public void DownloadFileAsync(string ftpFileName, string localFileName)
        {
            WebClient client = new WebClient();
            try
            {
                Uri uri = new Uri(_ftpDirectory + "/" + ftpFileName);
                FileInfo file = new FileInfo(localFileName);
                if (file.Exists)
                {
                    throw new Exception("Error: File " + localFileName + " exists.");
                }
                else
                {
                    client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.Credentials = new NetworkCredential(_userName, _password);
                    client.DownloadFileAsync(uri, localFileName);
                    _downloadCompleted = false;
                }

            }
            catch (Exception)
            {
                client.Dispose();
                throw new Exception("Error: It's not possible to download file");
            }
        }

        public void UploadFileAsync(string fileName)
        {
            try
            {
                RequestCachePolicy cache = new RequestCachePolicy(RequestCacheLevel.Reload);
                WebClient client = new WebClient();
                FileInfo file = new FileInfo(fileName);
                Uri uri = new Uri((_ftpDirectory + "/" + file.Name).ToString());
                
                client.Credentials = new NetworkCredential(_userName, _password);
                _uploadCompleted = false;
                if (file.Exists)
                {
                    client.UploadFileCompleted += new UploadFileCompletedEventHandler(client_UploadFileCompleted);
                    client.UploadProgressChanged += new UploadProgressChangedEventHandler(client_UploadProgressChanged);
                    client.UploadFileAsync(uri, fileName);
                }

            }
            catch (Exception)
            {
                throw new Exception("Error: Can't upload file");
            }
        }
        public string DeleteFile(string fileName)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_ftpDirectory + "//" + fileName);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(_userName, _password);
                request.KeepAlive = false;
                request.UsePassive = true;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                return response.StatusDescription;
            }
            catch (Exception exc)
            {
                throw new Exception("Error: Cannot delete file" + fileName + " (" +exc.Message + ")");
            }
        }


        private void client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            this.OnUploadProgressChanged(sender, e);
        }

        private void client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            this.OnUploadCompleted(sender, e);
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.OnDownloadProgressChanged(sender, e);
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            this.OnDownloadCompleted(sender, e);
        }

        #region Events
        public delegate void DownProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);
        public event DownProgressChangedEventHandler DownProgressChanged;
        protected virtual void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (DownProgressChanged != null) DownProgressChanged(sender, e);
        }

        public delegate void DownCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
        public event DownCompletedEventHandler DownCompleted;
        protected virtual void OnDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (DownCompleted != null) DownCompleted(sender, e);
        }

        public delegate void UpProgressChangedEventHandler(object sender,UploadProgressChangedEventArgs e);
        public event UpProgressChangedEventHandler UpProgressChanged;
        protected virtual void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (UpProgressChanged != null) UpProgressChanged(sender, e);
        }

        public delegate void UpCompletedEventHandler(object sender, UploadFileCompletedEventArgs e);
        public event UpCompletedEventHandler UpCompleted;
        protected virtual void OnUploadCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (UpCompleted != null) UpCompleted(sender, e);
        }
        #endregion



    }


}
