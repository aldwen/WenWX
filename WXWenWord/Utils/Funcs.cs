using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using WXWenWord.Models;
using WXWenWord.Utils;

namespace WeiXin.Utils
{
    public static class Funcs
    {
        public static string PostFormData(List<FormItem> list, string uri)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            //请求 
            WebRequest req = WebRequest.Create(uri);
            req.Method = "POST";
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            //组织表单数据 
            StringBuilder sb = new StringBuilder();
            foreach (FormItem item in list)
            {
                switch (item.ParamType)
                {
                    case ParamType.Text:
                        sb.Append("--" + boundary);
                        sb.Append("\r\n");
                        sb.Append("Content-Disposition: form-data; name=\"" + item.Name + "\"");
                        sb.Append("\r\n\r\n");
                        sb.Append(item.Value);
                        sb.Append("\r\n");
                        break;
                    case ParamType.File:
                        sb.Append("--" + boundary);
                        sb.Append("\r\n");
                        sb.Append("Content-Disposition: form-data; name=\""+item.Name+"\"; filename=\""+item.Value+"\"");
                        sb.Append("\r\n");
                        sb.Append("Content-Type: application/octet-stream");
                        sb.Append("\r\n\r\n");
                        break;
                }
            }
            string head = sb.ToString();
            //post字节总长度
            long length = 0;
            byte[] form_data = Encoding.UTF8.GetBytes(head);
            //结尾 
            byte[] foot_data = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            List<FormItem> fileList = list.Where(f => f.ParamType == ParamType.File).ToList();
            length = form_data.Length + foot_data.Length;
            foreach (FormItem fi in fileList)
            {
                FileStream fileStream = new FileStream(fi.Value, FileMode.Open, FileAccess.Read);
                length += fileStream.Length;
                fileStream.Close();
            }
            req.ContentLength = length;           

            Stream requestStream = req.GetRequestStream();
            //发送表单参数 
            requestStream.Write(form_data, 0, form_data.Length);
            foreach (FormItem fd in fileList)
            {
                FileStream fileStream = new FileStream(fd.Value, FileMode.Open, FileAccess.Read);
                //文件内容 
                byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    requestStream.Write(buffer, 0, bytesRead);
                //结尾 
                requestStream.Write(foot_data, 0, foot_data.Length);
            }
            requestStream.Close();

            //响应 
            WebResponse pos = req.GetResponse();
            StreamReader sr = new StreamReader(pos.GetResponseStream(), Encoding.UTF8);
            string html = sr.ReadToEnd().Trim();
            sr.Close();
            if (pos != null)
            {
                pos.Close();
                pos = null;
            }
            if (req != null)
            {
                req = null;
            }
            return html;
        }
        ///
        /// 从URL地址下载文件到本地磁盘
        ///
        /// 本地磁盘地址
        /// URL网址
        ///
        public static string SaveFileFromUrl(string FileName, string Url)
        {
            WebResponse response = null;
            Stream stream = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                response = request.GetResponse();
                stream = response.GetResponseStream();

                if (!response.ContentType.ToLower().StartsWith("text/"))
                {
                    SaveBinaryFile(response, FileName);
                }
                else
                {
                    StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
                    return sr.ReadToEnd();
                }

            }
            catch (Exception err)
            {
                return err.ToString();
            }
            return "complete";
        }
        ///
        /// 将二进制文件保存到磁盘
        ///
        /// 将二进制文件保存到磁盘
        // 将二进制文件保存到磁盘
        private static bool SaveBinaryFile(WebResponse response, string FileName)
        {
            bool Value = true;
            byte[] buffer = new byte[1024];

            try
            {
                if (File.Exists(FileName))
                    File.Delete(FileName);
                Stream outStream = System.IO.File.Create(FileName);
                Stream inStream = response.GetResponseStream();

                int l;
                do
                {
                    l = inStream.Read(buffer, 0, buffer.Length);
                    if (l > 0)
                        outStream.Write(buffer, 0, l);
                }
                while (l > 0);

                outStream.Close();
                inStream.Close();
            }
            catch
            {
                Value = false;
            }
            return Value;
        }
    }
}