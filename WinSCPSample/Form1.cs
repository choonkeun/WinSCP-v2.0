using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using WinSCPClient;


namespace WinSCPSample
{
    public partial class Form1 : Form
    {
        String ftpServer = string.Empty;
        String[] ftpServers = new string[] { "HOST", "test.cal.org" };
        int port = 21;
        String ftpSecure = string.Empty;        //Implicit/Explicit SSL/TLS

        public Form1()
        {
            InitializeComponent();

            if (rdoFTP.Checked)
                rdoFTP_CheckedChanged(this, EventArgs.Empty);
            else if (rdoSFTP.Checked)
                rdoSFTP_CheckedChanged(this, EventArgs.Empty);
            else if (rdoFTPS.Checked)
                rdoFTPS_CheckedChanged(this, EventArgs.Empty);
            else if (rdoFTPES.Checked)
                rdoFTPES_CheckedChanged(this, EventArgs.Empty);

            txtSiteAddress.Text = ftpServer;

            txtFTPFilter.Text = @"convey";
            txtUpFilter.Text = @"S*.txt";
            txtDownFilter.Text = @"*.txt";

        }

        string localFullPath = string.Empty;
        string remoteFullPath = string.Empty;
        bool status = false;

#region list file names
        private void btnListLOCALFiles_Click(object sender, EventArgs e)
        {
            string[] localFiles = Directory.GetFiles(txtLocalFolderPath.Text, txtLocalFilter.Text)
                                     .Select(Path.GetFileName)
                                     .ToArray();
            txtLocalFile.Text = string.Join(Environment.NewLine, localFiles);
        }

        private void btnListFTPFiles_Click(object sender, EventArgs e)
        {
            try
            {
                SSHClient sc = new SSHClient();
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                status = sc.Get_FTPFileNames_Filtered(txtFTPFolderPath.Text, txtFTPFilter.Text.ToLower());


                Console.WriteLine(DateTime.Now);
                if (status)
                {
                    txtFTPFiles.Text = sc.LogData;
                    MessageBox.Show("List Filename(s) is done");
                }
                else
                {
                    txtFTPFiles.Text = string.Empty;
                    MessageBox.Show("List Filename(s) has failed");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
        }
#endregion


#region upload
        private void btnUploadFile_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                status = sc.UploadFile(txtLocalFolderPath.Text, txtLocalFile.Text, txtFTPFolderPath.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Upload is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //throw;
            }
            progressBar1.Value = 0;
        }

        private void btnUploadFiles_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                string[] localFiles = txtLocalFile.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                status = sc.UploadFiles(txtLocalFolderPath.Text, localFiles, txtFTPFolderPath.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Upload is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //throw;
            }
            progressBar1.Value = 0;
        }

        private void btnUploadFiltereFiles_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                string[] localFiles = Directory.GetFiles(txtLocalFolderPath.Text, txtUpFilter.Text)
                         .Select(Path.GetFileName)
                         .ToArray();
                txtLocalFile.Text = string.Join(Environment.NewLine, localFiles);

                status = sc.UploadFiles(txtLocalFolderPath.Text, localFiles, txtFTPFolderPath.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Upload is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //throw;
            }
            progressBar1.Value = 0;
        }

#endregion


#region download
        private void btnDownloadFile_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();
                sc.MaxRetry = int.Parse(txtMaxRetry.Text);      //keep retry to download for maxTimes(minutes)

                status = sc.Download(txtLocalFolderPath.Text, txtFTPFolderPath.Text, txtFTPFiles.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Download is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
            progressBar1.Value = 0;
        }

