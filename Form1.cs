using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace Tiffpaint
{
    public partial class Form1 : Form
    {
        short[] red;
        short[] green;
        short[] blue;
        int width;
        int height;

        private Point previousPoint;
        private bool isDrawing = false;
        private Bitmap drawingBitmap;



        public Form1()
        {
            InitializeComponent();
            GdalBase.ConfigureAll();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadImage(@"C:\Users\ivan3\Desktop\subimage_1536_1536.tiff");
            PrintOriginalImage(pictureBox1);
            PrintSelectingAreasImage(pictureBox2);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            //drawingBitmap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            //pictureBox2.Image = drawingBitmap;
            //ClearDrawing();
        }

        /// <summary>
        /// ��������� � ������� �������� ��������
        /// </summary>
        /// <param name="filePath">���� � �����������</param>
        public void LoadImage(string filePath)
        {
            Dataset dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset == null)
            {
                Console.WriteLine("�� ������� ������� ����");
                return;
            }

            // ��������� �������� �����������
            width = dataset.RasterXSize;
            height = dataset.RasterYSize;

            // ������ ������ ��������
            short[] buffer = new short[width * height * 3]; // 3 ������ (RGB) �� 8 ��� �� �����
            dataset.ReadRaster(0, 0, width, height, buffer, width, height, 3, null, 0, 0, 0);

            // �������� �������� GDAL
            dataset.Dispose();
            red = new short[width * height];
            green = new short[width * height];
            blue = new short[width * height];
            for (int i = 0; i < width * height; i++)
            {
                red[i] = buffer[i];
            }
            for (int i = 0; i < width * height; i++)
            {
                green[i] = buffer[i + width * height];
            }
            for (int i = 0; i < width * height; i++)
            {
                blue[i] = buffer[i + width * height * 2];
            }
        }

        /// <summary>
        /// ������� ������������ �����������
        /// </summary>
        /// <param name="box">���������, ���� ��������</param>
        public void PrintOriginalImage(PictureBox box)
        {
            Bitmap bitmap = new Bitmap(width, height);

            // �������������� �������� �� short � byte (0-255)
            byte[] redBytes = Array.ConvertAll(red, val => (byte)(val / 10)); // ����� �� 256 ����� �������� �������� � �������� 0-255
            byte[] greenBytes = Array.ConvertAll(green, val => (byte)(val / 10));
            byte[] blueBytes = Array.ConvertAll(blue, val => (byte)(val / 10));

            // ���������� ����������� �������
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; // ������ �������� ������� � �������� ������
                    Color color = Color.FromArgb(redBytes[index], greenBytes[index], blueBytes[index]);
                    bitmap.SetPixel(x, y, color);
                }
            }

            box.Image = bitmap;
        }

        /// <summary>
        /// ������� ����������� � ����������� ���������
        /// </summary>
        /// <param name="box">���������, ���� ��������</param>
        public void PrintSelectingAreasImage(PictureBox box)
        {
            // �������� ������� Bitmap
            Bitmap bitmap2 = new Bitmap(width, height);

            Color gray = Color.FromArgb(128, 128, 128);

            // ������ ����
            Color black = Color.Black;

            // ����� ����
            Color white = Color.White;
            // ���������� ����������� �������
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; // ������ �������� ������� � �������� ������
                    int brightness = (red[index] + green[index] + blue[index]) / 3;
                    if (brightness < 300)
                        bitmap2.SetPixel(x, y, gray);
                    else if (brightness < 800 && brightness > 300)
                        bitmap2.SetPixel(x, y, white);
                    else
                        bitmap2.SetPixel(x, y, black);
                }
            }

            box.Image = bitmap2;
            drawingBitmap = bitmap2;
        }




        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            previousPoint = e.Location;
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                using (Graphics g = Graphics.FromImage(drawingBitmap))
                {
                    Pen pen = new Pen(Color.Black, 2); // ��������� ���� � ������� �����
                    g.DrawLine(pen, previousPoint, e.Location);
                }
                pictureBox2.Invalidate(); // ���������� PictureBox
                previousPoint = e.Location;
            }
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private void ClearDrawing()
        {
            using (Graphics g = Graphics.FromImage(drawingBitmap))
            {
                g.Clear(Color.White);
            }
            pictureBox2.Invalidate();
        }
    }
}
