using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Evaisa.Twitch
{
    internal class Utils
    {
        public class IlLine
        {
            public static void init()
            {
                new ILHook(typeof(StackTrace).GetMethod("AddFrames", BindingFlags.Instance | BindingFlags.NonPublic), IlHook);
            }

            private static void IlHook(ILContext il)
            {
                var cursor = new ILCursor(il);
                cursor.GotoNext(
                    x => x.MatchCallvirt(typeof(StackFrame).GetMethod("GetFileLineNumber", BindingFlags.Instance | BindingFlags.Public))
                );

                cursor.RemoveRange(2);
                cursor.EmitDelegate<Func<StackFrame, string>>(GetLineOrIL);
            }

            private static string GetLineOrIL(StackFrame instace)
            {
                var line = instace.GetFileLineNumber();
                if (line == StackFrame.OFFSET_UNKNOWN || line == 0)
                {
                    return "IL_" + instace.GetILOffset().ToString("X4");
                }

                return line.ToString();
            }
        }

        public static string Base64UrlencodeNoPadding(byte[] buffer)
        {
            string text = Convert.ToBase64String(buffer);
            text = text.Replace("+", "-");
            text = text.Replace("/", "_");
            return text.Replace("=", "");
        }
        public static string RandomDataBase64Url(uint length)
        {
            RNGCryptoServiceProvider rngcryptoServiceProvider = new RNGCryptoServiceProvider();
            byte[] array = new byte[length];
            rngcryptoServiceProvider.GetBytes(array);
            return Utils.Base64UrlencodeNoPadding(array);
        }

    }
}
