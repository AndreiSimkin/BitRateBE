using BitRateBE.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace BitRateBE.Services
{
    public static class Bitmex
    {
        #region Нытьё и оправдания
        // Я дико извеняюсь за статический класс, но конструировать что-то лучше у меня физически не хватит времени
        #endregion

        static Rate CurrentRate { get; set; }
        public static bool IsRuning { get; private set; }
        static WebSocket Socket { get; set; }
        public delegate void RateRecivedEventHandler(Rate rate);
        public static event RateRecivedEventHandler RateRecived;

        class BitmexResponce
        {
            public class Quote
            {
                public float bidPrice { get; set; }
            }

            public Quote[] data { get; set; }
        }

        public static void Start()
        {
            Socket = new WebSocket("wss://www.bitmex.com/realtime?subscribe=quote:XBTUSD");
            Socket.Open();
            Socket.Opened += Socket_Opened;
            Socket.Error += Socket_Error;

        }

        private static void Socket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Socket.Error -= Socket_Error;
            Socket.Opened -= Socket_Opened;
            Socket.Closed -= Socket_Closed;
        }

        private static void Socket_Opened(object sender, EventArgs e)
        {
            IsRuning = true;
            Socket.MessageReceived += Socket_MessageReceived;
            Socket.Opened -= Socket_Opened;
            Socket.Closed += Socket_Closed;
        }

        private static void Socket_Closed(object sender, EventArgs e)
        {
            Socket.Error -= Socket_Error;
            Socket.Opened -= Socket_Opened;
            Socket.Closed -= Socket_Closed;
        }

        private static void Socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.Contains("table"))
                CurrentRate = new Rate() { Current = JsonConvert.DeserializeObject<BitmexResponce>(e.Message).data.Last().bidPrice };
            RateRecived?.Invoke(CurrentRate);
        }

        public static void Stop()
        {
            IsRuning = false;
            Socket.Close();
            CurrentRate = null;
        }

        public static Rate GetRate()
        {
            while (IsRuning && CurrentRate == null)
                Thread.Sleep(10);
            if (IsRuning)
                return new Rate() { Current = CurrentRate.Current };
            else
                return new Rate();
        }
    }
}
