using System.IO;
using PolskiMatHandel.Tools.Common;

namespace PolskiMatHandel.Tools.GetList
{
	/// <summary>
	/// The program (re)generates intermediate geek list.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The program is mostly for designing/debugging purposes as other tools generate/load the
	/// intermediate geek list anyway.
	/// </para>
	/// </remarks>
	static class Program
	{
		/// <summary>
		/// Program startup function.
		/// </summary>
		/// <param name="args">
		/// External arguments for the program.
		/// <list type="number">
		///   <item>
		///     <description>
		/// First argument is the MathTrade geek list identifier. It is required.
		///     </description>
		///   </item>
		/// </list>
		/// </param>
		static void Main(string[] args)
		{
			// Required argument.
			string geekListId = args[0];

			// First delete the output file. Regenerating the list is actually the only purpose of
			// this program.
			File.Delete(GeekList.GetFileName(geekListId));

			// Now regenerate the list. Note that Get will save the downloaded list.
			GeekList.Get(geekListId);
		}
	}
}
