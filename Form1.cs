using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using System.Drawing.Imaging;


namespace Tiffpaint
{
    public partial class Form1 : Form
    {
        //�����������
        short[] red;
        short[] green;
        short[] blue;
        int width;
        int height;

        //���������
        private Point previousPoint;
        private bool isDrawing = false;
        private Bitmap drawingBitmap;
        private Bitmap OriginalImage;
        Pen pen;
        Color CurrentColor;


        public Form1()
        {
            InitializeComponent();
            GdalBase.ConfigureAll();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializePen();
            Graytxt.Text = "300";
            Whitetxt.Text = "800";
            pictureBox2.Visible = false;
            pictureBox2.MouseWheel += pictureBox_MouseWheel;
            pictureBox1.MouseWheel += pictureBox_MouseWheel;
            pictureBox3.MouseWheel += pictureBox_MouseWheel;
        }

        /// <summary>
        /// ��������� � ������� �������� ��������
        /// </summary>
        public void LoadImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "�����������|*.tif;*.tiff;*.jpg;*.jpeg;*.png;*.bmp|��� �����|*.*";
            openFileDialog.Title = "�������� �����������";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                Dataset dataset = Gdal.Open(filePath, Access.GA_ReadOnly);
                if (dataset == null)
                {
                    MessageBox.Show("�� ������� ������� ����", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                width = dataset.RasterXSize;
                height = dataset.RasterYSize;

                short[] buffer = new short[width * height * 3];
                dataset.ReadRaster(0, 0, width, height, buffer, width, height, 3, null, 0, 0, 0);

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
        }

        /// <summary>
        /// ������� ������������ �����������
        /// </summary>
        /// <param name="box">���������, ���� ��������</param>
        public void PrintOriginalImage(PictureBox box)
        {
            OriginalImage = new Bitmap(width, height);

            // �������������� �������� �� short � byte (0-255)
            byte[] redBytes = Array.ConvertAll(red, val => (byte)(val / 7)); // ����� �� 256 ����� �������� �������� � �������� 0-255
            byte[] greenBytes = Array.ConvertAll(green, val => (byte)(val / 7));
            byte[] blueBytes = Array.ConvertAll(blue, val => (byte)(val / 7));

            // ���������� ����������� �������
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; // ������ �������� ������� � �������� ������
                    Color color = Color.FromArgb(redBytes[index], greenBytes[index], blueBytes[index]);
                    OriginalImage.SetPixel(x, y, color);
                }
            }
            box.Image = OriginalImage;
        }

