using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatHandelTools.Common;

namespace MatHandelTools.ExchangeLists
{
	/// <summary>
	/// Based on intermediate geek list the program analyzes per user wants lists doing some basic
	/// validation and outputting useful data.
	/// </summary>
	/// <remarks>
	///   <para>
	/// Validations done by program are:
	/// <list type="bullet">
	///   <item>
	///     <description>
	/// Syntax of the wants list (based on experience and BGG Wiki).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// User name equality.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Covering of all offers which have not been marked as out of date.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Empty wants list for offers which have been marked as out of data.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of wanting an own offer.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of wanting an offer which does not exist.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of wants lists of unknown users (usually bad naming of a file).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of repeated wanted offers within single line.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of undefined named groups.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of defined and unused named groups.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Detection of named groups refering to only offers (not other named groups) which are not all
	/// the same game.
	///     </description>
	///   </item>
	/// </list>
	///   </para>
	///   <para>
	/// Output data is:
	/// <list type="bullet">
	///   <item>
	///     <description>
	/// Information on users who already sent their wants lists. This is meant to be put on BGG for
	/// everyone to see on whom we are still waiting. (<see cref="UsersFileName"/>)
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Merged wants file to be used by TradeMaximizer. (<see cref="WantsFileName"/>)
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	/// Information on warnings and errors on the lists. (<see cref="WarningsFileName"/>)
	///     </description>
	///   </item>
	/// </list>
	///   </para>
	/// </remarks>
	static class Program
	{
		/// <summary>
		/// Name of file which will receive the information on users who already sent their wants
		/// lists.
		/// </summary>
		/// <remarks>
		///   <para>
		/// This is meant to be put on BGG for everyone to see on whom we are still waiting.
		///   </para>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string UsersFileName = "ExchangeLists-Users.txt";

		/// <summary>
		/// Name of file which will receive merged wants lists to be used by TradeMaximizer.
		/// </summary>
		/// <remarks>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string WantsFileName = "ExchangeLists-Wants.txt";

		/// <summary>
		/// Name of file which will receive information on warnings and errors on the lists.
		/// </summary>
		/// <remarks>
		///   <para>
		/// The file is overwritten.
		///   </para>
		/// </remarks>
		private const string WarningsFileName = "ExchangeLists-Warnings.txt";


		/// <summary>
		/// Extension of the per-user wants list file.
		/// </summary>
		private const string WantsListFileExtension = "txt";


		/// <summary>
		/// Offer data used during validation.
		/// </summary>
		private struct OfferData
		{
			/// <summary>
			/// Name of the user owning the offer.
			/// </summary>
			public string fromUser;

			/// <summary>
			/// Determines whether the offer is out of data.
			/// </summary>
			public bool isOutOfDate;

			/// <summary>
			/// BGG id of the main game in the offer.
			/// </summary>
			/// <remarks>
			/// <para>
			/// Main game in the offer is the one from the geek list item (as opposed to those that
			/// are in comments).
			/// </para>
			/// </remarks>
			public string mainGameId;

		};


