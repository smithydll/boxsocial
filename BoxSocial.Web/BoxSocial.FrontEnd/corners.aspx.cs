/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web;

namespace BoxSocial.FrontEnd
{
    public partial class corners : System.Web.UI.Page
    {
        public corners()
            : base()
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected Color headColour;
        protected void Page_Load(object sender, EventArgs e)
        {
            string ext = Request.QueryString["ext"];
            Response.Clear();
            //ColorPalette cp;

            if (ext == "gif")
            {
                Response.ContentType = "image/gif";
            }
            else
            {
                Response.ContentType = "image/png";
            }

            int width = 0;
            int.TryParse(Request.QueryString["width"], out width);
            int cornerSize = 0;
            int.TryParse(Request.QueryString["roundness"], out cornerSize);
            string colour = Request.QueryString["colour"];

            headColour = Color.FromArgb((int)Convert.ToByte(colour.Substring(0, 2), 16),
                (int)Convert.ToByte(colour.Substring(2, 2), 16),
                (int)Convert.ToByte(colour.Substring(4, 2), 16));

            string imagePath;
            if (ext == "gif")
            {
                imagePath = Path.Combine(Path.Combine(Server.MapPath("./"), "images"), string.Format("corners-{0}-{1}-{2}-{3}.gif",
                    Request.QueryString["location"], colour, width, cornerSize));
            }
            else
            {
                imagePath = Path.Combine(Path.Combine(Server.MapPath("./"), "images"), string.Format("corners-{0}-{1}-{2}-{3}.png",
                    Request.QueryString["location"], colour, width, cornerSize));
            }
            if (File.Exists(imagePath))
            {
                Response.TransmitFile(imagePath);
                Response.End();
            }

            Bitmap cornerImage = new Bitmap(width, cornerSize, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(cornerImage);
            g.Clear(Color.Transparent);
            if (ext == "gif")
            {
                g.SmoothingMode = SmoothingMode.None;
            }
            else
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
            }

            if (Request["location"] == "top")
            {
                g.FillEllipse(new SolidBrush(headColour), 0, 0, cornerSize * 2, cornerSize * 2);
                g.FillEllipse(new SolidBrush(headColour), width - cornerSize * 2 - 1, 0, cornerSize * 2, cornerSize * 2);
                g.FillRectangle(new SolidBrush(headColour), cornerSize, -1, width - cornerSize * 2 - 1, cornerSize + 2);
            }
            else if (Request["location"] == "top,left")
            {
                g.FillEllipse(new SolidBrush(headColour), 0, 0, cornerSize * 2, cornerSize * 2);
            }
            else if (Request["location"] == "top,right")
            {
                g.FillEllipse(new SolidBrush(headColour), -cornerSize, 0, cornerSize * 2, cornerSize * 2);
            }
            else if (Request["location"] == "bottom")
            {
                g.FillEllipse(new SolidBrush(headColour), 0, -cornerSize, cornerSize * 2, cornerSize * 2 - 1);
                g.FillEllipse(new SolidBrush(headColour), width - cornerSize * 2 - 1, -cornerSize - 1, cornerSize * 2, cornerSize * 2);
                g.FillRectangle(new SolidBrush(headColour), cornerSize, -1, width - cornerSize * 2, cornerSize + 2);
            }
            else if (Request["location"] == "bottom,left")
            {
                g.FillEllipse(new SolidBrush(headColour), 0, -cornerSize, cornerSize * 2, cornerSize * 2 - 1);
            }
            else if (Request["location"] == "bottom,right")
            {
                g.FillEllipse(new SolidBrush(headColour), -cornerSize, -cornerSize - 1, cornerSize * 2, cornerSize * 2);
            }
            else if (Request.QueryString["location"] == "middle")
            {
                g.Clear(headColour);
            }
            else if (Request.QueryString["location"] == "middle,centre")
            {
                g.Clear(headColour);
            }
            else if (Request.QueryString["location"] == "middle,left")
            {
                g.Clear(headColour);
            }
            else if (Request.QueryString["location"] == "middle,right")
            {
                g.Clear(headColour);
            }
            else if (Request.QueryString["location"] == "top,centre")
            {
                g.Clear(headColour);
            }
            else if (Request.QueryString["location"] == "bottom,centre")
            {
                g.Clear(headColour);
            }

            if (ext == "gif")
            {
                SaveGIFWithNewColorTable(cornerImage, imagePath, 2, true);
                Response.WriteFile(imagePath);
            }
            else
            {
                try
                {
                    FileStream newFileStream = new FileStream(imagePath, FileMode.Create);
                    cornerImage.Save(newFileStream, ImageFormat.Png);
                }
                catch { }

                MemoryStream newStream = new MemoryStream();

                cornerImage.Save(newStream, ImageFormat.Png);

                newStream.WriteTo(Response.OutputStream);
            }
        }

