/** @file MainWindow.xaml.cs
 *  @brief 產生 PDF 檔案

 *  這個頁面將 ListView 上的檔案全都整合成一個 PDF 檔案，尺寸固定為 A4 = 210 × 297mm。

 *  @author Shu-Kai Yang (skyang@csie.nctu.edu.tw)
 *  @date 2023/7/24 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Dialogs = System.Windows.Forms;

using iTextSharp.text;
using iTextSharp.text.pdf;
using Imgs2Pdf.Properties;
using MetadataExtractor;

namespace Imgs2Pdf
{
    public partial class MainWindow : Window
    {
        /// 影像檔案的集合。
        private ObservableCollection<ImgFileItem> FileItems = new ObservableCollection<ImgFileItem>();

        /// 關於輸出 PDF 檔案的選項:
        private Boolean IsPortrait = false;
        private Boolean AutoRotate = false;
        private const float PdfMargin = 16;

        #region Window size and position.
        public MainWindow()
        {
            InitializeComponent();
            FileItemListView.ItemsSource = FileItems;

            /// 載入 PDF 檔案的選項:
            IsPortrait = Settings.Default.Portrait;
            AutoRotate = Settings.Default.RotateImgs;

            /// 載入視窗位置與尺寸:
            if (Settings.Default.WindowPos != null)
            {
                this.Left = Settings.Default.WindowPos.X;
                this.Top = Settings.Default.WindowPos.Y;
            }

            if (Settings.Default.WindowSize != null)
            {
                this.Width = Settings.Default.WindowSize.Width;
                this.Height = Settings.Default.WindowSize.Height;
            }

            /// 初始化頁面上的控制項:
            if (IsPortrait) {  PdfOptPortrait.IsChecked = true;  }
            else {  PdfOptLandscape.IsChecked = true;  }
            PdfOptRotate.IsChecked = AutoRotate;
        }

        /// <summary>
        /// 紀錄視窗位置與尺寸。
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (PdfOptPortrait.IsChecked == true)  {  IsPortrait = true;  }
            if (PdfOptLandscape.IsChecked == true) {  IsPortrait = false;  }
            if (PdfOptRotate.IsChecked == true) {  AutoRotate = true;  } else {  AutoRotate = false;  }

            Settings.Default.Portrait = IsPortrait;
            Settings.Default.RotateImgs = AutoRotate;

            if (this.WindowState == WindowState.Normal)
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
            }
            else
            {
                Settings.Default.WindowPos = new System.Drawing.Point((int)RestoreBounds.Left, (int)RestoreBounds.Top);
                Settings.Default.WindowSize = new System.Drawing.Size((int)RestoreBounds.Size.Width, (int)RestoreBounds.Size.Height);
            }

            Settings.Default.Save();
        }
        #endregion

        #region Drag and drop the .jpg/.png files.
        /// <summary>
        ///  允許接受檔案拖曳。
        /// </summary>
        private void FileItemListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {                
                String[] files = e.Data.GetData(DataFormats.FileDrop, true) as String[];
                if (files.Length > 0)
                {
                    Array.Sort(files, StringComparer.InvariantCultureIgnoreCase);

                    foreach (String pathName in files)
                    {
                        String fileExt = Path.GetExtension(pathName).ToLower();
                        if (fileExt.Equals(".jpg") || fileExt.Equals(".jpeg") || fileExt.Equals(".png"))
                        {
                            ImgFileItem item = new ImgFileItem();
                            item.PathName = pathName;
                            item.FileName = Path.GetFileName(pathName);
                            item.Orientation = 0;

                            /// 地方的 JPEG 檔案需要立即的 EXIF 資訊:
                            if (fileExt.Equals(".jpg") || fileExt.Equals(".jpeg"))
                            {
                                var dirs = ImageMetadataReader.ReadMetadata(pathName);

                                foreach (var dir in dirs)
                                {
                                    try
                                    {
                                        int value = dir.GetInt32(0x0112); /* Orientation */
                                        item.Orientation = value;
                                        break;
                                    }
                                    catch { }
                                }
                            }

                            FileItems.Add(item);
                        }
                    }
                } 
            }
        }
        #endregion

        #region Remove file items.
        /// <summary>
        ///  清除所有檔案。
        /// </summary>
        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {   FileItems.Clear();  }

        /// <summary>
        ///  移除所選的所有檔案。
        /// </summary>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileItemListView.SelectedItems.Count > 0)
            {
                List<ImgFileItem> items = new List<ImgFileItem>();
                foreach (ImgFileItem item in FileItemListView.SelectedItems)
                {   items.Add(item);  }

                if (items.Count > 0)
                {
                    foreach (ImgFileItem item in items)
                    {  FileItems.Remove(item);  }
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {   RemoveButton_Click(null, null);  }
        }
        #endregion

        /// <summary>
        ///  進行 PDF 檔案之轉換。
        /// </summary>
        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (PdfOptPortrait.IsChecked == true)  {  IsPortrait = true;  }
            if (PdfOptLandscape.IsChecked == true) {  IsPortrait = false;  }
            if (PdfOptRotate.IsChecked == true) {  AutoRotate = true;  } else {  AutoRotate = false;  }
            if (FileItems.Count == 0) {  return; }

            Dialogs.SaveFileDialog dialog = new Dialogs.SaveFileDialog();
            dialog.Filter = "Portable Document Files (*.pdf)|*.pdf";
            dialog.Title = Properties.Resources.PdfDialogTitle;
         /* dialog.FileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".album.pdf"; */
            dialog.FileName = DateTime.Now.ToString("yyyy-MM-dd") + ".album.pdf";
            dialog.DefaultExt = "pdf";
            dialog.OverwritePrompt = true;

            Dialogs.DialogResult result = dialog.ShowDialog();
            if (result == Dialogs.DialogResult.OK)
            {
                MessageLabel.Text = Properties.Resources.PdfOutputStart;

                /// 建立一個檔案串流，並且以此建立一個新的 PDF Document 物件:
                FileStream fs = null;
                Document doc = null;
                try
                {   fs= new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);  }
                catch (IOException ex) {  MessageLabel.Text = ex.Message;  return;  }

                var pageSize = PageSize.A4;
                if (IsPortrait == false) {  pageSize = PageSize.A4.Rotate();  }

                try {  doc = new Document(pageSize, PdfMargin, PdfMargin, PdfMargin, PdfMargin);  }
                catch (Exception ex)
                {   MessageLabel.Text = ex.Message;  return;  }

                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                /// 計算扣除邊界(margin)之後的可用頁面範圍:
                float canvasWidth = doc.PageSize.Width - (PdfMargin * 2);
                float canvasHeight = doc.PageSize.Height - (PdfMargin * 2);

                /// 將影像檔案清單中的每個影像旋轉到正確的角度:
                foreach (ImgFileItem item in FileItems)
                {
                    Image img = Image.GetInstance(item.PathName);
                    float rawWidth = img.PlainWidth;
                    float rawHeight = img.PlainHeight;
                    bool imgIsSquare = false;
                    bool imgIsPortrait = false;
                    if (rawHeight == rawWidth) {  imgIsSquare = true; }
                    if (rawHeight > rawWidth)  {  imgIsPortrait = true;  }

                    /// 先根據 EXIF 資訊將影像旋轉到正確的角度:
                    float rotationDegrees = 0.0f;
                    if ((item.Orientation == 3) || (item.Orientation == 4))
                    {   rotationDegrees = 180.0f;  }

                    if ((item.Orientation == 5) || (item.Orientation == 6))
                    {   rotationDegrees = -90.0f;  imgIsPortrait = !imgIsPortrait;  }

                    if ((item.Orientation == 7) || (item.Orientation == 8))
                    {   rotationDegrees = 90.0f;   imgIsPortrait = !imgIsPortrait;  }

                    /// 再執行自動旋轉(如果 imgIsSquare 為 true 則不需要旋轉):
                    if ((AutoRotate == true) && (imgIsSquare == false))
                    {
                        if (IsPortrait != imgIsPortrait)
                        {   rotationDegrees += 90.0f;  }
                    }

                    if (rotationDegrees != 0.0f)
                    {   img.RotationDegrees = rotationDegrees;  }

                    img.Alignment = Image.ALIGN_CENTER;
                    img.ScaleToFit(canvasWidth, canvasHeight);
                    img.SetAbsolutePosition((pageSize.Width - img.ScaledWidth) / 2, (pageSize.Height - img.ScaledHeight) / 2);
                    doc.NewPage();
                    doc.Add(img);
                }

                /// 把記憶體中的 PDF Document 寫入檔案中:
                PdfDestination dest = new PdfDestination(PdfDestination.FIT);
                PdfAction action = PdfAction.GotoLocalPage(1, dest, writer);
                writer.SetOpenAction(action);
                doc.Close();

                MessageLabel.Text = Properties.Resources.PdfOutputDone + " " + dialog.FileName;
            }
        }

        /// <summary>
        ///  前往官方網站。
        /// </summary>
        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(HyperlinkButton.Content.ToString());
        }
    }
}
