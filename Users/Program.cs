using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using PolskiMatHandel.Tools.Common;

namespace PolskiMatHandel.Tools.Users
{
	/// <summary>
	/// The program generates list of MathTrade users.
	/// </summary>
	class Program
	{
		/// <summary>
		/// Name of file which will receive the list of MathTrade users.
		/// </summary>
		/// <remarks>
		///   <para>
		/// This file could be used as contents of "To:" field when composing GeekMail.
		///   </para>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string UsersListFileName = "Users-List.txt";

		/// <summary>
		/// Separator placed between user nicks.
		/// </summary>
		private const string Separator = ",";

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
			File.Delete(UsersListFileName);

			// Get intermediate geek list.
			XDocument geekList = GeekList.Get(geekListId);
			// Stores all users.
			ISet<string> allUsers = new SortedSet<string>(new CultureComparer(Culture.Generic));

			// Retrieve from the intermediate geek list users taking part in the trade.
			foreach (XElement itemElement in geekList.Root.Elements("item"))
			{
				string userName = itemElement.Element("username").Value;
				allUsers.Add(userName);
			}


			// Now generate output.


			// Output anything to buffers and save the buffers to disk after all is done. This way
			// we avoid (as much as possible) partial files in case of errors.
			StringWriter usersListBuffer = new StringWriter(Culture.Generic);


			bool isFirst = true;
			foreach (string userName in allUsers)
			{
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					usersListBuffer.Write(Separator);
				}

				usersListBuffer.Write(userName);
			}


			// Now output to files

			File.WriteAllText(UsersListFileName, usersListBuffer.ToString());
		}
	}
}
