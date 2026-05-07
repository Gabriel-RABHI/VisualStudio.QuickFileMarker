using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickFileMarker.Markers
{
    internal class MarkerRecord
    {
        /// <summary>
        /// Absolute path of the marked file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The sellected text in the IDE editor.
        /// </summary>
        public string SellectedText { get; set; }

        /// <summary>
        /// The sellected text, entire line.
        /// </summary>
        public string SellectedTextLine { get; set; }

        /// <summary>
        /// The current carret line number.
        /// </summary>
        public string CarretLine { get; set; }

        /// <summary>
        /// The char number in the carret line.
        /// </summary>
        public string CharPositionInCarretLine { get; set; }

        /// <summary>
        /// Sellection state line, if there is a sellection
        /// </summary>
        public int SellectionStartLine { get; set; } = -1;

        /// <summary>
        /// Sellection state line, if there is a sellection
        /// </summary>
        public int SellectionEndLine { get; set; } = -1;

        public TimeStempRecord TimeStamps { get; set; } = new TimeStempRecord() {
            Year = DateTime.Now.Year,
            Month = DateTime.Now.Month,
            Day = DateTime.Now.Day,
            Hour = DateTime.Now.Hour,
            Minute = DateTime.Now.Minute,
            Second = DateTime.Now.Second
        };
    }

    public class TimeStempRecord
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public int Second { get; set; }
    }
}
