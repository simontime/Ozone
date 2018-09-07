using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Filters.Artistic;
using ImageProcessor.Imaging.Filters.EdgeDetection;
using ImageProcessor.Imaging.Filters.Photo;
using ImageProcessor.Imaging.Helpers;

namespace Ozone
{
    // Heavily based on the comic filter

    internal class OkamiFilter : MatrixFilterBase
    {

        public static ColorMatrix OkamiLow
        {
            get
            {
                return new ColorMatrix(
                       new[]
                            {
                                new float[] { 1, 0, 0, 0, 0 }, 
                                new float[] { 0, 1, 0, 0, 0 },  
                                new float[] { 0, 0, 1, 0, 0 }, 
                                new float[] { 0, 0, 0, 1, 0 },
                                new[] { .075f, .075f, .075f, 0, 1 }
                            });
            }
        }
        public static ColorMatrix OkamiHigh
        {
            get
            {
                return new ColorMatrix(
                    new[]
                            {
                                new[] { 2, -0.5f, -0.5f, 0, 0 },
                                new[] { -0.5f, 2, -0.5f, 0, 0 },
                                new[] { -0.5f, -0.5f, 2, 0, 0 },
                                new float[] { 0, 0, 0, 1, 0 },
                                new float[] { 0, 0, 0, 0, 1 }
                            });
            }
        }
        public static ColorMatrix InvertColours
        {
            get
            {
                return new ColorMatrix(
                    new[]
                            {
                                new float[] { -1, 0, 0, 0, 0 },
                                new float[] { 0, -1, 0, 0, 0 },
                                new float[] { 0, 0, -1, 0, 0 },
                                new float[] { 0, 0, 0, 1, 0 },
                                new float[] { 1, 1, 1, 0, 1 }
                            });
            }
        }

        public override ColorMatrix Matrix => throw new System.NotImplementedException();

        public static Bitmap Invert(Image source, Image destination)
        {
            using (Graphics graphics = Graphics.FromImage(destination))
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(InvertColours);
                    Rectangle rectangle = new Rectangle(0, 0, source.Width, source.Height);
                    graphics.DrawImage(source, rectangle, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return (Bitmap)destination;
        }
        public override Bitmap TransformImage(Image image, Image newImage)
        {
            Bitmap highBitmap = null;
            Bitmap lowBitmap = null;
            Bitmap patternBitmap = null;
            Bitmap edgeBitmap = null;
            int width = image.Width;
            int height = image.Height;
            try
            {
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    Rectangle rectangle = new Rectangle(0, 0, image.Width, image.Height);
                    attributes.SetColorMatrix(OkamiHigh);                 
                    highBitmap = new Bitmap(rectangle.Width, rectangle.Height);
                    highBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    highBitmap = new OilPaintingFilter(3, 5).ApplyFilter((Bitmap)image);
                    edgeBitmap = new Bitmap(width, height);
                    edgeBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    edgeBitmap = Trace(image, edgeBitmap, 120);
                    using (Graphics graphics = Graphics.FromImage(highBitmap))
                    {
                        graphics.DrawImage(highBitmap, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                    }
                    lowBitmap = new Bitmap(rectangle.Width, rectangle.Height);
                    lowBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    attributes.SetColorMatrix(OkamiLow);
                    using (Graphics graphics = Graphics.FromImage(lowBitmap))
                    {
                        graphics.DrawImage(highBitmap, rectangle, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                    }
                    patternBitmap = new Bitmap(rectangle.Width, rectangle.Height);
                    patternBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                    using (Graphics graphics = Graphics.FromImage(patternBitmap))
                    {
                        graphics.Clear(Color.Transparent);
                        graphics.SmoothingMode = SmoothingMode.HighQuality;

                        for (int y = 0; y < height; y += 8)
                        {
                            for (int x = 0; x < width; x += 4)
                            {
                                graphics.FillEllipse(Brushes.White, x, y, 3, 3);
                                graphics.FillEllipse(Brushes.White, x + 2, y + 4, 3, 3);
                            }
                        }
                    }           
                    lowBitmap = Effects.ApplyMask(lowBitmap, patternBitmap);
                    using (Graphics graphics = Graphics.FromImage(newImage))
                    {
                        graphics.Clear(Color.Transparent);
                        graphics.DrawImage(highBitmap, 0, 0);
                        graphics.DrawImage(lowBitmap, 0, 0);
                        graphics.DrawImage(edgeBitmap, 0, 0);
                        using (Pen blackPen = new Pen(Color.Black))
                        {
                            blackPen.Width = 4;
                            graphics.DrawRectangle(blackPen, rectangle);
                        }
                        highBitmap.Dispose();
                        lowBitmap.Dispose();
                        patternBitmap.Dispose();
                        edgeBitmap.Dispose();
                    }
                }
                image.Dispose();
                image = newImage;
            }
            catch
            {
                if (newImage != null)
                {
                    newImage.Dispose();
                }
                if (highBitmap != null)
                {
                    highBitmap.Dispose();
                }
                if (lowBitmap != null)
                {
                    lowBitmap.Dispose();
                }
                if (patternBitmap != null)
                {
                    patternBitmap.Dispose();
                }
                if (edgeBitmap != null)
                {
                    edgeBitmap.Dispose();
                }
            }
            return (Bitmap)image;
        }	
        public static Bitmap Trace(Image source, Image destination, byte threshold = 0)
        {
            int width = source.Width;
            int height = source.Height;    
            ConvolutionFilter filter = new ConvolutionFilter(new SobelEdgeFilter(), true);
            using (Bitmap temp = filter.Process2DFilter(source))
            {
                destination = Invert(temp, destination);    
                destination = Adjustments.Brightness(destination, -5);
            }
            using (FastBitmap destinationBitmap = new FastBitmap(destination))
            {
                Parallel.For(
                    0,
                    height,
                    y =>
                    {
                        for (int x = 0; x < width; x++)
                        {                    
                            Color color = destinationBitmap.GetPixel(x, y);
                            if (color.B >= threshold)
                            {
                                destinationBitmap.SetPixel(x, y, Color.Transparent);
                            }                          
                        }
                    });
            }
            destination = Adjustments.Brightness(destination, -5);
            return (Bitmap)destination;
        }
    }
}