﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFWCoreLib.CoreFrame.Common;
using EFWCoreLib.WcfFrame.DataSerialize;
using EFWCoreLib.WcfFrame.WcfHandler;

namespace EFWCoreLib.WcfFrame.ServerManage
{
    /// <summary>
    /// 发布订阅服务管理
    /// </summary>
    public class PublishServiceManage
    {
        private static Object syncObj = new Object();//定义一个静态对象用于线程部份代码块的锁定，用于lock操作
        public static List<Subscriber> subscriberList;//订阅者列表
        public static Dictionary<string, PublishServiceObject> serviceDic;
        /// <summary>
        /// 初始化订阅服务
        /// </summary>
        public static void InitPublishService()
        {
            subscriberList = new List<Subscriber>();//订阅者列表
            serviceDic = new Dictionary<string, PublishServiceObject>();

            PublishServiceObject serviceObj = new PublishServiceObject();
            serviceObj.publishServiceName = "DistributedCache";//分布式缓存服务
            serviceDic.Add("DistributedCache", serviceObj);

            serviceObj = new PublishServiceObject();
            serviceObj.publishServiceName = "RemotePlugin";//远程插件服务
            serviceDic.Add("RemotePlugin", serviceObj);
        }

        public static void Subscribe(string ServerIdentify,string clientId, string publishServiceName, IDataReply callback)
        {
            if (ServerIdentify == WcfGlobal.Identify) return;//不能订阅自己

            if (subscriberList.FindIndex(x => x.clientId == clientId && x.publishServiceName == publishServiceName) == -1)
            {
                Subscriber sub;
                lock (syncObj)
                {
                    sub = new Subscriber();
                    sub.clientId = clientId;
                    sub.publishServiceName = publishServiceName;
                    sub.callback = callback;

                    subscriberList.Add(sub);
                }
                ShowHostMsg(Color.Blue, DateTime.Now, "客户端[" + clientId + "]订阅“" + publishServiceName + "”服务成功！");
                SendNotify(sub);
            }
        }

        public static void UnSubscribe(string clientId, string publishServiceName)
        {
            if (subscriberList.FindIndex(x => x.clientId == clientId && x.publishServiceName == publishServiceName) == -1)
            {
                lock (syncObj)
                {
                    Subscriber sub = subscriberList.Find(x => x.clientId == clientId && x.publishServiceName == publishServiceName);
                    subscriberList.Remove(sub);
                }
                ShowHostMsg(Color.Black, DateTime.Now, "客户端[" + clientId + "]取消订阅“" + publishServiceName + "”服务成功！");
            }
        }
        /// <summary>
        /// 服务端发送通知
        /// </summary>
        public static void SendNotify(string publishServiceName)
        {
            List<Subscriber> list = subscriberList.FindAll(x => x.publishServiceName == publishServiceName);
            foreach(var item in list)
            {
                item.callback.Notify(publishServiceName);
            }
            ShowHostMsg(Color.Blue, DateTime.Now, "向已订阅的客户端发送了“" + publishServiceName + "”服务通知！");
        }
        /// <summary>
        /// 给指定订阅者发布通知
        /// </summary>
        /// <param name="sub"></param>
        public static void SendNotify(Subscriber sub)
        {
            if (sub != null)
            {
                sub.callback.Notify(sub.publishServiceName);
                ShowHostMsg(Color.Blue, DateTime.Now, "向已订阅的客户端发送了“" + sub.publishServiceName + "”服务通知！");
            }
        }

        /// <summary>
        /// 客户端接收通知
        /// </summary>
        /// <param name="publishServiceName">订阅服务名称</param>
        /// <param name="_clientLink">客户端连接</param>
        public static void ReceiveNotify(string publishServiceName, ClientLink _clientLink)
        {
            serviceDic[publishServiceName].ProcessPublishService(_clientLink);
            ShowHostMsg(Color.Blue, DateTime.Now, "收到订阅的“" + publishServiceName + "”服务通知！");
        }

        private static void ShowHostMsg(Color clr, DateTime time, string text)
        {
            MiddlewareLogHelper.WriterLog(LogType.MidLog, true, clr, text);
        }
    }
    /// <summary>
    /// 订阅者
    /// </summary>
    public class Subscriber
    {
        /// <summary>
        /// 中间件标识
        /// </summary>
        public string ServerIdentify { get; set; }
        /// <summary>
        /// 客户端标识
        /// </summary>
        public string clientId { get; set; }
        /// <summary>
        /// 发布服务名称
        /// </summary>
        public string publishServiceName { get; set; }
        /// <summary>
        /// 回调通知客户端
        /// </summary>
        public IDataReply callback { get; set; }
    }
    /// <summary>
    /// 发布服务对象
    /// </summary>
    public class PublishServiceObject
    {

        public string publishServiceName { get; set; }

        public void ProcessPublishService(ClientLink _clientLink)
        {
            switch (publishServiceName)
            {
                case "DistributedCache"://分布式缓存服务
                    List<CacheIdentify> ciList = DistributedCacheManage.GetCacheIdentifyList();
                    if (ciList.Count > 0)
                    {
                        List<CacheObject> coList = _clientLink.GetDistributedCacheData(ciList);
                        if (coList.Count > 0)
                        {
                            DistributedCacheManage.SetCacheObjectList(coList);
                        }
                    }
                    break;
                case "RemotePlugin"://远程插件服务
                    LocalPlugin localPlugin = RemotePluginManage.GetLocalPlugin();
                    if (localPlugin.PluginDic.Count > 0)
                        _clientLink.RegisterRemotePlugin(WcfGlobal.Identify, localPlugin.PluginDic.Keys.ToArray());
                    break;
            }
        }
    }
}
