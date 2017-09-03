using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using MatHandelTools.Common;

namespace MatHandelTools.ShortList
{
	/// <summary>
	/// Based on intermediate geek list the program prepares short list and does some basic
	/// validations.
	/// </summary>
	/// <remarks>
	///   <para>
	/// Validations done by program are:
	/// <list type="bullet">
	///   <item>
	///     <description>
	/// Detection of comments on offers belonging to other users.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Comments containing no game link.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Comments containing more than one game link.
	///     </description>
	///   </item>
	/// </list>
	///   </para>
	///   <para>
	/// Output data is:
	/// <list type="bullet">
	///   <item>
	///     <description>
	/// Short offers list. The list includes link to geek list item, name and link to games and name
	/// and link to offering user. This is meant to be put on BGG for everyone to see easier all
	/// offers. (<see cref="ListFileName"/>)
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Groups list sorted by number. The list includes named groups for very game that appeared.
	/// (<see cref="GroupsByNumberFileName"/>)
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Groups list sorted by game name. The list includes named groups for very game that appeared.
	/// (<see cref="GroupsByNameFileName"/>)
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Information on warnings and errors on the geek list. (<see cref="WarningsFileName"/>)
	///     </description>
	///   </item>
	/// </list>
	///   </para>
	/// </remarks>
	public static class Program
	{
		/// <summary>
		/// Name of file which will receive the short offers list.
		/// </summary>
		/// <remarks>
		///   <para>
		/// This is meant to be put on BGG for everyone to see easier all offers.
		///   </para>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string ListFileName = "ShortList-List.html";

		/// <summary>
		/// Name of file which will receive the offers groups.
		/// </summary>
		/// <remarks>
		///   <para>
		/// This is meant to be put on BGG for everyone to use it as starting point while making their
		/// own exchange lists. This way it is easier to assure proper formatting (lots of errors are
		/// made with the named groups) and including all the games of the group (and not including
		/// improper ones).
		///   </para>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string GroupsByNumberFileName = "ShortList-Groups-ByNumber.html";

		/// <summary>
		/// Name of file which will receive the offers groups.
		/// </summary>
		/// <remarks>
		///   <para>
		/// This is meant to be put on BGG for everyone to use it as starting point while making their
		/// own exchange lists. This way it is easier to assure proper formatting (lots of errors are
		/// made with the named groups) and including all the games of the group (and not including
		/// improper ones).
		///   </para>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string GroupsByNameFileName = "ShortList-Groups-ByName.html";

		/// <summary>
		/// Name of file which will receive information on warnings and errors on the list.
		/// </summary>
		/// <remarks>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string WarningsFileName = "ShortList-Warnings.txt";


