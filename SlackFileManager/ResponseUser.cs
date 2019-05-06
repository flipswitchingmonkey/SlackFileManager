using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackAPI;

namespace SlackFileManager
{
    public class ResponseUser
    {
        public string id { get; set; }
        public bool IsSlackBot
        {
            get
            {
                return id.Equals("USLACKBOT", StringComparison.CurrentCultureIgnoreCase);
            }
        }
        public string name { get; set; }
        public bool deleted { get; set; }
        public string color { get; set; }
        public bool is_admin { get; set; }
        public bool is_owner { get; set; }
        public bool is_primary_owner { get; set; }
        public bool is_restricted { get; set; }
        public bool is_ultra_restricted { get; set; }
        public bool has_2fa { get; set; }
        public string two_factor_type { get; set; }
        public bool has_files { get; set; }
        public string presence { get; set; }
        public bool is_bot { get; set; }
        public string tz { get; set; }
        public string tz_label { get; set; }
        public int tz_offset { get; set; }
        public string team_id { get; set; }
        public string real_name { get; set; }

        public static ResponseUser GetAllUser()
        {
            var u = new ResponseUser();
            u.name = "All Users";
            u.id = null;
            return u;
        }

        public static ResponseUser FromUser(SlackAPI.User user)
        {
            var u = new ResponseUser();
            u.id = user.id;
            u.name = user.name;
            u.deleted = user.deleted;
            u.color = user.color;
            u.is_admin = user.is_admin;
            u.is_owner = user.is_owner;
            u.is_owner = user.is_owner;
            u.is_primary_owner = user.is_primary_owner;
            u.is_restricted = user.is_restricted;
            u.is_ultra_restricted = user.is_ultra_restricted;
            u.has_2fa = user.has_2fa;
            u.two_factor_type = user.two_factor_type;
            u.has_files = user.has_files;
            u.presence = user.presence;
            u.is_bot = user.is_bot;
            u.tz = user.tz;
            u.tz_label = user.tz_label;
            u.tz_offset = user.tz_offset;
            u.team_id = user.team_id;
            u.real_name = user.real_name;
            return u;
        }
    }
}
