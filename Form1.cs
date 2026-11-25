using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PNG2ICO
{
    public partial class Form1 : Form
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaCaptionColor = 35;
        private const int DwmwaTextColor = 36;
        private readonly List<Image> controlImages = new();

        private PictureBox previewBox;
        private Button selectBtn, convertBtn, batchConvertBtn, previousImageBtn, nextImageBtn, settingsBtn;
        private readonly ContextMenuStrip settingsMenu;
        private readonly ToolStripMenuItem themeMenuItem;
        private string pngPath = "";
        private string[] previewPaths = Array.Empty<string>();
        private int previewIndex;
        private bool isDarkMode;
        private const int MaxSize = 256; // Windows 光标最大尺寸

        public Form1()
        {
            Text = "[PNG to ICO Converter v1.0]";
            BackgroundImage = LoadControlImage("bg.png");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(350, 300);
            Padding = new Padding(12);
            themeMenuItem = new ToolStripMenuItem("Switch to Dark Mode");
            themeMenuItem.Click += (_, _) => SetDarkMode(!isDarkMode);
            settingsMenu = new ContextMenuStrip();
            settingsMenu.Items.Add(themeMenuItem);
            Disposed += (_, _) =>
            {
                settingsMenu.Dispose();
                foreach (Image image in controlImages)
                {
                    image.Dispose();
                }
            };

            selectBtn = new Button
            {
                Text = "Select PNG image",
                Location = new Point(12, 12),
                Width = 100,
                BackgroundImage = LoadControlImage("selectBtn.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.Black
            };
            selectBtn.FlatAppearance.BorderSize = 0;
            selectBtn.Click += SelectPng;

            previewBox = new PictureBox { Location = new Point(12, selectBtn.Bottom + 12), Size = new Size(160, 160),
                                        BorderStyle = BorderStyle.None, SizeMode = PictureBoxSizeMode.Zoom,
                                        BackgroundImage = LoadControlImage("previewBox.png"), BackgroundImageLayout = ImageLayout.Stretch };
            previewBox.Paint += (_, e) =>
            {
                Color borderColor = isDarkMode ? Color.FromArgb(100, 100, 100) : Color.FromArgb(80, 80, 80);
                ControlPaint.DrawBorder(e.Graphics, previewBox.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
            };

            previousImageBtn = new Button
            {
                Text = "↑",
                Location = new Point(180, 82),
                Size = new Size(32, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(148, 148, 148),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            previousImageBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            previousImageBtn.Click += PreviousImageClick;

            nextImageBtn = new Button
            {
                Text = "↓",
                Location = new Point(180, 122),
                Size = new Size(32, 32),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.FromArgb(148, 148, 148),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            nextImageBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            nextImageBtn.Click += NextImageClick;

            convertBtn = new BlackTextButton
            {
                Text = "Convert to ICO file",
                Location = new Point(210, 125),
                AutoSize = false,
                Size = new Size(125, 30),
                BackgroundImage = LoadControlImage("convertToICOfile.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.Black
            };
            convertBtn.FlatAppearance.BorderSize = 0;
            convertBtn.Click += ConvertClick;
            convertBtn.Enabled = false;

            batchConvertBtn = new Button
            {
                Text = "Multi convert",
                Location = new Point(selectBtn.Right + 12, 12),
                Width = 110,
                BackgroundImage = LoadControlImage("multiConvert.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                ForeColor = Color.Black
            };
            batchConvertBtn.FlatAppearance.BorderSize = 0;
            batchConvertBtn.Click += SelectImagesForPreview;

            settingsBtn = new Button
            {
                Text = "⚙",
                Location = new Point(ClientSize.Width - Padding.Right - 25, 12),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.Black
            };
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.ContextMenuStrip = settingsMenu;
            settingsBtn.Click += (_, _) => settingsMenu.Show(
                settingsBtn,
                new Point(settingsBtn.Width, settingsBtn.Height),
                ToolStripDropDownDirection.BelowLeft);

            convertBtn.Location = new Point(previewBox.Left, previewBox.Bottom + 12);

            Controls.AddRange(new Control[] { selectBtn, batchConvertBtn, settingsBtn, previewBox, previousImageBtn, nextImageBtn,
                convertBtn });
            UpdateNavigationButtons();
            UpdateThemeColors();
        }

        private sealed class BlackTextButton : Button
        {
            public BlackTextButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (BackgroundImage != null)
                {
                    e.Graphics.DrawImage(BackgroundImage, ClientRectangle);
                }
                else
                {
                    e.Graphics.Clear(BackColor);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    ClientRectangle,
                    Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

                if (Focused && ShowFocusCues)
                {
                    ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -3, -3));
                }
            }
        }

        private Image LoadControlImage(string fileName)
        {
            string imagePath = Path.Combine(AppContext.BaseDirectory, "imgs", fileName);
            using Image source = Image.FromFile(imagePath);
            Image image = new Bitmap(source);
            controlImages.Add(image);
            return image;
        }

        private void SetDarkMode(bool enabled)
        {
            isDarkMode = enabled;
            SetThemedImage(this, "bg.png");
            SetThemedImage(selectBtn, "selectBtn.png");
            SetThemedImage(batchConvertBtn, "multiConvert.png");
            SetThemedImage(previewBox, "previewBox.png");
            SetThemedImage(convertBtn, "convertToICOfile.png");
            themeMenuItem.Text = isDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";
            UpdateThemeColors();
            UpdateTitleBarTheme();
        }

        private void UpdateThemeColors()
        {
            Color buttonBackColor = isDarkMode
                ? Color.FromArgb(64, 64, 64)
                : Color.FromArgb(240, 240, 240);
            Color buttonBorderColor = isDarkMode
                ? Color.FromArgb(100, 100, 100)
                : Color.FromArgb(180, 180, 180);
            Color buttonHoverColor = isDarkMode
                ? Color.FromArgb(80, 80, 80)
                : Color.FromArgb(225, 225, 225);
            Color buttonPressedColor = isDarkMode
                ? Color.FromArgb(48, 48, 48)
                : Color.FromArgb(205, 205, 205);
            Color inputForeColor = isDarkMode ? Color.White : Color.Black;

            foreach (Button button in new[] { previousImageBtn, nextImageBtn })
            {
                button.BackColor = buttonBackColor;
                button.ForeColor = inputForeColor;
                button.FlatAppearance.BorderColor = buttonBorderColor;
                button.FlatAppearance.MouseOverBackColor = buttonHoverColor;
                button.FlatAppearance.MouseDownBackColor = buttonPressedColor;
            }

            previewBox.BackColor = isDarkMode ? Color.FromArgb(32, 32, 32) : Color.White;
            previewBox.Invalidate();
            settingsBtn.BackColor = buttonBackColor;
            settingsBtn.ForeColor = inputForeColor;
        }

        private void SetThemedImage(Control control, string lightFileName)
        {
            string imageFileName = lightFileName;
            if (isDarkMode)
            {
                string darkFileName = Path.GetFileNameWithoutExtension(lightFileName) +
                    "_dark" + Path.GetExtension(lightFileName);
                string darkImagePath = Path.Combine(AppContext.BaseDirectory, "imgs", darkFileName);
                if (File.Exists(darkImagePath))
                {
                    imageFileName = darkFileName;
                }
            }

            Image newImage = LoadControlImage(imageFileName);
            Image? previousImage = control.BackgroundImage;
            control.BackgroundImage = newImage;
            if (previousImage != null)
            {
                controlImages.Remove(previousImage);
                previousImage.Dispose();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateTitleBarTheme();
        }

        private void UpdateTitleBarTheme()
        {
            int captionColor = isDarkMode ? 0x000000 : 0x00FFFFFF;
            int textColor = isDarkMode ? 0x00FFFFFF : 0x000000;
            int captionResult = DwmSetWindowAttribute(Handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
            int textResult = DwmSetWindowAttribute(Handle, DwmwaTextColor, ref textColor, sizeof(int));
            if (captionResult != 0 || textResult != 0)
            {
                int enabled = isDarkMode ? 1 : 0;
                int result = DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int));
                if (result != 0)
                {
                    DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
                }
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        private void SelectPng(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "PNG Image|*.png" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            previewPaths = new[] { ofd.FileName };
            previewIndex = 0;
            ShowSelectedImage();
        }

        private void ConvertClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pngPath)) return;

            int imageWidth = previewBox.Image?.Width ?? 0;
            int imageHeight = previewBox.Image?.Height ?? 0;

            if (imageWidth > MaxSize || imageHeight > MaxSize)
            {
                MessageBox.Show($"Image size {imageWidth}x{imageHeight} exceeded the maximum allowed size {MaxSize}x{MaxSize}! Please try again after resizing the image.", "[Size exceeded the limit]");
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "ICO File|*.ico", FileName = Path.GetFileNameWithoutExtension(pngPath) + ".ico" };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                PngToIcoConverter.Convert(pngPath, sfd.FileName);
                MessageBox.Show("Successfully converted!", "[Success]");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to convert: " + ex.Message, "[Error]");
            }
        }

        private RectangleF GetPreviewImageBounds(Image image)
        {
            float scale = Math.Min(
                (float)previewBox.ClientSize.Width / image.Width,
                (float)previewBox.ClientSize.Height / image.Height);
            float width = image.Width * scale;
            float height = image.Height * scale;
            return new RectangleF(
                (previewBox.ClientSize.Width - width) / 2,
                (previewBox.ClientSize.Height - height) / 2,
                width,
                height);
        }

        private void SelectImagesForPreview(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "PNG Image|*.png",
                Multiselect = true,
                Title = "Select PNG images to preview"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            previewPaths = ofd.FileNames;
            previewIndex = 0;
            ShowSelectedImage();
        }

        private void PreviousImageClick(object? sender, EventArgs e)
        {
            ChangePreview(-1);
        }

        private void NextImageClick(object? sender, EventArgs e)
        {
            ChangePreview(1);
        }

        private void ChangePreview(int direction)
        {
            if (previewPaths.Length < 2) return;

            previewIndex = (previewIndex + direction + previewPaths.Length) % previewPaths.Length;
            ShowSelectedImage();
        }

        private void ShowSelectedImage()
        {
            UpdateNavigationButtons();
            if (previewPaths.Length == 0)
            {
                pngPath = "";
                previewBox.Image?.Dispose();
                previewBox.Image = null;
                convertBtn.Enabled = false;
                return;
            }

            pngPath = previewPaths[previewIndex];
            try
            {
                using var image = Image.FromFile(pngPath);
                Image previewImage = new Bitmap(image);
                Image? previousImage = previewBox.Image;
                previewBox.Image = previewImage;
                previousImage?.Dispose();

                bool withinLimit = image.Width <= MaxSize && image.Height <= MaxSize;
                convertBtn.Enabled = withinLimit;
                if (!withinLimit)
                {
                    MessageBox.Show($"Image size {image.Width}x{image.Height} exceeded the maximum allowed size {MaxSize}x{MaxSize}! Please try again after resizing the image.", "[Size exceeded the limit]");
                }
            }
            catch (Exception ex)
            {
                pngPath = "";
                previewBox.Image?.Dispose();
                previewBox.Image = null;
                convertBtn.Enabled = false;
                MessageBox.Show("Error loading image: " + ex.Message, "[Error]");
            }
        }

        private void UpdateNavigationButtons()
        {
            bool canNavigate = previewPaths.Length > 1;
            previousImageBtn.Enabled = canNavigate;
            nextImageBtn.Enabled = canNavigate;
        }
    }


    static class PngToIcoConverter
    {
        public static void Convert(string pngPath, string icoPath)
        {
            using var bmp = new Bitmap(pngPath);
            int w = bmp.Width, h = bmp.Height;

            // 锁定像素
            var rect = new Rectangle(0, 0, w, h);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = data.Stride;
            byte[] pixels = new byte[stride * h];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);

            // DIB header (BITMAPINFOHEADER)
            int dibSize = 40;
            int xorStride = ((w * 32 + 31) / 32) * 4;      // XOR行对齐到4字节
            int andStride = ((w + 31) / 32) * 4;            // AND行对齐到4字节
            int xorSize = xorStride * h;
            int andSize = andStride * h;

            byte[] dib = new byte[dibSize + xorSize + andSize];

            // BITMAPINFOHEADER
            BitConverter.GetBytes(dibSize).CopyTo(dib, 0);          // biSize
            BitConverter.GetBytes(w).CopyTo(dib, 4);                 // biWidth
            BitConverter.GetBytes(h * 2).CopyTo(dib, 8);              // biHeight (包含XOR图像和AND掩码)
            BitConverter.GetBytes((short)1).CopyTo(dib, 12);         // biPlanes
            BitConverter.GetBytes((short)32).CopyTo(dib, 14);        // biBitCount
            BitConverter.GetBytes(0).CopyTo(dib, 16);                // biCompression (BI_RGB)

            // XOR数据 (BGRA -> 写入DIB)
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * stride;
                int dstRow = (h - 1 - y) * xorStride; // BMP存储倒序
                for (int x = 0; x < w; x++)
                {
                    int si = srcRow + x * 4;
                    int di = dibSize + dstRow + x * 4;
                    // BGRA -> BGRA (无需交换)
                    dib[di] = pixels[si];       // B
                    dib[di+1] = pixels[si+1];   // G
                    dib[di+2] = pixels[si+2];   // R
                    dib[di+3] = pixels[si+3];   // A
                }
            }

            // AND掩码 (透明度: alpha<128 视为透明，对应掩码位=1)
            int andOffset = dibSize + xorSize;
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * stride;
                int dstRow = (h - 1 - y) * andStride;
                for (int x = 0; x < w; x++)
                {
                    byte alpha = pixels[srcRow + x * 4 + 3];
                    if (alpha < 128)
                    {
                        int byteIdx = andOffset + dstRow + (x / 8);
                        int bitIdx = 7 - (x % 8);
                        dib[byteIdx] |= (byte)(1 << bitIdx);
                    }
                }
            }

            // 构造ICO文件
            using var fs = new FileStream(icoPath, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            // ICO header
            bw.Write((short)0);     // reserved
            bw.Write((short)1);     // type=1 图标
            bw.Write((short)1);     // count=1个图像

            // 目录项
            int imageSize = dibSize + xorSize + andSize;
            bw.Write((byte)(w == 256 ? 0 : w));     // width (0表示256)
            bw.Write((byte)(h == 256 ? 0 : h));     // height
            bw.Write((byte)0);                      // color palette count
            bw.Write((byte)0);                      // reserved
            bw.Write((short)0);                     // color planes
            bw.Write((short)32);                    // bits per pixel
            bw.Write(imageSize);                    // bytes in resource
            bw.Write(22);                           // offset from start (header 6 + dir entry 16 = 22)

            // 写入DIB数据
            bw.Write(dib);
        }
    }
}

/// ■       ■   ■ ■ ■ ■             ■ ■ ■ ■     ■ ■ ■ ■ ■   ■ ■ ■ ■     ■ ■ ■ ■         ■       ■ ■ ■ ■ ■   ■ ■ ■ ■ ■
///   ■   ■     ■       ■           ■       ■   ■           ■       ■   ■       ■     ■   ■         ■       ■
///     ■       ■ ■ ■ ■     ■ ■ ■   ■ ■ ■ ■     ■ ■ ■ ■     ■       ■   ■ ■ ■ ■     ■ ■ ■ ■ ■       ■       ■ ■ ■ ■
///   ■   ■     ■       ■           ■       ■   ■           ■       ■   ■       ■   ■       ■       ■       ■
/// ■       ■   ■       ■           ■       ■   ■ ■ ■ ■ ■   ■ ■ ■ ■     ■       ■   ■       ■       ■       ■ ■ ■ ■ ■