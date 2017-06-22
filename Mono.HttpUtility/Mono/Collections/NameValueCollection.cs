using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Collections
{

    public class NameValueCollection
        : Dictionary<string, object>
    {

        public string[] AllKeys
        {
            get
            {
                return this.Keys.ToArray();
            }
        }

    }

}