        /// <summary>
        /// http://support.microsoft.com/kb/319061
        /// </summary>
        /// <param name="nColors"></param>
        /// <returns></returns>
        protected ColorPalette GetColorPalette(uint nColors)
        {
            // Assume monochrome image.
            PixelFormat bitscolordepth = PixelFormat.Format1bppIndexed;
            ColorPalette palette;    // The Palette we are stealing
            Bitmap bitmap;     // The source of the stolen palette

            // Determine number of colors.
            if (nColors > 2)
                bitscolordepth = PixelFormat.Format4bppIndexed;
            if (nColors > 16)
                bitscolordepth = PixelFormat.Format8bppIndexed;

            // Make a new Bitmap object to get its Palette.
            bitmap = new Bitmap(1, 1, bitscolordepth);

            palette = bitmap.Palette;   // Grab the palette

            bitmap.Dispose();           // cleanup the source Bitmap

            return palette;             // Send the palette back
        }

        /// <summary>
        /// http://support.microsoft.com/kb/319061
        /// </summary>
        /// <param name="image"></param>
        /// <param name="filename"></param>
        /// <param name="nColors"></param>
        /// <param name="fTransparent"></param>
        protected void SaveGIFWithNewColorTable(Image image, string filename, uint nColors, bool fTransparent)
        {

            // GIF codec supports 256 colors maximum, monochrome minimum.
            if (nColors > 256)
                nColors = 256;
            if (nColors < 2)
                nColors = 2;

            // Make a new 8-BPP indexed bitmap that is the same size as the source image.
            int Width = image.Width;
            int Height = image.Height;

            // Always use PixelFormat8bppIndexed because that is the color
            // table-based interface to the GIF codec.
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format8bppIndexed);

            // Create a color palette big enough to hold the colors you want.
            ColorPalette pal = GetColorPalette(nColors);

            // Initialize a new color table with entries that are determined
            // by some optimal palette-finding algorithm; for demonstration 
            // purposes, use a grayscale.
            for (uint i = 0; i < nColors; i++)
            {
                uint Alpha = 0xFF;                      // Colors are opaque.
                uint Intensity = i * 0xFF / (nColors - 1);    // Even distribution. 

                // The GIF encoder makes the first entry in the palette
                // that has a ZERO alpha the transparent color in the GIF.
                // Pick the first one arbitrarily, for demonstration purposes.

                if (i == 0 && fTransparent) // Make this color index...
                    Alpha = 0;          // Transparent

                // Create a gray scale for demonstration purposes.
                // Otherwise, use your favorite color reduction algorithm
                // and an optimum palette for that algorithm generated here.
                // For example, a color histogram, or a median cut palette.
                pal.Entries[i] = Color.FromArgb((int)Alpha, (int)headColour.R, (int)headColour.G, (int)headColour.B);
            }

            // Set the palette into the new Bitmap object.
            bitmap.Palette = pal;