		/// <summary>
		/// Program startup function.
		/// </summary>
		/// <param name="args">
		/// External arguments for the program.
		/// <list type="number">
		///   <item>
		///     <description>
		/// First argument is the MathTrade geek list identifier. It is required. Other arguments
		/// arguments are ignored.
		///     </description>
		///   </item>
		/// </list>
		/// </param>
		public static void Main(string[] args)
		{
			// Required argument.
			string geekListId = args[0];

			// First delete output files so there will be no misleading files in case of failure.
			File.Delete(ListFileName);
			File.Delete(GroupsByNumberFileName);
			File.Delete(GroupsByNameFileName);
			File.Delete(WarningsFileName);

			// Download intermediate geek list.
			// Note that we are enforcing downloading as that's the hole point of this app.
			XDocument geekList = GeekList.Download(geekListId);
			// Then save for reuse in other apps.
			GeekList.Save(geekListId, geekList);

			// Output anything to buffers and save the buffers to disk after all is done. This way
			// we avoid (as much as possible) partial files in case of errors.
			StringWriter listBuffer = new StringWriter(Culture.Generic);
			StringWriter groupsByNumberBuffer = new StringWriter(Culture.Generic);
			StringWriter groupsByNameBuffer = new StringWriter(Culture.Generic);
			StringWriter warningsBuffer = new StringWriter(Culture.Generic);

			// Trace whether there are any warnings at all. Since if there are none do not create
			// warnings file to give clear feedback that everything is OK.
			bool anyWarnings = false;

			// Maps BGG game id to a map of offers (mapping GeekList item index to its GeekList item id)
			// containing that game. This allows later to generate named groups.
			IDictionary<string, IDictionary<int, string>> groups = new Dictionary<string, IDictionary<int, string>>();
			// Maps BGG game id to that game's primary name for group names use.
			IDictionary<string, string> groupNames = new SortedDictionary<string, string>();

			listBuffer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			listBuffer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			listBuffer.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"pl\" lang=\"pl\">");
			listBuffer.WriteLine("\t<head>");
			listBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Language\" content=\"pl\" />");
			listBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\" />");
			listBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Style-Type\" content=\"text/css\" />");
			listBuffer.WriteLine("\t\t<title>Lista skrócona</title>");
			listBuffer.WriteLine("\t\t<style type=\"text/css\">");
			listBuffer.WriteLine("\t\t\tbody {");
			listBuffer.WriteLine("\t\t\t\tfont-family: Verdana, \"Lucida Grande\", Arial, sans-serif;");
			listBuffer.WriteLine("\t\t\t}");
			listBuffer.WriteLine();
			listBuffer.WriteLine("\t\t\tol {");
			listBuffer.WriteLine("\t\t\t\tlist-style-type: none;");
			listBuffer.WriteLine("\t\t\t\tmargin: 0em;");
			listBuffer.WriteLine("\t\t\t\tpadding: 0em;");
			listBuffer.WriteLine("\t\t\t}");
			listBuffer.WriteLine("\t\t</style>");
			listBuffer.WriteLine("\t</head>");
			listBuffer.WriteLine("\t<body>");
			listBuffer.WriteLine("\t\t<ol>");

			// Iterate over all items on the GeekList.

			// Count index from 1 (and not 0) as GeekList (visually) counts from 1 too.
			int index = 1;
			foreach (XElement itemElement in geekList.Root.Elements("item"))
			{
				listBuffer.Write("\t\t\t<li>");

				// Get name of the user that added the item. It is later used to filter out comments
				// done by others (and thus not containing any games for offer).
				string itemUserName = itemElement.Element("username").Value;

				// First output order number which is also a link to the GeekList item itself.
				string itemId = itemElement.Attribute("id").Value;
				listBuffer.Write("<a href=\"http://www.boardgamegeek.com/geeklist/{0}/item/{1}#item{1}\">{2}.</a>", geekListId, itemId, index);

				// Output separator.
				listBuffer.Write("&nbsp;");

				// Then output game entry.

				// Check whether the offer is ouf of data.
				string itemOutOfDateAsString = itemElement.Attribute("outofdate").Value;
				bool itemOutOfDate = bool.Parse(itemOutOfDateAsString);

				if (itemOutOfDate)
				{
					listBuffer.Write("<em>NIEAKTUALNE</em>");
				}
				// If the game is available then add game entry to the list.
				else
				{
					// First output game name which is also a link to the game itself.
					string gameId = itemElement.Element("objectid").Value;
					string gameName = itemElement.Element("objectname").Value;
					listBuffer.Write("<a href=\"http://www.boardgamegeek.com/boardgame/{0}\">{1}</a>", gameId, gameName);

					AddGameToGroups(groups, gameId, index, itemId);
					AddGameToGroupNames(groupNames, gameId, gameName);

					// Then output additional games in the offer (if there are any).

					// Iterate over all comments.
					foreach (XElement commentElement in itemElement.Elements("comment"))
					{
						// Get name of the user that added the comment. It will be compared with the
						// name of the user which added GeekList item to determine whether this
						// comment should be taken into account as an offer.
						string commentUserName = commentElement.Element("username").Value;
						if (itemUserName != commentUserName)
						{
							warningsBuffer.WriteLine("Item {0} (\"{1}\" from \"{2}\")", index, gameName, itemUserName);
							warningsBuffer.WriteLine("\tComment from user \"{0}\"", commentUserName);
							// It seems there is no way to query for comment identifiers and thus we
							// can only give a link to entire item.
							warningsBuffer.WriteLine("\thttp://www.boardgamegeek.com/geeklist/{0}/item/{1}#item{1}", geekListId, itemId);
							anyWarnings = true;
							continue;
						}

						// Output additional game entry.

						// Check whether the offer is ouf of data.
						string commentOutOfDateAsString = commentElement.Attribute("outofdate").Value;
						bool commentOutOfDate = bool.Parse(commentOutOfDateAsString);

						if (commentOutOfDate)
						{
							listBuffer.Write("<em>NIEAKTUALNE</em>");
						}
						// If the additional game is available then add game entry to the list.
						else
						{
							int gamesCount = 0;
							// Iterate over all games in this comment.
							foreach (XElement gameElement in commentElement.Elements("game"))
							{
								++gamesCount;

								listBuffer.Write("&nbsp;+&nbsp;");

								XElement gameIdElement = gameElement.Element("objectid");
								if (gameIdElement != null)
								{
									XElement gameNameElement = gameElement.Element("objectname");

									AddGameToGroups(groups, gameIdElement.Value, index, itemId);
									AddGameToGroupNames(groupNames, gameIdElement.Value, gameNameElement.Value);

									listBuffer.Write("<a href=\"http://www.boardgamegeek.com/boardgame/{0}\">{1}</a>", gameIdElement.Value, gameNameElement.Value);
								}
								else
								{
									listBuffer.Write("?");
								}
							}

							if (gamesCount == 0)
							{
								warningsBuffer.WriteLine("Item {0} (\"{1}\" from \"{2}\")", index, gameName, itemUserName);
								warningsBuffer.WriteLine("\tNo game link");
								// It seems there is no way to query for comment identifiers and
								// thus we can only give a link to entire item.
								warningsBuffer.WriteLine("\thttp://www.boardgamegeek.com/geeklist/{0}/item/{1}#item{1}", geekListId, itemId);
								anyWarnings = true;

								listBuffer.Write("&nbsp;+&nbsp;");
								listBuffer.Write("?");
							}
							else if (gamesCount > 1)
							{
								warningsBuffer.WriteLine("Item {0} (\"{1}\" from \"{2}\")", index, gameName, itemUserName);
								warningsBuffer.WriteLine("\tMore than one game link");
								// It seems there is no way to query for comment identifiers and
								// thus we can only give a link to entire item.
								warningsBuffer.WriteLine("\thttp://www.boardgamegeek.com/geeklist/{0}/item/{1}#item{1}", geekListId, itemId);
								anyWarnings = true;
							}
						}
					}
				}

				// Output separator.
				listBuffer.Write("&nbsp;");

				// Finally output user name which is also a link to the profile itself.
				listBuffer.Write("(od&nbsp;<a href=\"http://www.boardgamegeek.com/user/{0}\">{1}</a>)", Uri.EscapeUriString(itemUserName), itemUserName);

				// Finish the game entry.
				listBuffer.WriteLine("</li>");

				++index;
			}

			listBuffer.WriteLine("\t\t</ol>");
			listBuffer.WriteLine("\t</body>");
			listBuffer.WriteLine("</html>");


			// Now generate groups description sorted by number (once we gathered all the offers).

			// Set of names already used for groups. This is used to detect cases
			// when different games have the same name.
			ISet<string> namesUsed = new HashSet<string>();

			// Maps group name to its definition (entire line). This will be later used to output groups
			// again but sorted by game name.
			IDictionary<string, string> groupsByName = new SortedDictionary<string, string>(new CultureComparer(Culture.Native));

			groupsByNumberBuffer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			groupsByNumberBuffer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			groupsByNumberBuffer.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"pl\" lang=\"pl\">");
			groupsByNumberBuffer.WriteLine("\t<head>");
			groupsByNumberBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Language\" content=\"pl\" />");
			groupsByNumberBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\" />");
			groupsByNumberBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Style-Type\" content=\"text/css\" />");
			groupsByNumberBuffer.WriteLine("\t\t<title>Grupy nazwane (indeksami)</title>");
			groupsByNumberBuffer.WriteLine("\t\t<style type=\"text/css\">");
			groupsByNumberBuffer.WriteLine("\t\t\tbody {");
			groupsByNumberBuffer.WriteLine("\t\t\t\tfont-family: Verdana, \"Lucida Grande\", Arial, sans-serif;");
			groupsByNumberBuffer.WriteLine("\t\t\t}");
			groupsByNumberBuffer.WriteLine();
			groupsByNumberBuffer.WriteLine("\t\t\tul {");
			groupsByNumberBuffer.WriteLine("\t\t\t\tlist-style-type: none;");
			groupsByNumberBuffer.WriteLine("\t\t\t\tmargin: 0em;");
			groupsByNumberBuffer.WriteLine("\t\t\t\tpadding: 0em;");
			groupsByNumberBuffer.WriteLine("\t\t\t}");
			groupsByNumberBuffer.WriteLine("\t\t</style>");
			groupsByNumberBuffer.WriteLine("\t</head>");
			groupsByNumberBuffer.WriteLine("\t<body>");
			groupsByNumberBuffer.WriteLine("\t\t<ul>");

			foreach (KeyValuePair<string, IDictionary<int, string>> group in groups)
			{
				StringWriter groupBuffer = new StringWriter(Culture.Generic);

				string name = groupNames[group.Key];
				name = TransformName(name);
				while (namesUsed.Contains(name))
				{
					name += "_1";
				}
				namesUsed.Add(name);

				groupBuffer.Write("\t\t\t<li>(nick) <a href=\"http://www.boardgamegeek.com/boardgame/{0}\">%{1}</a> :", group.Key, name);

				Debug.Assert(group.Value != null);
				foreach(KeyValuePair<int, string> offer in group.Value)
				{
					groupBuffer.Write("&nbsp;<a href=\"http://www.boardgamegeek.com/geeklist/{0}/item/{1}#item{1}\">{2}</a>", geekListId, offer.Value, offer.Key);
				}

				groupBuffer.Write("</li>");

				string line = groupBuffer.ToString();
				groupsByNumberBuffer.WriteLine(line);
				groupsByName.Add(name, line);
			}

			groupsByNumberBuffer.WriteLine("\t\t</ul>");
			groupsByNumberBuffer.WriteLine("\t</body>");
			groupsByNumberBuffer.WriteLine("</html>");

			// Now generate groups description sorted by name.

			groupsByNameBuffer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			groupsByNameBuffer.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			groupsByNameBuffer.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"pl\" lang=\"pl\">");
			groupsByNameBuffer.WriteLine("\t<head>");
			groupsByNameBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Language\" content=\"pl\" />");
			groupsByNameBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\" />");
			groupsByNameBuffer.WriteLine("\t\t<meta http-equiv=\"Content-Style-Type\" content=\"text/css\" />");
			groupsByNameBuffer.WriteLine("\t\t<title>Grupy nazwane (alfabetycznie)</title>");
			groupsByNameBuffer.WriteLine("\t\t<style type=\"text/css\">");
			groupsByNameBuffer.WriteLine("\t\t\tbody {");
			groupsByNameBuffer.WriteLine("\t\t\t\tfont-family: Verdana, \"Lucida Grande\", Arial, sans-serif;");
			groupsByNameBuffer.WriteLine("\t\t\t}");
			groupsByNameBuffer.WriteLine();
			groupsByNameBuffer.WriteLine("\t\t\tul {");
			groupsByNameBuffer.WriteLine("\t\t\t\tlist-style-type: none;");
			groupsByNameBuffer.WriteLine("\t\t\t\tmargin: 0em;");
			groupsByNameBuffer.WriteLine("\t\t\t\tpadding: 0em;");
			groupsByNameBuffer.WriteLine("\t\t\t}");
			groupsByNameBuffer.WriteLine("\t\t</style>");
			groupsByNameBuffer.WriteLine("\t</head>");
			groupsByNameBuffer.WriteLine("\t<body>");
			groupsByNameBuffer.WriteLine("\t\t<ul>");

			foreach (KeyValuePair<string, string> group in groupsByName)
			{
				groupsByNameBuffer.WriteLine(group.Value);
			}

			groupsByNameBuffer.WriteLine("\t\t</ul>");
			groupsByNameBuffer.WriteLine("\t</body>");
			groupsByNameBuffer.WriteLine("</html>");

			// Now output to files

			File.WriteAllText(ListFileName, listBuffer.ToString());
			File.WriteAllText(GroupsByNumberFileName, groupsByNumberBuffer.ToString());
			File.WriteAllText(GroupsByNameFileName, groupsByNameBuffer.ToString());

			if (anyWarnings)
			{
				File.WriteAllText(WarningsFileName, warningsBuffer.ToString());
			}
		}

		private static void AddGameToGroups(IDictionary<string, IDictionary<int, string>> groups, string gameId, int offerIndex, string offerId)
		{
			Debug.Assert(groups != null);
			if (!groups.ContainsKey(gameId))
			{
				groups.Add(gameId, new SortedDictionary<int, string>());
			}
			IDictionary<int, string> groupOffers = groups[gameId];

			// It is possible to have the same game as main entry in the offer and also in comment of the
			// offer. So we have to check.
			if (!groupOffers.ContainsKey(offerIndex))
			{
				groupOffers.Add(offerIndex, offerId);
			}
		}

		private static void AddGameToGroupNames(IDictionary<string, string> groupNames, string gameId, string gameName)
		{
			Debug.Assert(groupNames != null);
			if (!groupNames.ContainsKey(gameId))
			{
				groupNames.Add(gameId, gameName);
			}
			else
			{
				Debug.Assert(groupNames[gameId] == gameName);
			}
		}

		private static string TransformName(string name)
		{
			StringBuilder newName = new StringBuilder();
			foreach(char c in name)
			{
				if (!char.IsLetterOrDigit(c))
				{
					newName.Append('_');
				}
				else
				{
					newName.Append(char.ToUpper(c));
				}
			}
			return newName.ToString();
		}
	}
}
