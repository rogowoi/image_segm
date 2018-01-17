using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Configuration;
using System.Windows.Forms;
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

        private static void FilterSame(List<RotatedRect> boxList, IList<Triangle2DF> triangleList, List<CircleF> circles, int imageSize, int areaDiff =15000, double threshold = 10)
        {
            if (boxList == null) throw new ArgumentNullException(nameof(boxList));
            if (triangleList == null) throw new ArgumentNullException(nameof(triangleList));
            if (circles == null) throw new ArgumentNullException(nameof(circles));

            var deleted = (from box in boxList from circle in circles where Math.Abs(box.Center.X - circle.Center.X) < threshold && Math.Abs(box.Center.Y - circle.Center.Y) < threshold select box).ToList();

            foreach (var box in deleted)
            {
                if (boxList.Contains(box))
                {
                    boxList.Remove(box);
                }
            }
            deleted.Clear();
            //List<CircleF> circleList = circles.ToList();
            Console.WriteLine("Circles");
            Console.WriteLine(circles.Count);

            var deletedC = new List<CircleF>();

            for (var i = 0; i < circles.Count; i++)
            {
                var c1 = circles[i];
                for (var j = i + 1; j < circles.Count; j++)
                {
                    var c2 = circles[j];
                    if (Math.Abs(c1.Center.X - c2.Center.X) < Math.Max(c1.Radius, c2.Radius) && Math.Abs(c1.Center.Y - c2.Center.Y) < Math.Max(c1.Radius, c2.Radius))
                    {
                        deletedC.Add(c1);
                    }
                }
            }

            foreach (var circle in deletedC)
            {
                if (circles.Contains(circle))
                {
                    circles.Remove(circle);
                }
            }

            deletedC.Clear();
            //circles = circleList.ToArray();
            Console.WriteLine(circles.Count);

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

        private static PointF[] SortPoints(List<PointF> points, Image<Bgr, byte> image)
        {
            points.Sort((a, b) => a.X.CompareTo(b.X));
            var listPoints = new PointF[points.Count];
            var visited = new bool[points.Count];
            for (var i = 0; i < visited.Length; i++)
            {
                visited[i] = false;
            }
            bool yellow = false;
            /*for (int i = 0; i<points.Count; i++)
            {
                float x = points[i].X;
                float y = points[i].Y;
                if (image[(int)y, (int)x].Red == 255 && image[(int)y, (int)x].Green == 255)
                {
                    listPoints[0] = points[i];
                    yellow = true;
                    visited[i] = true;
                }
            }*/
            if (!yellow)
            {
                listPoints[0] = points[0];
                visited[0] = true;

            }

            var posCount = 0;


            for (var i = 0; i < points.Count - 1; i++)
            {
                double minDist = 1000000;
                posCount++;
                var nxtPointIndex = i + 1;
                for (var j = 0; j < points.Count; j++)
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
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 30, 80, 80);
                CvInvoke.MedianBlur(img, img, 5);
                resImage = img;
            }
            else if (level == 2)
            {
                CvInvoke.MedianBlur(img, img, 5);
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 25, 75, 75);
                CvInvoke.Blur(img, img, new Size(5, 5), new Point(0, 0));
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
            
            var cannyEdges = new UMat();
            
            Console.WriteLine("Canny threshold using KMEANS found " + cannyThreshold);

            var uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);
            
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThreshold);
                
            BlobCounter blobCounter = new BlobCounter( );
            if (level == 1)
            {
                blobCounter.FilterBlobs = true;
                blobCounter.MinHeight = 25;
                blobCounter.MinWidth = 25;
                blobCounter.ProcessImage(cannyEdges.Bitmap);

            }
            else
            {
                blobCounter.ProcessImage(grayimage.ToBitmap());
            }            
            //blobCounter.ProcessImage(grayimage.ToBitmap());
            
            Blob[] blobs = blobCounter.GetObjectsInformation( );
            
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            
            var triangleList = new List<Triangle2DF>();
            var boxList = new List<RotatedRect>();
            var circleList = new List<CircleF>();
            Bitmap newBM = new Bitmap(img.Bitmap);
            Graphics g = Graphics.FromImage(newBM);
            Pen redPen = new Pen(Color.Red, 2);
            
            
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
                    //g.DrawEllipse(bluePen,
                    //    (float)(center.X - radius), (float)(center.Y - radius),
                    //    (float)(radius * 2), (float)(radius * 2));
                    circleList.Add(new CircleF(new PointF(center.X, center.Y), radius));
                }
                else
                {
                    List<IntPoint> corners;
                    if (edgePoints.Count > 1)
                    {
                        if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                        {
                            System.Console.WriteLine(corners.Count);
                            if (shapeChecker.CheckPolygonSubType(corners) ==
                                PolygonSubType.Square || shapeChecker.CheckPolygonSubType(corners) ==
                                PolygonSubType.Rectangle)
                            {
                                IntPoint minXY, maxXY;
                                
                                PointsCloud.GetBoundingRectangle(corners, out minXY, out maxXY);
                                AForge.Point c = PointsCloud.GetCenterOfGravity(corners);
                                //g.DrawPolygon(greenPen, ToPointsArray(corners));
                                boxList.Add(new RotatedRect(new PointF(c.X, c.Y), new SizeF(maxXY.X - minXY.X, maxXY.Y - minXY.Y),0));
                            }
                            
                        }
                        else
                        {
                            corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                            if (corners.Count == 3)
                            {
                                Triangle2DF tri = new Triangle2DF(new PointF(corners[0].X, corners[0].Y), new PointF(corners[1].X, corners[1].Y), new PointF(corners[2].X, corners[2].Y));
                                triangleList.Add(tri);
                                //g.DrawPolygon(yellowPen, ToPointsArray(corners));
                            }
                            //g.DrawPolygon(redPen, ToPointsArray(corners));
                        }
                    }
                    
                }
            }
            Console.WriteLine("boxes "+boxList.Count);
            Console.WriteLine("triangles "+triangleList.Count);
            Console.WriteLine("circles "+circleList.Count);

            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            yellowPen.Dispose();
            //g.Dispose();
            resPicBox.Image = newBM;
            CircleF[] circles = circleList.ToArray();
            var cList = circles.ToList();
            FilterSame(boxList, triangleList, cList, img.Width * img.Height);
            circles = cList.ToArray();
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

            var listPoints = SortPoints(points, img);

            for (var i = 0; i < listPoints.Length; i++)
            {
                Console.WriteLine(listPoints[i].X.ToString() + " " + listPoints[i].Y.ToString());
            }

            System.Console.WriteLine("Points sorted, num of objects " + listPoints.Length.ToString());
            resPicBox.Image = (Image + img).ToBitmap();
            if (listPoints.Length > 3)
            {
                var bezSegList = InterpolatePointWithBeizerCurves(listPoints.ToList<PointF>());
                var gr = Graphics.FromImage(resPicBox.Image);
                var p = new Pen(Color.Red);

                foreach (BeizerCurveSegment seg in bezSegList)
                {

                    var bezierList = GetBez(new PointF[]
                        {seg.StartPoint, seg.FirstControlPoint, seg.SecondControlPoint, seg.EndPoint});
                    for (var i = 0; i < bezierList.Length - 1; i++)
                    {
                        gr.DrawLine(p, bezierList[i], bezierList[i + 1]);
                    }

                }
            }
            else
            {
                var gr = Graphics.FromImage(resPicBox.Image);
                var p = new Pen(Color.Red);

                for (var i = 0; i < listPoints.Length - 1; i++)
                {
                    gr.DrawLine(p, listPoints[i], listPoints[i + 1]);
                }
            }
            
            //var bezierList = GetBezierCurve1(listPoints);
            

        }
        
        private void Process(Bitmap bm, int level, double circleAccumulatorThreshold = 70.0, int maxRadius = 0)
        {
            double cannyThreshold = 0;
            var img = new Image<Bgr, byte>(bm);
            if (level == 1)
            {
                var resImage = new Image<Bgr, byte>(img.Bitmap);
                CvInvoke.BilateralFilter(resImage, img, 30, 75, 75);
                CvInvoke.MedianBlur(img, img, 5);
                resImage = img;
                
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
            //uimage = grayimage.ToUMat();
            //resPicBox.Image = grayimage.Bitmap;

            var circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 5.0, cannyThreshold, circleAccumulatorThreshold, 1, maxRadius);
            
            Console.WriteLine("Circles found " + circles.Length.ToString());

            var cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThreshold);
            
            
            var lines = CvInvoke.HoughLinesP(uimage,
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

            var cList = circles.ToList();
            FilterSame(boxList, triangleList, cList, img.Width * img.Height);
            circles = cList.ToArray();
            
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

            var listPoints = SortPoints(points, img);


            System.Console.WriteLine("Points sorted, num of objects " + listPoints.Length.ToString());

            resPicBox.Image = (Image+img).ToBitmap();
            if (listPoints.Length > 3)
            {
                var bezSegList = InterpolatePointWithBeizerCurves(listPoints.ToList<PointF>());
                var gr = Graphics.FromImage(resPicBox.Image);
                var p = new Pen(Color.Red);

                foreach (BeizerCurveSegment seg in bezSegList)
                {

                    var bezierList = GetBez(new PointF[]
                        {seg.StartPoint, seg.FirstControlPoint, seg.SecondControlPoint, seg.EndPoint});
                    for (var i = 0; i < bezierList.Length - 1; i++)
                    {
                        gr.DrawLine(p, bezierList[i], bezierList[i + 1]);
                    }

                }
            }
            else
            {
                var gr = Graphics.FromImage(resPicBox.Image);
                var p = new Pen(Color.Red);

                for (var i = 0; i < listPoints.Length - 1; i++)
                {
                    gr.DrawLine(p, listPoints[i], listPoints[i + 1]);
                }
            }
            
            
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
            altProcess((Bitmap)srcPicBox.Image, 1);
        }

        private void processManiacToolStripMenuItem_Click(object sender, EventArgs e)
        {
            altProcess((Bitmap)srcPicBox.Image, 2);
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

        private static PointF[] GetBezierCurve1(IReadOnlyList<PointF> arr, float step = 0f)
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
        

        private int n = 100;
        private PointF[] getBezierCurve(PointF[] arr, float step = 0f)
        {
            PointF[] res = new PointF[n + 1];
            int posCount = 0;
            step = (float)1 / n;

            float d = 0;
            for (int i = 0; i < arr.Length - 1; i++) {
                    d += (float)GetDistance(arr[i], arr[i+1]); ;
            }
            float t1 = (float)GetDistance(arr[0], arr[1])/d + (float)GetDistance(arr[2], arr[1]) / d + (float)GetDistance(arr[2], arr[3]) / d;
            float t3 = (float)GetDistance(arr[2], arr[1])/d;
            float t2 = 1 - (float)GetDistance(arr[arr.Length - 2], arr[arr.Length - 1]) / d - (float)GetDistance(arr[arr.Length - 2], arr[arr.Length - 3]) / d;

            float a1 = 3 * (1 - t1) * (1 - t1) * t1;
            float a2 = 3 * (1 - t2) * (1 - t2) * t2;
            float a3 = 3 * (1 - t3) * (1 - t3) * t3;

            float b1 = 3 * (1 - t1) * t1 * t1;
            float b2 = 3 * (1 - t2) * t2 * t2;
            float b3 = 3 * (1 - t3) * t3 * t3;


            float x1 = (float)(((arr[3].X - Math.Pow(1 - t1, 3) * arr[0].X - Math.Pow(t1, 3) * arr[arr.Length - 1].X) * b2
                - (arr[arr.Length - 3].X - Math.Pow(1 - t2, 3) * arr[0].X - Math.Pow(t2, 3) * arr[arr.Length - 1].X) * b1) / (a1 * b2 - a2 * b1));
            float y1 = (float)(((arr[3].Y - Math.Pow(1 - t1, 3) * arr[0].Y - Math.Pow(t1, 3) * arr[arr.Length - 1].Y) * b2
                - (arr[arr.Length - 3].Y - Math.Pow(1 - t2, 3) * arr[0].Y - Math.Pow(t2, 3) * arr[arr.Length - 1].Y) * b1) / (a1 * b2 - a2 * b1));


            float x2 = (float)(((arr[3].X - Math.Pow(1 - t1, 3) * arr[0].X - Math.Pow(t1, 3) * arr[arr.Length - 1].X) * a2
                - (arr[arr.Length - 3].X - Math.Pow(1 - t2, 3) * arr[0].X - Math.Pow(t2, 3) * arr[arr.Length - 1].X) * a1) / (b1 * a2 - b2 * a1));

            float y2 = (float)(((arr[3].Y - Math.Pow(1 - t1, 3) * arr[0].Y - Math.Pow(t1, 3) * arr[arr.Length - 1].Y) * a2
                - (arr[arr.Length - 3].Y - Math.Pow(1 - t2, 3) * arr[0].Y - Math.Pow(t2, 3) * arr[arr.Length - 1].Y) * a1) / (b1 * a2 - b2 * a1));


            float x3 = (float)(((arr[1].X - Math.Pow(1 - t3, 3) * arr[0].X - Math.Pow(t3, 3) * arr[arr.Length - 1].X) * a2
                - (arr[arr.Length - 3].X - Math.Pow(1 - t2, 3) * arr[0].X - Math.Pow(t2, 3) * arr[arr.Length - 1].X) * a3) / (b3 * a2 - b2 * a3));

            float y3 = (float)(((arr[1].Y - Math.Pow(1 - t3, 3) * arr[0].Y - Math.Pow(t3, 3) * arr[arr.Length - 1].Y) * a2
                - (arr[arr.Length - 3].Y - Math.Pow(1 - t2, 3) * arr[0].Y - Math.Pow(t2, 3) * arr[arr.Length - 1].Y) * a3) / (b3 * a2 - b2 * a3));



            /*float x1 = (float)(((arr[1].X - Math.Pow(1 - t1, 3) * arr[0].X - Math.Pow(t1, 3) * arr[arr.Length - 1].X)*3*(1-t2)*t2*t2 
                - (arr[1].X - Math.Pow(1 - t2, 3) * arr[0].X - Math.Pow(t2, 3) * arr[arr.Length - 1].X) * 3 * (1 - t1) * t1 * t1)/(9*(1-t1)*(1-t1)*t1*(1-t2)*t2*t2 - 9*(1-t2)*(1-t2)*t2*(1-t1)*t1*t1));
            float y1 = (float)(((arr[1].Y - Math.Pow(1 - t1, 3) * arr[0].Y - Math.Pow(t1, 3) * arr[arr.Length - 1].Y) * (1 - t2) * t2 * t2*3
                - (arr[1].Y - Math.Pow(1 - t2, 3) * arr[0].Y - Math.Pow(t2, 3) * arr[arr.Length - 1].Y) * 3*(1 - t1) * t1 * t1) / (9*(1 - t1) * (1 - t1) * t1 * (1 - t2) * t2 * t2 - 9*(1 - t2) * (1 - t2) * t2 * (1 - t1) * t1 * t1));


            float x2 = (float)(((arr[1].X - Math.Pow(1 - t1, 3) * arr[0].X - Math.Pow(t1, 3) * arr[arr.Length - 1].X) * (1 - t2) * (1-t2) * t2*3
                - (arr[1].X - Math.Pow(1 - t2, 3) * arr[0].X - Math.Pow(t2, 3) * arr[arr.Length - 1].X) * 3*(1 - t1) * (1-t1) * t1) / (-9*(1 - t1) * (1 - t1) * t1 * (1 - t2) * t2 * t2 + 9*(1 - t2) * (1 - t2) * t2 * (1 - t1) * t1 * t1));

            float y2 = (float)(((arr[1].Y - Math.Pow(1 - t1, 3) * arr[0].Y - Math.Pow(t1, 3) * arr[arr.Length - 1].Y) * (1 - t2) * (1-t2) * t2*3
                - (arr[1].Y - Math.Pow(1 - t2, 3) * arr[0].Y - Math.Pow(t2, 3) * arr[arr.Length - 1].Y) * 3*(1 - t1) * (1-t1) * t1) / (-9*(1 - t1) * (1 - t1) * t1 * (1 - t2) * t2 * t2 + 9*(1 - t2) * (1 - t2) * t2 * (1 - t1) * t1 * t1));
                */
            System.Console.WriteLine("p1: " + x1 + ' ' + y1);
            System.Console.WriteLine("p2: " + x2 + ' ' + y2);
            System.Console.WriteLine("p3: " + x3 + ' ' + y3);

            PointF p1 = new PointF(x1, y1);
            PointF p2 = new PointF(x2, y2);
            PointF p3 = new PointF(x3, y3);

            PointF[] points = new PointF[5];

            points[0] = arr[0];
            points[1] = p1;
            points[2] = p3;
            points[3] = p2;
            points[4] = arr[arr.Length - 1];

            for (float t = 0f; t < 1; t += step)
            {

                for (int i = 0; i < points.Length; i++)
                {
                    float b = GetBezierBasis(i, points.Length - 1, t);

                    res[posCount].X += points[i].X * b;
                    res[posCount].Y += points[i].Y * b;
                }
                //System.Console.WriteLine("t = " + t);
                posCount++;
            }


            return res;
        }


        public class BeizerCurveSegment
        {
            public System.Drawing.PointF StartPoint { get; set; }
            public System.Drawing.PointF EndPoint { get; set; }
            public System.Drawing.PointF FirstControlPoint { get; set; }
            public System.Drawing.PointF SecondControlPoint { get; set; }
        }
        
         List<BeizerCurveSegment> InterpolatePointWithBeizerCurves(List<PointF> points)
                {
            if (points.Count < 3)
                return null;
            var toRet = new List<BeizerCurveSegment>();



            for (int i = 0; i < points.Count - 1; i++)
            {

                float x1 = points[i].X;
                float y1 = points[i].Y;

                float x2 = points[i + 1].X;
                float y2 = points[i + 1].Y;

                float x0;
                float y0;

                if (i == 0)
                {

                    {
                        var previousPoint = points[i];
                        x0 = previousPoint.X;
                        y0 = previousPoint.Y;
                    }
                }
                else
                {
                    x0 = points[i - 1].X;
                    y0 = points[i - 1].Y;
                }

                float x3, y3;

                if (i == points.Count - 2)
                {
                    {
                        var nextPoint = points[i + 1];
                        x3 = nextPoint.X;
                        y3 = nextPoint.Y;
                    }
                }
                else
                {
                    x3 = points[i + 2].X;
                    y3 = points[i + 2].Y;
                }

                float xc1 = (x0 + x1) / (float)2.0;
                float yc1 = (y0 + y1) / (float)2.0;
                float xc2 = (x1 + x2) / (float)2.0;
                float yc2 = (y1 + y2) / (float)2.0;
                float xc3 = (x2 + x3) / (float)2.0;
                float yc3 = (y2 + y3) / (float)2.0;

                float len1 = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
                float len2 = (float)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                float len3 = (float)Math.Sqrt((x3 - x2) * (x3 - x2) + (y3 - y2) * (y3 - y2));

                float k1 = len1 / (len1 + len2);
                float k2 = len2 / (len2 + len3);

                float xm1 = xc1 + (xc2 - xc1) * k1;
                float ym1 = yc1 + (yc2 - yc1) * k1;

                float xm2 = xc2 + (xc3 - xc2) * k2;
                float ym2 = yc2 + (yc3 - yc2) * k2;

                const float smoothValue = (float)0.8;

                float ctrl1_x = xm1 + (xc2 - xm1) * smoothValue + x1 - xm1;
                float ctrl1_y = ym1 + (yc2 - ym1) * smoothValue + y1 - ym1;

                float ctrl2_x = xm2 + (xc2 - xm2) * smoothValue + x2 - xm2;
                float ctrl2_y = ym2 + (yc2 - ym2) * smoothValue + y2 - ym2;
                toRet.Add(new BeizerCurveSegment
                {
                    StartPoint = new PointF(x1, y1),
                    EndPoint = new PointF(x2, y2),
                    FirstControlPoint = i == 0 ? new PointF(x1, y1) : new PointF(ctrl1_x, ctrl1_y),
                    SecondControlPoint = i == points.Count - 2 ? new PointF(x2, y2) : new PointF(ctrl2_x, ctrl2_y)
                });
            }

            return toRet;
        }

        

        int Fuctorial(int n)
        {
            int res = 1;
            for (int i = 1; i <= n; i++)
                res *= i;
            return res;
        }
        private float Ber(int i, int n, float t)
        {
            return (Fuctorial(n) / (Fuctorial(i) * Fuctorial(n - i))) * (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i);
        }
        
        

        private PointF[] GetBez(PointF[] arr, float step = 0f)
        {
            int N = 100;
            var res = new PointF[N + 1];
            var posCount = 0;

            step = (float)1 / N;
            for (var t = 0f; t < 1; t += step)
            {
                for (var i = 0; i < arr.Count(); i++)
                {
                    var b = Ber(i, arr.Count() - 1, t);

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
