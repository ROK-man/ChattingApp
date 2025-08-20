using MessageLib;
using MongoDB.Driver;
using Npgsql;

namespace Chatting_Server
{
    internal class DBController
    {
        private readonly string m_connectionString;
        private readonly string m_mongoDB;
        private readonly IMongoDatabase _db;


        public DBController(string connectionString, string mongoDB)
        {
            m_connectionString = connectionString;
            m_mongoDB = mongoDB;
            _db = new MongoClient(mongoDB).GetDatabase("chattingserver");
        }

        public void InsertChattingMessage(ChattingMessage? message)
        {
            if(message == null || message.SenderNo == 0 || message.TargetNo == 0)
            {
                Console.WriteLine("SenderNo or TargetNo is not set. Cannot insert message.");
                return;
            }
            message.TimeStamp = DateTime.UtcNow;
            var collection = _db.GetCollection<ChattingMessage>("friend_messages");
            collection.InsertOne(message);
        }

        // User @@@@@@@@
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


        // Friend @@@@@@@@
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




        /// GroupType @@@@@@@@
        public GroupInfo? GetGroupInfo(string groupName)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();

                string query = @"SELECT group_id, name, password, created_at
                                FROM groups
                                WHERE name ILIKE @groupName
                                ;";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupName", groupName);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return new GroupInfo
                        {
                            GroupNo = reader.GetInt64(reader.GetOrdinal("group_id")),
                            GroupName = reader.GetString(reader.GetOrdinal("name")),
                            Password = reader.IsDBNull(reader.GetOrdinal("password")) ? string.Empty : reader.GetString(reader.GetOrdinal("password")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                        };
                    }
                }
            }
            return null;
        }

        public void InsertGroup(string groupName)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO groups (name)
                            VALUES (@groupName)
                            ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupName", groupName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int InsertGroupWithPassword(string groupName, string password)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO groups (name, password)
                            VALUES (@groupName, @password)
                            ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupName", groupName);
                    cmd.Parameters.AddWithValue("password", password);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public bool JoinGroup(long userNo, long groupNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO group_members (user_no, group_id)
                         VALUES (@userNo, @groupNo)
                         ON CONFLICT DO NOTHING;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("userNo", userNo);
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    int affected = cmd.ExecuteNonQuery();
                    return affected > 0; // 1이면 새로 삽입, 0이면 이미 존재
                }
            }
        }

        public bool JoinGroupWithPassword(long userNo, long groupNo, string password)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"INSERT INTO group_members (user_no, group_id)
                         SELECT @userNo, @groupNo
                         FROM groups
                         WHERE group_id = @groupNo AND password = @password
                         ON CONFLICT DO NOTHING;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("userNo", userNo);
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    cmd.Parameters.AddWithValue("password", password);
                    int affected = cmd.ExecuteNonQuery();
                    return affected > 0; // 1이면 성공, 0이면 실패(중복 또는 비밀번호 불일치)
                }
            }
        }


        public bool QuitGroup(long userNo, long groupNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"DELETE FROM group_members
                         WHERE user_no = @userNo AND group_id = @groupNo;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("userNo", userNo);
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    int affected = cmd.ExecuteNonQuery();
                    return affected > 0; // 삭제된 행이 있으면 true, 없으면 false
                }
            }
        }

        public void RemoveGroup(long groupNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"DELETE FROM groups
                         WHERE group_id = @groupNo
                         ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int GetGroupMemberCount(long groupNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"SELECT COUNT(*)
                             FROM group_members
                             WHERE group_id = @groupNo
                             ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public List<GroupInfo> GetGroupList(long userNo)
        {
            var groups = new List<GroupInfo>();
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT g.group_id, g.name, g.password, g.created_at
                    FROM group_members gm
                    JOIN groups g ON gm.group_id = g.group_id
                    WHERE gm.user_no = @userNo
                    ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("userNo", userNo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            groups.Add(new GroupInfo
                            {
                                GroupName = reader.GetString(reader.GetOrdinal("name")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
                            });
                        }
                    }
                }
            }
            return groups;
        }

        public bool IsUserInGroup(long userNo, long groupNo)
        {
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT COUNT(*)
                    FROM group_members
                    WHERE user_no = @userNo AND group_id = @groupNo
                    ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("userNo", userNo);
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public List<UserInfo> GetGroupMembers(long groupNo)
        {
            var members = new List<UserInfo>();
            using (var conn = new NpgsqlConnection(m_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT u.user_no, u.nickname, u.status, u.last_login
                    FROM group_members gm
                    JOIN users u ON gm.user_no = u.user_no
                    WHERE gm.group_id = @groupNo
                    ;";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("groupNo", groupNo);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            members.Add(new UserInfo
                            {
                                Nickname = reader.GetString(reader.GetOrdinal("nickname"))
                            });
                        }
                    }
                }
            }
            return members;
        }
    }
}
