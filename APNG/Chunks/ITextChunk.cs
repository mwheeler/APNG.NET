using System.IO;
using System.Text;

namespace LibAPNG
{
    public interface ITextChunk
    {
        /// <summary>
        /// The chunk's keyword.
        /// 
        /// Standard keywords include:
        ///  * Title            - Short (one line) title or caption for image
        ///  * Author           - Name of image's creator
        ///  * Description      - Description of image (possibly long)
        ///  * Copyright        - Copyright notice
        ///  * Creation Time    - Time of original image creation
        ///  * Software         - Software used to create the image
        ///  * Disclaimer       - Legal disclaimer
        ///  * Warning          - Warning of nature of content
        ///  * Source           - Device used to create the image
        ///  * Comment          - Miscellaneous comment
        /// 
        /// Note: This string is implicitly limited to 80 characters, any extra characters will not be serialized.
        /// </summary>
        string Keyword { get; set; }

        /// <summary>
        /// The text associated w/ the keyword.
        /// </summary>
        string Text { get; set; }
    }
}