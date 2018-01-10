using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV.Stitching;
using AForge;
using Point = System.Drawing.Point;
using AForge.Imaging;
using AForge.Math.Geometry;

namespace image_segm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        
        // what's n???????
        private const int N = 100;


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private static float BoxArea(RotatedRect box)
        {
            return box.Size.Height * box.Size.Width;
        }

        private static double GetDistance(PointF p1, PointF p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        Bitmap InitialImage;
        bool loaded = false;
        bool processed = false;

        private static void FilterSame(List<RotatedRect> boxList, IList<Triangle2DF> triangleList, CircleF[] circles, int imageSize, int areaDiff = 8000, double threshold = 10)
        {
            if (boxList == null) throw new ArgumentNullException(nameof(boxList));
            if (triangleList == null) throw new ArgumentNullException(nameof(triangleList));
            var deleted = (from box in boxList from circle in circles where Math.Abs(box.Center.X - circle.Center.X) < threshold && Math.Abs(box.Center.Y - circle.Center.Y) < threshold select box).ToList();

            foreach (var box in deleted)
            {
                if (boxList.Contains(box))
                {
                    boxList.Remove(box);
                }
            }
            deleted.Clear();
            for (var i = 0; i < boxList.Count; i++)
            {
                var box1 = boxList[i];
                for (var j = i + 1; j < boxList.Count; j++)
                {
                    var box2 = boxList[j];
                    if (Math.Abs(BoxArea(box1) - BoxArea(box2)) < areaDiff && Math.Abs(box1.Center.X - box2.Center.X) < threshold && Math.Abs(box1.Center.Y - box2.Center.Y) < threshold)
                    {
                        deleted.Add(box2);
                    }
                }
            }

            foreach (var box in deleted)
            {
                if (boxList.Contains(box))
                {
                    boxList.Remove(box);
                }
            }
            deleted.Clear();

            var deletedTri = new List<Triangle2DF>();

            for (var i = 0; i < triangleList.Count; i++)
            {
                var tri1 = triangleList[i];
                for (var j = i + 1; j < triangleList.Count; j++)
                {
                    var tri2 = triangleList[j];
                    if (Math.Abs(tri1.Centeroid.X - tri2.Centeroid.X) < threshold && Math.Abs(tri1.Centeroid.Y - tri2.Centeroid.Y) < threshold)
                    {
                        deletedTri.Add(tri1);
                    }
                }
            }

            foreach (Triangle2DF triangle in deletedTri)
            {
                if (triangleList.Contains(triangle))
                {
                    triangleList.Remove(triangle);
                }
            }
            deletedTri.Clear();

            foreach (var box in boxList)
            {
                if (Math.Abs(BoxArea(box) - imageSize) < areaDiff)
                {
                    deleted.Add(box);
                }
            }
            foreach (var box in deleted)
            {
                if (boxList.Contains(box))
                {
                    boxList.Remove(box);
                }
            }
            deleted.Clear();
        }

        private static PointF[] SortPoints(List<PointF> points)
        {
            points.Sort((a, b) => a.X.CompareTo(b.X));
            var listPoints = new PointF[points.Count];
            listPoints[0] = points[0];
            var posCount = 0;
            double minDist = 1000000;
            var visited = new bool[points.Count];
            for (var i = 0; i < visited.Length; i++)
            {
                visited[i] = false;
            }
            visited[0] = true;

            for (var i = 0; i < points.Count - 1; i++)
            {
                posCount++;
                var nxtPointIndex = i + 1;
                for (var j = i + 1; j < points.Count; j++)
                {
                    if (!(GetDistance(points[i], points[j]) < minDist) || visited[j]) continue;
                    nxtPointIndex = j;
                    minDist = GetDistance(points[i], points[j]);
                }
                listPoints[posCount] = points[nxtPointIndex];
                visited[nxtPointIndex] = true;

            }
            return listPoints;
        }


        private double GetKMeansThreshold(Image<Gray, byte> image)
        {
            double threshold = 0;
            var hist = new int[256];
            for (var i = 0; i < hist.Length; i++)
            {
                hist[i] = 0;
            }

            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    var val = (int)image[j, i].Intensity;
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

        private void BlackBG(Image<Gray, byte> image)
        {
            var hist = new int[256];
            for (var i = 0; i < hist.Length; i++)
            {
                hist[i] = 0;
            }

            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    var val = (int)image[j, i].Intensity;
                    hist[val] += 1;
                }
            }
            var max = 0;
            for (var i = 0; i < hist.Length; i++)
            {
                if (hist[i] > hist[max])
                {
                    max = i;
                }
            }
            
            for (var i = 0; i < image.Width; i++)
            {
                for (var j = 0; j < image.Height; j++)
                {
                    if ((int) image[j, i].Intensity == max)
                        image[j, i] = new Gray(0);
                }
            }
        }

        private void Thresholding(Image<Gray, byte> image, double threshold)
        {
            for (var i = 0; i < image.Cols; i++)
            {
                for (var j = 0; j < image.Rows; j++)
                {
                    if (image[j, i].Intensity < threshold)
                        image[j, i] = new Gray(0);
                    else image[j, i] = new Gray(255);
                }
            }
        }

        private void altProcess(Bitmap bm, int level)
        {
            var img = new Image<Bgr, byte>(bm);
            if (level == 1)
            {
                CvInvoke.MedianBlur(img, img, 5);
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 30, 75, 75);
            }
            else if (level == 2)
            {
                CvInvoke.MedianBlur(img, img, 5);
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 25, 75, 75);
                CvInvoke.Blur(img, img, new Size(5, 5), new Point(0, 0));
            }
            else if (level == 3)
            {
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.FastNlMeansDenoising(resImage, img);
            }
            
            var grayimage = new Image<Gray, byte>(bm);
            CvInvoke.CvtColor(img, grayimage, ColorConversion.Bgr2Gray);
            
            BlackBG(grayimage);
            
            Console.WriteLine("Filtering done");

            var cannyThreshold = GetKMeansThreshold(grayimage);
            
            label2.Text = cannyThreshold.ToString();
            
            Thresholding(grayimage, cannyThreshold);

            Console.WriteLine("Canny threshold using KMEANS found " + cannyThreshold);

            //Convert the image to grayscale and filter out the noise
            var uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
                
            // create instance of blob counter
            BlobCounter blobCounter = new BlobCounter( );
            // process input image
            blobCounter.ProcessImage(grayimage.ToBitmap());
            // get information about detected objects
            Blob[] blobs = blobCounter.GetObjectsInformation( );
            Bitmap newBM = new Bitmap(img.Bitmap);
            Graphics g = Graphics.FromImage(newBM);
            Pen redPen = new Pen(Color.Red, 2);
            // check each object and draw circle around objects, which
            // are recognized as circles
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Pen yellowPen = new Pen(Color.Yellow, 2);
            Pen greenPen = new Pen(Color.Green, 2);
            Pen bluePen = new Pen(Color.Blue, 2);

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints =
                    blobCounter.GetBlobsEdgePoints(blobs[i]);

                AForge.Point center;
                float radius;

                if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                {
                    g.DrawEllipse(yellowPen,
                        (float)(center.X - radius), (float)(center.Y - radius),
                        (float)(radius * 2), (float)(radius * 2));
                }
                else
                {
                    List<IntPoint> corners;
                    if (edgePoints.Count > 1)
                    {
                        if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                        {
                            if (shapeChecker.CheckPolygonSubType(corners) ==
                                PolygonSubType.Square)
                            {
                                g.DrawPolygon(greenPen, ToPointsArray(corners));
                            }
                            else if(shapeChecker.CheckPolygonSubType(corners) ==
                                PolygonSubType.Square)
                            {
                                g.DrawPolygon(bluePen, ToPointsArray(corners));
                            }
                        }
                        else
                        {
                            corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                            g.DrawPolygon(redPen, ToPointsArray(corners));
                        }
                    }
                    
                }
            }

            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            yellowPen.Dispose();
            g.Dispose();
            resPicBox.Image = newBM;
            

        }
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            return points.Select(p => new System.Drawing.Point(p.X, p.Y)).ToArray();
        }

        private void Process(Bitmap bm, int level, double circleAccumulatorThreshold = 70.0, int maxRadius = 0)
        {
            
            double cannyThreshold = 0;
            
            var img = new Image<Bgr, byte>(bm);
            if (level == 1)
            {
                CvInvoke.MedianBlur(img, img, 5);
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 30, 75, 75);
            }
            else if (level == 2)
            {
                CvInvoke.MedianBlur(img, img, 5);
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 25, 75, 75);
                CvInvoke.Blur(img, img, new Size(5, 5), new Point(0, 0));
            }
            else if (level == 3)
            {
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.FastNlMeansDenoising(resImage, img);
            }
            var grayimage = new Image<Gray, byte>(bm);
            CvInvoke.CvtColor(img, grayimage, ColorConversion.Bgr2Gray);
            
            maxRadius = img.Width / 10;
            
            BlackBG(grayimage);
            
            Console.WriteLine("Filtering done");

            cannyThreshold = GetKMeansThreshold(grayimage);
            
            label2.Text = cannyThreshold.ToString();
            
            Thresholding(grayimage, cannyThreshold);

            Console.WriteLine("Canny threshold using KMEANS found " + cannyThreshold);

            //Convert the image to grayscale and filter out the noise
            var uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);



            var circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 5.0, cannyThreshold, circleAccumulatorThreshold, 1, maxRadius);
            
            Console.WriteLine("Circles found " + circles.Length.ToString());

            var cannyEdges = new UMat();
            switch (level)
            {
                case 0:
                    CvInvoke.Canny(grayimage.ToUMat(), cannyEdges, cannyThreshold, cannyThreshold);
                    break;
                case 1:
                    CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThreshold);
                    break;
                default:
                    CvInvoke.Canny(grayimage.ToUMat(), cannyEdges, cannyThreshold, cannyThreshold);
                    break;
            }
            
            var lines = CvInvoke.HoughLinesP(cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 180.0, //Angle resolution measured in radians.
               1, //threshold
               5, //min Line length
               5); //gap between lines
            Console.WriteLine("Lines detected");

            var triangleList = new List<Triangle2DF>();
            var boxList = new List<RotatedRect>();

            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                var count = contours.Size;
                for (var i = 0; i < count; i++)
                {
                    using (var contour = contours[i])
                    using (var approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        if (!(CvInvoke.ContourArea(approxContour, false) > 10)) continue;
                        if (approxContour.Size == 3)
                        {
                            var pts = approxContour.ToArray();
                            triangleList.Add(new Triangle2DF(
                                pts[0],
                                pts[1],
                                pts[2]
                            ));
                        }
                        else if (approxContour.Size == 4)
                        {
                            var pts = approxContour.ToArray();
                            var edges = PointCollection.PolyLine(pts, true);

                            var isRectangle = edges
                                .Select((t, j) => Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(t)))
                                .All(angle => !(angle < 80) && !(angle > 100));
                            if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                        }
                    }
                }
            }
            
            System.Console.WriteLine("Boxes found " + boxList.Count.ToString());
            System.Console.WriteLine("Triangles found " + triangleList.Count.ToString());

            FilterSame(boxList, triangleList, circles, img.Width * img.Height);

            var points = new List<PointF>();

            var Image = img.CopyBlank();
            foreach (var triangle in triangleList)
            {
                Image.Draw(triangle, new Bgr(Color.Red), 3);
                points.Add(triangle.Centeroid);
            }
            
            foreach (var box in boxList)
            {
                Image.Draw(box, new Bgr(Color.Blue), 3);
                points.Add(box.Center);
            }
                
            foreach (var circle in circles)
            {
                Image.Draw(circle, new Bgr(Color.DarkCyan), 3);
                points.Add(circle.Center);
            }

            var listPoints = SortPoints(points);


            System.Console.WriteLine("Points sorted, num of objects " + listPoints.Length.ToString());

            resPicBox.Image = (Image+img).ToBitmap();
            var bezierList = GetBezierCurve(listPoints);
            var g = Graphics.FromImage(resPicBox.Image);
            var p = new Pen(Color.Red);
            for (var i = 0; i < N - 1; i++) {
                g.DrawLine(p, bezierList[i], bezierList[i+1]);
            }


            System.Console.WriteLine(bezierList[0].X + "   " + bezierList[0].Y);
            System.Console.WriteLine(bezierList[1].X + "   " + bezierList[1].Y);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();

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
            var sfd = new SaveFileDialog();
            sfd.Filter = "Images|*.png;*.bmp;*.jpg";
            var format = ImageFormat.Png;
            if (sfd.ShowDialog() != DialogResult.OK) return;
            var ext = System.IO.Path.GetExtension(sfd.FileName);
            if (ext == ".jpg")
                format = ImageFormat.Jpeg;
            else if (ext == ".bmp")
            {
                format = ImageFormat.Bmp;
            }
            else if (ext == ".png")
            {
                format = ImageFormat.Png;
            }
            resPicBox.Image.Save(sfd.FileName, format);
        }

        private void processSimpleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            altProcess((Bitmap)srcPicBox.Image, 0);
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
            var img = !processed ? new Image<Bgr, byte>((Bitmap)srcPicBox.Image) : new Image<Bgr, byte>((Bitmap)resPicBox.Image);
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

            var grayimage = new Image<Gray, byte>(bm);
            
            BlackBG(grayimage);
            
            var cannyThreshold = GetKMeansThreshold(grayimage);
            
            var res = new Bitmap(bm.Width, bm.Height);
            label2.Text = cannyThreshold.ToString();
            
            for (var i = 0; i<grayimage.Height; i++)
            {
                for (var j = 0; j<grayimage.Width; j++)
                {
                    res.SetPixel(j, i, grayimage[i, j].Intensity > cannyThreshold ? Color.White : Color.Black);
                }
            }
            resPicBox.Image = res;
            processed = true;
        }
        
        
        private void grayscalingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var img = !processed ? new Image<Bgr, byte>((Bitmap)srcPicBox.Image) : new Image<Bgr, byte>((Bitmap)resPicBox.Image);
            var uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
            resPicBox.Image = uimage.Bitmap;
            processed = true;
        }
        
        
        private void cannyToolStripMenuItem_Click(object sender, EventArgs e)
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
            
            Image<Bgr, Byte> img = new Image<Bgr, Byte>(bm);

            var uimage = new UMat();

            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
            var grayimage = new Image<Gray, byte>(bm);
            CvInvoke.CvtColor(img, grayimage, ColorConversion.Bgr2Gray);
            BlackBG(grayimage);
            var cannyThreshold = GetKMeansThreshold(grayimage);
            cannyThreshold = GetKMeansThreshold(grayimage);

            var cannyEdges = new UMat();

            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThreshold);

            resPicBox.Image = cannyEdges.Bitmap;

            processed = true;
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            processed = false;
            label2.Text = "";
        }

        //Bezier
        private static int Factor(int n) {
            var fact = 1;
            for (var j = 2; j <= n; j++)
            {
                fact *= j;
            }
            return fact;
        }

        private static float GetBezierBasis(int i, int n, float t)
        {
            var fact = 1;
            for (var j = 2; j <= n; j++) {
                fact *= j;
            }

            return (Factor(n) / (Factor(i) * Factor(n - i))) * (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i);
        }

        private static PointF[] GetBezierCurve(IReadOnlyList<PointF> arr, float step = 0f)
        {
            var res = new PointF[N + 1];
            var posCount = 0;
            step = (float)1 / N;

            for (var t = 0f; t < 1; t += step)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    var b = GetBezierBasis(i, arr.Count - 1, t);

                    res[posCount].X += arr[i].X * b;
                    res[posCount].Y += arr[i].Y * b;
                }
                posCount++;
            }


            return res;
        }

        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var img = !processed ? new Image<Bgr, byte>((Bitmap)srcPicBox.Image) : new Image<Bgr, byte>((Bitmap)resPicBox.Image);
            var resImage = new Image<Bgr, byte>(img.Bitmap);
            CvInvoke.BilateralFilter(img, resImage,25, 75, 75);
            resPicBox.Image = resImage.ToBitmap();
            processed = true;
        }
    }
}
