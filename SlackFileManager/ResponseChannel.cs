using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackFileManager
{
    public class ResponseChannel
    {
        public string name { get; set; }

        public ResponseChannel(string name="blank")
        {
            this.name = name;
        }
    }
}
