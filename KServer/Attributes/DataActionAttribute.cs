using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KServer.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DataAction(string dataType) : Attribute
    {
        public string dataType = dataType;
    }
}