        /// <summary>
        /// ������� ����������� � ����������� ���������
        /// </summary>
        /// <param name="box">���������, ���� ��������</param>
        public void PrintSelectingAreasImage(PictureBox box, int grayThreshold, int whiteThreshold) 
        {
            // �������� ������� Bitmap
            drawingBitmap = new Bitmap(width, height);
            Color gray = Color.FromArgb(128, 128, 128);
            Color black = Color.Black;
            Color white = Color.White;
            // ���������� ����������� �������
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x; // ������ �������� ������� � �������� ������
                    int brightness = (red[index] + green[index] + blue[index]) / 3;
                    if (brightness < grayThreshold)
                        drawingBitmap.SetPixel(x, y, gray);
                    else if (brightness < whiteThreshold && brightness > grayThreshold)
                        drawingBitmap.SetPixel(x, y, white);
                    else
                        drawingBitmap.SetPixel(x, y, black);
                }
            }
            box.Image = drawingBitmap;
            pictureBox3.Image = drawingBitmap;
        }

        //=================================================== ���������

        /// <summary>
        /// ������� ������� ���� ��� ���������
        /// </summary>
        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            if (pictureBox2.SizeMode == PictureBoxSizeMode.Zoom)
            {
                previousPoint = PictureBoxToBitmapCoordinates(pictureBox2, e.Location);
            }
            else
                previousPoint = e.Location;
        }

        /// <summary>
        /// ��������� ������� ������������ ���� ��� ���������
        /// </summary>
        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                // �������������� ��������� ���� �� PictureBox � ���������� Bitmap
                Point bitmapLocation;
                if (pictureBox2.SizeMode == PictureBoxSizeMode.Zoom)
                {
                    bitmapLocation = PictureBoxToBitmapCoordinates(pictureBox2, e.Location);
                }
                else
                    bitmapLocation = e.Location;

                using (Graphics g = Graphics.FromImage(drawingBitmap))
                {
                    // ���������� ��������� ����
                    Point roundedPreviousPoint = new Point((int)Math.Round((double)previousPoint.X), (int)Math.Round((double)previousPoint.Y));
                    Point roundedBitmapLocation = new Point((int)Math.Round((double)bitmapLocation.X), (int)Math.Round((double)bitmapLocation.Y));

                    g.DrawLine(pen, roundedPreviousPoint, roundedBitmapLocation);
                }
                pictureBox2.Invalidate(); // ���������� PictureBox
                pictureBox3.Invalidate(); // ���������� PictureBox
                previousPoint = bitmapLocation;
            }
        }

        /// <summary>
        /// ���������� ������� ���������� ���� ��� ���������
        /// </summary>
        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        /// <summary>
        /// ����� ��� �������������� ��������� ���� �� PictureBox � ���������� Bitmap
        /// </summary>
        private Point PictureBoxToBitmapCoordinates(PictureBox pictureBox, Point pictureBoxLocation)
        {
            float scaleX = (float)drawingBitmap.Width / pictureBox.ClientSize.Width;
            float scaleY = (float)drawingBitmap.Height / pictureBox.ClientSize.Height;

            // ��������� �������� PictureBox ��� ���������������
            int bitmapX = (int)(pictureBoxLocation.X * scaleX);
            int bitmapY = (int)(pictureBoxLocation.Y * scaleY);

            return new Point(bitmapX, bitmapY);
        }

        /// <summary>
        /// ���������� ������ ��������� �����
        /// </summary>
        private void SaveBtn_Click(object sender, EventArgs e)
        {
            SaveAsGeoTiff(drawingBitmap, @"C:\Users\ivan3\Desktop\subimage_1536_1536_mask.tiff");
        }

        /// <summary>
        /// ������� ���������� �������� �����������
        /// </summary>
        /// <param name="bitmap">�����������</param>
        /// <param name="outputPath">���� ����������</param>
        public static void SaveAsGeoTiff(Bitmap bitmap, string outputPath)
        {
            Gdal.AllRegister();
            Dataset ds = Gdal.GetDriverByName("GTiff").Create(outputPath, bitmap.Width, bitmap.Height, 1, DataType.GDT_UInt16, null);

            // Write bitmap data to the GeoTIFF dataset
            BitmapDataToRaster(bitmap, ds);

            // Close the dataset
            ds.Dispose();
        }

        /// <summary>
        /// ������� ����������� ������ �� bitmap � dataset
        /// </summary>
        private static void BitmapDataToRaster(Bitmap bitmap, Dataset ds)
        {
            // ���������� ����������� ��� ��������� ������ � ������� BitmapData
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                int stride = data.Stride;
                byte[] buffer = new byte[stride * bitmap.Height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                // �������� �� ������� ������� �����������
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int index = y * stride + x * 3; // ������ �������� ������� � ������� buffer

                        // ��������� �������� �������� ������ �������� �������
                        byte redValue = buffer[index]; // ������� ����� (������ 2 � ������� BGR)

                        // ������ �������� �������� ������ ������� � �����
                        ds.GetRasterBand(1).WriteRaster(x, y, 1, 1, new byte[] { redValue }, 1, 1, 1, 1);
                    }
                }
            }
            finally
            {
                // ������������� ������ �����������
                bitmap.UnlockBits(data);
            }
        }

        /// <summary>
        /// ���������� ����������� ������� � ��������� ���� ���-���� ����� ����
        /// </summary>
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // ������������� ���� �������
            }
        }

        /// <summary>
        /// ������������� ����� ��� ���������
        /// </summary>
        private void InitializePen()
        {
            txtSize.Text = "2";
            BlackRd.Checked = true;
        }

        /// <summary>
        /// ����� ������ ������� �����
        /// </summary>
        private void BlackRd_CheckedChanged(object sender, EventArgs e)
        {
            if (BlackRd.Checked)
            {
                pen = new Pen(Color.Black, Convert.ToInt32(txtSize.Text));
                CurrentColor = Color.Black;
            }
        }

        /// <summary>
        /// ����� ������ ������ �����
        /// </summary>
        private void WhiteRb_CheckedChanged(object sender, EventArgs e)
        {
            if (WhiteRb.Checked)
            {
                CurrentColor = Color.White;
                pen = new Pen(Color.White, Convert.ToInt32(txtSize.Text));
            }
        }

        /// <summary>
        /// ����� ������ ������ �����
        /// </summary>
        private void GrayRb_CheckedChanged(object sender, EventArgs e)
        {
            if (GrayRb.Checked)
            {
                pen = new Pen(Color.Gray, Convert.ToInt32(txtSize.Text));
                CurrentColor = Color.Gray;
            }

        }

        /// <summary>
        /// ���������� ������� ��������� ���������� ����
        /// </summary>
        private void txtSize_TextChanged(object sender, EventArgs e)
        {
            pen = new Pen(CurrentColor, Convert.ToInt32(txtSize.Text));
        }

        /// <summary>
        /// ���������� ��������� ������ ���� ��� ���������� ����������� zoom
        /// </summary>
        private void pictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox box)
            {
                if (e.Delta > 0)
                {
                    // ���������� �������� ��� ��������� �������� �����
                    if (box.SizeMode != PictureBoxSizeMode.Zoom)
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
                        pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
                        //previousPoint = PictureBoxToBitmapCoordinates(pictureBox2, previousPoint);
                    }


                }
                else
                {
                    // ���������� �������� ��� ��������� �������� ����
                    if (box.SizeMode != PictureBoxSizeMode.Normal)
                    {
                        pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                        pictureBox2.SizeMode = PictureBoxSizeMode.Normal;
                        pictureBox3.SizeMode = PictureBoxSizeMode.Normal;
                    }
                }
            }
        }

        /// <summary>
        /// ���������� ������� ������ � ����������
        /// </summary>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // ��������� ������� ������ Z, X � C
            if (e.KeyCode == Keys.Z)
            {
                if (pictureBox2.Visible == true)
                {
                    pictureBox2.Visible = false;
                }
            }
            else if (e.KeyCode == Keys.X)
            {
                if (pictureBox2.Visible == false)
                {
                    pictureBox2.Visible = true;
                }
            }
            else if (e.KeyCode == Keys.C)
            {

            }
        }

        /// <summary>
        /// ���������� ������� ������� ������ �������� �����������
        /// </summary>
        private void LoadOriginalImagebtn_Click(object sender, EventArgs e)
        {
            LoadImage();
            PrintOriginalImage(pictureBox1);
        }


        private void Colortxt_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Graytxt.Text) || string.IsNullOrEmpty(Whitetxt.Text))
            {
                ApplyBtn.Enabled = false;
            }
            else
            {
                ApplyBtn.Enabled = true;
            }
        }

        /// <summary>
        /// ���������� ������� ������ ���������� ��������� ��������
        /// </summary>
        private void ApplyBtn_Click(object sender, EventArgs e)
        {
            int Gray = Convert.ToInt32(Graytxt.Text);
            int White = Convert.ToInt32(Whitetxt.Text);
            if(Gray>0 && White>0 && Gray < White)
            {
                PrintSelectingAreasImage(pictureBox2, Gray, White);
            }
        }
    }
}
