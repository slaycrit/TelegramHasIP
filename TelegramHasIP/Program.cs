using System;
using System.Threading;
using System.Net;
using Telegram.Bot;

namespace TelegramHasIP
{
    class Program
    {
        //=========================================================================================
        //

        #region Fields


        /// <summary>
        /// Client Ref. for the Telegram Client
        /// </summary>
        static ITelegramBotClient botClient;

        /// <summary>
        /// External IP Address of the Network
        /// </summary>
        static string IP = "0.0.0.0";

        /// <summary>
        /// Timer for periodically Updating the IP
        /// </summary>
        static Timer IpTimer;

        /// <summary>
        /// Used to check if the IP has already been requested on the current day
        /// </summary>
        static int checkDay = 0;


        #endregion

        //=========================================================================================
        //

        #region Methods


        /// <summary>
        /// Main Execution
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //Initialize Bot, Secret.Token is from an external File that is not included with the Repository
            //An Example Class is provided in the SecretExample.txt
            botClient = new TelegramBotClient(Secret.Token);

            //Get Initial Information
            var me = botClient.GetMeAsync().Result;
            Console.WriteLine($"Bot with Id \"{me.Id}\" and Name \"{me.FirstName}\" started");

            //Start the Event Listener
            botClient.OnMessage += BotClient_OnMessage;
            botClient.StartReceiving();

            //Start the Timer for Checking the IP
            IpTimer = new Timer(TimerCallback, null, 0, 1000);

            //Prompt User
            Console.WriteLine("Running... Press 'c' to stop");
            while (Console.ReadKey().KeyChar != 'c') ;

            //Clear up Tasks
            IpTimer.Dispose();
            botClient.StopReceiving();
        }

        /// <summary>
        /// Event Handler for Processing incomming Messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static async void BotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            //Only Continue if the Text is Valid
            if (e.Message.Text != null)
            {
                Console.WriteLine($"{DateTime.Now:g} | Request Received Name: \"{e.Message.Chat.FirstName} {e.Message.Chat.LastName}\" Request: {e.Message.Text}");

                //Decide what to do
                switch(e.Message.Text.ToLower())
                {
                    case "/start": await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: "Use \"/giben\" to get the current IP"); break;
                    case "/giben": await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: IP); break;

                    default: await botClient.SendTextMessageAsync(chatId: e.Message.Chat, text: $"Unrecognized Command: \"{e.Message.Text}\""); break;
                }
            }
        }

        /// <summary>
        /// Called by the Timer every Second to check if the IP Address has to be retrieved
        /// </summary>
        /// <param name="o"></param>
        static void TimerCallback(Object o)
        {
            //Check if the IP is at the Default Value or it is 6 in the morning
            if(DateTime.Now.Day != checkDay && (IP == "0.0.0.0" || DateTime.Now.Hour == 6))
            {
                //Try to get the External IP from the Website
                if (TryGetIP(out string newIP))
                {
                    Console.WriteLine($"{DateTime.Now:g} | Get IP: {newIP}");
                    //Avoid 
                    if (DateTime.Now.Hour >= 6) checkDay = DateTime.Now.Day;
                    else checkDay = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)).Day;
                    IP = newIP;
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now:g} | Get IP: Failed");
                }
            }
        }

        /// <summary>
        /// Tried to get the IP address from a remote Server
        /// </summary>
        /// <param name="ip">The IP of the Network, 0.0.0.0 if the operation failed</param>
        /// <returns>True when the IP was retrieved successfully</returns>
        static bool TryGetIP(out string ip)
        {
            try
            {
                //Request the ExternalIP from the Website
                string externalip = new WebClient().DownloadString("https://api.ipify.org/");

                //Assign IP
                ip = externalip;

                return true;
            }
            catch(Exception e)
            {
                //Caused by Network Problems or other stuff that might go wrong
                Console.WriteLine($"{ DateTime.Now:g} | WARNING: {e.Message}");
                ip = "0.0.0.0";
            }
            return false;
        }


        #endregion Methods

        //=========================================================================================
        //
    }
}
