using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml.Linq;
using WeiXin.Utils;
using WXWenWord.Models;
using WXWenWord.Utils;

namespace WXWenWord.Controllers
{
    public class WeiXinController : Controller
    {
        private string TOKEN = "formylover";

        private static string accesstoken="";
        private static int expireTime = 0;


        public string AccessTOKEN
        {
            get
            {
                int currentTimeMiles = TimeStampUtils.ConvertDateTimeInt(DateTime.Now);
                if (accesstoken == "" || currentTimeMiles > expireTime)
                {
                    string url = "https://api.weixin.qq.com/cgi-bin/token";
                    string param = "grant_type=client_credential&appid=wx390ae98292984a22&secret=bc1451b271f2159a2e08b542d9aa866c";
                    string jsonStr = HttpUtils.HttpGet(url, param);

                    if (jsonStr.IndexOf("access_token")==-1) 
                    {
                        accesstoken = "";
                    }
                    else
                    {
                        AccessTokenResult result = JsonConvert.DeserializeObject<AccessTokenResult>(jsonStr);
                        accesstoken = result.access_token;
                        expireTime = currentTimeMiles + result.expires_int - 200;
                    }
                }
                return accesstoken;
            }
        }
        
        //
        // GET: /Index/
        [HttpGet]
        public string Index(string signature, string timestamp, string nonce, string echostr)
        {
            if (CheckSignature(signature, timestamp, nonce))
                return echostr;
            else
                return "";

        }
        [HttpPost]
        public string Index(string signature, string timestamp, string nonce)
        {
            string resultStr = "";

            //if (!CheckSignature(signature, timestamp, nonce))
            //{
            //    resultStr = "Not send from WeiXin Center";
            //    return resultStr;
            //}

            string receiveMsg = HttpUtils.getPostData(Request);
            XElement msgXml = XElement.Parse(receiveMsg);
            string toUserName_myserver = msgXml.Element("ToUserName").Value;
            string fromUserName_weixinhost = msgXml.Element("FromUserName").Value;
            string msgType = msgXml.Element("MsgType").Value;
            
            string msgId = msgXml.Element("MsgId").Value;

            switch (msgType)
            {
                case "text":
                    string content = msgXml.Element("Content").Value;
                    resultStr = processTextMsg(content, toUserName_myserver, fromUserName_weixinhost);
                    break;
                case "image":
                    string picUrl = msgXml.Element("PicUrl").Value;
                    string mediaId = msgXml.Element("MediaId").Value;
                    resultStr = processImageMsg(mediaId, toUserName_myserver, fromUserName_weixinhost);
                    break;
             
            }


            return resultStr;

        }

        private string processImageMsg(string mediaId, string toUserName_myserver, string fromUserName_weixinhost)
        {
            string resultStr = @"<xml>
                                <ToUserName><![CDATA[{0}]]></ToUserName>
                                <FromUserName><![CDATA[{1}]]></FromUserName>
                                <CreateTime>{2}</CreateTime>
                                <MsgType><![CDATA[image]]></MsgType>
                                <Image>
                                <MediaId><![CDATA[{3}]]></MediaId>
                                </Image>
                                </xml>";
            int createtime=TimeStampUtils.ConvertDateTimeInt(DateTime.Now);
            return string.Format(resultStr, fromUserName_weixinhost, toUserName_myserver, createtime, mediaId);
        }

        private string processTextMsg(string content, string myserver, string weixinhost)
        {
            int createtime=TimeStampUtils.ConvertDateTimeInt(DateTime.Now);
            string returnStr = @"<xml>
                                <ToUserName><![CDATA[{0}]]></ToUserName>
                                <FromUserName><![CDATA[{1}]]></FromUserName>
                                <CreateTime>{2}</CreateTime>
                                <MsgType><![CDATA[text]]></MsgType>
                                <Content><![CDATA[温心居说：{3}]]></Content>
                            </xml>";
            return string.Format(returnStr, weixinhost, myserver, createtime, content);

            
        }

        public ViewResult SayHello()
        {
            ViewBag.name = "Wen Cong Xuan";
            return View();
        }

        private string UploadMedia()
        {
        
            string url = "https://api.weixin.qq.com/cgi-bin/media/upload";
            string filename=Server.MapPath("~/Media/2015-04-26_105841.jpg");
            List<FormItem> list = new List<FormItem>() { 
                new FormItem {Name="access_token",Value=AccessTOKEN,ParamType=ParamType.Text},
                new FormItem {Name="type",Value="image",ParamType=ParamType.Text},
                new FormItem {Name="media",Value=filename,ParamType=ParamType.File}
            };
            string jsonresult = Funcs.PostFormData(list, url);
            return jsonresult;
            string resultStr = "resultStr";

            if (jsonresult.IndexOf("media_id")!=-1)
            {
                UploadMediaResult result = JsonConvert.DeserializeObject<UploadMediaResult>(jsonresult);
                resultStr= result.media_id;
            }
            else
            {
                ErrorResult result = JsonConvert.DeserializeObject<ErrorResult>(jsonresult);
                resultStr= result.errcode.ToString()+" | "+result.errmsg;
            }
            return resultStr;

            
        }

        public string test()
        {
            //return AccessTOKEN;
            return UploadMedia();
            
        }

        private bool CheckSignature(string signature, string timestamp, string nonce)
        {
            string[] temparr = { TOKEN, timestamp, nonce };
            //Array.Sort(temparr);
            string[] newstrarray=temparr.OrderBy(s => s).ToArray();
            string tempstring = string.Join("", newstrarray);
            string resultStr = FormsAuthentication.HashPasswordForStoringInConfigFile(tempstring, "SHA1");
            if (resultStr.ToLower() == signature)
            {
                return true;
            }
            return false;
        }

        class AccessTokenResult
        {
            public string access_token { get; set; }
            public int expires_int { get; set; }
            
        }
        class  UploadMediaResult
        {
            public string  type { get; set; }
            public string media_id { get; set; }
            public int created_at { get; set; }
            
        }

        class ErrorResult
        {
            public string errmsg { get; set; }
            public int errcode { get; set; }

        }
    }
}