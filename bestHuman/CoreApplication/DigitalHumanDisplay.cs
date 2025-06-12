using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CoreApplication
{
    public class DigitalHumanDisplay : PictureBox
    {
        private Color _chromaKeyColor = Color.Green; // 默认抠像颜色为绿色
        private int _tolerance = 30; // 默认颜色容差

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor
        {
            get { return _chromaKeyColor; }
            set { _chromaKeyColor = value; Invalidate(); }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Tolerance
        {
            get { return _tolerance; }
            set { _tolerance = value; Invalidate(); }
        }

        public DigitalHumanDisplay()
        {
            DoubleBuffered = true; // 启用双缓冲，减少闪烁
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent; // 设置背景透明
        }

        // 模拟接收像素流数据并渲染
        public void SetPixelStream(Bitmap pixelData)
        {
            if (pixelData == null) return;

            // 在这里可以进行像素流的预处理，例如缩放等
            Image = ApplyChromaKey(pixelData);
        }

        private Bitmap ApplyChromaKey(Bitmap originalBitmap)
        {
            Bitmap processedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, PixelFormat.Format32bppArgb);

            BitmapData originalData = originalBitmap.LockBits(
                new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            BitmapData processedData = processedBitmap.LockBits(
                new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            int byteCount = originalData.Stride * originalData.Height;
            byte[] originalPixels = new byte[byteCount];
            byte[] processedPixels = new byte[byteCount];

            Marshal.Copy(originalData.Scan0, originalPixels, 0, byteCount);

            int chromaR = _chromaKeyColor.R;
            int chromaG = _chromaKeyColor.G;
            int chromaB = _chromaKeyColor.B;

            for (int i = 0; i < byteCount; i += 4)
            {
                byte b = originalPixels[i];
                byte g = originalPixels[i + 1];
                byte r = originalPixels[i + 2];
                byte a = originalPixels[i + 3]; // Original alpha

                // 计算颜色差异
                int diffR = Math.Abs(r - chromaR);
                int diffG = Math.Abs(g - chromaG);
                int diffB = Math.Abs(b - chromaB);

                // 如果颜色在容差范围内，则设置为透明
                if (diffR <= _tolerance && diffG <= _tolerance && diffB <= _tolerance)
                {
                    processedPixels[i] = 0;     // B
                    processedPixels[i + 1] = 0; // G
                    processedPixels[i + 2] = 0; // R
                    processedPixels[i + 3] = 0; // A (完全透明)
                }
                else
                {
                    processedPixels[i] = b;
                    processedPixels[i + 1] = g;
                    processedPixels[i + 2] = r;
                    processedPixels[i + 3] = a; // Keep original alpha
                }
            }

            Marshal.Copy(processedPixels, 0, processedData.Scan0, byteCount);

            originalBitmap.UnlockBits(originalData);
            processedBitmap.UnlockBits(processedData);

            return processedBitmap;
        }

        // 窗口置顶、点击穿透等功能将通过P/Invoke调用Windows API实现
        // 这些功能通常在主窗口级别进行控制，而不是在PictureBox控件级别
        // 因此，这些方法将作为MainForm的扩展方法或在MainForm中直接实现
    }
}