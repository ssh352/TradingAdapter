﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using log4net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Timers;
using System.Diagnostics;
using UtilityClass;
using System.Configuration;
using DataManager;
using SymbolClass;
using TradeManager;
using AccountClass;
using MarginClass;
using PositionClass;
using OrderClass;
using TCMonitor;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis;
using System.Threading;

namespace tradebox
{
    public partial class tradebox : Form
    {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string channelname = "tradeboxReInit";
        private string[] channelnames = { "tradeboxReInit" };
        private RedisClient redisClient;

        public tradebox()
        {
            InitializeComponent();
            redisClient = RedisUtil.getRedisClientInstance();
            ThreadPool.QueueUserWorkItem(new WaitCallback(doSub)); 
            init();            
        }

        private void doSub(object parameter)
        {
            IRedisSubscription subClient = redisClient.CreateSubscription();
            subClient.OnSubscribe += new Action<string>(onSub);
            subClient.OnMessage += new Action<string, string>(onOrderMsg);
            subClient.SubscribeToChannels(channelnames);
        }

        private void onSub(string channel)
        {
            logger.Info("onSubMsg : channel = #" + channel + "#");
        }

        private void onOrderMsg(string channel, string value)
        {
            if (String.Compare(channel, channelnames[0]) != 0)
            {
                return;
            }
            logger.Info("onOrderMsg : "+channelname+" message recieved = " + value);
            init();
        }

        public void init()
        {
            TouchanceMonitor.Instance.init();
            TouchanceMonitor.Instance.ifTouchanceNotExistCloseApplication();

            TradeboxXmlReader.Instance.init();
            SymbolManager.Instance.init();
            AccountManager.Instance.init();
            QuoteAdapter.Instant.init();
            DataCenter.Instance.init();
            TradeCenter.Instance.init();
            MarginMonitor.Instance.init();
            PositionMonitor.Instance.init();
            OrderManager.Instance.init();

            //RandomEntryController.Instance.init();

            logger.Info("Working Directory : " + Misc.getWorkingDirectory());

            if (TradeboxXmlReader.Instance.isAutoShutdown)
            {
                setAutoShutdownTimer();
            }
        }

        private void setAutoShutdownTimer()
        {
            System.Timers.Timer myTimer = new System.Timers.Timer();
            myTimer.Elapsed += new ElapsedEventHandler(HalfHourTask);
            myTimer.Interval = 30 * 60000;
            myTimer.Start();
        }

        private void tradebox_Load(object sender, EventArgs e)
        {
           
        }

        private void HalfHourTask( object source, ElapsedEventArgs e )
        {
            int lhhmmss = DateTimeFunc.getHHMMSS();
            if (lhhmmss > TradeboxXmlReader.Instance.autoShutdownTime)
            {
                Environment.Exit(0);                
            }   
         
            if((lhhmmss>80000)&(lhhmmss<90000))
            {
                OrderManager.Instance.ReInit();
            }
        }


        private void tradebox_FormClosed(object sender, FormClosedEventArgs e)
        {
            
        }


    
    }
}
