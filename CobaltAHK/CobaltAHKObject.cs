using System;
using System.Linq;
using System.Collections.Generic;

namespace CobaltAHK
{
	public class CobaltAHKObject : Dictionary<object, object>
	{
		public CobaltAHKObject(IEnumerable<object> keys, IEnumerable<object> values)
		{
			if (keys.Count() != values.Count()) {
				throw new Exception(); // todo
			}
			for (var i = 0; i < keys.Count(); i++) {
				Add(keys.ElementAt(i), values.ElementAt(i));
			}
		}
	}
}

