using DevopPubSubTask2.Models;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Diagnostics;
using System.Threading.Channels;

namespace DevopPubSubTask2.Controllers
{
    public class HomeController : Controller
    {
        static readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("redis-12939.c251.east-us-mz.azure.redns.redis-cloud.com:12939,password=gQEQnmuLQyxtwFApDk2TmWiYPyd6830N");
        public static string cashListName = "channelNames";
        public static string cashClickChannelName = "clickChannel";
        public static string cashChannelMessages = "channelMessages";
        public async Task<IActionResult> Index()
        {
            var db = redis.GetDatabase();

            db.KeyDelete(cashChannelMessages);

            await db.KeyDeleteAsync(cashClickChannelName);

            return View();
        }

        public async Task<IActionResult> AddChannel(string channelName)
        {
            var subscriber = redis.GetSubscriber();
            var db = redis.GetDatabase();

            if (subscriber != null)
            {
                await subscriber.PublishAsync(channelName, "");
                await db.ListLeftPushAsync(cashListName, channelName);
            }

            return Ok();
        }

        public async Task<IActionResult> GetAllChannels()
        {
            var db = redis.GetDatabase();

            var channels = await db.ListRangeAsync(cashListName);

            var channelsList = channels.Select(c => c.ToString()).ToList();

            return Ok(channelsList);
        }

        public async Task<IActionResult> GetAllMessages()
        {
            var db = redis.GetDatabase();


            var channelName = await db.StringGetAsync(cashClickChannelName);

            var messages = await db.HashGetAllAsync(cashChannelMessages);

            //List<string> messageList = messages.Select(m => m.ToString()).ToList();


            //var messages = await db.HashGetAllAsync(cashChannelMessages);


            //HashEntry[] messages = await db.HashGetAllAsync("cashChannelMessages");

            // Her bir girdiyi kontrol ederek "message" alanını alın
            List<string> messageList = new List<string>();
            List<string> ids = new List<string>();

            //var d = messages.Select(c => c.Name == "id").ToList();

            foreach (var entry in messages)
            {
                string id = entry.Name;

                if (id == "id")
                {
                    ids.Add(id);
                }
            }

            bool ayniDegerlerVarMi = false;
            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = i + 1; j < ids.Count; j++)
                {
                    if (ids[i] == ids[j])
                    {
                        ayniDegerlerVarMi = true;
                        break;
                    }
                }
                if (ayniDegerlerVarMi)
                {
                    break;
                }
            }

            if (!ayniDegerlerVarMi)
            {

            }

            return Ok(messageList);
        }

        public async Task ClickChannelMessagesAddedCash()
        {
            var db = redis.GetDatabase();

            var channelName = await db.StringGetAsync(cashClickChannelName);
            var c = channelName.ToString();
            c = c.Replace("{", "").Replace("}", "");
            var subscriber = redis.GetSubscriber();
            if (subscriber != null)
            {
                await subscriber.SubscribeAsync(c, async (channel, message) =>
                {
                    Guid id = Guid.NewGuid();
                    bool isIn = false;

                    HashEntry[] messages = db.HashGetAll(cashChannelMessages);

                    foreach (var item in messages)
                    {
                        // Her bir alanın ID'sini ve mesajını alın
                        string id2 = item.Value.ToString(); // ID

                        // Eğer alanın ID'si aranan ID'ye eşitse, o alanın mesajını yazdırın
                        if (id2 == id.ToString())
                        {
                            isIn = true;
                        }
                    }

                    if (!isIn)
                    {
                        //HashEntry[] hashEntries = new HashEntry[]
                        //{
                        //   new HashEntry($"id", $"{id}"),
                        //   new HashEntry($"message", $"{message}")
                        //};

                        db.HashSet(cashChannelMessages, "message:" + id, message);

                        //await db.HashSetAsync(cashChannelMessages, hashEntries);
                    }

                    //await db.ListLeftPushAsync(cashChannelMessages, message);
                });
            }
        }

        public async Task SendMessage(string message)
        {
            var subscriber = redis.GetSubscriber();
            var db = redis.GetDatabase();

            if (subscriber != null)
            {
                var channelName = await db.StringGetAsync(cashClickChannelName);
                var c = channelName.ToString();
                c = c.Replace("{", "").Replace("}", "");
                subscriber.Publish(c, message);
            }
        }

        public async Task ClickChannel(string channelName)
        {
            var db = redis.GetDatabase();

            db.KeyDelete(cashChannelMessages);

            await db.StringSetAsync(cashClickChannelName, channelName);
        }
    }
}
