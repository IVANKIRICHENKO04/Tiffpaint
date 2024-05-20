using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using System.Diagnostics;
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

        private int grey;
        public int GreyThreshold
        {
            get
            {
                return grey;
            }
            set
            {
                if (value != GreyThreshold)
                {
                    grey = value;
                    TrackGrey.Value = grey;
                    Greytxt.Text = grey.ToString();
                }
            }
        }
        private int white;
        public int WhiteThreshold
        {
            get
            {
                return white;
            }
            set
            {
                if (value != WhiteThreshold)
                {
                    white = value;
                    Whitetxt.Text = white.ToString();
                    TrackWhite.Value = white;
                }
            }
        }

        Bitmap ChunkImage;
        Pen pen;
        Color CurrentColor;
        private string imagePath; // ���������� ��� �������� ���� � �����������
        string filePath;

        private int chunkSize = 512;
        private int currentChunkX = 0;
        private int currentChunkY = 0;


        private int ConvertingNumber = 13;                              // �����, �� ������� ���� ��������� 16 ��� ���������� � 8


        public Form1()
        {
            InitializeComponent();
            GdalBase.ConfigureAll();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TrackInit();
            InitializePen();
            GreyThreshold = 300;
            WhiteThreshold = 800;
            pictureBox2.Visible = false;
            pictureBox2.MouseWheel += pictureBox_MouseWheel;
            pictureBox1.MouseWheel += pictureBox_MouseWheel;
            pictureBox3.MouseWheel += pictureBox_MouseWheel;

        }

        /// <summary>
        /// ������������� ���������
        /// </summary>
        private void TrackInit()
        {
            TrackGrey.Minimum = 100;
            TrackGrey.Maximum = 500;
            TrackGrey.TickFrequency = 10;
            TrackGrey.SmallChange = 10; // ��� ��������� �������� ��� ������������� �������
            TrackGrey.LargeChange = 20; // ��� ��������� �������� ��� ����� �� �������� ��� ������� ��������
            TrackGrey.ValueChanged += (sender, e) =>
            {
                GreyThreshold = TrackGrey.Value;
                PrintSelectingAreasImage(pictureBox2, ChunkImage, GreyThreshold, WhiteThreshold);
            };

            TrackWhite.Minimum = 600;
            TrackWhite.Maximum = 1000;
            TrackWhite.TickFrequency = 10;
            TrackWhite.SmallChange = 10; // ��� ��������� �������� ��� ������������� �������
            TrackWhite.LargeChange = 20; // ��� ��������� �������� ��� ����� �� �������� ��� ������� ��������
            TrackWhite.ValueChanged += (sender, e) =>
            {
                WhiteThreshold = TrackWhite.Value;
                PrintSelectingAreasImage(pictureBox2, ChunkImage, GreyThreshold, WhiteThreshold);
            };
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
                imagePath = openFileDialog.FileName; // ��������� ���� � �����
                filePath = imagePath; // ����� ��������� ���� � ����� � ��������� ����������, ���� ��� ����������

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
        /// ������� ����� ������������� �����������
        /// </summary>
        /// <param name="box">��������� ��� ������</param>
        /// <param name="chunkX">���������� X �������� ������ ���� �����</param>
        /// <param name="chunkY">���������� Y �������� ������ ���� �����</param>
        /// <param name="chunkHeight">������ �����</param>
        /// <param name="chunkWidth">������ �����</param>
        public void PrintImageChunk(PictureBox box, int chunkX, int chunkY, int chunkWidth, int chunkHeight)
        {
            Bitmap chunkImage = new Bitmap(chunkWidth, chunkHeight);
            // �������������� �������� �� short � byte (0-255) � ��������� ����� �����������
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int x = 0; x < chunkWidth; x++)
                {
                    int index = (chunkY + y) * width + chunkX + x;
                    if (index >= 0 && index < red.Length)
                    {
                        byte redByte = (byte)(red[index] / ConvertingNumber);
                        byte greenByte = (byte)(green[index] / ConvertingNumber);
                        byte blueByte = (byte)(blue[index] / ConvertingNumber);
                        Color color = Color.FromArgb(redByte, greenByte, blueByte);
                        chunkImage.SetPixel(x, y, color);

                    }
                    else
                    {
                        // ���� ������ ��������� �� ��������� �������, ��������� ���� ������� ������ ������ ��� ����� ������ �� ������ ������
                        chunkImage.SetPixel(x, y, Color.Black);
                    }
                }
            }
            box.Image = chunkImage;
            ChunkImage = chunkImage;

            string fileName = Path.GetFileNameWithoutExtension(imagePath); // ��� ����� ��� ����������
            string chunkName = $"{fileName}_Chunk_{currentChunkX}_{currentChunkY}.tiff"; // ���������� ��� �����
            string chunkPath = Path.Combine(Path.GetDirectoryName(imagePath), chunkName); // ������ ���� � �����
            if (LoadChunkMask(chunkPath))
            {
                PrintSelectingAreasImage(pictureBox2, ChunkImage, 300, 800);
            }
        }

        /// <summary>
        /// ������� ����������� � ����������� ���������
        /// </summary>
        /// <param name="box">���������, ���� ��������</param>
        /// <param name="bitmap">�������� �����������</param>
        /// <param name="grayThreshold">����� ������� ��� ����������� ����� �������</param>
        /// <param name="whiteThreshold">����� ������� ��� ����������� ����� �������</param>
        public void PrintSelectingAreasImage(PictureBox box, Bitmap bitmap, int grayThreshold, int whiteThreshold)
        {
            if (bitmap != null)
            {


                // �������� ������� Bitmap
                drawingBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                Color gray = Color.FromArgb(128, 128, 128);
                Color black = Color.Black;
                Color white = Color.White;
                // ���������� ����������� �������
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color pixelColor = bitmap.GetPixel(x, y);
                        int brightness = (int)(pixelColor.R * ConvertingNumber + pixelColor.G * ConvertingNumber + pixelColor.B * ConvertingNumber) / 3;
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
            else if (e.KeyCode == Keys.A)
            {
                ShowPreviousChunk();
            }
            else if (e.KeyCode == Keys.S)
            {
                ShowNextChunk();
            }
        }

        /// <summary>
        /// ���������� ������� ������� ������ �������� �����������
        /// </summary>
        private void LoadOriginalImagebtn_Click(object sender, EventArgs e)
        {
            LoadImage();
            StatisticksWrite();
            //PrintOriginalImage(pictureBox1);
            PrintImageChunk(pictureBox1, 0, 0, 512, 512);

            ShowChunkInfo();
        }

        /// <summary>
        /// ����� ����������
        /// </summary>
        private void StatisticksWrite()
        {

            int chunkSize = 512;

            int rows = (int)Math.Ceiling((double)height / chunkSize);
            int cols = (int)Math.Ceiling((double)width / chunkSize);

            int totalChunks = rows * cols;

            int lastRowChunks = (int)Math.Ceiling((double)(width % chunkSize) / chunkSize);
            int lastColChunks = (int)Math.Ceiling((double)(height % chunkSize) / chunkSize);

            totalChunks += (lastRowChunks * (rows - 1)) + lastColChunks;

            Debug.WriteLine("���������� �������� �������� 512x512 � ����������� {0}x{1}: {2}", width, height, totalChunks);
        }

        /// <summary>
        /// �������� ��������� ��������
        /// </summary>
        private void Colortxt_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Greytxt.Text) || string.IsNullOrEmpty(Whitetxt.Text))
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
            if (GreyThreshold > 0 && WhiteThreshold > 0 && GreyThreshold < WhiteThreshold)
            {
                PrintSelectingAreasImage(pictureBox2, ChunkImage, GreyThreshold, WhiteThreshold);
            }
        }

        /// <summary>
        /// ����� ��� ����������� ���������� �����
        /// </summary>
        private void ShowNextChunk()
        {
            int chunkWidth = Math.Min(chunkSize, width - currentChunkX);
            int chunkHeight = Math.Min(chunkSize, height - currentChunkY);

            // ��������� ������� ���� ��� ���������� ������
            string fileName = Path.GetFileNameWithoutExtension(imagePath); // ��� ����� ��� ����������
            string chunkName = $"{fileName}_Chunk_{currentChunkX}_{currentChunkY}.tiff"; // ���������� ��� �����
            string chunkPath = Path.Combine(Path.GetDirectoryName(imagePath), chunkName); // ������ ���� � �����
                                                                                          // ��������� ������� ���� ��� ����������� �� ����������� ����
            SaveChunkImage(chunkPath);
            // ���� ������� ���� �������� ���� ���, ��������� � ���������� ����
            if (currentChunkX + chunkWidth >= width)
            {
                currentChunkX = 0;
                currentChunkY += chunkSize;
            }
            else
            {
                currentChunkX += chunkSize;
            }

            // ���������� ���� � ��������� ���������� � �����
            PrintImageChunk(pictureBox1, currentChunkX, currentChunkY, chunkWidth, chunkHeight);
            ShowChunkInfo();
            chunkName = $"{fileName}_Chunk_{currentChunkX}_{currentChunkY}.tiff"; // ���������� ��� �����
            chunkPath = Path.Combine(Path.GetDirectoryName(imagePath), chunkName); // ������ ���� � �����
            if (LoadChunkMask(chunkPath))
            {
                PrintSelectingAreasImage(pictureBox2, ChunkImage, 300, 800);
            }


        }

        private void SaveChunkImage(string chunkPath)
        {
            // ���������, ���������� �� ����
            if (File.Exists(chunkPath))
            {
                File.Delete(chunkPath);
            }

            // ��������� Bitmap � ����
            drawingBitmap.Save(chunkPath, ImageFormat.Tiff);
            drawingBitmap.Dispose();
        }

        private bool LoadChunkMask(string chunkPath)
        {
            if (File.Exists(chunkPath))
            {
                Bitmap originalBitmap = new Bitmap(chunkPath);
                drawingBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);
                for (int y = 0; y < originalBitmap.Height; y++)
                {
                    for (int x = 0; x < originalBitmap.Width; x++)
                    {
                        // �������� ���� ������� �� originalBitmap
                        Color pixelColor = originalBitmap.GetPixel(x, y);

                        // ������������� ���� ������� � drawingBitmap
                        drawingBitmap.SetPixel(x, y, pixelColor);
                    }
                }
                originalBitmap.Dispose();
                // ���������� ������������� Bitmap �� PictureBox'��
                pictureBox2.Image = drawingBitmap;
                pictureBox3.Image = drawingBitmap;
                return false;


            }
            return true;
        }

        /// <summary>
        /// ����� ��� ����������� ����������� �����
        /// </summary>
        private void ShowPreviousChunk()
        {
            // ��������� ������� ���� ��� ���������� ������
            string fileName = Path.GetFileNameWithoutExtension(imagePath); // ��� ����� ��� ����������
            string chunkName = $"{fileName}_Chunk_{currentChunkX}_{currentChunkY}.tiff"; // ���������� ��� �����
            string chunkPath = Path.Combine(Path.GetDirectoryName(imagePath), chunkName); // ������ ���� � �����
            SaveChunkImage(chunkPath);

            int newChunkX = currentChunkX - chunkSize;

            // ���� ����� ���������� X �������������, ������ ����� ������� � ����������� ����
            if (newChunkX < 0 && currentChunkY >= chunkSize)
            {
                // ������� � ����������� ����
                currentChunkX = Math.Max(0, width - chunkSize);
                currentChunkY -= chunkSize;
            }
            // ���� ����� ���������� X �� �������������, ��������� �� ��� ���������
            else
            {
                currentChunkX = Math.Max(0, newChunkX);
            }

            // ������� ����� ����������� �����, ���� ��� ����������


            // ���������� ����
            PrintImageChunk(pictureBox1, currentChunkX, currentChunkY, chunkSize, chunkSize);
            string maskFolderPath = Path.GetDirectoryName(imagePath);
            string fileName2 = Path.GetFileNameWithoutExtension(imagePath); // ��� ����� ��� ����������
            string maskFileName = $"{fileName2}_Chunk_{currentChunkX}_{currentChunkY}.tiff";
            string maskFilePath = Path.Combine(maskFolderPath, maskFileName);
            LoadChunkMask(maskFilePath);
            ShowChunkInfo();
        }

        private void Back_btn_Click(object sender, EventArgs e)
        {
            ShowPreviousChunk();
        }

        private void Further_btn_Click(object sender, EventArgs e)
        {
            ShowNextChunk();
        }

        private void ShowChunkInfo()
        {
            int chunkWidth = Math.Min(chunkSize, width - currentChunkX);
            int chunkHeight = Math.Min(chunkSize, height - currentChunkY);

            int chunkNumberX = currentChunkX / chunkSize + 1;
            int chunkNumberY = currentChunkY / chunkSize + 1;
            int totalChunksX = (int)Math.Ceiling((double)width / chunkSize);
            int totalChunksY = (int)Math.Ceiling((double)height / chunkSize);
            int totalChunks = totalChunksX * totalChunksY;
            int currentChunkNumber = (chunkNumberY - 1) * totalChunksX + chunkNumberX;

            string info = string.Format("���������� �����: ({0}, {1})\n", currentChunkX, currentChunkY);
            info += string.Format("������ �����: {0}x{1}\n", chunkWidth, chunkHeight);
            info += string.Format("����� ���������� ������: {0}\n", totalChunks);
            info += string.Format("��� ���� ����� {0} �� {1}", currentChunkNumber, totalChunks);

            ChunkInfo_lbl.Text = info;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
