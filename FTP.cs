using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace Sony.TMS
{
    public class FTP
    {
        #region 私有变量
        /// <summary>
        /// FTP请求对象
        /// </summary>
        private FtpWebRequest request = null;
        /// <summary>
        /// FTP响应对象
        /// </summary>
        private FtpWebResponse response = null;
        /// <summary>
        /// FTP服务器地址
        /// </summary>
        private Uri uri;
        /// <summary>
        /// 当前工作目录
        /// </summary>
        private string directoryPath;
        /// <summary>
        /// FTP登录用户
        /// </summary>
        private string userName;
        /// <summary>
        /// 错误信息
        /// </summary>
        private string errorMsg;
        /// <summary>
        /// FTP登录密码
        /// </summary>
        private string password;
        /// <summary>
        /// 连接FTP服务器的代理服务
        /// </summary>
        private WebProxy proxy = null;
        /// <summary>
        /// 是否需要删除临时文件
        /// </summary>
        private bool isDeleteTempFile = false;
        /// <summary>
        /// 异步上传所临时生成的文件
        /// </summary>
        private string uploadTempFile = "";

        #endregion

        #region 属性信息
        /// <summary>
        /// FTP服务器地址
        /// </summary>
        public Uri Uri
        {
            get
            {
                if (directoryPath == "/")
                {
                    return uri;
                }
                else
                {
                    string strUri = uri.ToString();
                    if (strUri.EndsWith("/"))
                    {
                        strUri = strUri.Substring(0, strUri.Length - 1);
                    }
                    return new Uri(strUri + this.DirectoryPath);
                }
            }
            set
            {
                if (value.Scheme != Uri.UriSchemeFtp)
                {
                    throw new Exception("Ftp 地址格式错误!");
                }
                uri = new Uri(value.GetLeftPart(UriPartial.Authority));
                directoryPath = value.AbsolutePath;
                if (!directoryPath.EndsWith("/"))
                {
                    directoryPath += "/";
                }
            }
        }

        /// <summary>
        /// 当前工作目录
        /// </summary>
        public string DirectoryPath
        {
            get { return directoryPath; }
            set { directoryPath = value; }
        }

        /// <summary>
        /// FTP登录用户
        /// </summary>
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMsg
        {
            get { return errorMsg; }
            set { errorMsg = value; }
        }

        /// <summary>
        /// FTP登录密码
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        /// <summary>
        /// 连接FTP服务器的代理服务
        /// </summary>
        public WebProxy Proxy
        {
            get
            {
                return proxy;
            }
            set
            {
                proxy = value;
            }
        }

        #endregion

        #region 事件
        /// <summary>
        /// 异步下载进度发生改变触发的事件
        /// </summary>
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;
        /// <summary>
        /// 异步下载文件完成之后触发的事件
        /// </summary>
        public event EventHandler<System.ComponentModel.AsyncCompletedEventArgs> DownloadDataCompleted;
        /// <summary>
        /// 异步上传进度发生改变触发的事件
        /// </summary>
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged;
        /// <summary>
        /// 异步上传文件完成之后触发的事件
        /// </summary>
        public event EventHandler<UploadFileCompletedEventArgs> UploadFileCompleted;
        #endregion

        #region 构造析构函数
        /// <summary>
        /// 匿名访问 FTP 服务器
        /// </summary>
        /// <param name="uri">FTP地址</param>
        public FTP(Uri uri)
            : this(uri, "anonymous", "@anonymous", null)
        {
        }
        /// <summary>
        /// 使用用户名密码直接访问 FTP 服务器
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="userName">登录用户名</param>
        /// <param name="password">登录密码</param>
        public FTP(Uri uri, string userName, string password)
            : this(uri, userName, password, null)
        {
        }
        /// <summary>
        /// 使用用户名密码通过代理访问 FTP 服务器
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="userName">登录用户名</param>
        /// <param name="password">登录密码</param>
        /// <param name="proxy">连接代理</param>
        public FTP(Uri uri, string userName, string password, WebProxy proxy)
        {
            this.uri = new Uri(uri.GetLeftPart(UriPartial.Authority));
            directoryPath = uri.AbsolutePath;
            if (!directoryPath.EndsWith("/"))
            {
                directoryPath += "/";
            }
            this.userName = userName;
            this.password = password;
            this.proxy = proxy;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~FTP()
        {
            if (response != null)
            {
                response.Close();
                response = null;
            }
            if (request != null)
            {
                request.Abort();
                request = null;
            }
        }
        #endregion

        #region 建立连接
        /// <summary>
        /// 建立FTP链接,返回响应对象
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="method">操作命令</param>
        private FtpWebResponse Open(Uri uri, string method)
        {
            try
            {
                request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = method;
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(this.UserName, this.Password);
                if (this.Proxy != null)
                {
                    request.Proxy = this.Proxy;
                }
                return (FtpWebResponse)request.GetResponse();
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 建立FTP链接,返回请求对象
        /// </summary>
        /// <param name="uri">FTP地址</param>
        /// <param name="method">操作命令</param>
        private FtpWebRequest OpenRequest(Uri uri, string method)
        {
            try
            {
                request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = method;
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(this.UserName, this.Password);
                if (this.Proxy != null)
                {
                    request.Proxy = this.Proxy;
                }
                return request;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion

        #region 下载文件
        /// <summary>
        /// 从FTP服务器下载文件，使用与远程文件同名的文件名来保存文件
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        /// <param name="localPath">本地路径</param>
        public bool DownloadFile(string remoteFileName, string localPath)
        {
            return DownloadFile(remoteFileName, localPath, Path.GetFileName(remoteFileName));
        }
        /// <summary>
        /// 从FTP服务器下载文件，指定本地路径和本地文件名
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        /// <param name="localPath">本地路径</param>
        /// <param name="localFileName">保存本地的文件名</param>
        /// <param name="overwrite">存在同名文件是是否覆盖</param>
        public bool DownloadFile(string remoteFileName, string localPath, string localFileName, bool overwrite = true)
        {
            byte[] bt = null;
            try
            {
                if (!IsValidFileChars(remoteFileName) || !IsValidFileChars(localFileName) || !IsValidPathChars(localPath))
                {
                    throw new ArgumentException("非法文件名或目录名!");
                }
                if (!Directory.Exists(localPath))
                {
                    Directory.CreateDirectory(localPath);
                }
                string localFullPath = Path.Combine(localPath, localFileName);
                if (!overwrite && File.Exists(localFullPath))
                {
                    throw new ArgumentException("当前路径下已经存在同名文件！");
                }
                bt = DownloadFile(remoteFileName);
                if (bt != null)
                {
                    FileStream stream = new FileStream(localFullPath, FileMode.Create);
                    stream.Write(bt, 0, bt.Length);
                    stream.Flush();
                    stream.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }

        /// <summary>
        /// 从FTP服务器下载文件，返回文件二进制数据
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        public byte[] DownloadFile(string remoteFileName)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName))
                {
                    throw new ArgumentException("非法文件名或目录名!");
                }
                response = Open(new Uri(this.Uri.ToString() + remoteFileName), WebRequestMethods.Ftp.DownloadFile);
                Stream Reader = response.GetResponseStream();

                MemoryStream mem = new MemoryStream(1024 * 500);
                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                int TotalByteRead = 0;
                while (true)
                {
                    bytesRead = Reader.Read(buffer, 0, buffer.Length);
                    TotalByteRead += bytesRead;
                    if (bytesRead == 0)
                        break;
                    mem.Write(buffer, 0, bytesRead);
                }
                if (mem.Length > 0)
                {
                    return mem.ToArray();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 异步下载文件
        /// <summary>
        /// 从FTP服务器异步下载文件，指定本地路径和本地文件名
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>        
        /// <param name="localPath">保存文件的本地路径,后面带有"\"</param>
        /// <param name="localFileName">保存本地的文件名</param>
        /// <param name="overwrite">存在同名文件是是否覆盖</param>
        public void DownloadFileAsync(string remoteFileName, string localPath, string localFileName, bool overwrite = true)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName) || !IsValidFileChars(localFileName) || !IsValidPathChars(localPath))
                {
                    throw new ArgumentException("非法文件名或目录名!");
                }
                if (!Directory.Exists(localPath))
                {
                    Directory.CreateDirectory(localPath);
                }

                string localFullPath = Path.Combine(localPath, localFileName);
                if (!overwrite && File.Exists(localFullPath))
                {
                    throw new ArgumentException("当前路径下已经存在同名文件！");
                }
                DownloadFileAsync(remoteFileName, localFullPath);

            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }

        /// <summary>
        /// 从FTP服务器异步下载文件，指定本地完整路径文件名
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        /// <param name="localFullPath">本地完整路径文件名</param>
        public void DownloadFileAsync(string remoteFileName, string localFullPath)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName))
                {
                    throw new Exception("非法文件名或目录名!");
                }
                if (File.Exists(localFullPath))
                {
                    throw new Exception("当前路径下已经存在同名文件！");
                }
                MyWebClient client = new MyWebClient();

                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);
                client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(OnDownloadFileCompleted);
                client.Credentials = new NetworkCredential(this.UserName, this.Password);
                if (this.Proxy != null)
                {
                    client.Proxy = this.Proxy;
                }
                client.DownloadFileAsync(new Uri(this.Uri.ToString() + remoteFileName), localFullPath);
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }

        /// <summary>
        /// 异步下载进度发生改变触发的事件
        /// </summary>
        /// <param name="sender">下载对象</param>
        /// <param name="e">进度信息对象</param>
        void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(sender, e);
            }
        }

        /// <summary>
        /// 异步下载文件完成之后触发的事件
        /// </summary>
        /// <param name="sender">下载对象</param>
        /// <param name="e">数据信息对象</param>
        void OnDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (DownloadDataCompleted != null)
            {
                DownloadDataCompleted(sender, e);
            }
        }
        #endregion
        #region 上传文件
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件名</param>
        public bool UploadFile(string localFullPath)
        {
            return UploadFile(localFullPath, Path.GetFileName(localFullPath), false);
        }
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public bool UploadFile(string localFullPath, bool overwrite)
        {
            return UploadFile(localFullPath, Path.GetFileName(localFullPath), overwrite);
        }
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        public bool UploadFile(string localFullPath, string remoteFileName)
        {
            return UploadFile(localFullPath, remoteFileName, false);
        }
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件名</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public bool UploadFile(string localFullPath, string remoteFileName, bool overwrite)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName) || !IsValidFileChars(Path.GetFileName(localFullPath)) || !IsValidPathChars(Path.GetDirectoryName(localFullPath)))
                {
                    throw new Exception("非法文件名或目录名!");
                }
                if (File.Exists(localFullPath))
                {
                    if (!overwrite && FileExist(remoteFileName))
                    {
                        throw new FileExistedException(remoteFileName, "FTP服务上面已经存在同名文件！");
                    }
                    FileStream Stream = new FileStream(localFullPath, FileMode.Open, FileAccess.Read);
                    byte[] bt = new byte[Stream.Length];
                    Stream.Read(bt, 0, (Int32)Stream.Length);   //注意，因为Int32的最大限制，最大上传文件只能是大约2G多一点
                    Stream.Close();
                    return UploadFile(bt, remoteFileName, true);
                }
                else
                {
                    throw new Exception("本地文件不存在!");
                }
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="fileBytes">上传的二进制数据</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        public bool UploadFile(byte[] fileBytes, string remoteFileName)
        {
            if (!IsValidFileChars(remoteFileName))
            {
                throw new Exception("非法文件名或目录名!");
            }
            return UploadFile(fileBytes, remoteFileName, false);
        }
        /// <summary>
        /// 上传文件到FTP服务器
        /// </summary>
        /// <param name="fileBytes">文件二进制内容</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public bool UploadFile(byte[] fileBytes, string remoteFileName, bool overwrite)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName))
                {
                    throw new Exception("非法文件名！");
                }
                if (!overwrite && FileExist(remoteFileName))
                {
                    throw new FileExistedException(remoteFileName, "FTP服务上面已经存在同名文件！");
                }
                response = Open(new Uri(this.Uri.ToString() + remoteFileName), WebRequestMethods.Ftp.UploadFile);
                Stream requestStream = request.GetRequestStream();
                MemoryStream mem = new MemoryStream(fileBytes);

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                int TotalRead = 0;
                while (true)
                {
                    bytesRead = mem.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;
                    TotalRead += bytesRead;
                    requestStream.Write(buffer, 0, bytesRead);
                }
                requestStream.Close();
                response = (FtpWebResponse)request.GetResponse();
                mem.Close();
                mem.Dispose();
                fileBytes = null;
                return true;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 异步上传文件
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件名</param>
        public void UploadFileAsync(string localFullPath)
        {
            UploadFileAsync(localFullPath, Path.GetFileName(localFullPath), false);
        }
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public void UploadFileAsync(string localFullPath, bool overwrite)
        {
            UploadFileAsync(localFullPath, Path.GetFileName(localFullPath), overwrite);
        }
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        public void UploadFileAsync(string localFullPath, string remoteFileName)
        {
            UploadFileAsync(localFullPath, remoteFileName, false);
        }
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="localFullPath">本地带有完整路径的文件名</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public void UploadFileAsync(string localFullPath, string remoteFileName, bool overwrite)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName) || !IsValidFileChars(Path.GetFileName(localFullPath)) || !IsValidPathChars(Path.GetDirectoryName(localFullPath)))
                {
                    throw new Exception("非法文件名或目录名!");
                }
                if (!overwrite && FileExist(remoteFileName))
                {
                    throw new FileExistedException(remoteFileName, "FTP服务上面已经存在同名文件！");
                }
                if (File.Exists(localFullPath))
                {
                    MyWebClient client = new MyWebClient();

                    client.UploadProgressChanged += new UploadProgressChangedEventHandler(OnUploadProgressChanged);
                    client.UploadFileCompleted += new UploadFileCompletedEventHandler(OnUploadFileCompleted);
                    client.Credentials = new NetworkCredential(this.UserName, this.Password);
                    if (this.Proxy != null)
                    {
                        client.Proxy = this.Proxy;
                    }
                    client.UploadFileAsync(new Uri(this.Uri.ToString() + remoteFileName), localFullPath);

                }
                else
                {
                    throw new Exception("本地文件不存在!");
                }
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="fileBytes">上传的二进制数据</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        public void UploadFileAsync(byte[] fileBytes, string remoteFileName)
        {
            if (!IsValidFileChars(remoteFileName))
            {
                throw new Exception("非法文件名或目录名!");
            }
            UploadFileAsync(fileBytes, remoteFileName, false);
        }
        /// <summary>
        /// 异步上传文件到FTP服务器
        /// </summary>
        /// <param name="fileBytes">文件二进制内容</param>
        /// <param name="remoteFileName">要在FTP服务器上面保存文件名</param>
        /// <param name="overwrite">是否覆盖远程服务器上面同名的文件</param>
        public void UploadFileAsync(byte[] fileBytes, string remoteFileName, bool overwrite)
        {
            try
            {

                if (!IsValidFileChars(remoteFileName))
                {
                    throw new Exception("非法文件名！");
                }
                if (!overwrite && FileExist(remoteFileName))
                {
                    throw new FileExistedException(remoteFileName, "FTP服务上面已经存在同名文件！");
                }
                string TempPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Templates);
                if (!TempPath.EndsWith("\\"))
                {
                    TempPath += "\\";
                }
                string TempFile = TempPath + Path.GetRandomFileName();
                TempFile = Path.ChangeExtension(TempFile, Path.GetExtension(remoteFileName));
                FileStream Stream = new FileStream(TempFile, FileMode.CreateNew, FileAccess.Write);
                Stream.Write(fileBytes, 0, fileBytes.Length);   //注意，因为Int32的最大限制，最大上传文件只能是大约2G多一点
                Stream.Flush();
                Stream.Close();
                Stream.Dispose();
                isDeleteTempFile = true;
                uploadTempFile = TempFile;
                fileBytes = null;
                UploadFileAsync(TempFile, remoteFileName, overwrite);
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }

        /// <summary>
        /// 异步上传文件完成之后触发的事件
        /// </summary>
        /// <param name="sender">上传对象</param>
        /// <param name="e">数据信息对象</param>
        void OnUploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (isDeleteTempFile)
            {
                if (File.Exists(uploadTempFile))
                {
                    File.SetAttributes(uploadTempFile, FileAttributes.Normal);
                    File.Delete(uploadTempFile);
                }
                isDeleteTempFile = false;
            }
            if (UploadFileCompleted != null)
            {
                UploadFileCompleted(sender, e);
            }
        }

        /// <summary>
        /// 异步上传进度发生改变触发的事件
        /// </summary>
        /// <param name="sender">上传对象</param>
        /// <param name="e">进度信息对象</param>
        void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (UploadProgressChanged != null)
            {
                UploadProgressChanged(sender, e);
            }
        }
        #endregion
        #region 列出目录文件信息
        /// <summary>
        /// 列出FTP服务器上面当前目录的所有文件和目录
        /// </summary>
        public FileStruct[] ListFilesAndDirectories()
        {
            response = Open(this.Uri, WebRequestMethods.Ftp.ListDirectoryDetails);
            StreamReader stream = new StreamReader(response.GetResponseStream(), Encoding.Default);
            string data = stream.ReadToEnd();
            FileStruct[] list = GetList(data);
            return list;
        }
        /// <summary>
        /// 列出FTP服务器上面当前目录的所有文件
        /// </summary>
        public FileStruct[] ListFiles()
        {
            FileStruct[] listAll = ListFilesAndDirectories();
            List<FileStruct> listFile = new List<FileStruct>();
            foreach (FileStruct file in listAll)
            {
                if (!file.IsDirectory)
                {
                    listFile.Add(file);
                }
            }
            return listFile.ToArray();
        }

        /// <summary>
        /// 列出FTP服务器上面当前目录的所有的目录
        /// </summary>
        public FileStruct[] ListDirectories()
        {
            FileStruct[] listAll = ListFilesAndDirectories();
            List<FileStruct> listDirectory = new List<FileStruct>();
            foreach (FileStruct file in listAll)
            {
                if (file.IsDirectory)
                {
                    listDirectory.Add(file);
                }
            }
            return listDirectory.ToArray();
        }
        /// <summary>
        /// 获得文件和目录列表
        /// </summary>
        /// <param name="datastring">FTP返回的列表字符信息</param>
        private FileStruct[] GetList(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();
            string[] dataRecords = datastring.Split('\n');
            FileListStyle directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (directoryListStyle != FileListStyle.Unknown && s != "")
                {
                    FileStruct f = new FileStruct();
                    f.Name = "..";
                    switch (directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                    }
                    if (!(f.Name == "." || f.Name == ".."))
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray.ToArray();
        }

        /// <summary>
        /// 从Windows格式中返回文件信息
        /// </summary>
        /// <param name="record">文件信息</param>
        private FileStruct ParseFileStructFromWindowsStyleRecord(string record)
        {
            FileStruct f = new FileStruct();
            string processstr = record.Trim();
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            DateTimeFormatInfo myDTFI = new CultureInfo("en-US", false).DateTimeFormat;
            myDTFI.ShortTimePattern = "t";
            f.CreateTime = DateTime.Parse(dateStr + " " + timeStr, myDTFI);
            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);   // true);
                processstr = strs[1];
                f.IsDirectory = false;
            }
            f.Name = processstr;
            return f;
        }


        /// <summary>
        /// 判断文件列表的方式Window方式还是Unix方式
        /// </summary>
        /// <param name="recordList">文件信息列表</param>
        private FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                if (s.Length > 10
                 && Regex.IsMatch(s.Substring(0, 10), "(-|d)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)(-|r)(-|w)(-|x)"))
                {
                    return FileListStyle.UnixStyle;
                }
                else if (s.Length > 8
                 && Regex.IsMatch(s.Substring(0, 8), "[0-9][0-9]-[0-9][0-9]-[0-9][0-9]"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }

        /// <summary>
        /// 从Unix格式中返回文件信息
        /// </summary>
        /// <param name="record">文件信息</param>
        private FileStruct ParseFileStructFromUnixStyleRecord(string record)
        {
            FileStruct f = new FileStruct();
            string processstr = record.Trim();
            f.Flags = processstr.Substring(0, 10);
            f.IsDirectory = (f.Flags[0] == 'd');
            processstr = (processstr.Substring(11)).Trim();
            CutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //跳过一部分
            f.Owner = CutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            f.Group = CutSubstringFromStringWithTrim(ref processstr, ' ', 0);
            CutSubstringFromStringWithTrim(ref processstr, ' ', 0);   //跳过一部分
            string yearOrTime = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
            if (yearOrTime.IndexOf(":") >= 0)  //time
            {
                processstr = processstr.Replace(yearOrTime, DateTime.Now.Year.ToString());
            }
            f.CreateTime = DateTime.Parse(CutSubstringFromStringWithTrim(ref processstr, ' ', 8));
            f.Name = processstr;   //最后就是名称
            return f;
        }

        /// <summary>
        /// 按照一定的规则进行字符串截取
        /// </summary>
        /// <param name="s">截取的字符串</param>
        /// <param name="c">查找的字符</param>
        /// <param name="startIndex">查找的位置</param>
        private string CutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }
        #endregion
        #region 目录或文件存在的判断
        /// <summary>
        /// 判断当前目录下指定的子目录是否存在
        /// </summary>
        /// <param name="remoteDirectoryName">指定的目录名</param>
        public bool DirectoryExist(string remoteDirectoryName)
        {
            try
            {
                if (!IsValidPathChars(remoteDirectoryName))
                {
                    throw new Exception("目录名非法！");
                }
                FileStruct[] listDir = ListDirectories();
                foreach (FileStruct dir in listDir)
                {
                    if (dir.Name == remoteDirectoryName)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 判断一个远程文件是否存在服务器当前目录下面
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        public bool FileExist(string remoteFileName)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName))
                {
                    throw new Exception("文件名非法！");
                }
                FileStruct[] listFile = ListFiles();
                foreach (FileStruct file in listFile)
                {
                    if (file.Name == remoteFileName)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 删除文件
        /// <summary>
        /// 从FTP服务器上面删除一个文件
        /// </summary>
        /// <param name="remoteFileName">远程文件名</param>
        public void DeleteFile(string remoteFileName)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName))
                {
                    throw new Exception("文件名非法！");
                }
                response = Open(new Uri(this.Uri.ToString() + remoteFileName), WebRequestMethods.Ftp.DeleteFile);
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 重命名文件
        /// <summary>
        /// 更改一个文件的名称或一个目录的名称
        /// </summary>
        /// <param name="remoteFileName">原始文件或目录名称</param>
        /// <param name="newFileName">新的文件或目录的名称</param>
        public bool ReName(string remoteFileName, string newFileName)
        {
            try
            {
                if (!IsValidFileChars(remoteFileName) || !IsValidFileChars(newFileName))
                {
                    throw new Exception("文件名非法！");
                }
                if (remoteFileName == newFileName)
                {
                    return true;
                }
                if (FileExist(remoteFileName))
                {
                    request = OpenRequest(new Uri(this.Uri.ToString() + remoteFileName), WebRequestMethods.Ftp.Rename);
                    request.RenameTo = newFileName;
                    response = (FtpWebResponse)request.GetResponse();

                }
                else
                {
                    throw new Exception("文件在服务器上不存在！");
                }
                return true;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 拷贝、移动文件
        /// <summary>
        /// 把当前目录下面的一个文件拷贝到服务器上面另外的目录中，注意，拷贝文件之后，当前工作目录还是文件原来所在的目录
        /// </summary>
        /// <param name="remoteFile">当前目录下的文件名</param>
        /// <param name="directoryName">新目录名称。
        /// 说明：如果新目录是当前目录的子目录，则直接指定子目录。如: SubDirectory1/SubDirectory2 ；
        /// 如果新目录不是当前目录的子目录，则必须从根目录一级一级的指定。如： ./NewDirectory/SubDirectory1/SubDirectory2
        /// </param>
        /// <returns></returns>
        public bool CopyFileToAnotherDirectory(string remoteFile, string directoryName)
        {
            string CurrentWorkDir = this.DirectoryPath;
            try
            {
                byte[] bt = DownloadFile(remoteFile);
                GotoDirectory(directoryName);
                bool Success = UploadFile(bt, remoteFile, false);
                this.DirectoryPath = CurrentWorkDir;
                return Success;
            }
            catch (Exception ep)
            {
                this.DirectoryPath = CurrentWorkDir;
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 把当前目录下面的一个文件移动到服务器上面另外的目录中，注意，移动文件之后，当前工作目录还是文件原来所在的目录
        /// </summary>
        /// <param name="remoteFile">当前目录下的文件名</param>
        /// <param name="directoryName">新目录名称。
        /// 说明：如果新目录是当前目录的子目录，则直接指定子目录。如: SubDirectory1/SubDirectory2 ；
        /// 如果新目录不是当前目录的子目录，则必须从根目录一级一级的指定。如： ./NewDirectory/SubDirectory1/SubDirectory2
        /// </param>
        /// <returns></returns>
        public bool MoveFileToAnotherDirectory(string remoteFile, string directoryName)
        {
            string CurrentWorkDir = this.DirectoryPath;
            try
            {
                if (directoryName == "")
                    return false;
                if (!directoryName.StartsWith("/"))
                    directoryName = "/" + directoryName;
                if (!directoryName.EndsWith("/"))
                    directoryName += "/";
                bool Success = ReName(remoteFile, directoryName + remoteFile);
                this.DirectoryPath = CurrentWorkDir;
                return Success;
            }
            catch (Exception ep)
            {
                this.DirectoryPath = CurrentWorkDir;
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 建立、删除子目录
        /// <summary>
        /// 在FTP服务器上当前工作目录建立一个子目录
        /// </summary>
        /// <param name="directoryName">子目录名称</param>
        public bool MakeDirectory(string directoryName)
        {
            try
            {
                if (!IsValidPathChars(directoryName))
                {
                    throw new Exception("目录名非法！");
                }
                if (DirectoryExist(directoryName))
                {
                    throw new Exception("服务器上面已经存在同名的文件名或目录名！");
                }
                response = Open(new Uri(this.Uri.ToString() + directoryName), WebRequestMethods.Ftp.MakeDirectory);
                return true;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 从当前工作目录中删除一个子目录
        /// </summary>
        /// <param name="directoryName">子目录名称</param>
        public bool RemoveDirectory(string directoryName)
        {
            try
            {
                if (!IsValidPathChars(directoryName))
                {
                    throw new Exception("目录名非法！");
                }
                if (!DirectoryExist(directoryName))
                {
                    throw new Exception("服务器上面不存在指定的文件名或目录名！");
                }
                response = Open(new Uri(this.Uri.ToString() + directoryName), WebRequestMethods.Ftp.RemoveDirectory);
                return true;
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        #endregion
        #region 文件、目录名称有效性判断
        /// <summary>
        /// 判断目录名中字符是否合法
        /// </summary>
        /// <param name="directoryName">目录名称</param>
        public bool IsValidPathChars(string directoryName)
        {
            char[] invalidPathChars = Path.GetInvalidPathChars();
            char[] DirChar = directoryName.ToCharArray();
            foreach (char C in DirChar)
            {
                if (Array.BinarySearch(invalidPathChars, C) >= 0)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断文件名中字符是否合法
        /// </summary>
        /// <param name="fileName">文件名称</param>
        public bool IsValidFileChars(string fileName)
        {
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            char[] NameChar = fileName.ToCharArray();
            foreach (char C in NameChar)
            {
                if (Array.BinarySearch(invalidFileChars, C) >= 0)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion
        #region 目录切换操作
        /// <summary>
        /// 进入一个目录
        /// </summary>
        /// <param name="directoryName">
        /// 新目录的名字。 
        /// 说明：如果新目录是当前目录的子目录，则直接指定子目录。如: SubDirectory1/SubDirectory2 ； 
        /// 如果新目录不是当前目录的子目录，则必须从根目录一级一级的指定。如： ./NewDirectory/SubDirectory1/SubDirectory2
        /// </param>
        public bool GotoDirectory(string directoryName)
        {
            string CurrentWorkPath = this.DirectoryPath;
            try
            {
                directoryName = directoryName.Replace("\\", "/");
                string[] DirectoryNames = directoryName.Split(new char[] { '/' });
                if (DirectoryNames[0] == ".")
                {
                    this.DirectoryPath = "/";
                    if (DirectoryNames.Length == 1)
                    {
                        return true;
                    }
                    Array.Clear(DirectoryNames, 0, 1);
                }
                bool Success = false;
                foreach (string dir in DirectoryNames)
                {
                    if (dir != null)
                    {
                        Success = EnterOneSubDirectory(dir);
                        if (!Success)
                        {
                            this.DirectoryPath = CurrentWorkPath;
                            return false;
                        }
                    }
                }
                return Success;

            }
            catch (Exception ep)
            {
                this.DirectoryPath = CurrentWorkPath;
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 从当前工作目录进入一个子目录
        /// </summary>
        /// <param name="directoryName">子目录名称</param>
        private bool EnterOneSubDirectory(string directoryName)
        {
            try
            {
                if (directoryName.IndexOf("/") >= 0 || !IsValidPathChars(directoryName))
                {
                    throw new Exception("目录名非法!");
                }
                if (directoryName.Length > 0 && DirectoryExist(directoryName))
                {
                    if (!directoryName.EndsWith("/"))
                    {
                        directoryName += "/";
                    }
                    directoryPath += directoryName;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ep)
            {
                ErrorMsg = ep.ToString();
                throw ep;
            }
        }
        /// <summary>
        /// 从当前工作目录往上一级目录
        /// </summary>
        public bool ComeoutDirectory()
        {
            if (directoryPath == "/")
            {
                ErrorMsg = "当前目录已经是根目录！";
                throw new Exception("当前目录已经是根目录！");
            }
            char[] sp = new char[1] { '/' };

            string[] strDir = directoryPath.Split(sp, StringSplitOptions.RemoveEmptyEntries);
            if (strDir.Length == 1)
            {
                directoryPath = "/";
            }
            else
            {
                directoryPath = String.Join("/", strDir, 0, strDir.Length - 1);
            }
            return true;

        }
        #endregion
        #region 重载WebClient，支持FTP进度
        internal class MyWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                FtpWebRequest req = (FtpWebRequest)base.GetWebRequest(address);
                req.UsePassive = false;
                return req;
            }
        }
        #endregion
    }

    #region 文件信息结构
    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public string Group;
        public bool IsDirectory;
        public DateTime CreateTime;
        public string Name;
    }
    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }
    #endregion

    public class FileExistedException : Exception
    {
        public string FileName { get; set; }

        public FileExistedException(string fileName)
            : this(fileName, "文件" + fileName + "已存在")
        {
            FileName = @fileName;
        }

        public FileExistedException(string fileName, string msg)
            : base(msg)
        {
            FileName = @fileName;
        }
    }
}
