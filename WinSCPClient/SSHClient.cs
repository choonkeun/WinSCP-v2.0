#define LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WinSCP;
using System.Linq;
using System.Text.RegularExpressions;

namespace WinSCPClient
{
    //public enum Protocol
    //{
    //    Sftp = 0,
    //    Scp = 1,
    //    Ftp = 2,
    //    Webdav = 3,
    //    S3 = 4,
    //}
    //public enum FtpMode
    //{
    //    Passive = 0,
    //    Active = 1,
    //}
    //public enum FtpSecure
    //{
    //    None = 0,
    //    Implicit = 1,
    //    Explicit = 3,
    //}
    //FTPS: Protocol.Ftp && (FtpSecure.Implicit || FtpSecure.Explicit)

    public class SSHClient
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int PortNumber { get; set; }
        public string ProtocolType { get; set; }
        public string FtpSecure { get; set; }
        public Guid SessionNo { get; set; }
        public Boolean SessionStatus { get; set; }
        public string LogData { get; set; }
        public int MaxRetry { get; set; }


        private Protocol protocol { get; set; }
        private FtpMode ftpMode { get; set; }
        private FtpSecure ftpSecure { get; set; }
        private bool WebdavSecure { get; set; }


        private Session session { get; set; }
        private Boolean IsLocked { get; set; }
        public TransferOperationResult transferResult { get; set; }
        public TransferOptions transferOptions { get; set; }


        public event EventHandler<PassingValueByEventArgs> OnTransferProgress;

        string dateFormat = "yyyy-MM-dd hh:mm:ss ";


        public SSHClient()
        {
            SessionNo = Guid.NewGuid();
            SessionStatus = false;
        }

#region Create New_Session
        public Boolean New_Session()
        {
            bool results = ParseProtocol(ProtocolType.ToLower());

            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = protocol,        //Protocol.Sftp,
                HostName = HostName,        //hostname e.g. IP: 192.54.23.32, or mysftpsite.com
                UserName = UserName,
                Password = Password,
                PortNumber = PortNumber,
                SshHostKeyFingerprint = "ssh-rsa 2048 fa:cf:7d:2a:9d:bb:a0:92:11:22:02:5b:d4:21:2f:63" //test ftp
            };

            session = new Session();        //global variable
            SessionStatus = true;
            LogData = string.Empty;

            //Session
            try
            {
                session.FileTransferred += OnFileTransferred;
                session.FileTransferProgress += OnFileTransferProgress;
                session.ExecutablePath = @"C:\Program Files\WinSCP\WinSCP.exe";
                session.Open(sessionOptions);
                //TransferOptions transferOptions = new TransferOptions();
                transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
                transferOptions.FilePermissions = null;
                transferOptions.PreserveTimestamp = false;
                transferOptions.OverwriteMode = OverwriteMode.Overwrite;
                //transferOptions.ResmeSupport.State = TransferResumeSupportState.Off;
            }
            catch (Exception ex)
            {
                SessionStatus = false;
            }
            return SessionStatus;
        }

        private bool ParseProtocol(string ProtocolType)
        {
            bool result = true;
            ftpSecure = WinSCP.FtpSecure.None;

            if (ProtocolType.Equals("sftp", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Sftp;
            }
            else if (ProtocolType.Equals("scp", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Scp;
            }
            else if (ProtocolType.Equals("ftp", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Ftp;
            }
            else if (ProtocolType.Equals("ftps", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Ftp;
                ftpSecure = WinSCP.FtpSecure.Implicit;
            }
            else if (ProtocolType.Equals("ftpes", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Ftp;
                ftpSecure = WinSCP.FtpSecure.Explicit;
            }
            else if (ProtocolType.Equals("dav", StringComparison.OrdinalIgnoreCase) ||
                     ProtocolType.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Webdav;
            }
            else if (ProtocolType.Equals("davs", StringComparison.OrdinalIgnoreCase) ||
                     ProtocolType.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.Webdav;
                WebdavSecure = true;
            }
            else if (ProtocolType.Equals("s3", StringComparison.OrdinalIgnoreCase))
            {
                protocol = WinSCP.Protocol.S3;
            }
            else
            {
                result = false;
            }
            return result;
        }

#endregion

#region Get_FTPFileNames_Filtered
        public Boolean Get_FTPFileNames_Filtered(String remoteFolder, String filter)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    var listFiles = GetFileList(remoteFolder);
                    if (filter == "*")
                        LogData = string.Join( Environment.NewLine, listFiles );
                    else
                        LogData = string.Join(Environment.NewLine, listFiles.Where(item => item.Name.ToLower().Contains(filter)));
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                //transferResult.Check();
                session.Close();
            }
            return SessionStatus;
        }

        //list remoteFile List
        public IEnumerable<RemoteFileInfo> GetFileList(string remoteFolder)
        {
            //session.Open(sessionOptions);
            session.FileExists(remoteFolder);
            var result = session.ListDirectory(remoteFolder);
            return result.Files;
        }

        public FtpFileInfo[] ListFTPFiles(string Remote_FolderPath)
        {
            RemoteDirectoryInfo rdirInfo = session.ListDirectory(Remote_FolderPath);

            var qry = from c in rdirInfo.Files.AsEnumerable()
                      where !(c.IsDirectory || c.IsThisDirectory || c.IsParentDirectory)
                      select new FtpFileInfo
                      {
                          FullName = c.FullName,
                          LastWriteTime = c.LastWriteTime,
                          Length = c.Length,
                          Name = c.Name
                      };

            return qry.ToArray();
        }

        #endregion




#region upload
        public Boolean UploadFile(String localPath, String localFileName, String remoteFolder)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";
                    string[] localFiles = localFileName.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    string[] fileList = localFiles.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    //localFileName = localFileName.Replace(Environment.NewLine, String.Empty);
                    localFileName = fileList[0];
                    LogData = UploadEachFile(Path.Combine(localPath, localFileName), remoteFolder);
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }
            return SessionStatus;
        }

        public Boolean UploadFiles(String localPath, String[] localFileList, String remoteFolder)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    string[] fileList = localFileList.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach (var item in fileList)
                    {
                        LogData += UploadEachFile(Path.Combine(localPath, item), remoteFolder);
                    }
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }
            return SessionStatus;
        }

        private string UploadEachFile(String localFullPath, String remoteFolder)
        {
            session.FileExists(remoteFolder);
            FileInfo localFilepath = new FileInfo(localFullPath);
            if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";
            string remoteFullPath = remoteFolder + Path.GetFileName(localFullPath);
            var result = session.PutFiles(localFilepath.FullName, remoteFullPath, false, transferOptions);
            //result.Check();     // Throw on any error & quit
            if (result.IsSuccess)
            {
                return localFullPath + " upload" + Environment.NewLine;
            }
            else
            {
                return localFullPath + " upload failed" + Environment.NewLine + result.Failures[0].Message + Environment.NewLine;
            }
            //return localFullPath + " uploaded" + Environment.NewLine;
        }