            // Use GetPixel below to pull out the color data of Image.
            // Because GetPixel isn't defined on an Image, make a copy 
            // in a Bitmap instead. Make a new Bitmap that is the same size as the
            // image that you want to export. Or, try to
            // interpret the native pixel format of the image by using a LockBits
            // call. Use PixelFormat32BppARGB so you can wrap a Graphics  
            // around it.
            Bitmap BmpCopy = new Bitmap(Width,
                                    Height,
                                    PixelFormat.Format32bppArgb);
            {
                Graphics g = Graphics.FromImage(BmpCopy);

                g.PageUnit = GraphicsUnit.Pixel;

                // Transfer the Image to the Bitmap
                g.DrawImage(image, 0, 0, Width, Height);

                // g goes out of scope and is marked for garbage collection.
                // Force it, just to keep things clean.
                g.Dispose();
            }

            // Lock a rectangular portion of the bitmap for writing.
            BitmapData bitmapData;
            Rectangle rect = new Rectangle(0, 0, Width, Height);

            bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.WriteOnly,
                PixelFormat.Format8bppIndexed);

            // Write to the temporary buffer that is provided by LockBits.
            // Copy the pixels from the source image in this loop.
            // Because you want an index, convert RGB to the appropriate
            // palette index here.
            IntPtr pixels = bitmapData.Scan0;

            unsafe
            {
                // Get the pointer to the image bits.
                // This is the unsafe operation.
                byte* pBits;
                if (bitmapData.Stride > 0)
                    pBits = (byte*)pixels.ToPointer();
                else
                    // If the Stide is negative, Scan0 points to the last 
                    // scanline in the buffer. To normalize the loop, obtain
                    // a pointer to the front of the buffer that is located 
                    // (Height-1) scanlines previous.
                    pBits = (byte*)pixels.ToPointer() + bitmapData.Stride * (Height - 1);
                uint stride = (uint)Math.Abs(bitmapData.Stride);

                for (uint row = 0; row < Height; ++row)
                {
                    for (uint col = 0; col < Width; ++col)
                    {
                        // Map palette indexes for a gray scale.
                        // If you use some other technique to color convert,
                        // put your favorite color reduction algorithm here.
                        Color pixel;    // The source pixel.

                        // The destination pixel.
                        // The pointer to the color index byte of the
                        // destination; this real pointer causes this
                        // code to be considered unsafe.
                        byte* p8bppPixel = pBits + row * stride + col;

                        pixel = BmpCopy.GetPixel((int)col, (int)row);

                        // Use luminance/chrominance conversion to get grayscale.
                        // Basically, turn the image into black and white TV.
                        // Do not calculate Cr or Cb because you 
                        // discard the color anyway.
                        // Y = Red * 0.299 + Green * 0.587 + Blue * 0.114

                        // This expression is best as integer math for performance,
                        // however, because GetPixel listed earlier is the slowest 
                        // part of this loop, the expression is left as 
                        // floating point for clarity.

                        double luminance = (pixel.R * 0.299) +
                            (pixel.G * 0.587) +
                            (pixel.B * 0.114);

                        // Gray scale is an intensity map from black to white.
                        // Compute the index to the grayscale entry that
                        // approximates the luminance, and then round the index.
                        // Also, constrain the index choices by the number of
                        // colors to do, and then set that pixel's index to the 
                        // byte value.
                        *p8bppPixel = (byte)(luminance * (nColors - 1) / 255 + 0.5);

                    } /* end loop for col */
                } /* end loop for row */
            } /* end unsafe */

            // To commit the changes, unlock the portion of the bitmap.  
            bitmap.UnlockBits(bitmapData);

            bitmap.Save(filename, ImageFormat.Gif);

            // Bitmap goes out of scope here and is also marked for
            // garbage collection.
            // Pal is referenced by bitmap and goes away.
            // BmpCopy goes out of scope here and is marked for garbage
            // collection. Force it, because it is probably quite large.
            // The same applies to bitmap.
            BmpCopy.Dispose();
            bitmap.Dispose();

        }


    }
}
