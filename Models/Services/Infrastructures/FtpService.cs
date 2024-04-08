using FluentFTP;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace mes.Models.Services.Application
{
    public class FtpService
    {
        public string _host="";
        public string _username="";
        public string _passwd="";

        public FtpService(string host, string username, string passwd)
        {
            _host= host;
            _username = username;
            _passwd = passwd;
        }
        
        public string FtpUploadFile(string filePath, string destFile)        
        {
            string res = "";
            try
            {
                using (var ftp = new FtpClient(_host, _username, _passwd))
                {
                    ftp.Connect();
                    res = ftp.UploadFile(filePath, destFile).ToString();
                    ftp.Disconnect();
                }
                return res.ToString();
            }
            catch (Exception excp)
            {
                return excp.Message;
            }
        }

        public void FtpDownloadFile(string remoteFile, string localFile)
        {
            using (var ftp = new FtpClient(_host, _username, _passwd))
            {
                ftp.Connect();
                //ftp.SetWorkingDirectory(remoteFolder);
                var res = ftp.DownloadFile(localFile, remoteFile);
                ftp.Disconnect();
            }            
        }

        public List<string> ReadRemoteFtpFile (string remoteFilePath, string localFile)
        {
            List<string> results= new List<string>();
            using (var ftp = new FtpClient(_host, _username, _passwd))
            {
                //TO DO: il file potrebbe non essere presente in remoto
                ftp.Connect();
                var res = ftp.DownloadFile(localFile, remoteFilePath, FtpLocalExists.Overwrite);
                if(res == FtpStatus.Success)
                {
                    results.AddRange(File.ReadAllLines(localFile));                
                }
                else
                {
                    results.Add("");
                    if(File.Exists(localFile)) File.Delete(localFile);
                }                
                ftp.Disconnect();
            }

            return results;         
        }        

        public List<FtpListItem> FtpDir(string remoteFolder)
        {
            List<FtpListItem> result = new List<FtpListItem>();
            try
            {
                using (var ftp = new FtpClient(_host, _username, _passwd))
                {
                    ftp.Connect();
                    result = ftp.GetListing(remoteFolder).ToList();  
                    ftp.Disconnect();                  
                }
            }
            catch (Exception ex)
            {

            }
    
            return result;
        }


        //public List<FtpListItem> FtpListTransformer(List<Renci.SshNet.Sftp.SftpFile> inputList)
        //{
        //    List<FtpListItem> output = new List<FtpListItem>();
//
        //    foreach(var oneSftp in inputList)
        //    {
        //        FtpListItem itemFtp = new FtpListItem ();            
        //        itemFtp.FullName = oneSftp.FullName;
        //        itemFtp.Name = oneSftp.Name;
        //        itemFtp.Type = (oneSftp.IsDirectory)?FtpFileSystemObjectType.Directory: FtpFileSystemObjectType.File;
        //        itemFtp.Created = oneSftp.LastWriteTime;
        //        itemFtp.Modified = oneSftp.LastWriteTime;
//
        //        output.Add(itemFtp);
        //    }
//
        //    return output;
        //}

        //public List<FtpListItem> SftpRemoteDir(List<string> remoteFolder)
        //{
        //    List<FtpListItem> resultList = new List<FtpListItem>();
        //    List<Renci.SshNet.Sftp.SftpFile> remoteDir = new List<Renci.SshNet.Sftp.SftpFile>();
        //    //List<string>tempList = new List<string>();
//
        //    var sftpClient = new SftpClient(_host, 22, _username, _passwd);
        //    sftpClient.Connect();
        //    sftpClient.ChangeDirectory("/");
//
        //    //TO DO: togliere i files "." e ".." - fatto
        //    //TO DO: trasformare List<string> in List <FtpItems>
//
        //    foreach(var oneFolder in remoteFolder)
        //    {
        //        //tempList.AddRange(sftpClient.ListDirectory(oneFolder)
        //        //                        .Where(z => z.Name != ".")
        //        //                        .Where(d => d.Name !="..")
        //        //                        .Select(s => s.FullName));
//
        //        remoteDir.AddRange(sftpClient.ListDirectory(oneFolder)
        //                                .Where(s => s.Name != ".")
        //                                .Where(d => d.Name != ".."));
//
//
        //    }
        //   
        //    sftpClient.Dispose();
        //    return FtpListTransformer(remoteDir);
        //}

        public void SftpDownloadFile(List<KeyValuePair<string,string>> downloadOps)
        {
            var sftpClient = new SftpClient(_host, 22, _username, _passwd);
            sftpClient.ChangeDirectory("/");

            foreach(var oneDownloadOps in downloadOps)
            {
                var files = sftpClient.ListDirectory(oneDownloadOps.Key);
            }
        }
    }
}