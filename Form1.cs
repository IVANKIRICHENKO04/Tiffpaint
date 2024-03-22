using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace Tiffpaint
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            GdalBase.ConfigureAll();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ���� � GeoTIFF �����
            //string filePath = @"C:\Users\ivan3\Desktop\subimage_1536_1536.tiff";
            string filePath = @"C:\Users\ivan3\Desktop\1_image.tiff";
            // �������� GeoTIFF �����
            Dataset dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
            if (dataset == null)
            {
                Console.WriteLine("�� ������� ������� ����");
                return;
            }

            // ��������� �������� �����������
            int width = dataset.RasterXSize;
            int height = dataset.RasterYSize;

            // ������ ������ ��������
            short[] buffer = new short[width * height * 3]; // 3 ������ (RGB) �� 8 ��� �� �����
            dataset.ReadRaster(0, 0, width, height, buffer, width, height, 3, null, 0, 0, 0);

            // �������� �������� GDAL
            dataset.Dispose();
            short[] red = new short[width * height];
            short[] green = new short[width * height];
            short[] blue = new short[width * height];
            for (int i = 0; i < width*height; i++)
            {
                red[i] = buffer[i];
            }
            for (int i = 0; i < width * height; i++)
            {
                green[i] = buffer[i+ width * height];
            }
            for (int i = 0; i < width * height; i++)
            {
                blue[i] = buffer[i+ width * height*2];
            }
            //// �������� ������� Bitmap
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

            pictureBox1.Image = bitmap;

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
                    if(brightness <300)
                        bitmap2.SetPixel(x, y, gray);
                    else if(brightness <800&&brightness>300)
                        bitmap2.SetPixel(x, y, white);
                    else 
                        bitmap2.SetPixel(x, y, black);
                }
            }

            pictureBox2.Image = bitmap2;

        }
    }
}
