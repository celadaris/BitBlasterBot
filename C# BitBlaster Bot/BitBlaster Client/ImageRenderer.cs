using System.Drawing.Imaging;
using System.Drawing;
using ScreenCapturerNS;

public class ImageRenderer
{
    public ImageRenderer()
    {
        MainThreadNetcode.timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        ScreenCapturer.StartCapture((Bitmap bitmap) =>
        {
            PacketConfig packetConfig = new PacketConfig()
            {
                packetFlag = ENet.PacketFlags.None
            };

            MemoryStream ms = new MemoryStream();
            Bitmap smallImg = Resize(bitmap);
            Bitmap grayImg = MakeGrayscale3(smallImg);
            grayImg.Save(ms, ImageFormat.Jpeg);
            byte[] buffer = ms.ToArray();

            VideoPacket videoPacket = new VideoPacket()
            {
                packetID = 3,
                arraySize = (ushort)buffer.Length,
                videoFeed = buffer,
            };

            //send image to server
            Courier.SendVideoPacket(packetConfig, videoPacket);
        });
    }

    //https://stackoverflow.com/a/2265990
    public static Bitmap MakeGrayscale3(Bitmap original)
    {
        //create a blank bitmap the same size as original
        Bitmap newBitmap = new Bitmap(original.Width, original.Height);

        //get a graphics object from the new image
        using (Graphics g = Graphics.FromImage(newBitmap))
        {

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            using (ImageAttributes attributes = new ImageAttributes())
            {

                //set the color matrix attribute
                attributes.SetColorMatrix(colorMatrix);

                //draw the original image on the new image
                //using the grayscale color matrix
                g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                            0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        return newBitmap;
    }

    Bitmap Resize(Bitmap original)
    {
        return new Bitmap(original, new Size(120, 120));
    }
}
