using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace image_segm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        
        // what's n???????
        int n = 100;

        
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private float boxArea(RotatedRect box)
        {
            return box.Size.Height * box.Size.Width;
        }

        private double getDistance(PointF p1, PointF p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        Bitmap InitialImage;
        bool loaded = false;
        bool processed = false;
        
        private void FilterSame(List<RotatedRect> boxList, List<Triangle2DF> triangleList, CircleF[] circles, int imageSize, int areaDiff = 7000, double threshold = 10)
        {
            List<RotatedRect> deleted = new List<RotatedRect>();

            for (int i = 0; i < boxList.Count; i++)
            {
                RotatedRect box = boxList[i];
                for (int j = 0; j < circles.Length; j++)
                {
                    CircleF circle = circles[j];
                    if (Math.Abs(box.Center.X - circle.Center.X) < threshold && Math.Abs(box.Center.Y - circle.Center.Y) < threshold)
                    {
                        deleted.Add(box);
                    }
                }
            }

            for (int i = 0; i < deleted.Count; i++)
            {
                if (boxList.Contains(deleted[i]))
                {
                    boxList.Remove(deleted[i]);
                }
            }
            deleted.Clear();
            for (int i = 0; i < boxList.Count; i++)
            {
                RotatedRect box1 = boxList[i];
                for (int j = i + 1; j < boxList.Count; j++)
                {
                    RotatedRect box2 = boxList[j];
                    if (Math.Abs(boxArea(box1) - boxArea(box2)) < areaDiff && Math.Abs(box1.Center.X - box2.Center.X) < threshold && Math.Abs(box1.Center.Y - box2.Center.Y) < threshold)
                    {
                        deleted.Add(box2);
                    }
                }
            }

            for (int i = 0; i < deleted.Count; i++)
            {
                if (boxList.Contains(deleted[i]))
                {
                    boxList.Remove(deleted[i]);
                }
            }
            deleted.Clear();

            List<Triangle2DF> deletedTri = new List<Triangle2DF>();

            for (int i = 0; i < triangleList.Count; i++)
            {
                Triangle2DF tri1 = triangleList[i];
                for (int j = i + 1; j < triangleList.Count; j++)
                {
                    Triangle2DF tri2 = triangleList[j];
                    if (Math.Abs(tri1.Centeroid.X - tri2.Centeroid.X) < threshold && Math.Abs(tri1.Centeroid.Y - tri2.Centeroid.Y) < threshold)
                    {
                        deletedTri.Add(tri1);
                    }
                }
            }

            for (int i = 0; i < deletedTri.Count; i++)
            {
                if (triangleList.Contains(deletedTri[i]))
                {
                    triangleList.Remove(deletedTri[i]);
                }
            }
            deletedTri.Clear();

            foreach (RotatedRect box in boxList)
            {
                if (Math.Abs(boxArea(box) - imageSize) < areaDiff)
                {
                    deleted.Add(box);
                }
            }
            for (int i = 0; i < deleted.Count; i++)
            {
                if (boxList.Contains(deleted[i]))
                {
                    boxList.Remove(deleted[i]);
                }
            }
            deleted.Clear();
        }

        private PointF[] SortPoints(List<PointF> points)
        {
            points.Sort((a, b) => a.X.CompareTo(b.X));
            PointF[] listPoints = new PointF[points.Count];
            listPoints[0] = points[0];
            int posCount = 0;
            double minDist = 1000000;
            bool[] visited = new bool[points.Count];
            for (int i = 0; i < visited.Length; i++)
            {
                visited[i] = false;
            }
            visited[0] = true;

            for (int i = 0; i < points.Count - 1; i++)
            {
                posCount++;
                int nxtPointIndex = i + 1;
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (getDistance(points[i], points[j]) < minDist && !visited[j])
                    {
                        nxtPointIndex = j;
                        minDist = getDistance(points[i], points[j]);
                    }
                }
                listPoints[posCount] = points[nxtPointIndex];
                visited[nxtPointIndex] = true;

            }
            return listPoints;
        }

        private List<int> GetNeighbours(int xPos, int yPos, Bitmap bitmap)
        {
            List<int> neighboursList = new List<int>();

            int xStart, yStart, xFinish, yFinish;

            int pixel;

            xStart = xPos - 15;
            yStart = yPos - 15;

            xFinish = xPos + 15;
            yFinish = yPos + 15;

            for (int y = yStart; y <= yFinish; y++)
            {
                for (int x = xStart; x <= xFinish; x++)
                {
                    if (x < 0 || y < 0 || x > (bitmap.Width - 1) || y > (bitmap.Height - 1))
                    {
                        continue;
                    }
                    else
                    {
                        pixel = bitmap.GetPixel(x, y).R;

                        neighboursList.Add(pixel);
                    }
                }
            }

            return neighboursList;
        }

        private void BernsenBinarization(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap);

            byte iMin, iMax, t, c, contrastThreshold, pixel;

            contrastThreshold = 15;

            List<int> list = new List<int>();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    list.Clear();

                    pixel = bitmap.GetPixel(x, y).R;

                    list = GetNeighbours(x, y, bitmap);

                    list.Sort();

                    iMin = Convert.ToByte(list[0]);

                    iMax = Convert.ToByte(list[list.Count - 1]);

                    t = (byte)((iMax + iMin) / 2);

                    c = (byte)(iMax - iMin);

                    if (c < contrastThreshold)
                    {
                        pixel = (byte)((t >= 128) ? 0 : 255);
                    }
                    else
                    {
                        pixel = (byte)((pixel >= t) ? 0 : 255);
                    }

                    result.SetPixel(x, y, Color.FromArgb(pixel, pixel, pixel));
                }
            }
            resPicBox.Image = result;
        }

        /*
        private Image<Gray, byte> AdaptiveBinarization(Image<Gray, byte> image)
        {
            int x1, y1, x2, y2;
            int s2 = image.Width / 2;
            Image<Gray, byte> integralImg = new Image<Gray, byte>(image.Width, image.Height);
            Image<Gray, byte> res = new Image<Gray, byte>(image.Width, image.Height);
            for (int i = 0; i < image.Width; i++)
            {
                double sum = 0;
                for (int j = 0; j < image.Height; j++)
                {
                    sum += image[j, i].Intensity;
                    if (i == 0) integralImg[j, i] = new Gray(sum);
                    else integralImg[j, i] = new Gray(sum + image[j, i - 1].Intensity);
                }
            }
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    x1 = i - s2; x2 = i + s2;
                    y1 = j - s2; y2 = j + s2;

                    if (x1 < 0) x1 = 0;
                    if (x2 >= image.Width) x2 = image.Width - 1;
                    if (y1 < 0) y1 = 0;
                    if (y2 >= image.Height) y2 = image.Height - 1;

                    int count = (x2 - x1) * (y2 - y1);
                    double sum = integralImg[y2, x2].Intensity - integralImg[y1, x2].Intensity - integralImg[y2, x1].Intensity + integralImg[y1, x1].Intensity;
                    if (image[j, i].Intensity * count < sum * (1 - 0.15))
                        res[j, i] = new Gray(0);
                    else res[j, i] = new Gray(255);
                }
            }

            return res;
        }
        */

        private double GetKMeansThreshold(Image<Gray, byte> image)
        {
            double threshold = 0;
            int[] hist = new int[256];
            for (int i = 0; i < hist.Length; i++)
            {
                hist[i] = 0;
            }

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    int val = (int)image[j, i].Intensity;
                    hist[val] += 1;
                }
            }

            threshold = hist.Length / 2;
            double tOld = 0;

            while(Math.Abs(threshold - tOld) > 3)
            {
                tOld = threshold;
                double m1 = 0 , m2 = 0;
                int m1Count = 0, m2Count = 0;
                for (var i = 0; i < image.Width; i++)
                {
                    for (var j = 0; j < image.Height; j++)
                    {
                        var val = (int)image[j, i].Intensity;
                        if(val > threshold)
                        {
                            m1 += val;
                            m1Count++;
                        }
                        else
                        {
                            m2 += val;
                            m2Count++;
                        }
                    }
                }
                m1 /= m1Count;
                m2 /= m2Count;

                threshold = (m1 + m2) / 2;
            }
            
            return threshold;
        }

        
        private double GetOtsuThreshold(Image<Gray, byte> image)
        {
            int N = image.Width * image.Height;
            double threshold = 0;
            int[] hist = new int[256];
            for (int i = 0; i < hist.Length; i++)
            {
                hist[i] = 0;
            }

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    int val = (int)image[j, i].Intensity;
                    hist[val] += 1;
                }
            }

            int sum = 0;
            for (int t = 0; t < 256; t++) sum += t * hist[t];

            float sumB = 0;
            int wB = 0;
            int wF = 0;

            float varMax = 0;
            threshold = 0;

            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];               // Weight Background
                if (wB == 0) continue;

                wF = N - wB;                 // Weight Foreground
                if (wF == 0) break;

                sumB += (float)(t * hist[t]);

                float mB = sumB / wB;            // Mean Background
                float mF = (sum - sumB) / wF;    // Mean Foreground

                float varBetween = wB * wF * (mB - mF) * (mB - mF);

                // Check if new maximum found
                if (varBetween > varMax)
                {
                    varMax = varBetween;
                    threshold = t;
                }
            }
            return threshold/2;
        }

        //3
        private void Process(Bitmap bm, int level, double circleAccumulatorThreshold = 70.0)
        {
            double cannyThreshold = 0;
            Image<Bgr, Byte> img = new Image<Bgr, Byte>(bm);
            if (level == 1)
            {
                CvInvoke.MedianBlur(img, img, 5);
                circleAccumulatorThreshold = 100.0;
            }
            else if(level > 1)
            {
                for (int i = 0; i<level; i++)
                {
                    CvInvoke.MedianBlur(img, img, 5);

                }
                circleAccumulatorThreshold = 120.0;
            }

            System.Console.WriteLine("Filtering done");

            Image<Gray, byte> grayimage = new Image<Gray, byte>(bm);
            CvInvoke.CvtColor(img, grayimage, ColorConversion.Bgr2Gray);

            //cannyThreshold = getOtsuThreshold(grayimage);
            cannyThreshold = GetKMeansThreshold(grayimage);
            label2.Text = cannyThreshold.ToString();


            System.Console.WriteLine("Canny threshold using OTSU found " + cannyThreshold.ToString());

            //Convert the image to grayscale and filter out the noise
            UMat uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);



            CircleF[] circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 5.0, cannyThreshold, circleAccumulatorThreshold, 1, img.Height/10);
            System.Console.WriteLine("Circles founf " + circles.Length.ToString());

            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThreshold);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               20, //threshold
               30, //min Line width
               10); //gap between lines
            System.Console.WriteLine("Lines detected");

            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<RotatedRect> boxList = new List<RotatedRect>();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        if (!(CvInvoke.ContourArea(approxContour, false) > 10)) continue;
                        switch (approxContour.Size)
                        {
                            case 3:
                            {
                                Point[] pts = approxContour.ToArray();
                                triangleList.Add(new Triangle2DF(
                                    pts[0],
                                    pts[1],
                                    pts[2]
                                ));
                                break;
                            }
                            case 4:
                            {
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(
                                        edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 80 || angle > 100)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                                break;
                            }
                        }
                    }
                }
            }
            
            System.Console.WriteLine("Boxes found " + boxList.Count.ToString());
            System.Console.WriteLine("Triangles found " + triangleList.Count.ToString());

            FilterSame(boxList, triangleList, circles, img.Width * img.Height);

            List<PointF> points = new List<PointF>();

            Image<Bgr, Byte> Image = img.CopyBlank();
            foreach (Triangle2DF triangle in triangleList)
            {
                Image.Draw(triangle, new Bgr(Color.Red), 3);
                points.Add(triangle.Centeroid);
            }
            
            foreach (RotatedRect box in boxList)
            {
                Image.Draw(box, new Bgr(Color.Blue), 3);
                points.Add(box.Center);
            }
                
            foreach (CircleF circle in circles)
            {
                Image.Draw(circle, new Bgr(Color.DarkCyan), 3);
                points.Add(circle.Center);
            }

            PointF[] listPoints = SortPoints(points);


            System.Console.WriteLine("Points sorted, num of objects " + listPoints.Length.ToString());

            resPicBox.Image = (Image+img).ToBitmap();
            PointF[] bezierList = GetBezierCurve(listPoints);
            Graphics g = Graphics.FromImage(resPicBox.Image);
            Pen p = new Pen(Color.Red);
            for (int i = 0; i < n - 1; i++) {
                g.DrawLine(p, bezierList[i], bezierList[i+1]);
            }


            System.Console.WriteLine(bezierList[0].X + "   " + bezierList[0].Y);
            System.Console.WriteLine(bezierList[1].X + "   " + bezierList[1].Y);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "Open first image";
            dlg.Filter = "Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                srcPicBox.Image = new Bitmap(dlg.FileName);
                InitialImage = new Bitmap(dlg.FileName);
            }
            loaded = true;
            dlg.Dispose();
            processed = false;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png;*.bmp;*.jpg";
            ImageFormat format = ImageFormat.Png;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string ext = System.IO.Path.GetExtension(sfd.FileName);
                switch (ext)
                {
                    case ".jpg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                    case ".png":
                        format = ImageFormat.Png;
                        break;
                }
                resPicBox.Image.Save(sfd.FileName, format);
            }
        }

        private void processSimpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process((Bitmap)srcPicBox.Image, 0);
        }

        private void processMediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process((Bitmap)srcPicBox.Image, 1);
        }

        private void processManiacToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process((Bitmap)srcPicBox.Image, 2);
        }

        private void medianFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image<Bgr, Byte> img;
            if (!processed)
            {
                img = new Image<Bgr, Byte>((Bitmap)srcPicBox.Image);
            }
            else
            {
                img = new Image<Bgr, Byte>((Bitmap)resPicBox.Image);
            }
            CvInvoke.MedianBlur(img, img, 5);
            resPicBox.Image = img.ToBitmap();
            processed = true;

        }

        private void thresholdingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bitmap bm; 
            if (!processed)
            {
                bm = (Bitmap)srcPicBox.Image;
            }
            else
            {
                bm = (Bitmap)resPicBox.Image;
            }

            Image<Gray, byte> grayimage = new Image<Gray, byte>(bm);
            double cannyThreshold = GetOtsuThreshold(grayimage);
            cannyThreshold = GetKMeansThreshold(grayimage);
            Bitmap res = new Bitmap(bm.Width, bm.Height);
            label2.Text = cannyThreshold.ToString();
            for (int i = 0; i<grayimage.Height; i++)
            {
                for (int j = 0; j<grayimage.Width; j++)
                {
                    if (grayimage[i, j].Intensity > cannyThreshold)
                    {
                        res.SetPixel(j, i, Color.White);
                    }
                    else
                    {
                        res.SetPixel(j, i, Color.Black);
                    }
                }
            }
            resPicBox.Image = res;
            processed = true;
        }

        private void grayscalingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image<Bgr, Byte> img;
            if (!processed)
            {
                img = new Image<Bgr, Byte>((Bitmap)srcPicBox.Image);
            }
            else
            {
                img = new Image<Bgr, Byte>((Bitmap)resPicBox.Image);
            }
            UMat uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
            resPicBox.Image = uimage.Bitmap;
            processed = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            processed = false;
            label2.Text = "";
        }

        //Bezier
        int Factor(int n) {
            int fact = 1;
            for (int j = 2; j <= n; j++)
            {
                fact *= j;
            }
            return fact;
        }

        float GetBezierBasis(int i, int n, float t)
        {
            // Факториал
            int fact = 1;
            for (int j = 2; j <= n; j++) {
                fact *= j;
            }

            // считаем i-й элемент полинома Берштейна
            return (Factor(n) / (Factor(i) * Factor(n - i))) * (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i);
        }

        // arr - массив опорных точек. Точка - двухэлементный массив, (x = arr[0], y = arr[1])
        // step - шаг при расчете кривой (0 < step < 1), по умолчанию 0.01
        private PointF[] GetBezierCurve(PointF[] arr, float step = 0f)
        {
            PointF[] res = new PointF[n + 1];
            int posCount = 0;
            step = (float)1 / n;

            for (float t = 0f; t < 1; t += step)
            {
                //var ind = res.length;

                //res[ind] = new Array(0, 0);

                for (int i = 0; i < arr.Length; i++)
                {
                    float b = GetBezierBasis(i, arr.Length - 1, t);

                    res[posCount].X += arr[i].X * b;
                    res[posCount].Y += arr[i].Y * b;
                }
                //System.Console.WriteLine("t = " + t);
                posCount++;
            }


            return res;
        }
    }
}
