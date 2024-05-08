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

            //var channelName = await db.StringGetAsync(cashClickChannelName);

            var messages = await db.HashGetAllAsync(cashChannelMessages);

            var messagesAll = new List<string>();

            foreach (var message in messages)
            {
                var messageValue = message.Value.ToString();
                messagesAll.Add(messageValue);
            }

            return Ok(messagesAll);
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
                        string id2 = item.Name.ToString();

                        var d = id.ToString().Replace("{", "").Replace("}", "");

                        if (id2 == d)
                        {
                            isIn = true;
                        }
                    }

                    if (!isIn)
                    {
                        HashEntry[] hashEntries = new HashEntry[]
                        {
                           new HashEntry($"{id}", $"{message}"),
                        };

                        db.HashSet(cashChannelMessages, hashEntries);

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

            //db.KeyDelete(cashChannelMessages);

            db.KeyDelete(cashChannelMessages);

            await db.KeyDeleteAsync(cashClickChannelName);

            await db.StringSetAsync(cashClickChannelName, channelName);
        }
    }
}
