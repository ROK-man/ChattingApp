using Npgsql;

namespace Chatting_Server
{
    internal class UserInfoDB
    {
        private readonly string m_connectionString;

        public UserInfoDB(string connectionString)
        {
            m_connectionString = connectionString;
        }

        public UserInfo? GetUserInfo(string nickname)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();

                string query = @"SELECT user_no, id, nickname, status, last_login, banned, created_at
                                FROM users
                                WHERE nickname ILIKE @nickname";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("nickname", nickname);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserInfo
                            {
                                UserNo = reader.GetInt64(reader.GetOrdinal("user_no")), // bigint → Int64
                                UserID = reader.GetString(reader.GetOrdinal("id")),
                                Nickname = reader.GetString(reader.GetOrdinal("nickname")),
                                Status = reader.GetString(reader.GetOrdinal("status")) == "ONLINE",
                                LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? null : reader.GetDateTime(reader.GetOrdinal("last_login")),
                                Banned = reader.GetBoolean(reader.GetOrdinal("banned")),
                                RegisterDate = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            };
                        }
                    }
                }
            }
            return null;
        }


        public void InsertUserInfo(string id, string nickname)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO users (id, nickname)
                             VALUES (@id, @nickname)";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("nickname", nickname);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
