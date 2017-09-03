using System.Diagnostics;
using System.Globalization;

namespace MatHandelTools.Common
{
	/// <summary>
	/// Serves <see cref="CultureInfo"/> objects to be used when handling data conversions.
	/// </summary>
	public static class Culture
	{
		/// <summary>
		/// Gets <see cref="CultureInfo"/> object to be used when handling persistence of
		/// intermediate data (like the simplified geek list) or formatting scriptural data (like
		/// arguments in tags).
		/// </summary>
		/// <value>
		/// Gets (and if needed creates) <see cref="generic"/> object.
		/// </value>
		/// <remarks>
		///   <para>
		/// This will never be <c>null</c>.
		///   </para>
		/// </remarks>
		public static CultureInfo Generic
		{
			get
			{
				if (generic == null)
				{
					generic = CultureInfo.InvariantCulture;
				}
				Debug.Assert(generic != null);
				return generic;
			}
		}

		/// <summary>
		/// Gets <see cref="CultureInfo"/> object to be used when handling users-provided texts.
		/// </summary>
		/// <value>
		/// Gets (and if needed creates) <see cref="native"/> object.
		/// </value>
		/// <remarks>
		///   <para>
		/// This will never be <c>null</c>.
		///   </para>
		/// </remarks>
		public static CultureInfo Native
		{
			get
			{
				if (native == null)
				{
					native = new CultureInfo("pl-PL");
				}
				Debug.Assert(native != null);
				return native;
			}
		}


		/// <summary>
		/// <see cref="CultureInfo"/> object to be used when handling persistence of
		/// intermediate data (like the simplified geek list) or formatting scriptural data (like
		/// arguments in tags).
		/// </summary>
		private static CultureInfo generic;

		/// <summary>
		/// <see cref="CultureInfo"/> object to be used when handling users-provided texts.
		/// </summary>
		private static CultureInfo native;
	}
}
