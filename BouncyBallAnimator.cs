using ImageProcessor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ozone
{
    class BouncyBallAnimator
    {
        public static Stream Animate(Image img)
        {
            Stream Frame = new MemoryStream();

            var Format = new ImageProcessor.Imaging.Formats.PngFormat();

            var Img = new ImageFactory();

            var Gif = new Gifed.AnimatedGif();

            var Out = new MemoryStream();

            foreach (int Num in Enumerable.Range(1, 30))
            {
                Img.Load(img);
                Img.Constrain(new Size(img.Width + 50, img.Height + 50));
                Img.Rotate(Num * 12);
                Img.Resize(new Size(500, 500));
                Img.BackgroundColor(Color.White);
                Img.Save(Frame);
                Gif.AddFrame(Image.FromStream(Frame), TimeSpan.FromMilliseconds(33));
            }

            Gif.Save(Out);

            Img.Dispose();
            Gif.Dispose();

            Out.Position = 0;

            return Out;

        }
    }
}
