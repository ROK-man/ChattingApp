using Npgsql;

namespace Chatting_Server
{
    internal class DBController
    {
        private readonly string m_connectionString;

        public DBController(string connectionString)
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

        public void UpdateLastLogin(long userNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();

                string query = @"UPDATE users
                         SET last_login = @lastLogin, status = 'OFFLINE'
                         WHERE user_no = @senderNo";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("lastLogin", DateTime.UtcNow); // UTC 권장
                    cmd.Parameters.AddWithValue("senderNo", userNo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public FriendInfo GetFriendInfo(long userNo, long friendNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();
            string query = @"
                SELECT user_no, friend_no, status, created_at
                FROM friends 
                WHERE (user_no = @senderNo AND friend_no = @targetNo);
            ";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);
            cmd.Parameters.AddWithValue("@targetNo", friendNo);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new FriendInfo
                {
                    UserNo = reader.GetInt64(reader.GetOrdinal("user_no")),
                    FriendNo = reader.GetInt64(reader.GetOrdinal("friend_no")),
                    Status = Enum.Parse<FriendStatus>(
                        reader.GetString(reader.GetOrdinal("status")),
                        ignoreCase: true
                    ),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                };
            }
            return null;
        }



        public void RequestFriend(long userNo, long friendNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                INSERT INTO friends (user_no, friend_no, status, created_at)
                VALUES (@senderNo, @targetNo, 'PENDING', NOW())
                ON CONFLICT (user_no, friend_no) DO NOTHING;
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);
            cmd.Parameters.AddWithValue("@targetNo", friendNo);
            cmd.ExecuteNonQuery();
        }

        public void AcceptFriend(long senderNo, long targetNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                    INSERT INTO friends (user_no, friend_no, status, created_at)
                    VALUES (@senderNo, @targetNo, 'ACCEPTED', NOW())
                    ON CONFLICT (user_no, friend_no) 
                    DO UPDATE 
                    SET status = 'ACCEPTED',
                        created_at = NOW();
                ";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@senderNo", senderNo);
                cmd.Parameters.AddWithValue("@targetNo", targetNo);
                cmd.ExecuteNonQuery();
            }
        }

        public void BlockFriend(long userNo, long targetNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                    INSERT INTO friends (user_no, friend_no, status, created_at)
                    VALUES (@senderNo, @targetNo, 'BLOCKED', NOW())
                    ON CONFLICT (user_no, friend_no) 
                    DO UPDATE 
                    SET status = 'BLOCKED',
                        created_at = NOW();
                ";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@senderNo", userNo);
                cmd.Parameters.AddWithValue("@targetNo", targetNo);
                cmd.ExecuteNonQuery();
            }
        }

        public void RejectFriend(long userNo, long friendNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                DELETE FROM friends
                WHERE user_no = @targetNo AND friend_no = @senderNo AND status = 'PENDING';
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);
            cmd.Parameters.AddWithValue("@targetNo", friendNo);
            cmd.ExecuteNonQuery();
        }
        public void CancelFriendRequest(long userNo, long friendNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                DELETE FROM friends
                WHERE user_no = @senderNo AND friend_no = @targetNo AND status = 'PENDING';
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);
            cmd.Parameters.AddWithValue("@targetNo", friendNo);
            cmd.ExecuteNonQuery();
        }

        public void RemoveFriendInfo(long userNo, long friendNo)
        {
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                DELETE FROM friends
                WHERE (user_no = @senderNo AND friend_no = @targetNo);
            ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);
            cmd.Parameters.AddWithValue("@targetNo", friendNo);
            cmd.ExecuteNonQuery();
        }

        public List<UserInfo> GetFriendList(long userNo)
        {
            var friends = new List<UserInfo>();

            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();

            string query = @"
                    SELECT u.nickname, u.status, u.last_login
                    FROM friends f
                    JOIN users u ON f.friend_no = u.user_no
                    WHERE f.user_no = @senderNo AND f.status = 'ACCEPTED';
                ";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@senderNo", userNo);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var user = new UserInfo
                {
                    Nickname = reader.GetString(reader.GetOrdinal("nickname")),
                    Status = reader.GetString(reader.GetOrdinal("status")) == "ONLINE",
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login"))
                                  ? null
                                  : reader.GetDateTime(reader.GetOrdinal("last_login")),
                };

                friends.Add(user);
            }

            return friends;
        }

        public List<UserInfo> GetFriendRequests(long userNo)
        {
            var pendingRequests = new List<UserInfo>();
            using var conn = new NpgsqlConnection(m_connectionString);
            conn.Open();
            string query = @"
                    SELECT u.nickname,  f.created_at
                    FROM friends f
                    JOIN users u ON f.user_no = u.user_no
                    WHERE f.friend_no = @targetNo AND f.status = 'PENDING';
                ";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@targetNo", userNo);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var user = new UserInfo
                {
                    Nickname = reader.GetString(reader.GetOrdinal("nickname")),
                    LastLogin = reader.IsDBNull(reader.GetOrdinal("created_at"))
                                  ? null
                                  : reader.GetDateTime(reader.GetOrdinal("created_at")),
                };
                pendingRequests.Add(user);
            }
            return pendingRequests;
        }
    }
}
