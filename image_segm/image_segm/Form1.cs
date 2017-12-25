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


        private void process(Bitmap bm, int level, double cannyThreshold = 100.0, double circleAccumulatorThreshold = 100, double cannyThresholdLinking = 120.0)
        {
            Image<Bgr, Byte> img = new Image<Bgr, Byte>(bm);
            
            //Convert the image to grayscale and filter out the noise
            UMat uimage = new UMat();
            CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);

            
            
            CircleF[] circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 3.0, cannyThreshold, circleAccumulatorThreshold, 1);

            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
               cannyEdges,
               1, //Distance resolution in pixel-related units
               Math.PI / 45.0, //Angle resolution measured in radians.
               20, //threshold
               30, //min Line width
               10); //gap between lines

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
                        if (CvInvoke.ContourArea(approxContour, false) > 20)
                        {
                            if (approxContour.Size == 3)
                            {
                                Point[] pts = approxContour.ToArray();
                                triangleList.Add(new Triangle2DF(
                                   pts[0],
                                   pts[1],
                                   pts[2]
                                   ));
                            }
                            else if (approxContour.Size == 4)
                            {
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(
                                       edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 85 || angle > 95)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                            }
                        }
                    }
                }
            }
            List<RotatedRect> deleted = new List<RotatedRect>();

            for (int i = 0; i < boxList.Count; i++)
            {
                RotatedRect box = boxList[i];
                for (int j = 0; j < circles.Length; j++)
                {
                    CircleF circle = circles[j];
                    if(Math.Abs(box.Center.X - circle.Center.X) < 10 && Math.Abs(box.Center.Y - circle.Center.Y) < 10)
                    {
                        deleted.Add(box);
                    }
                }
            }
            
            for (int i = 0; i<deleted.Count; i++)
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
                    if (Math.Abs(boxArea(box1) - boxArea(box2)) < 3000 && Math.Abs(box1.Center.X - box2.Center.X) < 10 && Math.Abs(box1.Center.Y - box2.Center.Y) < 10 )
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
                for (int j = i+1; j < triangleList.Count; j++)
                {
                    Triangle2DF tri2 = triangleList[j];
                    if (Math.Abs(tri1.Centeroid.X - tri2.Centeroid.X) < 10 && Math.Abs(tri1.Centeroid.Y - tri2.Centeroid.Y) < 10)
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
                if(Math.Abs(boxArea(box)-img.Height*img.Width) < 3000)
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



            List<PointF> points = new List<PointF>();

            Image<Bgr, Byte> Image = img.CopyBlank();
            foreach (Triangle2DF triangle in triangleList)
            {
                Image.Draw(triangle, new Bgr(Color.DarkBlue), 2);
                points.Add(triangle.Centeroid);
            }
                
            foreach (RotatedRect box in boxList)
            {
                Image.Draw(box, new Bgr(Color.DarkOrange), 2);
                points.Add(box.Center);
            }
                
            foreach (CircleF circle in circles)
            {
                Image.Draw(circle, new Bgr(Color.Brown), 2);
                points.Add(circle.Center);
            }

            points.Sort((a, b) => a.X.CompareTo(b.X));
            PointF[] listPoints = new PointF[points.Count];
            listPoints[0] = points[0];
            int posCount = 0;
            double minDist = img.Height * img.Width;
            bool[] visited = new bool [points.Count];
            for(int i = 0; i< visited.Length; i++)
            {
                visited[i] = false;
            }
            visited[0] = true;

            for ( int i = 0; i<points.Count - 1; i++)
            {
                posCount++;
                int nxtPointIndex = i + 1;
                for (int j = i + 1; j<points.Count; j++)
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

            resPicBox.Image = (Image).ToBitmap();
           
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
            process((Bitmap)srcPicBox.Image, 0);
        }

        private void processMediumToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void processManiacToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
