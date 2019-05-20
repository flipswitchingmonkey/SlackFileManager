using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackAPI;

namespace SlackFileManager
{
    public class ResponseChannel
    {
        // from Conversation
        public string id { get; set; }
        public DateTime created { get; set; }
        public DateTime last_read { get; set; }
        public bool is_open { get; set; }
        public bool is_starred { get; set; }
        public int unread_count { get; set; }
        public Message latest { get; set; }

        // from Channel
        public string name { get; set; }
        public string creator { get; set; }

        public bool is_archived { get; set; }
        public bool is_member { get; set; }
        public bool is_general { get; set; }
        public bool is_channel { get; set; }
        public bool is_group { get; set; }
        //Is this deprecated by is_open?
        public bool IsPrivateGroup { get { return id != null && id[0] == 'G'; } }

        public int num_members { get; set; }
        public OwnedStampedMessage topic { get; set; }
        public OwnedStampedMessage purpose { get; set; }

        public string[] members { get; set; }

        public ResponseChannel(string name="blank")
        {
            this.name = name;
        }

        public static ResponseChannel GetAllChannel()
        {
            var u = new ResponseChannel();
            u.name = "All Channels";
            u.id = null;
            return u;
        }

        public static ResponseChannel FromChannel(Channel channel)
        {
            var rs = new ResponseChannel();
            rs.id = channel.id;
            rs.created = channel.created;
            rs.last_read = channel.last_read;
            rs.is_open = channel.is_open;
            rs.is_starred = channel.is_starred;
            rs.unread_count = channel.unread_count;
            rs.latest = channel.latest;
            rs.name = channel.name;
            rs.creator = channel.creator;
            rs.is_archived = channel.is_archived;
            rs.is_member = channel.is_member;
            rs.is_general = channel.is_general;
            rs.is_channel = channel.is_channel;
            rs.is_group = channel.is_group;
            rs.num_members = channel.num_members;
            rs.topic = channel.topic;
            rs.purpose = channel.purpose;
            rs.members = channel.members;
            return rs;
        }
    }
}
