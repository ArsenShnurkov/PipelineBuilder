using System;
using System.Collections.Generic;

namespace PipelineBuilder
{
	public static class Extensions
	{
		public static bool NotAllTrue<T>(this IEnumerable<T> things, Func<T, bool> fMap)
		{
			foreach (var thing in things)
				if (!fMap(thing)) return true;
			return false;
		}

		public static bool Not(this bool input) 
		{
			return !input;
		}

		public static bool AllTrue<T>(this IEnumerable<T> things, Func<int, T, bool> fMap)
		{
			// --start-- should be closure captured to be updated throughout iterations.
			bool start = true;
			return FoldL(things, start, (index, item, curr) => start &= fMap(index, item));
		}

		public static bool AllTrue<T>(this IEnumerable<T> things, Func<T, bool> fMap)
		{
			return AllTrue(things, (i, item) => fMap(item));
		}

		/// <summary>
		/// Folds left-wise (to right, since enumeration goes that way).
		/// </summary>
		/// <typeparam name="T">Type of item in enumerable.</typeparam>
		/// <typeparam name="TRes">Result-type.</typeparam>
		/// <param name="things">Things to fold over.</param>
		/// <param name="init">Initialization value.</param>
		/// <param name="fMap">Function to apply to each value, the first parameter being index.</param>
		/// <returns>The result of applying fMap to all items in the enumeration.</returns>
		public static TRes FoldL<T, TRes>(this IEnumerable<T> things, TRes init, Func<int, T, TRes, TRes> fMap)
		{
			TRes curr = init;

			int i = 0;
			foreach (var thing in things)
				curr = fMap(i++, thing, curr);

			return curr;
		}

		public static void Each<T>(this IEnumerable<T> ts, Action<T> apply)
		{
			foreach (var item in ts)
				apply(item);
		}
	}
}