		/// <summary>
		/// Auxiliary function generating warning message in unified format.
		/// </summary>
		/// <param name="userName">
		/// Name of the user for which wants list the warning is issued.
		/// </param>
		/// <param name="maxUserNameLength">
		/// Maximal user name length (for formatting).
		/// </param>
		/// <param name="line">
		/// Index of the line in the wants list file for which the warning is issued or <c>null</c>
		/// if the warning is not associated with any particular line.
		/// </param>
		/// <param name="text">
		/// Warning text.
		/// </param>
		/// <returns>
		/// Warning message in unified format. This message is supposed to be output to warnings
		/// file (<see cref="WarningsFileName"/>).
		/// </returns>
		private static string formatWarning(string userName, int maxUserNameLength, int? line, string text)
		{
			string userNameField = string.Format(Culture.Generic, "{{0,-{0}}}", maxUserNameLength);
			string lineField;
			if (line.HasValue)
			{
				lineField = "at line {1,-3}";
			}
			else
				lineField = "           ";

			return string.Format(Culture.Generic, userNameField + "; " + lineField + "; {2}", userName, line, text);
		}


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
		///   <item>
		///     <description>
		/// Second argument is path (absolute or relative) to directory containing exchange list
		/// files for each user. It is optional, if not specified current directory is used. Other
		/// arguments are ignored.
		///     </description>
		///   </item>
		/// </list>
		/// </param>
		static void Main(string[] args)
		{
			// Required argument.
			string geekListId = args[0];

			// Optional argument.
			string wantsListsPath;
			if (args.Length > 1)
				wantsListsPath = args[1];
			else
				wantsListsPath = ".";

			// First delete output files so there will be no misleading files in case of failure.
			File.Delete(UsersFileName);
			File.Delete(WantsFileName);
			File.Delete(WarningsFileName);

			// Get intermediate geek list.
			XDocument geekList = GeekList.Get(geekListId);
			// Maps user names to collection of their offers (indexes). Also sorts users by name.
			IDictionary<string, ICollection<string>> allUsers = new SortedList<string, ICollection<string>>(new CultureComparer(Culture.Native));
			// Maximal length of user name. Used in formatting.
			int maxUserNameLength = 0;
			// Lists all offers keeping their data.
			IDictionary<string, OfferData> allOffers = new Dictionary<string, OfferData>();

			// Retrieve from the intermediate geek list users taking part in the trade and offers
			// each of them made.
			foreach (XElement itemElement in geekList.Root.Elements("item"))
			{
				string userName = itemElement.Element("username").Value;
				string index = itemElement.Attribute("index").Value;

				maxUserNameLength = Math.Max(maxUserNameLength, userName.Length);

				// Check whether the offer is ouf of data.
				string outOfDateAsString = itemElement.Attribute("outofdate").Value;
				bool outOfDate = bool.Parse(outOfDateAsString);

				string mainGameId = itemElement.Element("objectid").Value;

				allOffers.Add(index, new OfferData() { isOutOfDate = outOfDate, fromUser = userName, mainGameId = mainGameId });

				// Check if user is already in the dictionary and if not add an entry.
				// One way or the other offersCollection will be the collection of offers for that
				// user.
				ICollection<string> offersCollection;
				if (!allUsers.TryGetValue(userName, out offersCollection))
				{
					offersCollection = new HashSet<string>();
					allUsers.Add(userName, offersCollection);
				}
				Debug.Assert(offersCollection != null);

				// Add the offer.
				Debug.Assert(!offersCollection.Contains(index));
				offersCollection.Add(index);
			}


			// Now generate output.


			// Output anything to buffers and save the buffers to disk after all is done. This way
			// we avoid (as much as possible) partial files in case of errors.
			StringWriter usersListBuffer = new StringWriter(Culture.Generic);
			StringWriter wantsBuffer = new StringWriter(Culture.Generic);
			StringWriter warningsBuffer = new StringWriter(Culture.Generic);

			// Trace whether there are any warnings at all. Since if there are none do not create
			// warnings file to give clear feedback that everything is OK.
			bool anyWarnings = false;


			// First check if there are file of unknown users.

			foreach (string filePath in Directory.EnumerateFiles(wantsListsPath, "*." + WantsListFileExtension))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
				string fileName = Path.GetFileName(filePath);
				if (!allUsers.ContainsKey(fileNameWithoutExtension))
				{
					warningsBuffer.WriteLine(
						formatWarning(
							fileNameWithoutExtension,
							maxUserNameLength,
							null,
							string.Format(Culture.Generic, "Unknown user from file {0}", fileName)
						)
					);
					anyWarnings = true;
				}
			}


			// Then validate files of known users.