#endregion

#region upload_Filtered
        public Boolean Upload_Filtered(String localFolder, String remoteFolder, String filter)
        {
            return SessionStatus;
        }

        private void Upload_FilteredFile(String localFolder, String remoteFolder, String filter)
        {
        }

#endregion


#region download
        public Boolean Download(String localPath, String remoteFolder, String remoteFileName)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    IsLocked = false;
                    string[] remoteFiles = remoteFileName.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    string[] fileList = remoteFiles.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";
                    remoteFileName = fileList[0];
                    string remoteFullPath = remoteFolder + remoteFileName;
                    int retry = 0;
                    LogData += LogMessage(remoteFullPath + " download starts");

                    do
                    {
                        LogData += DownloadEachFile(Path.Combine(localPath, remoteFileName), remoteFullPath);
                        System.Threading.Thread.Sleep(10 * 1000);
                        retry++;
                    } while (IsLocked && retry < MaxRetry);

                    LogData += LogMessage(remoteFullPath + " download ended");
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData += LogMessage(ex.Message);
            }
            finally
            {
                session.Close();
            }
            return SessionStatus;
        }

        public Boolean DownloadFiles(String localFolder, String remoteFolder, String[] remoteFileList)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";
                    string[] fileList = remoteFileList.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach (var item in fileList)
                    {
                        if (item != "." && item != "..")
                        {
                            string remoteFullPath = remoteFolder + item;
                            LogData += DownloadEachFile(Path.Combine(localFolder, item), remoteFullPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }
            return SessionStatus;
        }

        public Boolean DownloadFilterFiles(String localFolder, String remoteFolder, String filter)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";

                    var listFiles = GetFileList(remoteFolder);
                    filter = RegEx_Pattern(filter);
                    Regex regex = new Regex(filter);

                    foreach (var item in listFiles)
                    {
                        if (!item.IsDirectory)
                        {
                            Match match = regex.Match(item.Name);
                            if (match.Success)
                            {
                                string remoteFullPath = remoteFolder + item.Name;
                                LogData += DownloadEachFile(Path.Combine(localFolder, item.Name), item.FullName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }
            return SessionStatus;
        }

        private string RegEx_Pattern(string filter)
        {
            //set anchor
            if (filter.IndexOf('*') != -1)
            {
                string[] wildcards = filter.Split('*');
                if (wildcards[0].Length > 0)     wildcards[0] = "^" + wildcards[0];
                if (wildcards.Last().Length > 0) wildcards[wildcards.Length - 1] = wildcards.Last() + "$";
                filter = string.Join("*", wildcards);
            }

            //translate
            filter = filter.Replace("@", "[a-zA-Z]");
            filter = filter.Replace("#", "[0-9]");
            filter = filter.Replace("+", "[0-9a-zA-Z]");
            //searchPattern = searchPattern.Replace(".", "Any character (except \n newline)");

            //change '*' to '.*' for Regex use
            filter = filter.Replace("*", ".*");

            return filter;
        }

        private string DownloadEachFile(String localFullPath, String remoteFullPath)
        {
            IsLocked = false;
            session.FileExists(remoteFullPath);
            FileInfo localFilepath = new FileInfo(localFullPath);
            try
            {
                TransferOperationResult result = session.GetFiles(remoteFullPath, localFullPath, false, transferOptions);
                //result.IsSuccess
                result.Check();     // Throw on any error & quit, result.IsSuccess
                //string results = "download from " + result.Transfers[0].FileName + " to " + result.Transfers[0].Destination;
                string results = LogMessage(remoteFullPath + " download");
                return results;
            }
            catch (Exception ex)
            {
                IsLocked = ex.Message.IndexOf("Failed to open") > 0 ? true : false;

                string results = remoteFullPath;
                results += IsLocked ? " download failed(Failed to open)" : " download failed" + ex.Message;
                results = LogMessage(results);
                return results;
            }
            //return remoteFullPath + " download failed" + Environment.NewLine;
        }
        #endregion

#region FTP Rename File
        public Boolean RenameFTPFileName(String remoteOldFile, String remoteNewFile)
        {
            //Rename and Move file is same, 
            //if you change file path then the file will be moved to new location and change file name too
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    session.FileExists(remoteOldFile);
                    session.MoveFile(remoteOldFile, remoteNewFile);
                    string results = "rename " + remoteOldFile + " to " + remoteNewFile;
                    LogData = results;
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }

            return SessionStatus;
        }

#endregion

#region FTP Delete File
        public Boolean DeleteFTPFileName(String remoteFolder, String remoteFileName)
        {
            try
            {
                SessionStatus = New_Session();

                LogData = string.Empty;
                if (SessionStatus)
                {
                    string[] remoteFiles = remoteFileName.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    string[] fileList = remoteFiles.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    if (!remoteFolder.EndsWith("/")) remoteFolder = remoteFolder + "/";

                    foreach (var item in fileList)
                    {
                        if (item != "." && item != "..")
                        {
                            string remoteFullPath = remoteFolder + item;
                            session.FileExists(remoteFullPath);
                            session.RemoveFiles(remoteFullPath);
                            string results = remoteFullPath + " is deleted";
                            LogData += results + Environment.NewLine;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SessionStatus = false;
                LogData = ex.Message;
            }
            finally
            {
                session.Close();
            }

            return SessionStatus;
        }

#endregion

        internal bool IsSsh { get { return (protocol == Protocol.Sftp) || (protocol == Protocol.Scp); } }
        internal bool IsTls { get { return GetIsTls(); } }

        private bool GetIsTls()
        {
            return
                ((protocol == Protocol.Ftp) && (ftpSecure != WinSCP.FtpSecure.None)) ||
                ((protocol == Protocol.Webdav) && WebdavSecure) ||
                (protocol == Protocol.S3);
        }

        protected virtual void OnFileTransferred(object sender, TransferEventArgs e)
        {
            if (e.Error == null)
            {
                SessionStatus = true;
                //Logger.Log($"{e.FileName} have been uploaded");
            }
            else
            {
                SessionStatus = false;
                //Logger.Log($"{e.FileName} failed: {e.Error}");
            }
        }

        protected virtual void OnFileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            //Console.WriteLine(e.FileName + ": " + e.FileProgress.ToString());   //e.FileProgress: percent
            var localCopy = OnTransferProgress;
            if (localCopy != null)
            {
                localCopy(this, new PassingValueByEventArgs((long)(e.FileProgress * 100), (long)100, e.FileName));
            }
        }

        private string LogMessage(string message)
        {
            string logOut = DateTime.Now.ToString(dateFormat) + message + Environment.NewLine;
            return logOut;
        }

    }

    public class PassingValueByEventArgs : EventArgs
    {
        public PassingValueByEventArgs(long Processed, long Total, string Name)
        {
            this.Processed = Processed;
            this.Total = Total;
            this.Name = Name;
        }
        public long Processed { get; set; }
        public long Total { get; set; }
        public string Name { get; set; }
    }

    public sealed class FtpFileInfo
    {
        public string FullName { get; internal set; }
        public DateTime LastWriteTime { get; internal set; }
        public long Length { get; internal set; }
        public string Name { get; internal set; }
    }

}
