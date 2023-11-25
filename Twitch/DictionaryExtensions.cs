using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Evaisa.Twitch;

namespace Evaisa.Twitch
{
	// Token: 0x02000734 RID: 1844
	public static class DictionaryExtensions
	{
		// Token: 0x060027D9 RID: 10201 RVA: 0x0015728C File Offset: 0x0015548C
		public static string ToUriQueryParameters(this IDictionary<string, string> parameters)
		{
			return string.Join("&", from parameter in parameters
									select parameter.Key + "=" + WebUtility.UrlEncode(parameter.Value));
		}

		// Token: 0x060027DA RID: 10202 RVA: 0x001572C0 File Offset: 0x001554C0
		public static T Value<T>(this IDictionary<string, object> dict, string key)
		{
			object obj;
			if (dict.TryGetValue(key, out obj))
			{
				if (obj is T)
				{
					return (T)((object)obj);
				}
				try
				{
					return (T)((object)Convert.ChangeType(obj, typeof(T)));
				}
				catch (InvalidCastException)
				{
					return default(T);
				}
			}
			return default(T);
		}
	}
}
