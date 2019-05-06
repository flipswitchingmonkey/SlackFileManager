using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlackAPI;

namespace SlackFileManager
{
    public class ResponseFile
    {
        public string id { get; set; }
        public string thumb_480 { get; set; }
        public int thumb_480_w { get; set; }
        public int thumb_480_h { get; set; }
        public string permalink { get; set; }
        public string permalink_public { get; set; }
        public string edit_link { get; set; }
        public string preview { get; set; }
        public string preview_highlight { get; set; }
        public int lines { get; set; }
        public int lines_more { get; set; }
        public bool is_public { get; set; }
        public bool public_url_shared { get; set; }
        public bool display_as_bot { get; set; }
        public string[] channels { get; set; }
        public string[] groups { get; set; }
        public string[] ims { get; set; }
        public FileComment initial_comment { get; set; }
        public int comments_count { get; set; }
        public int num_stars { get; set; }
        public bool is_starred { get; set; }
        public string[] pinned_to { get; set; }
        public int thumb_360_h { get; set; }
        public Reaction[] reactions { get; set; }
        public int thumb_360_w { get; set; }
        public string thumb_360 { get; set; }
        public DateTime created { get; set; }
        public DateTime timestamp { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string mimetype { get; set; }
        public string filetype { get; set; }
        public string pretty_type { get; set; }
        public string user { get; set; }
        public string mode { get; set; }
        public bool editable { get; set; }
        public bool is_external { get; set; }
        public string external_type { get; set; }
        public string username { get; set; }
        public ResponseUser linkedUser { get; set; }
        public int size { get; set; }
        public string url { get; set; }
        public string url_download { get; set; }
        public string url_private { get; set; }
        public string url_private_download { get; set; }
        public string thumb_64 { get; set; }
        public string thumb_80 { get; set; }
        public string thumb_160 { get; set; }
        public string thumb_360_gif { get; set; }

        ResponseFile(string name = "blank")
        {
            this.name = name;
        }

        public static ResponseFile FromFile(File file)
        {
            var f = new ResponseFile(file.name);

            f.id = file.id;
            f.thumb_480 = file.thumb_480;
            f.thumb_480_w = file.thumb_480_w;
            f.thumb_480_h = file.thumb_480_h;
            f.permalink = file.permalink;
            f.permalink_public = file.permalink_public;
            f.edit_link = file.edit_link;
            f.preview = file.preview;
            f.preview_highlight = file.preview_highlight;
            f.lines = file.lines;
            f.lines_more = file.lines_more;
            f.is_public = file.is_public;
            f.public_url_shared = file.public_url_shared;
            f.display_as_bot = file.display_as_bot;
            f.channels = file.channels;
            f.groups = file.groups;
            f.ims = file.ims;
            f.initial_comment = file.initial_comment;
            f.comments_count = file.comments_count;
            f.num_stars = file.num_stars;
            f.is_starred = file.is_starred;
            f.pinned_to = file.pinned_to;
            f.thumb_360_h = file.thumb_360_h;
            f.reactions = file.reactions;
            f.thumb_360_w = file.thumb_360_w;
            f.thumb_360 = file.thumb_360;
            f.created = file.created;
            f.timestamp = file.timestamp;
            f.name = file.name;
            f.title = file.title;
            f.mimetype = file.mimetype;
            f.filetype = file.filetype;
            f.pretty_type = file.pretty_type;
            f.user = file.user;
            f.mode = file.mode;
            f.editable = file.editable;
            f.is_external = file.is_external;
            f.external_type = file.external_type;
            f.username = file.username;
            f.size = file.size;
            f.url = file.url;
            f.url_download = file.url_download;
            f.url_private = file.url_private;
            f.url_private_download = file.url_private_download;
            f.thumb_64 = file.thumb_64;
            f.thumb_80 = file.thumb_80;
            f.thumb_160 = file.thumb_160;
            f.thumb_360_gif = file.thumb_360_gif;

            return f;
        }
    }
}