        private void btnDownFiles_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                string[] remoteFiles = txtFTPFiles.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                status = sc.DownloadFiles(txtLocalFolderPath.Text, txtFTPFolderPath.Text, remoteFiles);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Download is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
            progressBar1.Value = 0;
        }

        private void btnRenameFile_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                if (!txtFTPFolderPath.Text.EndsWith("/")) txtFTPFolderPath.Text = txtFTPFolderPath.Text + "/";

                string remoteOldFile = txtFTPFolderPath.Text + txtFTPFiles.Text.Substring(0, txtFTPFiles.Text.IndexOf(Environment.NewLine));
                string remoteNewFile = txtFTPFolderPath.Text + txtDownFilter.Text;
                status = sc.RenameFTPFileName(remoteOldFile, remoteNewFile);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Download is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
            progressBar1.Value = 0;
        }

        private void btnDeleteFiles_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                status = sc.DeleteFTPFileName(txtFTPFolderPath.Text, txtFTPFiles.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Download is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception: " + ex.Message);
            }
            progressBar1.Value = 0;
        }

        private void btnDownFilterFiles_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            richMessages.Text = string.Empty;

            try
            {
                SSHClient sc = new SSHClient();
                sc.OnTransferProgress += ssh_OnTransferProgress;
                sc.HostName = txtSiteAddress.Text;
                sc.UserName = txtUserId.Text;
                sc.Password = txtPassword.Text;
                sc.PortNumber = int.Parse(txtPort.Text);
                sc.FtpSecure = ftpSecure;
                sc.ProtocolType = GetProtocolType();

                status = sc.DownloadFilterFiles(txtLocalFolderPath.Text, txtFTPFolderPath.Text, txtDownFilter.Text);

                richMessages.Text = sc.LogData;
                //Console.WriteLine(DateTime.Now);

                if (status)
                    MessageBox.Show("Download is done");
                else
                    MessageBox.Show(sc.LogData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //throw;
            }
            progressBar1.Value = 0;
        }

#endregion


        private void rdoFTP_CheckedChanged(object sender, EventArgs e)
        {
            ftpServer = ftpServers[0];
            port = 21;
            txtPort.Text = port.ToString();
            txtSiteAddress.Text = ftpServer;
            txtUserId.Text = "tester";
            txtPassword.Text = "password";
        }
        private void rdoSFTP_CheckedChanged(object sender, EventArgs e)
        {
            //SFTP: protocol that runs over SSH2, plain FTP over port 22
            ftpServer = ftpServers[1];
            port = 22;
            txtPort.Text = port.ToString();
            txtSiteAddress.Text = ftpServer;
            txtUserId.Text = "mmmm";
            txtPassword.Text = "18$";
        }
        private void rdoFTPS_CheckedChanged(object sender, EventArgs e)
        {
            //FTPS:  Implicit SSL/TLS encrypted FTP, plain FTP over port 990
            ftpServer = ftpServers[1];
            port = 990;
            ftpSecure = "Implicit";
            txtPort.Text = port.ToString();
            txtSiteAddress.Text = ftpServer;
            txtUserId.Text = "mmmm";
            txtPassword.Text = "18$";
        }
        private void rdoFTPES_CheckedChanged(object sender, EventArgs e)
        {
            //FTPES: Explicit SSL/TLS encrypted FTP, plain FTP over port 21
            ftpServer = ftpServers[1];
            port = 21;
            ftpSecure = "Explicit";
            txtPort.Text = port.ToString();
            txtSiteAddress.Text = ftpServer;
            txtUserId.Text = "mmmm";
            txtPassword.Text = "18$";
        }

        private string GetProtocolType()
        {
            if (rdoFTPS.Checked)
                return rdoFTPS.Text;
            else if (rdoSFTP.Checked)
                return rdoSFTP.Text;
            else if (rdoFTPS.Checked)
                return rdoFTPS.Text;
            else
                return rdoFTPES.Text;
        }

#region progressBar
        void ssh_OnTransferProgress(object sender, PassingValueByEventArgs e)
        {
            if (progressBar1 != null)
            {
                progressBar1.Value = (int)e.Processed;
                progressBar1.Maximum = (int)e.Total;
                this.Text = e.Total.ToString();    // Set the text.
            }
        }
        #endregion

    }
}
