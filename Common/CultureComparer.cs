using System;
using System.Collections.Generic;
using System.Globalization;

namespace MatHandelTools.Common
{
	/// <summary>
	/// Compares strings in string sorting order according to provided <see cref="CultureInfo"/>
	/// </summary>
	public class CultureComparer : IComparer<string>
	{
		/// <summary>
		/// Constructs <see cref="CultureComparer"/> object with specified <see cref="CultureInfo"/>.
		/// </summary>
		/// <param name="cultureInfo">
		/// <see cref="CultureInfo"/> object used to perform strings comparisons.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="cultureInfo"/> is <c>null</c>.
		/// </exception>
		public CultureComparer(CultureInfo cultureInfo)
		{
			if (cultureInfo == null)
				throw new ArgumentNullException("cultureInfo");
			this.cultureInfo = cultureInfo;
		}

		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or
		/// greater than the other.
		/// </summary>
		/// <param name="x">
		/// The first object to compare.
		/// </param>
		/// <param name="y">
		/// The second object to compare.
		/// </param>
		/// <returns>
		/// A signed integer that indicates the relative values of <paramref name="x"/> and
		/// <paramref name="y"/>, as shown in the following table.
		/// <list type="table">
		///   <listheader>
		///     <term>
		/// Value
		///     </term>
		///     <description>
		/// Meaning
		///     </description>
		///   </listheader>
		///   <item>
		///     <term>
		/// Less than zero
		///     </term>
		///     <description>
		/// <paramref name="x"/> is less than <paramref name="y"/>.
		///     </description>
		///   </item>
		///   <item>
		///     <term>
		/// Zero
		///     </term>
		///     <description>
		/// <paramref name="x"/> equals <paramref name="y"/>.
		///     </description>
		///   </item>
		///   <item>
		///     <term>
		/// Greater than zero
		///     </term>
		///     <description>
		/// <paramref name="x"/> is greater than <paramref name="y"/>.
		///     </description>
		///   </item>
		/// </list>
		/// </returns>
		public int Compare(string x, string y)
		{
			return this.cultureInfo.CompareInfo.Compare(x, y, CompareOptions.StringSort);
		}

		/// <summary>
		/// <see cref="CultureInfo"/> object used to perform strings comparisons.
		/// </summary>
		private readonly CultureInfo cultureInfo;
	}
}
