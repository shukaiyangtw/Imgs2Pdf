/** @file ImgFileItem.cs
 *  @brief 影像檔案項目

 *  這個類別包裝了要放在 ListView 中讓使用者拖曳的影像檔案項目。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2021/6/1 */

using System;

namespace Imgs2Pdf
{
    class ImgFileItem
    {
        public String FileName { get; set; }
        public String PathName { get; set; }
        public Int32 Orientation { get; set; }
    }
}
