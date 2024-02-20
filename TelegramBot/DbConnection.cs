using Microsoft.Data.SqlClient;
using System.Data;
using Telegram.Bot.Types;

namespace TelegramBot
{
    class DbTgBot:IDisposable
    {
        private readonly SqlConnection connection;
        public DbTgBot(string connectionString)
        {
            connection = new SqlConnection(connectionString);
        }
        public void CreateDb()
        {
            string sql = "IF DB_ID('tgbot') IS NOT NULL DROP DATABASE tgbot; " +
                "CREATE DATABASE tgbot; " +
                "USE tgbot; " +
                "CREATE TABLE Queue([User] [nvarchar](max) NULL) " +
                "CREATE TABLE ActiveChats([User1] [nvarchar](max) NULL,[User2] [nvarchar](max) NULL)";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
        public async Task InsertIntoActiveChats(string chatId, string chatId2 )
        {
            string sql = $"INSERT INTO ActiveChats (User1, User2) values('{chatId}', '{chatId2}')";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
        public async Task DeleteFromActiveChats(string chatId) 
        {
            string sql = $"Delete from ActiveChats where user1 ='{chatId}' or user2 ='{chatId}'";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
        public async Task<long> GetRecipientId(string chatId)
        {
            string sql = $"select * from ActiveChats where User1='{chatId}' or User2='{chatId}'";
            SqlCommand myCommand = new SqlCommand(sql, connection);
            using (SqlDataReader myDataReader = myCommand.ExecuteReader())
            {
                if (!myDataReader.Read()) return 0;
                long recipientId = long.Parse(chatId);
                if (myDataReader["User1"].ToString() == chatId)
                {
                    recipientId = long.Parse(myDataReader["User2"].ToString());
                }
                else if (myDataReader["User2"].ToString() == chatId)
                {
                    recipientId = long.Parse(myDataReader["User1"].ToString());
                }
                return recipientId;
            }
        }
        public async Task InsertIntoQueue(string chatId)
        {
            string sql = $"INSERT INTO Queue([User]) values('{chatId}')";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
        public async Task DeleteFromQueue(string chatId)
        {
            string sql = $"Delete from Queue where [User]='{chatId}'";
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandType = CommandType.Text;
                command.ExecuteNonQuery();
            }
        }
        public async Task<string> GetUserFromQueue()
        {
            string sql = $"select * from Queue";
            using (SqlCommand myCommand = new SqlCommand(sql, connection))
            {
                using (SqlDataReader myDataReader = myCommand.ExecuteReader())
                {
                    if (myDataReader.Read()) return myDataReader["User"].ToString();
                    else return "0";
                }
            }
        }

        public void Open()
        {
            connection.Open();
        }
        public void Close()
        {
            Dispose();
        }
        public void Dispose()
        {
            connection.Close();
        }
    }
}
