using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Planets_Bot
{
    class Program
    {
        private static TelegramBotClient? Bot;

        public static async Task Main()
        {
            Bot = new TelegramBotClient("5085989743:AAEKlblmFkUc4piQvAhGvEliOTlTupgKupY");

            User me = await Bot.GetMeAsync();

            using var cts = new CancellationTokenSource();

            ReceiverOptions receiverOptions = new() { AllowedUpdates = { } };
            Bot.StartReceiving(HandleUpdateAsync,
                               HandleErrorAsync,
                               receiverOptions,
                               cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            cts.Cancel();
        }



        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            Console.WriteLine($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;


            var action = message.Text switch
            {
                "/keyboard" => SendReplyKeyboard(botClient, message),
                "/help" or "/start" => help(botClient, message),
                _ => getPlanetInfo(botClient, message)
            };
            Message sentMessage = await action;
            Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");


            static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message)
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new(
                    new[]
                    {
                        new KeyboardButton[] { "Mercury", "Venus", "Earth", "Mars" },
                        new KeyboardButton[] { "Jupiter", "Saturn", "Uranus", "Neptune" },
                        new KeyboardButton[] { "Sun" },
                    })
                {
                    ResizeKeyboard = true
                };

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: "Choose",
                                                            replyMarkup: replyKeyboardMarkup);
            }


            static async Task<Message> getPlanetInfo(ITelegramBotClient botClient, Message message)
            {

                string msg = message.Text;
                string name = "";
                string image = "";
                string velocity = "";
                string distance = "";
                string description = "";


                StreamReader reader = new StreamReader("h://Planets.json");
                string readedJson = reader.ReadToEnd();
                var json = JsonConvert.DeserializeObject<Root>(readedJson);
                List<string> story = new List<string>();

                foreach ( var i in json.planets)
                {
                    if(msg.ToUpper() == i.name.ToUpper())
                    {
                        name = i.name;
                        image = i.image;
                        velocity = i.velocity;
                        distance = i.distance;
                        description = i.description;

                        return await botClient.SendPhotoAsync(
                                            chatId: message.Chat.Id,
                                            photo: image,
                                            caption: "<b>Planet Name: </b>"+ name + "\n" +
                                                     "<b>Planet velocity: </b>" + velocity + "\n" +
                                                     "<b>Planet distance: </b>" + distance + "\n" +
                                                     "<b>Descriptions: </b>" + description + "\n",
                                            parseMode: ParseMode.Html
                                            );
                    }
                }

                return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Make sure you type the name of the planet correctly or choose the name from the keyboard." +
                "/help - get help" +
                "\n" +
                "/keyboard - use kayboard"
                        );


            }



            static async Task<Message> help(ITelegramBotClient botClient, Message message)
            {

                return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Type the name of the planet you want to get information about or choose its name from the keyboard\n" +
                "/help - get help" +
                "\n"+
                "/keyboard - use kayboard"
                        );

   
            }


        }


        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }


   

    }


    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Planet
    {
        public string position { get; set; }
        public string name { get; set; }
        public string image { get; set; }
        public string velocity { get; set; }
        public string distance { get; set; }
        public string description { get; set; }
    }

    public class Root
    {
        public List<Planet> planets { get; set; }
    }

}