			// Count number of wants lists already present. Number of all lists that we should get
			// is the size of allUsers.
			int receivedListsCount = 0;
			// Count number of offers having a wants list. Number of all offers that we should get
			// is the size of allOfferss.
			int receivedOffersCount = 0;
			// For each user taking part in the MathTrade analyze his (her) wants list.
			foreach (KeyValuePair<string, ICollection<string>> userEntry in allUsers)
			{
				string userName = userEntry.Key;
				string fileName = userEntry.Key + "." + WantsListFileExtension;
				string filePath = Path.Combine(wantsListsPath, fileName);

				// Make a copy of the offers of current user as we will remove offers from this
				// collection while analyzing wants list (to see in the end whether all offers were
				// covered.
				ICollection<string> userOffersCopy = new HashSet<string>(userEntry.Value);
				
				// Stores all names (%...) defined by the user.
				ICollection<string> definedNames = new HashSet<string>();
				// Stores all names (%...) referenced by the user.
				ICollection<string> referencedNames = new HashSet<string>();

				// Flag set if there is any file with wants list.
				bool isExchangeList;
				try
				{
					// Try to open wants list of the user.
					using (StreamReader file = File.OpenText(filePath))
					{
						isExchangeList = true;

						// Analyze each line of the wants list.
						// Count line index from 1 to have it aligned with what text editors show.
						int index = 1;
						for (string line = file.ReadLine(); line != null; line = file.ReadLine(), ++index)
						{
							// Output to merged file anything to prevent problems if this program
							// would work incorrectly.
							wantsBuffer.WriteLine(line);

							// Skip empty lines to allow some more formatting on lists.
							if (line.Length == 0)
							{
								continue;
							}
							Debug.Assert(line.Length > 0);

							// Skip comment lines.
							if (line[0] == '#')
							{
								// Check whether it is not an instruction line.
								if ((line.Length > 1) && (line[1] == '!'))
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											"Instruction line"
										)
									);
									anyWarnings = true;
								}

								continue;
							}

							// Otherwise interpret the line as part of wants list.
							Match match = Regex.Match(line, @"\A *\((?<username>[^\)]+)\) +((?<name>%[^\s:#]+)|(?<index>\d+)) *: *(( +|( *; *)+)((?<wantedname>%[^\s:#]+)|(?<wantedindex>\d+)))* *\Z");
							if (!match.Success)
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										"Syntax error"
									)
								);
								anyWarnings = true;
								continue;
							}

							if (match.Groups["username"].Value != userName)
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										"Bad user name"
									)
								);
								anyWarnings = true;
								continue;
							}

							if (match.Groups["index"].Success)
							{
								if (!userOffersCopy.Remove(match.Groups["index"].Value))
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											"Bad offer index"
										)
									);
									anyWarnings = true;
									continue;
								}
							}
							else if (match.Groups["name"].Success)
							{
								definedNames.Add(match.Groups["name"].Value);
							}
							else
							{
								// There must be either index or name.
								Debug.Assert(false);
							}

							// If the index is in userOffersCopy then it must be in allOffers as
							// well.
							Debug.Assert(!match.Groups["index"].Success || allOffers.ContainsKey(match.Groups["index"].Value));

							if (match.Groups["index"].Success)
								++receivedOffersCount;

							if (match.Groups["index"].Success && match.Groups["wanted"].Success && allOffers[match.Groups["index"].Value].isOutOfDate)
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										"Non-empty list for out of date offer"
									)
								);
								anyWarnings = true;
								continue;
							}

							// Stores all wanted things (both indexes and names) to see whether
							// nothing is repeated.
							ICollection<string> wantedThings = new HashSet<string>();

							foreach (Capture capture in match.Groups["wantedindex"].Captures)
							{
								if (!allOffers.ContainsKey(capture.Value))
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Wanting unknown offer {0}", capture.Value)
										)
									);
									anyWarnings = true;
									continue;
								}
								if (allOffers[capture.Value].fromUser == userName)
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Wanting own offer {0}", capture.Value)
										)
									);
									anyWarnings = true;
									continue;
								}
								if (allOffers[capture.Value].isOutOfDate)
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Wanting out of date offer {0}", capture.Value)
										)
									);
									anyWarnings = true;
									continue;
								}
								if (wantedThings.Contains(capture.Value))
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Repeated \"{0}\"", capture.Value)
										)
									);
									anyWarnings = true;
									continue;
								}
								else
								{
									wantedThings.Add(capture.Value);
								}
							}

							foreach (Capture capture in match.Groups["wantedname"].Captures)
							{
								if (wantedThings.Contains(capture.Value))
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Repeated \"{0}\"", capture.Value)
										)
									);
									anyWarnings = true;
									continue;
								}
								else
								{
									wantedThings.Add(capture.Value);
									referencedNames.Add(capture.Value);
								}
							}

							// For names that refer only to indexes (and not other names) check if
							// all the indexes are the same offer type (same game).
							if (match.Groups["name"].Success && (match.Groups["wantedname"].Captures.Count == 0))
							{
								ICollection<string> gameIds = new HashSet<string>();

								foreach (Capture capture in match.Groups["wantedindex"].Captures)
								{
									// This was checked before and a warning was issued if needed
									// and to avoid further errors be safe now.
									if (allOffers.ContainsKey(capture.Value))
										gameIds.Add(allOffers[capture.Value].mainGameId);
								}

								if (gameIds.Count != 1)
								{
									warningsBuffer.WriteLine(
										formatWarning(
											userName,
											maxUserNameLength,
											index,
											string.Format(Culture.Generic, "Group \"{0}\" has different games", match.Groups["name"].Value)
										)
									);
									anyWarnings = true;
									continue;
								}
							}

							Debug.Assert(!match.NextMatch().Success);
						}

						// Check if all referenced names are defined.
						foreach (string name in referencedNames)
						{
							if (!definedNames.Contains(name))
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										string.Format(Culture.Generic, "Use of undefined name \"{0}\"", name)
									)
								);
								anyWarnings = true;
								continue;
							}
						}

						// Check if all defined names are used.
						foreach (string name in definedNames)
						{
							if (!referencedNames.Contains(name))
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										string.Format(Culture.Generic, "Defined unused name \"{0}\"", name)
									)
								);
								anyWarnings = true;
								continue;
							}
						}

						// For each offer not present (or understood) on the wants list...
						foreach (string offerIndex in userOffersCopy)
						{
							// ...issue a warning if it is not marked as out of date...
							if (!allOffers[offerIndex].isOutOfDate)
							{
								warningsBuffer.WriteLine(
									formatWarning(
										userName,
										maxUserNameLength,
										index,
										string.Format(Culture.Generic, "Skipped offer: {0}", offerIndex)
									)
								);
								anyWarnings = true;
							}

							// ...and output empty entry to merged wants list to avoid
							// TradeMaximizer errors.
							wantsBuffer.WriteLine(string.Format(Culture.Generic, "({0}) {1} : ", userName, offerIndex));

							++receivedOffersCount;
						}
					}
				}
				catch (FileNotFoundException)
				{
					// There is no wants list for that user (yet).
					isExchangeList = false;
				}


				if (isExchangeList)
				{
					++receivedListsCount;
				}


				if (isExchangeList)
				{
					usersListBuffer.Write("[-]");
				}
				else
				{
					usersListBuffer.Write("[b]");
				}
				usersListBuffer.Write("[geekurl=/user/{0}]{1}[/geekurl]", Uri.EscapeUriString(userName), userName);
				if (isExchangeList)
				{
					usersListBuffer.Write("[/-]");
				}
				else
				{
					usersListBuffer.Write("[/b]");
				}

				usersListBuffer.WriteLine();
			}


			// Now output to files

			StringWriter usersListWithHeaderBuffer = new StringWriter(Culture.Generic);
			usersListWithHeaderBuffer.WriteLine(string.Format(Culture.Native, "Odebrane listy wymian: {0}/{1} uczestników, {2}/{3} ofert", receivedListsCount, allUsers.Count, receivedOffersCount, allOffers.Count));
			usersListWithHeaderBuffer.WriteLine();
			usersListWithHeaderBuffer.WriteLine("[i][-]przekreślenie[/-] oznacza, że listę wymian od tej osoby już odebrałem i \"przetworzyłem\" - jeśli nie dostałeś żadnego GeekMail'a z uwagami, to wszystko jest poprawne, przynajmniej technicznie. [b]wytłuszczenie[/b] oznacza, że albo nie wysłałeś listy, albo ciągle czeka na \"przetworzenie\".[/i]");
			usersListWithHeaderBuffer.WriteLine();
			usersListWithHeaderBuffer.Write(usersListBuffer.ToString());

			File.WriteAllText(UsersFileName, usersListWithHeaderBuffer.ToString());

			File.WriteAllText(WantsFileName, wantsBuffer.ToString());

			if (anyWarnings)
			{
				File.WriteAllText(WarningsFileName, warningsBuffer.ToString());
			}
		}
	}
}
