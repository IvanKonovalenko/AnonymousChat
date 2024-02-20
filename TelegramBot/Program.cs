using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
var root=builder.Build();

var botClient = new TelegramBotClient(root["token"].ToString());

using var dbConnection = new DbTgBot(root["stringConnection"].ToString());
dbConnection.Open();
dbConnection.CreateDb();

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() 
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();



async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    if (message.Text == "/find")
    {
        long recipientId = await dbConnection.GetRecipientId(chatId.ToString());
        if (recipientId != 0)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы уже находитесь в чате\n/stop-закончить диалог",
            cancellationToken: cancellationToken);
            return;
        }
        string userFromQueue = await dbConnection.GetUserFromQueue();
        if (userFromQueue == chatId.ToString()) 
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы уже добавлены в очередь, ожидайте собеседника",
            cancellationToken: cancellationToken);
            return; 
        }
        if (userFromQueue == "0") 
        {
            await dbConnection.InsertIntoQueue(chatId.ToString());
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы добавлены в очередь, ожидайте собеседника",
            cancellationToken: cancellationToken);
        }
        else
        {
            await dbConnection.InsertIntoActiveChats(chatId.ToString(), userFromQueue);
            await dbConnection.DeleteFromQueue(userFromQueue);
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Собеседник найден",
            cancellationToken: cancellationToken);
            Message sentMessage1 = await botClient.SendTextMessageAsync(
            chatId: userFromQueue,
            text: "Собеседник найден",
            cancellationToken: cancellationToken);
        }

    }
    else if (message.Text == "/stop")
    {
        long recipientId=await dbConnection.GetRecipientId(chatId.ToString());
        if (recipientId == 0) 
        {
            await dbConnection.DeleteFromQueue(chatId.ToString());
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы удалены из очереди",
            cancellationToken: cancellationToken);
            return; 
        }
        else
        {
            await dbConnection.DeleteFromActiveChats(chatId.ToString());
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: recipientId,
            text: "Собеседник закончил чат",
            cancellationToken: cancellationToken);
            Message sentMessage1 = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы закончили чат",
            cancellationToken: cancellationToken);
        }
    }
    else
    {
        long recipientId = await dbConnection.GetRecipientId(chatId.ToString());
        if (recipientId == 0)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы не находитесь в чате\n/find-Найти собеседника",
            cancellationToken: cancellationToken);
            return;
        }
        else
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
             chatId: recipientId,
             text: messageText,
             cancellationToken: cancellationToken);
        }
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
