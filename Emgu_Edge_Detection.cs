using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace EMGUCV_edge_detection_11._05._2018
{
    public partial class Form1 : Form
    {

        private VideoCapture _capture;
        private Thread _captureThread;
        public Form1()
        {
            InitializeComponent();

            _capture = new VideoCapture(0);
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();
        }

        private void DisplayWebcam()
        {
            while (_capture.IsOpened)
            {
                Mat frame = _capture.QueryFrame();

                int newHeight = (frame.Size.Height * pictureBox1.Size.Width) / frame.Size.Width;
                Size newSize = new Size(pictureBox1.Size.Width, newHeight);
                CvInvoke.Resize(frame, frame, newSize);

                pictureBox1.Image = frame.Bitmap;

                // copy the source image so we can display a copy with artwork without editing the original:
                Mat Image1 = frame.Clone();
                // create an image version of the source frame, will be used when warping the image
                Image<Bgr, byte> Image2 = frame.ToImage<Bgr, byte>();

                // Isolating the ROI: convert to a gray, apply binary threshold:
                Image<Gray,byte> grayImg = frame.ToImage<Gray, byte>().ThresholdBinary(new Gray(125), new Gray(255));  
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    // Build list of contours
                    CvInvoke.FindContours(grayImg, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    // Selecting largest contour
                    if (contours.Size > 0)
                    {
                        double maxArea = 0;
                        int chosen = 0;
                        for (int i = 0; i < contours.Size; i++)
                        {
                            VectorOfPoint contour = contours[i];
                            double area = CvInvoke.ContourArea(contour);
                            if (area > maxArea)
                            {
                                maxArea = area;
                                chosen = i;
                            }
                        }

                       // float Size = contours.Size;

                        
                        
                        
                        // Getting minimal rectangle which contains the contour
                        Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[chosen]);
                        // Draw on the display frame
                        MarkDetectedObject(Image1, contours[chosen], boundingBox, maxArea);
                        // Create a slightly larger bounding rectangle, we'll set it as the ROI for later warping
                        Image2.ROI = new Rectangle((int) Math.Min(0, boundingBox.X - 30), (int) Math.Min(0, boundingBox.Y - 30), (int) Math.Max(Image2.Width - 1, boundingBox.X + boundingBox.Width + 30), (int) Math.Max(Image2.Height - 1, boundingBox.Y + boundingBox.Height + 30));
                        // Display the version of the source image with the added artwork, simulating ROI focus:
                       // pictureBox2.Image = sourceFrameWithArt.Bitmap;
                        // Warp the image, output it
                        pictureBox3.Image = Image1.Bitmap;

                        Image<Bgr, byte> Warped_ImgColor = WarpImage(Image2, contours[chosen]);
                        pictureBox2.Image = Warped_ImgColor.Bitmap;

                        Image<Gray, byte> GrayScale_WarpedImgGray = Warped_ImgColor.Mat.ToImage<Gray, byte>().ThresholdBinary(new Gray(125), new Gray(255));

                        CvInvoke.FindContours(GrayScale_WarpedImgGray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                        
                        for (int j = 0; j < contours.Size; j++)
                        {
                            VectorOfPoint contour = contours[j];
                            VectorOfPoint ApproxContour = new VectorOfPoint();

                            CvInvoke.ApproxPolyDP(contour, ApproxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);

                            var moments = CvInvoke.Moments((contours[j]));

                            int x = (int) (moments.M10 / moments.M00);
                            int y = (int) (moments.M01 / moments.M00);

                            Point Ct = new Point(x, y);

                           string text = Ct.ToString();

                            MarkDetectedObject(Warped_ImgColor.Mat, ApproxContour, new Rectangle(1, 1, 1, 1), CvInvoke.ContourArea(contour));

                            Warped_ImgColor.Draw(new CircleF(Ct,1), new Bgr(Color.CadetBlue), 5);
                            CvInvoke.PutText(Warped_ImgColor, text, Ct, FontFace.HersheyComplex, 0.08, new Bgr(Color.CadetBlue).MCvScalar);

                        }

                        Invoke(new Action(() => { label1.Text = $"{contours.Size}"; }));
                       
                    }



                   
                }
            }
        }

        private void moveArm_Bar1(object sender, EventArgs e)
        {
            if (SerialPort.IsOpen == true)
            {
                int Angle1 = 90;

            }

        }

        private static Image<Bgr, Byte> WarpImage(Image<Bgr, byte> frame, VectorOfPoint contour)
        {
            // set the output size:
            var size = new Size(frame.Width, frame.Height);
            using (VectorOfPoint approxContour = new VectorOfPoint())
            {
                CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                // get an array of points in the contour
                Point[] points = approxContour.ToArray();
                // if array length isn't 4, something went wrong, abort warping process (for demo, draw points instead)
                if (points.Length != 4)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        frame.Draw(new CircleF(points[i], 5), new Bgr(Color.Red), 5);
                    }
                    return frame;
                }

                IEnumerable<Point> query = points.OrderBy(point => point.Y).ThenBy(point => point.X);
                PointF[] ptsSrc = new PointF[4];
                PointF[] ptsDst = new PointF[] { new PointF(0, 0), new PointF(size.Width - 1, 0), new PointF(0, size.Height - 1),
                        new PointF(size.Width - 1, size.Height - 1) };
                for (int i = 0; i < 4; i++)
                {
                    ptsSrc[i] = new PointF(query.ElementAt(i).X, query.ElementAt(i).Y);
                }
                using (var matrix = CvInvoke.GetPerspectiveTransform(ptsSrc, ptsDst))
                {
                    using (var cutImagePortion = new Mat())
                    {
                        CvInvoke.WarpPerspective(frame, cutImagePortion, matrix, size, Inter.Cubic);
                        return cutImagePortion.ToImage<Bgr, Byte>();
                    }
                } 
            }
        }
        private void MarkDetectedObject(Mat frame, VectorOfPoint ApproxContour, Rectangle boundingBox, double area)
        {
            // Drawing contour and box around it

            Color clr = Color.Blue;
            if (ApproxContour.Size == 4)
            {
                clr = Color.Red;
            }
            else if (ApproxContour.Size == 3)
            {
                clr = Color.Green;
            }

            CvInvoke.Polylines(frame, ApproxContour, true, new Bgr(clr).MCvScalar);
            CvInvoke.Rectangle(frame, boundingBox, new Bgr(Color.Purple).MCvScalar);
            // Write information next to marked object
            Point center = new Point(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2);

           
           
        }
        private static void WriteMultilineText(Mat frame, string[] lines, Point origin)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int y = i * 10 + origin.Y; // Moving down on each line
                CvInvoke.PutText(frame, lines[i], new Point(origin.X, y),
                                 FontFace.HersheyPlain, 0.8, new Bgr(Color.Red).MCvScalar);
            }
        }


      
       // private static void ContourCount(Mat frame, )

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _captureThread.Abort();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
