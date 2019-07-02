using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Starwatch.Entities
{
    /// <summary>
    /// TaggedText is a formatted version of text received from Starbound. This text will strip the colour tags away from the text and store them. 
    /// </summary>
    public class TaggedText
    {
        /// <summary>
        /// This is the Regex used to seperate the tags from the Starbound text. 
        /// </summary>
        public static readonly Regex TagRegex = new Regex("\\^(.*?)\\;", RegexOptions.Compiled);

        /// <summary>
        /// The text straight from starbound
        /// </summary>
        public string TaggedContent { get; }

        /// <summary>
        /// The text from starbound with its colour tags removed.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Does this text contain any tags? If this is false, the TaggedContent and Content will be exactly the same.
        /// </summary>
        public bool IsTagged { get; }

        /// <summary>
        /// Creates a new instance of the TaggedText, parsing the stripping the tags.
        /// </summary>
        /// <param name="text">The raw text received from Starbound.</param>
        public TaggedText(string text)
        {
            this.TaggedContent = text.Trim();
            this.Content = StripColourTags(this.TaggedContent);
            this.IsTagged = !this.Content.Equals(this.TaggedContent);
        }

        /// <summary>
        /// Turns the object into a string without the tags.
        /// </summary>
        /// <returns>Content without tags</returns>
        public override string ToString()
        {
            return this.Content;
        }

        /// <summary>
        /// Implicidly casts a TaggedText into a string. This way it will not be required to use ToString() or do a cast.
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator string(TaggedText text)
        {
            return text.ToString();
        }

        public static implicit operator TaggedText(string text)
        {
            return new TaggedText(text);
        }

        /// <summary>
        /// Strips colour tags from the given text
        /// </summary>
        /// <param name="text">Text to strip tags from</param>
        /// <returns>Tagless text</returns>
        public static string StripColourTags(string text)
        {
            string cname = text;

            MatchCollection matches = TagRegex.Matches(cname);
            for (int ctr = 0; ctr < matches.Count; ctr++)
            {
                cname = cname.Replace(matches[ctr].Value, "");
            }

            return cname.Trim();
        }
    }
}
