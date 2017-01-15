using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PolskiMatHandel.Tools.Common
{
	/// <summary>
	/// Handles geek list querying as well as storing and loading in intermediate format.
	/// </summary>
	/// <remarks>
	///   <para>
	/// The intermediate format is an even more simplified XML file. Using this file allows to use
	/// simpler processing in actual tools. Also persisting it allows to do only a single query even
	/// if few tools will be run in sequence (also this assures data consistency).
	///   </para>
	/// </remarks>
	public static class GeekList
	{
		/// <summary>
		/// Returns intermediate geek list either by loading it from file or downloading.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier.
		/// </param>
		/// <returns>
		///   <para>
		/// Document with intermediate geek list.
		///   </para>
		///   <para>
		/// The document is loaded from file (with <see cref="Load"/> function) if there is such a
		/// (valid) file. Otherwise the document is downloaded (with <see cref="Download"/>
		/// function) instead. It is important to delete the intermediate geek list file if the
		/// online list changes as otherwise this function will not see online changes always
		/// loading the offline file.
		///   </para>
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier. Note that this does
		/// not cover cases where there is no geek list with such identifier. It is only thrown if
		/// the general properties of the identifier do not hold.
		/// </exception>
		/// <remarks>
		///   <para>
		/// If the geek list is downloaded it is also saved. So next call to <see cref="Get"/>
		/// function (including a call in different application) will load the offline file.
		///   </para>
		/// </remarks>
		/// <seealso cref="Download"/>
		/// <seealso cref="Load"/>
		/// <seealso cref="Save"/>
		public static XDocument Get(string id)
		{
			VerifyId(id);

			XDocument document = null;

			// First try to load the document from file (as it is faster than downloading and
			// parsing).
			try
			{
				document = Load(id);
			}
			catch (FileNotFoundException)
			{
				// Ignore this error. document remains null and we will download it bellow.
			}
			catch (XmlException)
			{
				// Ignore this error. document remains null and we will download it bellow.
			}

			// If we failed to load the document from file (in a predicted way) then try downloading
			// it and saving it back to file for future use.
			if (document == null)
			{
				document = Download(id);
				Save(id, document);
			}

			Debug.Assert(document != null);

			return document;
		}

		/// <summary>
		/// Downloads specified geek list and returns an intermediate version of it.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier.
		/// </param>
		/// <returns>
		/// Document with intermediate geek list.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier. Note that this does
		/// not cover cases where there is no geek list with such identifier. It is only thrown if
		/// the general properties of the identifier do not hold.
		/// </exception>
		/// <remarks>
		///   <para>
		/// The geek list is downloaded using BGG XML API 2 and then parsed into an intermediate
		/// format.
		///   </para>
		/// </remarks>
		/// <seealso cref="Load"/>
		/// <seealso cref="Save"/>
		public static XDocument Download(string id)
		{
			VerifyId(id);

			string queryUriFormat = "https://www.boardgamegeek.com/xmlapi2/geeklist/{0}?comments=1";
			string queryUriUnescaped = string.Format(Culture.Generic, queryUriFormat, id);
			string queryUri = Uri.EscapeUriString(queryUriUnescaped);

			XDocument document = XDocument.Load(queryUri);

			return ParseList(document);
		}

		/// <summary>
		/// Loads specified intermediate geek list.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier.
		/// </param>
		/// <returns>
		/// Document with intermediate geek list.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier. Note that this does
		/// not cover cases where there is no geek list with such identifier. It is only thrown if
		/// the general properties of the identifier do not hold.
		/// </exception>
		/// <exception cref="FileNotFoundException">
		/// Thrown if there is no file with previously saved intermediate geek list (for specified
		/// <paramref name="id"/>).
		/// </exception>
		/// <exception cref="XmlException">
		/// Thrown if the file with intermediate geek list is not valid.
		/// </exception>
		/// <seealso cref="Download"/>
		/// <seealso cref="Save"/>
		public static XDocument Load(string id)
		{
			VerifyId(id);

			string fileName = GetFileName(id);

			return XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
		}

		/// <summary>
		/// Saves specified intermediate geek list.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier.
		/// </param>
		/// <param name="document">
		/// Intermediate geek list to be saved. Note that this document is not verified in any way
		/// (except for being non-<c>null</c>) and thus it is callers responsibility to assure that
		/// only documents returned by <see cref="Load"/> and <see cref="Download"/> functions are
		/// provided.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier. Note that this does
		/// not cover cases where there is no geek list with such identifier. It is only thrown if
		/// the general properties of the identifier do not hold.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if <paramref name="document"/> is <c>null</c>.
		/// </exception>
		/// <seealso cref="Download"/>
		/// <seealso cref="Load"/>
		public static void Save(string id, XDocument document)
		{
			VerifyId(id);
			if (document == null)
				throw new ArgumentNullException("document");

			string fileName = GetFileName(id);

			XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
			xmlWriterSettings.CheckCharacters = true;
			xmlWriterSettings.CloseOutput = true;
			xmlWriterSettings.ConformanceLevel = ConformanceLevel.Document;
			xmlWriterSettings.Encoding = Encoding.UTF8;
			xmlWriterSettings.Indent = true;

			using (XmlWriter xmlWriter = XmlWriter.Create(fileName, xmlWriterSettings))
			{
				document.Save(xmlWriter);
			}
		}

		/// <summary>
		/// Returns intermediate geek list file name.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier.
		/// </param>
		/// <returns>
		/// Intermediate geek list file name.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier. Note that this does
		/// not cover cases where there is no geek list with such identifier. It is only thrown if
		/// the general properties of the identifier do not hold.
		/// </exception>
		/// <remarks>
		///   <para>
		/// The file name returned by this function is used by other functions of this class which
		/// persist the intermediate geek list (<see cref="Get"/> and <see cref="Save"/> functions).
		///   </para>
		/// </remarks>
		public static string GetFileName(string id)
		{
			VerifyId(id);
			return "Geek List " + id + ".xml";
		}

		/// <summary>
		/// Verifies validity of the specified geek list identifier.
		/// </summary>
		/// <param name="id">
		/// Geek list identifier which validity is to be checked.
		/// </param>
		/// <exception cref="ArgumentException">
		/// Thrown if <paramref name="id"/> is not a valid geek list identifier.
		/// </exception>
		/// <remarks>
		///   <para>
		/// The function does not return a value. Instead it throws if the identifier is not valid.
		///   </para>
		///   <para>
		/// Note that the function does not check whether there is any geek list with such
		/// identifier. It only checks the general properties of the identifier.
		///   </para>
		/// </remarks>
		private static void VerifyId(string id)
		{
			int intId;
			if (!int.TryParse(id, out intId))
				throw new ArgumentException("id is not valid geek list identifier", "id");
		}

		private static XDocument ParseList(XDocument listDocument)
		{
			XDocument document = new XDocument(new XElement("geeklist"));

			// Maps BGG game ids to their primary names.
			// This is used mostly to reduce number of times we have to make a separate query to find out
			// what is the name of a game in a comment (as this are provided through a link only).
			IDictionary<string, string> objectNames = new Dictionary<string, string>();

			// Iterate over all items on the geek list creating for each of them an entry on our
			// intermediate geek list. Note that indexes start from 1 to align with indexes shown on
			// BBG geek list items.
			int index = 1;
			foreach (XElement itemElement in listDocument.Root.Elements("item"))
			{
				document.Root.Add(ParseItem(itemElement, index, objectNames));
				++index;
			}

			return document;
		}

		private static XElement ParseItem(XElement itemElement, int index, IDictionary<string, string> objectNames)
		{
			Debug.Assert(objectNames != null);

			XElement element = new XElement("item");

			// Add index of the geek list item (as seen by BGG user).
			element.SetAttributeValue("index", index.ToString(Culture.Generic));

			// Add id of the geek list item (as used by BGG).
			string id = itemElement.Attribute("id").Value;
			element.SetAttributeValue("id", id);

			// First check whether the offer is out of date and set proper attribute if so.
			string text = itemElement.Element("body").Value;
			bool isOutOfDate = IsOutOfDate(text);
			element.SetAttributeValue("outofdate", isOutOfDate.ToString(Culture.Generic));

			// Then retrieve other data. If the offer is out of date we could skip that but there is
			// not much gain in doing that so lets have full data in case we would ever want it
			// later.

			// Add name of the user that added the item.
			string userName = itemElement.Attribute("username").Value;
			element.Add(new XElement("username", userName));

			// Add game identifier.
			string objectId = itemElement.Attribute("objectid").Value;
			element.Add(new XElement("objectid", objectId));

			// Add game displayable name.
			string objectName = itemElement.Attribute("objectname").Value;
			element.Add(new XElement("objectname", objectName));

			if (!objectNames.ContainsKey(objectId))
			{
				objectNames.Add(objectId, objectName);
			}
			Debug.Assert(objectNames[objectId] == objectName);

			// Iterate over all comments of the geek list item creating for each of them a
			// sub-entry.
			foreach (XElement commentElement in itemElement.Elements("comment"))
			{
				element.Add(ParseComment(commentElement, objectNames));
			}

			return element;
		}

		private static XElement ParseComment(XElement commentElement, IDictionary<string, string> objectNames)
		{
			Debug.Assert(objectNames != null);

			XElement element = new XElement("comment");

			// Note that as for today (2011-03-14) comment id is not returned by BGG XML API 2.

			// First check whether the offer is out of date and set proper attribute if so.
			string text = commentElement.Value;
			bool isOutOfDate = IsOutOfDate(text);
			element.SetAttributeValue("outofdate", isOutOfDate.ToString(Culture.Generic));

			// Then retrieve other data. If the offer is out of date we could skip that but there is
			// not much gain in doing that so lets have full data in case we would ever want it
			// later.

			// Add name of the user that added the comment.
			string userName = commentElement.Attribute("username").Value;
			element.Add(new XElement("username", userName));

			// Note that as for today (2011-03-14) there is no way of associating a game with a
			// comment so we have to use some heuristics to get the game. It is assumed that users
			// will include in the comment link to the game. So look for those links.

			// Match strings "[thing=ID]TEXT[/thing]" where ID is first capture and TEXT is second
			// capture.
			Match match = Regex.Match(text, @"\[thing=(?:(?<1>[^\]]+)\](?<2>[^\]]*))\[/thing\]", RegexOptions.IgnoreCase);
			// Iterate over each game in the comment (in case there is more than one).
			while (match.Success)
			{
				// Game sub-entry.
				XElement gameElement = new XElement("game");

				string objectId = match.Groups[1].Value;
				gameElement.Add(new XElement("objectid", objectId));

				if (!objectNames.ContainsKey(objectId))
				{
					string objectName = QueryName(objectId);
					objectNames.Add(objectId, objectName);
				}
				gameElement.Add(new XElement("objectname", objectNames[objectId]));

				string linkText = match.Groups[2].Value;
				gameElement.Add(new XElement("linktext", linkText));

				string full = match.Value;
				gameElement.Add(new XElement("full", full));

				element.Add(gameElement);

				match = match.NextMatch();
			}

			return element;
		}

		private static bool IsOutOfDate(string text)
		{
			Debug.Assert(text != null);

			string upperCaseText = text.ToUpper(Culture.Native);
			return upperCaseText.Contains("NIEAKTUALNE")
				|| upperCaseText.Contains("NIEAKTUALNA");
		}

		/// <summary>
		/// Returns primary name of the game specified by identifier.
		/// </summary>
		/// <param name="objectId">BGG identifier of the game.</param>
		/// <returns>
		/// <para>Primary name of the game specified by <paramref name="objectId"/>.</para>
		/// </returns>
		private static string QueryName(string objectId)
		{
			System.Threading.Thread.Sleep(500);
			string queryUriFormat = "https://www.boardgamegeek.com/xmlapi2/thing?id={0}";
			string queryUriUnescaped = string.Format(Culture.Generic, queryUriFormat, objectId);
			string queryUri = Uri.EscapeUriString(queryUriUnescaped);

			XDocument document = XDocument.Load(queryUri);
			XElement item = document.Root.Element("item");
			foreach (XElement name in item.Elements("name"))
			{
				if (name.Attribute("type").Value == "primary")
					return name.Attribute("value").Value;
			}
			IEnumerable<XElement> elements = item.Elements("name");
			Debug.Assert(false);
			return "Unknown Game";
		}
	}
}
