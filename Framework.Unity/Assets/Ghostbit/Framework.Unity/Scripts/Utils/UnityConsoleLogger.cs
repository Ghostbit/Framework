using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ghostbit.Framework.Unity.Utils
{
    public static class UnityConsoleLogger
    {
        public static void Log(string level, string logger, string message)
        {
            Debug.Log(level + "|" + logger + "|" + message);
        }
    }
}
